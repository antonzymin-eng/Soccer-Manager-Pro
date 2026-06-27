// File:     src/defensive-ai/DefensiveAITick.cs
// Created:  2026-05-29
// Modified: 2026-06-27 (Match Engine Phase D D4: CaptureState snapshot seam)
// Author:   —
// Spec:     Defensive AI #14 §3.13, §4.1, §4.2, Code Standards #20
// Purpose:  10 Hz Defensive AI orchestrator for one team. Implements the 9-step pipeline
//           from §3.13: phase gate → pool filter → emergency overrides → assignment →
//           tackle intent → offside trap → anti-chaos → publish.

using UnityEngine;
using Unity.Profiling;

using TacticalDirector.PositioningAI;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// 10 Hz Defensive AI orchestrator for one team.
    ///
    /// Lifecycle: construct once per team per match → call <see cref="Tick"/> every 10 Hz →
    /// read results via <see cref="GetMarkDirective"/>, <see cref="GetAssignment"/>,
    /// and <see cref="GetTackleIntentRequests"/>.
    ///
    /// Pre-allocated scratch buffers ensure zero heap allocation on the hot path after
    /// construction. Defensive AI #14 §3.13 / §4.1.
    ///
    /// Stage 1 activation: all output surfaces are populated at Stage 0 but integration
    /// with the match orchestrator and Decision Tree #8 occurs at Stage 1 (KD-16).
    /// </summary>
    public sealed class DefensiveAITick
    {
        // ── Profiler markers ──────────────────────────────────────────────────
        private static readonly ProfilerMarker s_TickMarker =
            new ProfilerMarker("DefensiveAI.Tick");

        // ── Persistent per-ENTITY state ───────────────────────────────────────
        // Keyed by EntityId (NOT pool slot): the HOLD_SHAPE pool is compacted EntityId-ascending
        // each tick, so slot indices shift whenever membership changes (a presser joins/leaves,
        // a substitution). Carrying hysteresis + the prior committed assignment by slot would
        // mis-attribute one agent's dwell lock to another (AR-2 H-1). These two arrays are the
        // authoritative cross-tick stores; the per-slot working buffers below are loaded from
        // and written back to them every tick.
        private readonly MarkHysteresisState[] _hysteresisByEntity;  // entityId → hysteresis state
        private readonly MarkAssignment[]      _prevAssignByEntity;  // entityId → last committed assignment
        private OffsideLineState               _offsideState; // per-team (value-type in-place)
        private int                            _lastProcessedTick = -1;

        // ── Pre-allocated output / scratch buffers (indexed by pool slot, per tick) ───
        private readonly int[]                 _poolBuffer;       // HOLD_SHAPE pool (EntityId-ascending)
        private readonly MarkAssignment[]      _assignments;      // per pool-slot assignment output
        private readonly MarkHysteresisState[] _hysteresis;       // per pool-slot working hysteresis (loaded per tick)
        private readonly TackleIntentRequest[] _tackleBuffer;     // tackle intent output

        // ── Published output ──────────────────────────────────────────────────
        private MarkDirective _lastDirective;
        private int           _poolCount;
        private int           _tackleCount;

        // ── EntityId → assignment-index map ──────────────────────────────────
        private readonly int[] _entityToPoolIdx; // entityId → pool slot index; -1 = not in pool
        private readonly int   _entityIdCapacity;

        // ── Public accessors ──────────────────────────────────────────────────

        /// <summary>The mark directive produced by the most recent Tick() call.</summary>
        public MarkDirective LastDirective => _lastDirective;

        /// <summary>Number of valid TackleIntentRequests produced by the most recent Tick().</summary>
        public int TackleCount => _tackleCount;

        /// <summary>
        /// Constructs the orchestrator for one team.
        /// </summary>
        /// <param name="maxEntityId">Highest possible EntityId in the match. Default 64.</param>
        public DefensiveAITick(int maxEntityId = 64)
        {
            int squadSize = DefensiveAIConstants.SQUAD_SIZE;

            _poolBuffer   = new int[squadSize];
            _assignments  = new MarkAssignment[squadSize];
            _hysteresis   = new MarkHysteresisState[squadSize];
            _tackleBuffer = new TackleIntentRequest[squadSize];

            for (int i = 0; i < squadSize; i++)
                _hysteresis[i] = MarkHysteresisState.Default();

            _offsideState   = OffsideLineState.Default();
            _lastDirective  = MarkDirective.Inactive(0);

            _entityIdCapacity  = maxEntityId + 1;
            _entityToPoolIdx   = new int[_entityIdCapacity];
            _hysteresisByEntity = new MarkHysteresisState[_entityIdCapacity];
            _prevAssignByEntity = new MarkAssignment[_entityIdCapacity];
            for (int i = 0; i < _entityIdCapacity; i++)
            {
                _entityToPoolIdx[i]    = -1;
                _hysteresisByEntity[i] = MarkHysteresisState.Default();
                _prevAssignByEntity[i] = MarkAssignment.MakeZonal(i, Vector2.zero, -1);
            }
        }

        // ── Per-tick entry point ───────────────────────────────────────────────

        /// <summary>
        /// Executes one 10 Hz Defensive AI tick (§3.13 9-step pipeline).
        /// Results are immediately available via <see cref="GetMarkDirective"/>,
        /// <see cref="GetAssignment"/>, and <see cref="GetTackleIntentRequests"/>.
        /// </summary>
        /// <param name="snapshot">Current tick input; populated by the match orchestrator.</param>
        public void Tick(DefensiveSnapshot snapshot)
        {
            using var _ = s_TickMarker.Auto();

            int defensiveTeamId = snapshot.DefensiveTeamId;
            int currentTick     = snapshot.TickIndex;

            // F1 — stale perception (FR-DA-029): reuse previous output.
            if (currentTick < _lastProcessedTick)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogWarning(
                    $"[DefensiveAI] F1 stale tick {currentTick} < last {_lastProcessedTick}; reusing previous directive.");
#endif
                return;
            }

            // Step 1: Read inputs (snapshot pre-populated by orchestrator).
            // Mirror defensive line depth into directive.
            _lastDirective = MarkDirective.Inactive(defensiveTeamId);
            _lastDirective.OffensiveLineDepth = snapshot.DefensiveLineDepth;

            // F2 — #12 slot unavailable: any agent with BaselineSlot == sentinel → all-ZONAL.
            // (Orchestrator sets BaselineSlot to Vector2.negativeInfinity for SENTINEL_NO_SLOT.)
            if (HasSentinelSlot(snapshot, defensiveTeamId))
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogWarning("[DefensiveAI] F2 SENTINEL_NO_SLOT: emitting all-ZONAL.");
#endif
                EmitAllZonal(snapshot, currentTick);
                _lastProcessedTick = currentTick;
                return;
            }

            // Step 2: Phase gate (§3.1 / FR-DA-013 / KD-19).
            if (snapshot.TeamPhase == Phase.InPoss)
            {
                EmitAllZonal(snapshot, currentTick);
                _lastProcessedTick = currentTick;
                return;
            }

            // Step 3: Build HOLD_SHAPE pool (§3.2 / FR-DA-009 / FR-DA-010).
            HoldShapePoolFilter.BuildPool(snapshot, _poolBuffer, out _poolCount);
            RebuildEntityMap(_poolCount);

            // Step 3a: Load each pool slot's working assignment + hysteresis from the persistent
            // per-ENTITY stores (AR-2 H-1). The pool is compacted EntityId-ascending, so slot p
            // can hold a different agent than last tick; the prior committed assignment and dwell
            // lock must travel with the EntityId, not the slot. OverriddenThisTick is the per-tick
            // transient flag reset here (former §3.13 Step 3b).
            for (int p = 0; p < _poolCount; p++)
            {
                int entityId = _poolBuffer[p];
                if (entityId >= 0 && entityId < _entityIdCapacity)
                {
                    _assignments[p] = _prevAssignByEntity[entityId];
                    _hysteresis[p]  = _hysteresisByEntity[entityId];
                }
                else
                {
                    _assignments[p] = MarkAssignment.MakeZonal(entityId, Vector2.zero, currentTick);
                    _hysteresis[p]  = MarkHysteresisState.Default();
                }
                _assignments[p].OverriddenThisTick = false;
            }

            // Step 4: Last-man predicate (§3.8) — highest priority override.
            LastManDetector.Evaluate(snapshot, _poolBuffer, _poolCount, out LastManResult lastManResult);

            if (lastManResult.IsEmergency)
            {
                _lastDirective.EmergencyFlag = true;

                // Override last-man agent's assignment.
                int lmPoolIdx = lastManResult.LastManPoolIndex;
                if (lmPoolIdx >= 0 && lastManResult.AdvancingAttackerEntityId >= 0)
                {
                    // Target the advancing attacker's perceived position (§3.8 Step 3).
                    int advAIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, lastManResult.AdvancingAttackerEntityId);
                    Vector2 oppPos = advAIdx >= 0 ? snapshot.Agents[advAIdx].Position : Vector2.zero;

                    _assignments[lmPoolIdx] = new MarkAssignment
                    {
                        AgentEntityId      = lastManResult.LastManEntityId,
                        Mode               = MarkMode.InterceptRunner,
                        TargetEntityId     = lastManResult.AdvancingAttackerEntityId,
                        TargetPosition     = oppPos,
                        ValidThroughTick   = currentTick,
                        OverriddenThisTick = true,
                        IsManuallyAssigned = false,
                    };
                    MarkHysteresis.Reset(ref _hysteresis[lmPoolIdx]);
                }

                // Step 4a: GK out-of-position check (§3.9).
                if (lastManResult.IsGkOutOfZone && lastManResult.CoverAgentPoolIndex >= 0)
                {
                    int coverPoolIdx = lastManResult.CoverAgentPoolIndex;
                    if (_offsideState.CoverGkZoneActiveTicks < DefensiveAIConstants.CoverGkZoneMaxTicks)
                    {
                        _assignments[coverPoolIdx] = new MarkAssignment
                        {
                            AgentEntityId      = lastManResult.CoverAgentEntityId,
                            Mode               = MarkMode.CoverGkZone,
                            TargetEntityId     = -1,
                            TargetPosition     = lastManResult.AbandonedZoneCenter,
                            ValidThroughTick   = currentTick,
                            OverriddenThisTick = true,
                            IsManuallyAssigned = false,
                        };
                        MarkHysteresis.Reset(ref _hysteresis[coverPoolIdx]);
                        _offsideState.CoverGkZoneActiveTicks++;
                    }
                    else
                    {
                        // Safety release: revert to ZONAL (§3.9.3).
                        int cId = lastManResult.CoverAgentEntityId;
                        int cAIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, cId);
                        Vector2 slot = cAIdx >= 0 ? snapshot.Agents[cAIdx].BaselineSlot : Vector2.zero;
                        _assignments[coverPoolIdx] = MarkAssignment.MakeZonal(cId, slot, currentTick);
                        _offsideState.CoverGkZoneActiveTicks = 0;
                    }
                }
                else
                {
                    _offsideState.CoverGkZoneActiveTicks = 0;
                }
            }
            else
            {
                // No emergency: reset cover-GK counter.
                _lastDirective.EmergencyFlag         = false;
                _offsideState.CoverGkZoneActiveTicks = 0;
            }

            // Step 5: Regular assignment loop (§3.3) — EntityId-ascending.
            MarkAssigner.Assign(snapshot, _poolBuffer, _poolCount, currentTick, _assignments, _hysteresis);

            // Step 6: Tackle intent evaluation (§3.6).
            TackleIntentEvaluator.Evaluate(
                snapshot, _poolBuffer, _poolCount,
                _assignments,
                lastManResult.LastManPoolIndex,
                lastManResult.IsEmergency,
                _tackleBuffer, out _tackleCount);

            // Step 7: Offside trap check (§3.7).
            OffsideTrapController.Update(
                snapshot, _poolBuffer, _poolCount, currentTick,
                ref _offsideState, ref _lastDirective, _assignments);

            // Step 8: Anti-chaos enforcement (§3.10 / FR-DA-024).
            bool enforced = InvariantEnforcer.Enforce(
                snapshot, _poolBuffer, _poolCount, currentTick, _assignments);

            if (!enforced)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogWarning($"[DefensiveAI] F4 DEFENSIVE_INVARIANT_FALLBACK team={snapshot.DefensiveTeamId} tick={currentTick}");
