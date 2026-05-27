// File:     src/pass-mechanics/PassErrorCalculator.cs
// Created:  2026-05-26
// Modified: 2026-05-27
// Author:   —
// Spec:     Pass Mechanics #5 §3.5, §3.7, Code Standards #20
// Purpose:  Pure static calculator for the multiplicative error chain (§3.5),
//           deterministic error direction hash (§3.5.7), and weak foot modifiers
//           (§3.7). No side effects. No System.Random — deterministic per replay.

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Computes the deterministic pass error angle and direction, and weak foot
    /// modifiers. All methods are pure functions. Pass Mechanics #5 §3.5, §3.7.
    /// </summary>
    internal static class PassErrorCalculator
    {
        // ── §3.5 — Error Angle ──────────────────────────────────────────────────────

        /// <summary>
        /// Computes the deterministic error angle (degrees) from the multiplicative
        /// modifier chain. Called at CONTACT state with fresh pressureScalar.
        /// Formula: BASE_ERROR × PassingModifier × PressureModifier × FatigueModifier
        ///          × OrientationModifier × UrgencyModifier × WeakFootModifier,
        /// clamped to [MIN_ERROR_ANGLE, MAX_ERROR_ANGLE].
        /// Pass Mechanics #5 §3.5.3, §3.5.8.
        /// </summary>
        /// <param name="passType">Determines BASE_ERROR.</param>
        /// <param name="crossSubType">Sub-type for base error lookup (Cross only).</param>
        /// <param name="passing">Agent Passing attribute [1, 20].</param>
        /// <param name="pressureScalar">Pressure from spatial hash query [0, 1].</param>
        /// <param name="fatigue">Agent fatigue [0, 1]. 0=rested.</param>
        /// <param name="bodyAngleDeg">Angle between facing direction and pass direction [0°, 90°].</param>
        /// <param name="urgency">Pass urgency from PassRequest [0, 1].</param>
        /// <param name="isWeakFoot">True if non-preferred foot used.</param>
        /// <param name="weakFootRating">Weak foot quality [1, 5]. 5 = ambidextrous.</param>
        /// <returns>Error angle in degrees clamped to [MIN_ERROR_ANGLE, MAX_ERROR_ANGLE].</returns>
        public static float ComputeErrorAngle(
            PassType passType,
            CrossSubType crossSubType,
            float passing,
            float pressureScalar,
            float fatigue,
            float bodyAngleDeg,
            float urgency,
            bool isWeakFoot,
            int weakFootRating)
        {
            float P = Mathf.Clamp(passing,       PassMechanicsConstants.ATTR_MIN, PassMechanicsConstants.ATTR_MAX);
            float S = Mathf.Clamp01(pressureScalar);
            float F = Mathf.Clamp01(fatigue);
            float alpha = Mathf.Clamp(bodyAngleDeg, 0f, 90f);
            float U = Mathf.Clamp01(urgency);

            float passingMod = PassMechanicsConstants.PassingErrorMax
                             - ((P - 1f) / (PassMechanicsConstants.ATTR_MAX - 1f))
                             * (PassMechanicsConstants.PassingErrorMax - PassMechanicsConstants.PassingErrorMin);

            float pressureMod    = 1.0f + S * PassMechanicsConstants.PressureWeight;
            float fatigueMod     = 1.0f + F * PassMechanicsConstants.FatigueAccuracyReduction;
            float orientationMod = 1.0f + (alpha / 90.0f) * PassMechanicsConstants.OrientationMaxPenalty;
            float urgencyMod     = 1.0f + U * PassMechanicsConstants.UrgencyErrorScale;
            float weakFootMod    = ComputeWeakFootAccuracyModifier(isWeakFoot, weakFootRating);

            float baseError = PassMechanicsConstants.GetBaseError(passType, crossSubType);
            float rawError  = baseError * passingMod * pressureMod * fatigueMod
                            * orientationMod * urgencyMod * weakFootMod;

            float errorAngle = Mathf.Clamp(rawError,
                PassMechanicsConstants.MinErrorAngle,
                PassMechanicsConstants.MaxErrorAngle);

            if (float.IsNaN(errorAngle))
            {
                Debug.LogError("[PassError] FM-04: ErrorAngle is NaN. Returning MinErrorAngle.");
                return PassMechanicsConstants.MinErrorAngle;
            }

            return errorAngle;
        }

        // ── §3.5.7 — Error Direction ────────────────────────────────────────────────

        /// <summary>
        /// Computes a deterministic error-direction angle [0, 2π) using a prime-XOR
        /// hash of (agentId, frameNumber, passTypeIndex). Identical inputs always
        /// produce identical output — replay-safe. No System.Random. §3.5.7.
        /// </summary>
        /// <param name="agentId">Unique ID of the passing agent.</param>
        /// <param name="frameNumber">Simulation frame from PassRequest.</param>
        /// <param name="passTypeIndex">PassType cast to int.</param>
        /// <returns>Error rotation angle in radians [0, 2π).</returns>
        public static float ComputeErrorDirection(int agentId, int frameNumber, int passTypeIndex)
        {
            int hashInput;
            unchecked  // intentional 32-bit wrap-around; deterministic hash mixing per §3.5.7
            {
                hashInput = agentId        * 73856093
                          ^ frameNumber    * 19349663
                          ^ passTypeIndex  * 83492791;
            }

            float normalised = (float)(hashInput & 0x7FFFFFFF) / (float)0x7FFFFFFF;
            return normalised * Mathf.PI * 2.0f;
        }

        // ── §3.7 — Weak Foot Modifiers ──────────────────────────────────────────────

        /// <summary>
        /// Returns the weak foot accuracy modifier for the error chain.
        /// Returns 1.0 if IsWeakFoot is false (no penalty for preferred foot).
        /// Formula: 1.0 + PenaltyFraction × WEAK_FOOT_BASE_PENALTY where
        /// PenaltyFraction = 1 − ((rating − 1) / 4). Pass Mechanics #5 §3.7.3.
        /// </summary>
        public static float ComputeWeakFootAccuracyModifier(bool isWeakFoot, int weakFootRating)
        {
            if (!isWeakFoot)
                return 1.0f;

            int R = Mathf.Clamp(weakFootRating, 1, 5);
            float penaltyFraction = 1.0f - ((float)(R - 1) / 4.0f);
            return 1.0f + penaltyFraction * PassMechanicsConstants.WeakFootBasePenalty;
        }

        /// <summary>
        /// Returns the weak foot power penalty applied to kick speed.
        /// Returns 1.0 if IsWeakFoot is false (no reduction for preferred foot).
        /// Formula: 1.0 − PenaltyFraction × WEAK_FOOT_POWER_PENALTY.
        /// Pass Mechanics #5 §3.7.4.
        /// </summary>
        public static float ComputeWeakFootPowerPenalty(bool isWeakFoot, int weakFootRating)
        {
            if (!isWeakFoot)
                return 1.0f;

            int R = Mathf.Clamp(weakFootRating, 1, 5);
            float penaltyFraction = 1.0f - ((float)(R - 1) / 4.0f);
            return 1.0f - penaltyFraction * PassMechanicsConstants.WeakFootPowerPenalty;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                     |
// | 1.0     | 2026-05-26 | —      | Initial implementation.                                                   |
// | 1.1     | 2026-05-26 | —      | H2: unchecked block added to ComputeErrorDirection hash for intentional   |
// |         |            |        |     32-bit wrap-around (coding guide determinism rule).                   |
// | 1.2     | 2026-05-27 | —      | AR-1 M-1: class changed public → internal (implementation detail).        |
// |         |            |        | AR-1 L-1: NaN fallback returns MinErrorAngle (0.1°) instead of 0f;       |
// |         |            |        |     0f violated the clamp contract [MinErrorAngle, MaxErrorAngle].        |
#endregion
