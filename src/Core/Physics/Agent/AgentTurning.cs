// File:     src/Core/Physics/Agent/AgentTurning.cs
// Created:  2026-05-22
// Modified: 2026-05-22
// Author:   —
// Spec:     Agent Movement #2 §3.4, Code Standards #20
// Purpose:  Turn rate, lean angle, and stumble probability calculations.

using UnityEngine;

namespace TacticalDirector.Core.Physics.Agent
{
    /// <summary>
    /// Speed-dependent turning model. Returns probabilities and rates; does not apply RNG.
    /// The main loop owns any RNG roll for stumble (§4.4.1 Step 6b). Agent Movement #2 §3.4.
    /// </summary>
    public static class AgentTurning
    {
        /// <summary>
        /// Maximum turn rate (°/s) available this frame given speed and attributes.
        /// Formula: TURN_RATE_BASE / (1 + k_turn × speed) × balance_mod × state_mod.
        /// Agent Movement #2 §3.4.2.
        /// </summary>
        public static float CalculateMaxTurnRate(
            float speed,
            int agility,
            int balance,
            AgentMovementState state)
        {
            float kTurn = TurnConstants.K_TURN_MAX
                        - (agility - 1) * TurnConstants.K_TURN_PER_POINT;
            kTurn = Mathf.Clamp(kTurn, TurnConstants.K_TURN_MIN, TurnConstants.K_TURN_MAX);

            float balanceMod = TurnConstants.BALANCE_MOD_MIN
                             + (balance - 1) * TurnConstants.BALANCE_MOD_PER_POINT;
            balanceMod = Mathf.Clamp(balanceMod, TurnConstants.BALANCE_MOD_MIN, TurnConstants.BALANCE_MOD_MAX);

            float stateMod = StateModifier(state);

            float rate = TurnConstants.TURN_RATE_BASE / (1.0f + kTurn * speed)
                       * balanceMod * stateMod;

            return Mathf.Clamp(rate, TurnConstants.TURN_RATE_FLOOR, TurnConstants.TURN_RATE_CAP);
        }

        /// <summary>
        /// Minimum turn radius (m) at a given speed and turn rate.
        /// r = v / ω_rad. Agent Movement #2 §3.4.3.
        /// </summary>
        public static float MinimumTurnRadius(float speedMs, float maxTurnRateDeg)
        {
            if (maxTurnRateDeg < 1e-4f)
            {
                return float.MaxValue;
            }

            float omegaRad = maxTurnRateDeg * Mathf.Deg2Rad;
            return speedMs / omegaRad;
        }

        /// <summary>
        /// Lean angle (degrees) as a read-only output for animation.
        /// Proportional to centripetal acceleration, capped at MAX_LEAN_ANGLE.
        /// Agent Movement #2 §3.4.
        /// </summary>
        public static float CalculateLeanAngle(float speedMs, float turnRateDeg)
        {
            float omegaRad = turnRateDeg * Mathf.Deg2Rad;
            float centripetalAccel = speedMs * omegaRad;

            float normalizedG = centripetalAccel / Physics.gravity.magnitude;
            float leanRad = Mathf.Atan(normalizedG);
            float leanDeg = leanRad * Mathf.Rad2Deg;
            return Mathf.Min(leanDeg, TurnConstants.MAX_LEAN_ANGLE);
        }

        /// <summary>
        /// Stumble probability in [0, MAX_STUMBLE_PROB] for a requested turn that exceeds
        /// the safe fraction of max turn rate. Returns 0.0 if within safe zone.
        /// Caller (main loop §4.4.1 Step 6b) performs the actual RNG roll.
        /// Agent Movement #2 §3.4.4.
        /// </summary>
        public static float CalculateStumbleProbability(
            float requestedTurnDeg, float maxTurnRateDeg)
        {
            float safeTurnDeg = maxTurnRateDeg * TurnConstants.SAFE_TURN_FRACTION;

            if (requestedTurnDeg <= safeTurnDeg)
            {
                return 0.0f;
            }

            float overshoot = Mathf.Clamp01(
                (requestedTurnDeg - safeTurnDeg)
              / Mathf.Max(maxTurnRateDeg - safeTurnDeg, 1.0f));

            return overshoot * TurnConstants.MAX_STUMBLE_PROB;
        }

        private static float StateModifier(AgentMovementState state)
        {
            switch (state)
            {
                case AgentMovementState.DECELERATING:
                    return TurnConstants.DECEL_TURN_MODIFIER;

                case AgentMovementState.STUMBLING:
                    return 0.0f;

                case AgentMovementState.GROUNDED:
                    return 0.0f;

                default:
                    return 1.0f;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-22 | —      | Initial implementation. |
#endregion
