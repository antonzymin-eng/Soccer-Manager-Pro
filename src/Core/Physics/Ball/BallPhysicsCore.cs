using UnityEngine;

namespace TacticalDirector.Core.Physics.Ball
{
    /// <summary>
    /// Main physics update loop and force calculations for the ball.
    /// Pure calculations — no state management, no ownership of BallState.
    /// All methods static; side-effect free except validation warnings.
    /// </summary>
    public static class BallPhysicsCore
    {
        /// <summary>
        /// Main physics update. Called at 60 Hz by the match simulator.
        /// </summary>
        public static void UpdateBallPhysics(
            ref BallState ball,
            float dt,
            SurfaceType surface,
            Vector3 windVelocity,
            BallEventLogger logger,
            float matchTime)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Profiling.Profiler.BeginSample("BallPhysics.Update");
#endif
            ball.LastValidPosition = ball.Position;
            ball.LastValidVelocity = ball.Velocity;

            // BOUNCING: apply impulse first, then continue to integration
            if (ball.State == BallStateType.BOUNCING)
                BallGroundInteraction.ApplyBounce(ref ball, surface, logger, matchTime);

            Vector3 netForce         = Vector3.zero;
            Vector3 relativeVelocity = ball.Velocity - windVelocity;

            switch (ball.State)
            {
                case BallStateType.AIRBORNE:
                    netForce = GetGravityForce()
                             + CalculateDragForce(relativeVelocity)
                             + CalculateMagnusForce(relativeVelocity, ball.AngularVelocity);
                    break;

                case BallStateType.ROLLING:
                    netForce = CalculateDragForce(relativeVelocity)
                             + BallGroundInteraction.CalculateRollingFriction(ball.Velocity, surface);
                    break;

                case BallStateType.BOUNCING:
                    netForce = CalculateDragForce(relativeVelocity);
                    break;

                default:
                    // STATIONARY, CONTROLLED, OUT_OF_PLAY: no physics forces.
                    // Velocity not cleared; callers must not read Velocity in OUT_OF_PLAY.
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    UnityEngine.Profiling.Profiler.EndSample();
#endif
                    return;
            }

            // Semi-implicit Euler integration
            Vector3 acceleration = netForce / BallPhysicsConstants.Ball.MASS;
            ball.Velocity += acceleration * dt;
            ball.Position += ball.Velocity * dt;

            // Spin decay: aerodynamic torque model (airborne only)
            if (ball.State == BallStateType.AIRBORNE)
                ball.AngularVelocity = UpdateSpinDecay(ball.AngularVelocity, ball.Velocity, dt);

            // Spin decay: surface-contact friction model (rolling only)
            // Do NOT use UpdateSpinDecay() here — its aerodynamic torque model is incorrect
            // for ground-contact spin damping (see §3.1.7.2).
            if (ball.State == BallStateType.ROLLING)
                ball.AngularVelocity = UpdateRollingSpinDecay(ball.AngularVelocity, dt);

            ValidatePhysicsState(ref ball);
            ball.State = BallStateMachine.UpdateBallState(ball);
            logger?.TryLogSnapshot(ball, matchTime);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Profiling.Profiler.EndSample();
#endif
        }

        // ── FORCE CALCULATIONS ───────────────────────────────────────────────────

        /// <summary>
        /// Calculates Magnus force on a spinning airborne ball.
        /// F = 0.5 × ρ × |v|² × A × C_L × (ω̂ × v̂)
        /// </summary>
        public static Vector3 CalculateMagnusForce(Vector3 velocity, Vector3 angularVelocity)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Profiling.Profiler.BeginSample("BallPhysics.Magnus");
#endif
            float speed    = velocity.magnitude;
            float spinRate = angularVelocity.magnitude;

            if (speed    < BallPhysicsConstants.State.MIN_VELOCITY ||
                spinRate < BallPhysicsConstants.State.MIN_SPIN)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Profiling.Profiler.EndSample();
#endif
                return Vector3.zero;
            }

            float spinParameter = (BallPhysicsConstants.Ball.RADIUS * spinRate) / speed;
            spinParameter = Mathf.Clamp(
                spinParameter,
                BallPhysicsConstants.Magnus.MIN_SPIN_PARAMETER,
                BallPhysicsConstants.Magnus.MAX_SPIN_PARAMETER);

            float normalizedS = (spinParameter - BallPhysicsConstants.Magnus.MIN_SPIN_PARAMETER)
                              / (BallPhysicsConstants.Magnus.MAX_SPIN_PARAMETER
                               - BallPhysicsConstants.Magnus.MIN_SPIN_PARAMETER);
            float C_L = BallPhysicsConstants.Magnus.LIFT_COEFFICIENT_BASE
                      + BallPhysicsConstants.Magnus.LIFT_COEFFICIENT_SCALE * normalizedS;

            Vector3 forceDirection = Vector3.Cross(
                angularVelocity.normalized,
                velocity.normalized);

            if (forceDirection.sqrMagnitude < 0.0001f)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Profiling.Profiler.EndSample();
