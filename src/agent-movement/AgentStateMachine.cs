// File:     src/agent-movement/AgentStateMachine.cs
// Created:  2026-05-22
// Modified: 2026-06-03 (AR-9 fix pass)
// Author:   —
// Spec:     Agent Movement #2 §3.1.4–§3.1.7, Code Standards #20
// Purpose:  Pure state evaluation for movement state transitions. No side effects.

using UnityEngine;

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Stateless evaluator for AgentMovementState transitions.
    /// Called once per physics frame (60 Hz) before locomotion formulas.
    /// All methods are static and pure — no state is modified here.
    /// Agent Movement #2 §3.1.5.
    /// </summary>
    public static class AgentStateMachine
    {
        /// <summary>
        /// Evaluates the next movement state from current conditions.
        /// Returns the new state; caller applies the transition to the Agent.
        /// Collision knockdown has highest priority and overrides all other logic.
        /// groundedReason and collisionForce are only meaningful when current == GROUNDED.
        /// Agent Movement #2 §3.1.5.
        /// </summary>
        public static AgentMovementState EvaluateState(
            AgentMovementState current,
            float speed,
            float commandSpeed,
            float turnAngle,
            float dwellTimer,
            int balance,
            int agility,
            int strength,
            float sprintReservoir,
            float aerobicPool,
            bool isCollisionKnockdown,
            float collisionForce = 0.0f,
            GroundedReason groundedReason = GroundedReason.NONE)
        {
            // Boundary assert mirroring PerformanceContext.EvaluateAttribute (AR-7 L-1):
            // raw player attributes are integers in [1, 20] per Spec #2 §3.5.1. `default(PlayerAttributes)`
            // leaves all fields at 0, which would propagate negative `(attr - AttributeMinInt)` factors
            // into downstream formulas — they range-clamp defensively, but the assert catches the
            // upstream contract violation in development builds.
            Debug.Assert(balance >= PlayerAttributeConstants.AttributeMinInt
                         && balance <= PlayerAttributeConstants.AttributeMaxInt,
                "EvaluateState: balance must be in [1, 20] per Spec #2 §3.5.1.");
            Debug.Assert(agility >= PlayerAttributeConstants.AttributeMinInt
                         && agility <= PlayerAttributeConstants.AttributeMaxInt,
                "EvaluateState: agility must be in [1, 20] per Spec #2 §3.5.1.");
            Debug.Assert(strength >= PlayerAttributeConstants.AttributeMinInt
                         && strength <= PlayerAttributeConstants.AttributeMaxInt,
                "EvaluateState: strength must be in [1, 20] per Spec #2 §3.5.1.");

            // Knockdown signal unconditionally forces GROUNDED. The prior `current != GROUNDED`
            // guard created a Case-2 gap: when the GROUNDED dwell expired on the same frame a
            // fresh collision arrived, EvaluateFromGrounded returned IDLE, the transition cleared
            // GroundedReason/CollisionForce, and only the NEXT frame re-grounded the agent —
            // leaving a single IDLE frame in the middle of a continuous knockdown sequence. The
            // System Step 3 newState==current==GROUNDED branch now refreshes the entry state
            // (AR-5 M-2 fix in AgentMovementSystem.cs).
            if (isCollisionKnockdown)
            {
                return AgentMovementState.GROUNDED;
            }

            switch (current)
            {
                case AgentMovementState.IDLE:
                    return EvaluateFromIdle(speed);

                case AgentMovementState.WALKING:
                    return EvaluateFromWalking(speed, commandSpeed);

                case AgentMovementState.JOGGING:
                    return EvaluateFromJogging(speed, commandSpeed, sprintReservoir, aerobicPool);

                case AgentMovementState.SPRINTING:
                    return EvaluateFromSprinting(speed, commandSpeed, turnAngle, balance, agility, sprintReservoir, aerobicPool);

                case AgentMovementState.DECELERATING:
                    return EvaluateFromDecelerating(speed, commandSpeed, turnAngle, balance, agility, sprintReservoir, aerobicPool);

                case AgentMovementState.STUMBLING:
                    return EvaluateFromStumbling(speed, dwellTimer, balance);

                case AgentMovementState.GROUNDED:
                    return EvaluateFromGrounded(dwellTimer, balance, strength, groundedReason, collisionForce);

                default:
                    return AgentMovementState.IDLE;
            }
        }

        /// <summary>
        /// Determines whether a stumble occurs for a sharp turn at speed.
        /// Deterministic: no RNG. Given identical inputs, result is always the same.
        /// Agent Movement #2 §3.1.5.
        /// </summary>
        public static bool ShouldStumble(float speed, float turnAngle, int balance, int agility)
        {
            float stumbleRisk = (speed / MovementThresholds.MAX_SPEED)
                              * (turnAngle / TurnConstants.HALF_ROTATION_DEG)
                              * MovementThresholds.StumbleDifficultyFactor;

            stumbleRisk = Mathf.Max(stumbleRisk, MovementThresholds.MinStumbleRisk);

            float resistance = (float)(agility + balance) / PlayerAttributeConstants.AttributePairMax;

            return stumbleRisk > resistance;
        }

        private static AgentMovementState EvaluateFromIdle(float speed)
        {
            if (speed > MovementThresholds.IdleExit)
            {
                return AgentMovementState.WALKING;
            }

            return AgentMovementState.IDLE;
        }

        private static AgentMovementState EvaluateFromWalking(float speed, float commandSpeed)
        {
            if (speed < MovementThresholds.IdleEnter)
            {
                return AgentMovementState.IDLE;
            }

            if (speed > MovementThresholds.JogEnter)
            {
                return AgentMovementState.JOGGING;
            }

            // Respect deceleration intent from AI (e.g., STOP command with commandSpeed=0).
            if (commandSpeed < speed - MovementThresholds.CommandSpeedHysteresis)
            {
                return AgentMovementState.DECELERATING;
            }

            return AgentMovementState.WALKING;
        }

        private static AgentMovementState EvaluateFromJogging(
            float speed, float commandSpeed, float sprintReservoir, float aerobicPool)
        {
            if (aerobicPool < MovementThresholds.AerobicJogFloor)
            {
                return AgentMovementState.DECELERATING;
            }

            if (speed < MovementThresholds.JogExit)
            {
                return AgentMovementState.WALKING;
            }

            if (speed > MovementThresholds.SprintEnter
                && sprintReservoir >= MovementThresholds.SprintReservoirReentry)
            {
                return AgentMovementState.SPRINTING;
            }

            if (commandSpeed < speed - MovementThresholds.CommandSpeedHysteresis)
            {
                return AgentMovementState.DECELERATING;
            }

            return AgentMovementState.JOGGING;
        }

        private static AgentMovementState EvaluateFromSprinting(
            float speed, float commandSpeed, float turnAngle,
            int balance, int agility, float sprintReservoir, float aerobicPool)
        {
            if (sprintReservoir < MovementThresholds.SprintReservoirFloor)
            {
                return AgentMovementState.JOGGING;
            }

            // Aerobic floor mirrors EvaluateFromJogging's gate — an exhausted player cannot keep
            // sprinting indefinitely on a recently-refilled anaerobic reservoir.
            if (aerobicPool < MovementThresholds.AerobicJogFloor)
            {
                return AgentMovementState.JOGGING;
            }

            if (speed < MovementThresholds.SprintExit)
            {
                return AgentMovementState.JOGGING;
            }

            if (turnAngle > MovementThresholds.StumbleTurnAngle
                && speed > MovementThresholds.StumbleSpeedThreshold
                && ShouldStumble(speed, turnAngle, balance, agility))
            {
                return AgentMovementState.STUMBLING;
            }

            if (commandSpeed < speed - MovementThresholds.CommandSpeedHysteresis)
            {
                return AgentMovementState.DECELERATING;
            }

            return AgentMovementState.SPRINTING;
        }

        private static AgentMovementState EvaluateFromDecelerating(
            float speed, float commandSpeed, float turnAngle,
            int balance, int agility, float sprintReservoir, float aerobicPool)
        {
            if (speed < MovementThresholds.IdleEnter)
            {
                return AgentMovementState.IDLE;
            }

            // Fall through to WALKING only when the AI's command intent is consistent with walking
            // (commandSpeed ≥ current speed within hysteresis, i.e. caller no longer demanding
            // active deceleration). Otherwise WALKING would immediately re-evaluate to DECELERATING
            // on the next frame and the pair would flap until OscillationGuard clamps it.
            bool commandPermitsWalking =
                commandSpeed >= speed - MovementThresholds.CommandSpeedHysteresis;

            if (speed < MovementThresholds.JogExit && commandPermitsWalking)
            {
                return AgentMovementState.WALKING;
            }

            if (commandSpeed > speed + MovementThresholds.CommandSpeedHysteresis)
            {
                if (speed > MovementThresholds.SprintEnter
                    && sprintReservoir >= MovementThresholds.SprintReservoirReentry)
                {
                    return AgentMovementState.SPRINTING;
                }

                if (aerobicPool >= MovementThresholds.AerobicJogFloor)
                {
                    return AgentMovementState.JOGGING;
                }
            }

            if (turnAngle > MovementThresholds.StumbleTurnAngle
                && speed > MovementThresholds.StumbleSpeedThreshold
                && ShouldStumble(speed, turnAngle, balance, agility))
            {
                return AgentMovementState.STUMBLING;
            }

            return AgentMovementState.DECELERATING;
        }

        private static AgentMovementState EvaluateFromStumbling(
            float speed, float dwellTimer, int balance)
        {
            float requiredDwell = CalculateStumbleDwell(balance);

            if (dwellTimer < requiredDwell)
            {
                return AgentMovementState.STUMBLING;
            }

            if (speed < MovementThresholds.IdleEnter)
            {
                return AgentMovementState.IDLE;
            }

            if (speed < MovementThresholds.JogExit)
            {
                return AgentMovementState.WALKING;
            }

            return AgentMovementState.JOGGING;
        }

        private static AgentMovementState EvaluateFromGrounded(
            float dwellTimer,
            int balance,
            int strength,
            GroundedReason groundedReason,
            float collisionForce)
        {
            float requiredDwell = CalculateGroundedDwell(balance, strength, groundedReason, collisionForce);

            if (dwellTimer < requiredDwell)
            {
                return AgentMovementState.GROUNDED;
            }

            return AgentMovementState.IDLE;
        }

        /// <summary>
        /// Minimum dwell in STUMBLING. Higher Balance = faster recovery.
        /// Formula: StumbleMinDwellBase / (balance / 20.0), clamped [0.3, 1.5]s. Agent Movement #2 §3.1.5.
        /// </summary>
        public static float CalculateStumbleDwell(int balance)
        {
            float balanceFactor = Mathf.Max(
                (float)balance / PlayerAttributeConstants.AttributeMax,
                PlayerAttributeConstants.AttributeNearZeroFloor);
            float dwell = MovementThresholds.StumbleMinDwellBase / balanceFactor;
            return Mathf.Clamp(dwell, MovementThresholds.StumbleDwellClampMin, MovementThresholds.StumbleDwellClampMax);
        }

        /// <summary>
        /// Minimum dwell in GROUNDED. Scaled by strength + balance, collision force, and entry reason.
        /// `collisionForce` is the cached entry-force (`state.CollisionForce`), not this-frame's
        /// incoming impulse — the caller in EvaluateState forwards the cached value per AR-9 M-1.
        /// Clamped [0.5, 2.5]s. Agent Movement #2 §3.1.5.
        /// </summary>
        public static float CalculateGroundedDwell(
            int balance,
            int strength,
            GroundedReason reason,
            float collisionForce)
        {
            float reasonMultiplier = reason switch
            {
                GroundedReason.COLLISION => 1.0f,
                GroundedReason.SLIDING_TACKLE => MovementThresholds.SlidingTackleDwellMult,
                GroundedReason.DIVING_HEADER => MovementThresholds.DivingHeaderDwellMult,
                _ => 1.0f
            };

            float combinedFactor = Mathf.Max(
                (float)(strength + balance) / PlayerAttributeConstants.AttributePairMax,
                PlayerAttributeConstants.AttributeNearZeroFloor);
            float dwell = (MovementThresholds.GroundedMinDwellBase * reasonMultiplier)
                         / combinedFactor;

            if (reason == GroundedReason.COLLISION)
            {
                float forceScale = MovementThresholds.CollisionDwellMin
                                 + (1.0f - MovementThresholds.CollisionDwellMin)
                                 * Mathf.Clamp01(collisionForce);
                dwell *= forceScale;
            }

            return Mathf.Clamp(dwell, MovementThresholds.GroundedDwellClampMin, MovementThresholds.GroundedDwellClampMax);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                         |
// | 1.0     | 2026-05-22 | —      | Initial implementation.                                                                       |
// | 1.1     | 2026-05-25 | —      | Pass-1 fix: H-2 namespace; H-3 StumbleDifficultyFactor; L-1 PascalCase refs; M-4 hysteresis; |
// |         |            |        | M-1 OscillationGuard extracted.                                                               |
// | 1.2     | 2026-05-25 | —      | Pass-2 fix: dwell clamps / reason multipliers → named constants; M-2 denominator comments.      |
// |         |            |        | Pass-3: 20.0f/40.0f/0.05f literals → PlayerAttributeConstants.AttributeMax/PairMax/NearZeroFloor. |
// | 1.3     | 2026-05-25 | —      | Pass-4 fix: M-3 180.0f → TurnConstants.HALF_ROTATION_DEG [FIXED].                              |
// | 1.4     | 2026-05-26 | —      | AR-2 fix: H-1 groundedReason+collisionForce propagated through EvaluateState →                 |
// |         |            |        | EvaluateFromGrounded → CalculateGroundedDwell (reason multipliers and force scaling now         |
// |         |            |        | reachable from live code path). M-2 commandSpeed decel check added to EvaluateFromWalking      |
// |         |            |        | so STOP/slow commands are respected without escalating to JOGGING. L-2 explicit (float)         |
// |         |            |        | cast added in CalculateStumbleDwell and CalculateGroundedDwell int/float division.              |
// | 1.5     | 2026-05-26 | —      | AR-2 fix (continued): explicit (float) cast added in ShouldStumble for                          |
// |         |            |        | (agility + balance) / AttributePairMax (int/float division). Consistent with L-2 casts        |
// |         |            |        | applied in CalculateStumbleDwell and CalculateGroundedDwell.                                    |
// | 1.6     | 2026-06-03 | —      | AR-4 fix: M-1 EvaluateFromDecelerating no longer auto-falls to WALKING when commandSpeed       |
// |         |            |        | still demands active deceleration; closes the WALKING↔DECELERATING flap that previously       |
// |         |            |        | relied on OscillationGuard as a structural fallback. M-3 EvaluateFromSprinting gains an        |
// |         |            |        | aerobicPool < AerobicJogFloor gate symmetric with EvaluateFromJogging.                         |
// | 1.7     | 2026-06-03 | —      | AR-5 M-2 / AR-6 follow-up: collision-knockdown short-circuit no longer gated on                |
// |         |            |        | `current != GROUNDED`. With the guard, a fresh collision that arrived on the same frame the    |
// |         |            |        | prior GROUNDED dwell expired produced a one-frame IDLE flicker before re-grounding. System    |
// |         |            |        | Step 3 newState==current==GROUNDED branch now handles the refresh side.                       |
// | 1.8     | 2026-06-03 | —      | AR-8 fix: L-3 EvaluateState asserts balance / agility / strength ∈ [1, 20] mirroring the      |
// |         |            |        | PerformanceContext.EvaluateAttribute boundary assert (AR-7 L-1). default(PlayerAttributes)    |
// |         |            |        | leaves all int attribute fields at 0, which propagated negative `(attr - AttributeMinInt)`    |
// |         |            |        | factors into downstream formulas; downstream range-clamps defensively but the upstream        |
// |         |            |        | contract violation was previously silent.                                                      |
// | 1.9     | 2026-06-03 | —      | AR-9 fix: L-2 CalculateGroundedDwell parameter defaults (reason / collisionForce) dropped —   |
// |         |            |        | EvaluateFromGrounded is the only caller and always supplies explicit values. The `1.0f`       |
// |         |            |        | default for collisionForce was also misleading after the AR-9 M-1 contract change             |
// |         |            |        | (parameter is now the cached entry-force, not this-frame's impulse). XML doc updated to       |
// |         |            |        | name the new contract.                                                                         |
#endregion
