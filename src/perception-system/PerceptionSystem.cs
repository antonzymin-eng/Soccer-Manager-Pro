// File:     src/perception-system/PerceptionSystem.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Perception System #7 §3.0–§3.8, §4.1, §4.6, Code Standards #20
// Purpose:  Main 10Hz orchestrator. Runs the full perception pipeline for all 22 agents
//           each heartbeat: spatial query → pressure → FoV → occlusion → L_rec →
//           shoulder check → ball perception → ViewBuilder assembly (§3.0 Steps 1–7).
//           Handles forced mid-heartbeat refresh (§4.6). Constructor-injected dependencies.
//           Zero heap allocation per heartbeat (INV-10); all buffers pre-allocated.

using System.Collections.Generic;

using UnityEngine;
using Unity.Profiling;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.CollisionSystem;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// 10Hz per-agent perception orchestrator. Produces one FilteredView and one
    /// PerceptionDiagnostics per agent per heartbeat via a deterministic pipeline.
    /// Maintains recognition latency counters and shoulder check scheduling state
    /// across heartbeats. Handles forced mid-heartbeat refresh (§4.6).
    /// Constructor-injected dependencies (FR-CS-051–054). Zero heap allocation on hot path.
    /// Perception System #7 §3.0–§3.8, §4.1.
    /// </summary>
    public sealed class PerceptionSystem
    {
        // ── Dependencies ─────────────────────────────────────────────────────────────

        private readonly SpatialHashGrid _spatialHash;

        // ── Internal state (persisted across heartbeats) ─────────────────────────────

        private readonly RecognitionLatencyTracker _latencyTracker;
        private readonly ShoulderCheckScheduler    _shoulderCheckScheduler;

        // Per-agent ball state persisted from the previous heartbeat (§3.5.2)
        private readonly bool[]    _ballVisiblePrev;
        private readonly Vector2[] _ballPerceivedPositionPrev;
        private readonly int[]     _ballStalenessFramesPrev;

        // ── Pre-allocated output buffers (INV-10) ────────────────────────────────────

        private readonly FilteredView[]          _filteredViews;
        private readonly PerceptionDiagnostics[] _diagnostics;

        // ── Per-pipeline temporary buffers (reused per agent; not re-allocated) ──────

        // Candidate entity IDs from the spatial hash query (agents 0–21 + ball -1)
        private readonly int[] _candidateIds;
        private int            _candidateCount;

        // Per-agent FoV visibility flag: true if entity passed range+FoV+occlusion this pipeline run
        private readonly bool[] _entitySeenThisFrame;

        // Per-agent dedup flag: true if entity ID was already copied from spatial hash candidates this run.
        // Prevents latency counter double-increment when an entity spans multiple grid cells (INV-10 safe).
        private readonly bool[] _candidateVisited;

        // ── Profiler Markers ─────────────────────────────────────────────────────────

        private static readonly ProfilerMarker s_heartbeatMarker =
            new ProfilerMarker("PerceptionSystem.OnHeartbeat");

        private static readonly ProfilerMarker s_forcedRefreshMarker =
            new ProfilerMarker("PerceptionSystem.HandleForcedRefresh");

        // ── Constructor ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Allocates all pre-allocated buffers and wires PerceivedAgent arrays into FilteredView slots.
        /// No allocation occurs during OnHeartbeat or HandleForcedRefresh calls after this point.
        /// Perception System #7 §4.1.
        /// </summary>
        public PerceptionSystem(SpatialHashGrid spatialHash)
        {
            _spatialHash            = spatialHash;
            _latencyTracker         = new RecognitionLatencyTracker();
            _shoulderCheckScheduler = new ShoulderCheckScheduler();

            int maxAgents = PerceptionConstants.MaxAgents;

            _ballVisiblePrev           = new bool[maxAgents];
            _ballPerceivedPositionPrev = new Vector2[maxAgents];
            _ballStalenessFramesPrev   = new int[maxAgents];

            _filteredViews  = new FilteredView[maxAgents];
            _diagnostics    = new PerceptionDiagnostics[maxAgents];

            // +1 to accommodate ball entity (ID = -1 stored as a value, not as an index)
            _candidateIds        = new int[maxAgents + 1];
            _entitySeenThisFrame = new bool[maxAgents];
            _candidateVisited    = new bool[maxAgents];

            // Pre-wire PerceivedAgent arrays into each FilteredView slot (INV-10: no runtime alloc)
            for (int i = 0; i < maxAgents; i++)
            {
                _filteredViews[i].VisibleTeammates         = new PerceivedAgent[PerceptionConstants.MaxVisibleTeammates];
                _filteredViews[i].VisibleOpponents         = new PerceivedAgent[PerceptionConstants.MaxVisibleOpponents];
                _filteredViews[i].BlindSidePerceivedAgents = new PerceivedAgent[PerceptionConstants.MaxBlindSideAgents];
            }
        }

        // ── Public API ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the most recently assembled FilteredView for the given agent.
        /// Valid after OnHeartbeat or HandleForcedRefresh for this agent.
        /// The PerceivedAgent array references inside are shared with internal buffers;
        /// read entries [0..Count-1] before the next heartbeat. §4.5.2.
        /// </summary>
        public FilteredView GetFilteredView(int agentId) => _filteredViews[agentId];

        /// <summary>Returns the most recently assembled PerceptionDiagnostics for the given agent. Not delivered to Decision Tree. §3.7.2.</summary>
        public PerceptionDiagnostics GetDiagnostics(int agentId) => _diagnostics[agentId];

        /// <summary>
        /// 10Hz heartbeat entry point. Runs the full perception pipeline for all 22 agents
        /// in ascending agent ID order (0→21). Populates FilteredView and PerceptionDiagnostics
        /// for each agent. Read results via GetFilteredView / GetDiagnostics.
        /// Perception System #7 §3.0, §4.5.2.
        /// </summary>
        /// <param name="currentFrame">Current 10Hz heartbeat tick index.</param>
        /// <param name="agentStates">Full agent state array (length must equal MaxAgents = 22). FM-AM-01/02.</param>
        /// <param name="ballState">Current BallState snapshot from Ball Physics #1. §4.3.1.</param>
        /// <param name="agentAttrs">Per-agent perception attribute snapshot (Decisions, Anticipation, TeamId, IsHalfTurned). §4.2.2.</param>
        /// <param name="agentHasPossession">Per-agent possession flag (length = MaxAgents). External possession tracker per §4.2.1 / XC-4.2-02.</param>
        public void OnHeartbeat(
            int                       currentFrame,
            AgentState[]              agentStates,
            BallState                 ballState,
            PerceptionAgentAttributes[] agentAttrs,
            bool[]                    agentHasPossession)
        {
            using var _ = s_heartbeatMarker.Auto();

            // FM-AM-01: null guard
            if (agentStates == null)
            {
                Debug.LogError("[PerceptionSystem] agentStates is null — heartbeat skipped.");
                return;
            }

            // FM-AM-02: length guard
            if (agentStates.Length != PerceptionConstants.MaxAgents)
            {
                Debug.LogError($"[PerceptionSystem] agentStates.Length={agentStates.Length} ≠ {PerceptionConstants.MaxAgents} — heartbeat skipped.");
                return;
            }

            if (agentAttrs == null || agentAttrs.Length != PerceptionConstants.MaxAgents)
            {
                Debug.LogError("[PerceptionSystem] agentAttrs is null or wrong length — heartbeat skipped.");
                return;
            }

            if (agentHasPossession != null && agentHasPossession.Length != PerceptionConstants.MaxAgents)
            {
                Debug.LogError("[PerceptionSystem] agentHasPossession wrong length — heartbeat skipped.");
                return;
            }

            for (int agentId = 0; agentId < PerceptionConstants.MaxAgents; agentId++)
            {
                RunAgentPipeline(
                    agentId, currentFrame, agentStates, ballState, agentAttrs,
                    agentHasPossession, forcedRefresh: false,
                    triggeringEntityId: PerceptionConstants.AGENT_ID_NONE);
            }
        }

        /// <summary>
        /// Handles a forced mid-heartbeat refresh for the agents named in the event.
        /// Runs the full pipeline immediately (out of the normal 10Hz schedule) for the
        /// primary and secondary agents. Sets L_rec = 0 for the triggering entity only (§4.6.2).
        /// Publishes PerceptionRefreshEvent via EventBusStub (no-op at Stage 0).
        /// Called directly by Ball Physics or Collision System at Stage 0 (§4.6.1 coupling note).
        /// Perception System #7 §3.8, §4.6.
        /// </summary>
        public void HandleForcedRefresh(
            in PerceptionRefreshEvent evt,
            AgentState[]              agentStates,
            BallState                 ballState,
            PerceptionAgentAttributes[] agentAttrs,
            bool[]                    agentHasPossession)
        {
            using var _ = s_forcedRefreshMarker.Auto();

            if (agentStates == null || agentStates.Length != PerceptionConstants.MaxAgents) return;
            if (agentAttrs  == null || agentAttrs.Length  != PerceptionConstants.MaxAgents) return;

            if (evt.PrimaryAgentId >= 0 && evt.PrimaryAgentId < PerceptionConstants.MaxAgents)
            {
                RunAgentPipeline(
                    evt.PrimaryAgentId, evt.SimulationFrame,
                    agentStates, ballState, agentAttrs, agentHasPossession,
                    forcedRefresh: true, triggeringEntityId: evt.TriggeringEntityId);
            }

            if (evt.SecondaryAgentId >= 0 && evt.SecondaryAgentId < PerceptionConstants.MaxAgents)
            {
                RunAgentPipeline(
                    evt.SecondaryAgentId, evt.SimulationFrame,
                    agentStates, ballState, agentAttrs, agentHasPossession,
                    forcedRefresh: true, triggeringEntityId: evt.TriggeringEntityId);
            }

            EventBusStub.Publish(in evt);
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Full perception pipeline for one agent. Seven steps per §3.0.
        /// Writes results into _filteredViews[agentId] and _diagnostics[agentId].
        /// Perception System #7 §3.0–§3.8.
        /// </summary>
        private void RunAgentPipeline(
            int                       agentId,
            int                       currentFrame,
            AgentState[]              agentStates,
            BallState                 ballState,
            PerceptionAgentAttributes[] agentAttrs,
            bool[]                    agentHasPossession,
            bool                      forcedRefresh,
            int                       triggeringEntityId)
        {
            // ── Step 0: Cache agent state and attributes ──────────────────────────────
            AgentState              observer = agentStates[agentId];
            PerceptionAgentAttributes attrs  = agentAttrs[agentId];

            Vector2 observerPos = observer.Position;
            Vector2 facingDir   = observer.FacingDirection;

            // FM-AM-03: zero facing direction guard — default to (1,0) to avoid degenerate FoV cone
            if (facingDir.sqrMagnitude < PerceptionConstants.FACING_DIR_ZERO_GUARD_SQ)
            {
                facingDir = new Vector2(1.0f, 0.0f);
#if DEVELOPMENT_BUILD
                Debug.LogWarning($"[PerceptionSystem] Agent {agentId}: FacingDirection is zero; defaulting to (1,0). FM-AM-03.");
#endif
            }

            bool hasPossession = agentHasPossession != null && agentHasPossession[agentId];

            // ── Step 1: Spatial hash query (candidate enumeration) ────────────────────
            // Reset tracking buffers. _candidateVisited deduplicates candidates that appear in
            // multiple grid cells (body-radius straddle); _entitySeenThisFrame tracks FoV+occlusion
            // pass results for the second-pass ProcessInvisible loop (Step 4+5 below).
            for (int i = 0; i < PerceptionConstants.MaxAgents; i++)
            {
                _candidateVisited[i]    = false;
                _entitySeenThisFrame[i] = false;
            }

            Vector3 observerPos3D = new Vector3(observerPos.x, observerPos.y, 0.0f);
            List<int> candidates  = _spatialHash.Query(observerPos3D, PerceptionConstants.MaxPerceptionRange);

            // Dedup BEFORE truncation: the spatial hash can return the same entity from
            // multiple cells (body-radius straddle). _candidateIds holds MaxAgents + 1 unique
            // slots (22 agents + 1 ball), so we scan the entire raw list and the buffer cannot
            // overflow once duplicates are filtered. Truncating the raw list first (old behaviour)
            // could drop a unique agent that only appeared past the cap behind duplicate entries.
            _candidateCount = 0;
            bool ballAdded = false;
            if (candidates != null)
            {
                int rawCount = candidates.Count;
                for (int i = 0; i < rawCount; i++)
                {
                    int id = candidates[i];

                    if (id >= 0 && id < PerceptionConstants.MaxAgents)
                    {
                        // Deduplicate: skip agent IDs seen in a prior cell during this query
                        if (_candidateVisited[id]) continue;
                        _candidateVisited[id] = true;
                    }
                    else if (id < 0)
                    {
                        // Ball sentinel (-1): keep a single entry; multiple cells can return it
                        if (ballAdded) continue;
                        ballAdded = true;
                    }
                    else
                    {
                        // id >= MaxAgents: out-of-range entity, not perceivable here — skip
                        continue;
                    }

                    if (_candidateCount >= _candidateIds.Length) break; // unreachable post-dedup; defensive
                    _candidateIds[_candidateCount++] = id;
                }
            }

            // ── Step 2: Pressure evaluation ───────────────────────────────────────────
            float pressureScalar = PressureEvaluator.ComputePressureScalar(
                observerPos, attrs.TeamId, agentStates, agentAttrs, _candidateIds, _candidateCount);

            // ── Step 3: FoV computation ───────────────────────────────────────────────
            float effectiveFoV          = FovCalculator.ComputeEffectiveFoV(attrs.Decisions, pressureScalar);
            float effectiveFoVHalfAngle = FovCalculator.ComputeEffectiveFoVHalfAngle(effectiveFoV);

            // ── Step 4+5: FoV filter → occlusion test → L_rec update ─────────────────
            int teammatesCount = 0;
            int opponentsCount = 0;

            for (int ci = 0; ci < _candidateCount; ci++)
            {
                int targetId = _candidateIds[ci];

                // Skip ball (-1), self, and out-of-range agent IDs
                if (targetId < 0 || targetId >= PerceptionConstants.MaxAgents || targetId == agentId)
                {
                    continue;
                }

                Vector2 targetPos   = agentStates[targetId].Position;
                float   angSep      = FovCalculator.ComputeAngularSeparationDeg(observerPos, facingDir, targetPos);
                float   angSepAbs   = Mathf.Abs(angSep);
                bool    inFoV       = angSepAbs <= effectiveFoVHalfAngle;

                if (!inFoV) continue;

                bool occluded = OcclusionFilter.IsOccluded(
                    observerPos, targetPos, targetId, agentStates, _candidateIds,
                    _candidateCount, attrs.TeamId, agentAttrs);

                if (occluded) continue;

                // Entity passed range + FoV + occlusion tests
                _entitySeenThisFrame[targetId] = true;

                bool confirmed;
                if (forcedRefresh)
                {
                    // A forced refresh runs out of the normal 10 Hz schedule and must NOT advance
                    // cross-heartbeat latency counters (§4.6.2). L_rec = 0 (force-confirm) applies
                    // ONLY to the directly triggering entity; every other visible entity is reported
                    // from its already-established confirmation state via a side-effect-free read.
                    confirmed = targetId == triggeringEntityId
                        ? _latencyTracker.ProcessVisible(
                            agentId, targetId, attrs.Decisions, attrs.IsHalfTurned,
                            angSepAbs, currentFrame, forcedRefresh: true)
                        : _latencyTracker.IsConfirmed(agentId, targetId);
                }
                else
                {
                    confirmed = _latencyTracker.ProcessVisible(
                        agentId, targetId, attrs.Decisions, attrs.IsHalfTurned,
                        angSepAbs, currentFrame, forcedRefresh: false);
                }

                if (!confirmed) continue;

                AgentState  targetState = agentStates[targetId];
                bool        isTeammate  = agentAttrs[targetId].TeamId == attrs.TeamId;

                if (isTeammate && teammatesCount < PerceptionConstants.MaxVisibleTeammates)
                {
                    _filteredViews[agentId].VisibleTeammates[teammatesCount++] = new PerceivedAgent
                    {
                        AgentId           = targetId,
                        PerceivedPosition = targetState.Position,
                        PerceivedVelocity = targetState.Velocity,
                        ConfidenceScore   = 1.0f
                    };
                }
                else if (!isTeammate && opponentsCount < PerceptionConstants.MaxVisibleOpponents)
                {
                    _filteredViews[agentId].VisibleOpponents[opponentsCount++] = new PerceivedAgent
                    {
                        AgentId           = targetId,
                        PerceivedPosition = targetState.Position,
                        PerceivedVelocity = targetState.Velocity,
                        ConfidenceScore   = 1.0f
                    };
                }
            }

            // Second pass: advance expiry counters for all agents that were NOT fully visible
            // (out of range, failed FoV, or failed occlusion). §3.3.6.
            // Skipped on a forced refresh — it runs out of the normal 10 Hz schedule, so advancing
            // expiry/eviction here would double-tick those counters and prematurely evict confirmed
            // entities (§4.6.2). The next scheduled heartbeat advances them exactly once.
            if (!forcedRefresh)
            {
                for (int targetId = 0; targetId < PerceptionConstants.MaxAgents; targetId++)
                {
                    if (targetId == agentId || _entitySeenThisFrame[targetId]) continue;
                    _latencyTracker.ProcessInvisible(agentId, targetId);
                }
            }

            // ── Step 6: Shoulder check scheduling + blind-side processing ─────────────
            // On a forced refresh, do NOT advance the autonomous schedule — UpdateAgent would
            // fire or retime shoulder checks off the 10 Hz cadence (§4.6.2). Read the current
            // window state and rebuild blind-side entries from established confirmation only.
            if (!forcedRefresh)
            {
                _shoulderCheckScheduler.UpdateAgent(agentId, attrs.Anticipation, hasPossession, currentFrame);
            }
            bool windowActive = _shoulderCheckScheduler.IsWindowActive(agentId);

            int blindSideCount = 0;

            if (windowActive)
            {
                for (int ci = 0; ci < _candidateCount; ci++)
                {
                    int targetId = _candidateIds[ci];

                    if (targetId < 0 || targetId >= PerceptionConstants.MaxAgents || targetId == agentId)
                    {
                        continue;
                    }

                    if (_entitySeenThisFrame[targetId]) continue; // already in FoV — not blind-side

                    Vector2 targetPos = agentStates[targetId].Position;

                    if (!FovCalculator.IsInBlindSide(observerPos, facingDir, targetPos, effectiveFoVHalfAngle))
                    {
                        continue;
                    }

                    float angSep    = FovCalculator.ComputeAngularSeparationDeg(observerPos, facingDir, targetPos);
                    float angSepAbs = Mathf.Abs(angSep);

                    bool confirmed = forcedRefresh
                        ? _shoulderCheckScheduler.IsBlindSideConfirmed(agentId, targetId)
                        : _shoulderCheckScheduler.ProcessBlindSideEntity(
                            agentId, targetId, attrs.Decisions, attrs.IsHalfTurned,
                            angSepAbs, currentFrame, _latencyTracker);

                    if (confirmed && blindSideCount < PerceptionConstants.MaxBlindSideAgents)
                    {
                        AgentState targetState = agentStates[targetId];
                        _filteredViews[agentId].BlindSidePerceivedAgents[blindSideCount++] = new PerceivedAgent
                        {
                            AgentId           = targetId,
                            PerceivedPosition = targetState.Position,
                            PerceivedVelocity = targetState.Velocity,
                            ConfidenceScore   = 1.0f
                        };
                    }
                }
            }

            // ── Step 7: Ball perception ───────────────────────────────────────────────
            BallPerceptionEvaluator.Evaluate(
                observerPos, facingDir, attrs.TeamId, effectiveFoVHalfAngle,
                ballState, agentStates, agentAttrs, _candidateIds, _candidateCount,
                _ballPerceivedPositionPrev[agentId],
                _ballStalenessFramesPrev[agentId],
                out bool ballVisible,
                out Vector2 perceivedBallPos,
                out int     ballStaleness);

            // Persist ball state for the next heartbeat
            _ballVisiblePrev[agentId]           = ballVisible;
            _ballPerceivedPositionPrev[agentId] = perceivedBallPos;
            _ballStalenessFramesPrev[agentId]   = ballStaleness;

            // ── Step 8: ViewBuilder assembly ──────────────────────────────────────────
            ShoulderCheckAnimData animData    = _shoulderCheckScheduler.GetAnimData(agentId);
            float                 windowExpiry = (float)_shoulderCheckScheduler.GetWindowExpiryFrame(agentId);

            ViewBuilder.Build(
                agentId, currentFrame, forcedRefresh,
                ballVisible, perceivedBallPos, ballStaleness,
                teammatesCount, opponentsCount, blindSideCount,
                effectiveFoV, pressureScalar,
                windowActive, windowExpiry, animData,
                ref _filteredViews[agentId],
                ref _diagnostics[agentId]);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                                                                                  |
// | 1.1     | 2026-05-28 | —      | AR-1 fix M-1: added _candidateVisited dedup to prevent latency counter multi-increment from spatial hash cell overlap.                                    |
// | 1.2     | 2026-05-29 | —      | AR-2 fixes L-1/L-2: removed prevBallVisible argument (dead param); added length guards to HandleForcedRefresh; added agentHasPossession length guard. |
// | 1.3     | 2026-06-12 | —      | Build fix (dotnet CI gate): using UnityEngine.Profiling -> Unity.Profiling. ProfilerMarker's actual namespace is Unity.Profiling; the old using was   |
// |         |            |        | CS0246 under Unity and the Linux compile gate alike, so this assembly could not have compiled in-engine. No functional change.                        |
// | 1.4     | 2026-06-13 | —      | AR-3 fixes. H-1: HandleForcedRefresh no longer double-advances cross-heartbeat state — RunAgentPipeline gates the ProcessInvisible expiry loop,        |
// |         |            |        | the ShoulderCheckScheduler.UpdateAgent advance, and the per-entity latency/blind-side increments behind !forcedRefresh; non-triggering entities are    |
// |         |            |        | read via the new side-effect-free IsConfirmed/IsBlindSideConfirmed, triggering entity still force-confirms (L_rec=0) per §4.6.2. M-1: Step 1 candidate |
// |         |            |        | enumeration dedups (agents + ball) across the FULL raw query before any cap, so multi-cell straddle can no longer truncate a unique agent out.         |
#endregion