#endif
                return Vector3.zero;
            }
            forceDirection.Normalize();

            float forceMagnitude = 0.5f
                                 * BallPhysicsConstants.Environment.AIR_DENSITY
                                 * speed * speed
                                 * BallPhysicsConstants.Ball.CROSS_SECTION_AREA
                                 * C_L;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            return forceDirection * forceMagnitude;
        }

        /// <summary>
        /// Calculates aerodynamic drag force.
        /// F_drag = -0.5 × ρ × |v_rel|² × C_d × A × v̂_rel
        /// </summary>
        public static Vector3 CalculateDragForce(Vector3 relativeVelocity)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Profiling.Profiler.BeginSample("BallPhysics.Drag");
#endif
            float speed = relativeVelocity.magnitude;

            if (speed < BallPhysicsConstants.State.MIN_VELOCITY)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Profiling.Profiler.EndSample();
#endif
                return Vector3.zero;
            }

            float C_d           = GetDragCoefficient(speed);
            float forceMagnitude = 0.5f
                                 * BallPhysicsConstants.Environment.AIR_DENSITY
                                 * speed * speed
                                 * C_d
                                 * BallPhysicsConstants.Ball.CROSS_SECTION_AREA;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            return -relativeVelocity.normalized * forceMagnitude;
        }

        /// <summary>
        /// Returns gravitational force. Only applied when AIRBORNE.
        /// </summary>
        public static Vector3 GetGravityForce()
        {
            return new Vector3(
                0f, 0f,
                -BallPhysicsConstants.Ball.MASS * BallPhysicsConstants.Environment.GRAVITY);
        }

        // ── SPIN DYNAMICS ────────────────────────────────────────────────────────

        /// <summary>
        /// Decays angular velocity for an airborne ball using the aerodynamic torque model.
        /// AIRBORNE ONLY. Do NOT call for rolling balls — use UpdateRollingSpinDecay().
        /// </summary>
        public static Vector3 UpdateSpinDecay(
            Vector3 angularVelocity,
            Vector3 velocity,
            float dt)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Profiling.Profiler.BeginSample("BallPhysics.SpinDecay");
#endif
            float spinRate = angularVelocity.magnitude;
            float speed    = velocity.magnitude;

            if (spinRate < BallPhysicsConstants.State.MIN_SPIN)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Profiling.Profiler.EndSample();
