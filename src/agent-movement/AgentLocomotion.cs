// File:     src/agent-movement/AgentLocomotion.cs
// Created:  2026-05-22
// Modified: 2026-06-09 (AR-12 fix pass)
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
            // The approach must never overshoot its asymptote: from below, float rounding
            // could land 1 ulp above topSpeed, and topSpeed is command-capped at exact band
            // thresholds (e.g. SprintEnter) — a 1-ulp overshoot would trip the strict
            // `speed > SprintEnter` promotion in the state machine for a command that never
            // requested sprinting. From above (directional multiplier dropped topSpeed below
            // current), the exponential decays toward topSpeed and must not be snapped down.
            float ceiling = Mathf.Max(currentSpeed, topSpeed);
            return Mathf.Clamp(newSpeed, 0.0f, Mathf.Min(ceiling, MovementThresholds.MAX_SPEED_CLAMP));
        }

        /// <summary>
        /// Applies proportional deceleration (velocity-squared braking) for one physics frame.
        /// Stopping distance d determines the requested decel magnitude via a = v² / (2d),
        /// clamped to [MinDecelerationFloor, MAX_ACCELERATION] (m/s²). When v²/(2d) exceeds
        /// the cap, the agent overshoots the requested stopping distance — this is the
        /// documented physical limit on safe deceleration. The floor terminates the profile:
        /// recomputing v²/(2d) each frame against the FIXED total d is hyperbolic decay
        /// (v(t) = v₀/(1 + v₀t/2d)) which never reaches the IdleEnter threshold in bounded
        /// time. Below the speed where v²/(2d) crosses the floor, braking is constant at
        /// MinDecelerationFloor. Agent Movement #2 §3.2.5, §4.3.1.
        ///
        /// SPEC DEVIATION NOTE (AR-12): §3.2.5 normatively models braking as a CONSTANT
        /// decelRate with d = v₀²/(2·rate) as the derived outcome; this implementation
        /// re-derives the rate per frame from the decaying current speed (pre-AR-12
        /// deviation), which the floor now bounds (worst-case travel ≈ d × (1 + ln(v₀²/(2d·floor))),
        /// ~1.5× the requested d from jog speed). Full re-parametrisation to the spec's
        /// constant-rate form conflicts with the MAX_ACCELERATION=8 cap (§3.2.5 rates reach
        /// 11.6–23 m/s² at sprint speeds) and is deferred to a spec-alignment pass.
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
            decelMagnitude = Mathf.Clamp(decelMagnitude,
                LocomotionConstants.MinDecelerationFloor, MovementThresholds.MAX_ACCELERATION);

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
// | 1.4     | 2026-06-03 | —      | AR-4 fix: M-7 ApplyDeceleration XML doc now explicitly states the MAX_ACCELERATION cap and    |
// |         |            |        | that overshoot is possible when v²/(2d) exceeds the cap.                                       |
// | 1.5     | 2026-06-09 | —      | AR-12 fix: H-3 ApplyDeceleration gains MinDecelerationFloor — recomputing v²/(2d) per frame   |
// |         |            |        | against the FIXED total d was hyperbolic (Zeno) decay; stopping from 6 m/s took ~78 s and     |
// |         |            |        | ~32 m of travel before the IdleEnter threshold. With the floor, the high-speed kinematic      |
// |         |            |        | profile is unchanged and the tail brakes at a constant 2.5 m/s². ApplyAcceleration result is  |
// |         |            |        | additionally ceilinged at max(currentSpeed, topSpeed) — a 1-ulp overshoot of a command-capped |
// |         |            |        | topSpeed sitting exactly on a band threshold (SprintEnter) would trip the strict `>`           |
// |         |            |        | promotion in the state machine (supports AR-12 H-2).                                          |
#endregion