#endif
                EmitAllZonal(snapshot, currentTick);
                _tackleCount = 0;
                _lastProcessedTick = currentTick;
                return;
            }

            // Step 9: Publish + persist per-entity state for the next tick (AR-2 H-1).
            WriteBackEntityState();
            _lastDirective.TeamId = defensiveTeamId;
            _lastProcessedTick    = currentTick;
        }

        /// <summary>
        /// Persists each pool slot's committed assignment + hysteresis back into the per-ENTITY
        /// stores so the next tick reloads them by EntityId regardless of slot churn (AR-2 H-1).
        /// </summary>
        private void WriteBackEntityState()
        {
            for (int p = 0; p < _poolCount; p++)
            {
                int entityId = _poolBuffer[p];
                if (entityId >= 0 && entityId < _entityIdCapacity)
                {
                    _prevAssignByEntity[entityId] = _assignments[p];
                    _hysteresisByEntity[entityId] = _hysteresis[p];
                }
            }
        }

        // ── Public accessors ──────────────────────────────────────────────────

        /// <summary>Returns the committed mark directive for the defensive team.</summary>
        public MarkDirective GetMarkDirective() => _lastDirective;

        /// <summary>
        /// Returns the committed <see cref="MarkAssignment"/> for the given EntityId.
        /// Returns a ZONAL assignment with zero position when the EntityId is not in the pool.
        /// </summary>
        public MarkAssignment GetAssignment(int entityId)
        {
            if (entityId >= 0 && entityId < _entityIdCapacity)
            {
                int poolIdx = _entityToPoolIdx[entityId];
                if (poolIdx >= 0)
                    return _assignments[poolIdx];
            }

            return MarkAssignment.MakeZonal(entityId, Vector2.zero, _lastProcessedTick);
        }

        /// <summary>
        /// Snapshot seam: bundles this tick's cross-tick state (per-agent mark hysteresis, per-agent last
        /// committed assignment, per-team offside-line state) into a <see cref="DefensiveTickState"/> view
        /// so a host snapshot layer can serialize it canonically for deterministic save/restore (parallel
        /// to the Positioning / Pressing seams). The bundled arrays are the live, allocated-once instances
        /// (read-only serialization use only).
        /// </summary>
        public DefensiveTickState CaptureState() =>
            new DefensiveTickState(_hysteresisByEntity, _prevAssignByEntity, in _offsideState);

        /// <summary>
        /// Returns a read-only span of the tackle intent requests produced by the most recent Tick().
        /// Length == <see cref="TackleCount"/>.
        /// </summary>
        public System.ReadOnlySpan<TackleIntentRequest> GetTackleIntentRequests()
        {
            return new System.ReadOnlySpan<TackleIntentRequest>(_tackleBuffer, 0, _tackleCount);
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private void EmitAllZonal(DefensiveSnapshot snapshot, int currentTick)
        {
            _poolCount   = 0;
            _tackleCount = 0;

            RebuildEntityMap(0);

            _lastDirective = new MarkDirective
            {
                TeamId             = snapshot.DefensiveTeamId,
                OffensiveLineDepth = snapshot.DefensiveLineDepth,
                OffsideTrapActive  = false,
                StepUpTargetDepth  = 0f,
                EmergencyFlag      = false,
            };

            // Rebuild pool to populate ZONAL assignments for orchestrator consumers.
            HoldShapePoolFilter.BuildPool(snapshot, _poolBuffer, out _poolCount);
            RebuildEntityMap(_poolCount);

            for (int p = 0; p < _poolCount; p++)
            {
                int agentId = _poolBuffer[p];
                int aIdx    = HoldShapePoolFilter.SnapshotIndexOf(snapshot, agentId);
                Vector2 slot = aIdx >= 0 ? snapshot.Agents[aIdx].BaselineSlot : Vector2.zero;
                _assignments[p] = MarkAssignment.MakeZonal(agentId, slot, currentTick);
                // AR-2 M-1: clear dwell accumulators on the all-ZONAL paths (InPoss / F2 / F4) so a
                // stale candidate/HoldTicks from a prior defensive phase cannot leak into the first
                // tick after possession is regained.
                _hysteresis[p] = MarkHysteresisState.Default();
            }

            WriteBackEntityState();
        }

        private void RebuildEntityMap(int poolCount)
        {
            for (int i = 0; i < _entityIdCapacity; i++)
                _entityToPoolIdx[i] = -1;

            for (int p = 0; p < poolCount; p++)
            {
                int entityId = _poolBuffer[p];
                if (entityId >= 0 && entityId < _entityIdCapacity)
                    _entityToPoolIdx[entityId] = p;
            }
        }

        private static bool HasSentinelSlot(DefensiveSnapshot snapshot, int defensiveTeamId)
        {
            // Sentinel: orchestrator sets BaselineSlot.x = float.NegativeInfinity for SENTINEL_NO_SLOT.
            for (int i = 0; i < snapshot.AgentCount; i++)
            {
                ref readonly DefensiveAgentSnapshot a = ref snapshot.Agents[i];
                if (a.TeamId != defensiveTeamId) continue;
                if (!a.IsActive) continue;
                if (a.IsGoalkeeper) continue;
                if (float.IsNegativeInfinity(a.BaselineSlot.x))
                    return true;
            }

            return false;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                              |
// | 1.1     | 2026-06-15 | —      | AR-2 H-1: hysteresis + prior assignment were carried in per-pool-SLOT arrays across   |
// |         |            |        |   ticks, but the pool is compacted EntityId-ascending so slots shift on membership     |
// |         |            |        |   churn (presser join/leave, substitution), mis-attributing one agent's dwell lock to  |
// |         |            |        |   another. Added persistent per-ENTITY stores (_hysteresisByEntity / _prevAssignByEntity); |
// |         |            |        |   Step 3a loads working slot buffers by EntityId, Step 9 (and EmitAllZonal) writes back. |
// |         |            |        |   AR-2 M-1: EmitAllZonal now resets per-entity hysteresis on InPoss/F2/F4 paths.        |
// |         |            |        |   AR-2 L-1: removed the dead last-man placeholder oppPos lookup (immediately overwritten). |
// | 1.3     | 2026-06-27 | —      | Match Engine Phase D D4 follow-up: CaptureState() snapshot seam bundles the cross-tick    |
// |         |            |        |   state (per-entity mark hysteresis, per-entity last assignment, per-team offside state)  |
// |         |            |        |   into a DefensiveTickState view for the host snapshot layer. Read-only; no behaviour change. |
#endregion
