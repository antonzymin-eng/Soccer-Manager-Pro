// File:     src/Core/Physics/Agent/AgentMovementSystem.cs
// Created:  2026-05-22
// Modified: 2026-05-22
// Author:   —
// Spec:     Agent Movement #2 §4.4, Code Standards #20
// Purpose:  Per-frame pipeline (60 Hz) that sequences all locomotion steps for one agent.

using UnityEngine;
using UnityEngine.Profiling;

namespace TacticalDirector.Core.Physics.Agent
{
    /// <summary>
    /// Orchestrates the 12-step per-agent per-frame pipeline described in §4.4.1.
    /// Injected with MatchClock for deterministic time; no static mutable state.
    /// Agent Movement #2 §4.4.
    /// </summary>
    public sealed class AgentMovementSystem
    {
        private static readonly ProfilerMarker s_updateMarker =
            new ProfilerMarker("AgentMovement.Update");

        private static readonly ProfilerMarker s_updateAllAgentsMarker =
            new ProfilerMarker("AgentMovement.AllAgents");

        // Injected dependencies (constructor injection per FR-CS-051).
        private readonly float _physicsHz;

        /// <summary>
        /// Constructs the system.
        /// physicsHz: physics tick rate (normally 60). Injected for testability.
        /// </summary>
        public AgentMovementSystem(float physicsHz = 60.0f)
        {
            _physicsHz = physicsHz;
        }

        /// <summary>
        /// Advances one agent by one physics frame.
        /// All 12 pipeline steps from §4.4.1 are applied in order.
        /// Agent Movement #2 §4.4.1.
        /// </summary>
        public void Update(
            ref AgentState state,
            in PlayerAttributes attrs,
            in PerformanceContext perf,
            in MovementCommand command,
            float dt,
            bool isCollisionKnockdown = false,
            float collisionForce = 0.0f)
        {
            using var _ = s_updateMarker.Auto();

            // Step 1 — command is already received as parameter.

            // Step 2 — Evaluate new state.
            float commandSpeed = (command.TargetPosition - state.Position).magnitude / dt;
            Vector2 movementDir = state.Velocity.sqrMagnitude > 1e-6f
                ? state.Velocity.normalized
                : state.FacingDirection;

            float movementAngle = AgentDirectionalMovement.MovementAngleDeg(
                movementDir,
                state.FacingDirection);

            float turnAngleRequested = CalculateRequestedTurnAngle(
                state.FacingDirection,
                command.TargetPosition - state.Position);

            AgentMovementState newState = AgentStateMachine.EvaluateState(
                state.CurrentState,
                state.Speed,
                commandSpeed,
                turnAngleRequested,
                state.TimeInState,
                attrs.Balance,
                attrs.Agility,
                attrs.Strength,
                state.SprintReservoir,
                state.AerobicPool,
                isCollisionKnockdown,
                collisionForce);

            // Step 3 — Apply transition if changed.
            if (newState != state.CurrentState)
            {
                state.PreviousState = state.CurrentState;
                state.CurrentState = newState;
                state.TimeInState = 0.0f;

                if (isCollisionKnockdown && newState == AgentMovementState.GROUNDED)
                {
                    state.GroundedReason = GroundedReason.COLLISION;
                    state.CollisionForce = collisionForce;
                }
            }
            else
            {
                state.TimeInState += dt;
            }

            // Step 4–5 — Acceleration / deceleration with directional penalty.
            float effectivePace = perf.EvaluateAttribute(attrs.Pace);
            float effectiveAccel = perf.EvaluateAttribute(attrs.Acceleration);
            float effectiveAgility = perf.EvaluateAttribute(attrs.Agility);

            float directionalMult = IsDirectionalMultActive(state.CurrentState)
                ? AgentDirectionalMovement.CalculateDirectionalMultiplier(movementAngle, effectiveAgility)
                : 1.0f;

            float topSpeed = AgentLocomotion.CalculateBaseTopSpeed(effectivePace)
                           * directionalMult
                           * AgentLocomotion.CalculateAerobicModifier(state.AerobicPool);

            float kBase = AgentLocomotion.CalculateBaseAccelK(effectiveAccel);
            float kDir = AgentDirectionalMovement.ApplyDirectionalToAccelK(kBase, directionalMult);

            float newSpeed = state.Speed;

            switch (state.CurrentState)
            {
                case AgentMovementState.IDLE:
                    newSpeed = Mathf.MoveTowards(state.Speed, 0.0f, MovementThresholds.MAX_ACCELERATION * dt);
                    break;

                case AgentMovementState.WALKING:
                    newSpeed = AgentLocomotion.ApplyAcceleration(state.Speed, topSpeed, kDir, dt);
                    break;

                case AgentMovementState.JOGGING:
                case AgentMovementState.SPRINTING:
                    newSpeed = AgentLocomotion.ApplyAcceleration(state.Speed, topSpeed, kDir, dt);
                    break;

                case AgentMovementState.DECELERATING:
                {
                    float stopDist = AgentLocomotion.CalculateStoppingDistance(
                        command.DecelerationMode, effectivePace);
                    newSpeed = AgentLocomotion.ApplyDeceleration(state.Speed, stopDist, dt);
                    break;
                }

                case AgentMovementState.STUMBLING:
                    // Drag-only: friction decelerates without voluntary braking.
                    newSpeed = AgentLocomotion.ApplyDeceleration(state.Speed, 3.0f, dt);
                    break;

                case AgentMovementState.GROUNDED:
                    newSpeed = 0.0f;
                    break;
            }

            // Step 6 — Turn rate and lean angle.
            float maxTurnRate = AgentTurning.CalculateMaxTurnRate(
                state.Speed, attrs.Agility, attrs.Balance, state.CurrentState);

            state.LeanAngle = AgentTurning.CalculateLeanAngle(state.Speed, maxTurnRate);
            state.CurrentTurnRate = maxTurnRate;

            // Step 7 — Facing direction update.
            state.FacingDirection = UpdateFacing(
                state.FacingDirection,
                command,
                maxTurnRate * dt);

            // Step 8 — Integrate velocity (direction × new speed).
            Vector2 desiredDir = (command.TargetPosition - state.Position);
            if (desiredDir.sqrMagnitude < 1e-6f)
            {
                desiredDir = state.FacingDirection;
            }

            state.Velocity = desiredDir.normalized * newSpeed;

            // Step 9 — Integrate position.
            state.Position += state.Velocity * dt;

            // Step 10 — Safety validation.
            Vector2 pos = state.Position;
            Vector2 vel = state.Velocity;
            AgentSafetySystem.Validate(
                ref pos, ref vel,
                state.LastValidPosition, state.LastValidVelocity,
                out bool recovered);
            state.Position = pos;
            state.Velocity = vel;

            // Step 11 — Update caches.
            state.Speed = state.Velocity.magnitude;
            if (!recovered)
            {
                state.LastValidPosition = state.Position;
                state.LastValidVelocity = state.Velocity;
            }

            // Step 12 — Fatigue update.
            UpdateFatigue(ref state, dt);
        }

