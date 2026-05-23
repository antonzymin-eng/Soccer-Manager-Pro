// File:     src/Core/Physics/Agent/AgentMovementConstants.cs
// Created:  2026-05-22
// Modified: 2026-05-22
// Author:   —
// Spec:     Agent Movement #2 §3.1–§3.4, §4.3, Code Standards #20
// Purpose:  All constants for the agent movement system. No literals in formula code.

namespace TacticalDirector.Core.Physics.Agent
{
    /// <summary>
    /// Speed thresholds and energy gate constants for the movement state machine (§3.1.3).
    /// </summary>
    public static class MovementThresholds
    {
        #region Fixed

        /// <summary>[FIXED] Maximum possible agent speed; used for stumble risk normalisation. Agent Movement #2 §3.4.</summary>
        public const float MAX_SPEED = 12.0f;

        /// <summary>[FIXED] Maximum allowable speed before safety clamp (m/s). Agent Movement #2 §4.3.1.</summary>
        public const float MAX_SPEED_CLAMP = 12.0f;

        /// <summary>[FIXED] Maximum allowable acceleration magnitude before clamp (m/s²). Agent Movement #2 §4.3.1.</summary>
        public const float MAX_ACCELERATION = 8.0f;

        /// <summary>[FIXED] Minimum velocity magnitude; below this is treated as effectively zero (m/s). Agent Movement #2 §4.3.1.</summary>
        public const float MIN_VELOCITY_MAGNITUDE = 0.001f;

        #endregion

        #region GT

        /// <summary>[GT] Speed below which agent enters IDLE (m/s). Agent Movement #2 §3.1.3.</summary>
        public static readonly float IDLE_ENTER = 0.1f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Speed above which agent exits IDLE (m/s). Agent Movement #2 §3.1.3.</summary>
        public static readonly float IDLE_EXIT = 0.3f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Speed above which agent enters JOGGING (m/s). Agent Movement #2 §3.1.3.</summary>
        public static readonly float JOG_ENTER = 2.2f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Speed below which agent exits JOGGING (m/s). Agent Movement #2 §3.1.3.</summary>
        public static readonly float JOG_EXIT = 1.9f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Speed above which agent enters SPRINTING (m/s). Agent Movement #2 §3.1.3.</summary>
        public static readonly float SPRINT_ENTER = 5.8f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Speed below which agent exits SPRINTING (m/s). Agent Movement #2 §3.1.3.</summary>
        public static readonly float SPRINT_EXIT = 5.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum speed at which a stumble can occur (m/s). Agent Movement #2 §3.1.3.</summary>
        public static readonly float STUMBLE_SPEED_THRESHOLD = 2.2f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Turn angle (degrees) that triggers a stumble check when at speed. Agent Movement #2 §3.4.4.</summary>
        public static readonly float STUMBLE_TURN_ANGLE = 60.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum stumble risk floor; prevents elite players being physically immune. Agent Movement #2 §3.1.5.</summary>
        public static readonly float MIN_STUMBLE_RISK = 0.03f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Sprint reservoir below which SPRINTING is forced to JOGGING. Agent Movement #2 §3.1.3.</summary>
        public static readonly float SPRINT_RESERVOIR_FLOOR = 0.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Sprint reservoir required to re-enter SPRINTING from JOGGING. Agent Movement #2 §3.1.3.</summary>
        public static readonly float SPRINT_RESERVOIR_REENTRY = 0.35f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aerobic pool below which JOGGING is forced to DECELERATING. Agent Movement #2 §3.1.3.</summary>
        public static readonly float AEROBIC_JOG_FLOOR = 0.15f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aerobic modifier applied at AEROBIC_MODIFIER_THRESHOLD pool level. Agent Movement #2 §3.1.3.</summary>
        public static readonly float AEROBIC_MODIFIER_FLOOR = 0.70f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aerobic pool level at which modifier begins degrading. Agent Movement #2 §3.1.3.</summary>
        public static readonly float AEROBIC_MODIFIER_THRESHOLD = 0.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Base dwell time for STUMBLING recovery (seconds). Agent Movement #2 §3.1.5.</summary>
        public static readonly float STUMBLE_MIN_DWELL_BASE = 0.6f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Base dwell time for GROUNDED recovery (seconds). Agent Movement #2 §3.1.5.</summary>
        public static readonly float GROUNDED_MIN_DWELL_BASE = 1.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Force-scale minimum for collision dwell calculation (light nudge factor). Agent Movement #2 §3.1.5.</summary>
        public static readonly float COLLISION_DWELL_MIN = 0.65f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Max state transitions per second before oscillation guard activates. Agent Movement #2 §3.1.7.</summary>
        public static readonly int MAX_TRANSITIONS_PER_SECOND = 6; // TODO: replace with config loader (Stage 1)

        #endregion
    }

    /// <summary>
    /// Sprint and aerobic drain/recovery rates per movement state (§3.1.3).
    /// </summary>
    public static class FatigueRates
    {
        #region GT

