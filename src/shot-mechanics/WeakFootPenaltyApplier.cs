// File:     src/shot-mechanics/WeakFootPenaltyApplier.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §3.8, Code Standards #20
// Purpose:  Applies weak foot error multiplier and velocity penalty when IsWeakFoot=true.
//           Formula reuses Pass Mechanics §3.7 structure (§4.1, direct reuse note).
//           Pure static calculation; no side effects.

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Applies weak foot penalties per §3.8. Pure static calculation.
    /// Shot Mechanics #6 §3.8.
    /// </summary>
    public static class WeakFootPenaltyApplier
    {
        /// <summary>
        /// Computes error cone multiplier for weak-foot shots.
        /// Returns 1.0 (no penalty) when IsWeakFoot=false or WeakFootRating=5.
        /// At WeakFootRating=1: multiplier = 1 + WeakFootBaseErrorPenalty = 1.60.
        /// Shot Mechanics #6 §3.8.3.
        /// </summary>
        public static float ComputeErrorMultiplier(bool isWeakFoot, int weakFootRating)
        {
            if (!isWeakFoot)
                return 1.0f;

            float penaltyFraction = (ShotMechanicsConstants.WeakFootRatingMax - weakFootRating)
                                    / (float)ShotMechanicsConstants.WeakFootRatingRange;
            return 1.0f + penaltyFraction * ShotMechanicsConstants.WeakFootBaseErrorPenalty;
        }

        /// <summary>
        /// Computes velocity multiplier for weak-foot shots.
        /// Returns 1.0 (no penalty) when IsWeakFoot=false or WeakFootRating=5.
        /// At WeakFootRating=1: multiplier = 1 - WeakFootVelocityPenalty = 0.80.
        /// Shot Mechanics #6 §3.8.4.
        /// </summary>
        public static float ComputeVelocityMultiplier(bool isWeakFoot, int weakFootRating)
        {
            if (!isWeakFoot)
                return 1.0f;

            float penaltyFraction = (ShotMechanicsConstants.WeakFootRatingMax - weakFootRating)
                                    / (float)ShotMechanicsConstants.WeakFootRatingRange;
            return 1.0f - penaltyFraction * ShotMechanicsConstants.WeakFootVelocityPenalty;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                       |
// | 1.0     | 2026-05-27 | —      | Initial implementation.                                                     |
// | 1.1     | 2026-05-28 | —      | M-3: Bare integer literals 5/4 replaced with WeakFootRatingMax/RatingRange.  |
// | 1.2     | 2026-05-28 | —      | L-1: XML doc constant refs corrected: SHOT_WF_BASE_ERROR_PENALTY→           |
// |         |            |        |   WeakFootBaseErrorPenalty; SHOT_WF_VELOCITY_PENALTY→WeakFootVelocityPenalty. |
#endregion
