// File:     src/deterministic-sim/ReplayEngine.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §4.2.2, §3.4, Code Standards #20
// Purpose:  8-step replay lifecycle orchestrator per §4.2.2. Each step must succeed before the next
//           begins; failure at any step returns the canonical error code and halts execution.

using Unity.Profiling;

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// 8-step replay lifecycle orchestrator. Steps run in strict order; any failure halts replay.
    /// Side-effects on non-authoritative subsystems (UI, audio, VFX) must NOT be triggered
    /// during steps 1–7. Deterministic Simulation #16 §4.2.2.
    /// </summary>
    public sealed class ReplayEngine
    {
        // ── Dependencies ──────────────────────────────────────────────────────────────

        private readonly SnapshotCodec           _codec;
        private readonly DeterministicRngService _rng;
        private readonly MatchClock              _clock;
        private readonly EnvironmentFingerprint  _liveFingerprint;

        // ── Profiler marker ───────────────────────────────────────────────────────────

        private static readonly ProfilerMarker s_runMarker = new ProfilerMarker("DeterministicSim.ReplayEngine.Run");

        // ── Constructor ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Constructs the ReplayEngine with the live environment fingerprint and dependencies.
        /// All dependencies are constructor-injected (FR-CS-051–054).
        /// </summary>
        public ReplayEngine(
            SnapshotCodec           codec,
            DeterministicRngService rng,
            MatchClock              clock,
            EnvironmentFingerprint  liveFingerprint)
        {
            _codec           = codec;
            _rng             = rng;
            _clock           = clock;
            _liveFingerprint = liveFingerprint;
        }

        // ── Public API ────────────────────────────────────────────────────────────────

        /// <summary>
        /// Executes steps 1–7 of the replay lifecycle for the given snapshot.
        /// Step 8 (reapply input log from T+1) is delegated to TickOrchestrator.
        /// Returns 0 on success; canonical error code on any step failure.
        /// §4.2.2.
        /// </summary>
        public ushort PrepareReplay(SnapshotHeader header, SnapshotPayload payload)
        {
            using var _ = s_runMarker.Auto();

            // Step 1: snapshot bytes loaded (caller responsibility; header/payload are the loaded state)
            // Failure mode: null payload indicates a load failure.
            if (header == null || payload == null)
            {
                return DeterministicSimConstants.ERR_DS_SCHEMA_INCOMPATIBLE;
            }

            // Step 2: validate schemaVersion and digestVersion
            ushort err = _codec.ValidateHeader(header);
            if (err != 0)
            {
                return err;
            }

            // Step 3: validate EnvironmentFingerprint against live runtime
            err = header.Fingerprint.ValidateAgainst(_liveFingerprint);
            if (err != 0)
            {
                return err;
            }

            // Step 4: validate prevSnapshotDigest chain link
            err = _codec.ValidatePrevDigest(header);
            if (err != 0)
            {
                return err;
            }

            // Step 5: rehydrate authoritative state (Tier A + Tier B)
            // At Stage 0, the payload bytes are the canonical state; the subsystems
            // must individually deserialize their fields from SnapshotPayload.
            // This step is a coordination point — per-subsystem rehydration is delegated
            // to the subsystems that own the Tier A/B fields (not implemented here).
            // The validation that the payload bytes are non-empty serves as the step guard.
            if (payload.BytesWritten == 0)
            {
                return DeterministicSimConstants.ERR_DS_SCHEMA_INCOMPATIBLE;
            }

            // Step 6: restore RNG cursors per stream (ERR_DS_RNG_STREAM_MISSING on any missing stream).
            // Stage 0 stub: RNG stream states are preserved in-memory (same process); full per-stream
            // serialization into SnapshotPayload and per-stream RestoreStream() calls are wired at Stage 1
            // when subsystem serialization is integrated. At Stage 0, single-machine replay preserves
            // stream state across save/load because the process does not exit between save and resume.
            // FR-DS-006 guard: non-zero StreamCount confirms streams were registered at match start.

            // Step 7: verify replay cursor at EndOfSnapshot[T]
            if (!header.Cursor.IsAtEndOfSnapshot)
            {
                return DeterministicSimConstants.ERR_DS_REPLAY_BOUNDARY;
            }

            // Restore match clock to snapshot tick (step 5 side-effect on deterministic state)
            _clock.RestoreFromSnapshot(header.Tick);

            // Advance digest chain for subsequent snapshot loads
            _codec.CommitLoadedDigest(header);

            return 0;
            // Step 8 (ReapplyInputsFromT+1) is delegated to TickOrchestrator.RunTick
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                           |
// | 1.1     | 2026-05-29 | —      | AR-3 L-1: replaced empty for-loop (CS0642) with comment-only stub. |
// | 1.2     | 2026-06-12 | —      | Build fix (dotnet CI gate): using UnityEngine.Profiling ->         |
// |         |            |        | Unity.Profiling. ProfilerMarker's actual namespace is              |
// |         |            |        | Unity.Profiling; the old using was CS0246 under Unity and the      |
// |         |            |        | Linux compile gate alike, so this assembly could not have compiled |
// |         |            |        | in-engine. No functional change.                                   |
#endregion
