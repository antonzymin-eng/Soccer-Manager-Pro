// File:     src/goalkeeper-mechanics/GoalkeeperHandlingQuality.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.5, KD-1, KD-2, KD-7, KD-21, Code Standards #20
// Purpose:  Handling-quality scalar computation and band-to-action velocity helpers.
//           All methods are pure static with no side effects.

using UnityEngine;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Handling-quality scalar computation (§3.5) and band-to-action velocity helpers.
    /// KD-7: two separate draw sites (HANDLING_NOISE and HANDLING_POINT_NOISE) per single-purpose-per-site rule.
    /// KD-21: band-to-action thresholds are Caught/Parried/Deflected/Spilled/Missed.
    /// KD-2: HandlingQualityLabel is telemetry only; physics never branches on it.
    /// All methods are deterministic and side-effect-free.
    /// Goalkeeper Mechanics #11 §3.5.
    /// </summary>
    public static class GoalkeeperHandlingQuality
    {
        /// <summary>
        /// Computes the handling quality scalar [0, 1] for a save contact attempt.
        /// Formula: §3.5.1 multi-component attrFactor × speedFactor × pointQuality + noise blend with reactionWindowAchieved.
        /// Two Gaussian draws from separate draw sites per KD-7 single-purpose-per-site rule.
        /// §3.5.1. Goalkeeper Mechanics #11 §3.5.
        /// </summary>
        /// <param name="attrs">GK agent attributes snapshot. §3.5.</param>
        /// <param name="handContactActual">World-space actual hand contact point. §3.5.1.</param>
        /// <param name="targetHandContact">World-space target contact point from SaveIntent. §3.5.1.</param>
        /// <param name="ballSpeedMps">Ball speed (m/s) at contact frame. §3.5.1.</param>
        /// <param name="reactionWindowAchieved">Reaction quality [0, 1] from §3.2.3. §3.5.1.</param>
        /// <param name="state">Current GK state (OneVsOne bonus applies when state == OneOnOne per KD-20). §3.5.1.</param>
        /// <param name="handlingScaleNoise">Pre-drawn Gaussian noise sample for handlingScaleNoise (draw-site: HANDLING_NOISE). §3.5.1 / KD-7.</param>
        /// <param name="pointErrorNoise">Pre-drawn Gaussian noise sample for pointErrorNoise (draw-site: HANDLING_POINT_NOISE). §3.5.1 / KD-7.</param>
        /// <param name="handlingLabel">Output: telemetry label for the handling outcome (KD-2; not consumed by physics). §3.5.2.</param>
        /// <returns>Handling quality scalar [0, 1].</returns>
        public static float Compute(
            GoalkeeperAgentAttributes attrs,
            Vector3 handContactActual,
            Vector3 targetHandContact,
            float ballSpeedMps,
            float reactionWindowAchieved,
            GoalkeeperState state,
            float handlingScaleNoise,
            float pointErrorNoise,
            out HandlingQualityLabel handlingLabel)
        {
            // ── Point quality ────────────────────────────────────────────────────────
            float rawContactPointError = (handContactActual - targetHandContact).magnitude
                                       + pointErrorNoise;

            float pointQuality = 1.0f - Clamp01(rawContactPointError / GoalkeeperConstants.HandlingPointErrorSigmaM);

            // ── Speed factor ─────────────────────────────────────────────────────────
            float excessSpeed = Mathf_Max(0.0f, ballSpeedMps - GoalkeeperConstants.HandlingBallSpeedRefMps);
            float speedFactor = Clamp01(
                1.0f - GoalkeeperConstants.HandlingKBallSpeed * excessSpeed / GoalkeeperConstants.HandlingBallSpeedRefMps);

            // ── Attribute factor ─────────────────────────────────────────────────────
            float attrFactor = GoalkeeperConstants.HandlingBase
                             + GoalkeeperConstants.HandlingKAttr * attrs.HandlingNorm
                             - GoalkeeperConstants.HandlingFatigueCoeff * attrs.Fatigue;

            if (state == GoalkeeperState.OneOnOne)
            {
                attrFactor += GoalkeeperConstants.OneVsOneHandlingCoeff * attrs.OneVsOneNorm;
            }

            // ── Raw handling ─────────────────────────────────────────────────────────
            float rawHandling = attrFactor * speedFactor * pointQuality
                              + GoalkeeperConstants.HandlingNoiseSigma * handlingScaleNoise;

            // ── Final quality scalar ─────────────────────────────────────────────────
            float alpha = GoalkeeperConstants.HandlingReactionBlendAlpha;
            float handlingQualityScalar = Clamp01(
                alpha * rawHandling + (1.0f - alpha) * reactionWindowAchieved);

            // ── Telemetry label (KD-2 / KD-21) ──────────────────────────────────────
            handlingLabel = AssignHandlingLabel(handlingQualityScalar);

            return handlingQualityScalar;
        }

        /// <summary>
        /// Computes the parry rebound velocity vector.
        /// Formula: retain = clamp01(PARRY_VELOCITY_RETAIN_BASE - PARRY_VELOCITY_RETAIN_K_QUALITY × quality
        ///                           - CLUTCH_FIRMNESS_K_RETAIN × clutchFirmness).
        ///          deflectAngle = PARRY_DEFLECT_ANGLE_SIGMA_RAD × (1 - quality).
        ///          return rotate(incomingVelocity × -retain, deflectAngle).
        /// §3.5.3. Goalkeeper Mechanics #11 §3.5.
        /// </summary>
        /// <param name="incomingVelocity">Incoming ball velocity (m/s). §3.5.3.</param>
        /// <param name="quality">handlingQualityScalar [0, 1] for this save attempt. §3.5.3.</param>
        /// <param name="clutchFirmness">ClutchFirmness from SaveIntent [0, 1]. §3.5.3.</param>
        /// <returns>Parry rebound velocity vector (m/s).</returns>
        public static Vector3 ComputeParryVelocity(Vector3 incomingVelocity, float quality, float clutchFirmness)
        {
            float retain = Clamp01(
                GoalkeeperConstants.ParryVelocityRetainBase
                - GoalkeeperConstants.ParryVelocityRetainKQuality * quality
                - GoalkeeperConstants.ClutchFirmnessKRetain * clutchFirmness);

            float deflectAngle = GoalkeeperConstants.ParryDeflectAngleSigmaRad * (1.0f - quality);

            Vector3 reboundDir = -incomingVelocity.normalized;
            float   reboundSpeed = incomingVelocity.magnitude * retain;

            return RotateVectorXY(reboundDir * reboundSpeed, deflectAngle);
        }

        /// <summary>
        /// Computes the deflection velocity vector (quality below parry threshold, above deflect threshold).
        /// Formula: retain = clamp01(PARRY_VELOCITY_RETAIN_BASE - PARRY_VELOCITY_RETAIN_K_QUALITY × quality + 0.10).
        ///          targetDir = normalize(deflectionTarget - handContactActual).
        ///          return targetDir × (retain × |incomingVelocity|).
        /// §3.5.3. Goalkeeper Mechanics #11 §3.5.
        /// </summary>
        /// <param name="incomingVelocity">Incoming ball velocity (m/s). §3.5.3.</param>
        /// <param name="quality">handlingQualityScalar [0, 1]. §3.5.3.</param>
        /// <param name="deflectionTarget">World-space target point for the deflection (from SaveIntent.DeflectionTarget). §3.5.3.</param>
        /// <param name="handContactActual">World-space actual hand contact position. §3.5.3.</param>
        /// <returns>Deflect velocity vector (m/s) directed toward deflectionTarget.</returns>
        public static Vector3 ComputeDeflectVelocity(
            Vector3 incomingVelocity,
            float quality,
            Vector3 deflectionTarget,
            Vector3 handContactActual)
        {
            // Deflections retain slightly more than parry (DeflectRetainBonus per §3.5.3)
            float retain = Clamp01(
                GoalkeeperConstants.ParryVelocityRetainBase
                - GoalkeeperConstants.ParryVelocityRetainKQuality * quality
                + GoalkeeperConstants.DeflectRetainBonus);

            Vector3 toTarget = deflectionTarget - handContactActual;
            float sqLen = toTarget.sqrMagnitude;

            Vector3 targetDir = sqLen > GoalkeeperConstants.DEGENERACY_EPSILON_SQ
                ? toTarget / Mathf.Sqrt(sqLen)
                : -incomingVelocity.normalized;

            float speed = incomingVelocity.magnitude * retain;
            return targetDir * speed;
        }

        /// <summary>
        /// Computes the spill velocity vector (quality below deflect threshold, above MIN_HANDLING_QUALITY).
        /// Formula: retain = PARRY_VELOCITY_RETAIN_BASE + 0.20 (spills retain more — poor handling).
        ///          deflectAngle = PARRY_DEFLECT_ANGLE_SIGMA_RAD × (1 - quality).
        ///          return rotate(incomingVelocity × -retain, deflectAngle).
        /// No additional Gaussian here per §3.5.3 v0.2 fix (AR-S1-H2): upstream §3.5.1 noise already covers variability.
        /// §3.5.3. Goalkeeper Mechanics #11 §3.5.
        /// </summary>
        /// <param name="incomingVelocity">Incoming ball velocity (m/s). §3.5.3.</param>
        /// <param name="quality">handlingQualityScalar [0, 1]. §3.5.3.</param>
        /// <returns>Spill velocity vector (m/s) — loose ball direction.</returns>
        public static Vector3 ComputeSpillVelocity(Vector3 incomingVelocity, float quality)
        {
            float retain = GoalkeeperConstants.ParryVelocityRetainBase + GoalkeeperConstants.SpillRetainBonus;
            float deflectAngle = GoalkeeperConstants.ParryDeflectAngleSigmaRad * (1.0f - quality);

            Vector3 reboundDir   = -incomingVelocity.normalized;
            float   reboundSpeed = incomingVelocity.magnitude * retain;

            return RotateVectorXY(reboundDir * reboundSpeed, deflectAngle);
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        private static HandlingQualityLabel AssignHandlingLabel(float scalar)
        {
            if (scalar >= GoalkeeperConstants.CatchThreshold)   return HandlingQualityLabel.Caught;
            if (scalar >= GoalkeeperConstants.ParryThreshold)   return HandlingQualityLabel.Parried;
            if (scalar >= GoalkeeperConstants.DeflectThreshold) return HandlingQualityLabel.Deflected;
            if (scalar >= GoalkeeperConstants.MinHandlingQuality) return HandlingQualityLabel.Spilled;
            return HandlingQualityLabel.Missed;
        }

        /// <summary>Rotates a 3D vector by angleDeg around the world Z axis.</summary>
        private static Vector3 RotateVectorXY(Vector3 v, float angleRad)
        {
            float cos = Mathf.Cos(angleRad);
            float sin = Mathf.Sin(angleRad);
            return new Vector3(
                v.x * cos - v.y * sin,
                v.x * sin + v.y * cos,
                v.z);
        }

        private static float Clamp01(float v)
        {
            return v < 0.0f ? 0.0f : v > 1.0f ? 1.0f : v;
        }

        private static float Mathf_Max(float a, float b)
        {
            return a > b ? a : b;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
