// File:     src/first-touch/FirstTouchConstants.cs
// Created:  2026-05-25
// Modified: 2026-05-26
// Author:   —
// Spec:     First Touch Mechanics #4 §3.1–§3.6, §6.1, Code Standards #20
// Purpose:  All constants for the first-touch system. No literals in formula code.

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// All tunable, derived, cross-spec, and physical constants for First Touch Mechanics.
    /// Region order: Fixed → Derived → Cross → GT. First Touch Mechanics #4 §6.1.
    /// </summary>
    public static class FirstTouchConstants
    {
        #region Fixed

        /// <summary>[FIXED] Sentinel value indicating no agent. First Touch Mechanics #4 §4.3.1.</summary>
        public const int AGENT_ID_NONE = -1;

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Half the pitch length (m).
        /// Formula: PitchLength × 0.5. First Touch Mechanics #4 §3.1.1.
        /// Source constants: BallPhysicsConstants.Pitch.LENGTH.
        /// </summary>
        public static readonly float PitchHalfLength = BallPhysicsConstants.Pitch.LENGTH * 0.5f;

        /// <summary>
        /// [DERIVED] Half the pitch width (m).
        /// Formula: PitchWidth × 0.5. First Touch Mechanics #4 §3.1.1.
        /// Source constants: BallPhysicsConstants.Pitch.WIDTH.
        /// </summary>
        public static readonly float PitchHalfWidth = BallPhysicsConstants.Pitch.WIDTH * 0.5f;

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS] Pitch length (m). Used for ball position clamping in §3.3.4.
        /// Authoritative source: BallPhysicsConstants.Pitch.LENGTH. Ball Physics #1 §1.2. Value: 105.0m.
        /// </summary>
        public static readonly float PitchLength = BallPhysicsConstants.Pitch.LENGTH;

        /// <summary>
        /// [CROSS] Pitch width (m). Used for ball position clamping in §3.3.4.
        /// Authoritative source: BallPhysicsConstants.Pitch.WIDTH. Ball Physics #1 §1.2. Value: 68.0m.
        /// </summary>
        public static readonly float PitchWidth = BallPhysicsConstants.Pitch.WIDTH;

        /// <summary>
        /// [CROSS] Ball radius (m), mirrored from Ball Physics for positional clamping.
        /// Authoritative source: BallPhysicsConstants.Ball.RADIUS. Ball Physics #1 §3.1.2. Value: 0.11m.
        /// </summary>
        public static readonly float BallRadius = BallPhysicsConstants.Ball.RADIUS;

        /// <summary>
        /// [CROSS] Maximum player attribute value.
        /// Authoritative source: PlayerAttributeConstants.AttributeMax. Agent Movement #2 §3.5.1. Value: 20.
        /// </summary>
        public static readonly float AttrMax = PlayerAttributeConstants.AttributeMax;

        #endregion

        #region GT

        /// <summary>[GT] Weight applied to the Technique attribute in control quality. First Touch Mechanics #4 §3.1.</summary>
        public static readonly float TechniqueWeight = 0.70f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Weight applied to the FirstTouch attribute in control quality. First Touch Mechanics #4 §3.1.</summary>
        public static readonly float FirstTouchWeight = 0.30f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Guard floor for uninitialised player attributes (int). First Touch Mechanics #4 §3.1.</summary>
        public static readonly int AttrMinGuard = 1; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Reference ball speed for difficulty scaling (m/s). First Touch Mechanics #4 §3.1.</summary>
        public static readonly float VelocityReference = 15.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum velocity difficulty multiplier. First Touch Mechanics #4 §3.1.</summary>
        public static readonly float VelocityMaxFactor = 4.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Lower clamp on velocity difficulty in §3.1.4. First Touch Mechanics #4 §3.1.4.</summary>
        public static readonly float VelocityDifficultyMin = 0.1f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum input ball speed guard (m/s). First Touch Mechanics #4 §3.1.</summary>
        public static readonly float VelocityMin = 0.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Reference agent movement speed for penalty scaling (m/s). First Touch Mechanics #4 §3.1.2.</summary>
        public static readonly float MovementReference = 7.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Penalty applied when agent is moving above MovementReference. First Touch Mechanics #4 §3.1.2.</summary>
        public static readonly float MovementPenalty = 0.50f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Orientation bonus added when agent is half-turn oriented. First Touch Mechanics #4 §3.6.</summary>
        public static readonly float HalfTurnBonus = 0.15f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum angle (degrees) for half-turn orientation detection. First Touch Mechanics #4 §3.6.</summary>
        public static readonly float HalfTurnAngleMin = 30.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum angle (degrees) for half-turn orientation detection. First Touch Mechanics #4 §3.6.</summary>
        public static readonly float HalfTurnAngleMax = 60.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Weight of pressure in final control quality formula. First Touch Mechanics #4 §3.1 Step 7.</summary>
        public static readonly float PressureWeight = 0.40f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Lower bound of the Good quality band; q ∈ [ControlledThreshold, QualityBandPerfect) → radius in [RadiusPerfect, RadiusGood]. Also the threshold above which outcome is CONTROLLED. First Touch Mechanics #4 §3.2, §3.4.</summary>
        public static readonly float ControlledThreshold = 0.60f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Lower bound of the Perfect quality band; q ∈ [QualityBandPerfect, 1] → radius in [RadiusMin, RadiusPerfect]. First Touch Mechanics #4 §3.2.</summary>
        public static readonly float QualityBandPerfect = 0.85f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Lower bound of the Poor quality band; q ∈ [QualityBandPoor, QualityBandGood) → radius in [RadiusGood, RadiusPoor]. First Touch Mechanics #4 §3.2.</summary>
        public static readonly float QualityBandPoor = 0.35f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Ball displacement radius for a CONTROLLED touch (m). First Touch Mechanics #4 §3.4.</summary>
        public static readonly float ControlledRadius = 0.60f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Displacement radius band for a perfect touch (m). First Touch Mechanics #4 §3.2.</summary>
        public static readonly float RadiusPerfect = 0.30f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Displacement radius band for a good touch (m). First Touch Mechanics #4 §3.2.</summary>
        public static readonly float RadiusGood = 0.60f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Displacement radius band for a poor touch (m). First Touch Mechanics #4 §3.2.</summary>
        public static readonly float RadiusPoor = 1.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Displacement radius band for a heavy touch (m). First Touch Mechanics #4 §3.2.</summary>
        public static readonly float RadiusHeavy = 2.00f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum achievable touch radius (m). First Touch Mechanics #4 §3.2.</summary>
        public static readonly float RadiusMin = 0.10f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Fraction of ball speed added to radius when ball is fast. First Touch Mechanics #4 §3.2.3.</summary>
        public static readonly float VelocityRadiusFactor = 0.25f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum dribble speed cap (m/s). First Touch Mechanics #4 §3.3.5.</summary>
        public static readonly float DribbleMaxSpeed = 5.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Fraction of incoming ball momentum retained during a general touch. First Touch Mechanics #4 §3.3.5.</summary>
        public static readonly float MomentumRetentionContact = 0.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum fraction of incoming ball speed the ball can exit at. First Touch Mechanics #4 §3.3.5.</summary>
        public static readonly float MomentumRetentionMax = 0.80f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Hard speed cap on any touch output ball velocity (m/s). First Touch Mechanics #4 §3.3.5.</summary>
        public static readonly float TouchMaxBallSpeed = 12.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum angular error in touch direction (degrees). First Touch Mechanics #4 §3.3.</summary>
        public static readonly float MaxTouchAngleError = 45.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum blend vector magnitude before fallback activates. First Touch Mechanics #4 §3.3.2.</summary>
        public static readonly float BlendMinMagnitude = 0.001f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Ball speed threshold at which thunderbolt cap applies (m/s). First Touch Mechanics #4 §3.3.7.</summary>
        public static readonly float ThunderboltSpeed = 28.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum control quality when receiving a thunderbolt. First Touch Mechanics #4 §3.3.7.</summary>
        public static readonly float ThunderboltQualityCap = 0.30f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Displacement radius threshold above which INTERCEPTION is checked (m). First Touch Mechanics #4 §3.4.2.</summary>
        public static readonly float InterceptionThreshold = 1.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Radius within which an opponent can intercept (m). First Touch Mechanics #4 §3.4.2.</summary>
        public static readonly float InterceptionRadius = 2.50f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Displacement radius threshold for DEFLECTION classification (m). First Touch Mechanics #4 §3.4.2.</summary>
        public static readonly float DeflectionThreshold = 1.50f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum dot product (cos 45°) for deflection momentum alignment. First Touch Mechanics #4 §3.4.2.</summary>
        public static readonly float DeflectionAlignmentMin = 0.70f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Displacement radius threshold for LOOSE_BALL classification (m). Equals ControlledRadius; named for clarity. First Touch Mechanics #4 §3.4.2.</summary>
        public static readonly float LooseBallThreshold = 0.60f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Radius within which opponents contribute to pressure (m). First Touch Mechanics #4 §3.5.</summary>
        public static readonly float PressureRadius = 3.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum opponent distance guard for inverse-square pressure (m). First Touch Mechanics #4 §3.5.2.</summary>
        public static readonly float MinPressureDistance = 0.3f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Saturation value above which pressure is clamped to 1. First Touch Mechanics #4 §3.5.3.</summary>
        public static readonly float PressureSaturation = 1.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Ball height threshold below which touch is treated as a ground touch (m). First Touch Mechanics #4 §3.4.3.</summary>
        public static readonly float GroundControlHeight = 0.50f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Ball displacement radius beyond which dribble attach is broken (m). First Touch Mechanics #4 §3.4.4.</summary>
        public static readonly float DribbleDetachRadius = 1.50f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Metre-scale epsilon for floating-point comparisons. First Touch Mechanics #4.</summary>
        public static readonly float ComparisonEpsilon = 0.0001f; // TODO: replace with config loader (Stage 1)

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                                                             |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                                                                    |
// | 1.1     | 2026-05-26 | —      | Adversarial review fixes: M-3 BallRadius moved from Derived→Cross; H-2 QualityBandPerfect 0.75→0.85, ControlledThreshold 0.55→0.60, QualityBandPoor 0.30→0.35. |
// | 1.2     | 2026-05-26 | —      | Adversarial review pass 2: Added PitchLength/PitchWidth [CROSS] constants (§3.3.4); removed dead constants InterceptionQualityMin (unused, no §3.4.2 backing) and MomentumRetentionDeflection (unused, §3.3.6 does not exist in spec). |
#endregion
