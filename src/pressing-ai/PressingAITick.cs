// File:     src/pressing-ai/PressingAITick.cs
// Created:  2026-05-29
// Modified: 2026-06-27 (Match Engine Phase D D4: CaptureState snapshot seam)
// Author:   —
// Spec:     Pressing AI #13 §3.11, §4.2, Code Standards #20
// Purpose:  10 Hz Pressing AI orchestrator for one team. Runs the full §3.11 pipeline:
//           trigger evaluation → primary presser selection → cover-shadow selection →
//           role hysteresis → stamina accumulation → disengage/invariant enforcement.

using UnityEngine;
using Unity.Profiling;

using TacticalDirector.PassMechanics;
using TacticalDirector.PositioningAI;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// 10 Hz Pressing AI orchestrator for one team.
    ///
    /// Lifecycle: construct once per team per match → call Tick() every 10 Hz →
    /// read the result via <see cref="LastDirective"/> and <see cref="GetAssignment"/>.
    ///
    /// Pre-allocated scratch buffers ensure zero heap allocation on the hot path
    /// after construction. Pressing AI #13 §3.11.
    ///
    /// Phase gate: when PositioningAI reports InPoss, Tick() returns
    /// <see cref="PressDirective.Inactive"/> immediately without evaluating triggers.
    /// </summary>
    public sealed class PressingAITick
    {
        // ── Profiler markers ──────────────────────────────────────────────────
        private static readonly ProfilerMarker s_TickMarker =
            new ProfilerMarker("PressingAI.Tick");

        // ── Injected dependencies ─────────────────────────────────────────────
        private readonly PositioningAIView _posAI;
        private readonly PassEventRing     _passRing;

        // ── Persistent state ──────────────────────────────────────────────────
        private readonly RoleHysteresisState _hystState;
        private PressTrigger                 _triggerState;   // struct — value-type in-place
        private int                          _disengageDwell;
        private int                          _cooldownTicks;

        // ── Pre-allocated output buffers ──────────────────────────────────────
        private readonly PressAssignment[] _assignments; // length = SQUAD_SIZE

        // ── Last computed output ──────────────────────────────────────────────
        private PressDirective _lastDirective;
        private int            _lastProcessedTick = -1;
        private int            _assignmentCount;

        // ── Fast EntityId → assignment-index map ─────────────────────────────
        private readonly int[] _entityToAssignmentIdx; // entityId → idx; -1 = not mapped
        private readonly int   _entityIdCapacity;

        // ── Persistent press-fatigue ledger (AR-2 M-1) ───────────────────────
        // Accumulated press-role fatigue per EntityId. The input snapshot is rebuilt
        // each tick from authoritative state and discarded, so per-tick stamina costs
        // (§3.7) must persist here rather than in the throwaway snapshot. Folded onto
        // snapshot.Fatigue at the start of every tick so eligibility (§3.3 / §3.7
        // ceiling) sees the accumulated total.
        private readonly float[] _pressFatigue; // entityId → accumulated press fatigue [0, 1]

        /// <summary>The press directive produced by the most recent Tick() call.</summary>
        public PressDirective LastDirective => _lastDirective;

        /// <summary>
        /// Constructs the orchestrator.
        /// </summary>
        /// <param name="posAI">Read-only view of the Positioning AI for phase and slot queries.</param>
        /// <param name="passRing">Ring buffer of recent pass events for BackwardPass trigger.</param>
        /// <param name="maxEntityId">Highest possible EntityId; default 64.</param>
        public PressingAITick(PositioningAIView posAI, PassEventRing passRing, int maxEntityId = 64)
        {
            _posAI      = posAI;
            _passRing   = passRing;
            _assignments = new PressAssignment[PressingAIConstants.SQUAD_SIZE];

            _entityIdCapacity      = maxEntityId + 1;
            _entityToAssignmentIdx = new int[_entityIdCapacity];
            for (int i = 0; i < _entityIdCapacity; i++)
                _entityToAssignmentIdx[i] = -1;

            // Hysteresis state is keyed by EntityId (AR-2 M-2), so size it to the
            // EntityId space rather than the squad-size array-index space.
            _hystState    = new RoleHysteresisState(_entityIdCapacity);
            _pressFatigue = new float[_entityIdCapacity];

            _lastDirective  = PressDirective.Inactive;
            _triggerState   = default;
            _disengageDwell = 0;
            _cooldownTicks  = 0;
        }

        // ── Per-tick entry point ───────────────────────────────────────────────

        /// <summary>
        /// Executes one 10 Hz Pressing AI tick.
        /// Results are immediately available via <see cref="LastDirective"/> and
        /// <see cref="GetAssignment"/>.
        /// </summary>
        /// <param name="snapshot">Current tick input; TickIndex must be ≥ 0.</param>
        public void Tick(PressingSnapshot snapshot)
        {
            using var _ = s_TickMarker.Auto();

            // F1 — stale perception guard.
            if (snapshot.TickIndex < _lastProcessedTick)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogWarning(
                    $"[PressingAI] F1 stale tick {snapshot.TickIndex} < last {_lastProcessedTick}; reusing previous directive.");
#endif
                return;
            }

            // Fold persisted press fatigue (AR-2 M-1) onto the freshly-populated snapshot
            // so every eligibility/ceiling check this tick sees the accumulated total.
            // Assumes the orchestrator repopulates snapshot.Fatigue from authoritative
            // state each tick (PressingSnapshot contract) — folding once is then exact.
            FoldPressFatigue(snapshot);

            // Phase gate (§3.11 step 0): if own team is InPoss, no pressing.
            Phase phase = _posAI.GetPhase();
            if (phase == Phase.InPoss)
            {
                SetAllHoldShape(snapshot);
                _lastDirective     = PressDirective.Inactive;
                _lastProcessedTick = snapshot.TickIndex;
                return;
            }

            // Cooldown gate: if in reset latency, force HOLD_SHAPE.
            if (DisengageResolver.IsInCooldown(_cooldownTicks))
            {
                _cooldownTicks--;
                SetAllHoldShape(snapshot);
                _lastDirective     = PressDirective.Inactive;
                _lastProcessedTick = snapshot.TickIndex;
                return;
            }

            // ── Step 1: Evaluate triggers (§3.1–§3.2) ────────────────────────
            bool hasLatestPass = _passRing.TryGetLatest(out PassAttemptEvent latestPass);
            TriggerFlags committed = TriggerEvaluator.Evaluate(
                snapshot, latestPass, hasLatestPass, ref _triggerState);

            // ── Step 2: Disengage check (§3.8) ───────────────────────────────
            bool disengaged = DisengageResolver.Evaluate(
                snapshot, committed, ref _disengageDwell, ref _cooldownTicks);

            if (disengaged)
            {
                RoleHysteresis.ForceAllHoldShape(_hystState);
                SetAllHoldShape(snapshot);
                _lastDirective     = PressDirective.Inactive;
                _lastProcessedTick = snapshot.TickIndex;
                return;
            }

            // If no trigger active, produce HOLD_SHAPE but don't disengage yet.
            if (committed == TriggerFlags.None)
            {
                SetAllHoldShape(snapshot);
                _lastDirective     = PressDirective.Inactive;
                _lastProcessedTick = snapshot.TickIndex;
                return;
            }

            // ── Step 3: Primary press selection (§3.3) ────────────────────────
            Vector2 interceptPt = PrimaryPressSelector.ComputeInterceptionPoint(snapshot);
            int primaryId = PrimaryPressSelector.Select(
                snapshot, interceptPt, out Vector2 primaryTargetPos);

            // ── Step 4: Cover-shadow selection (§3.4–§3.5) ───────────────────
            int shadowCount = CoverShadowSelector.Select(
                snapshot, primaryId,
                out CoverShadow shadow0, out CoverShadow shadow1);

            // ── Step 5: Build candidate directive ─────────────────────────────
            PressDirective directive = new PressDirective
            {
                PrimaryPresserId      = primaryId,
                PrimaryTargetPosition = primaryTargetPos,
                Shadow0               = shadow0,
                Shadow1               = shadow1,
                CoverShadowCount      = shadowCount,
                ActiveTriggers        = committed,
            };

            // ── Step 6: Invariant enforcement (§3.9) ─────────────────────────
            bool valid = InvariantEnforcer.Enforce(snapshot, ref directive);
            if (!valid)
            {
                RoleHysteresis.ForceAllHoldShape(_hystState);
                SetAllHoldShape(snapshot);
                _lastDirective     = PressDirective.Inactive;
                _lastProcessedTick = snapshot.TickIndex;
                return;
            }

            // ── Step 7: Apply role hysteresis (§3.6) + build assignments ─────
            BuildAssignments(snapshot, directive);

            // ── Step 8: Apply stamina costs (§3.7) into the persistent ledger ─
            StaminaAccumulator.ApplyAll(_assignments, _assignmentCount, _pressFatigue, _entityIdCapacity);

            _lastDirective     = directive;
            _lastProcessedTick = snapshot.TickIndex;
        }

        // ── Accessors ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the committed <see cref="PressAssignment"/> for the given EntityId.
        /// Returns a HoldShape assignment when the EntityId is unknown.
        /// </summary>
        /// <param name="entityId">Agent entity identifier.</param>
        public PressAssignment GetAssignment(int entityId)
        {
            if (entityId >= 0 && entityId < _entityIdCapacity)
            {
                int idx = _entityToAssignmentIdx[entityId];
                if (idx >= 0)
                    return _assignments[idx];
            }

            return new PressAssignment { EntityId = entityId, Role = PressRole.HoldShape };
        }

        /// <summary>
        /// Snapshot seam: bundles this tick's cross-tick state (role hysteresis, trigger debounce
        /// counters, disengage/cooldown dwell, accumulated press fatigue) into a <see cref="PressingTickState"/>
        /// view so a host snapshot layer can serialize it canonically for deterministic save/restore
        /// (parallel to the Positioning <see cref="PositioningAI.HysteresisState"/> seam). The bundled
        /// containers are the live, allocated-once instances (read-only serialization use only).
        /// </summary>
        public PressingTickState CaptureState() =>
            new PressingTickState(_hystState, in _triggerState, _disengageDwell, _cooldownTicks, _pressFatigue);

        // ── Private helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Sets all own-team agents to HoldShape in the assignments buffer.
        /// </summary>
        private void SetAllHoldShape(PressingSnapshot snapshot)
        {
            _assignmentCount = 0;
            for (int i = 0; i < _entityIdCapacity; i++)
                _entityToAssignmentIdx[i] = -1;

            int pressingTeamId = snapshot.PressingTeamId;
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.TeamId != pressingTeamId)
                    continue;
                if (!a.IsActive)
                    continue;

                int idx = _assignmentCount++;
                _assignments[idx] = new PressAssignment
                {
                    EntityId       = a.EntityId,
                    Role           = PressRole.HoldShape,
                    TargetPosition = Vector2.zero,
                };

                if (a.EntityId < _entityIdCapacity)
                    _entityToAssignmentIdx[a.EntityId] = idx;
            }
        }

        /// <summary>
        /// Builds per-agent assignments by applying role hysteresis to the directive roles.
        /// </summary>
        private void BuildAssignments(PressingSnapshot snapshot, PressDirective directive)
        {
            _assignmentCount = 0;
            for (int i = 0; i < _entityIdCapacity; i++)
                _entityToAssignmentIdx[i] = -1;

            int pressingTeamId = snapshot.PressingTeamId;

            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.TeamId != pressingTeamId)
                    continue;
                if (!a.IsActive)
                    continue;

                // Determine candidate role for this agent from the directive.
                PressRole candidateRole  = PressRole.HoldShape;
                Vector2   candidateTarget = Vector2.zero;

                if (a.EntityId == directive.PrimaryPresserId)
                {
                    candidateRole   = PressRole.PrimaryPress;
                    candidateTarget = directive.PrimaryTargetPosition;
                }
                else if (directive.CoverShadowCount >= 1
                      && a.EntityId == directive.Shadow0.DefenderId)
                {
                    candidateRole   = PressRole.CoverShadow;
                    candidateTarget = directive.Shadow0.TargetPosition;
                }
                else if (directive.CoverShadowCount >= 2
                      && a.EntityId == directive.Shadow1.DefenderId)
                {
                    candidateRole   = PressRole.CoverShadow;
                    candidateTarget = directive.Shadow1.TargetPosition;
                }

                // Apply hysteresis, keyed by EntityId (AR-2 M-2) so dwell state follows the
                // agent across snapshot re-ordering. EntityIds outside the state capacity
                // bypass hysteresis (commit the candidate directly) rather than aliasing.
                PressRole committed =
                    (a.EntityId >= 0 && a.EntityId < _hystState.Capacity)
                        ? RoleHysteresis.Commit(a.EntityId, candidateRole, _hystState)
                        : candidateRole;

                // If hysteresis holds a press role but directive no longer has a target,
                // fall back to HoldShape target (position comes from PositioningAI).
                Vector2 committedTarget = committed == PressRole.HoldShape
                    ? Vector2.zero
                    : candidateTarget;

                int idx = _assignmentCount++;
                _assignments[idx] = new PressAssignment
                {
                    EntityId       = a.EntityId,
                    Role           = committed,
                    TargetPosition = committedTarget,
                };

                if (a.EntityId < _entityIdCapacity)
                    _entityToAssignmentIdx[a.EntityId] = idx;
            }
        }

        /// <summary>
        /// Adds the persisted press-fatigue ledger (AR-2 M-1) onto the snapshot's own-team
        /// agent Fatigue fields, clamped to [0, 1], so eligibility checks this tick see the
        /// accumulated total. Called once per tick before any fatigue is read.
        /// </summary>
        private void FoldPressFatigue(PressingSnapshot snapshot)
        {
            int pressingTeamId = snapshot.PressingTeamId;
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.TeamId != pressingTeamId)
                    continue;
                if (a.EntityId < 0 || a.EntityId >= _entityIdCapacity)
                    continue;

                float total = a.Fatigue + _pressFatigue[a.EntityId];
                a.Fatigue = total > 1f ? 1f : total;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-05-29 | —      | AR-1 M-1: added using TacticalDirector.PositioningAI; simplified Phase references. AR-1 H-1: added IsActive guards in SetAllHoldShape and BuildAssignments. |
// | 1.2     | 2026-06-15 | —      | AR-2 M-1: persistent per-EntityId press-fatigue ledger folded onto snapshot each tick; Step 8 accumulates into it instead of the throwaway snapshot. AR-2 M-2: hysteresis state sized to EntityId space and keyed by EntityId (not snapshot array index). |
// | 1.3     | 2026-06-27 | —      | Match Engine Phase D D4 follow-up: CaptureState() snapshot seam bundles the cross-tick state (role hysteresis, trigger debounce, disengage/cooldown dwell, press-fatigue ledger) into a PressingTickState view for the host snapshot layer. Read-only serialization use; no behaviour change. |
#endregion
