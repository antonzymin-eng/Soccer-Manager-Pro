// File:     src/agent-movement/AgentMovementConstants.cs
// Created:  2026-05-22
// Modified: 2026-05-26
// Author:   —
// Spec:     Agent Movement #2 §3.1–§3.4, §4.1.3, §4.3.1, Code Standards #20
// Purpose:  All constants for the agent movement system. No literals in formula code.

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Speed thresholds, energy gate constants, stumble parameters, and oscillation guard
    /// constants for the movement state machine (§3.1.3 / §3.1.5 / §3.1.7).
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
        public static readonly float IdleEnter = 0.1f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Speed above which agent exits IDLE (m/s). Agent Movement #2 §3.1.3.</summary>
        public static readonly float IdleExit = 0.3f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Speed above which agent enters JOGGING (m/s). Agent Movement #2 §3.1.3.</summary>
        public static readonly float JogEnter = 2.2f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Speed below which agent exits JOGGING (m/s). Agent Movement #2 §3.1.3.</summary>
        public static readonly float JogExit = 1.9f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Speed above which agent enters SPRINTING (m/s). Agent Movement #2 §3.1.3.</summary>
        public static readonly float SprintEnter = 5.8f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Speed below which agent exits SPRINTING (m/s). Agent Movement #2 §3.1.3.</summary>
        public static readonly float SprintExit = 5.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum speed at which a stumble can occur (m/s). Agent Movement #2 §3.1.3.</summary>
        public static readonly float StumbleSpeedThreshold = 2.2f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Turn angle (degrees) that triggers a stumble check when at speed. Agent Movement #2 §3.4.4.</summary>
        public static readonly float StumbleTurnAngle = 60.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum stumble risk floor; prevents elite players being physically immune. Agent Movement #2 §3.1.5.</summary>
        public static readonly float MinStumbleRisk = 0.03f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Sprint reservoir below which SPRINTING is forced to JOGGING. Agent Movement #2 §3.1.3.</summary>
        public static readonly float SprintReservoirFloor = 0.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Sprint reservoir required to re-enter SPRINTING from JOGGING. Agent Movement #2 §3.1.3.</summary>
        public static readonly float SprintReservoirReentry = 0.35f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aerobic pool below which JOGGING is forced to DECELERATING. Agent Movement #2 §3.1.3.</summary>
        public static readonly float AerobicJogFloor = 0.15f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aerobic modifier applied at low pool levels. Agent Movement #2 §3.1.3.</summary>
        public static readonly float AerobicModifierFloor = 0.70f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Aerobic pool level at which modifier begins degrading. Agent Movement #2 §3.1.3.</summary>
        public static readonly float AerobicModifierThreshold = 0.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Base dwell time for STUMBLING recovery (seconds). Agent Movement #2 §3.1.5.</summary>
        public static readonly float StumbleMinDwellBase = 0.6f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Base dwell time for GROUNDED recovery (seconds). Agent Movement #2 §3.1.5.</summary>
        public static readonly float GroundedMinDwellBase = 1.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Force-scale minimum for collision dwell calculation (light nudge factor). Agent Movement #2 §3.1.5.</summary>
        public static readonly float CollisionDwellMin = 0.65f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Max state transitions per second before oscillation guard activates. Agent Movement #2 §3.1.7.</summary>
        public static readonly int MaxTransitionsPerSecond = 6; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Difficulty scalar in stumble risk formula. Agent Movement #2 §3.1.5.</summary>
        public static readonly float StumbleDifficultyFactor = 1.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Speed hysteresis band (m/s) used in command-speed vs current-speed comparisons. Agent Movement #2 §3.1.4.</summary>
        public static readonly float CommandSpeedHysteresis = 0.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Effective stopping distance (m) applied during STUMBLING deceleration. Agent Movement #2 §3.2.5.</summary>
        public static readonly float StumbleDecelerationDistance = 3.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum STUMBLING recovery dwell clamp (seconds). Agent Movement #2 §3.1.5.</summary>
        public static readonly float StumbleDwellClampMin = 0.3f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum STUMBLING recovery dwell clamp (seconds). Agent Movement #2 §3.1.5.</summary>
        public static readonly float StumbleDwellClampMax = 1.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum GROUNDED recovery dwell clamp (seconds). Agent Movement #2 §3.1.5.</summary>
        public static readonly float GroundedDwellClampMin = 0.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum GROUNDED recovery dwell clamp (seconds). Agent Movement #2 §3.1.5.</summary>
        public static readonly float GroundedDwellClampMax = 2.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Dwell multiplier for GROUNDED entered via sliding tackle. Agent Movement #2 §3.1.5.</summary>
        public static readonly float SlidingTackleDwellMult = 0.6f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Dwell multiplier for GROUNDED entered via diving header. Agent Movement #2 §3.1.5.</summary>
        public static readonly float DivingHeaderDwellMult = 0.7f; // TODO: replace with config loader (Stage 1)

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

        /// <summary>[FIXED] Walking state linear acceleration rate (m/s²). Agent Movement #2 §3.2.4.</summary>
        public const float WALK_ACCELERATION = 2.0f;

        /// <summary>[FIXED] Walking state linear deceleration rate (m/s²). Agent Movement #2 §3.2.4.</summary>
        public const float WALK_DECELERATION = 2.5f;

        /// <summary>[FIXED] Kinematic equation divisor for v²-based deceleration: a = v²/(2d). Agent Movement #2 §3.2.5.</summary>
        public const float KINEMATIC_HALF = 2.0f;

        #endregion

        #region Derived

        // Derived from [FIXED] sources; safe to place before GT per region ordering rule.

        /// <summary>
        /// [DERIVED] Top speed increment per Pace attribute point.
        /// Formula: (TOP_SPEED_MAX - TOP_SPEED_MIN) / 19. FM-NNN. Agent Movement #2 §3.2.4.
        /// Source constants: LocomotionConstants.TOP_SPEED_MIN, LocomotionConstants.TOP_SPEED_MAX.
        /// </summary>
        public static readonly float TopSpeedPerPacePoint =
            (TOP_SPEED_MAX - TOP_SPEED_MIN) / PlayerAttributeConstants.AttributeRangeSpan;

        #endregion

        #region GT

        /// <summary>[GT] Exponential acceleration k at Acceleration attribute 1 (s⁻¹). Agent Movement #2 §3.2.3.</summary>
        public static readonly float AccelKMin = 0.658f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Exponential acceleration k at Acceleration attribute 20 (s⁻¹). Agent Movement #2 §3.2.3.</summary>
        public static readonly float AccelKMax = 0.921f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Controlled deceleration stopping distance at Pace 1 (m). Agent Movement #2 §3.2.5.</summary>
        public static readonly float ControlledDecelDistMin = 3.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Controlled deceleration stopping distance at Pace 20 (m). Agent Movement #2 §3.2.5.</summary>
        public static readonly float ControlledDecelDistMax = 5.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Emergency deceleration stopping distance at Pace 1 (m). Agent Movement #2 §3.2.5.</summary>
        public static readonly float EmergencyDecelDistMin = 2.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Emergency deceleration stopping distance at Pace 20 (m). Agent Movement #2 §3.2.5.</summary>
        public static readonly float EmergencyDecelDistMax = 3.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum stopping distance guard to prevent division by near-zero (m). Agent Movement #2 §3.2.5.</summary>
        public static readonly float MinStoppingDistanceM = 0.1f; // TODO: replace with config loader (Stage 1)

        #endregion

        #region Derived

        // [DERIVED] from [GT] sources; declared after GT to ensure correct C# static field initialization order.

        /// <summary>
        /// [DERIVED] Acceleration k increment per Acceleration attribute point.
        /// Formula: (AccelKMax - AccelKMin) / 19. Agent Movement #2 §3.2.3.
        /// Source constants: LocomotionConstants.AccelKMin, LocomotionConstants.AccelKMax.
        /// </summary>
        public static readonly float AccelKPerPoint =
            (AccelKMax - AccelKMin) / PlayerAttributeConstants.AttributeRangeSpan;

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
        public static readonly float LateralMultMin = 0.65f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Lateral multiplier at Agility 20. Agent Movement #2 §3.3.2.</summary>
        public static readonly float LateralMultMax = 0.75f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Backward multiplier at Agility 1. Agent Movement #2 §3.3.2.</summary>
        public static readonly float BackwardMultMin = 0.45f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Backward multiplier at Agility 20. Agent Movement #2 §3.3.2.</summary>
        public static readonly float BackwardMultMax = 0.55f; // TODO: replace with config loader (Stage 1)

        #endregion

        #region Derived

        // [DERIVED] from [GT] sources; declared after GT to ensure correct C# static field initialization order.

        /// <summary>
        /// [DERIVED] Lateral multiplier increment per Agility point.
        /// Formula: (LateralMultMax - LateralMultMin) / 19. Agent Movement #2 §3.3.2.
        /// Source constants: DirectionalConstants.LateralMultMin, DirectionalConstants.LateralMultMax.
        /// </summary>
        public static readonly float LateralMultPerAgilityPoint =
            (LateralMultMax - LateralMultMin) / PlayerAttributeConstants.AttributeRangeSpan;

        /// <summary>
        /// [DERIVED] Backward multiplier increment per Agility point.
        /// Formula: (BackwardMultMax - BackwardMultMin) / 19. Agent Movement #2 §3.3.2.
        /// Source constants: DirectionalConstants.BackwardMultMin, DirectionalConstants.BackwardMultMax.
        /// </summary>
        public static readonly float BackwardMultPerAgilityPoint =
            (BackwardMultMax - BackwardMultMin) / PlayerAttributeConstants.AttributeRangeSpan;

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

        /// <summary>[FIXED] Standard gravity (m/s²). ISO 80000-3:2019. Agent Movement #2 §3.4.</summary>
        public const float GRAVITY_MAGNITUDE = 9.81f;

        /// <summary>[FIXED] Degrees in a half-rotation (full reversal). Agent Movement #2 §3.4.4.</summary>
        public const float HALF_ROTATION_DEG = 180.0f;

        /// <summary>[FIXED] Dimensionless offset in the turn-rate velocity denominator. Agent Movement #2 §3.4.2.</summary>
        public const float TURN_RATE_VELOCITY_OFFSET = 1.0f;

        /// <summary>[FIXED] Minimum divisor guard (degrees) in the stumble overshoot denominator. Agent Movement #2 §3.4.4.</summary>
        public const float MIN_TURN_RATE_DIVISOR = 1.0f;

        /// <summary>[FIXED] Turn-rate near-zero guard (°/s) — values below this are treated as zero. Agent Movement #2 §3.4.3.</summary>
        public const float TURN_RATE_EPSILON_DEG = 1e-4f;

        #endregion

        #region GT

        /// <summary>[GT] Turn stiffness k at Agility 1 (stiffest). Agent Movement #2 §3.4.2.</summary>
        public static readonly float KTurnMax = 0.78f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Turn stiffness k at Agility 20 (nimblest). Agent Movement #2 §3.4.2.</summary>
        public static readonly float KTurnMin = 0.35f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Balance modifier minimum (Balance 1). Agent Movement #2 §3.4.2.</summary>
        public static readonly float BalanceModMin = 0.85f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Balance modifier maximum (Balance 20). Agent Movement #2 §3.4.2.</summary>
        public static readonly float BalanceModMax = 1.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Turn rate scale in DECELERATING state (fraction of normal). Agent Movement #2 §3.4.2.</summary>
        public static readonly float DecelTurnModifier = 0.60f; // TODO: replace with config loader (Stage 1)

        /// <summary>
        /// [GT] Safe fraction of max turn rate below which stumble risk is zero. Agent Movement #2 §3.4.4.
        /// Reserved for Stage 1+ probabilistic stumble (SplitMix64 required). Currently unused;
        /// Stage 0 uses the deterministic AgentStateMachine.ShouldStumble formula instead.
        /// </summary>
        public static readonly float SafeTurnFraction = 0.70f; // TODO: replace with config loader (Stage 1)

        /// <summary>
        /// [GT] Maximum stumble probability at full overshoot. Agent Movement #2 §3.4.4.
        /// Reserved for Stage 1+ probabilistic stumble (SplitMix64 required). Currently unused;
        /// Stage 0 uses the deterministic AgentStateMachine.ShouldStumble formula instead.
        /// </summary>
        public static readonly float MaxStumbleProb = 0.30f; // TODO: replace with config loader (Stage 1)

        #endregion

        #region Derived

        // [DERIVED] from [GT] sources; declared after GT to ensure correct C# static field initialization order.

        /// <summary>
        /// [DERIVED] Turn stiffness k decrement per Agility point.
        /// Formula: (KTurnMax - KTurnMin) / 19. Agent Movement #2 §3.4.2.
        /// Source constants: TurnConstants.KTurnMax, TurnConstants.KTurnMin.
        /// </summary>
        public static readonly float KTurnPerPoint =
            (KTurnMax - KTurnMin) / PlayerAttributeConstants.AttributeRangeSpan;

        /// <summary>
        /// [DERIVED] Balance modifier increment per Balance point.
        /// Formula: (BalanceModMax - BalanceModMin) / 19. Agent Movement #2 §3.4.2.
        /// Source constants: TurnConstants.BalanceModMin, TurnConstants.BalanceModMax.
        /// </summary>
        public static readonly float BalanceModPerPoint =
            (BalanceModMax - BalanceModMin) / PlayerAttributeConstants.AttributeRangeSpan;

        #endregion
    }

    /// <summary>
    /// Oscillation guard constants for the state machine anti-thrash mechanism (§3.1.7).
    /// </summary>
    public static class OscillationGuardConstants
    {
        #region Fixed

        /// <summary>
        /// [FIXED] Ring buffer capacity for transition timestamps. Agent Movement #2 §3.1.7.
        /// MUST match the number of _t0.._tN fields declared in OscillationGuard struct.
        /// Changing this value requires a corresponding change to OscillationGuard's field declarations.
        /// Tagged [FIXED] (not [GT]) because the struct field count is a compile-time constraint.
        /// </summary>
        public const int BufferSize = 8;

        #endregion

        #region GT

        /// <summary>[GT] Duration (seconds) for which state transitions are locked after oscillation is detected. Agent Movement #2 §3.1.7.</summary>
        public static readonly float LockDuration = 0.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Rolling window (seconds) over which transitions are counted. Agent Movement #2 §3.1.7.</summary>
        public static readonly float WindowSeconds = 1.0f; // TODO: replace with config loader (Stage 1)

        #endregion
    }

    /// <summary>
    /// Attribute range constants shared by all locomotion formulas.
    /// All player attributes are integers in [1, 20] per Agent Movement #2 §3.5.1.
    /// </summary>
    public static class PlayerAttributeConstants
    {
        #region Fixed

        /// <summary>[FIXED] Minimum player attribute value. Agent Movement #2 §3.5.1.</summary>
        public const float AttributeMin = 1.0f;

        /// <summary>[FIXED] Integer form of AttributeMin for use in integer-attribute arithmetic. Agent Movement #2 §3.5.1.</summary>
        public const int AttributeMinInt = 1;

        /// <summary>[FIXED] Maximum player attribute value. Agent Movement #2 §3.5.1.</summary>
        public const float AttributeMax = 20.0f;

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Span of the attribute range (max − min).
        /// Formula: AttributeMax - AttributeMin. Used to normalise per-point increments.
        /// Source constants: PlayerAttributeConstants.AttributeMax, PlayerAttributeConstants.AttributeMin.
        /// </summary>
        public static readonly float AttributeRangeSpan = AttributeMax - AttributeMin;

        /// <summary>
        /// [DERIVED] Maximum sum of two independent attributes (used as normalisation denominator).
        /// Formula: 2 × AttributeMax. Agent Movement #2 §3.1.5.
        /// Source constants: PlayerAttributeConstants.AttributeMax.
        /// </summary>
        public static readonly float AttributePairMax = 2.0f * AttributeMax;

        /// <summary>
        /// [DERIVED] Near-zero floor applied to attribute-derived denominators to prevent divide-by-zero.
        /// Formula: AttributeMin / AttributeMax. Agent Movement #2 §3.1.5.
        /// Source constants: PlayerAttributeConstants.AttributeMin, PlayerAttributeConstants.AttributeMax.
        /// </summary>
        public static readonly float AttributeNearZeroFloor = AttributeMin / AttributeMax;

        #endregion
    }

    /// <summary>
    /// Safety system boundary constants (§4.1.3, §4.3.1).
    /// Pitch dimensions are cross-spec from Ball Physics #1 §1.2.
    /// </summary>
    public static class SafetyConstants
    {
        #region Fixed

        /// <summary>[FIXED] Squared-magnitude threshold below which a 2-D vector is treated as effectively zero. Agent Movement #2 §4.3.1.</summary>
        public const float VELOCITY_SQR_MAGNITUDE_EPSILON = 1e-6f;

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS] Pitch length along the X axis (metres).
        /// Authoritative source: Ball Physics #1 §1.2 coordinate system. Value: 105 m.
        /// </summary>
        public static readonly float PitchLengthX = 105.0f;

        /// <summary>
        /// [CROSS] Pitch width along the Y axis (metres).
        /// Authoritative source: Ball Physics #1 §1.2 coordinate system. Value: 68 m.
        /// </summary>
        public static readonly float PitchWidthY = 68.0f;

        #endregion

        #region GT

        /// <summary>[GT] Exterior buffer (metres) outside pitch bounds before position is clamped. Covers goal area and corner flag space. Agent Movement #2 §4.3.1.</summary>
        public static readonly float PitchBoundaryBuffer = 5.0f; // TODO: replace with config loader (Stage 1)

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                            |
// | 1.0     | 2026-05-22 | —      | Initial implementation.                                                                          |
// | 1.1     | 2026-05-25 | —      | Pass-1 fix: H-3 StumbleDifficultyFactor; H-4 OscillationGuardConstants class; L-1 PascalCase;   |
// |         |            |        | M-2 SafetyConstants; M-4 CommandSpeedHysteresis; M-5 StumbleDecelerationDistance; M-6 double     |
// |         |            |        | #region resolved; H-2 namespace → TacticalDirector.AgentMovement / src/agent-movement/.          |
// | 1.2     | 2026-05-25 | —      | Pass-2 fix: H-1 OscillationGuardConstants.BufferSize → [FIXED] const; M-1 dwell clamps /        |
// |         |            |        | reason multipliers / MinStoppingDistanceM promoted. Pass-3: PlayerAttributeConstants class added  |
// |         |            |        | (AttributeMin, AttributeMax, AttributeRangeSpan, AttributePairMax, AttributeNearZeroFloor);       |
// |         |            |        | all derived-constant divisors updated from 19.0f literal to PlayerAttributeConstants.AttributeRangeSpan. |
// | 1.3     | 2026-05-25 | —      | Pass-4 fix: TurnConstants.GRAVITY_MAGNITUDE, HALF_ROTATION_DEG, TURN_RATE_VELOCITY_OFFSET,        |
// |         |            |        | MIN_TURN_RATE_DIVISOR, TURN_RATE_EPSILON_DEG added [FIXED]. LocomotionConstants.WALK_ACCELERATION, |
// |         |            |        | WALK_DECELERATION, KINEMATIC_HALF added [FIXED]. SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON   |
// |         |            |        | added [FIXED]. PlayerAttributeConstants.AttributeMinInt added [FIXED].                           |
// | 1.4     | 2026-05-26 | —      | AR-2 fix: M-4 three #region DerivedFromGT renamed to #region Derived (non-standard name).       |
// |         |            |        | M-3 AttributeNearZeroFloor promoted [GT]→[DERIVED] (formula: AttributeMin/AttributeMax);         |
// |         |            |        | GT region removed (empty). Modified date updated.                                              |
// | 1.5     | 2026-05-26 | —      | AR-2 fix (continued): M-2 SafeTurnFraction/MaxStumbleProb XML doc updated to note they are       |
// |         |            |        | reserved for Stage 1+ probabilistic stumble; unused at Stage 0 (deterministic ShouldStumble).   |
#endregion
