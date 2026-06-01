// File:     src/performance-optimization/RegressionGate.cs
// Created:  2026-06-01
// Modified: 2026-06-01
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.5.2, §3.5.6, FR-PO-031, Code Standards #20
// Purpose:  Implements the FR-PO-031 per-PR regression threshold (+5%) and the §3.5.6
//           absolute milestone-drift guard (+10%). Used by the Stage 0+1 CI perf gate;
//           callable manually via tools/run-perf-local.sh at Stage 0.

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Evaluates the §3.5.2 per-PR regression threshold and §3.5.6 absolute-drift guard
    /// for a pair of baseline records. FR-PO-031 / FR-PO-039.
    /// Stage 0: called manually via the Appendix E runbook.
    /// Stage 0+1: called by the CI perf gate; failures block merge per FR-PO-036.
    /// Performance Optimization Strategy #18 §3.5.2 / §3.5.6.
    /// </summary>
    public static class RegressionGate
    {
        /// <summary>
        /// Checks whether the per-PR metric change falls within the
        /// <see cref="PerformanceOptimizationConstants.PerPrRegressionFraction"/> threshold.
        /// Returns true (no regression) when <c>(currentMs − baselineMs) / baselineMs ≤ threshold</c>.
        /// Improvements (negative delta) always return true.
        /// </summary>
        /// <param name="baselineMs">Pre-PR p50 in milliseconds.</param>
        /// <param name="currentMs">Post-PR p50 in milliseconds.</param>
        public static bool PassesPerPrCheck(float baselineMs, float currentMs)
        {
            if (baselineMs <= 0f)
            {
                return true;
            }

            float delta = (currentMs - baselineMs) / baselineMs;
            return delta <= PerformanceOptimizationConstants.PerPrRegressionFraction;
        }

        /// <summary>
        /// Checks whether the cumulative drift from the milestone baseline falls within
        /// the <see cref="PerformanceOptimizationConstants.AbsoluteDriftFraction"/> guard.
        /// Returns true when <c>(currentMs − milestoneMs) / milestoneMs ≤ threshold</c>.
        /// </summary>
        /// <param name="milestoneMs">p50 captured at the last Stage milestone baseline.</param>
        /// <param name="currentMs">Current p50 in milliseconds.</param>
        public static bool PassesAbsoluteDriftCheck(float milestoneMs, float currentMs)
        {
            if (milestoneMs <= 0f)
            {
                return true;
            }

            float drift = (currentMs - milestoneMs) / milestoneMs;
            return drift <= PerformanceOptimizationConstants.AbsoluteDriftFraction;
        }

        /// <summary>
        /// Full gate evaluation combining the per-PR check and the absolute-drift guard.
        /// Both must pass for <see cref="RegressionResult.AllPassed"/> to be true.
        /// </summary>
        /// <param name="baseline">Pre-PR baseline record (same scenario + seed + platform pin).</param>
        /// <param name="current">Post-PR baseline record to compare against.</param>
        /// <param name="milestoneMs">
        /// p50 from the last Stage milestone baseline; supply float.NaN when unavailable.
        /// When NaN the absolute-drift check returns true (not enough data to guard).
        /// </param>
        public static RegressionResult Evaluate(
            BaselineRecord baseline,
            BaselineRecord current,
            float milestoneMs)
        {
            float baselineP50 = baseline.P50Ms;
            float currentP50  = current.P50Ms;

            float deltaFraction = baselineP50 > 0f
                ? (currentP50 - baselineP50) / baselineP50
                : 0f;

            bool perPrPassed = deltaFraction
                <= PerformanceOptimizationConstants.PerPrRegressionFraction;

            float milestoneDrift = float.NaN;
            bool absoluteDriftPassed = true;

            if (!float.IsNaN(milestoneMs) && milestoneMs > 0f)
            {
                milestoneDrift      = (currentP50 - milestoneMs) / milestoneMs;
                absoluteDriftPassed = milestoneDrift
                    <= PerformanceOptimizationConstants.AbsoluteDriftFraction;
            }

            return new RegressionResult(
                perPrPassed,
                absoluteDriftPassed,
                deltaFraction,
                milestoneDrift);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-01 | —      | Initial implementation. |
#endregion
