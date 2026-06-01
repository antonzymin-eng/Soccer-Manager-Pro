// File:     src/performance-optimization/RegressionResult.cs
// Created:  2026-06-01
// Modified: 2026-06-01
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.5.2, §3.5.6, FR-PO-031, Code Standards #20
// Purpose:  Result returned by RegressionGate.Evaluate(). Carries per-PR and absolute-drift
//           verdicts plus the measured delta fraction for CI gate reporting (FR-PO-036).

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Regression gate evaluation result for one (baseline, current) record pair.
    /// Produced by <see cref="RegressionGate.Evaluate"/>.
    /// Performance Optimization Strategy #18 §3.5.2 / §3.5.6 / FR-PO-031.
    /// </summary>
    public readonly struct RegressionResult
    {
        /// <summary>
        /// True when the per-PR delta is within <see cref="PerformanceOptimizationConstants.PerPrRegressionFraction"/>.
        /// False means the CI perf gate BLOCKS merge per FR-PO-036.
        /// </summary>
        public bool PerPrPassed { get; }

        /// <summary>
        /// True when drift from the milestone baseline is within
        /// <see cref="PerformanceOptimizationConstants.AbsoluteDriftFraction"/>.
        /// False means the absolute-drift guard BLOCKS merge per §3.5.6 / FR-PO-039.
        /// </summary>
        public bool AbsoluteDriftPassed { get; }

        /// <summary>
        /// Signed fractional delta: (currentMs − baselineMs) / baselineMs.
        /// Positive = regression; negative = improvement.
        /// </summary>
        public float DeltaFraction { get; }

        /// <summary>
        /// Absolute drift from the milestone baseline: (currentMs − milestoneMs) / milestoneMs.
        /// Compared against <see cref="PerformanceOptimizationConstants.AbsoluteDriftFraction"/>.
        /// Float.NaN when no milestone baseline is available.
        /// </summary>
        public float MilestoneDriftFraction { get; }

        /// <summary>True when both <see cref="PerPrPassed"/> and <see cref="AbsoluteDriftPassed"/> are true.</summary>
        public bool AllPassed => PerPrPassed && AbsoluteDriftPassed;

        /// <summary>
        /// Initialises a regression result from gate evaluation output.
        /// </summary>
        public RegressionResult(
            bool perPrPassed,
            bool absoluteDriftPassed,
            float deltaFraction,
            float milestoneDriftFraction)
        {
            PerPrPassed            = perPrPassed;
            AbsoluteDriftPassed    = absoluteDriftPassed;
            DeltaFraction          = deltaFraction;
            MilestoneDriftFraction = milestoneDriftFraction;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-01 | —      | Initial implementation. |
#endregion
