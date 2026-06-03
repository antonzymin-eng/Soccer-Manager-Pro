// File:     src/agent-movement/AgentTurning.cs
// Created:  2026-05-22
// Modified: 2026-06-03
// Author:   —
// Spec:     Agent Movement #2 §3.4, Code Standards #20
// Purpose:  Turn rate and lean angle calculations. All static, no side effects.

using UnityEngine;

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Speed-dependent turning model. Returns rates and animation outputs; no RNG.
    /// Stumble decisions are made deterministically by AgentStateMachine.ShouldStumble.
    /// Agent Movement #2 §3.4.
    /// </summary>
    public static class AgentTurning
    {
        /// <summary>
        /// Maximum turn rate (°/s) available this frame given speed and attributes.
        /// Formula: TURN_RATE_BASE / (1 + kTurn × speed) × balanceMod × stateMod.
        /// Agent Movement #2 §3.4.2.
        /// </summary>
        public static float CalculateMaxTurnRate(
            float speed,
            int agility,
            int balance,
            AgentMovementState state)
        {
            float kTurn = TurnConstants.KTurnMax
                        - (agility - PlayerAttributeConstants.AttributeMinInt) * TurnConstants.KTurnPerPoint;
            kTurn = Mathf.Clamp(kTurn, TurnConstants.KTurnMin, TurnConstants.KTurnMax);

            float balanceMod = TurnConstants.BalanceModMin
                             + (balance - PlayerAttributeConstants.AttributeMinInt) * TurnConstants.BalanceModPerPoint;
            balanceMod = Mathf.Clamp(balanceMod, TurnConstants.BalanceModMin, TurnConstants.BalanceModMax);

            float stateMod = StateModifier(state);

            float rate = TurnConstants.TURN_RATE_BASE / (TurnConstants.TURN_RATE_VELOCITY_OFFSET + kTurn * speed)
                       * balanceMod * stateMod;

            return Mathf.Clamp(rate, TurnConstants.TURN_RATE_FLOOR, TurnConstants.TURN_RATE_CAP);
        }

        /// <summary>
        /// Minimum turn radius (m) at a given speed and turn rate.
        /// r = v / ω_rad. Agent Movement #2 §3.4.3.
        /// </summary>
        public static float MinimumTurnRadius(float speedMs, float maxTurnRateDeg)
        {
            if (maxTurnRateDeg < TurnConstants.TURN_RATE_EPSILON_DEG)
            {
                return float.MaxValue;
            }

            float omegaRad = maxTurnRateDeg * Mathf.Deg2Rad;
            return speedMs / omegaRad;
        }

        /// <summary>
        /// Signed lean angle (degrees) as a read-only output for animation.
        /// Proportional to centripetal acceleration, magnitude clamped to MAX_LEAN_ANGLE.
        /// Sign of signedTurnRateDeg is preserved so left/right turn lean can be distinguished
        /// downstream (positive = counter-clockwise rotation in Unity XY plane).
        /// Agent Movement #2 §3.4.
        /// </summary>
        public static float CalculateLeanAngle(float speedMs, float signedTurnRateDeg)
        {
            float omegaRad = signedTurnRateDeg * Mathf.Deg2Rad;
            float centripetalAccel = speedMs * omegaRad;

            float normalizedG = centripetalAccel / TurnConstants.GRAVITY_MAGNITUDE;
            float leanRad = Mathf.Atan(normalizedG);
            float leanDeg = leanRad * Mathf.Rad2Deg;
            return Mathf.Clamp(leanDeg, -TurnConstants.MAX_LEAN_ANGLE, TurnConstants.MAX_LEAN_ANGLE);
        }

        private static float StateModifier(AgentMovementState state)
        {
            switch (state)
            {
                case AgentMovementState.DECELERATING:
                    return TurnConstants.DecelTurnModifier;

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
// | Version | Date       | Author | Notes                                                                           |
// | 1.0     | 2026-05-22 | —      | Initial implementation.                                                         |
// | 1.1     | 2026-05-25 | —      | Pass-1: H-2 namespace; L-1 PascalCase refs.                                      |
// | 1.2     | 2026-05-25 | —      | Pass-3: integer 1 literals in CalculateMaxTurnRate → (int)PlayerAttributeConstants.AttributeMin. |
// | 1.3     | 2026-05-25 | —      | Pass-4 fix: H-5 Physics.gravity.magnitude → TurnConstants.GRAVITY_MAGNITUDE [FIXED];            |
// |         |            |        | L-1 1e-4f → TurnConstants.TURN_RATE_EPSILON_DEG; L-2 1.0f divisor guard →                    |
// |         |            |        | TurnConstants.MIN_TURN_RATE_DIVISOR; L-3 1.0f denominator offset →                           |
// |         |            |        | TurnConstants.TURN_RATE_VELOCITY_OFFSET; L-4 (int)AttributeMin → AttributeMinInt.             |
// | 1.4     | 2026-05-26 | —      | AR-2 fix: H-2 CalculateStumbleProbability removed (dead code; system uses                       |
// |         |            |        | AgentStateMachine.ShouldStumble deterministically; no RNG roll existed at Step 6b).            |
// |         |            |        | Class summary updated to remove incorrect RNG-roll reference.                                   |
// | 1.5     | 2026-06-03 | —      | AR-4 fix: L-1 CalculateLeanAngle accepts signed turn rate; returns signed lean clamped to       |
// |         |            |        | ±MAX_LEAN_ANGLE so animation can distinguish left vs right lean direction.                     |
#endregion
