// File:     src/attacking-ai/AttackingAITick.cs
// Created:  2026-05-29
// Modified: 2026-06-27 (Match Engine Phase D D4: CaptureState snapshot seam)
// Author:   —
// Spec:     Attacking AI #15 §3.13, §4.1–§4.3, FR-AT-001–FR-AT-027, Code Standards #20
// Purpose:  10 Hz attacking AI orchestrator for one team. Runs the §3.13 pipeline:
//           phase gate → pool build → role assignment → width enforcement → weak-side →
//           overload detection → invariant enforcement → publish directive + intents.
//           Pre-allocated scratch buffers ensure zero heap allocation on the hot path.

using UnityEngine;
using Unity.Profiling;

using TacticalDirector.PositioningAI;
using TacticalDirector.PressingAI;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// 10 Hz Attacking AI orchestrator for one team.
    ///
    /// Lifecycle: construct once per team per match → call Tick() every 10 Hz →
    /// read results via <see cref="LastDirective"/>, <see cref="GetIntent"/>,
    /// and <see cref="GetSnapshot"/>.
    ///
    /// Pre-allocated scratch buffers ensure zero heap allocation on the hot path
    /// after construction. Attacking AI #15 §3.13 / §4.1.
    ///
    /// Phase gate: when PositioningAI reports OutOfPoss, Tick() emits an empty directive
    /// immediately. TRANSITION phase emits the frozen last-IN_POSSESSION directive for
    /// TRANSITION_HOLD_TICKS ticks, then an empty directive.
    /// </summary>
    public sealed class AttackingAITick
    {
        // ── Profiler marker ───────────────────────────────────────────────────
        private static readonly ProfilerMarker s_TickMarker =
            new ProfilerMarker("AttackingAI.Tick");

        // ── Injected dependencies ─────────────────────────────────────────────
        private readonly PositioningAIView _posAI;
        private readonly StyleProfile      _styleProfile;

        // ── Persistent state ──────────────────────────────────────────────────
        private readonly AttackHysteresisState[] _hysteresis;  // indexed by EntityId
        private TransitionHoldState              _transitionState;
        private int                              _lastProcessedTick = -1;

        // ── Pre-allocated scratch buffers (hot path zero-alloc) ───────────────
        private readonly AttackPoolEntry[] _poolBuffer;    // scratch pool during pipeline
        private readonly AttackIntent[]    _intentBuffer;  // output intent array (reused)
        private int                        _intentCount;

        // ── Fast EntityId → intent-index map ─────────────────────────────────
        private readonly int[] _entityToIntentIdx;  // entityId → idx in _intentBuffer; −1 = absent
        private readonly int   _entityIdCapacity;

        // ── Last computed output ──────────────────────────────────────────────
        private AttackDirective _lastDirective;
        private AttackDirective _lastInPossDirective; // frozen copy for transition emit

        /// <summary>The directive produced by the most recent Tick() call.</summary>
        public AttackDirective LastDirective => _lastDirective;

        // ── Construction ─────────────────────────────────────────────────────

        /// <summary>Constructs the orchestrator.</summary>
        /// <param name="posAI">Read-only view of the Positioning AI (phase and slot queries).</param>
        /// <param name="styleProfile">Team-style profile selected at match init.</param>
        /// <param name="maxEntityId">Highest possible EntityId; default 64.</param>
        public AttackingAITick(PositioningAIView posAI, StyleProfile styleProfile, int maxEntityId = 64)
        {
            _posAI        = posAI;
            _styleProfile = styleProfile;

            _entityIdCapacity   = maxEntityId + 1;
            _hysteresis         = new AttackHysteresisState[_entityIdCapacity];
            _poolBuffer         = new AttackPoolEntry[AttackingAIConstants.SQUAD_SIZE];
            _intentBuffer       = new AttackIntent[AttackingAIConstants.SQUAD_SIZE];
            _entityToIntentIdx  = new int[_entityIdCapacity];

            for (int i = 0; i < _entityIdCapacity; i++)
                _entityToIntentIdx[i] = -1;

            _lastDirective      = AttackDirective.Empty;
            _lastInPossDirective = AttackDirective.Empty;
            _transitionState    = default;
        }

        // ── Per-tick entry point ──────────────────────────────────────────────

        /// <summary>
        /// Executes one 10 Hz attacking AI tick for the team identified by
        /// <see cref="AttackingSnapshot.AttackingTeamId"/>.
        /// Results are available via <see cref="LastDirective"/> and <see cref="GetIntent"/>.
        /// </summary>
        /// <param name="snapshot">Current tick input; TickIndex must be ≥ 0.</param>
        public void Tick(AttackingSnapshot snapshot)
        {
            using var _ = s_TickMarker.Auto();

            // F1 — stale perception guard (FR-AT-024).
            if (snapshot.TickIndex < _lastProcessedTick)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogWarning(
                    $"[AttackingAI] F1 stale tick {snapshot.TickIndex} < last {_lastProcessedTick}; reusing previous directive.");
#endif
                return;
            }

            // ── Step 1: Read phase (§3.1) ─────────────────────────────────────
            Phase phase    = _posAI.GetPhase();
            Phase prevPhase = _transitionState.PrevPhase;

            // ── Step 2: Phase gate ────────────────────────────────────────────
            // OUT_OF_POSSESSION → emit empty; no algorithm run.
            if (phase == Phase.OutOfPoss)
            {
                SetEmpty(snapshot.TickIndex, phase);
                return;
            }

            // TRANSITION → TransitionController.
            bool isTransition = (phase == Phase.TransToAtk || phase == Phase.TransToDef);

            if (isTransition)
            {
                AttackDirective frozen = TransitionController.Evaluate(
                    prevPhase, phase, _styleProfile, _lastInPossDirective,
                    ref _transitionState);

                _lastDirective             = frozen;
                _transitionState.PrevPhase = phase;
                _lastProcessedTick         = snapshot.TickIndex;
                return;
            }

            // ── IN_POSSESSION ─────────────────────────────────────────────────
            _transitionState.TransitionHoldTick = 0; // reset countdown on IN_POSSESSION return

            // Loose-ball guard (FR-AT-008 / §2.3): an IN_POSSESSION phase with no identified
            // carrier (BallCarrierEntityId < 0) has an undefined BallCarrierPosition — run-target
            // origin (§3.4) and support radius (§3.5) would key off stale data. Emit empty; no
            // role assignment is meaningful without a carrier.
            if (snapshot.BallCarrierEntityId < 0)
            {
                SetEmpty(snapshot.TickIndex, phase);
                return;
            }

            // ── Step 3: Build attacking pool (§3.2) ──────────────────────────
            int poolCount = AttackingPoolBuilder.Build(snapshot, _poolBuffer);

            if (poolCount < 0)
            {
                // F2: SENTINEL_NO_SLOT detected (FR-AT-025).
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Debug.LogWarning("[AttackingAI] F2 sentinel slot in pool; emitting empty directive.");
#endif
                SetEmpty(snapshot.TickIndex, phase);
                return;
            }

            if (poolCount == 0)
            {
                SetEmpty(snapshot.TickIndex, phase);
                return;
            }

            // ── Steps 4–5: Role assignment + run parameters (§3.3–§3.5) ──────
            float cosAngle = Mathf.Cos(snapshot.TeamAttackAngle);
            float sinAngle = Mathf.Sin(snapshot.TeamAttackAngle);

            RoleAssigner.Assign(
                snapshot, _poolBuffer, poolCount, _styleProfile,
                _hysteresis, cosAngle, sinAngle, snapshot.TickIndex);

            // ── Step 6: Width-holding enforcement (§3.6) ─────────────────────
            WidthHolder.Enforce(
                snapshot.BallPosition, snapshot.BallCarrierPosition,
                _poolBuffer, poolCount);

            // ── Step 7: Weak-side post-check (§3.7) ──────────────────────────
            WeakSideController.EnsureWeakSide(
                snapshot.BallPosition, snapshot.BallCarrierPosition,
                _poolBuffer, poolCount);

            // ── Step 8: Overload detection (§3.8) ────────────────────────────
            bool overloadActive = OverloadDetector.Evaluate(
                snapshot.BallPosition, _poolBuffer, poolCount, out Flank overloadFlank);

            // ── Step 9: Anti-chaos invariant enforcement (§3.11) ─────────────
            bool invariantOk = InvariantEnforcer.Apply(
                _poolBuffer, poolCount, _styleProfile, snapshot.TeamAttackAngle);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (!invariantOk)
                UnityEngine.Debug.LogWarning("[AttackingAI] F3 invariant fallback applied; emitting all-default directive.");
#endif

            // ── Step 10: Publish AttackDirective + per-agent AttackIntent ─────
            AttackDirective directive = new AttackDirective(
                snapshot.AttackingTeamId, overloadActive, overloadFlank,
                _transitionState.TransitionHoldTick);

            PublishIntents(snapshot.TickIndex, poolCount);

            _lastDirective       = directive;
            _lastInPossDirective = directive; // cache for transition freeze
            _transitionState.PrevPhase = phase;
            _lastProcessedTick   = snapshot.TickIndex;
        }

        // ── Accessors ─────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the <see cref="AttackIntent"/> for the given EntityId.
        /// Returns a HoldWidth intent with no run parameters when the EntityId is absent.
        /// </summary>
        public AttackIntent GetIntent(int entityId)
        {
            if (entityId >= 0 && entityId < _entityIdCapacity)
            {
                int idx = _entityToIntentIdx[entityId];
                if (idx >= 0) return _intentBuffer[idx];
            }
            return new AttackIntent(entityId, AttackRole.HoldWidth, null, _lastProcessedTick);
        }

        /// <summary>
        /// Returns an <see cref="AttackIntentSnapshot"/> read-only view over the last tick's output.
        /// </summary>
        public AttackIntentSnapshot GetSnapshot()
        {
            return new AttackIntentSnapshot(_lastDirective, _intentBuffer, _intentCount, _lastProcessedTick);
        }

        /// <summary>
        /// Snapshot seam: bundles this tick's cross-tick state (per-agent role hysteresis, per-team
        /// transition-hold state, frozen in-possession directive) into an <see cref="AttackingTickState"/>
        /// view so a host snapshot layer can serialize it canonically for deterministic save/restore
        /// (parallel to the Positioning / Pressing / Defensive seams). The bundled array is the live,
        /// allocated-once instance (read-only serialization use only).
        /// </summary>
        public AttackingTickState CaptureState() =>
            new AttackingTickState(_hysteresis, in _transitionState, in _lastInPossDirective);

        // ── Private helpers ───────────────────────────────────────────────────

        private void SetEmpty(int tickIndex, Phase phase)
        {
            _intentCount = 0;
            for (int i = 0; i < _entityIdCapacity; i++)
                _entityToIntentIdx[i] = -1;
            _lastDirective             = AttackDirective.Empty;
            _transitionState.PrevPhase = phase;
            _lastProcessedTick         = tickIndex;
        }

        private void PublishIntents(int currentTick, int poolCount)
        {
            _intentCount = 0;
            for (int i = 0; i < _entityIdCapacity; i++)
                _entityToIntentIdx[i] = -1;

            for (int i = 0; i < poolCount; i++)
            {
                ref AttackPoolEntry entry = ref _poolBuffer[i];

                RunParameters? runParams = null;
                if (entry.AssignedRole == AttackRole.Runner && entry.HasRunParams)
                {
                    runParams = new RunParameters(
                        entry.DepthOffsetM, entry.LateralOffsetM, entry.RunTriggerTick);
                }

                int idx = _intentCount++;
                _intentBuffer[idx] = new AttackIntent(
                    entry.EntityId, entry.AssignedRole, runParams, currentTick);

                if (entry.EntityId < _entityIdCapacity)
                    _entityToIntentIdx[entry.EntityId] = idx;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-05-29 | —      | AR-1 M-4: removed dead firstLossThisTick variable and || clause (OutOfPoss already caught above; remaining non-InPoss phases are TransToAtk/TransToDef, covered by isTransition). |
// | 1.2     | 2026-06-15 | —      | AR-5 M-2 (ERR-015-011): added the FR-AT-008 loose-ball guard — an IN_POSSESSION phase with BallCarrierEntityId < 0 now emits an empty directive instead of running the pipeline against an undefined BallCarrierPosition. |
// | 1.3     | 2026-06-27 | —      | Match Engine Phase D D4 follow-up: CaptureState() snapshot seam bundles the cross-tick state (per-agent role hysteresis, transition-hold state, frozen in-possession directive) into an AttackingTickState view for the host snapshot layer. Read-only; no behaviour change. |
#endregion