        /// <summary>[GT] Sprint reservoir drain rate while SPRINTING (units/second). Agent Movement #2 §3.1.3.</summary>
        public static readonly float SprintDrainSprinting = 0.12f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Sprint reservoir recovery rate while JOGGING (units/second). Agent Movement #2 §3.1.3.</summary>
        public static readonly float SprintRecoveryJogging = 0.04f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Sprint reservoir recovery rate while WALKING (units/second). Agent Movement #2 §3.1.3.</summary>
        public static readonly float SprintRecoveryWalking = 0.06f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Sprint reservoir recovery rate while IDLE (units/second). Agent Movement #2 §3.1.3.</summary>
        public static readonly float SprintRecoveryIdle = 0.08f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aerobic pool drain rate while SPRINTING (units/second). Agent Movement #2 §3.1.3.</summary>
        public static readonly float AerobicDrainSprinting = 0.006f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aerobic pool drain rate while JOGGING (units/second). Agent Movement #2 §3.1.3.</summary>
        public static readonly float AerobicDrainJogging = 0.002f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aerobic pool recovery rate while WALKING (units/second). Agent Movement #2 §3.1.3.</summary>
        public static readonly float AerobicRecoveryWalking = 0.001f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aerobic pool recovery rate while IDLE (units/second). Agent Movement #2 §3.1.3.</summary>
        public static readonly float AerobicRecoveryIdle = 0.002f; // TODO: replace with config loader (Stage 1)

        #endregion
    }

    /// <summary>
    /// Locomotion constants: top speed, acceleration, and deceleration mappings (§3.2).
    /// </summary>
    public static class LocomotionConstants
    {
        #region Fixed

        /// <summary>[FIXED] Minimum base top speed (m/s) at Pace attribute 1. Agent Movement #2 §3.2.4.</summary>
        public const float TOP_SPEED_MIN = 7.5f;

        /// <summary>[FIXED] Maximum base top speed (m/s) at Pace attribute 20. Agent Movement #2 §3.2.4.</summary>
        public const float TOP_SPEED_MAX = 10.2f;

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Top speed increment per Pace attribute point.
        /// Formula: (TOP_SPEED_MAX - TOP_SPEED_MIN) / 19. Agent Movement #2 §3.2.4.
        /// Source constants: LocomotionConstants.TOP_SPEED_MIN, LocomotionConstants.TOP_SPEED_MAX.
        /// </summary>
        public static readonly float TOP_SPEED_PER_PACE_POINT =
            (TOP_SPEED_MAX - TOP_SPEED_MIN) / 19.0f;

        #endregion

        #region GT

        /// <summary>[GT] Exponential acceleration k at Acceleration attribute 1 (s⁻¹). Agent Movement #2 §3.2.3.</summary>
        public static readonly float ACCEL_K_MIN = 0.658f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Exponential acceleration k at Acceleration attribute 20 (s⁻¹). Agent Movement #2 §3.2.3.</summary>
        public static readonly float ACCEL_K_MAX = 0.921f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Controlled deceleration stopping distance at Pace 1 (m). Agent Movement #2 §3.2.5.</summary>
        public static readonly float CONTROLLED_DECEL_DIST_MIN = 3.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Controlled deceleration stopping distance at Pace 20 (m). Agent Movement #2 §3.2.5.</summary>
        public static readonly float CONTROLLED_DECEL_DIST_MAX = 5.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Emergency deceleration stopping distance at Pace 1 (m). Agent Movement #2 §3.2.5.</summary>
        public static readonly float EMERGENCY_DECEL_DIST_MIN = 2.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Emergency deceleration stopping distance at Pace 20 (m). Agent Movement #2 §3.2.5.</summary>
        public static readonly float EMERGENCY_DECEL_DIST_MAX = 3.5f; // TODO: replace with config loader (Stage 1)

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Acceleration k increment per Acceleration attribute point.
        /// Formula: (ACCEL_K_MAX - ACCEL_K_MIN) / 19. Agent Movement #2 §3.2.3.
        /// </summary>
        public static readonly float ACCEL_K_PER_POINT =
            (ACCEL_K_MAX - ACCEL_K_MIN) / 19.0f;

        #endregion
    }

    /// <summary>
    /// Directional speed multiplier constants (§3.3).
    /// </summary>
    public static class DirectionalConstants
    {
        #region Fixed

        /// <summary>[FIXED] Forward zone upper boundary (degrees). Agent Movement #2 §3.3.2.</summary>
        public const float FORWARD_ZONE_MAX = 30.0f;

        /// <summary>[FIXED] Lateral zone start after forward interpolation band (degrees). Agent Movement #2 §3.3.2.</summary>
        public const float LATERAL_ZONE_START = 40.0f;

        /// <summary>[FIXED] Lateral zone end before backward interpolation band (degrees). Agent Movement #2 §3.3.2.</summary>
        public const float LATERAL_ZONE_END = 80.0f;

