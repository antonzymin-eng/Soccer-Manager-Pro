// File:     src/collision-system/CollisionResponse.cs
// Created:  2026-05-25
// Modified: 2026-06-10  [v1.6]
// Author:   —
// Spec:     Collision System #3 §3.3.1–§3.3.2, FR-04, FR-05, Code Standards #20
// Purpose:  Impulse-based collision resolution, penetration separation, fall/stumble triggers.

using UnityEngine;

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Computes collision response for agent-agent contacts. Static and pure.
    /// Collision System #3 §3.3.2.
    /// With n pointing a1 → a2 and vRel = (v1 − v2)·n (&gt; 0 = approaching):
    /// j = (1+e)·vRel / (1/m₁ + 1/m₂),  Δv₁ = -(j / m₁) × n,  Δv₂ = +(j / m₂) × n.
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

            // Relative velocity along the collision normal (2D projection). Normal points
            // a1 → a2, so vRel = (v1 − v2)·n > 0 means a1 closes on a2 — APPROACHING.
            // ERR-003-005: the original gate read `vRel > 0 → separating, return`, which is
            // inverted for this normal convention — real closing collisions produced
            // separation only (no impulse, no fall/stumble), while already-separating
            // overlapped pairs had their velocities reversed back toward re-collision.
            var v1 = new Vector2(a1.Velocity.x, a1.Velocity.y);
            var v2 = new Vector2(a2.Velocity.x, a2.Velocity.y);
            float vRel = Vector2.Dot(v1 - v2, manifold.Normal);

            ApplySeparation(in a1, in a2, in manifold, a1Active, a2Active, ref result);

            if (vRel <= 0f)
            {
                // Separating or grazing — resolve penetration only.
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

            // vRel > 0 (early-return above), e > 0, invMSum > 0 → j > 0 always.
            // SameTeamMomentumScale > 0, so the scaled value stays positive.
            // Upper clamp is therefore the only meaningful bound.
            float j = (1f + e) * vRel / invMSum;

            if (isSameTeam)
            {
                j *= CollisionPhysicsConstants.SameTeamMomentumScale;
            }

            j = Mathf.Min(j, CollisionPhysicsConstants.MaxImpulseMagnitude);

            // a1 recoils against the a1 → a2 normal; a2 is pushed along it.
            Vector2 impulse = j * manifold.Normal;

            if (a1Active)
            {
                result.VelocityImpulse1 = new Vector3(
                    -impulse.x * invM1, -impulse.y * invM1, 0f);
            }

            if (a2Active)
            {
                result.VelocityImpulse2 = new Vector3(
                    impulse.x * invM2, impulse.y * invM2, 0f);
            }

            // j is non-negative by the invariant documented above (vRel <= 0 early-return).
            // Average contact force over the physical contact window (ERR-003-001) — NOT over a
            // single 16.7 ms frame, which inflated forces ~10× against the literature-calibrated
            // fall/stumble thresholds below.
            float impactForce = j / CollisionPhysicsConstants.ContactDurationS;
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

            // Monotonic clamp: apply slop first, then cap. The previous form
            // (PD <= MaxPD ? PD*Slop : MaxPD) silently dropped the slop on the
            // cap branch, so a 0.495m overlap separated by 0.500m while a 1.0m
            // overlap separated by only 0.500m — severely-overlapping pairs
            // received LESS slop and were prone to re-collide next frame.
            float sep = Mathf.Min(
                manifold.PenetrationDepth * CollisionPhysicsConstants.SeparationSlop,
                FallThresholdConstants.MaxPenetrationDepth);

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

            // Clamp01 keeps the branch monotonic for same-team contacts above fallThreshold
            // (the fall branch is opposing-team only) — previously such hits escaped both
            // branches and the hardest same-team collisions were consequence-free (ERR-003-003).
            if (impactForce > stumbleThreshold)
            {
                float prob = Mathf.Clamp01(
                    (impactForce - stumbleThreshold) / (fallThreshold - stumbleThreshold));

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
// | 1.3     | 2026-06-05 | —      | AR-4 M-3. ApplySeparation sep clamp made monotonic — was                            |
// |         |            |        | `PD <= MaxPD ? PD*Slop : MaxPD` (silently dropped slop on cap branch); now          |
// |         |            |        | `Mathf.Min(PD*Slop, MaxPD)`. Severely-overlapping pairs no longer receive less      |
// |         |            |        | separation than mildly-overlapping pairs.                                           |
// | 1.4     | 2026-06-05 | —      | AR-5 L-3. impactForce = Mathf.Abs(j) * PHYSICS_TICK_HZ simplified to                |
// |         |            |        | j * PHYSICS_TICK_HZ — j is non-negative by the AR-3 L-4 invariant.                 |
// | 1.5     | 2026-06-10 | —      | AR-7 H-1 (ERR-003-001): impactForce = j / ContactDurationS — the j × 60 Hz form    |
// |         |            |        | assumed the whole impulse acts in one frame, inflating force ~10× and making every |
// |         |            |        | opposing contact above ~0.5 m/s closing speed a guaranteed knockdown.              |
// |         |            |        | AR-7 M-2 (ERR-003-003): stumble branch drops the `<= fallThreshold` gate and       |
// |         |            |        | clamps prob to 1 — same-team hits above fallThreshold no longer escape both        |
// |         |            |        | branches (hardest same-team collisions were consequence-free).                     |
// | 1.6     | 2026-06-10 | —      | AR-8 H-1 (ERR-003-005): approach gate un-inverted. With the a1→a2 normal,          |
// |         |            |        | vRel = (v1−v2)·n > 0 is APPROACHING; the old `vRel > 0 → return` gave real closing |
// |         |            |        | collisions separation-only (no impulse, no fall/stumble — EvaluateFallOrStumble    |
// |         |            |        | was unreachable for genuine contacts) and reversed separating pairs back inward.   |
// |         |            |        | j = +(1+e)·vRel/invMSum (j > 0 invariant preserved); Δv1 = −j·n/m1, Δv2 = +j·n/m2. |
// |         |            |        | Spec §3.3 Step 2/Step 4 pseudocode carried the same inversion; patched same commit.|
#endregion
