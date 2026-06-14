// File:     src/perception-system/FovCalculator.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Perception System #7 §3.1, Code Standards #20
// Purpose:  Computes effective FoV angle per agent per heartbeat (§3.1).
//           Applies Decisions attribute modifier and PressureScalar degradation.
//           Performs angular candidacy test for each entity in the candidate list.
//           Static class. All methods are deterministic. No side effects.

using UnityEngine;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Computes effective FoV angle per agent per heartbeat and tests entity candidacy.
    /// Applies Decisions modifier (§3.1.3) and pressure degradation (§3.1.4).
    /// All methods are deterministic and produce no side effects.
    /// Perception System #7 §3.1.
    /// </summary>
    public static class FovCalculator
    {
        /// <summary>
        /// Computes the effective FoV full angle (degrees) after Decisions modifier and pressure narrowing.
        /// Decisions normalisation is decisions/ATTR_MAX per the §3.9 worked examples (160° + (D/20)×10°).
        /// Only the MIN_FOV_ANGLE floor is applied explicitly; the upper bound of
        /// BASE_FOV_ANGLE + MAX_FOV_BONUS_ANGLE = 170° holds by construction (pressure only narrows,
        /// and D ≤ 20), so the effective range is [120°, 170°].
        /// Perception System #7 §3.1.3–§3.1.4.
        /// </summary>
        /// <param name="decisions">Decisions attribute [1–20].</param>
        /// <param name="pressureScalar">Pressure scalar [0, 1] from PressureEvaluator.</param>
        public static float ComputeEffectiveFoV(int decisions, float pressureScalar)
        {
            float foVBeforePressure = PerceptionConstants.BASE_FOV_ANGLE
                + (decisions / PerceptionConstants.ATTR_MAX) * PerceptionConstants.MAX_FOV_BONUS_ANGLE;

            float reduction = pressureScalar * PerceptionConstants.MAX_FOV_PRESSURE_REDUCTION;
            float effectiveFoV = foVBeforePressure - reduction;

            return Mathf.Max(effectiveFoV, PerceptionConstants.MIN_FOV_ANGLE);
        }

        /// <summary>
        /// Returns the half-angle of the effective FoV cone (effectiveFoV / 2).
        /// Perception System #7 §3.1.4.
        /// </summary>
        public static float ComputeEffectiveFoVHalfAngle(float effectiveFoV)
        {
            return effectiveFoV * 0.5f;
        }

        /// <summary>
        /// Computes the signed angular separation in degrees between the agent's facing direction
        /// and the direction to the target entity. Result is normalised to [−180°, +180°].
        /// Perception System #7 §3.1.2.
        /// </summary>
        /// <param name="observerPos">Observer's 2D world position.</param>
        /// <param name="facingDir">Observer's normalised 2D facing direction.</param>
        /// <param name="entityPos">Target entity's 2D world position.</param>
        public static float ComputeAngularSeparationDeg(
            Vector2 observerPos,
            Vector2 facingDir,
            Vector2 entityPos)
        {
            Vector2 delta = entityPos - observerPos;
            float bearingToEntity = Mathf.Atan2(delta.y, delta.x) * PerceptionConstants.RAD_TO_DEG;
            float facingAngle     = Mathf.Atan2(facingDir.y, facingDir.x) * PerceptionConstants.RAD_TO_DEG;

            float separation = bearingToEntity - facingAngle;

            // Normalise to [−180°, +180°]
            if (separation > PerceptionConstants.HALF_CIRCLE_DEG)
            {
                separation -= PerceptionConstants.FULL_CIRCLE_DEG;
            }
            else if (separation < -PerceptionConstants.HALF_CIRCLE_DEG)
            {
                separation += PerceptionConstants.FULL_CIRCLE_DEG;
            }

            return separation;
        }

        /// <summary>
        /// Returns true if the entity at entityPos is within the agent's forward FoV cone.
        /// Entities exactly on the boundary (separation == halfAngle) are included.
        /// Perception System #7 §3.1.2.
        /// </summary>
        /// <param name="observerPos">Observer's 2D world position.</param>
        /// <param name="facingDir">Observer's normalised 2D facing direction.</param>
        /// <param name="entityPos">Target entity's 2D world position.</param>
        /// <param name="effectiveFoVHalfAngleDeg">Half-angle of the effective FoV cone (degrees).</param>
        public static bool IsInFoV(
            Vector2 observerPos,
            Vector2 facingDir,
            Vector2 entityPos,
            float effectiveFoVHalfAngleDeg)
        {
            float separation = ComputeAngularSeparationDeg(observerPos, facingDir, entityPos);
            return Mathf.Abs(separation) <= effectiveFoVHalfAngleDeg;
        }

        /// <summary>
        /// Returns true if the entity is in the blind-side arc — the portion of 360° not covered
        /// by the forward FoV cone. Perception System #7 §3.4.1.
        /// </summary>
        /// <param name="observerPos">Observer's 2D world position.</param>
        /// <param name="facingDir">Observer's normalised 2D facing direction.</param>
        /// <param name="entityPos">Target entity's 2D world position.</param>
        /// <param name="effectiveFoVHalfAngleDeg">Half-angle of the effective FoV cone (degrees).</param>
        public static bool IsInBlindSide(
            Vector2 observerPos,
            Vector2 facingDir,
            Vector2 entityPos,
            float effectiveFoVHalfAngleDeg)
        {
            float separation = ComputeAngularSeparationDeg(observerPos, facingDir, entityPos);
            return Mathf.Abs(separation) > effectiveFoVHalfAngleDeg;
        }

        /// <summary>
        /// Returns true if the entity is in the peripheral arc — the outer half of the forward FoV cone,
        /// beyond the central focal zone. Used for the half-turn L_rec reduction check (§3.3.3).
        /// Peripheral zone: angular separation in [PERIPHERAL_ARC_INNER_BOUND, BASE_FOV_HALF_ANGLE].
        /// Perception System #7 §3.3.3.
        /// </summary>
        /// <param name="angularSeparationAbsDeg">Absolute angular separation (degrees) to the entity.</param>
        public static bool IsInPeripheralArc(float angularSeparationAbsDeg)
        {
            return angularSeparationAbsDeg >= PerceptionConstants.PERIPHERAL_ARC_INNER_BOUND
                && angularSeparationAbsDeg <= PerceptionConstants.BASE_FOV_HALF_ANGLE;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
// | 1.1     | 2026-06-13 | —      | AR-3 L-4: ComputeEffectiveFoV doc clarifies the decisions/ATTR_MAX normalisation (per §3.9) and that only the MIN_FOV_ANGLE floor is explicit (170° upper bound holds by construction). No code change. |
#endregion
