// File:     src/shot-mechanics/BodyMechanicsEvaluator.cs
// Created:  2026-05-27
// Modified: 2026-06-12
// Author:   —
// Spec:     Shot Mechanics #6 §3.7, Code Standards #20
// Purpose:  Computes BodyMechanicsScore [0, 1] from run-up angle, plant foot offset,
//           agent velocity, and body lean. Also determines stumble trigger condition.
//           Pure static calculation; no side effects.

using UnityEngine;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Evaluates physical shot stance quality and outputs BodyMechanicsScore.
    /// Pure static calculation — no side effects, no mutable state.
    /// Shot Mechanics #6 §3.7.
    /// </summary>
    public static class BodyMechanicsEvaluator
    {
        /// <summary>
        /// Evaluates body mechanics for a shot attempt. §3.7.
        /// </summary>
        /// <param name="agentVelocity">Agent velocity at shot initiation (m/s).</param>
        /// <param name="agentPosition">Agent position at shot initiation (world space).</param>
        /// <param name="ballPosition">Ball position at shot initiation (world space).</param>
        /// <param name="toGoalDirection">Unit vector from agent toward goal centre (XY plane).</param>
        /// <param name="powerIntent">Shot power intent [0, 1]; used for stumble check.</param>
        public static BodyMechanicsResult Evaluate(
            Vector3 agentVelocity,
            Vector3 agentPosition,
            Vector3 ballPosition,
            Vector3 toGoalDirection,
            float   powerIntent)
        {
            float runUpScore    = ComputeRunUpScore(agentVelocity, toGoalDirection);
            float plantScore    = ComputePlantFootScore(agentPosition, ballPosition);
            float velocityScore = ComputeVelocityScore(agentVelocity.magnitude);
            float leanScore     = ComputeLeanScore(agentVelocity.magnitude);

            float bms = ShotMechanicsConstants.WeightRunUp    * runUpScore
                      + ShotMechanicsConstants.WeightPlant    * plantScore
                      + ShotMechanicsConstants.WeightVelocity * velocityScore
                      + ShotMechanicsConstants.WeightLean     * leanScore;

            bms = Mathf.Clamp01(bms);

            float cqm = ShotMechanicsConstants.ContactQualityModifierMin
                        + bms * (ShotMechanicsConstants.ContactQualityModifierMax
                                 - ShotMechanicsConstants.ContactQualityModifierMin);

            bool stumble = bms < ShotMechanicsConstants.StumbleThreshold
                        && powerIntent > ShotMechanicsConstants.StumblePowerThreshold;

            return new BodyMechanicsResult
            {
                Score                 = bms,
                ContactQualityModifier = cqm,
                StumbleTriggered      = stumble
            };
        }

        /// <summary>
        /// §3.7.3 — Run-up angle score. Measures angular deviation of agent velocity from ideal approach.
        /// Ideal: IdealRunUpAngle (37.5°, off the goal bearing) relative to goal direction.
        /// Linear ramp: 1.0 at zero deviation, decreasing to 0.0 at RunUpTolerance (45°) of deviation.
        /// Boundary checks (§3.7.3): dev=0°→1.0, dev=22.5°→0.5, dev≥45°→0.0 (incl. a 90° approach,
        /// whose 52.5° deviation clamps to 0).
        /// </summary>
        private static float ComputeRunUpScore(Vector3 agentVelocity, Vector3 toGoalDirection)
        {
            float speed = agentVelocity.magnitude;
            if (speed < ShotMechanicsConstants.StationarySpeedThreshold)
                return ShotMechanicsConstants.StationaryRunUpScore; // stationary: neutral score

            Vector3 velDir           = agentVelocity / speed;
            float   approachAngleDeg = Vector3.Angle(velDir, toGoalDirection);
            float   deviation        = Mathf.Abs(approachAngleDeg - ShotMechanicsConstants.IdealRunUpAngle);
            // §3.7.3 linear ramp: RunUpScore = 1 − Clamp01(dev / RUN_UP_TOLERANCE).
            // (The prior deadband form 1 − max(0, dev−tol)/tol held full score for dev ≤ 45°,
            //  contradicting the §3.7.3 boundary checks — a 90° approach scored 0.83, not 0.)
            return 1.0f - Mathf.Clamp01(deviation / ShotMechanicsConstants.RunUpTolerance);
        }

        /// <summary>
        /// §3.7.4 — Plant foot score. Approximated from horizontal distance between agent and ball.
        /// Within PlantFootTolerance: full score. Beyond: linearly decreases.
        /// </summary>
        private static float ComputePlantFootScore(Vector3 agentPosition, Vector3 ballPosition)
        {
            float lateralDist = Vector2.Distance(
                new Vector2(agentPosition.x, agentPosition.y),
                new Vector2(ballPosition.x,  ballPosition.y));
            return Mathf.Clamp01(1.0f - Mathf.Max(0, lateralDist - ShotMechanicsConstants.PlantFootTolerance)
                                        / ShotMechanicsConstants.PlantFootTolerance);
        }

        /// <summary>
        /// §3.7.5 — Velocity score. Full score within [VelocityIdealMin, VelocityIdealMax].
        /// Penalty for too slow (stationary) or too fast (sprinting full pace).
        /// </summary>
        private static float ComputeVelocityScore(float speed)
        {
            if (speed < ShotMechanicsConstants.VelocityIdealMin)
            {
                float deficit = ShotMechanicsConstants.VelocityIdealMin - speed;
                return Mathf.Clamp01(1.0f - deficit / ShotMechanicsConstants.VelocityPenaltyScaleNegative);
            }
            if (speed > ShotMechanicsConstants.VelocityIdealMax)
            {
                float excess = speed - ShotMechanicsConstants.VelocityIdealMax;
                return Mathf.Clamp01(1.0f - excess / ShotMechanicsConstants.VelocityPenaltyScalePositive);
            }
            return 1.0f;
        }

        /// <summary>
        /// §3.7.6 — Body lean score. Derived from velocity magnitude (Stage 0 approximation; §4.3.2).
        /// Lean within LeanTolerance: full score. Beyond: decreases.
        /// </summary>
        private static float ComputeLeanScore(float speed)
        {
            // Stage 0: LeanTolerance == BodyLeanMaxDeg (both 20°), so leanDeg ≤ LeanTolerance always → always 1.0.
            // WeightLean (0.25) is a dead-weight contributor at Stage 0; activates when §4.3.2 native lean is wired. §3.7.6.
            float leanDeg = ShotLaunchAngleCalculator.DeriveBodyLeanAngle(speed);
            return Mathf.Clamp01(1.0f - Mathf.Max(0, leanDeg - ShotMechanicsConstants.LeanTolerance)
                                        / ShotMechanicsConstants.LeanTolerance);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-05-27 | —      | Initial implementation.                                          |
// | 1.1     | 2026-05-28 | —      | H-1: BodyMechanicsResult extracted to BodyMechanicsResult.cs.    |
// | 1.2     | 2026-05-28 | —      | M-2: ComputeRunUpScore formula fixed: was 0 at 1× tolerance;              |
// |         |            |        |   now full score within tolerance, 0 at 2× (matches §3.7.3).          |
// | 1.3     | 2026-05-28 | —      | L-1: ComputeLeanScore: comment documents Stage 0 always-1.0 behaviour     |
// |         |            |        |   (LeanTolerance == BodyLeanMaxDeg == 20°) and dead-weight WeightLean.  |
// | 1.4     | 2026-05-28 | —      | L-2: 0.1f stationary threshold → StationarySpeedThreshold constant.        |
// | 1.5     | 2026-05-28 | —      | L-3: 0.5f stationary neutral run-up score → StationaryRunUpScore constant.  |
// | 1.6     | 2026-06-12 | —      | Dotnet-CI quarantine adjudication (PRODUCTION-DEFECT, BM-002): ComputeRunUpScore |
// |         |            |        |   reverted from the v1.2 deadband 1−max(0,dev−tol)/tol (full score for ALL dev  |
// |         |            |        |   ≤ 45°) to the §3.7.3 normative linear ramp 1−Clamp01(dev/tol). The v1.2 note   |
// |         |            |        |   "matches §3.7.3" was wrong: §3.7.3's boundary checks (dev=22.5→0.5, dev=45→0,  |
// |         |            |        |   90° approach→0) describe a ramp, not a deadband; the deadband scored a 90°     |
// |         |            |        |   approach 0.83 (composite 0.958) instead of 0. No constant changes.            |
#endregion
