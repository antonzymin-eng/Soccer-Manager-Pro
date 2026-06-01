// File:     src/performance-optimization/ReproducibilityResult.cs
// Created:  2026-06-01
// Modified: 2026-06-01
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.4.4, FR-PO-067, Code Standards #20
// Purpose:  Result returned by BaselineReproducibilityAuditor.Validate(). Carries the
//           reproducibility verdict and delta fraction for FR-PO-067 / FR-PO-068 reporting.

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Reproducibility audit result for a (original, recaptured) record pair.
    /// Produced by <see cref="BaselineReproducibilityAuditor.Validate"/>.
    /// Performance Optimization Strategy #18 §3.4.4 / FR-PO-067.
    /// </summary>
    public readonly struct ReproducibilityResult
    {
        /// <summary>
        /// True when the recaptured p50 is within
        /// <see cref="PerformanceOptimizationConstants.ReproducibilityToleranceFraction"/>
        /// of the original p50.
        /// False means the baseline is marked stale per FR-PO-068 and is merge-blocking.
        /// </summary>
        public bool IsReproducible { get; }

        /// <summary>Original p50 in milliseconds.</summary>
        public float OriginalP50Ms { get; }

        /// <summary>Recaptured p50 in milliseconds (re-run under same seed + fingerprint).</summary>
        public float RecapturedP50Ms { get; }

        /// <summary>
        /// Absolute fractional delta: |recaptured − original| / original.
        /// Compared against <see cref="PerformanceOptimizationConstants.ReproducibilityToleranceFraction"/>.
        /// </summary>
        public float AbsDeltaFraction { get; }

        /// <summary>
        /// True when scenario IDs matched between the two records.
        /// Mismatch means the re-run used a different scenario than the original (invalid).
        /// </summary>
        public bool ScenarioMatched { get; }

        /// <summary>
        /// True when seeds matched between the two records per KD-6.
        /// Mismatch means the re-run was not deterministically equivalent.
        /// </summary>
        public bool SeedMatched { get; }

        /// <summary>
        /// Initialises a reproducibility result from auditor evaluation output.
        /// </summary>
        public ReproducibilityResult(
            bool isReproducible,
            float originalP50Ms,
            float recapturedP50Ms,
            float absDeltaFraction,
            bool scenarioMatched,
            bool seedMatched)
        {
            IsReproducible    = isReproducible;
            OriginalP50Ms     = originalP50Ms;
            RecapturedP50Ms   = recapturedP50Ms;
            AbsDeltaFraction  = absDeltaFraction;
            ScenarioMatched   = scenarioMatched;
            SeedMatched       = seedMatched;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-01 | —      | Initial implementation. |
#endregion