        /// <summary>[FIXED] Backward zone lower boundary (degrees). Agent Movement #2 §3.3.2.</summary>
        public const float BACKWARD_ZONE_START = 90.0f;

        /// <summary>[FIXED] Hysteresis dead zone on zone boundaries (degrees). Agent Movement #2 §3.3.2.</summary>
        public const float ZONE_HYSTERESIS = 3.0f;

        #endregion

        #region GT

        /// <summary>[GT] Lateral multiplier at Agility 1. Agent Movement #2 §3.3.2.</summary>
        public static readonly float LATERAL_MULT_MIN = 0.65f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Lateral multiplier at Agility 20. Agent Movement #2 §3.3.2.</summary>
        public static readonly float LATERAL_MULT_MAX = 0.75f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Backward multiplier at Agility 1. Agent Movement #2 §3.3.2.</summary>
        public static readonly float BACKWARD_MULT_MIN = 0.45f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Backward multiplier at Agility 20. Agent Movement #2 §3.3.2.</summary>
        public static readonly float BACKWARD_MULT_MAX = 0.55f; // TODO: replace with config loader (Stage 1)

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Lateral multiplier increment per Agility point.
        /// Formula: (LATERAL_MULT_MAX - LATERAL_MULT_MIN) / 19. Agent Movement #2 §3.3.2.
        /// </summary>
        public static readonly float LATERAL_MULT_PER_AGILITY_POINT =
            (LATERAL_MULT_MAX - LATERAL_MULT_MIN) / 19.0f;

        /// <summary>
        /// [DERIVED] Backward multiplier increment per Agility point.
        /// Formula: (BACKWARD_MULT_MAX - BACKWARD_MULT_MIN) / 19. Agent Movement #2 §3.3.2.
        /// </summary>
        public static readonly float BACKWARD_MULT_PER_AGILITY_POINT =
            (BACKWARD_MULT_MAX - BACKWARD_MULT_MIN) / 19.0f;

        #endregion
    }

    /// <summary>
    /// Turning and momentum constants (§3.4).
    /// </summary>
    public static class TurnConstants
    {
        #region Fixed

        /// <summary>[FIXED] Turn rate at zero speed (°/s). Agent Movement #2 §3.4.2.</summary>
        public const float TURN_RATE_BASE = 720.0f;

        /// <summary>[FIXED] Minimum achievable turn rate (°/s). Agent Movement #2 §3.4.2.</summary>
        public const float TURN_RATE_FLOOR = 15.0f;

        /// <summary>[FIXED] Maximum achievable turn rate cap (°/s). Agent Movement #2 §3.4.2.</summary>
        public const float TURN_RATE_CAP = 720.0f;

        /// <summary>[FIXED] Maximum lean angle (degrees). Agent Movement #2 §3.4.</summary>
        public const float MAX_LEAN_ANGLE = 45.0f;

        #endregion

        #region GT

        /// <summary>[GT] Turn stiffness k at Agility 1 (stiffest). Agent Movement #2 §3.4.2.</summary>
        public static readonly float K_TURN_MAX = 0.78f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Turn stiffness k at Agility 20 (nimblest). Agent Movement #2 §3.4.2.</summary>
        public static readonly float K_TURN_MIN = 0.35f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Balance modifier minimum (Balance 1). Agent Movement #2 §3.4.2.</summary>
        public static readonly float BALANCE_MOD_MIN = 0.85f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Balance modifier maximum (Balance 20). Agent Movement #2 §3.4.2.</summary>
        public static readonly float BALANCE_MOD_MAX = 1.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Turn rate scale in DECELERATING state (fraction of normal). Agent Movement #2 §3.4.2.</summary>
        public static readonly float DECEL_TURN_MODIFIER = 0.60f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Safe fraction of max turn rate below which stumble risk is zero. Agent Movement #2 §3.4.4.</summary>
        public static readonly float SAFE_TURN_FRACTION = 0.70f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum stumble probability at full overshoot. Agent Movement #2 §3.4.4.</summary>
        public static readonly float MAX_STUMBLE_PROB = 0.30f; // TODO: replace with config loader (Stage 1)

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Turn stiffness k decrement per Agility point.
        /// Formula: (K_TURN_MAX - K_TURN_MIN) / 19. Agent Movement #2 §3.4.2.
        /// </summary>
        public static readonly float K_TURN_PER_POINT =
            (K_TURN_MAX - K_TURN_MIN) / 19.0f;

        /// <summary>
        /// [DERIVED] Balance modifier increment per Balance point.
        /// Formula: (BALANCE_MOD_MAX - BALANCE_MOD_MIN) / 19. Agent Movement #2 §3.4.2.
        /// </summary>
        public static readonly float BALANCE_MOD_PER_POINT =
            (BALANCE_MOD_MAX - BALANCE_MOD_MIN) / 19.0f;

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-22 | —      | Initial implementation. |
#endregion