#endif
                return Vector3.zero;
            }

            float r5             = Mathf.Pow(BallPhysicsConstants.Ball.RADIUS, 5);
            float torqueMagnitude = BallPhysicsConstants.Spin.TORQUE_COEFFICIENT
                                  * BallPhysicsConstants.Environment.AIR_DENSITY
                                  * r5 * spinRate * spinRate;
            Vector3 torque = -torqueMagnitude * angularVelocity.normalized;

            float velocityDecay = BallPhysicsConstants.Spin.DECAY_VELOCITY_FACTOR * speed;
            float spinDecay     = BallPhysicsConstants.Spin.DECAY_SPIN_FACTOR * spinRate;
            float totalDecay    = velocityDecay + spinDecay;

            Vector3 newOmega = angularVelocity * (1f - totalDecay * dt);
            newOmega += (torque / BallPhysicsConstants.Ball.MOMENT_OF_INERTIA) * dt;

            if (newOmega.magnitude < BallPhysicsConstants.State.MIN_SPIN)
            {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                UnityEngine.Profiling.Profiler.EndSample();
#endif
                return Vector3.zero;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            UnityEngine.Profiling.Profiler.EndSample();
#endif
            return newOmega;
        }

        /// <summary>
        /// Decays angular velocity for a rolling ball using surface-contact friction model.
        /// The aerodynamic torque model in UpdateSpinDecay() is physically incorrect here;
        /// ground-contact friction dominates and is better modelled as a linear decay rate.
        /// AngularVelocity entering this method already reflects ApplyBounce()'s spinRetention.
        /// </summary>
        public static Vector3 UpdateRollingSpinDecay(Vector3 angularVelocity, float dt)
        {
            float spinRate = angularVelocity.magnitude;

            if (spinRate < BallPhysicsConstants.State.MIN_SPIN)
                return Vector3.zero;

            float newSpinRate = spinRate
                              - BallPhysicsConstants.Spin.ROLLING_SPIN_DECAY_PER_SECOND * dt;

            if (newSpinRate < BallPhysicsConstants.State.MIN_SPIN)
                return Vector3.zero;

            return angularVelocity.normalized * newSpinRate;
        }

        // ── VALIDATION ───────────────────────────────────────────────────────────

        /// <summary>
        /// Validates physics state and applies safety clamps.
        /// Includes NaN/Infinity detection and recovery to last valid state.
        /// </summary>
        public static void ValidatePhysicsState(ref BallState ball)
        {
            if (HasInvalidValues(ball))
            {
                Debug.LogError("[BallPhysics] NaN/Infinity detected — recovering to last valid state.");
                ball.Position        = ball.LastValidPosition;
                ball.Velocity        = ball.LastValidVelocity;
                ball.AngularVelocity = Vector3.zero;
                ball.State           = BallStateType.STATIONARY;
                return;
            }

            float speed = ball.Velocity.magnitude;
            if (speed > BallPhysicsConstants.Limits.MAX_VELOCITY)
            {
                ball.Velocity = ball.Velocity.normalized * BallPhysicsConstants.Limits.MAX_VELOCITY;
                Debug.LogWarning($"[BallPhysics] Velocity clamped from {speed:F1} m/s");
            }

            float spinRate = ball.AngularVelocity.magnitude;
            if (spinRate > BallPhysicsConstants.Limits.MAX_SPIN)
            {
                ball.AngularVelocity = ball.AngularVelocity.normalized * BallPhysicsConstants.Limits.MAX_SPIN;
                Debug.LogWarning($"[BallPhysics] Spin clamped from {spinRate:F1} rad/s");
            }

            if (ball.Position.z > BallPhysicsConstants.Limits.MAX_HEIGHT)
            {
                ball.Position = new Vector3(
                    ball.Position.x, ball.Position.y,
                    BallPhysicsConstants.Limits.MAX_HEIGHT);
                ball.Velocity = new Vector3(
                    ball.Velocity.x, ball.Velocity.y,
                    Mathf.Min(ball.Velocity.z, 0f));
                Debug.LogWarning("[BallPhysics] Height clamped — possible instability");
            }

            float groundLevel = BallPhysicsConstants.Ball.RADIUS;
            if (ball.Position.z < groundLevel && ball.State != BallStateType.OUT_OF_PLAY)
            {
                ball.Position = new Vector3(ball.Position.x, ball.Position.y, groundLevel);
                if (ball.Velocity.z < 0f)
                    ball.Velocity = new Vector3(ball.Velocity.x, ball.Velocity.y, 0f);
            }

            float buffer = BallPhysicsConstants.Limits.PITCH_BUFFER;
            ball.Position = new Vector3(
                Mathf.Clamp(ball.Position.x, -buffer, BallPhysicsConstants.Pitch.LENGTH + buffer),
                Mathf.Clamp(ball.Position.y, -buffer, BallPhysicsConstants.Pitch.WIDTH  + buffer),
                ball.Position.z);
        }

        // ── HELPERS ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns drag coefficient using simplified linear drag-crisis interpolation.
        /// </summary>
        private static float GetDragCoefficient(float speed)
        {
            if (speed < BallPhysicsConstants.Drag.CRISIS_SPEED_LOW)
                return BallPhysicsConstants.Drag.COEFFICIENT_LAMINAR;

            if (speed > BallPhysicsConstants.Drag.CRISIS_SPEED_HIGH)
                return BallPhysicsConstants.Drag.COEFFICIENT_TURBULENT;

            float t = (speed - BallPhysicsConstants.Drag.CRISIS_SPEED_LOW)
                    / (BallPhysicsConstants.Drag.CRISIS_SPEED_HIGH
                     - BallPhysicsConstants.Drag.CRISIS_SPEED_LOW);
            return Mathf.Lerp(
                BallPhysicsConstants.Drag.COEFFICIENT_LAMINAR,
                BallPhysicsConstants.Drag.COEFFICIENT_TURBULENT, t);
        }

        private static bool HasInvalidValues(BallState ball)
        {
            return float.IsNaN(ball.Position.x)        || float.IsInfinity(ball.Position.x)
                || float.IsNaN(ball.Position.y)        || float.IsInfinity(ball.Position.y)
                || float.IsNaN(ball.Position.z)        || float.IsInfinity(ball.Position.z)
                || float.IsNaN(ball.Velocity.x)        || float.IsInfinity(ball.Velocity.x)
                || float.IsNaN(ball.Velocity.y)        || float.IsInfinity(ball.Velocity.y)
                || float.IsNaN(ball.Velocity.z)        || float.IsInfinity(ball.Velocity.z)
                || float.IsNaN(ball.AngularVelocity.x) || float.IsInfinity(ball.AngularVelocity.x)
                || float.IsNaN(ball.AngularVelocity.y) || float.IsInfinity(ball.AngularVelocity.y)
                || float.IsNaN(ball.AngularVelocity.z) || float.IsInfinity(ball.AngularVelocity.z);
        }
    }
}
