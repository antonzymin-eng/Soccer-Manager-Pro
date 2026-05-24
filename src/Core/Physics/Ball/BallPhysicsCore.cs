// File:     src/Core/Physics/Ball/BallPhysicsCore.cs
// Created:  2026-05-24
// Modified: 2026-05-24
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Main physics update loop and force calculations for the ball.
//           Pure calculations — no state management, no ownership of BallState.

using UnityEngine;
using UnityEngine.Profiling;

namespace TacticalDirector.BallPhysics
{
    /// <summary>
    /// Main physics update loop and force calculations for the ball.
    /// Pure calculations — no state management, no ownership of BallState.
    /// All methods static; side-effect free except validation warnings.
    /// </summary>
    public static class BallPhysicsCore
    {
        private static readonly ProfilerMarker s_updateMarker =
            new ProfilerMarker("BallPhysics.Update");
        private static readonly ProfilerMarker s_magnusMarker =
            new ProfilerMarker("BallPhysics.Magnus");
        private static readonly ProfilerMarker s_dragMarker =
            new ProfilerMarker("BallPhysics.Drag");
        private static readonly ProfilerMarker s_spinDecayMarker =
            new ProfilerMarker("BallPhysics.SpinDecay");

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
            using var _ = s_updateMarker.Auto();

            // Only update the recovery checkpoint when the incoming state is valid.
            // If ball.Position already contains NaN/Infinity, preserving the existing
            // LastValidPosition is the only way ValidatePhysicsState can recover to a
            // good state later in this same update.
            if (!HasInvalidValues(ball))
            {
                ball.LastValidPosition = ball.Position;
                ball.LastValidVelocity = ball.Velocity;
            }

            // BOUNCING: apply impulse first, then continue to integration.
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
                    return;
            }

            // Semi-implicit Euler integration.
            Vector3 acceleration = netForce / BallPhysicsConstants.Ball.MASS;
            ball.Velocity += acceleration * dt;
            ball.Position += ball.Velocity * dt;

            // Spin decay: aerodynamic torque model (airborne only).
            if (ball.State == BallStateType.AIRBORNE)
                ball.AngularVelocity = UpdateSpinDecay(ball.AngularVelocity, ball.Velocity, dt);

            // Spin decay: surface-contact friction model (rolling only).
            // Do NOT use UpdateSpinDecay() here — its aerodynamic torque model is incorrect
            // for ground-contact spin damping (see §3.1.7.2).
            if (ball.State == BallStateType.ROLLING)
                ball.AngularVelocity = UpdateRollingSpinDecay(ball.AngularVelocity, dt);

