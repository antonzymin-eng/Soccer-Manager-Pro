// File:     src/deterministic-sim/EnvironmentFingerprint.cs
// Created:  2026-05-29
// Modified: 2026-06-15 (AR fix M-1/M-2: ComputeDigest() added; mutation-guard doc corrected)
// Author:   —
// Spec:     Deterministic Simulation #16 §4.8, §4.8.1–§4.8.3, §3.4, Code Standards #20
// Purpose:  Records the runtime environment at match start and embeds it in every snapshot header.
//           Fields are readonly, so the §4.8.1 no-mutation invariant is enforced structurally.
//           Replay-side mismatch triggers ERR_DS_REPLAY_ENV_MISMATCH.

using System.Security.Cryptography;

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

        // ── Lifecycle / derived state ─────────────────────────────────────────────────

        private bool _locked;
        private byte[] _cachedDigest;

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
        /// Marks the §4.8.1 capture lifecycle as sealed. Field immutability itself is enforced
        /// structurally by the readonly fields — there is no mutation path to guard — so
        /// ERR_DS_ENV_MUTATION is reserved for a Stage 1 mutable-capture builder, should one be
        /// introduced. Called immediately after match-start capture. §4.8.1.
        /// </summary>
        public void Lock()
        {
            _locked = true;
        }

        /// <summary>
        /// Returns the 32-byte SHA-256 digest of this fingerprint's canonical preimage
        /// (DOMAIN_TAG_ENV_FP ‖ workerCount(u32) ‖ length-prefixed §4.8 strings). Cached after
        /// the first call (the fields are immutable, so the digest is deterministic). Consumed by
        /// SnapshotCodec.Encode as the envFp slot of the §3.2.3 header preimage. §4.8 / §3.2.3.
        /// </summary>
        public byte[] ComputeDigest()
        {
            if (_cachedDigest != null)
            {
                return _cachedDigest;
            }

            int size = DeterministicSimConstants.FIELD_WIDTH_DOMAIN_TAG + 4
                     + 4 + SchedulerPolicy.Length
                     + 4 + ReductionTopology.Length
                     + 4 + SimdFeatureLevel.Length
                     + 4 + FloatModelHash.Length
                     + 4 + UnicodeNormalizationVersion.Length;

            byte[] preimage = new byte[size];
            int o = 0;
            CanonicalSerializer.WriteU8(preimage, ref o, DeterministicSimConstants.DOMAIN_TAG_ENV_FP);
            CanonicalSerializer.WriteI32(preimage, ref o, WorkerCount);
            CanonicalSerializer.WriteString(preimage, ref o, SchedulerPolicy);
            CanonicalSerializer.WriteString(preimage, ref o, ReductionTopology);
            CanonicalSerializer.WriteString(preimage, ref o, SimdFeatureLevel);
            CanonicalSerializer.WriteString(preimage, ref o, FloatModelHash);
            CanonicalSerializer.WriteString(preimage, ref o, UnicodeNormalizationVersion);

            using (SHA256 sha = SHA256.Create())
            {
                _cachedDigest = sha.ComputeHash(preimage, 0, o);
            }
            return _cachedDigest;
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
            var fp = new EnvironmentFingerprint(
                workerCount:               1,
                schedulerPolicy:           "Stage0-SingleThread-v1",
                reductionTopology:         "Serial",
                simdFeatureLevel:          "SSE2",
                floatModelHash:            "STAGE0_DEV_PLACEHOLDER",
                unicodeNormalizationVersion: DeterministicSimConstants.UNICODE_NFC_VERSION);
            fp.Lock(); // §4.8.1 lifecycle: dev fingerprint is sealed at construction
            return fp;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                        |
// | 1.1     | 2026-06-15 | —      | AR fix M-1: ComputeDigest() (32-byte canonical digest) added   |
// |         |            |        | for the §3.2.3 header preimage. AR fix M-2: Lock() doc no       |
// |         |            |        | longer claims a runtime mutation guard — immutability is        |
// |         |            |        | enforced by the readonly fields; ERR_DS_ENV_MUTATION reserved   |
// |         |            |        | for a Stage 1 mutable builder. CreateStage0Dev() now Lock()s.   |
#endregion
