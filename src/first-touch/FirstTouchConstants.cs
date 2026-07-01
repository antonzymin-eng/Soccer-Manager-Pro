// File:     src/first-touch/FirstTouchConstants.cs
// Created:  2026-05-25
// Modified: 2026-06-10
// Author:   —
// Spec:     First Touch Mechanics #4 §3.1–§3.6, §6.1, Code Standards #20
// Purpose:  All constants for the first-touch system. No literals in formula code.

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// All tunable, cross-spec, and physical constants for First Touch Mechanics.
    /// Region order: Fixed → Cross → GT. First Touch Mechanics #4 §6.1.
    /// </summary>
    public static class FirstTouchConstants
    {
        #region Fixed

        /// <summary>[FIXED] Sentinel value indicating no agent. First Touch Mechanics #4 §4.3.1.</summary>
        public const int AGENT_ID_NONE = -1;

        /// <summary>[FIXED] Minimum vector magnitude before numerical fallback activates. Numerical stability guard — not designer-tunable. First Touch Mechanics #4 §3.3.2.</summary>
        public const float BLEND_MIN_MAGNITUDE = 0.001f;

        /// <summary>[FIXED] Square of BLEND_MIN_MAGNITUDE; cached to avoid repeated multiplications in sqrMagnitude predicates. First Touch Mechanics #4 §3.3.2.</summary>
        public const float BLEND_MIN_MAGNITUDE_SQ = BLEND_MIN_MAGNITUDE * BLEND_MIN_MAGNITUDE;

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

        /// <summary>
        /// [CROSS] Maximum ball-centre height (m) for ground control / First Touch eligibility.
        /// Ball above this height is routed to Heading Mechanics (#10); ball at or below is processed by First Touch §3.4.3.
        /// Authoritative source: BallPhysicsConstants.Possession.ControlHeight. Ball Physics #1 §3.1.11. Value: 0.50m.
        /// </summary>
        public static readonly float GroundControlHeight = BallPhysicsConstants.Possession.ControlHeight;

        #endregion

        #region GT

        /// <summary>[GT] Weight applied to the Technique attribute in control quality. First Touch Mechanics #4 §3.1.</summary>
        public static readonly float TechniqueWeight = Config.GetFloat("first-touch", "TechniqueWeight", 0.70f);

        /// <summary>[GT] Weight applied to the FirstTouch attribute in control quality. First Touch Mechanics #4 §3.1.</summary>
        public static readonly float FirstTouchWeight = Config.GetFloat("first-touch", "FirstTouchWeight", 0.30f);

        /// <summary>[GT] Guard floor for uninitialised player attributes (int). First Touch Mechanics #4 §3.1.</summary>
        public static readonly int AttrMinGuard = Config.GetInt("first-touch", "AttrMinGuard", 1);

        /// <summary>[GT] Reference ball speed for difficulty scaling (m/s). First Touch Mechanics #4 §3.1.</summary>
        public static readonly float VelocityReference = Config.GetFloat("first-touch", "VelocityReference", 15.0f);

        /// <summary>[GT] Maximum velocity difficulty multiplier. First Touch Mechanics #4 §3.1.</summary>
        public static readonly float VelocityMaxFactor = Config.GetFloat("first-touch", "VelocityMaxFactor", 4.0f);

        /// <summary>[GT] Lower clamp on velocity difficulty in §3.1.4. First Touch Mechanics #4 §3.1.4.</summary>
        public static readonly float VelocityDifficultyMin = Config.GetFloat("first-touch", "VelocityDifficultyMin", 0.1f);

        /// <summary>[GT] Minimum input ball speed guard (m/s). First Touch Mechanics #4 §3.1.</summary>
        public static readonly float VelocityMin = Config.GetFloat("first-touch", "VelocityMin", 0.5f);

        /// <summary>[GT] Reference agent movement speed for penalty scaling (m/s). First Touch Mechanics #4 §3.1.2.</summary>
        public static readonly float MovementReference = Config.GetFloat("first-touch", "MovementReference", 7.0f);

        /// <summary>[GT] Penalty applied when agent is moving above MovementReference. First Touch Mechanics #4 §3.1.2.</summary>
        public static readonly float MovementPenalty = Config.GetFloat("first-touch", "MovementPenalty", 0.50f);

        /// <summary>[GT] Orientation bonus added when agent is half-turn oriented. First Touch Mechanics #4 §3.6.</summary>
        public static readonly float HalfTurnBonus = Config.GetFloat("first-touch", "HalfTurnBonus", 0.15f);

        /// <summary>[GT] Minimum angle (degrees) for half-turn orientation detection. First Touch Mechanics #4 §3.6.</summary>
        public static readonly float HalfTurnAngleMin = Config.GetFloat("first-touch", "HalfTurnAngleMin", 30.0f);

        /// <summary>[GT] Maximum angle (degrees) for half-turn orientation detection. First Touch Mechanics #4 §3.6.</summary>
        public static readonly float HalfTurnAngleMax = Config.GetFloat("first-touch", "HalfTurnAngleMax", 60.0f);

        /// <summary>[GT] Weight of pressure in final control quality formula. First Touch Mechanics #4 §3.1 Step 7.</summary>
        public static readonly float PressureWeight = Config.GetFloat("first-touch", "PressureWeight", 0.40f);

        /// <summary>[GT] Lower bound of the Good quality band; q ∈ [ControlledThreshold, QualityBandPerfect) → radius in [RadiusPerfect, RadiusGood]. NOTE: the §3.4.2 CONTROLLED outcome is classified on displacement radius r (see LooseBallThreshold), not on q — the §3.2.3 velocity modifier can push r past 0.60 m even at q above this threshold. First Touch Mechanics #4 §3.2, §3.4.</summary>
        public static readonly float ControlledThreshold = Config.GetFloat("first-touch", "ControlledThreshold", 0.60f);

        /// <summary>[GT] Lower bound of the Perfect quality band; q ∈ [QualityBandPerfect, 1] → radius in [RadiusMin, RadiusPerfect]. First Touch Mechanics #4 §3.2.</summary>
        public static readonly float QualityBandPerfect = Config.GetFloat("first-touch", "QualityBandPerfect", 0.85f);

        /// <summary>[GT] Lower bound of the Poor quality band; q ∈ [QualityBandPoor, ControlledThreshold) → radius in [RadiusGood, RadiusPoor]. First Touch Mechanics #4 §3.2.</summary>
        public static readonly float QualityBandPoor = Config.GetFloat("first-touch", "QualityBandPoor", 0.35f);

        /// <summary>[GT] Ball displacement radius for a CONTROLLED touch (m). First Touch Mechanics #4 §3.4 / appendix B.
        /// STAGE 0 NOTE (AR-7 L-2): declared-but-unconsumed — the §3.4.2 classifier consumes LooseBallThreshold
        /// (same value, named for the classification role); retained for the §4.4 contract surface. Tune the two together.</summary>
        public static readonly float ControlledRadius = Config.GetFloat("first-touch", "ControlledRadius", 0.60f);

        /// <summary>[GT] Displacement radius band for a perfect touch (m). First Touch Mechanics #4 §3.2.</summary>
        public static readonly float RadiusPerfect = Config.GetFloat("first-touch", "RadiusPerfect", 0.30f);

        /// <summary>[GT] Displacement radius band for a good touch (m). First Touch Mechanics #4 §3.2.</summary>
        public static readonly float RadiusGood = Config.GetFloat("first-touch", "RadiusGood", 0.60f);

        /// <summary>[GT] Displacement radius band for a poor touch (m). First Touch Mechanics #4 §3.2.</summary>
        public static readonly float RadiusPoor = Config.GetFloat("first-touch", "RadiusPoor", 1.20f);

        /// <summary>[GT] Displacement radius band for a heavy touch (m). First Touch Mechanics #4 §3.2.</summary>
        public static readonly float RadiusHeavy = Config.GetFloat("first-touch", "RadiusHeavy", 2.00f);

        /// <summary>[GT] Minimum achievable touch radius (m). First Touch Mechanics #4 §3.2.</summary>
        public static readonly float RadiusMin = Config.GetFloat("first-touch", "RadiusMin", 0.10f);

        /// <summary>[GT] Fraction of ball speed added to radius when ball is fast. First Touch Mechanics #4 §3.2.3.</summary>
        public static readonly float VelocityRadiusFactor = Config.GetFloat("first-touch", "VelocityRadiusFactor", 0.25f);

        /// <summary>[GT] Maximum dribble speed cap (m/s). First Touch Mechanics #4 §3.3.5.</summary>
        public static readonly float DribbleMaxSpeed = Config.GetFloat("first-touch", "DribbleMaxSpeed", 5.5f);

        /// <summary>[GT] Fraction of incoming ball momentum retained during a general touch. First Touch Mechanics #4 §3.3.5.</summary>
        public static readonly float MomentumRetentionContact = Config.GetFloat("first-touch", "MomentumRetentionContact", 0.5f);

        /// <summary>[GT] Hard speed cap on any touch output ball velocity (m/s). First Touch Mechanics #4 §3.3.5.</summary>
        public static readonly float TouchMaxBallSpeed = Config.GetFloat("first-touch", "TouchMaxBallSpeed", 12.0f);

        /// <summary>[GT] Ball speed threshold at which thunderbolt cap applies (m/s). First Touch Mechanics #4 §3.3.7.</summary>
        public static readonly float ThunderboltSpeed = Config.GetFloat("first-touch", "ThunderboltSpeed", 28.0f);

        /// <summary>[GT] Maximum control quality when receiving a thunderbolt. First Touch Mechanics #4 §3.3.7.</summary>
        public static readonly float ThunderboltQualityCap = Config.GetFloat("first-touch", "ThunderboltQualityCap", 0.30f);

        /// <summary>[GT] Displacement radius threshold above which INTERCEPTION is checked (m). First Touch Mechanics #4 §3.4.2.</summary>
        public static readonly float InterceptionThreshold = Config.GetFloat("first-touch", "InterceptionThreshold", 1.20f);

        /// <summary>[GT] Radius within which an opponent can intercept (m). First Touch Mechanics #4 §3.4.2.</summary>
        public static readonly float InterceptionRadius = Config.GetFloat("first-touch", "InterceptionRadius", 2.50f);

        /// <summary>[GT] Displacement radius threshold for DEFLECTION classification (m). First Touch Mechanics #4 §3.4.2.</summary>
        public static readonly float DeflectionThreshold = Config.GetFloat("first-touch", "DeflectionThreshold", 1.50f);

        /// <summary>[GT] Minimum dot product (cos 45°) for deflection momentum alignment. First Touch Mechanics #4 §3.4.2.</summary>
        public static readonly float DeflectionAlignmentMin = Config.GetFloat("first-touch", "DeflectionAlignmentMin", 0.70f);

        /// <summary>[GT] Displacement radius threshold for LOOSE_BALL classification (m). Equals ControlledRadius; named for clarity. First Touch Mechanics #4 §3.4.2.</summary>
        public static readonly float LooseBallThreshold = Config.GetFloat("first-touch", "LooseBallThreshold", 0.60f);

        /// <summary>[GT] Radius within which opponents contribute to pressure (m). First Touch Mechanics #4 §3.5.</summary>
        public static readonly float PressureRadius = Config.GetFloat("first-touch", "PressureRadius", 3.0f);

        /// <summary>[GT] Minimum opponent distance guard for inverse-square pressure (m). First Touch Mechanics #4 §3.5.2.</summary>
        public static readonly float MinPressureDistance = Config.GetFloat("first-touch", "MinPressureDistance", 0.3f);

        /// <summary>[GT] Saturation value above which pressure is clamped to 1. First Touch Mechanics #4 §3.5.3.</summary>
        public static readonly float PressureSaturation = Config.GetFloat("first-touch", "PressureSaturation", 1.5f);

        /// <summary>[GT] Ball displacement radius beyond which dribble attach is broken (m). First Touch Mechanics #4 §3.4.4.
        /// STAGE 0 NOTE (AR-7 L-2): declared-but-unconsumed — the §3.4.4 dribble-detach monitor runs in the
        /// possession-update loop, which is a Stage 0+1 deliverable; this catalogue row is its landing site.</summary>
        public static readonly float DribbleDetachRadius = Config.GetFloat("first-touch", "DribbleDetachRadius", 1.50f);

        /// <summary>[GT] L_rec multiplier applied when agent is in half-turn stance and the target entity falls in the peripheral arc (40°–80°). Value = 1 − 0.15 (15% reduction matches HalfTurnBonus). First Touch Mechanics #4 §3.3.2. Consumed by Perception System #7 §3.3.3.</summary>
        public static readonly float HalfTurnLRecReduction = Config.GetFloat("first-touch", "HalfTurnLRecReduction", 0.85f);

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                                                             |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                                                                    |
// | 1.1     | 2026-05-26 | —      | Adversarial review fixes: M-3 BallRadius moved from Derived→Cross; H-2 QualityBandPerfect 0.75→0.85, ControlledThreshold 0.55→0.60, QualityBandPoor 0.30→0.35. |
// | 1.2     | 2026-05-26 | —      | Adversarial review pass 2: Added PitchLength/PitchWidth [CROSS] constants (§3.3.4); removed dead constants InterceptionQualityMin (unused, no §3.4.2 backing) and MomentumRetentionDeflection (unused, §3.3.6 does not exist in spec). |
// | 1.3     | 2026-05-26 | —      | Adversarial review pass 3: Removed dead constants MomentumRetentionMax (no spec §3.3.5 formula backing), MaxTouchAngleError (spec uses vector blend, not angle cap), ComparisonEpsilon (BlendMinMagnitude serves the role). Fixed PitchHalfLength/PitchHalfWidth doc to correctly cite BallPhysicsConstants.Pitch const (not PitchLength readonly) to avoid static initialisation order dependency. |
// | 1.4     | 2026-05-26 | —      | Adversarial review pass 4: Fixed QualityBandPoor doc ("QualityBandGood" → "ControlledThreshold"); changed BlendMinMagnitude tag from [GT] to [FIXED] (numerical stability guard, not designer-tunable). |
// | 1.4.1   | 2026-05-28 | —      | AR-1 fix: added HalfTurnLRecReduction constant (GT, 0.85f) to satisfy Perception System #7 §3.3.3 CROSS-tag contract. (Renumbered from duplicate 1.1 in v1.5 audit.) |
// | 1.5     | 2026-06-06 | —      | AR-5 M-2: BlendMinMagnitude relocated from end-of-GT region to #region Fixed, renamed BLEND_MIN_MAGNITUDE (ALL_CAPS per FR-CS-001 for [FIXED]), retyped `static readonly` → `const` to match AGENT_ID_NONE, stale "TODO: replace with config loader" comment dropped (FIXED is not designer-tunable). L-1: duplicate v1.1 row reconciled — earlier May-28 HalfTurnLRecReduction addition retroactively renumbered v1.4.1 to restore monotonic ordering. |
// | 1.6     | 2026-06-06 | —      | AR-6 L-1: added BLEND_MIN_MAGNITUDE_SQ compile-time const so callers (BallDisplacementProcessor, OrientationDetector, PossessionStateMachine) consume a single cached square instead of recomputing the product across 6 call sites. |
// | 1.7     | 2026-06-08 | —      | Cross-spec routing close-out (Spec #20 §4.2): GroundControlHeight relocated from #region GT to #region Cross, retagged [CROSS], and now mirrors BallPhysicsConstants.Possession.ControlHeight (Ball Physics #1 §3.1.11) verbatim. Closes the long-standing CLAUDE.md OPEN ISSUE "Possession.ControlHeight ↔ GroundControlHeight cross-spec routing" (since 2026-06-03). Ball Physics #1 is the authority because ControlHeight is a physical possession-geometry constant living next to the three sibling thresholds (ControlRadius / ControlVelocity / ChallengeRadius); First Touch's §3.4.3 use is a routing guard, not an authority claim. Designers now tune the single Ball Physics value; mirror tracks automatically. |
// | 1.8     | 2026-06-10 | —      | AR-7 L-1: ControlledThreshold doc no longer claims to be the CONTROLLED outcome gate (classification is r-based per §3.4.2; the velocity modifier breaks the q↔r coincidence). AR-7 L-2: dead [DERIVED] PitchHalfLength / PitchHalfWidth removed (orphaned since BallDisplacementProcessor v1.2 switched to PitchLength / PitchWidth; #region Derived dropped — empty regions prohibited); spec-backed ControlledRadius + DribbleDetachRadius doc-noted declared-but-unconsumed at Stage 0 per Collision AR-10 MaxIterations precedent. |
#endregion
