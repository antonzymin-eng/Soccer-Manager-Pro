// File:     src/pass-mechanics/PassMechanicsConstants.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §3.2.9, §3.3.7, §3.4.7, §3.5.9, §3.6.9, §3.7.6,
//           §3.8.10, Code Standards #20
// Purpose:  All constants for the pass mechanics system. No literals in formula code.
//           Region order: Fixed → Derived → Cross → GT → EST.

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
        /// </summary>
        public static readonly float PitchLength = BallPhysicsConstants.Pitch.LENGTH;

        /// <summary>
        /// [CROSS] Pitch width (metres). Ball moves from Y=0 to Y=68 (touchline-to-touchline).
        /// Authoritative source: BallPhysicsConstants.Pitch.WIDTH. Ball Physics #1 §1.2.
        /// Value: 68.0m.
        /// </summary>
        public static readonly float PitchWidth = BallPhysicsConstants.Pitch.WIDTH;

        #endregion

        // ── GAMEPLAY-TUNABLE CONSTANTS ────────────────────────────────────────────

        #region GT

        // §3.2 — Velocity Model

        /// <summary>[GT] Fatigue-induced velocity reduction coefficient. §3.2.5, [ALI-2011] direction.
        /// At Fatigue=1.0, velocity is reduced by this fraction. Tune in [0.10, 0.30].</summary>
        public static readonly float FatiguePowerReduction = 0.20f; // TODO: replace with config loader (Stage 1)

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

        #endregion

        // ── BASE ERROR PER PASS TYPE ──────────────────────────────────────────────

        /// <summary>
        /// Returns the BASE_ERROR (degrees) for the given pass type. §3.5.4.
        /// BASE_ERROR is the error an exactly average passer (Passing=10) produces at neutral conditions.
        /// All values are [GT].
        /// </summary>
        public static float GetBaseError(PassType passType, CrossSubType crossSubType = CrossSubType.Flat)
        {
            switch (passType)
            {
                case PassType.Ground:       return 1.5f;
                case PassType.Driven:       return 2.0f;
                case PassType.Lofted:       return 3.0f;
                case PassType.ThroughBall:  return 2.0f;
                case PassType.AerialThrough: return 3.5f;
                case PassType.Cross:
                    return (crossSubType == CrossSubType.Whipped) ? 3.0f : 2.5f;
                case PassType.Chip:         return 2.5f;
                default:
                    Debug.LogError($"[PassMechanics] FM-01: GetBaseError called for unknown PassType={passType}. Returning 2.0°.");
                    return 2.0f;
            }
        }

        // ── PER-TYPE TIMING ───────────────────────────────────────────────────────

        /// <summary>
        /// Returns WINDUP_FRAMES at Urgency=0 for the given pass type. §3.8.10.
        /// These are state-machine timing values; they do NOT live on PhysicalProfile (F-A02).
        /// All values are [GT].
        /// </summary>
        public static int GetWindupFrames(PassType passType, CrossSubType crossSubType = CrossSubType.Flat)
        {
            switch (passType)
            {
                case PassType.Ground:       return 8;
                case PassType.Driven:       return 12;
                case PassType.Lofted:       return 15;
                case PassType.ThroughBall:  return 8;
                case PassType.AerialThrough: return 14;
                case PassType.Cross:
                    return (crossSubType == CrossSubType.High) ? 14 : 12;
                case PassType.Chip:         return 10;
                default:
                    Debug.LogError($"[PassMechanics] FM-01: GetWindupFrames called for unknown PassType={passType}. Returning 10.");
                    return 10;
            }
        }

        /// <summary>
        /// Returns FOLLOWTHROUGH_FRAMES for the given pass type. §3.8.10.
        /// All values are [GT].
        /// </summary>
        public static int GetFollowThroughFrames(PassType passType, CrossSubType crossSubType = CrossSubType.Flat)
        {
            switch (passType)
            {
                case PassType.Ground:       return 6;
                case PassType.Driven:       return 8;
                case PassType.Lofted:       return 10;
                case PassType.ThroughBall:  return 6;
                case PassType.AerialThrough: return 10;
                case PassType.Cross:
                    return (crossSubType == CrossSubType.High) ? 10 : 8;
                case PassType.Chip:         return 8;
                default:
                    Debug.LogError($"[PassMechanics] FM-01: GetFollowThroughFrames called for unknown PassType={passType}. Returning 8.");
                    return 8;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                   |
// | 1.0     | 2026-05-26 | —      | Initial implementation.                                                 |
// | 1.1     | 2026-05-27 | —      | AR-1 round-2 M-A: added using UnityEngine; GetBaseError, GetWindupFrames, |
// |         |            |        |     GetFollowThroughFrames default cases now log FM-01 errors (consistent |
// |         |            |        |     with PassTypeProfiles.GetProfile and PassVelocityCalculator defaults). |
#endregion
