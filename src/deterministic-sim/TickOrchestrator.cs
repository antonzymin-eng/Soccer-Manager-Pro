// File:     src/deterministic-sim/TickOrchestrator.cs
// Created:  2026-05-29
// Modified: 2026-08-22 (ERR-016-009: the constructor takes the §2.3.2 buildHash and stamps it into
//           every header it writes)
// Author:   —
// Spec:     Deterministic Simulation #16 §3.1.2, §3.6.1, §3.4, FR-DS-001/002, Code Standards #20
// Purpose:  7-phase 60 Hz tick pipeline orchestrator. Enforces canonical phase order
//           (Input→Intent→AI→Physics→Resolve→Events→Snapshot; AI runs as a no-op on non-stride
//           ticks, same ordinal) and gates the AI phase on the AI_PHASE_STRIDE. Zero heap
//           allocation on hot path.

using Unity.Profiling;

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
        /// <para>
        /// <paramref name="buildHash"/> is the #16 §2.3.2 build identity, captured at match start
        /// alongside <paramref name="fingerprint"/> and stamped into every header this orchestrator
        /// writes (FR-DS-014). It is supplied by the composition root rather than derived here: this
        /// assembly is a cross-cutting foundation and cannot name the closure it would have to hash.
        /// </para>
        /// </summary>
        public TickOrchestrator(
            MatchClock              clock,
            SnapshotCodec           codec,
            EnvironmentFingerprint  fingerprint,
            string                  buildHash,
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

            _snapshotHeader.Initialize(0UL, null, fingerprint, buildHash);
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

            // ── Phase: AI (stride-gated; runs as a no-op on non-stride ticks) ────────
            if (_clock.IsAiStrideTick)
            {
                using (s_aiMarker.Auto())
                {
                    _runAI();
                }
            }
            else
            {
                // Non-stride tick: the AI phase runs but writes nothing. It occupies the same
                // PhaseId.AI ordinal (2) as a stride tick (§3.1.2). The snapshot digest is
                // computed once per tick over the payload (no per-phase digest is emitted here),
                // so "no writes" simply means the payload carries no AI-phase contribution.
                using (s_aiNoOpMarker.Auto())
                {
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
                // The codec is the digest-chain authority: Encode threads the previous digest into
                // PrevSnapshotDigest and computes CurrentSnapshotDigest over the §3.2.3 preimage.
                // Pass prevDigest: null here (Encode overwrites it) rather than the stale field.
                _snapshotHeader.Initialize(
                    tick, prevDigest: null, _snapshotHeader.Fingerprint, _snapshotHeader.BuildHash);
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
// | 1.1     | 2026-06-12 | —      | Build fix (dotnet CI    |
// |         |            |        | gate): using            |
// |         |            |        | UnityEngine.Profiling   |
// |         |            |        | -> Unity.Profiling.     |
// |         |            |        | ProfilerMarker's actual |
// |         |            |        | namespace is            |
// |         |            |        | Unity.Profiling; the    |
// |         |            |        | old using was CS0246    |
// |         |            |        | under Unity and the     |
// |         |            |        | Linux compile gate      |
// |         |            |        | alike, so this assembly |
// |         |            |        | could not have compiled |
// |         |            |        | in-engine. No           |
// |         |            |        | functional change.      |
// | 1.2     | 2026-06-15 | —      | AR fix L-3: Snapshot phase passes prevDigest:null to            |
// |         |            |        | Initialize (the codec is the chain authority and fills          |
// |         |            |        | PrevSnapshotDigest in Encode); removes dead self-referential    |
// |         |            |        | plumbing. AR fix L-4: AI-no-op comment no longer claims a       |
// |         |            |        | per-phase digest emission that does not exist.                  |
// | 1.3     | 2026-08-22 | —      | ERR-016-009: new required `buildHash` constructor parameter,    |
// |         |            |        | captured beside the fingerprint and threaded into every         |
// |         |            |        | SnapshotHeader.Initialize call (FR-DS-014). Supplied by the     |
// |         |            |        | composition root — this assembly cannot name the assembly       |
// |         |            |        | closure it would otherwise have to hash.                        |
#endregion