            ValidatePhysicsState(ref ball);
            ball.State = BallStateMachine.UpdateBallState(ball);
            logger?.TryLogSnapshot(ball, matchTime);
        }

        // ── FORCE CALCULATIONS ───────────────────────────────────────────────────

        /// <summary>
        /// Calculates Magnus force on a spinning airborne ball.
        /// F = 0.5 × ρ × |v|² × A × C_L × (ω̂ × v̂)
        /// </summary>
        public static Vector3 CalculateMagnusForce(Vector3 velocity, Vector3 angularVelocity)
        {
            using var _ = s_magnusMarker.Auto();

            float speed    = velocity.magnitude;
            float spinRate = angularVelocity.magnitude;

            if (speed    < BallPhysicsConstants.State.MinVelocity ||
                spinRate < BallPhysicsConstants.State.MinSpin)
                return Vector3.zero;

            float spinParameter = (BallPhysicsConstants.Ball.RADIUS * spinRate) / speed;
            spinParameter = Mathf.Clamp(
                spinParameter,
                BallPhysicsConstants.Magnus.MinSpinParameter,
                BallPhysicsConstants.Magnus.MaxSpinParameter);

            float normalizedS = (spinParameter - BallPhysicsConstants.Magnus.MinSpinParameter)
                              / (BallPhysicsConstants.Magnus.MaxSpinParameter
                               - BallPhysicsConstants.Magnus.MinSpinParameter);
            float C_L = BallPhysicsConstants.Magnus.LiftCoefficientBase
                      + BallPhysicsConstants.Magnus.LiftCoefficientScale * normalizedS;

            Vector3 forceDirection = Vector3.Cross(
                angularVelocity.normalized,
                velocity.normalized);

            if (forceDirection.sqrMagnitude < 0.0001f)
                return Vector3.zero;

            forceDirection.Normalize();

            float forceMagnitude = 0.5f
                                 * BallPhysicsConstants.Environment.AIR_DENSITY
                                 * speed * speed
                                 * BallPhysicsConstants.Ball.CrossSectionArea
                                 * C_L;

            return forceDirection * forceMagnitude;
        }

        /// <summary>
        /// Calculates aerodynamic drag force.
        /// F_drag = -0.5 × ρ × |v_rel|² × C_d × A × v̂_rel
        /// </summary>
        public static Vector3 CalculateDragForce(Vector3 relativeVelocity)
        {
            using var _ = s_dragMarker.Auto();

            float speed = relativeVelocity.magnitude;

            if (speed < BallPhysicsConstants.State.MinVelocity)
                return Vector3.zero;

            float C_d            = GetDragCoefficient(speed);
            float forceMagnitude = 0.5f
                                 * BallPhysicsConstants.Environment.AIR_DENSITY
                                 * speed * speed
                                 * C_d
                                 * BallPhysicsConstants.Ball.CrossSectionArea;

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
            using var _ = s_spinDecayMarker.Auto();

            float spinRate = angularVelocity.magnitude;
            float speed    = velocity.magnitude;

            if (spinRate < BallPhysicsConstants.State.MinSpin)
                return Vector3.zero;

            float r5              = Mathf.Pow(BallPhysicsConstants.Ball.RADIUS, 5);
            float torqueMagnitude = BallPhysicsConstants.Spin.TorqueCoefficient
                                  * BallPhysicsConstants.Environment.AIR_DENSITY
                                  * r5 * spinRate * spinRate;
            Vector3 torque = -torqueMagnitude * angularVelocity.normalized;

            float velocityDecay = BallPhysicsConstants.Spin.DecayVelocityFactor * speed;
            float spinDecay     = BallPhysicsConstants.Spin.DecaySpinFactor * spinRate;
            float totalDecay    = velocityDecay + spinDecay;

            Vector3 newOmega = angularVelocity * (1f - totalDecay * dt);
            newOmega += (torque / BallPhysicsConstants.Ball.MomentOfInertia) * dt;

            if (newOmega.magnitude < BallPhysicsConstants.State.MinSpin)
                return Vector3.zero;

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

            if (spinRate < BallPhysicsConstants.State.MinSpin)
                return Vector3.zero;

            float newSpinRate = spinRate
                              - BallPhysicsConstants.Spin.RollingSpinDecayPerSecond * dt;

            if (newSpinRate < BallPhysicsConstants.State.MinSpin)
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
            if (speed > BallPhysicsConstants.Limits.MaxVelocity)
            {
                ball.Velocity = ball.Velocity.normalized * BallPhysicsConstants.Limits.MaxVelocity;
                Debug.LogWarning($"[BallPhysics] Velocity clamped from {speed:F1} m/s");
            }

            float spinRate = ball.AngularVelocity.magnitude;
            if (spinRate > BallPhysicsConstants.Limits.MaxSpin)
            {
                ball.AngularVelocity = ball.AngularVelocity.normalized * BallPhysicsConstants.Limits.MaxSpin;
                Debug.LogWarning($"[BallPhysics] Spin clamped from {spinRate:F1} rad/s");
            }

            if (ball.Position.z > BallPhysicsConstants.Limits.MaxHeight)
            {
                ball.Position = new Vector3(
                    ball.Position.x, ball.Position.y,
                    BallPhysicsConstants.Limits.MaxHeight);
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

            float buffer = BallPhysicsConstants.Limits.PitchBuffer;
            ball.Position = new Vector3(
                Mathf.Clamp(ball.Position.x, -buffer, BallPhysicsConstants.Pitch.LENGTH + buffer),
                Mathf.Clamp(ball.Position.y, -buffer, BallPhysicsConstants.Pitch.WIDTH  + buffer),
                ball.Position.z);
        }

        // ── HELPERS ──────────────────────────────────────────────────────────────

        private static float GetDragCoefficient(float speed)
        {
            if (speed < BallPhysicsConstants.Drag.CrisisSpeedLow)
                return BallPhysicsConstants.Drag.CoefficientLaminar;

            if (speed > BallPhysicsConstants.Drag.CrisisSpeedHigh)
                return BallPhysicsConstants.Drag.CoefficientTurbulent;

            float t = (speed - BallPhysicsConstants.Drag.CrisisSpeedLow)
                    / (BallPhysicsConstants.Drag.CrisisSpeedHigh
                     - BallPhysicsConstants.Drag.CrisisSpeedLow);
            return Mathf.Lerp(
                BallPhysicsConstants.Drag.CoefficientLaminar,
                BallPhysicsConstants.Drag.CoefficientTurbulent, t);
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

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Bug fix: LastValidPosition/LastValidVelocity now guarded by        |
// |         |            |        | HasInvalidValues — prevents overwriting the recovery point when    |
// |         |            |        | the incoming state already contains NaN/Infinity.                  |
// |         |            |        | Standards fix: namespace → TacticalDirector.BallPhysics; ALL_CAPS  |
// |         |            |        | constant refs → PascalCase; ProfilerMarker replaces #if            |
// |         |            |        | DEVELOPMENT_BUILD Profiler.BeginSample/EndSample per FR-CS-070;    |
// |         |            |        | file header added per FR-CS-056/057.                               |
#endregion
