// File:     src/deterministic-sim/TickOrchestrator.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §3.1.2, §3.6.1, §3.4, FR-DS-001/002, Code Standards #20
// Purpose:  7-phase 60 Hz tick pipeline orchestrator. Enforces canonical phase order
//           (Input→Intent→AI/AI_NoOp→Physics→Resolve→Events→Snapshot) and gates the AI phase
//           on the AI_PHASE_STRIDE. Zero heap allocation on hot path.

using UnityEngine.Profiling;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// 7-phase 60 Hz tick pipeline orchestrator.
    /// Enforces canonical phase order per FR-DS-001; gates AI on AI_PHASE_STRIDE per §3.1.2.
    /// Phase callbacks are delegate-free (no closures) — callers register action delegates once
    /// at construction; the hot path calls only stored references.
    /// Constructor-injected (FR-CS-051–054). Deterministic Simulation #16 §3.1.2 / §3.6.1.
    /// </summary>
    public sealed class TickOrchestrator
    {
        // ── Dependencies ──────────────────────────────────────────────────────────────

        private readonly MatchClock     _clock;
        private readonly SnapshotCodec  _codec;

        // ── Phase callbacks (registered at construction; called once per phase per tick) ─
        // Using System.Action references avoids per-frame allocations from lambdas.

        private readonly System.Action _runInput;
        private readonly System.Action _runIntent;
        private readonly System.Action _runAI;
        private readonly System.Action _runPhysics;
        private readonly System.Action _runResolve;
        private readonly System.Action _runEvents;
        private readonly System.Action<SnapshotPayload> _runSnapshot;

        // ── Snapshot state ────────────────────────────────────────────────────────────

        private readonly SnapshotHeader  _snapshotHeader;
        private readonly SnapshotPayload _snapshotPayload;

        // ── Profiler markers ──────────────────────────────────────────────────────────

        private static readonly ProfilerMarker s_runTickMarker  = new ProfilerMarker("DeterministicSim.RunTick");
        private static readonly ProfilerMarker s_inputMarker    = new ProfilerMarker("DeterministicSim.Phase.Input");
        private static readonly ProfilerMarker s_intentMarker   = new ProfilerMarker("DeterministicSim.Phase.Intent");
        private static readonly ProfilerMarker s_aiMarker       = new ProfilerMarker("DeterministicSim.Phase.AI");
        private static readonly ProfilerMarker s_aiNoOpMarker   = new ProfilerMarker("DeterministicSim.Phase.AI.NoOp");
        private static readonly ProfilerMarker s_physicsMarker  = new ProfilerMarker("DeterministicSim.Phase.Physics");
        private static readonly ProfilerMarker s_resolveMarker  = new ProfilerMarker("DeterministicSim.Phase.Resolve");
        private static readonly ProfilerMarker s_eventsMarker   = new ProfilerMarker("DeterministicSim.Phase.Events");
        private static readonly ProfilerMarker s_snapshotMarker = new ProfilerMarker("DeterministicSim.Phase.Snapshot");

        // ── Constructor ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Constructs the orchestrator with constructor-injected dependencies and phase callbacks.
        /// All allocations occur here; RunTick is zero-allocation on the hot path.
        /// §3.1.2 / §3.6.1.
        /// </summary>
        public TickOrchestrator(
            MatchClock              clock,
            SnapshotCodec           codec,
            EnvironmentFingerprint  fingerprint,
            System.Action           runInput,
            System.Action           runIntent,
            System.Action           runAI,
            System.Action           runPhysics,
            System.Action           runResolve,
            System.Action           runEvents,
            System.Action<SnapshotPayload> runSnapshot)
        {
            _clock           = clock;
            _codec           = codec;
            _runInput        = runInput;
            _runIntent       = runIntent;
            _runAI           = runAI;
            _runPhysics      = runPhysics;
            _runResolve      = runResolve;
            _runEvents       = runEvents;
            _runSnapshot     = runSnapshot;

            _snapshotHeader  = new SnapshotHeader();
            _snapshotPayload = new SnapshotPayload();

            _snapshotHeader.Initialize(0UL, null, fingerprint);
        }

        // ── Public API ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Executes one 60 Hz tick in canonical phase order (FR-DS-001).
        /// Advances the MatchClock, then runs Input→Intent→AI/AI_NoOp→Physics→Resolve→Events→Snapshot.
        /// Zero heap allocation. §3.1.2.
        /// </summary>
        public void RunTick()
        {
            using var outerScope = s_runTickMarker.Auto();

            _clock.Advance();
            ulong tick = _clock.CurrentTick;

            // Reset snapshot payload for this tick
            _snapshotPayload.Reset();

            // ── Phase: Input ──────────────────────────────────────────────────────────
            using (s_inputMarker.Auto())
            {
                _runInput();
            }

            // ── Phase: Intent ─────────────────────────────────────────────────────────
            using (s_intentMarker.Auto())
            {
                _runIntent();
            }

            // ── Phase: AI (stride-gated) or AI_NoOp ──────────────────────────────────
            if (_clock.IsAiStrideTick)
            {
                using (s_aiMarker.Auto())
                {
                    _runAI();
                }
            }
            else
            {
                // AI_NoOp: emit empty phase digest so the digest stream remains invariant. §3.1.2
                using (s_aiNoOpMarker.Auto())
                {
                    // No subsystem writes. Phase ordinal still advances in the digest chain.
                }
            }

            // ── Phase: Physics ────────────────────────────────────────────────────────
            using (s_physicsMarker.Auto())
            {
                _runPhysics();
            }

            // ── Phase: Resolve ────────────────────────────────────────────────────────
            using (s_resolveMarker.Auto())
            {
                _runResolve();
            }

            // ── Phase: Events ─────────────────────────────────────────────────────────
            using (s_eventsMarker.Auto())
            {
                _runEvents();
            }

            // ── Phase: Snapshot ───────────────────────────────────────────────────────
            using (s_snapshotMarker.Auto())
            {
                _snapshotHeader.Initialize(tick, _snapshotHeader.CurrentSnapshotDigest, _snapshotHeader.Fingerprint);
                _runSnapshot(_snapshotPayload);
                _codec.Encode(_snapshotHeader, _snapshotPayload);
            }
        }

        /// <summary>Returns the current snapshot header (after the last RunTick call).</summary>
        public SnapshotHeader CurrentHeader => _snapshotHeader;

        /// <summary>Returns the current snapshot payload (after the last RunTick call).</summary>
        public SnapshotPayload CurrentPayload => _snapshotPayload;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
