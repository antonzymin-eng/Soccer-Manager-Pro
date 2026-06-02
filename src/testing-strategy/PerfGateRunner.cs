// File:     src/testing-strategy/PerfGateRunner.cs
// Created:  2026-06-02
// Modified: 2026-06-02
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.9.2 (pointer to Spec #18),
//           Performance Optimization Strategy #18 §3.5.2 / §3.5.6 / FR-PO-031 / FR-PO-036,
//           Code Standards #20
// Purpose:  CI-side wrapper around Spec #18's RegressionGate. Pairs a baseline
//           with the current capture, evaluates per-PR and absolute-drift gates,
//           and returns a PerfGateReport carrying the spec / loop / scenario context
//           the CI message formatter needs.

using System;

using TacticalDirector.PerformanceOptimization;

namespace TacticalDirector.TestingStrategy
{
    /// <summary>
    /// CI-side runner around <see cref="RegressionGate"/>. Owns the (baseline, current,
    /// milestone) input plumbing and the spec / loop / scenario context the CI gate
    /// needs to format a merge-block message (FR-PO-036). The gate verdict itself is
    /// delegated to <see cref="RegressionGate.Evaluate"/> — Spec #19 does not redefine
    /// regression mechanics; #18 is authoritative (KD-3 boundary, §3.9.2).
    /// Testing Strategy &amp; Framework #19 §3.9.2 / Performance Optimization #18 §3.5.2.
    /// </summary>
    public static class PerfGateRunner
    {
        /// <summary>
        /// Evaluates the perf gate for one (baseline, current) record pair against an
        /// optional milestone drift bound. Returns a structured report ready for CI
        /// dashboard rendering. Both <paramref name="baseline"/> and <paramref name="current"/>
        /// MUST be non-null; <see cref="ArgumentNullException"/> is thrown otherwise.
        ///
        /// Per FR-PO-031, the +5% gate is defined only for the same scenario, seed,
        /// platform pin, and loop. The runner rejects mismatched metadata via
        /// <see cref="ArgumentException"/> before delegating to
        /// <see cref="RegressionGate.Evaluate"/> — a P50-only comparison across
        /// different scenarios produces a meaningless verdict and could silently
        /// green-light a regression. Manifest fields are validated only when both
        /// records carry a non-null <see cref="BaselineRecord.Manifest"/>; pairs
        /// with a missing manifest fall back to the AR-1 H-1 malformed-record
        /// diagnostic path. PR #132 Codex P2.
        /// </summary>
        /// <param name="specId">Spec ID whose §6 budget row produced the records.</param>
        /// <param name="loopTag">
        /// On-disk loop tag (one of <see cref="PerformanceOptimizationConstants.LOOP_TAG_TACTICAL_10HZ"/>
        /// or <see cref="PerformanceOptimizationConstants.LOOP_TAG_PHYSICS_60HZ"/>).
        /// </param>
        /// <param name="baseline">Pre-PR baseline record (same scenario + seed + platform pin); MUST be non-null.</param>
        /// <param name="current">Post-PR baseline record to compare against; MUST be non-null.</param>
        /// <param name="milestoneMs">
        /// p50 from the last Stage milestone baseline. Supply <see cref="float.NaN"/> when no
        /// milestone is available (skips the drift check per <see cref="RegressionGate"/> docs).
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="baseline"/> or <paramref name="current"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="baseline"/> and <paramref name="current"/> disagree on
        /// <see cref="BaselineRecord.Loop"/>, <see cref="SessionManifest.ScenarioManifestId"/>,
        /// <see cref="SessionManifest.Seed"/>, or <see cref="SessionManifest.PlatformPin"/>.
        /// </exception>
        public static PerfGateReport Run(
            int specId,
            string loopTag,
            BaselineRecord baseline,
            BaselineRecord current,
            float milestoneMs)
        {
            if (baseline == null)
            {
                throw new ArgumentNullException(nameof(baseline));
            }
            if (current == null)
            {
                throw new ArgumentNullException(nameof(current));
            }

            // PR #132 Codex P2: FR-PO-031 defines the +5% gate only for the same
            // scenario, seed, platform pin, and loop. RegressionGate.Evaluate compares
            // only P50Ms, so mismatched metadata would silently produce a meaningless
            // verdict (potentially a false-pass). Fail closed via ArgumentException so
            // the CI gate surfaces the mismatch.
            if (baseline.Loop != current.Loop)
            {
                throw new ArgumentException(
                    "Loop mismatch: baseline=" + baseline.Loop + " vs current=" + current.Loop
                        + ". FR-PO-031 requires matching loop tags.",
                    nameof(current));
            }

            SessionManifest baselineManifest = baseline.Manifest;
            SessionManifest currentManifest = current.Manifest;
            if (baselineManifest != null && currentManifest != null)
            {
                if (!string.Equals(
                        baselineManifest.ScenarioManifestId,
                        currentManifest.ScenarioManifestId,
                        StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        "Scenario mismatch: baseline=" + baselineManifest.ScenarioManifestId
                            + " vs current=" + currentManifest.ScenarioManifestId
                            + ". FR-PO-031 requires matching scenarios.",
                        nameof(current));
                }
                if (baselineManifest.Seed != currentManifest.Seed)
                {
                    throw new ArgumentException(
                        "Seed mismatch: baseline=" + baselineManifest.Seed
                            + " vs current=" + currentManifest.Seed
                            + ". FR-PO-031 requires matching seeds.",
                        nameof(current));
                }
                if (!string.Equals(
                        baselineManifest.PlatformPin,
                        currentManifest.PlatformPin,
                        StringComparison.Ordinal))
                {
                    throw new ArgumentException(
                        "Platform pin mismatch: baseline=" + baselineManifest.PlatformPin
                            + " vs current=" + currentManifest.PlatformPin
                            + ". FR-PO-031 requires matching platform pins.",
                        nameof(current));
                }
            }

            RegressionResult regression = RegressionGate.Evaluate(baseline, current, milestoneMs);

            // current.Manifest is required per #18 §3.3.2; the inner null-conditional guards
            // against malformed callers that skipped SessionManifest.IsComplete() and produced
            // a record with a missing manifest. The verdict has already been computed; this
            // chain only affects the rendered diagnostic.
            string scenarioManifestId =
                current.Manifest?.ScenarioManifestId ?? string.Empty;

            return new PerfGateReport(specId, loopTag, scenarioManifestId, regression);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-02 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-02 | —      | AR-1 H-1: drop misleading current?. null-conditional (Evaluate     |
// |         |            |        | NPEs first on null current). Explicit ArgumentNullException guards |
// |         |            |        | on baseline + current; inner Manifest?. kept for malformed-record  |
// |         |            |        | diagnostic only.                                                    |
// | 1.2     | 2026-06-02 | —      | PR #132 Codex P2: reject mismatched perf baselines before          |
// |         |            |        | evaluating. FR-PO-031 defines the +5% gate only for the same       |
// |         |            |        | scenario, seed, platform pin, and loop; RegressionGate.Evaluate    |
// |         |            |        | compares only P50Ms and would silently green-light a mismatched   |
// |         |            |        | pair. Run now throws ArgumentException on Loop / ScenarioManifest- |
// |         |            |        | Id / Seed / PlatformPin mismatch (Manifest checks gated on both   |
// |         |            |        | manifests being non-null — preserves the AR-1 H-1 missing-mani-   |
// |         |            |        | fest tolerance).                                                   |
#endregion
