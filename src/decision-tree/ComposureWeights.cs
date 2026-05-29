// File:     src/decision-tree/ComposureWeights.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §3.3.10, Code Standards #20
// Purpose:  Constants for the composure noise scaling model (§3.3.4).
//           All composure-noise constants live here. Companion to UtilityWeights.cs.

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Constants for SelectAction() composure noise injection.
    /// Decision Tree #8 §3.3.10.
    /// </summary>
    public static class ComposureWeights
    {
        /// <summary>
        /// Maximum noise magnitude at zero composure. Range of effective utility shift
        /// at Composure=1: ±0.15. At Composure=20: ±0.0075.
        /// [GT] — validated by §3.3.4.3 against FR-09 thresholds.
        /// </summary>
        public const float NOISE_MAX = 0.15f;

        /// <summary>
        /// Composure suppression coefficient. Higher value = elite agents are more
        /// deterministic. At A_Composure=1.0: NoiseBound = NOISE_MAX × (1 − 1.0 × 0.95) = 0.0075.
        /// [GT] — derived in §3.3.4.1 to satisfy FR-09 Configuration B thresholds.
        /// </summary>
        public const float COMPOSURE_SUPPRESSION = 0.95f;

        /// <summary>
        /// Minimum EffectiveUtility delta below which two options are considered tied.
        /// Applied during tiebreak in SelectAction() (§3.3.5).
        /// [DERIVED] — epsilon small enough to not mask meaningful score differences.
        /// </summary>
        public const float TIEBREAK_EPSILON = 1e-6f;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
