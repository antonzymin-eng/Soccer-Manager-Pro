// File:     src/collision-system/CollisionResponse.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     Collision System #3 §3.3.1–§3.3.2, FR-04, FR-05, Code Standards #20
// Purpose:  Impulse-based collision resolution, penetration separation, fall/stumble triggers.

using UnityEngine;

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Computes collision response for agent-agent contacts. Static and pure.
    /// Collision System #3 §3.3.2.
    /// Δv₁ = (j / m₁) × n,  Δv₂ = -(j / m₂) × n,  j = -(1+e)·vRel / (1/m₁ + 1/m₂).
    /// </summary>
    public static class CollisionResponse
    {
        /// <summary>
        /// Calculates velocity impulses, position corrections, and fall/stumble triggers
        /// for both agents. Grounded agents act as static obstacles (receive no impulse).
        /// </summary>
        /// <param name="a1">First agent snapshot.</param>
        /// <param name="a2">Second agent snapshot.</param>
        /// <param name="manifold">Collision manifold from detection phase.</param>
        /// <param name="isSameTeam">True → apply SAME_TEAM_MOMENTUM_SCALE. §3.3.2.</param>
        /// <param name="rng">Deterministic RNG for probability sampling. FR-08.</param>
        public static AgentAgentCollisionResult CalculateAgentAgentResponse(
            in AgentPhysicalProperties a1,
            in AgentPhysicalProperties a2,
            in CollisionManifold manifold,
            bool isSameTeam,
            ref DeterministicRNG rng)
        {
            var result = new AgentAgentCollisionResult();

            bool a1Active = !a1.IsGrounded;
            bool a2Active = !a2.IsGrounded;

            if (!a1Active && !a2Active)
            {
                return result;
            }

            // Relative closing velocity along collision normal (2D projection).
            var v1 = new Vector2(a1.Velocity.x, a1.Velocity.y);
            var v2 = new Vector2(a2.Velocity.x, a2.Velocity.y);
            float vRel = Vector2.Dot(v1 - v2, manifold.Normal);

            ApplySeparation(in a1, in a2, in manifold, a1Active, a2Active, ref result);

            if (vRel > 0f)
            {
                // Agents already separating — resolve penetration only.
                return result;
            }

            float e = CollisionPhysicsConstants.CoefficientOfRestitution;
            float invM1 = a1Active ? (1.0f / a1.Mass) : 0f;
            float invM2 = a2Active ? (1.0f / a2.Mass) : 0f;
            float invMSum = invM1 + invM2;

            if (invMSum < SpatialHashConstants.MIN_DISTANCE_EPSILON)
            {
                return result;
            }

            float j = -(1f + e) * vRel / invMSum;

            if (isSameTeam)
            {
                j *= CollisionPhysicsConstants.SameTeamMomentumScale;
            }

            j = Mathf.Clamp(j,
                -CollisionPhysicsConstants.MaxImpulseMagnitude,
                 CollisionPhysicsConstants.MaxImpulseMagnitude);

            Vector2 impulse = j * manifold.Normal;

            if (a1Active)
            {
                result.VelocityImpulse1 = new Vector3(
                    impulse.x * invM1, impulse.y * invM1, 0f);
            }

            if (a2Active)
            {
                result.VelocityImpulse2 = new Vector3(
                    -impulse.x * invM2, -impulse.y * invM2, 0f);
            }

            // F = j / dt, dt = 1/60 s.
            float impactForce = Mathf.Abs(j) * 60f;
            result.ImpactForce = impactForce;

            if (a1Active)
            {
                EvaluateFallOrStumble(a1.Strength, a1.Agility, impactForce, isSameTeam, ref rng,
                    out result.TriggerGrounded1, out result.TriggerStumble1,
                    out result.GroundedDuration1);
            }

            if (a2Active)
            {
                EvaluateFallOrStumble(a2.Strength, a2.Agility, impactForce, isSameTeam, ref rng,
                    out result.TriggerGrounded2, out result.TriggerStumble2,
                    out result.GroundedDuration2);
            }

            return result;
        }

        private static void ApplySeparation(
            in AgentPhysicalProperties a1,
            in AgentPhysicalProperties a2,
            in CollisionManifold manifold,
            bool a1Active,
            bool a2Active,
            ref AgentAgentCollisionResult result)
        {
            if (manifold.PenetrationDepth <= 0f) return;

            float sep = manifold.PenetrationDepth <= FallThresholdConstants.MaxPenetrationDepth
                ? manifold.PenetrationDepth * 1.01f
                : FallThresholdConstants.MaxPenetrationDepth; // gentle correction for tunneling

            float invM1 = a1Active ? (1.0f / a1.Mass) : 0f;
            float invM2 = a2Active ? (1.0f / a2.Mass) : 0f;
            float invMSum = invM1 + invM2;

            if (invMSum < SpatialHashConstants.MIN_DISTANCE_EPSILON) return;

            Vector2 mtv = manifold.Normal * sep;

            if (a1Active)
            {
                float r1 = invM1 / invMSum;
                result.PositionCorrection1 = new Vector3(-mtv.x * r1, -mtv.y * r1, 0f);
            }

            if (a2Active)
            {
                float r2 = invM2 / invMSum;
                result.PositionCorrection2 = new Vector3(mtv.x * r2, mtv.y * r2, 0f);
            }
        }

        private static void EvaluateFallOrStumble(
            int strength,
            int agility,
            float impactForce,
            bool isSameTeam,
            ref DeterministicRNG rng,
            out bool triggerGrounded,
            out bool triggerStumble,
            out float groundedDuration)
        {
            triggerGrounded = false;
            triggerStumble = false;
            groundedDuration = 0f;

            float fallThreshold = FallThresholdConstants.FallForceBase
                + strength * FallThresholdConstants.FallForcePerStrength;
            float stumbleThreshold = fallThreshold * FallThresholdConstants.StumbleThresholdFraction;

            if (!isSameTeam && impactForce > fallThreshold)
            {
                float excess = impactForce - fallThreshold;
                float prob = Mathf.Clamp01(excess / FallThresholdConstants.FallProbabilityRange);

                if (rng.NextFloat() < prob)
                {
                    triggerGrounded = true;
                    groundedDuration = CalcGroundedDuration(agility);
                    return;
                }
            }

            if (impactForce > stumbleThreshold && impactForce <= fallThreshold)
            {
                float prob = (impactForce - stumbleThreshold) / (fallThreshold - stumbleThreshold);

                if (rng.NextFloat() < prob)
                {
                    triggerStumble = true;
                }
            }
        }

        private static float CalcGroundedDuration(int agility)
        {
            float d = GroundedDurationConstants.DurationBase
                - agility * GroundedDurationConstants.DurationPerAgility;
            return Mathf.Clamp(d,
                GroundedDurationConstants.DurationMin,
                GroundedDurationConstants.DurationMax);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
#endregion
