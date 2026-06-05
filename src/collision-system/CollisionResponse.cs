// File:     src/collision-system/CollisionResponse.cs
// Created:  2026-05-25
// Modified: 2026-06-05  [v1.2]
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

            // vRel <= 0 (early-return at line above), e > 0, invMSum > 0 → j >= 0 always.
            // SameTeamMomentumScale > 0, so the scaled value stays non-negative.
            // Upper clamp is therefore the only meaningful bound.
            float j = -(1f + e) * vRel / invMSum;

            if (isSameTeam)
            {
                j *= CollisionPhysicsConstants.SameTeamMomentumScale;
            }

            j = Mathf.Min(j, CollisionPhysicsConstants.MaxImpulseMagnitude);

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

            float impactForce = Mathf.Abs(j) * CollisionPhysicsConstants.PHYSICS_TICK_HZ;
            result.ImpactForce = impactForce;

            if (a1Active)
            {
                EvaluateFallOrStumble(a1.Strength, impactForce, isSameTeam, ref rng,
                    out result.TriggerGrounded1, out result.TriggerStumble1);
            }

            if (a2Active)
            {
                EvaluateFallOrStumble(a2.Strength, impactForce, isSameTeam, ref rng,
                    out result.TriggerGrounded2, out result.TriggerStumble2);
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
                ? manifold.PenetrationDepth * CollisionPhysicsConstants.SeparationSlop
                : FallThresholdConstants.MaxPenetrationDepth;

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
            float impactForce,
            bool isSameTeam,
            ref DeterministicRNG rng,
            out bool triggerGrounded,
            out bool triggerStumble)
        {
            triggerGrounded = false;
            triggerStumble = false;

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
                    return;
                }

                // Fall probability check failed — force was high enough for fall zone but agent
                // survived; treat as a stumble at maximum stumble probability (1.0).
                triggerStumble = true;
                return;
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
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                              |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                     |
// | 1.1     | 2026-05-25 | —      | M-3: EvaluateFallOrStumble now triggers stumble when force > fallThreshold but fall |
// |         |            |        | probability check fails (previously no consequence for surviving high-force hit).   |
// | 1.2     | 2026-06-05 | —      | AR-3 L-4. Impulse magnitude clamp simplified Mathf.Clamp(±M) → Mathf.Min(j, M).     |
// |         |            |        | j >= 0 invariant documented inline (guaranteed by vRel <= 0 early-return upstream). |
#endregion
