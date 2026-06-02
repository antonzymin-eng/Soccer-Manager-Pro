// File:     src/performance-optimization/PerformanceOptimizationConstants.cs
// Created:  2026-05-30
// Modified: 2026-06-02
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
        #region Fixed
        /// <summary>
        /// [FIXED] Zero-byte hot-path allocation budget per entry point.
        /// Any non-zero managed allocation on a listed hot-path entry is a build failure.
        /// §3.7.3 / FR-PO-032 / FR-CS-066.
        /// </summary>
        public const int HOT_PATH_ALLOC_BUDGET_BYTES = 0;

        /// <summary>
        /// [FIXED] On-disk loop-tag string for the 10 Hz tactical heartbeat loop.
        /// Used in §6 budget tables and baseline-record JSON projections. §3.2.2.
        /// </summary>
        public const string LOOP_TAG_TACTICAL_10HZ = "LOOP-TACTICAL-10HZ";

        /// <summary>
        /// [FIXED] On-disk loop-tag string for the 60 Hz physics/render loop.
        /// Used in §6 budget tables and baseline-record JSON projections. §3.2.2.
        /// </summary>
        public const string LOOP_TAG_PHYSICS_60HZ = "LOOP-PHYSICS-60HZ";
        #endregion

        #region GT
        /// <summary>
        /// [GT] Per-PR regression threshold as a fraction (5% = 0.05).
        /// A per-spec budget increase beyond this fraction triggers a regression alert.
        /// §3.5.2 / FR-PO-031. // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float PerPrRegressionFraction = 0.05f;

        /// <summary>
        /// [GT] Absolute milestone-baseline drift threshold as a fraction (10% = 0.10).
        /// Guards against slow accumulation that escapes per-PR alerts. §3.5.6 / FR-PO-039.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float AbsoluteDriftFraction = 0.10f;

        /// <summary>
        /// [GT] Minimum sample count for a statistical baseline to be considered stable,
        /// and rolling-window size for per-spec dashboard percentiles (Appendix F.1).
        /// N=100 reduces 1%-flake false-positive rate to an acceptable level. §3.4.4 / §8.4.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly int BaselineSampleCount = 100;

        /// <summary>
        /// [GT] Maximum acceptable test-flake rate before a scenario is flagged for
        /// boundary-defect routing. §5.7.3 / §3.10 / §8.4 / Appendix F.5.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float MaxFlakeRate = 0.01f;

        /// <summary>
        /// [GT] Typical headroom multiplier lower bound reserved per-spec §6 to absorb
        /// measurement variance. §3.1.2 / §3.4.4 / Appendix B / Appendix C.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float HeadroomMultiplierMin = 1.2f;

        /// <summary>
        /// [GT] Typical headroom multiplier upper bound. §3.1.2 / §3.4.4 / Appendix B.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float HeadroomMultiplierMax = 1.5f;

        /// <summary>
        /// [GT] [EST]→[GT] promotion tolerance fraction (±20% = 0.20).
        /// First Stage 0+1 baseline capture promotes a spec-time [EST] anchor to [GT]
        /// if within ±20% of the estimate; files an ERR-018-NNN finding if not. §3.9.1.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float PromotionToleranceFraction = 0.20f;

        /// <summary>
        /// [GT] Reproducibility tolerance fraction (±20% = 0.20).
        /// A re-captured baseline must match the original p50 within this fraction
        /// to be considered reproducible per §3.4.4 / FR-PO-067.
        /// Shares the §3.9.1 ±20% value; promoted independently if divergence is measured.
        /// // TODO: replace with config loader (Stage 1)
        /// </summary>
        public static readonly float ReproducibilityToleranceFraction = 0.20f;
        #endregion

        #region EST
        /// <summary>
        /// [EST] Sampling-profiler default sample rate in Hz (1 kHz = 1000).
        /// Pinned to the chosen profiler binary at Stage 0+1 (§7.5 D1). §3.3.4.
        /// </summary>
        public static readonly int SamplerDefaultHz = 1000;

        /// <summary>
        /// [EST] Independent profiling run count required for statistical significance.
        /// Provisional: 30 runs / 95% CI per §3.4.3, parallel to Spec #19 §3.4.3.
        /// Pinned at Stage 0+1 (§7.5 D8).
        /// </summary>
        public static readonly int StatisticalSignificanceN = 30;

        /// <summary>
        /// [EST] Number of first-tick warmup ticks excluded from regression gates.
        /// Accounts for JIT warm-up under Mono. Pinned at Stage 0+1 once Mono/IL2CPP
        /// warmup characteristics are measured (§3.9.4). Placeholder: 0 until pinned.
        /// </summary>
        public static readonly int FirstTickWarmupCount = 0;
        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                      |
// | 1.0     | 2026-05-30 | —      | Initial implementation.                                    |
// | 1.1     | 2026-06-01 | —      | Added Fixed region LOOP_TAG_* strings (§3.2.2). Added GT   |
// |         |            |        | PromotionToleranceFraction + ReproducibilityToleranceFrac- |
// |         |            |        | tion (§3.9.1 / FR-PO-067). Added EST region: SamplerDe-    |
// |         |            |        | faultHz, StatisticalSignificanceN, FirstTickWarmupCount     |
// |         |            |        | (§3.3.4 / §3.4.3 / §3.9.4). Region order corrected to      |
// |         |            |        | Fixed → GT → EST per src/CLAUDE.md most-immutable-first.   |
// | 1.2     | 2026-06-02 | —      | AR-1 H-2: HotPathAllocBudgetBytes → HOT_PATH_ALLOC_BUDGET_ |
// |         |            |        | BYTES (FIXED constants are ALL_CAPS per FR-CS-001).        |
// |         |            |        | AR-1 L-4: removed stale "Also used as the reproducibility  |
// |         |            |        | confidence-interval tolerance" sentence from Promotion-    |
// |         |            |        | ToleranceFraction; auditor consumes ReproducibilityToler-  |
// |         |            |        | anceFraction instead.                                       |
#endregion
