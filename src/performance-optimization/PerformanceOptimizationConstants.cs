// File:     src/performance-optimization/PerformanceOptimizationConstants.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.10, §8.4, Appendix B, Code Standards #20
// Purpose:  Constant catalogue for Spec #18 governance constants.
//           These are cross-cutting infrastructure constants; game-layer assemblies
//           must NOT import this assembly at runtime (src/CLAUDE.md layer taxonomy).

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Constant catalogue for Performance Optimization Strategy #18.
    /// All constants are infrastructure governance values; none are on game hot paths.
    /// Performance Optimization Strategy #18 §3.10 / §8.4.
    /// </summary>
    public static class PerformanceOptimizationConstants
    {
        #region GT
        /// <summary>
        /// [GT] Per-PR regression threshold as a fraction (5 % = 0.05).
        /// A per-spec budget increase beyond this fraction triggers a regression alert.
        /// §3.5.2 / FR-PO-031. // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float PerPrRegressionFraction = 0.05f;

        /// <summary>
        /// [GT] Absolute milestone-baseline drift threshold as a fraction (10 % = 0.10).
        /// Guards against slow accumulation that escapes per-PR alerts. §3.5.6 / FR-PO-031.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float AbsoluteDriftFraction = 0.10f;

        /// <summary>
        /// [GT] Minimum sample count for a statistical baseline to be considered stable.
        /// N=100 reduces 1%-flake false-positive rate to an acceptable level. §3.4.4 / §8.4.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly int BaselineSampleCount = 100;

        /// <summary>
        /// [GT] Maximum acceptable test-flake rate before a scenario is flagged for
        /// boundary-defect routing. §5.7.3 / §3.10 / §8.4.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float MaxFlakeRate = 0.01f;

        /// <summary>
        /// [GT] Typical headroom multiplier (lower bound) reserved per-spec §6 to absorb
        /// measurement variance. §3.4.4 / Appendix B / Appendix C.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float HeadroomMultiplierMin = 1.2f;

        /// <summary>
        /// [GT] Typical headroom multiplier (upper bound). §3.4.4 / Appendix B.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float HeadroomMultiplierMax = 1.5f;
        #endregion

        #region Fixed
        /// <summary>
        /// [FIXED] Zero-byte hot-path allocation budget per entry point.
        /// Any non-zero managed allocation on a listed hot-path entry is a build failure.
        /// §3.7 / FR-PO-032 / FR-CS-066.
        /// </summary>
        public const int HotPathAllocBudgetBytes = 0;
        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
