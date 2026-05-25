// File:     src/agent-movement/AgentMovementSystem.cs
// Created:  2026-05-22
// Modified: 2026-05-25
// Author:   —
// Spec:     Agent Movement #2 §4.4, Code Standards #20
// Purpose:  Per-frame pipeline (60 Hz) that sequences all locomotion steps for one agent.

using UnityEngine;
using UnityEngine.Profiling;

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Orchestrates the 12-step per-agent per-frame pipeline described in §4.4.1.
    /// Injected with physics tick rate for deterministic time; no static mutable state.
    /// Agent Movement #2 §4.4.
    /// </summary>
    public sealed class AgentMovementSystem
    {
        private static readonly ProfilerMarker s_updateMarker =
            new ProfilerMarker("AgentMovement.Update");

        private static readonly ProfilerMarker s_updateAllAgentsMarker =
            new ProfilerMarker("AgentMovement.UpdateAllAgents");

        private readonly float _physicsHz;

        /// <summary>
        /// Constructs the system.
        /// physicsHz: physics tick rate (normally 60 Hz). Injected for testability.
        /// TODO: default should reference ProjectConstants.PHYSICS_TICK_HZ once Stage 1 sets up project-constants assembly.
        /// </summary>
        public AgentMovementSystem(float physicsHz = 60.0f) // TODO: replace with ProjectConstants.PHYSICS_TICK_HZ (Stage 1)
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
            float currentTime,
            bool isCollisionKnockdown = false,
            float collisionForce = 0.0f)
        {
            using var _ = s_updateMarker.Auto();

            // Step 1 — command is already received as parameter.

            // Step 2 — Evaluate new state.
            // commandSpeed: map the AI's desired locomotion state to an equivalent speed so the
            // state machine can compare against current speed. Dividing distance by physics dt
            // produces a value 60× too large relative to actual m/s thresholds (§3.1.4).
            float commandSpeed = CommandSpeedFromDesiredState(command.DesiredState);
            Vector2 movementDir = state.Velocity.sqrMagnitude > SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON
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

            // Step 3 — Apply transition if changed. Gate through oscillation guard (§3.1.7).
            if (newState != state.CurrentState)
            {
                bool blocked = state.OscillationGuard.RecordAndCheck(currentTime);
                if (!blocked)
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
                    // Spec §3.1.6 / §3.2.4: WALKING uses linear accel/decel, not the exponential curve.
                    newSpeed = Mathf.MoveTowards(state.Speed, topSpeed, LocomotionConstants.WALK_ACCELERATION * dt);
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
                    newSpeed = AgentLocomotion.ApplyDeceleration(
                        state.Speed, MovementThresholds.StumbleDecelerationDistance, dt);
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
                state.Position,
                state.Velocity,
                maxTurnRate * dt);

            // Step 8 — Integrate velocity (direction × new speed).
            Vector2 desiredDir = command.TargetPosition - state.Position;
            if (desiredDir.sqrMagnitude < SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON)
            {
                desiredDir = state.FacingDirection;
            }

            state.Velocity = desiredDir.normalized * newSpeed;

            // Step 9 — Integrate position.
            state.Position += state.Velocity * dt;

            // Step 10 — Safety validation (skipped when OverrideSafetyConstraints is set by tooling).
            if (!command.OverrideSafetyConstraints)
            {
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
            }
            else
            {
                // Step 11 (override path) — Update caches; last-valid tracks current unconditionally.
                state.Speed = state.Velocity.magnitude;
                state.LastValidPosition = state.Position;
                state.LastValidVelocity = state.Velocity;
            }

            // Step 12 — Fatigue update.
            UpdateFatigue(ref state, dt);
        }

        /// <summary>
        /// Advances all agents sequentially (0–21). Goalkeepers are skipped (handled by Spec #11).
        /// Collision knockdown signals from Spec #3 are passed per-agent via isCollisionKnockdown
        /// and collisionForces arrays. Agent Movement #2 §4.4.2 / §4.4.4.
        /// </summary>
        public void UpdateAllAgents(
            AgentState[] states,
            PlayerAttributes[] attrs,
            PerformanceContext[] perfs,
            MovementCommand[] commands,
            bool[] isGoalkeeper,
            bool[] isCollisionKnockdown,
            float[] collisionForces,
            float dt,
            float currentTime)
        {
            using var _ = s_updateAllAgentsMarker.Auto();

            // All arrays must be co-sized — caller contract (§4.4.2).
            // Debug.Assert compiles out in release builds (zero hot-path cost).
            Debug.Assert(attrs.Length == states.Length && perfs.Length == states.Length
                && commands.Length == states.Length && isGoalkeeper.Length == states.Length
                && isCollisionKnockdown.Length == states.Length && collisionForces.Length == states.Length,
                "UpdateAllAgents: all arrays must have the same length as states.");

            for (int i = 0; i < states.Length; i++)
            {
                if (isGoalkeeper[i])
                {
                    continue;
                }

                Update(ref states[i], attrs[i], perfs[i], commands[i], dt, currentTime,
                       isCollisionKnockdown[i], collisionForces[i]);
            }
        }

        private static float CalculateRequestedTurnAngle(Vector2 facing, Vector2 toTarget)
        {
            if (toTarget.sqrMagnitude < SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON)
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
            Vector2 agentPosition,
            Vector2 agentVelocity,
            float maxTurnDeg)
        {
            Vector2 targetDir;
            if (command.FacingMode == FacingMode.TARGET_LOCK)
            {
                targetDir = command.FacingTarget - agentPosition;
            }
            else
            {
                // AUTO_ALIGN: track actual velocity direction (§3.3.4 — "facing auto-aligns to
                // movement velocity direction"). Fall back to target vector when nearly stopped.
                targetDir = agentVelocity.sqrMagnitude > SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON
                    ? agentVelocity
                    : command.TargetPosition - agentPosition;
            }

            return AgentDirectionalMovement.RotateFacingToward(currentFacing, targetDir, maxTurnDeg);
        }

        // Maps AI desired-state intent to an equivalent m/s speed for state-machine comparisons.
        // The state machine uses commandSpeed vs current speed to decide when to decelerate or re-accelerate.
        // We map each desired state to the upper bound of its valid speed range so that:
        //   - JOGGING desired → commandSpeed=SprintEnter: agent in JOGGING range never triggers decel.
        //   - WALKING desired → commandSpeed=JogEnter: jogging agent (>JogEnter) triggers decel.
        //   - IDLE/DECELERATING desired → 0: always triggers decel from any locomotion state.
        private static float CommandSpeedFromDesiredState(AgentMovementState desiredState)
        {
            switch (desiredState)
            {
                case AgentMovementState.SPRINTING:
                    return MovementThresholds.MAX_SPEED;

                case AgentMovementState.JOGGING:
                    return MovementThresholds.SprintEnter;

                case AgentMovementState.WALKING:
                    return MovementThresholds.JogEnter;

                default: // IDLE, DECELERATING, STUMBLING, GROUNDED — no voluntary locomotion
                    return 0.0f;
            }
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
                    state.SprintReservoir += FatigueRates.SprintRecoveryIdle * dt;
                    state.AerobicPool += FatigueRates.AerobicRecoveryIdle * dt;
                    break;

                case AgentMovementState.GROUNDED:
                    // §3.1.3 table: GROUNDED recovers sprint at WALKING rate; aerobic recovers at WALKING rate.
                    state.SprintReservoir += FatigueRates.SprintRecoveryWalking * dt;
                    state.AerobicPool += FatigueRates.AerobicRecoveryWalking * dt;
                    break;

                case AgentMovementState.STUMBLING:
                    // §3.1.3 table: STUMBLING recovers sprint at WALKING rate; aerobic drains at JOGGING rate
                    // (agent is still moving involuntarily, so aerobic exertion continues).
                    state.SprintReservoir += FatigueRates.SprintRecoveryWalking * dt;
                    state.AerobicPool -= FatigueRates.AerobicDrainJogging * dt;
                    break;

                case AgentMovementState.DECELERATING:
                    // §3.1.3 table: DECELERATING recovers sprint at JOGGING rate; aerobic drains at JOGGING rate.
                    state.SprintReservoir += FatigueRates.SprintRecoveryJogging * dt;
                    state.AerobicPool -= FatigueRates.AerobicDrainJogging * dt;
                    break;
            }

            state.SprintReservoir = Mathf.Clamp01(state.SprintReservoir);
            state.AerobicPool = Mathf.Clamp01(state.AerobicPool);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                           |
