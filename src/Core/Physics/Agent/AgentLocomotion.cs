// File:     src/Core/Physics/Agent/AgentLocomotion.cs
// Created:  2026-05-22
// Modified: 2026-05-22
// Author:   —
// Spec:     Agent Movement #2 §3.2, Code Standards #20
// Purpose:  Acceleration, top speed, and deceleration calculations. All static, no side effects.

using UnityEngine;
using UnityEngine.Profiling;

namespace TacticalDirector.Core.Physics.Agent
{
    /// <summary>
    /// Computes velocity updates for the 60 Hz physics loop.
    /// All methods are static and pure. State is passed by ref where mutation is needed.
    /// Agent Movement #2 §3.2.
    /// </summary>
    public static class AgentLocomotion
    {
        private static readonly ProfilerMarker s_calculateAccelerationMarker =
            new ProfilerMarker("AgentLocomotion.CalculateAcceleration");

        private static readonly ProfilerMarker s_calculateDecelerationMarker =
            new ProfilerMarker("AgentLocomotion.CalculateDeceleration");

        /// <summary>
        /// Base top speed for an agent given their effective Pace attribute.
        /// Formula: TOP_SPEED_MIN + (effectivePace - 1) × PER_POINT. Agent Movement #2 §3.2.4.
        /// </summary>
        public static float CalculateBaseTopSpeed(float effectivePace)
        {
            float t = Mathf.Clamp(effectivePace - 1.0f, 0.0f, 19.0f);
            return LocomotionConstants.TOP_SPEED_MIN + t * LocomotionConstants.TOP_SPEED_PER_PACE_POINT;
        }

        /// <summary>
        /// Base exponential acceleration k for an agent given their effective Acceleration attribute.
        /// Formula: ACCEL_K_MIN + (effectiveAccel - 1) × PER_POINT. Agent Movement #2 §3.2.3.
        /// </summary>
        public static float CalculateBaseAccelK(float effectiveAcceleration)
        {
            float t = Mathf.Clamp(effectiveAcceleration - 1.0f, 0.0f, 19.0f);
            return LocomotionConstants.ACCEL_K_MIN + t * LocomotionConstants.ACCEL_K_PER_POINT;
        }

        /// <summary>
        /// Applies exponential acceleration toward topSpeed for one physics frame.
        /// v(t+dt) = topSpeed × (1 - e^(-k×dt)) + currentSpeed × e^(-k×dt).
        /// Agent Movement #2 §3.2.3.
        /// </summary>
        public static float ApplyAcceleration(
            float currentSpeed, float topSpeed, float k, float dt)
        {
            using var _ = s_calculateAccelerationMarker.Auto();

            float decay = Mathf.Exp(-k * dt);
            float newSpeed = topSpeed * (1.0f - decay) + currentSpeed * decay;
            return Mathf.Clamp(newSpeed, 0.0f, MovementThresholds.MAX_SPEED_CLAMP);
        }

        /// <summary>
        /// Applies proportional deceleration (velocity-squared braking) for one physics frame.
        /// Stopping distance d determines decel magnitude: a = v² / (2d).
        /// Agent Movement #2 §3.2.5.
        /// </summary>
        public static float ApplyDeceleration(
            float currentSpeed, float stoppingDistanceM, float dt)
        {
            using var _ = s_calculateDecelerationMarker.Auto();

            if (currentSpeed <= MovementThresholds.MIN_VELOCITY_MAGNITUDE)
            {
                return 0.0f;
            }

            float decelMagnitude = (currentSpeed * currentSpeed)
                                 / (2.0f * Mathf.Max(stoppingDistanceM, 0.1f));
            decelMagnitude = Mathf.Min(decelMagnitude, MovementThresholds.MAX_ACCELERATION);

            float newSpeed = currentSpeed - decelMagnitude * dt;
            return Mathf.Max(newSpeed, 0.0f);
        }

        /// <summary>
        /// Stopping distance for the given deceleration mode and effective Pace attribute.
        /// Interpolates linearly across the attribute range. Agent Movement #2 §3.2.5.
        /// </summary>
        public static float CalculateStoppingDistance(
            DecelerationMode mode, float effectivePace)
        {
            float t = Mathf.Clamp01((effectivePace - 1.0f) / 19.0f);

            if (mode == DecelerationMode.EMERGENCY)
            {
                return Mathf.Lerp(
                    LocomotionConstants.EMERGENCY_DECEL_DIST_MIN,
                    LocomotionConstants.EMERGENCY_DECEL_DIST_MAX,
                    t);
            }

            return Mathf.Lerp(
                LocomotionConstants.CONTROLLED_DECEL_DIST_MIN,
                LocomotionConstants.CONTROLLED_DECEL_DIST_MAX,
                t);
        }

        /// <summary>
        /// Aerobic pool modifier applied to top speed when pool is below threshold.
        /// Piecewise: above AEROBIC_MODIFIER_THRESHOLD → 1.0; below → linear down to AEROBIC_MODIFIER_FLOOR.
        /// Agent Movement #2 §3.1.3.
        /// </summary>
        public static float CalculateAerobicModifier(float aerobicPool)
        {
            if (aerobicPool >= MovementThresholds.AEROBIC_MODIFIER_THRESHOLD)
            {
                return 1.0f;
            }

            float t = Mathf.Clamp01(aerobicPool / MovementThresholds.AEROBIC_MODIFIER_THRESHOLD);
            return Mathf.Lerp(MovementThresholds.AEROBIC_MODIFIER_FLOOR, 1.0f, t);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-22 | —      | Initial implementation. |
#endregion
