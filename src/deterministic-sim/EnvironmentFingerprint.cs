// File:     src/deterministic-sim/EnvironmentFingerprint.cs
// Created:  2026-05-29
// Modified: 2026-07-19 (ERR-016-006: SSE2->SSE4.2 pin fix; placeholder made assertable via IsDevPlaceholder;
//           then Option-A landing — CreateStage0MonoCertified() builds a REAL §4.8.3 floatModelHash via
//           FloatFlagTuple.ComputeHash())
// Author:   —
// Spec:     Deterministic Simulation #16 §4.8, §4.8.1–§4.8.3, §3.4, Code Standards #20
// Purpose:  Records the runtime environment at match start and embeds it in every snapshot header.
//           Fields are readonly, so the §4.8.1 no-mutation invariant is enforced structurally.
//           Replay-side mismatch triggers ERR_DS_REPLAY_ENV_MISMATCH.

using System;
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
        // ── Sentinels ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// The <see cref="FloatModelHash"/> value stamped by <see cref="CreateStage0Dev"/> in place of a
        /// real §4.8.3 hash. Named (not a bare literal) so it is greppable and so cert-run code can reject
        /// a placeholder fingerprint via <see cref="IsDevPlaceholder"/>. See ERR-016-006.
        /// </summary>
        public const string FloatModelHashDevPlaceholder = "STAGE0_DEV_PLACEHOLDER";

        /// <summary>Pinned Stage-0 SIMD baseline (certification-platform.md). §4.8.3 field 11 / §4.8.</summary>
        public const string Stage0SimdLevel = "SSE4.2";

        /// <summary>[Option A] §4.8.3 field 1 for the pinned Stage-0 Mono backend. See ERR-016-006.</summary>
        public const string Stage0MonoToolchain = "Mono";

        /// <summary>[Option A] §4.8.3 field 3 for the pinned Stage-0 host (Windows x64): the .NET RID. See ERR-016-006.</summary>
        public const string Stage0MonoTargetTriple = "win-x64";

        /// <summary>[Option A] §4.8.3 field 4 sentinel for the Mono backend. Accepted at Stage-0 certification; rejected from Stage 5+. See ERR-016-006.</summary>
        public const string Stage0MonoIl2cppSentinel = "MONO";

        // ── Fields (all captured at match start; immutable thereafter) ────────────────

        /// <summary>Number of authoritative worker threads. §4.8.</summary>
        public readonly int WorkerCount;

        /// <summary>Scheduler identity + version string (job-system fingerprint). §4.8.</summary>
        public readonly string SchedulerPolicy;

        /// <summary>Canonical reduction tree identifier for parallel reductions. §4.8 / §1.3.1.1.</summary>
        public readonly string ReductionTopology;

        /// <summary>Lowest-common-denominator SIMD level in authoritative paths (e.g. "SSE2", "AVX2"). §4.8.</summary>
        public readonly string SimdFeatureLevel;

        /// <summary>
        /// SHA-256 over the canonical 11-field float-flag tuple per §4.8.3. Hex-encoded, 64 chars.
        /// WARNING (ERR-016-006, open): no code computes this hash for a live host — the §4.8.3
        /// <c>SHA-256(SerializeCanonical(0x14 ‖ floatFlagTuple))</c> hasher is unimplemented, and the
        /// spec's tuple (fields 1–4: compilerToolchain / compilerVersion / targetTriple / il2cppVersion)
        /// is written for a natively-compiled IL2CPP build, which contradicts the Stage-0 Mono pin in
        /// <c>docs/tracking/certification-platform.md</c>. Until that spec question is resolved (see
        /// <c>docs/tracking/env-fingerprint-float-model-hash-mono-mapping.md</c>) the only value ever
        /// supplied here is the <see cref="FloatModelHashDevPlaceholder"/> sentinel from
        /// <see cref="CreateStage0Dev"/>. Do NOT treat a fingerprint carrying that sentinel as
        /// certification-grade — check <see cref="IsDevPlaceholder"/> at any cert-run gate.
        /// </summary>
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
        /// True when <see cref="FloatModelHash"/> is the <see cref="FloatModelHashDevPlaceholder"/>
        /// sentinel rather than a real §4.8.3 hash. A certification run MUST reject a fingerprint for
        /// which this is true (the analogue of §4.8.3's "certification MUST reject a MONO fingerprint"
        /// rule, for the not-yet-implemented float-model hasher). See ERR-016-006.
        /// </summary>
        public bool IsDevPlaceholder => FloatModelHash == FloatModelHashDevPlaceholder;

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
        /// Constructs a Stage-0 DEVELOPMENT/PLACEHOLDER fingerprint for single-machine replay only.
        /// This is NOT a certification-grade fingerprint: <see cref="FloatModelHash"/> is the
        /// <see cref="FloatModelHashDevPlaceholder"/> sentinel, not a real §4.8.3 hash (no live-host
        /// hasher exists yet — see ERR-016-006 and
        /// <c>docs/tracking/env-fingerprint-float-model-hash-mono-mapping.md</c>). Every current caller
        /// (MatchEngine boot, the perf-harness / scenario tests) uses this placeholder; a real
        /// certification run MUST build a genuine fingerprint and reject this one via
        /// <see cref="IsDevPlaceholder"/>.
        /// <para>
        /// <c>simdFeatureLevel</c> is <c>"SSE4.2"</c> to match the pinned Stage-0 baseline
        /// (<c>docs/tracking/certification-platform.md</c>) — it was previously "SSE2", which matched no
        /// pin (ERR-016-006). The IL2CPP-vs-Mono tuple fields (§4.8.3 fields 1–4) are deliberately NOT
        /// synthesised here: their meaning under the pinned Mono backend is an unresolved spec question
        /// (the proposal doc above), and inventing values would be exactly the fabrication ERR-016-006
        /// exists to prevent.
        /// </para>
        /// </summary>
        public static EnvironmentFingerprint CreateStage0Dev()
        {
            var fp = new EnvironmentFingerprint(
                workerCount:               1,
                schedulerPolicy:           "Stage0-SingleThread-v1",
                reductionTopology:         "Serial",
                simdFeatureLevel:          Stage0SimdLevel, // ERR-016-006: match the pinned SSE4.2 baseline (was "SSE2")
                floatModelHash:            FloatModelHashDevPlaceholder,
                unicodeNormalizationVersion: DeterministicSimConstants.UNICODE_NFC_VERSION);
            fp.Lock(); // §4.8.1 lifecycle: dev fingerprint is sealed at construction
            return fp;
        }

        /// <summary>
        /// Constructs a Stage-0 Mono-backend fingerprint with a REAL §4.8.3 <see cref="FloatModelHash"/>
        /// (ERR-016-006, Option A — see <c>docs/tracking/env-fingerprint-float-model-hash-mono-mapping.md</c>).
        /// The float-flag tuple uses the Option-A Mono mapping (<see cref="Stage0MonoToolchain"/> /
        /// <see cref="Stage0MonoTargetTriple"/> / <see cref="Stage0MonoIl2cppSentinel"/> /
        /// <see cref="Stage0SimdLevel"/>) plus the §4.8.3 "Required Stage-0 values" for the float-mode flags
        /// (denormals/FTZ off, rounding NearestEven, fp-contract off, FMA off, fast-math off), and the hash
        /// is computed by <see cref="FloatFlagTuple.ComputeHash"/>. Unlike <see cref="CreateStage0Dev"/> the
        /// result is NOT a placeholder (<see cref="IsDevPlaceholder"/> is false).
        /// <para>
        /// <paramref name="monoRuntimeVersion"/> (§4.8.3 field 2) is the one field that varies by host, so it
        /// is supplied by the caller at a real certification run (recorded in the pinned tuple in
        /// <c>certification-platform.md</c>) — this factory does not invent it. NOTE: the §4.8.2 runtime
        /// MXCSR validation (query the live float-mode flags and reject on mismatch) is a separate, still-
        /// deferred concern requiring native interop on the pinned host; the recorded tuple here uses the
        /// §4.8.3 pinned Stage-0 flag values, which is what §4.8.2 validates against.
        /// </para>
        /// </summary>
        public static EnvironmentFingerprint CreateStage0MonoCertified(string monoRuntimeVersion)
        {
            if (string.IsNullOrEmpty(monoRuntimeVersion))
            {
                throw new ArgumentException(
                    "monoRuntimeVersion (§4.8.3 field 2) must be supplied from the pinned certification host.",
                    nameof(monoRuntimeVersion));
            }

            var tuple = new FloatFlagTuple(
                compilerToolchain: Stage0MonoToolchain,
                compilerVersion:   monoRuntimeVersion,
                targetTriple:      Stage0MonoTargetTriple,
                il2cppVersion:     Stage0MonoIl2cppSentinel,
                denormalsAreZero:  false,
                flushToZero:       false,
                roundingMode:      0, // NearestEven
                fpContractMode:    0, // Off
                fmaEnabled:        false,
                fastMath:          false,
                simdLevel:         Stage0SimdLevel);

            var fp = new EnvironmentFingerprint(
                workerCount:               1,
                schedulerPolicy:           "Stage0-SingleThread-v1",
                reductionTopology:         "Serial",
                simdFeatureLevel:          Stage0SimdLevel,
                floatModelHash:            tuple.ComputeHash(),
                unicodeNormalizationVersion: DeterministicSimConstants.UNICODE_NFC_VERSION);
            fp.Lock(); // §4.8.1 lifecycle: sealed at construction
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
// | 1.2     | 2026-07-19 | —      | ERR-016-006: CreateStage0Dev simdFeatureLevel "SSE2" -> "SSE4.2" |
// |         |            |        | to match the pinned baseline; placeholder floatModelHash lifted |
// |         |            |        | to the named FloatModelHashDevPlaceholder sentinel + new        |
// |         |            |        | IsDevPlaceholder property so cert-run code can reject it; docs   |
// |         |            |        | flag the missing live §4.8.3 hasher + the IL2CPP/Mono spec gap. |
// |         |            |        | No live-host hasher added (blocked on the spec decision).       |
// | 1.3     | 2026-07-19 | —      | ERR-016-006 Option A (owner sign-off): §4.8.3/§5.5 reconciled  |
// |         |            |        | to the Stage-0 Mono backend. New CreateStage0MonoCertified()   |
// |         |            |        | builds a REAL floatModelHash via FloatFlagTuple.ComputeHash()  |
// |         |            |        | over the Option-A Mono tuple + the §4.8.3 pinned Stage-0 flag  |
// |         |            |        | values (Stage0Mono* / Stage0SimdLevel consts). monoRuntime-    |
// |         |            |        | Version is host-supplied; §4.8.2 runtime MXCSR validation +    |
// |         |            |        | the certified capture stay host-blocked.                       |
#endregion