// | 1.0     | 2026-05-22 | —      | Initial implementation.                                                                         |
// | 1.1     | 2026-05-25 | —      | Pass-1 fix: H-1 UpdateFacing direction; H-2 namespace; L-4 OverrideSafetyConstraints;          |
// |         |            |        | M-3 UpdateAllAgents collision arrays; M-5 StumbleDecelerationDistance; L-5 fatigue comment.    |
// | 1.2     | 2026-05-25 | —      | Pass-2 fix: L-1 STUMBLING/DECELERATING fatigue comments clarified (controlled vs involuntary).  |
// | 1.3     | 2026-05-25 | —      | Pass-4 fixes: H-1 WALKING → linear Mathf.MoveTowards (was exponential); H-2 commandSpeed →     |
// |         |            |        | CommandSpeedFromDesiredState() (was dividing distance by dt, 60× too large); H-3 OscillationGuard |
// |         |            |        | gating integrated into state transition; M-1 DECELERATING/STUMBLING fatigue updates added;      |
// |         |            |        | M-2 IDLE/GROUNDED split (GROUNDED now uses SprintRecoveryWalking); M-5 Debug.Assert co-size    |
// |         |            |        | guard; M-6 profiler marker string corrected; M-7 AUTO_ALIGN uses velocity direction;            |
// |         |            |        | L-1 1e-6f → SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON.                                   |
#endregion
