// File:     src/agent-movement/AgentLocomotion.cs
// Created:  2026-05-22
// Modified: 2026-05-25
// Author:   —
// Spec:     Agent Movement #2 §3.2, Code Standards #20
// Purpose:  Acceleration, top speed, and deceleration calculations. All static, no side effects.

using UnityEngine;

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Computes velocity updates for the 60 Hz physics loop.
    /// All methods are static and pure. State is passed by ref where mutation is needed.
    /// Agent Movement #2 §3.2.
    /// </summary>
    public static class AgentLocomotion
    {
        /// <summary>
        /// Base top speed for an agent given their effective Pace attribute.
        /// Formula: TOP_SPEED_MIN + (effectivePace - 1) × TopSpeedPerPacePoint. Agent Movement #2 §3.2.4.
        /// </summary>
        public static float CalculateBaseTopSpeed(float effectivePace)
        {
            float t = Mathf.Clamp(effectivePace - PlayerAttributeConstants.AttributeMin, 0.0f, PlayerAttributeConstants.AttributeRangeSpan);
            return LocomotionConstants.TOP_SPEED_MIN + t * LocomotionConstants.TopSpeedPerPacePoint;
        }

        /// <summary>
        /// Base exponential acceleration k for an agent given their effective Acceleration attribute.
        /// Formula: AccelKMin + (effectiveAccel - 1) × AccelKPerPoint. Agent Movement #2 §3.2.3.
        /// </summary>
        public static float CalculateBaseAccelK(float effectiveAcceleration)
        {
            float t = Mathf.Clamp(effectiveAcceleration - PlayerAttributeConstants.AttributeMin, 0.0f, PlayerAttributeConstants.AttributeRangeSpan);
            return LocomotionConstants.AccelKMin + t * LocomotionConstants.AccelKPerPoint;
        }

        /// <summary>
        /// Applies exponential acceleration toward topSpeed for one physics frame.
        /// v(t+dt) = topSpeed × (1 - e^(-k×dt)) + currentSpeed × e^(-k×dt).
        /// Agent Movement #2 §3.2.3.
        /// </summary>
        public static float ApplyAcceleration(
            float currentSpeed, float topSpeed, float k, float dt)
        {
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
            if (currentSpeed <= MovementThresholds.MIN_VELOCITY_MAGNITUDE)
            {
                return 0.0f;
            }

            float decelMagnitude = (currentSpeed * currentSpeed)
                                 / (LocomotionConstants.KINEMATIC_HALF * Mathf.Max(stoppingDistanceM, LocomotionConstants.MinStoppingDistanceM));
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
            float t = Mathf.Clamp01((effectivePace - PlayerAttributeConstants.AttributeMin) / PlayerAttributeConstants.AttributeRangeSpan);

            if (mode == DecelerationMode.EMERGENCY)
            {
                return Mathf.Lerp(
                    LocomotionConstants.EmergencyDecelDistMin,
                    LocomotionConstants.EmergencyDecelDistMax,
                    t);
            }

            return Mathf.Lerp(
                LocomotionConstants.ControlledDecelDistMin,
                LocomotionConstants.ControlledDecelDistMax,
                t);
        }

        /// <summary>
        /// Aerobic pool modifier applied to top speed when pool is below threshold.
        /// Piecewise: above AerobicModifierThreshold → 1.0; below → linear down to AerobicModifierFloor.
        /// Agent Movement #2 §3.1.3.
        /// </summary>
        public static float CalculateAerobicModifier(float aerobicPool)
        {
            if (aerobicPool >= MovementThresholds.AerobicModifierThreshold)
            {
                return 1.0f;
            }

            float t = Mathf.Clamp01(aerobicPool / MovementThresholds.AerobicModifierThreshold);
            return Mathf.Lerp(MovementThresholds.AerobicModifierFloor, 1.0f, t);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                              |
// | 1.0     | 2026-05-22 | —      | Initial implementation.                                                            |
// | 1.1     | 2026-05-25 | —      | Pass-1 fix: H-2 namespace; L-1 PascalCase refs; M-7 ProfilerMarkers removed.       |
// | 1.2     | 2026-05-25 | —      | Pass-2 fix: 0.1f → LocomotionConstants.MinStoppingDistanceM. Pass-3: 19.0f/1.0f attribute      |
// |         |            |        | literals → PlayerAttributeConstants.AttributeRangeSpan / AttributeMin.                         |
// | 1.3     | 2026-05-25 | —      | Pass-4 fix: M-4 2.0f kinematic divisor → LocomotionConstants.KINEMATIC_HALF [FIXED].           |
#endregion
