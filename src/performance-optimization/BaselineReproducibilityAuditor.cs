// File:     src/performance-optimization/BaselineReproducibilityAuditor.cs
// Created:  2026-06-01
// Modified: 2026-06-07
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.4.4, §5.4, FR-PO-067, Code Standards #20
// Purpose:  Implements the FR-PO-067 baseline-reproducibility auditor.
//           Re-run validation: confirms a recaptured baseline matches the original within
//           ReproducibilityToleranceFraction. Silently skipping the re-run is
//           an FR-PO-067 violation and is merge-blocking per FR-PO-068.
//           Stage 0 carve-out: no src/ runtime yet; the MUST activates at Stage 0+1.

using System;

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Validates that a re-captured baseline record reproduces the original within the
    /// §3.4.3 confidence interval. Implements FR-PO-067 §5.4 reproducibility auditor.
    /// Stage 0 carve-out per §3.4.4: the MUST constraint activates at Stage 0+1 when a
    /// deterministic runtime exists to re-run against.
    /// Performance Optimization Strategy #18 §3.4.4 / §5.4 / FR-PO-067.
    /// </summary>
    public static class BaselineReproducibilityAuditor
    {
        /// <summary>
        /// Validates that the recaptured baseline reproduces the original within
        /// <see cref="PerformanceOptimizationConstants.ReproducibilityToleranceFraction"/>.
        /// Both records must share the same scenario manifest ID and seed (KD-6).
        /// Mismatches mark the original baseline stale; stale baselines are merge-blocking
        /// per FR-PO-068.
        /// Degenerate originals (<c>origP50 ≤ 0</c> or NaN) fail-closed per AR-1 M-3.
        /// </summary>
        /// <param name="original">Original baseline record from the corpus.</param>
        /// <param name="recaptured">
        /// Baseline captured by re-running the same session manifest (same seed +
        /// EnvironmentFingerprint + platform pin + scenario ID).
        /// </param>
        public static ReproducibilityResult Validate(BaselineRecord original, BaselineRecord recaptured)
        {
            if (original == null)
            {
                throw new ArgumentNullException(nameof(original));
            }

            if (recaptured == null)
            {
                throw new ArgumentNullException(nameof(recaptured));
            }

            bool scenarioMatched = original.Manifest.ScenarioManifestId
                == recaptured.Manifest.ScenarioManifestId;

            bool seedMatched = original.Manifest.Seed == recaptured.Manifest.Seed;

            float origP50       = original.P50Ms;
            float recapturedP50 = recaptured.P50Ms;

            bool originalIsUsable = origP50 > 0f;
            float absDelta = originalIsUsable
                ? System.Math.Abs(recapturedP50 - origP50) / origP50
                : float.NaN;

            bool withinTolerance = originalIsUsable
                && absDelta <= PerformanceOptimizationConstants.ReproducibilityToleranceFraction;

            bool isReproducible = scenarioMatched && seedMatched && withinTolerance;

            return new ReproducibilityResult(
                isReproducible,
                origP50,
                recapturedP50,
                absDelta,
                scenarioMatched,
                seedMatched);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-01 | —      | Initial implementation.                                            |
// | 1.1     | 2026-06-02 | —      | AR-1 L-2: sealed class → static class (stateless validator).        |
// |         |            |        | AR-1 M-3: degenerate origP50 (≤0 or NaN) fails closed instead of    |
// |         |            |        | silently passing as reproducible.                                   |
// | 1.2     | 2026-06-07 | —      | AR-3 M-1: Validate now explicitly null-checks original / recaptured |
// |         |            |        | with ArgumentNullException (symmetric to PerfGateRunner.Run). The   |
// |         |            |        | BaselineRecord ctor now enforces non-null Manifest (AR-3 M-2), so   |
// |         |            |        | the Manifest dereferences below are NRE-free once both record args  |
// |         |            |        | are non-null.                                                       |
#endregion
