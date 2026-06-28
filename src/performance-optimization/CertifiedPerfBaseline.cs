// File:     src/performance-optimization/CertifiedPerfBaseline.cs
// Created:  2026-06-28
// Modified: 2026-06-28
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.3.2 / §3.4.4 / §4.3.2 / FR-PO-031 / FR-PO-052,
//           Deterministic Simulation #16 §4.8 (EnvironmentFingerprint), certification-platform.md
//           (Stage 0 host pin), Code Standards #20
// Purpose:  A perf baseline corpus entry tagged with its certification state. Models the
//           authoritative FR-PO-031 reference for one scenario+loop on the pinned certification
//           platform. At Stage 0 the entry is Pending (no run on the pinned tuple has executed —
//           the Linux compile/test gate is NON-certifying), so it deliberately carries NO measured
//           metric and refuses to build a BaselineRecord. Once a certification run on the pinned
//           platform supplies metrics, the entry is Certified and projects to a corpus
//           BaselineRecord ready for RegressionGate / PerfGateRunner.

using System;
using System.Collections.Generic;

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// A certification-tagged perf baseline corpus entry for one scenario + loop.
    /// The authoritative FR-PO-031 reference is captured on the pinned certification platform
    /// (certification-platform.md). A <see cref="CertificationStatus.Pending"/> entry records only
    /// the intent and carries no measured metric — the Linux gate is explicitly NON-certifying, so
    /// admitting a number from it would be a fabricated certification. A
    /// <see cref="CertificationStatus.Certified"/> entry carries a complete
    /// <see cref="SessionManifest"/> and finite, positive metrics and projects to a corpus
    /// <see cref="BaselineRecord"/> via <see cref="TryBuildBaselineRecord"/>.
    /// Performance Optimization Strategy #18 §3.4.4 / §4.3.2.
    /// </summary>
    public sealed class CertifiedPerfBaseline
    {
        /// <summary>
        /// [FIXED] Stage 0 certification-platform pin token. Encodes the certification-platform.md
        /// v1.2 tuple: Windows 11 / Unity 2022.3.62f1 / Mono / x64 / SSE4.2 / 1 worker /
        /// deterministic compiler flags. certification-platform.md is the source of truth; this token
        /// is the machine-readable form stamped into a certified <see cref="SessionManifest.PlatformPin"/>.
        /// </summary>
        public const string Stage0CertPlatformPin = "win11-unity2022.3.62f1-mono-x64-sse4.2-1w-detflags";

        /// <summary>
        /// [FIXED] Platform pin for the NON-certifying Linux compile/test gate (tools/dotnet-ci).
        /// Numbers stamped with this pin are advisory wiring proofs only and are never authoritative
        /// per certification-platform.md "Relationship to the Linux compile/test gate".
        /// </summary>
        public const string LinuxNonCertPlatformPin = "linux-dotnet-noncert";

        /// <summary>Certification state of this corpus entry.</summary>
        public CertificationStatus Status { get; }

        /// <summary>Scenario manifest ID this baseline certifies (matches the #19 scenario path).</summary>
        public string ScenarioManifestId { get; }

        /// <summary>Which simulation loop the metrics were/will be captured against (KD-8).</summary>
        public LoopTag Loop { get; }

        /// <summary>Platform pin this baseline is/will be certified on (the pinned tuple token).</summary>
        public string PlatformPin { get; }

        /// <summary>The FR-PO-### or per-spec §6 threshold this baseline is cited against.</summary>
        public string ThresholdCited { get; }

        /// <summary>
        /// Complete session manifest from the certification run, or <c>null</c> while
        /// <see cref="Status"/> is <see cref="CertificationStatus.Pending"/>.
        /// </summary>
        public SessionManifest Manifest { get; }

        /// <summary>
        /// Certified median per-tick budget consumption (ms), or <see cref="float.NaN"/> while Pending.
        /// </summary>
        public float CertifiedP50Ms { get; }

        /// <summary>
        /// Certified 99th-percentile per-tick budget consumption (ms), or
        /// <see cref="float.NaN"/> while Pending.
        /// </summary>
        public float CertifiedP99Ms { get; }

        private CertifiedPerfBaseline(
            CertificationStatus status,
            string scenarioManifestId,
            LoopTag loop,
            string platformPin,
            string thresholdCited,
            SessionManifest manifest,
            float certifiedP50Ms,
            float certifiedP99Ms)
        {
            Status             = status;
            ScenarioManifestId = scenarioManifestId;
            Loop               = loop;
            PlatformPin        = platformPin;
            ThresholdCited     = thresholdCited;
            Manifest           = manifest;
            CertifiedP50Ms     = certifiedP50Ms;
            CertifiedP99Ms     = certifiedP99Ms;
        }

        /// <summary>
        /// Creates a Pending corpus entry — the intent to certify a scenario+loop on
        /// <paramref name="platformPin"/>, before any run on that platform has produced metrics.
        /// Carries no measured metric (both percentiles are <see cref="float.NaN"/>) and cannot
        /// build a <see cref="BaselineRecord"/> until certified.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// Thrown when any of the string arguments is null or empty.
        /// </exception>
        public static CertifiedPerfBaseline Pending(
            string scenarioManifestId,
            LoopTag loop,
            string platformPin,
            string thresholdCited)
        {
            RequireNonEmpty(scenarioManifestId, nameof(scenarioManifestId));
            RequireNonEmpty(platformPin, nameof(platformPin));
            RequireNonEmpty(thresholdCited, nameof(thresholdCited));

            return new CertifiedPerfBaseline(
                CertificationStatus.Pending,
                scenarioManifestId,
                loop,
                platformPin,
                thresholdCited,
                manifest: null,
                certifiedP50Ms: float.NaN,
                certifiedP99Ms: float.NaN);
        }

        /// <summary>
        /// Creates a Certified corpus entry from a completed certification run. The scenario ID,
        /// platform pin, and seed are taken from <paramref name="manifest"/> as the single source of
        /// truth. The manifest MUST be complete (<see cref="SessionManifest.IsComplete"/>), and the
        /// metrics MUST be finite and positive with <c>p99 ≥ p50</c> — a certified entry with a
        /// degenerate or missing number is a contradiction and fails closed here rather than
        /// entering the corpus.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="manifest"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the manifest is incomplete or <paramref name="thresholdCited"/> is empty.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown when <paramref name="p50Ms"/> / <paramref name="p99Ms"/> are non-finite,
        /// non-positive, or violate <c>p99 ≥ p50</c>.
        /// </exception>
        public static CertifiedPerfBaseline Certified(
            SessionManifest manifest,
            LoopTag loop,
            float p50Ms,
            float p99Ms,
            string thresholdCited)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }

            if (!manifest.IsComplete())
            {
                throw new ArgumentException(
                    "A certified baseline requires a complete session manifest (§3.3.2).",
                    nameof(manifest));
            }

            RequireNonEmpty(thresholdCited, nameof(thresholdCited));

            if (!float.IsFinite(p50Ms) || p50Ms <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(p50Ms), p50Ms, "Certified p50Ms must be finite and positive.");
            }

            if (!float.IsFinite(p99Ms) || p99Ms <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(p99Ms), p99Ms, "Certified p99Ms must be finite and positive.");
            }

            if (p99Ms < p50Ms)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(p99Ms), p99Ms, "Certified p99Ms must be ≥ p50Ms.");
            }

            return new CertifiedPerfBaseline(
                CertificationStatus.Certified,
                manifest.ScenarioManifestId,
                loop,
                manifest.PlatformPin,
                thresholdCited,
                manifest,
                p50Ms,
                p99Ms);
        }

        /// <summary>
        /// Projects this entry to a corpus <see cref="BaselineRecord"/> for the FR-PO-031 gate.
        /// Returns <c>false</c> with <paramref name="record"/> set to <c>null</c> while the entry is
        /// <see cref="CertificationStatus.Pending"/> — there is no measured metric to compare against,
        /// so the gate must skip rather than green-light against a fabricated number. Returns
        /// <c>true</c> with a fully populated record once certified.
        /// </summary>
        public bool TryBuildBaselineRecord(out BaselineRecord record)
        {
            if (Status != CertificationStatus.Certified)
            {
                record = null;
                return false;
            }

            record = new BaselineRecord(
                manifest: Manifest,
                loop: Loop,
                p50Ms: CertifiedP50Ms,
                p99Ms: CertifiedP99Ms,
                perMethodAllocBytes: new Dictionary<string, ulong>(),
                passFailAtCapture: BaselinePassFail.Pass,
                thresholdCited: ThresholdCited);
            return true;
        }

        private static void RequireNonEmpty(string value, string paramName)
        {
            if (string.IsNullOrEmpty(value))
            {
                throw new ArgumentException(paramName + " must be non-empty.", paramName);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-28 | —      | Initial implementation. Pending/Certified perf baseline corpus     |
// |         |            |        | entry; Pending carries no metric and refuses to build a record;    |
// |         |            |        | Certified validates a complete manifest + finite positive metrics  |
// |         |            |        | and projects to a corpus BaselineRecord. Platform-pin tokens for    |
// |         |            |        | the Stage-0 cert tuple + the Linux non-cert gate.                  |
#endregion
