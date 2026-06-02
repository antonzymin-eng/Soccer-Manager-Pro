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
        /// dashboard rendering.
        /// </summary>
        /// <param name="specId">Spec ID whose §6 budget row produced the records.</param>
        /// <param name="loopTag">
        /// On-disk loop tag (one of <see cref="PerformanceOptimizationConstants.LOOP_TAG_TACTICAL_10HZ"/>
        /// or <see cref="PerformanceOptimizationConstants.LOOP_TAG_PHYSICS_60HZ"/>).
        /// </param>
        /// <param name="baseline">Pre-PR baseline record (same scenario + seed + platform pin).</param>
        /// <param name="current">Post-PR baseline record to compare against.</param>
        /// <param name="milestoneMs">
        /// p50 from the last Stage milestone baseline. Supply <see cref="float.NaN"/> when no
        /// milestone is available (skips the drift check per <see cref="RegressionGate"/> docs).
        /// </param>
        public static PerfGateReport Run(
            int specId,
            string loopTag,
            BaselineRecord baseline,
            BaselineRecord current,
            float milestoneMs)
        {
            RegressionResult regression = RegressionGate.Evaluate(baseline, current, milestoneMs);

            string scenarioManifestId =
                current?.Manifest?.ScenarioManifestId ?? string.Empty;

            return new PerfGateReport(specId, loopTag, scenarioManifestId, regression);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-02 | —      | Initial implementation. |
#endregion
