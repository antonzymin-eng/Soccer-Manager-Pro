// File:     src/agent-movement/AgentMovementSystem.cs
// Created:  2026-05-22
// Modified: 2026-11-21 (W13-F5/F6/F7 / ERR-002-001: §3.5.3.1 tracking command — arrival braking + target-velocity feed-forward)
// Modified (prior): 2026-06-12
// Author:   —
// Spec:     Agent Movement #2 §4.4, §3.5.3.1, ERR-002-001, Code Standards #20
// Purpose:  Per-frame pipeline (60 Hz) that sequences all locomotion steps for one agent.

using System;
using UnityEngine;
using Unity.Profiling;

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
            // physicsHz must be finite and positive — 0 / NaN / negative would silently disable
            // the dt-fidelity assert in Update (1.5f / 0 = +Infinity, 1.5f / NaN = NaN, etc.)
            // and let stalled-loop frames through the gate. Caught once at construction.
            Debug.Assert(physicsHz > 0.0f && !float.IsNaN(physicsHz),
                "AgentMovementSystem: physicsHz must be finite and positive.");
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
            bool isCollisionKnockdown,
            float collisionForce,
            bool accelerateMovingTrack = false)
        {
            using var _ = s_updateMarker.Auto();

            // dt should be ≤ 1.5× expected frame duration. Beyond that, the upstream loop has
            // stalled and per-frame integration loses fidelity; treat as a configuration bug.
            Debug.Assert(dt > 0.0f && dt <= 1.5f / _physicsHz,
                "AgentMovementSystem.Update: dt outside expected range for physicsHz (>1.5× frame).");

            // currentTime must come from MatchClock (Spec #16 §3.2.3). NaN, ±Infinity, or
            // negative inputs silently break OscillationGuard window math (NaN comparisons
            // are always false, +Infinity makes the window unconditionally include every
            // historical slot, -Infinity flips the comparison). `float.IsFinite` catches
            // NaN and both infinities in a single call.
            Debug.Assert(float.IsFinite(currentTime) && currentTime >= 0.0f,
                "AgentMovementSystem.Update: currentTime must be finite and non-negative (MatchClock contract).");

            // Step 1 — command is already received as parameter.

            // Step 2 — Evaluate new state. commandSpeed maps the AI's desired locomotion state to
            // a threshold-equivalent speed in m/s so the state machine can compare against the
            // agent's current speed directly (§3.1.4).
            float commandSpeed = CommandSpeedFromDesiredState(command.DesiredState);

            // §3.5.3.1 tracking command (W13-F5/F6/F7): the desired velocity is the tracked point's velocity plus an
            // arrival term that can always brake to the (extrapolated) point; its magnitude lowers the band ceiling.
            // Every non-tracking command skips this block, so its behaviour is the prior behaviour exactly.
            Vector2 trackDesiredVelocity = Vector2.zero;
            if (command.IsTracking)
            {
                trackDesiredVelocity = CalculateTrackDesiredVelocity(in command, state.Position, currentTime);
                commandSpeed = Mathf.Min(commandSpeed, trackDesiredVelocity.magnitude);
            }

            // Aerobic exhaustion degrades command intent (AR-13 M-1). Below AerobicJogFloor
            // the state machine refuses JOGGING/SPRINTING, so an above-walking commandSpeed
            // would push speed through JogEnter and flap WALKING→JOGGING→DECELERATING at
            // ~3 Hz until the OscillationGuard locks. Clamping intent to the walking band
            // the agent can sustain keeps both the state machine (Step 2) and the speed
            // integrator (Step 4–5 topSpeed cap) consistent. Pool recovery near the floor
            // produces a slow walk/jog alternation on pool timescales (seconds) — accepted
            // as exhausted-player behaviour, and far below the guard's transitions-per-
            // second threshold.
            if (state.AerobicPool < MovementThresholds.AerobicJogFloor)
            {
                commandSpeed = Mathf.Min(commandSpeed, MovementThresholds.JogEnter);
            }

            Vector2 movementDir = state.Velocity.sqrMagnitude > SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON
                ? state.Velocity.normalized
                : state.FacingDirection;

            float movementAngle = AgentDirectionalMovement.MovementAngleDeg(
                movementDir,
                state.FacingDirection);

            // A tracking command steers along its desired velocity (which may point away from a point the agent has passed),
            // not at the stored destination.
            Vector2 commandOffset = command.IsTracking
                ? trackDesiredVelocity
                : command.TargetPosition - state.Position;

            // Movement intent requires BOTH a moving commandSpeed and a non-degenerate target
            // offset (AR-13 M-2). The Decision Tree's HOLD action issues StrafeWhileWatching
            // with TargetPosition == current position (desired JOGGING, facing-locked on the
            // ball) — without the offset gate, the H-1 launch path would feed newSpeed > 0
            // into Step 8 with a degenerate target from rest, tripping RotateVelocityToward's
            // both-degenerate Debug.Assert every frame while the agent (correctly) holds still.
            bool hasMovementIntent = commandSpeed > 0.0f
                && commandOffset.sqrMagnitude > SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON;

            float turnAngleRequested = CalculateRequestedTurnAngle(
                state.FacingDirection,
                commandOffset);

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
                // Pass the cached entry-force (set on initial knockdown, refreshed on second
                // hit per AR-5 M-2) instead of the incoming this-frame collisionForce. The
                // incoming value is 0 on every dwell frame after entry (collision is a
                // one-frame impulse from Spec #3); using it would shrink the perceived dwell
                // 35% for a max-force hit and release the agent prematurely. On the entry
                // frame the cached value is still 0 but EvaluateState short-circuits on
                // isCollisionKnockdown before reaching EvaluateFromGrounded, so the
                // pre-cache-write value is unused.
                state.CollisionForce,
                state.GroundedReason);

            // Step 3 — Apply transition if changed. Gate through oscillation guard (§3.1.7).
            // Collision-driven GROUNDED transitions bypass the guard: an external knockdown
            // impulse is not state-machine flapping, and blocking it would keep the agent in
            // motion for LockDuration after a collision. The guard timestamp is also not
            // recorded for the bypass case so the history reflects normal-flow flapping only.
            if (newState != state.CurrentState)
            {
                bool isCollisionTransition =
                    isCollisionKnockdown && newState == AgentMovementState.GROUNDED;
                bool blocked = !isCollisionTransition
                    && state.OscillationGuard.RecordAndCheck(currentTime);
                if (!blocked)
                {
                    state.PreviousState = state.CurrentState;
                    state.CurrentState = newState;
                    state.TimeInState = 0.0f;

                    if (isCollisionTransition)
                    {
                        state.GroundedReason = GroundedReason.COLLISION;
                        // SanitiseCollisionForce enforces the [0, 1] doc contract on
                        // CollisionForce (AgentState.cs §3.5.1) AND filters out NaN —
                        // raw Mathf.Clamp01(NaN) returns NaN (both `<0` and `>1`
                        // comparisons against NaN are false), which would poison
                        // downstream CalculateGroundedDwell and cache consumers.
                        state.CollisionForce = SanitiseCollisionForce(collisionForce);

                        // A collision is a structural break in normal locomotion; any prior
                        // flap-history is irrelevant. Reset the guard so the post-recovery
                        // GROUNDED→IDLE transition is not blocked by a stale lock that was
                        // engaged before the collision arrived. Without this, a designer who
                        // tunes LockDuration above GroundedDwellClampMin silently extends
                        // collision recovery by the remaining lock window.
                        state.OscillationGuard.Initialize();
                    }
                    else if (state.PreviousState == AgentMovementState.GROUNDED)
                    {
                        // Exiting GROUNDED: restore the sentinel so the field's invariant
                        // ("NONE when CurrentState != GROUNDED") holds for the rest of the match.
                        state.GroundedReason = GroundedReason.NONE;
                        state.CollisionForce = 0.0f;
                    }
                }
                else
                {
                    state.TimeInState += dt;
                }
            }
            else
            {
                // A fresh collision impulse while already GROUNDED re-captures the entry
                // reason/force and resets the dwell timer so the second hit extends recovery.
                // Without this, the dwell continues against the first hit's CollisionForce only
                // and the second impulse is silently dropped (§3.1.5). The
                // `state.CurrentState == GROUNDED` clause is technically implied by
                // `isCollisionKnockdown` (the AR-6 M-1 unconditional short-circuit in
                // EvaluateState forces newState=GROUNDED on knockdown, and we are in the
                // newState==current branch) but kept as a belt-and-braces against future
                // EvaluateState changes.
                if (isCollisionKnockdown && state.CurrentState == AgentMovementState.GROUNDED)
                {
                    state.GroundedReason = GroundedReason.COLLISION;
                    // SanitiseCollisionForce mirrors the cache write in the outer transition
                    // branch — keeps CollisionForce ∈ [0, 1] and rejects NaN regardless of
                    // whether the second hit lands during a state transition or while already
                    // GROUNDED.
                    state.CollisionForce = SanitiseCollisionForce(collisionForce);
                    state.TimeInState = 0.0f;
                }
                else
                {
                    state.TimeInState += dt;
                }
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

            // Command intent caps the integration target (AR-12 H-2). Without the cap every
            // moving agent accelerated toward its pace-derived ceiling (~7.5–10.2 m/s),
            // overshot its commanded band, and the state machine promoted it — a JOGGING
            // command crossed SprintEnter into SPRINTING (draining the reservoir), a WALKING
            // command crossed JogEnter, and both flap-cycled through DECELERATING
            // indefinitely. The exponential/linear approaches never exceed topSpeed (see
            // ApplyAcceleration asymptote ceiling), so the strict band-promotion comparisons
            // (speed > JogEnter / SprintEnter) stay false unless the AI requested the band.
            float physicalTopSpeed = topSpeed;
            topSpeed = Mathf.Min(topSpeed, commandSpeed);

            float kBase = AgentLocomotion.CalculateBaseAccelK(effectiveAccel);
            float kDir = AgentDirectionalMovement.ApplyDirectionalToAccelK(kBase, directionalMult);

            // Tight markers need to catch the sampled velocity before entering the jogging band.
            // Ordinary tracking and stationary-target arrivals use the linear walk rate.
            float minTrackSpeed = MovementThresholds.MIN_VELOCITY_MAGNITUDE;
            bool accelerateToMovingTrack = accelerateMovingTrack && command.IsTracking
                && currentTime - command.IssueTimeS <= LocomotionConstants.TRACK_EXTRAPOLATION_MAX_S
                && command.TargetVelocity.sqrMagnitude > minTrackSpeed * minTrackSpeed
                && topSpeed > state.Speed;
            float newSpeed = state.Speed;

            switch (state.CurrentState)
            {
                case AgentMovementState.IDLE:
                    // Launch path (AR-12 H-1): the state machine exits IDLE only on
                    // speed > IdleExit, but this branch previously only decayed speed toward
                    // zero — an agent at rest given a moving command could never start moving
                    // (newSpeed stayed 0, Step 8 zeroed velocity, EvaluateFromIdle never
                    // fired). With movement intent (moving commandSpeed AND a non-degenerate
                    // target offset — AR-13 M-2), accelerate at the walking rate until the
                    // IDLE→WALKING transition takes over; otherwise decay as before.
                    if (hasMovementIntent && accelerateToMovingTrack)
                        newSpeed = TrackRunningSpeed(state.Speed, physicalTopSpeed, topSpeed, kDir, dt);
                    else
                        newSpeed = hasMovementIntent
                            ? Mathf.MoveTowards(state.Speed, topSpeed, LocomotionConstants.WALK_ACCELERATION * dt)
                            : Mathf.MoveTowards(state.Speed, 0.0f, MovementThresholds.MAX_ACCELERATION * dt);
                    break;

                case AgentMovementState.WALKING:
                {
                    // §3.5.3.1 supplies the moving-track acceleration exception to the linear walk rates.
                    float walkRate = state.Speed > topSpeed
                        ? LocomotionConstants.WALK_DECELERATION
                        : LocomotionConstants.WALK_ACCELERATION;
                    newSpeed = accelerateToMovingTrack
                        ? TrackRunningSpeed(state.Speed, physicalTopSpeed, topSpeed, kDir, dt)
                        : Mathf.MoveTowards(state.Speed, topSpeed, walkRate * dt);
                    break;
                }

                case AgentMovementState.JOGGING:
                case AgentMovementState.SPRINTING:
                    newSpeed = command.IsTracking
                        ? TrackRunningSpeed(state.Speed, physicalTopSpeed, topSpeed, kDir, dt)
                        : AgentLocomotion.ApplyAcceleration(state.Speed, topSpeed, kDir, dt);
                    break;

                case AgentMovementState.DECELERATING:
                {
                    float stopDist = AgentLocomotion.CalculateStoppingDistance(
                        command.DecelerationMode, effectivePace);
                    newSpeed = AgentLocomotion.ApplyDeceleration(state.Speed, stopDist, dt);
                    // §3.5.3.1: a tracking agent brakes to the tracked speed, not to rest, and never accelerates while
                    // DECELERATING (the state machine re-enters JOGGING once the command exceeds speed + hysteresis).
                    if (command.IsTracking)
                        newSpeed = Mathf.Max(newSpeed, Mathf.Min(state.Speed, topSpeed));
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

            // Step 6 — Turn rate (max available this frame). Lean angle is computed in Step 8
            // from the velocity-direction rotation actually applied, so its magnitude reflects
            // real path curvature (centripetal load), not facing rotation.
            float maxTurnRate = AgentTurning.CalculateMaxTurnRate(
                state.Speed, attrs.Agility, attrs.Balance, state.CurrentState);

            // Step 7 — Facing direction update; capture signed angle actually applied for the
            // achieved turn-rate cache. NOTE (AR-12 L-3): AUTO_ALIGN tracks the pre-Step-8
            // velocity, so facing lags the velocity it aligns to by exactly one frame at 60 Hz.
            // The §4.4.1 step order (facing before velocity integration) is preserved
            // deliberately; the lag is bounded and invisible at frame rate.
            state.FacingDirection = UpdateFacing(
                state.FacingDirection,
                command,
                state.Position,
                state.Velocity,
                maxTurnRate * dt,
                out float signedFacingAngle);

            float signedTurnRate = dt > 0.0f ? signedFacingAngle / dt : 0.0f;
            state.CurrentTurnRate = Mathf.Abs(signedTurnRate);

            // Step 8 — Integrate velocity with rate-limited direction rotation (momentum-respecting).
            // Voluntary states (WALKING / JOGGING / SPRINTING / DECELERATING with steering intent)
            // rotate velocity toward command.TargetPosition at maxTurnRate. Non-voluntary states
            // (STUMBLING / GROUNDED) and explicit stop intent (DesiredState == IDLE) maintain
            // current velocity direction so momentum is preserved instead of teleported.
            bool voluntarySteering = IsVoluntarySteeringActive(state.CurrentState, command.DesiredState);
            Vector2 velocityTarget = voluntarySteering
                ? commandOffset
                : state.Velocity;
            float velocityMaxTurnDeg = voluntarySteering ? maxTurnRate * dt : 0.0f;

            state.Velocity = AgentDirectionalMovement.RotateVelocityToward(
                state.Velocity, velocityTarget, newSpeed, velocityMaxTurnDeg,
                out float velocitySignedAngle);

            // Lean reflects actual path curvature (AR-12 M-1): centripetal load comes from
            // the velocity-direction rotation applied here in Step 8, not the facing rotation
            // in Step 7 — a TARGET_LOCK strafe curves the path while facing holds (lean was
            // ~0 when it should peak), and an in-place facing pivot at residual speed showed
            // phantom lean. The jump-start / maintain-momentum paths report 0 → lean 0.
            float velocityTurnRate = dt > 0.0f ? velocitySignedAngle / dt : 0.0f;
            state.LeanAngle = AgentTurning.CalculateLeanAngle(newSpeed, velocityTurnRate);

            // Step 9 — Integrate position.
            state.Position += state.Velocity * dt;

            // Step 10 — Safety validation (skipped when OverrideSafetyConstraints is set by tooling).
            if (!command.OverrideSafetyConstraints)
            {
                Vector2 pos = state.Position;
                Vector2 vel = state.Velocity;
                Vector2 facing = state.FacingDirection;
                AgentSafetySystem.Validate(
                    ref pos, ref vel, ref facing,
                    state.LastValidPosition, state.LastValidVelocity, state.LastValidFacing,
                    out bool recovered);
                state.Position = pos;
                state.Velocity = vel;
                state.FacingDirection = facing;

                // Step 11 — Update caches.
                state.Speed = state.Velocity.magnitude;
                if (!recovered)
                {
                    state.LastValidPosition = state.Position;
                    state.LastValidVelocity = state.Velocity;
                    state.LastValidFacing = state.FacingDirection;
                }
            }
            else
            {
                // Step 11 (override path) — Update caches and Speed, but only when values are
                // finite and facing is non-degenerate. Override mode is tooling-only (replay
                // scrubber / editor injection); a tool that injects NaN/Inf and then disables
                // override on the next frame would otherwise poison LastValid* permanently —
                // Validate would restore NaN values, repeat the corruption next frame, and the
                // agent would be stuck. Speed is gated on the same validity check so a NaN
                // velocity does not propagate a NaN Speed into the next frame's state-machine
                // evaluation (where comparisons against NaN silently return false and produce
                // arbitrary transitions).
                if (!AgentSafetySystem.HasInvalidValues(
                        state.Position, state.Velocity, state.FacingDirection))
                {
                    state.Speed = state.Velocity.magnitude;
                    state.LastValidPosition = state.Position;
                    state.LastValidVelocity = state.Velocity;
                    state.LastValidFacing = state.FacingDirection;
                }
                // else: preserve prior frame's Speed / LastValid* so neither NaN propagates.
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
            float currentTime,
            ReadOnlySpan<bool> accelerateMovingTracks = default)
        {
            using var _ = s_updateAllAgentsMarker.Auto();

            // No array may be null — caller contract (§4.4.2). Checked separately from the length
            // assert below so the diagnostic identifies the missing array instead of an opaque NRE
            // when the length-check expression dereferences a null .Length.
            Debug.Assert(
                states != null && attrs != null && perfs != null && commands != null
                && isGoalkeeper != null && isCollisionKnockdown != null && collisionForces != null,
                "UpdateAllAgents: no array argument may be null.");

            // All arrays must be co-sized — caller contract (§4.4.2).
            // Debug.Assert compiles out in release builds (zero hot-path cost).
            Debug.Assert(attrs.Length == states.Length && perfs.Length == states.Length
                && commands.Length == states.Length && isGoalkeeper.Length == states.Length
                && isCollisionKnockdown.Length == states.Length && collisionForces.Length == states.Length
                && (accelerateMovingTracks.IsEmpty || accelerateMovingTracks.Length == states.Length),
                "UpdateAllAgents: all arrays must have the same length as states.");

            for (int i = 0; i < states.Length; i++)
            {
                if (isGoalkeeper[i])
                {
                    continue;
                }

                Update(ref states[i], attrs[i], perfs[i], commands[i], dt, currentTime,
                       isCollisionKnockdown[i], collisionForces[i],
                       !accelerateMovingTracks.IsEmpty && accelerateMovingTracks[i]);
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
            float maxTurnDeg,
            out float signedAngleApplied)
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

            return AgentDirectionalMovement.RotateFacingToward(
                currentFacing, targetDir, maxTurnDeg, out signedAngleApplied);
        }

        // Maps AI desired-state intent to an equivalent m/s speed for state-machine comparisons.
        // The state machine uses commandSpeed vs current speed to decide when to decelerate or re-accelerate.
        // We map each desired state to the upper bound of its valid speed range so that:
        //   - JOGGING desired → commandSpeed=SprintEnter: agent in JOGGING range never triggers decel.
        //   - WALKING desired → commandSpeed=JogEnter: jogging agent (>JogEnter) triggers decel.
        //   - IDLE/DECELERATING desired → 0: always triggers decel from any locomotion state.
        /// <summary>
        /// §3.5.3.1 tracking law. The point is extrapolated by its velocity over the command's age
        /// τ = clamp(currentTime − IssueTimeS, 0, <see cref="LocomotionConstants.TRACK_EXTRAPOLATION_MAX_S"/>):
        /// p* = TargetPosition + TargetVelocity·τ. With e = p* − position and d = |e|, the arrival speed is
        /// v_arr = √(2·a·max(0, d − d_tol)) — the largest speed from which braking at a = <see cref="LocomotionConstants.MinDecelerationFloor"/>
        /// (2.5 m/s², the controlled floor) still stops within the remaining distance — with the settle tolerance
        /// d_tol = <see cref="LocomotionConstants.MinStoppingDistanceM"/> (0.1 m). Desired velocity = v_ff + v_arr·e/d, where the
        /// feed-forward v_ff is TargetVelocity while τ is within the limit and zero after it (an unrefreshed command freezes).
        /// Worked example: stationary point 1.0 m ahead ⇒ v_arr = √(2·2.5·0.9) = 2.12 m/s toward it; a point 0.5 m ahead
        /// moving at 3 m/s along the same line ⇒ 3 + √(2·2.5·0.4) = 4.41 m/s. Pure; zero allocation.
        /// </summary>
        internal static Vector2 CalculateTrackDesiredVelocity(in MovementCommand command, Vector2 position, float currentTime)
        {
            float age = currentTime - command.IssueTimeS;
            bool fresh = age <= LocomotionConstants.TRACK_EXTRAPOLATION_MAX_S;
            float tau = Mathf.Clamp(age, 0.0f, LocomotionConstants.TRACK_EXTRAPOLATION_MAX_S);
            Vector2 point = command.TargetPosition + command.TargetVelocity * tau;
            Vector2 feedForward = fresh ? command.TargetVelocity : Vector2.zero;

            Vector2 e = point - position;
            float d = e.magnitude;
            float slack = d - LocomotionConstants.MinStoppingDistanceM;
            if (slack <= 0.0f || d <= MovementThresholds.MIN_VELOCITY_MAGNITUDE)
                return feedForward;

            float arrive = Mathf.Sqrt(LocomotionConstants.KINEMATIC_HALF * LocomotionConstants.MinDecelerationFloor * slack);
            return feedForward + e * (arrive / d);
        }

        /// <summary>
        /// §3.5.3.1: speed update in JOGGING / SPRINTING for a tracking command. Below the tracked speed the agent accelerates on
        /// his PHYSICAL curve (toward <paramref name="physicalTopSpeed"/>, the pace / directional / aerobic ceiling) and is clamped
        /// at the tracked speed — a requested speed limits the speed reached, not the acceleration available to reach it. Above
        /// it he brakes at the controlled floor (<see cref="LocomotionConstants.MinDecelerationFloor"/>), the rate the arrival
        /// term assumes, down to the tracked speed.
        /// </summary>
        private static float TrackRunningSpeed(float speed, float physicalTopSpeed, float trackedSpeed, float k, float dt)
        {
            if (speed <= trackedSpeed)
                return Mathf.Min(AgentLocomotion.ApplyAcceleration(speed, physicalTopSpeed, k, dt), trackedSpeed);
            return Mathf.Max(speed - LocomotionConstants.MinDecelerationFloor * dt, trackedSpeed);
        }

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

        // Voluntary steering is permitted only when the current state allows it AND the AI's
        // desired locomotion intent is itself a moving state. STUMBLING / GROUNDED are always
        // non-voluntary by §3.1.2 (momentum-only / knocked down). A DesiredState of IDLE means
        // "stop here" — the AI has no directional preference, so velocity decays along the
        // existing momentum vector rather than reversing toward a stale captured TargetPosition.
        private static bool IsVoluntarySteeringActive(
            AgentMovementState current, AgentMovementState desired)
        {
            switch (current)
            {
                case AgentMovementState.STUMBLING:
                case AgentMovementState.GROUNDED:
                    return false;
            }

            switch (desired)
            {
                case AgentMovementState.IDLE:
                case AgentMovementState.STUMBLING:
                case AgentMovementState.GROUNDED:
                    return false;
            }

            return true;
        }

        // Clamps collisionForce to [0, 1] AND maps NaN → 0. Plain Mathf.Clamp01(NaN) returns
        // NaN (Unity's implementation is `value < 0 ? 0 : value > 1 ? 1 : value`; both halves
        // of the ternary are false for NaN, so NaN passes through). A poisoned cache propagates
        // through CalculateGroundedDwell (`forceScale = … + … * Clamp01(NaN) = NaN`, then
        // `dwell *= NaN = NaN`, then `Mathf.Clamp(NaN, min, max) = NaN`), so the
        // `dwellTimer < requiredDwell` gate returns false on the next frame and the agent
        // releases prematurely. Treating NaN as 0 force degrades to the minimum-impulse path
        // (forceScale = CollisionDwellMin) which is the safe-recovery default.
        private static float SanitiseCollisionForce(float collisionForce)
        {
            if (!float.IsFinite(collisionForce))
            {
                return 0.0f;
            }
            return Mathf.Clamp01(collisionForce);
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
// | 1.4     | 2026-05-26 | —      | AR-2 fix: H-3 Debug.Assert added in Update() consuming _physicsHz for dt range validation.     |
// |         |            |        | H-1 state.GroundedReason passed to EvaluateState so EvaluateFromGrounded receives actual        |
// |         |            |        | entry reason and collision force (reason multipliers and force scaling now live).               |
// | 1.5     | 2026-05-26 | —      | AR-2 fix (continued): M-3 WALKING case now uses WALK_DECELERATION when speed > topSpeed and    |
// |         |            |        | WALK_ACCELERATION when accelerating, matching §3.2.4 separate-rate intent (was using only       |
// |         |            |        | WALK_ACCELERATION for both directions). WALK_DECELERATION constant is now live.                |
// | 1.6     | 2026-05-26 | —      | AR-3 fix: R3-M-1 state.GroundedReason / CollisionForce reset to NONE/0 when leaving GROUNDED.  |
// |         |            |        | Restores field invariant "GroundedReason == NONE when CurrentState != GROUNDED" documented in   |
// |         |            |        | AgentState. Without this, reason/force fields retained stale COLLISION values indefinitely.     |
// | 1.7     | 2026-06-03 | —      | AR-4 fix: H-1/H-2/H-3/M-6 Step 8 now uses AgentDirectionalMovement.RotateVelocityToward with    |
// |         |            |        | a rate-limited rotation (≤ maxTurnRate*dt). Voluntary steering only in WALKING/JOGGING/         |
// |         |            |        | SPRINTING/DECELERATING current states and only when DesiredState is itself moving. STUMBLING   |
// |         |            |        | / GROUNDED / DesiredState==IDLE fall through to momentum-direction; closes the                 |
// |         |            |        | momentum-teleport defect and the Stop-command stale-target reversal. H-4 CurrentTurnRate is    |
// |         |            |        | now the actually-achieved rate (|signedAngle|/dt) not max-possible. L-1 LeanAngle carries     |
// |         |            |        | sign from facing rotation. M-5 Validate signature gains facing in/out + LastValidFacing.       |
// |         |            |        | L-3 isCollisionKnockdown / collisionForce are required parameters. L-4 dt assert tightened     |
// |         |            |        | from 2.0/physicsHz to 1.5/physicsHz. L-2 stale "60× too large" comment removed.                |
// | 1.8     | 2026-06-03 | —      | AR-5 fix: M-1 override-path Step 11 cache write now skipped when current values are            |
// |         |            |        | invalid (HasInvalidValues check) — prevents tooling-injected NaN/Inf from poisoning            |
// |         |            |        | LastValid* and trapping the agent in a permanent recovery loop. M-2 second-hit collision      |
// |         |            |        | while already GROUNDED now refreshes GroundedReason / CollisionForce and resets TimeInState   |
// |         |            |        | so the new impulse extends recovery (was silently dropped). L-2 added null-array guard in     |
// |         |            |        | UpdateAllAgents (precedes length assert). L-5 added MatchClock contract assert on             |
// |         |            |        | currentTime (finite + non-negative).                                                            |
// | 1.9     | 2026-06-03 | —      | AR-6 fix: M-2 OscillationGuard bypassed for collision-driven GROUNDED transitions (was        |
// |         |            |        | blocking legitimate knockdowns for LockDuration after a flap-triggered lock). Pairs with      |
// |         |            |        | AgentStateMachine v1.7 — knockdown short-circuit now unconditional, so newState==GROUNDED     |
// |         |            |        | reliably reaches Step 3. L-1 inner else block reorganised to skip the redundant               |
// |         |            |        | `state.TimeInState += dt` immediately followed by `= 0.0f` on refresh.                         |
// | 1.10    | 2026-06-03 | —      | AR-7 fix: M-1 Step 3 collision-bypass branch now calls state.OscillationGuard.Initialize()    |
// |         |            |        | to wipe any pre-existing flap-lock — a collision is a structural break and prior flap         |
// |         |            |        | history is irrelevant; without the reset a designer who tunes LockDuration above             |
// |         |            |        | GroundedDwellClampMin silently extends collision recovery by the remaining lock window.       |
// |         |            |        | M-2 override-path Step 11 `state.Speed = Velocity.magnitude` moved inside the                |
// |         |            |        | HasInvalidValues check — when tooling injects NaN/Inf, Speed is no longer poisoned and       |
// |         |            |        | does not propagate NaN into the next frame's state-machine evaluation. L-2 isCollisionTrans- |
// |         |            |        | ition declaration scope tightened (moved inside the outer if). L-3 inner-else GROUNDED       |
// |         |            |        | check retained as belt-and-braces with comment naming the AR-6 M-1 invariant dependency.    |
// | 1.11    | 2026-06-03 | —      | AR-8 fix: M-1 ctor asserts physicsHz finite and positive — 0 / NaN / negative would silently |
// |         |            |        | disable the dt-fidelity assert in Update (1.5/0 = +Inf, 1.5/NaN = NaN) and let stalled-loop  |
// |         |            |        | frames through the gate. L-1 Step 3 transition branch reuses the isCollisionTransition local |
// |         |            |        | (was recomputing the same `isCollisionKnockdown && newState == GROUNDED` predicate).         |
// |         |            |        | L-2 CollisionForce cache writes wrapped in Mathf.Clamp01 — enforces the AgentState.cs        |
// |         |            |        | [0, 1] doc contract for downstream debug/animation consumers that read the raw cached value. |
// | 1.12    | 2026-06-03 | —      | AR-9 fix: M-1 EvaluateState now receives state.CollisionForce (cached entry-force) for the   |
// |         |            |        | GROUNDED dwell calculation instead of the incoming this-frame collisionForce. The incoming    |
// |         |            |        | value is 0 on every dwell frame after entry (collision is a one-frame impulse from Spec #3); |
// |         |            |        | passing it shrank the perceived dwell ~35% for a max-force hit (force=1.0 entry → forceScale  |
// |         |            |        | dropped to CollisionDwellMin=0.65 on frame 1 and beyond), releasing the agent prematurely.   |
// |         |            |        | The cached state.CollisionForce (set on entry, refreshed on second-hit per AR-5 M-2) is the  |
// |         |            |        | value the §3.1.5 dwell formula was designed to consume.                                       |
// | 1.13    | 2026-06-07 | —      | AR-10 fix: M-1 NaN-typed collisionForce input would poison state.CollisionForce via the     |
// |         |            |        | AR-8 L-2 Mathf.Clamp01 wrap (Unity's Clamp01 is `<0 ? 0 : >1 ? 1 : value` and both halves    |
// |         |            |        | of the ternary are false for NaN, so NaN passes through). The poisoned cache propagates      |
// |         |            |        | into CalculateGroundedDwell on the next frame, turns the entire formula into NaN, and       |
// |         |            |        | flips the `dwellTimer < requiredDwell` gate to false → agent releases one frame after a     |
// |         |            |        | NaN-force hit instead of recovering. New private static SanitiseCollisionForce maps NaN /  |
// |         |            |        | ±Infinity → 0 (safe-recovery minimum-impulse default) before Clamp01; consumed at both the |
// |         |            |        | outer-transition and inner-else cache-write sites. L-1 currentTime assert tightened from   |
// |         |            |        | `!IsNaN && >= 0` to `float.IsFinite && >= 0` — the prior gate let +Infinity through, which |
// |         |            |        | makes the OscillationGuard `currentTime - ReadTime(i) < WindowSeconds` math evaluate true   |
// |         |            |        | for every historical slot. `IsFinite` catches NaN and both infinities in one call.         |
// | 1.14    | 2026-06-09 | —      | AR-12 fix: H-1 IDLE locomotion branch accelerates (walk rate) toward the command-capped     |
// |         |            |        | topSpeed when commandSpeed > 0 — previously it only decayed toward 0 while EvaluateFromIdle |
// |         |            |        | required speed > IdleExit, so an agent at rest given any moving command was deadlocked at   |
// |         |            |        | speed 0 forever. H-2 topSpeed = min(topSpeed, commandSpeed) — command intent now caps the   |
// |         |            |        | integration target; jog commands no longer auto-promote to SPRINTING (reservoir drain) and  |
// |         |            |        | walk commands no longer flap WALKING→JOGGING→DECELERATING. M-1 LeanAngle computed from the  |
// |         |            |        | Step 8 velocity-direction rotation (path curvature) at newSpeed instead of the Step 7       |
// |         |            |        | facing rotation; CurrentTurnRate remains the achieved facing rate. L-3 one-frame AUTO_ALIGN |
// |         |            |        | facing-vs-velocity lag documented as deliberate §4.4.1 ordering.                            |
// | 1.15    | 2026-06-09 | —      | AR-13 fix: M-1 commandSpeed clamped to JogEnter when AerobicPool < AerobicJogFloor —        |
// |         |            |        | found on the AR-12 re-review: the H-2 cap left an exhausted agent with a jog/sprint command |
// |         |            |        | flapping WALKING→JOGGING→DECELERATING at ~3 Hz (state machine refuses JOGGING on the       |
// |         |            |        | aerobic gate while the un-degraded commandSpeed kept pushing speed through JogEnter).      |
// |         |            |        | M-2 IDLE launch gated on hasMovementIntent (commandSpeed > 0 AND non-degenerate target     |
// |         |            |        | offset) — the Decision Tree HOLD action (StrafeWhileWatching with target == current        |
// |         |            |        | position, desired JOGGING) would otherwise feed newSpeed > 0 into Step 8 with a degenerate |
// |         |            |        | target from rest, tripping RotateVelocityToward's both-degenerate assert every frame.      |
// | 1.16    | 2026-06-12 | —      | Build fix (dotnet CI gate): using UnityEngine.Profiling -> Unity.Profiling.                 |
// |         |            |        | ProfilerMarker's actual namespace is Unity.Profiling; the old using was CS0246 under Unity  |
// |         |            |        | and the Linux compile gate alike, so this assembly could not have compiled in-engine. No    |
// |         |            |        | functional change.                                                                          |
// | 1.17    | 2026-11-21 | —      | W13-F5/F6/F7 (ERR-002-001): tracking commands (MovementCommand.IsTracking) — Step 2 lowers the |
// |         |            |        | band ceiling to |v_ff + v_arr| (CalculateTrackDesiredVelocity, §3.5.3.1) and steers along that  |
// |         |            |        | desired velocity; JOGGING / SPRINTING accelerate on the physical curve clamped at the tracked    |
// |         |            |        | speed and brake at MinDecelerationFloor above it; DECELERATING brakes to the tracked speed, not  |
// |         |            |        | to rest. Non-tracking commands take exactly the prior path.                                     |
// | 1.18    | 2026-11-23 | —      | ERR-002-002: tight markers tracking fresh moving targets use the capped physical curve in IDLE and WALKING. |
#endregion
