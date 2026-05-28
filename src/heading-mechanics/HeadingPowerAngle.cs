// File:     src/heading-mechanics/HeadingPowerAngle.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §3.5, §3.8, KD-6, KD-9, FR-HE-007, FR-HE-011, FR-HE-025, Code Standards #20
// Purpose:  Outgoing speed (FM-010-003), launch-angle reflection geometry, and own-goal-shape flag (§3.8).

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Outgoing speed and launch direction computation (§3.5 FM-010-003) and
    /// own-goal-shape trajectory flag (§3.8 KD-6 / FR-HE-007 / FR-HE-025).
    /// Fatigue convention: 0 = rested, 1 = fatigued (FR-HE-011 / KD-9).
    /// Heading Mechanics #10 §3.5 / §3.8.
    /// </summary>
    public static class HeadingPowerAngle
    {
        /// <summary>
        /// Computes outgoing speed (m/s) and outgoing velocity direction, then assembles the velocity vector.
        /// FM-010-003: outgoingSpeed = PowerMps × PowerIntent × contactQualityScalar.
        /// Launch direction from pure reflection geometry (§3.5 v0.2 H-5).
        /// Heading Mechanics #10 §3.5.
        /// </summary>
        /// <param name="attrs">Agent attribute snapshot.</param>
        /// <param name="intent">Header intent (PowerIntent locked at commit).</param>
        /// <param name="contactQualityScalar">Quality scalar from §3.4 [0, 1].</param>
        /// <param name="contactPointActual_worldspace">Actual contact point in world space (m).</param>
        /// <param name="headCentre_worldspace">Agent head centre in world space (m).</param>
        /// <param name="incomingBallVelocity">Ball velocity at the contact frame (m/s).</param>
        /// <returns>Outgoing velocity vector (m/s) in corner-origin world space.</returns>
        public static Vector3 ComputeOutgoingVelocity(
            HeadingAgentAttributes attrs,
            HeaderIntent intent,
            float contactQualityScalar,
            Vector3 contactPointActual_worldspace,
            Vector3 headCentre_worldspace,
            Vector3 incomingBallVelocity)
        {
            float outgoingSpeed = ComputeOutgoingSpeed(attrs, intent, contactQualityScalar);
            Vector3 direction   = ComputeReflectionDirection(
                contactPointActual_worldspace,
                headCentre_worldspace,
                incomingBallVelocity);

            return direction * outgoingSpeed;
        }

        /// <summary>
        /// FM-010-003: outgoingSpeed = PowerMps × PowerIntent × contactQualityScalar.
        /// PowerMps = POWER_BASE_MPS + POWER_K_STRENGTH·Strength_norm + POWER_K_HEADING·EffectiveAttribute.
        /// EffectiveAttribute = Heading_norm × (1 − POWER_FATIGUE_COEFF × fatigue). §3.5.
        /// </summary>
        public static float ComputeOutgoingSpeed(
            HeadingAgentAttributes attrs,
            HeaderIntent intent,
            float contactQualityScalar)
        {
            float headingNorm  = NormAttr(attrs.Heading);
            float strengthNorm = NormAttr(attrs.Strength);

            float effectiveAttribute = headingNorm
                * (1.0f - HeadingMechanicsConstants.PowerFatigueCoeff * attrs.Fatigue);

            float powerMps = HeadingMechanicsConstants.PowerBaseMps
                           + HeadingMechanicsConstants.PowerKStrength * strengthNorm
                           + HeadingMechanicsConstants.PowerKHeading  * effectiveAttribute;

            return powerMps * intent.PowerIntent * contactQualityScalar;
        }

        /// <summary>
        /// Computes the outgoing ball direction using pure reflection geometry (§3.5 v0.2 H-5).
        /// incident = -normalize(incomingBallVelocity).
        /// normal = normalize(contactPointActual - headCentre).
        /// reflectedDir = 2·dot(incident, normal)·normal − incident.
        /// Falls back to forward-facing unit vector when geometry is degenerate.
        /// Heading Mechanics #10 §3.5.
        /// </summary>
        public static Vector3 ComputeReflectionDirection(
            Vector3 contactPointActual_worldspace,
            Vector3 headCentre_worldspace,
            Vector3 incomingBallVelocity)
        {
            if (incomingBallVelocity.sqrMagnitude < HeadingMechanicsConstants.DEGENERACY_EPSILON_SQ)
            {
                return Vector3.forward;
            }

            Vector3 incident = -incomingBallVelocity.normalized;

            Vector3 surfaceDelta = contactPointActual_worldspace - headCentre_worldspace;
            if (surfaceDelta.sqrMagnitude < HeadingMechanicsConstants.SURFACE_NORMAL_EPSILON_SQ)
            {
                return incident;
            }

            Vector3 normal      = surfaceDelta.normalized;
            float   dotProduct  = Vector3.Dot(incident, normal);
            Vector3 reflected   = HeadingMechanicsConstants.REFLECTION_FORMULA_COEFF * dotProduct * normal - incident;

            if (reflected.sqrMagnitude < HeadingMechanicsConstants.SURFACE_NORMAL_EPSILON_SQ)
            {
                return incident;
            }

            return reflected.normalized;
        }

        /// <summary>
        /// Determines whether the outgoing trajectory has an own-goal-shaped path (§3.8 / KD-6 / FR-HE-007 / FR-HE-025).
        /// Uses dual horizon: min(OWN_GOAL_PROJECTION_HORIZON_S, OWN_GOAL_PROJECTION_HORIZON_M) per FR-HE-025.
        /// Returns true when the ballistic trajectory intersects the agent's own-goal bounding box.
        /// Does NOT adjudicate whether a goal was scored (KD-6).
        /// Heading Mechanics #10 §3.8.
        /// </summary>
        /// <param name="outgoingVelocity">Outgoing velocity (m/s) from ComputeOutgoingVelocity.</param>
        /// <param name="contactPosition">World-space ball position at the contact frame (m).</param>
        /// <param name="teamId">Agent's team ID (0 = defends X=0 goal; 1 = defends X=PitchLength goal).</param>
        public static bool ComputeOwnGoalFlag(
            Vector3 outgoingVelocity,
            Vector3 contactPosition,
            int teamId)
        {
            float horizonS = HeadingMechanicsConstants.OwnGoalProjectionHorizonS;
            float horizonM = HeadingMechanicsConstants.OwnGoalProjectionHorizonM;
            float stepS    = HeadingMechanicsConstants.FrameS;

            // Goal bounding box for the agent's own goal.
            float goalX          = teamId == 0
                ? 0.0f
                : HeadingMechanicsConstants.PitchLengthM;
            float goalHalfWidth  = HeadingMechanicsConstants.GoalHalfWidthM;
            float pitchCentreY   = HeadingMechanicsConstants.PitchCentreYM;
            float goalYMin       = pitchCentreY - goalHalfWidth;
            float goalYMax       = pitchCentreY + goalHalfWidth;
            float goalZMax       = HeadingMechanicsConstants.GoalHeightM;

            float goalXMin = goalX - HeadingMechanicsConstants.OwnGoalBoundingBoxDepthM;
            float goalXMax = goalX + HeadingMechanicsConstants.OwnGoalBoundingBoxDepthM;

            float arcLength = 0.0f;
            Vector3 prev    = contactPosition;

            for (float t = stepS; t <= horizonS; t += stepS)
            {
                Vector3 pos = PredictBallistic(contactPosition, outgoingVelocity, t);

                arcLength += Vector3.Distance(pos, prev);
                prev       = pos;

                if (arcLength >= horizonM)
                {
                    break;
                }

                if (pos.x >= goalXMin && pos.x <= goalXMax &&
                    pos.y >= goalYMin && pos.y <= goalYMax &&
                    pos.z >= 0.0f    && pos.z <= goalZMax)
                {
                    return true;
                }
            }

            return false;
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        private static Vector3 PredictBallistic(Vector3 origin, Vector3 velocity, float t)
        {
            return new Vector3(
                origin.x + velocity.x * t,
                origin.y + velocity.y * t,
                origin.z + velocity.z * t
                    - HeadingMechanicsConstants.KINEMATIC_HALF_COEFF * HeadingMechanicsConstants.GravityMps2 * t * t);
        }

        private static float NormAttr(int attribute)
        {
            float clamped = Mathf.Clamp(attribute,
                HeadingMechanicsConstants.ATTR_MIN,
                HeadingMechanicsConstants.ATTR_MAX);
            return clamped / HeadingMechanicsConstants.ATTR_MAX;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                                                  |
// | 1.1     | 2026-05-28 | —      | AR-1 M-4: GoalWidthM*0.5f → GoalHalfWidthM. M-5: local const GoalDepthM → constant.    |
// |         |            |        | L-1: 1e-6f → DEGENERACY_EPSILON_SQ. L-2: 1e-8f → SURFACE_NORMAL_EPSILON_SQ (×2).          |
// | 1.2     | 2026-05-28 | —      | AR-2 M-2: FrameMs/1000.0f → FrameS. M-3: PitchWidthM*0.5f → PitchCentreYM.     |
// |         |            |        | M-4: 2.0f*dot*normal → REFLECTION_FORMULA_COEFF. M-6: 0.5f*g → KINEMATIC_HALF_COEFF. |
#endregion
