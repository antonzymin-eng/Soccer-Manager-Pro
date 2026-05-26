// File:     src/first-touch/ControlQualityCalculator.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     First Touch Mechanics #4 §3.1, Code Standards #20
// Purpose:  Computes control quality scalar [0,1] from attributes, ball speed, agent speed, pressure, and orientation.

using UnityEngine;

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// Computes the control quality scalar (q ∈ [0,1]) for one first-touch attempt.
    /// Implements the 8-step formula from First Touch Mechanics #4 §3.1.
    /// </summary>
    internal static class ControlQualityCalculator
    {
        /// <summary>
        /// Calculates control quality from all contributing factors. First Touch Mechanics #4 §3.1.
        /// Returns a value in [0, ThunderboltQualityCap] when ballSpeed ≥ ThunderboltSpeed,
        /// otherwise in [0, 1].
        /// </summary>
        /// <param name="technique">Technique attribute of the agent [1–20].</param>
        /// <param name="firstTouch">FirstTouch attribute of the agent [1–20].</param>
        /// <param name="ballSpeed">Speed of the incoming ball (m/s); must be ≥ VelocityMin after guard.</param>
        /// <param name="agentSpeed">Speed of the receiving agent (m/s).</param>
        /// <param name="pressureScalar">Pre-computed pressure scalar [0,1].</param>
        /// <param name="orientationBonus">Bonus value from OrientationDetector (HalfTurnBonus or 0).</param>
        internal static float Calculate(
            int technique,
            int firstTouch,
            float ballSpeed,
            float agentSpeed,
            float pressureScalar,
            float orientationBonus)
        {
            // Step 1 — Guard attribute floor.
            technique = Mathf.Max(technique, FirstTouchConstants.AttrMinGuard);
            firstTouch = Mathf.Max(firstTouch, FirstTouchConstants.AttrMinGuard);

            // Step 2 — Weighted attribute blend, normalised to [0,1]. §3.1.1 Steps 1–2.
            float weightedAttr = FirstTouchConstants.TechniqueWeight * technique
                               + FirstTouchConstants.FirstTouchWeight * firstTouch;
            float normAttr = weightedAttr / FirstTouchConstants.AttrMax;

            // Step 3 — Orientation bonus: multiplicative boost on the normalised attribute. §3.1.1 Step 3.
            float attrWithBonus = normAttr * (1.0f + orientationBonus);

            // Step 4 — Velocity difficulty. §3.1.1 Step 4.
            ballSpeed = Mathf.Max(ballSpeed, FirstTouchConstants.VelocityMin);
            float velDifficulty = Mathf.Clamp(
                ballSpeed / FirstTouchConstants.VelocityReference,
                FirstTouchConstants.VelocityDifficultyMin,
                FirstTouchConstants.VelocityMaxFactor);

            // Step 5 — Movement difficulty: always applied; standing (0 m/s) → 1.0. §3.1.1 Step 5.
            float moveDifficulty = 1.0f
                + (agentSpeed / FirstTouchConstants.MovementReference)
                * FirstTouchConstants.MovementPenalty;

            // Step 6 — Raw quality. §3.1.1 Step 6.
            float rawQuality = attrWithBonus / (velDifficulty * moveDifficulty);

            // Step 7 — Pressure degradation: multiplicative. §3.1.1 Step 7.
            float q = rawQuality * (1.0f - pressureScalar * FirstTouchConstants.PressureWeight);

            // Step 8 — Clamp to [0,1]; then apply thunderbolt cap.
            q = Mathf.Clamp01(q);
            if (ballSpeed >= FirstTouchConstants.ThunderboltSpeed)
            {
                q = Mathf.Min(q, FirstTouchConstants.ThunderboltQualityCap);
            }

            return q;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                                                                               |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                                                                                      |
// | 1.1     | 2026-05-26 | —      | H-1 fix: orientation bonus now multiplicative on normAttr; moveDifficulty always applied (no conditional); pressure degradation now multiplicative. |
#endregion
