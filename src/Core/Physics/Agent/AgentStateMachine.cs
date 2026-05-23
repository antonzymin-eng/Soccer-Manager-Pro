// File:     src/Core/Physics/Agent/AgentStateMachine.cs
// Created:  2026-05-22
// Modified: 2026-05-22
// Author:   —
// Spec:     Agent Movement #2 §3.1.4–§3.1.7, Code Standards #20
// Purpose:  Pure state evaluation for movement state transitions. No side effects.

using UnityEngine;

namespace TacticalDirector.Core.Physics.Agent
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
            float collisionForce = 0.0f)
        {
            if (isCollisionKnockdown && current != AgentMovementState.GROUNDED)
            {
                return AgentMovementState.GROUNDED;
            }

            switch (current)
            {
                case AgentMovementState.IDLE:
                    return EvaluateFromIdle(speed);

                case AgentMovementState.WALKING:
                    return EvaluateFromWalking(speed);

                case AgentMovementState.JOGGING:
                    return EvaluateFromJogging(speed, commandSpeed, sprintReservoir, aerobicPool);

                case AgentMovementState.SPRINTING:
                    return EvaluateFromSprinting(speed, commandSpeed, turnAngle, balance, agility, sprintReservoir);

                case AgentMovementState.DECELERATING:
                    return EvaluateFromDecelerating(speed, commandSpeed, turnAngle, balance, agility, sprintReservoir, aerobicPool);

                case AgentMovementState.STUMBLING:
                    return EvaluateFromStumbling(speed, dwellTimer, balance);

                case AgentMovementState.GROUNDED:
                    return EvaluateFromGrounded(dwellTimer, balance, strength);

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
            float difficulty = 1.5f;
            float stumbleRisk = (speed / MovementThresholds.MAX_SPEED)
                              * (turnAngle / 180.0f)
                              * difficulty;

            stumbleRisk = Mathf.Max(stumbleRisk, MovementThresholds.MIN_STUMBLE_RISK);

            float resistance = (agility + balance) / 40.0f;

            return stumbleRisk > resistance;
        }

        private static AgentMovementState EvaluateFromIdle(float speed)
        {
            if (speed > MovementThresholds.IDLE_EXIT)
            {
                return AgentMovementState.WALKING;
            }

            return AgentMovementState.IDLE;
        }

        private static AgentMovementState EvaluateFromWalking(float speed)
        {
            if (speed < MovementThresholds.IDLE_ENTER)
            {
                return AgentMovementState.IDLE;
            }

            if (speed > MovementThresholds.JOG_ENTER)
            {
                return AgentMovementState.JOGGING;
            }

            return AgentMovementState.WALKING;
        }

        private static AgentMovementState EvaluateFromJogging(
            float speed, float commandSpeed, float sprintReservoir, float aerobicPool)
        {
            if (aerobicPool < MovementThresholds.AEROBIC_JOG_FLOOR)
            {
                return AgentMovementState.DECELERATING;
            }

            if (speed < MovementThresholds.JOG_EXIT)
            {
                return AgentMovementState.WALKING;
            }

            if (speed > MovementThresholds.SPRINT_ENTER
                && sprintReservoir >= MovementThresholds.SPRINT_RESERVOIR_REENTRY)
            {
                return AgentMovementState.SPRINTING;
            }

            if (commandSpeed < speed - 0.5f)
            {
                return AgentMovementState.DECELERATING;
            }

            return AgentMovementState.JOGGING;
        }

        private static AgentMovementState EvaluateFromSprinting(
            float speed, float commandSpeed, float turnAngle,
            int balance, int agility, float sprintReservoir)
        {
            if (sprintReservoir < MovementThresholds.SPRINT_RESERVOIR_FLOOR)
            {
                return AgentMovementState.JOGGING;
            }

            if (speed < MovementThresholds.SPRINT_EXIT)
            {
                return AgentMovementState.JOGGING;
            }

            if (turnAngle > MovementThresholds.STUMBLE_TURN_ANGLE
                && speed > MovementThresholds.STUMBLE_SPEED_THRESHOLD
                && ShouldStumble(speed, turnAngle, balance, agility))
            {
                return AgentMovementState.STUMBLING;
            }

            if (commandSpeed < speed - 0.5f)
            {
                return AgentMovementState.DECELERATING;
            }

            return AgentMovementState.SPRINTING;
        }

        private static AgentMovementState EvaluateFromDecelerating(
            float speed, float commandSpeed, float turnAngle,
            int balance, int agility, float sprintReservoir, float aerobicPool)
        {
            if (speed < MovementThresholds.IDLE_ENTER)
            {
                return AgentMovementState.IDLE;
            }

            if (speed < MovementThresholds.JOG_EXIT)
            {
                return AgentMovementState.WALKING;
            }

            if (commandSpeed > speed + 0.5f)
            {
                if (speed > MovementThresholds.SPRINT_ENTER
                    && sprintReservoir >= MovementThresholds.SPRINT_RESERVOIR_REENTRY)
                {
                    return AgentMovementState.SPRINTING;
                }

                if (aerobicPool >= MovementThresholds.AEROBIC_JOG_FLOOR)
                {
                    return AgentMovementState.JOGGING;
                }
            }

            if (turnAngle > MovementThresholds.STUMBLE_TURN_ANGLE
                && speed > MovementThresholds.STUMBLE_SPEED_THRESHOLD
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

            if (speed < MovementThresholds.IDLE_ENTER)
            {
                return AgentMovementState.IDLE;
            }

            if (speed < MovementThresholds.JOG_EXIT)
            {
                return AgentMovementState.WALKING;
            }

            return AgentMovementState.JOGGING;
        }

        private static AgentMovementState EvaluateFromGrounded(
            float dwellTimer, int balance, int strength)
        {
            float requiredDwell = CalculateGroundedDwell(balance, strength);

            if (dwellTimer < requiredDwell)
            {
                return AgentMovementState.GROUNDED;
            }

            return AgentMovementState.IDLE;
        }

        /// <summary>
        /// Minimum dwell in STUMBLING. Higher Balance = faster recovery.
        /// Formula: BASE / (balance / 20.0), clamped [0.3, 1.5]s. Agent Movement #2 §3.1.5.
        /// </summary>
        public static float CalculateStumbleDwell(int balance)
        {
            float balanceFactor = Mathf.Max(balance / 20.0f, 0.05f);
            float dwell = MovementThresholds.STUMBLE_MIN_DWELL_BASE / balanceFactor;
            return Mathf.Clamp(dwell, 0.3f, 1.5f);
        }

        /// <summary>
        /// Minimum dwell in GROUNDED. Scaled by strength + balance and collision force.
        /// Clamped [0.5, 2.5]s. Agent Movement #2 §3.1.5.
        /// </summary>
        public static float CalculateGroundedDwell(
            int balance,
            int strength,
            GroundedReason reason = GroundedReason.COLLISION,
            float collisionForce = 1.0f)
        {
            float reasonMultiplier = reason switch
            {
                GroundedReason.COLLISION => 1.0f,
                GroundedReason.SLIDING_TACKLE => 0.6f,
                GroundedReason.DIVING_HEADER => 0.7f,
                _ => 1.0f
            };

            float combinedFactor = Mathf.Max((strength + balance) / 40.0f, 0.05f);
            float dwell = (MovementThresholds.GROUNDED_MIN_DWELL_BASE * reasonMultiplier)
                         / combinedFactor;

            if (reason == GroundedReason.COLLISION)
            {
                float forceScale = MovementThresholds.COLLISION_DWELL_MIN
                                 + (1.0f - MovementThresholds.COLLISION_DWELL_MIN)
                                 * Mathf.Clamp01(collisionForce);
                dwell *= forceScale;
            }

            return Mathf.Clamp(dwell, 0.5f, 2.5f);
        }
    }

    /// <summary>
    /// Detects state machine oscillation and locks state for a cooldown period.
    /// Uses a fixed-size ring buffer — no heap allocations after initialisation.
    /// Agent Movement #2 §3.1.7.
    /// </summary>
    public struct OscillationGuard
    {
        private const int BufferSize = 8;
        private const float LockDuration = 0.5f;
        private const float WindowSeconds = 1.0f;

        private float _t0, _t1, _t2, _t3, _t4, _t5, _t6, _t7;
        private int _writeIndex;
        private bool _isLocked;
        private float _lockUntilTime;

        /// <summary>
        /// Records a transition and returns true if the transition should be BLOCKED.
        /// Agent Movement #2 §3.1.7.
        /// </summary>
        public bool RecordAndCheck(float currentTime)
        {
            if (_isLocked && currentTime < _lockUntilTime)
            {
                return true;
            }

            _isLocked = false;

            WriteTime(_writeIndex, currentTime);
            _writeIndex = (_writeIndex + 1) % BufferSize;

            int recentCount = 0;
            for (int i = 0; i < BufferSize; i++)
            {
                if (currentTime - ReadTime(i) < WindowSeconds)
                {
                    recentCount++;
                }
            }

            if (recentCount > MovementThresholds.MAX_TRANSITIONS_PER_SECOND)
            {
                _isLocked = true;
                _lockUntilTime = currentTime + LockDuration;
                return true;
            }

            return false;
        }

        private void WriteTime(int index, float value)
        {
            switch (index)
            {
                case 0: _t0 = value; break;
                case 1: _t1 = value; break;
                case 2: _t2 = value; break;
                case 3: _t3 = value; break;
                case 4: _t4 = value; break;
                case 5: _t5 = value; break;
                case 6: _t6 = value; break;
                case 7: _t7 = value; break;
            }
        }

        private float ReadTime(int index)
        {
            switch (index)
            {
                case 0: return _t0;
                case 1: return _t1;
                case 2: return _t2;
                case 3: return _t3;
                case 4: return _t4;
                case 5: return _t5;
                case 6: return _t6;
                case 7: return _t7;
                default: return 0.0f;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-22 | —      | Initial implementation. |
#endregion
