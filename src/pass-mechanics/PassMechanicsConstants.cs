// File:     src/pass-mechanics/PassMechanicsConstants.cs
// Created:  2026-05-26
// Modified: 2026-06-06
// Author:   —
// Spec:     Pass Mechanics #5 §3.2.9, §3.3.7, §3.4.7, §3.5.9, §3.6.9, §3.7.6,
//           §3.8.10, Code Standards #20
// Purpose:  All constants for the pass mechanics system. No literals in formula code.
//           Region order: Fixed → Cross → GT.

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Universal constants shared across all pass-mechanics subsystems.
    /// Sources: §3.2.9 (velocity), §3.3.7 (angle), §3.4.7 (spin), §3.5.9 (error),
    /// §3.6.9 (target), §3.7.6 (weak foot), §3.8.10 (timing).
    /// </summary>
    public static class PassMechanicsConstants
    {
        // ── ATTRIBUTE LIMITS ──────────────────────────────────────────────────────

        #region Fixed

        /// <summary>[FIXED] Maximum value for any PlayerAttribute [1–20]. Pass Mechanics #5 §3.2.9, [MASTER-VOL2].</summary>
        public const float ATTR_MAX = 20.0f;

        /// <summary>[FIXED] Minimum attribute floor for clamping. §3.2.4.</summary>
        public const float ATTR_MIN = 1.0f;

        /// <summary>[FIXED] Sentinel for no target agent (space-targeted pass). §2.4.1.</summary>
        public const int AGENT_ID_NONE = -1;

        /// <summary>[FIXED] Minimum spin below which knuckling regime is entered [HONG-2012]. §3.4.4.</summary>
        public const float SPIN_MIN = 1.0f;

        #endregion

        // ── CROSS-SPEC MIRRORS ────────────────────────────────────────────────────

        #region Cross

        /// <summary>
        /// [CROSS] Pitch length (metres). Ball moves from X=0 to X=105 (goal-to-goal).
        /// Authoritative source: BallPhysicsConstants.Pitch.LENGTH. Ball Physics #1 §1.2.
        /// Value: 105.0m.
        /// TODO: route via ProjectConstants once Stage 1 multi-consumer routing pass lands
        ///       (Spec #20 §4.2; parallel duplications: AgentMovement SafetyConstants.PitchLengthX,
        ///       Decision Tree PitchGeometry, Defensive AI DefensiveAIConstants).
        /// </summary>
        public static readonly float PitchLength = BallPhysicsConstants.Pitch.LENGTH;

        /// <summary>
        /// [CROSS] Pitch width (metres). Ball moves from Y=0 to Y=68 (touchline-to-touchline).
        /// Authoritative source: BallPhysicsConstants.Pitch.WIDTH. Ball Physics #1 §1.2.
        /// Value: 68.0m.
        /// TODO: route via ProjectConstants once Stage 1 multi-consumer routing pass lands
        ///       (Spec #20 §4.2; parallels SafetyConstants.PitchWidthY et al.).
        /// </summary>
        public static readonly float PitchWidth = BallPhysicsConstants.Pitch.WIDTH;

        #endregion

        // ── GAMEPLAY-TUNABLE CONSTANTS ────────────────────────────────────────────

        #region GT

        // §3.2 — Velocity Model

        /// <summary>[GT] Fatigue-induced velocity reduction coefficient. §3.2.5, [ALI-2011] direction.
        /// At Fatigue=1.0, velocity is reduced by this fraction. Tune in [0.10, 0.30].</summary>
        public static readonly float FatiguePowerReduction = 0.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Defensive floor for the weakFootPowerPenalty argument to ComputeKickSpeed.
        /// Caller contract is [0.85, 1.0]; this clamp guards against malformed input that would
        /// push velocity into a numerically invalid regime. §3.7.4.</summary>
        public static readonly float WeakFootPowerFloorMin = 0.5f; // TODO: replace with config loader (Stage 1)

        // §3.3 — Launch Angle (Apex Heights)

        /// <summary>[GT] Apex height (metres) for Lofted pass type. §3.3.7.
        /// Produces ~39° at 30m. Tune by 0.5m increments.</summary>
        public static readonly float ApexHeightLofted = 6.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Apex height (metres) for Chip pass type. §3.3.7.
        /// Produces ~56° at 12m; sufficient to clear goalkeeper.</summary>
        public static readonly float ApexHeightChip = 4.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Apex height (metres) for AerialThrough pass type. §3.3.7.</summary>
        public static readonly float ApexHeightAerialThrough = 5.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Apex height (metres) for Cross (High) sub-type. §3.3.7.</summary>
        public static readonly float ApexHeightCrossHigh = 5.5f; // TODO: replace with config loader (Stage 1)

        // §3.4 — Spin Vector

        /// <summary>[GT] TechniqueScale lower bound (at Technique=1). §3.4.3. [0.5, 1.5] range.</summary>
        public static readonly float TechniqueSpinMin = 0.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] TechniqueScale upper bound (at Technique=20). §3.4.3.</summary>
        public static readonly float TechniqueSpinMax = 1.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Topspin fraction for Lofted/AerialThrough types. §3.4.5.
        /// Applied to SpinMagnitude to produce mild topspin. Extract if per-type tuning required.</summary>
        public static readonly float LoftedTopspinFraction = 0.7f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Topspin/sidespin mix fraction for Cross (High) type. §3.4.5.
        /// Equal split: each component = SpinMagnitude × 0.5.</summary>
        public static readonly float CrossHighMixFraction = 0.5f; // TODO: replace with config loader (Stage 1)

        // §3.5 — Error Model

        /// <summary>[GT] PassingModifier at Passing=1 (worst). §3.5.4. Tune per completion rate targets.</summary>
        public static readonly float PassingErrorMax = 2.8f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] PassingModifier at Passing=20 (elite). §3.5.4.</summary>
        public static readonly float PassingErrorMin = 0.45f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Pressure modifier weight. §3.5.4. [BEILOCK-2007] range: maximum +50% error.</summary>
        public static readonly float PressureWeight = 0.50f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Fatigue-induced accuracy reduction. §3.5.4, independent from velocity fatigue.
        /// At Fatigue=1.0, error multiplier is 1.0 + this value.</summary>
        public static readonly float FatigueAccuracyReduction = 0.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum orientation penalty (at 90° body misalignment). §3.5.4.
        /// +150% error at perpendicular body angle.</summary>
        public static readonly float OrientationMaxPenalty = 1.50f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Urgency error scaling factor. §3.5.4. Maximum +35% error at Urgency=1.0.</summary>
        public static readonly float UrgencyErrorScale = 0.35f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum error angle (degrees). No pass is laser-precise. §3.5.5.</summary>
        public static readonly float MinErrorAngle = 0.1f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum error angle (degrees). Prevents multiplicative chain absurdities. §3.5.5.
        /// At 18° on 20m: miss ~6.5m lateral (≈ penalty area width).</summary>
        public static readonly float MaxErrorAngle = 18.0f; // TODO: replace with config loader (Stage 1)

        // §3.5.4 — BASE_ERROR per PassType (degrees)
        // Values are the error an exactly-average passer (Passing=10) produces at neutral conditions.

        /// <summary>[GT] BASE_ERROR for Ground passes (degrees). §3.5.4.</summary>
        public static readonly float BaseErrorGround = 1.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] BASE_ERROR for Driven passes (degrees). §3.5.4.</summary>
        public static readonly float BaseErrorDriven = 2.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] BASE_ERROR for Lofted passes (degrees). §3.5.4.</summary>
        public static readonly float BaseErrorLofted = 3.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] BASE_ERROR for ThroughBall passes (degrees). §3.5.4.</summary>
        public static readonly float BaseErrorThroughBall = 2.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] BASE_ERROR for AerialThrough passes (degrees). §3.5.4.</summary>
        public static readonly float BaseErrorAerialThrough = 3.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] BASE_ERROR for Cross.Flat / Cross.High passes (degrees). §3.5.4.</summary>
        public static readonly float BaseErrorCrossFlat = 2.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] BASE_ERROR for Cross.Whipped passes (degrees). §3.5.4.</summary>
        public static readonly float BaseErrorCrossWhipped = 3.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] BASE_ERROR for Chip passes (degrees). §3.5.4.</summary>
        public static readonly float BaseErrorChip = 2.5f; // TODO: replace with config loader (Stage 1)

        // §3.5.6 — Pressure Scalar

        /// <summary>[GT] Spatial hash query radius for pressure detection (metres). §3.5.6.</summary>
        public static readonly float PressureRadius = 3.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Pressure saturation denominator. §3.5.6.
        /// 2 opponents at close range saturates pressure to 1.0.</summary>
        public static readonly float PressureScalarMax = 2.0f; // TODO: replace with config loader (Stage 1)

        // §3.6 — Target Resolution

        /// <summary>[GT] Speed threshold below which receiver is treated as stationary (m/s). §3.6.5.</summary>
        public static readonly float VThresholdStationary = 0.5f; // TODO: replace with config loader (Stage 1)

        // §3.7 — Weak Foot Penalty

        /// <summary>[GT] Maximum accuracy penalty fraction for Rating=1. §3.7.3, [CAREY-2001].
        /// WeakFootModifier = 1.0 + PenaltyFraction × this value.</summary>
        public static readonly float WeakFootBasePenalty = 0.30f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum power penalty fraction for Rating=1. §3.7.4.
        /// WeakFootPowerPenalty = 1.0 - PenaltyFraction × this value.</summary>
        public static readonly float WeakFootPowerPenalty = 0.15f; // TODO: replace with config loader (Stage 1)

        // §3.8 — State Machine Timing

        /// <summary>[GT] Minimum windup frames regardless of Urgency. §3.8.8.
        /// 3 frames = 50ms at 60 Hz — physical minimum for kick motion.</summary>
        public static readonly int MinWindupFrames = 3; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Urgency windup reduction factor. §3.8.8.
        /// At Urgency=1.0, windup is halved: windupFrames × (1 - Urgency × 0.5).</summary>
        public static readonly float UrgencyWindupReduction = 0.50f; // TODO: replace with config loader (Stage 1)

        // §3.8.10 — Per-Type Windup Frames at Urgency=0

        /// <summary>[GT] Windup frames for Ground passes at Urgency=0. §3.8.10.</summary>
        public static readonly int WindupFramesGround = 8; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Windup frames for Driven passes at Urgency=0. §3.8.10.</summary>
        public static readonly int WindupFramesDriven = 12; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Windup frames for Lofted passes at Urgency=0. §3.8.10.</summary>
        public static readonly int WindupFramesLofted = 15; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Windup frames for ThroughBall passes at Urgency=0. §3.8.10.</summary>
        public static readonly int WindupFramesThroughBall = 8; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Windup frames for AerialThrough passes at Urgency=0. §3.8.10.</summary>
        public static readonly int WindupFramesAerialThrough = 14; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Windup frames for Cross.Flat / Cross.Whipped passes at Urgency=0. §3.8.10.</summary>
        public static readonly int WindupFramesCrossFlat = 12; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Windup frames for Cross.High passes at Urgency=0. §3.8.10.</summary>
        public static readonly int WindupFramesCrossHigh = 14; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Windup frames for Chip passes at Urgency=0. §3.8.10.</summary>
        public static readonly int WindupFramesChip = 10; // TODO: replace with config loader (Stage 1)

        // §3.8.10 — Per-Type Follow-Through Frames

        /// <summary>[GT] Follow-through frames for Ground passes. §3.8.10.</summary>
        public static readonly int FollowThroughFramesGround = 6; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Follow-through frames for Driven passes. §3.8.10.</summary>
        public static readonly int FollowThroughFramesDriven = 8; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Follow-through frames for Lofted passes. §3.8.10.</summary>
        public static readonly int FollowThroughFramesLofted = 10; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Follow-through frames for ThroughBall passes. §3.8.10.</summary>
        public static readonly int FollowThroughFramesThroughBall = 6; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Follow-through frames for AerialThrough passes. §3.8.10.</summary>
        public static readonly int FollowThroughFramesAerialThrough = 10; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Follow-through frames for Cross.Flat / Cross.Whipped passes. §3.8.10.</summary>
        public static readonly int FollowThroughFramesCrossFlat = 8; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Follow-through frames for Cross.High passes. §3.8.10.</summary>
        public static readonly int FollowThroughFramesCrossHigh = 10; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Follow-through frames for Chip passes. §3.8.10.</summary>
        public static readonly int FollowThroughFramesChip = 8; // TODO: replace with config loader (Stage 1)

        #endregion

        // ── BASE ERROR PER PASS TYPE ──────────────────────────────────────────────

        /// <summary>
        /// Returns the BASE_ERROR (degrees) for the given pass type. §3.5.4.
        /// BASE_ERROR is the error an exactly average passer (Passing=10) produces at neutral conditions.
        /// All values are [GT]; sourced from the named constants in the GT region.
        /// On unknown PassType (cast-from-int caller) the gated FM-01 LogError is the fault
        /// surface — the returned value is <see cref="BaseErrorDriven"/> (mid-range) so the
        /// error chain remains numerically well-formed and downstream clamping behaves normally.
        /// A "sentinel" base value would multiply through the chain and clamp identically to
        /// the legitimate worst case, defeating identification (AR-3 M-3).
        /// </summary>
        public static float GetBaseError(PassType passType, CrossSubType crossSubType = CrossSubType.Flat)
        {
            switch (passType)
            {
                case PassType.Ground:        return BaseErrorGround;
                case PassType.Driven:        return BaseErrorDriven;
                case PassType.Lofted:        return BaseErrorLofted;
                case PassType.ThroughBall:   return BaseErrorThroughBall;
                case PassType.AerialThrough: return BaseErrorAerialThrough;
                case PassType.Cross:
                    return (crossSubType == CrossSubType.Whipped) ? BaseErrorCrossWhipped : BaseErrorCrossFlat;
                case PassType.Chip:          return BaseErrorChip;
                default:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError($"[PassMechanics] FM-01: GetBaseError called for unknown PassType={passType}. Returning BaseErrorDriven mid-range default.");
#endif
                    return BaseErrorDriven;
            }
        }

        // ── PER-TYPE TIMING ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns WINDUP_FRAMES at Urgency=0 for the given pass type. §3.8.10.
        /// These are state-machine timing values; they do NOT live on PhysicalProfile (F-A02).
        /// All values are [GT]; sourced from the named constants in the GT region.
        /// Unknown PassType returns <see cref="WindupFramesDriven"/> as a defensive default
        /// (mid-range duration); FM-01 diagnostic is emitted in development builds.
        /// </summary>
        public static int GetWindupFrames(PassType passType, CrossSubType crossSubType = CrossSubType.Flat)
        {
            switch (passType)
            {
                case PassType.Ground:        return WindupFramesGround;
                case PassType.Driven:        return WindupFramesDriven;
                case PassType.Lofted:        return WindupFramesLofted;
                case PassType.ThroughBall:   return WindupFramesThroughBall;
                case PassType.AerialThrough: return WindupFramesAerialThrough;
                case PassType.Cross:
                    return (crossSubType == CrossSubType.High) ? WindupFramesCrossHigh : WindupFramesCrossFlat;
                case PassType.Chip:          return WindupFramesChip;
                default:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError($"[PassMechanics] FM-01: GetWindupFrames called for unknown PassType={passType}. Returning WindupFramesDriven.");
#endif
                    return WindupFramesDriven;
            }
        }

        /// <summary>
        /// Returns FOLLOWTHROUGH_FRAMES for the given pass type. §3.8.10.
        /// All values are [GT]; sourced from the named constants in the GT region.
        /// </summary>
        public static int GetFollowThroughFrames(PassType passType, CrossSubType crossSubType = CrossSubType.Flat)
        {
            switch (passType)
            {
                case PassType.Ground:        return FollowThroughFramesGround;
                case PassType.Driven:        return FollowThroughFramesDriven;
                case PassType.Lofted:        return FollowThroughFramesLofted;
                case PassType.ThroughBall:   return FollowThroughFramesThroughBall;
                case PassType.AerialThrough: return FollowThroughFramesAerialThrough;
                case PassType.Cross:
                    return (crossSubType == CrossSubType.High) ? FollowThroughFramesCrossHigh : FollowThroughFramesCrossFlat;
                case PassType.Chip:          return FollowThroughFramesChip;
                default:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError($"[PassMechanics] FM-01: GetFollowThroughFrames called for unknown PassType={passType}. Returning FollowThroughFramesDriven.");
#endif
                    return FollowThroughFramesDriven;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                       |
// | 1.0     | 2026-05-26 | —      | Initial implementation.                                                     |
// | 1.1     | 2026-05-27 | —      | AR-1 round-2 M-A: added using UnityEngine; GetBaseError, GetWindupFrames,   |
// |         |            |        |     GetFollowThroughFrames default cases now log FM-01 errors (consistent   |
// |         |            |        |     with PassTypeProfiles.GetProfile and PassVelocityCalculator defaults).  |
// | 1.2     | 2026-06-06 | —      | AR-2 M-4: switch-arm literals promoted to named GT constants                |
// |         |            |        |     (BaseError*/WindupFrames*/FollowThroughFrames* — 24 new constants).     |
// |         |            |        | AR-2 L-4: WeakFootPowerFloorMin added (replaces 0.5f magic floor in        |
// |         |            |        |     PassVelocityCalculator.ComputeKickSpeed).                              |
// |         |            |        | AR-2 L-9: GetBaseError unknown-type fallback changed 2.0f → MaxErrorAngle  |
// |         |            |        |     (out-of-band sentinel; no collision with legitimate per-type value).    |
// |         |            |        |     AR-3 M-3 reverts: MaxErrorAngle clamps to the same range as the         |
// |         |            |        |     legitimate worst case so the sentinel was unobservable. Fallback now    |
// |         |            |        |     returns BaseErrorDriven (mid-range) and the gated FM-01 LogError is     |
// |         |            |        |     the sole fault surface.                                                |
// |         |            |        | AR-2 L-10: PitchLength/Width Cross XML docs gained Stage 1 ProjectConstants |
// |         |            |        |     routing TODO (parallels Agent Movement v1.40 AR-5 L-3).                 |
// |         |            |        | AR-2 L-13: FM-01 Debug.LogError emits gated by                               |
// |         |            |        |     #if UNITY_EDITOR || DEVELOPMENT_BUILD (FR-CS-031 hot-path carve-out).    |
// | 1.3     | 2026-06-06 | —      | AR-3 M-3: GetBaseError unknown-type fallback reverted MaxErrorAngle →       |
// |         |            |        |     BaseErrorDriven. MaxErrorAngle was unobservable — the chain's            |
// |         |            |        |     output clamp masks the sentinel against legitimate worst-case error.    |
// |         |            |        |     Mid-range value + gated LogError is now the sole fault surface.         |
#endregion