        /// <summary>
        /// Advances all agents sequentially (0–21). Goalkeepers are skipped (handled by Spec #11).
        /// Agent Movement #2 §4.4.2 / §4.4.4.
        /// </summary>
        public void UpdateAllAgents(
            AgentState[] states,
            PlayerAttributes[] attrs,
            PerformanceContext[] perfs,
            MovementCommand[] commands,
            bool[] isGoalkeeper,
            float dt)
        {
            using var _ = s_updateAllAgentsMarker.Auto();

            for (int i = 0; i < states.Length; i++)
            {
                if (isGoalkeeper[i])
                {
                    continue;
                }

                Update(ref states[i], attrs[i], perfs[i], commands[i], dt);
            }
        }

        private static float CalculateRequestedTurnAngle(Vector2 facing, Vector2 toTarget)
        {
            if (toTarget.sqrMagnitude < 1e-6f)
            {
                return 0.0f;
            }

            float dot = Vector2.Dot(facing.normalized, toTarget.normalized);
            dot = Mathf.Clamp(dot, -1.0f, 1.0f);
            return Mathf.Acos(dot) * Mathf.Rad2Deg;
        }

        private static Vector2 UpdateFacing(
            Vector2 currentFacing,
            in MovementCommand command,
            float maxTurnDeg)
        {
            Vector2 targetDir;

            if (command.FacingMode == FacingMode.TARGET_LOCK)
            {
                targetDir = command.FacingTarget - Vector2.zero;
            }
            else
            {
                targetDir = command.TargetPosition - Vector2.zero;
            }

            return AgentDirectionalMovement.RotateFacingToward(currentFacing, targetDir, maxTurnDeg);
        }

        private static bool IsDirectionalMultActive(AgentMovementState state)
        {
            switch (state)
            {
                case AgentMovementState.WALKING:
                case AgentMovementState.JOGGING:
                case AgentMovementState.SPRINTING:
                case AgentMovementState.DECELERATING:
                    return true;

                default:
                    return false;
            }
        }

        private static void UpdateFatigue(ref AgentState state, float dt)
        {
            switch (state.CurrentState)
            {
                case AgentMovementState.SPRINTING:
                    state.SprintReservoir -= FatigueRates.SprintDrainSprinting * dt;
                    state.AerobicPool -= FatigueRates.AerobicDrainSprinting * dt;
                    break;

                case AgentMovementState.JOGGING:
                    state.SprintReservoir += FatigueRates.SprintRecoveryJogging * dt;
                    state.AerobicPool -= FatigueRates.AerobicDrainJogging * dt;
                    break;

                case AgentMovementState.WALKING:
                    state.SprintReservoir += FatigueRates.SprintRecoveryWalking * dt;
                    state.AerobicPool += FatigueRates.AerobicRecoveryWalking * dt;
                    break;

                case AgentMovementState.IDLE:
                case AgentMovementState.GROUNDED:
                    state.SprintReservoir += FatigueRates.SprintRecoveryIdle * dt;
                    state.AerobicPool += FatigueRates.AerobicRecoveryIdle * dt;
                    break;
            }

            state.SprintReservoir = Mathf.Clamp01(state.SprintReservoir);
            state.AerobicPool = Mathf.Clamp01(state.AerobicPool);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-22 | —      | Initial implementation. |
#endregion
