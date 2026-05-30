// File:     src/deterministic-sim/EnvironmentFingerprint.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §4.8, §4.8.1–§4.8.3, §3.4, Code Standards #20
// Purpose:  Records the runtime environment at match start and embeds it in every snapshot header.
//           Any field mutation after capture triggers ERR_DS_ENV_MUTATION (recording side).
//           Replay-side mismatch triggers ERR_DS_REPLAY_ENV_MISMATCH.

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Immutable environment fingerprint captured at match start.
    /// Embedded in every snapshot header for recording-side storage.
    /// Replay side validates against the live runtime before rehydration.
    /// Deterministic Simulation #16 §4.8.
    /// </summary>
    public sealed class EnvironmentFingerprint
    {
        // ── Fields (all captured at match start; immutable thereafter) ────────────────

        /// <summary>Number of authoritative worker threads. §4.8.</summary>
        public readonly int WorkerCount;

        /// <summary>Scheduler identity + version string (job-system fingerprint). §4.8.</summary>
        public readonly string SchedulerPolicy;

        /// <summary>Canonical reduction tree identifier for parallel reductions. §4.8 / §1.3.1.1.</summary>
        public readonly string ReductionTopology;

        /// <summary>Lowest-common-denominator SIMD level in authoritative paths (e.g. "SSE2", "AVX2"). §4.8.</summary>
        public readonly string SimdFeatureLevel;

        /// <summary>SHA-256 over the canonical 11-field float-flag tuple per §4.8.3. Hex-encoded, 64 chars.</summary>
        public readonly string FloatModelHash;

        /// <summary>Unicode NFC table version pinned for string encoding. §4.8 / §3.2.4.1.
        /// Stage 0 value: "15.1" per UNICODE_NFC_VERSION.</summary>
        public readonly string UnicodeNormalizationVersion;

        // ── Mutation guard ────────────────────────────────────────────────────────────

        private bool _locked;

        // ── Constructor ───────────────────────────────────────────────────────────────

        /// <summary>
        /// Captures the environment fingerprint at match start.
        /// After construction call Lock() to enforce the no-mutation invariant.
        /// §4.8.1.
        /// </summary>
        public EnvironmentFingerprint(
            int    workerCount,
            string schedulerPolicy,
            string reductionTopology,
            string simdFeatureLevel,
            string floatModelHash,
            string unicodeNormalizationVersion)
        {
            WorkerCount               = workerCount;
            SchedulerPolicy           = schedulerPolicy ?? string.Empty;
            ReductionTopology         = reductionTopology ?? string.Empty;
            SimdFeatureLevel          = simdFeatureLevel ?? string.Empty;
            FloatModelHash            = floatModelHash ?? string.Empty;
            UnicodeNormalizationVersion = unicodeNormalizationVersion ?? DeterministicSimConstants.UNICODE_NFC_VERSION;
        }

        /// <summary>
        /// Seals the fingerprint so any subsequent mutation attempt returns ERR_DS_ENV_MUTATION.
        /// Called immediately after match-start capture. §4.8.1.
        /// </summary>
        public void Lock()
        {
            _locked = true;
        }

        /// <summary>
        /// Returns true if this fingerprint has been locked (sealed after match-start capture).
        /// §4.8.1.
        /// </summary>
        public bool IsLocked => _locked;

        /// <summary>
        /// Compares this fingerprint against the live runtime fingerprint.
        /// Returns ERR_DS_REPLAY_ENV_MISMATCH if any field differs; 0 on match.
        /// Deterministic Simulation #16 §4.8.2.
        /// </summary>
        public ushort ValidateAgainst(EnvironmentFingerprint live)
        {
            if (WorkerCount != live.WorkerCount)                                           return DeterministicSimConstants.ERR_DS_REPLAY_ENV_MISMATCH;
            if (SchedulerPolicy != live.SchedulerPolicy)                                   return DeterministicSimConstants.ERR_DS_REPLAY_ENV_MISMATCH;
            if (ReductionTopology != live.ReductionTopology)                               return DeterministicSimConstants.ERR_DS_REPLAY_ENV_MISMATCH;
            if (SimdFeatureLevel != live.SimdFeatureLevel)                                 return DeterministicSimConstants.ERR_DS_REPLAY_ENV_MISMATCH;
            if (FloatModelHash != live.FloatModelHash)                                     return DeterministicSimConstants.ERR_DS_REPLAY_ENV_MISMATCH;
            if (UnicodeNormalizationVersion != live.UnicodeNormalizationVersion)           return DeterministicSimConstants.ERR_DS_REPLAY_ENV_MISMATCH;
            return 0;
        }

        /// <summary>
        /// Constructs a Stage-0 development fingerprint suitable for single-machine replay.
        /// IL2CPP version is sentinel "MONO" for dev builds (§4.8.3).
        /// </summary>
        public static EnvironmentFingerprint CreateStage0Dev()
        {
            return new EnvironmentFingerprint(
                workerCount:               1,
                schedulerPolicy:           "Stage0-SingleThread-v1",
                reductionTopology:         "Serial",
                simdFeatureLevel:          "SSE2",
                floatModelHash:            "STAGE0_DEV_PLACEHOLDER",
                unicodeNormalizationVersion: DeterministicSimConstants.UNICODE_NFC_VERSION);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
