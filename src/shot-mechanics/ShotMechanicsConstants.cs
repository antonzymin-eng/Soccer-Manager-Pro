// File:     src/shot-mechanics/ShotMechanicsConstants.cs
// Created:  2026-05-27
// Modified: 2026-05-28
// Modified: 2026-07-28 (ERR-006-004 — VFloor 10 → 20 by measurement (shot-speed design KD-2))
// Author:   —
// Spec:     Shot Mechanics #6 §3.2–§3.9, §6.1, Code Standards #20
// Purpose:  All constants for the shot mechanics system. No magic literals in formula code.
//           Region order: Fixed → Derived → Cross → GT (EST region omitted — no estimated constants).

using UnityEngine;

using TacticalDirector.BallPhysics;
using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Universal constants for all shot-mechanics subsystems.
    /// Sources: §3.2 (velocity), §3.3 (angle), §3.4 (spin), §3.5 (placement),
    /// §3.6 (error), §3.7 (body mechanics), §3.8 (weak foot), §3.9 (timing).
    /// </summary>
    public static class ShotMechanicsConstants
    {
        #region Fixed

        /// <summary>[FIXED] Maximum value for any PlayerAttribute [1–20]. Shot Mechanics #6 §3.2, [MASTER-VOL2].</summary>
        public const float ATTR_MAX = 20.0f;

        /// <summary>[FIXED] Minimum attribute floor for clamping. §3.2.</summary>
        public const float ATTR_MIN = 1.0f;

        /// <summary>[FIXED] Minimum spin below which knuckling regime is entered (rad/s). §3.4.</summary>
        public const float SPIN_MIN = 1.0f;

        /// <summary>[FIXED] FIFA regulation goal width (left post to right post, metres). §3.5.</summary>
        public const float GOAL_WIDTH = 7.32f;

        /// <summary>[FIXED] FIFA regulation goal height (ground to underside of crossbar, metres). §3.5.</summary>
        public const float GOAL_HEIGHT = 2.44f;

        #endregion

        #region Derived

        /// <summary>[DERIVED] Goal-relative U coordinate of goal centre (midpoint of [0, 1] horizontal range).
        /// Formula: 0.5 = midpoint of the [0, 1] unit interval. §3.5, §3.7.
        /// Source constants: none (value is the mathematical midpoint of the unit interval).</summary>
        public static readonly float GoalCentreU = 0.5f;

        /// <summary>[DERIVED] Goal-relative V coordinate of goal centre (midpoint of [0, 1] vertical range).
        /// Formula: 0.5 = midpoint of the [0, 1] unit interval. §3.5, §3.7.
        /// Source constants: none (value is the mathematical midpoint of the unit interval).</summary>
        public static readonly float GoalCentreV = 0.5f;

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS] Pitch length (metres). X axis: goal-to-goal (0 → 105).
        /// Authoritative source: BallPhysicsConstants.Pitch.LENGTH. Ball Physics #1 §1.2.
        /// Value: 105.0m.
        /// </summary>
        public static readonly float PitchLength = BallPhysicsConstants.Pitch.LENGTH;

        /// <summary>
        /// [CROSS] Pitch width (metres). Y axis: touchline-to-touchline (0 → 68).
        /// Authoritative source: BallPhysicsConstants.Pitch.WIDTH. Ball Physics #1 §1.2.
        /// Value: 68.0m.
        /// </summary>
        public static readonly float PitchWidth = BallPhysicsConstants.Pitch.WIDTH;

        #endregion

        #region GT

        // ── §3.2 Velocity Model ─────────────────────────────────────────────────────

        /// <summary>[GT] Velocity floor: minimum kick speed before contact zone / fatigue modifiers
        /// (m/s). §3.2. Retuned 10 → 20 by measurement (ERR-006-004 / shot-speed design KD-2): at
        /// 10, a neutral player's FULL-power vBase capped at ~16 m/s before reducers, composing
        /// with the #8 PowerIntent defect into measured shot-tick means of 7–10 m/s against
        /// football's ~25. At 20 a neutral shot lands ~17–19 after typical reducers; an elite
        /// clean strike reaches ~33 (VCeiling unchanged). Appendix A.1.4's stacked-penalty
        /// visibility is preserved (worst stack ≈ 8.8 m/s, still above VAbsoluteMin 8).</summary>
        public static readonly float VFloor = Config.GetFloat("shot-mechanics", "VFloor", 24.0f);

        /// <summary>[GT] Velocity ceiling: maximum kick speed at KickPower=20, PowerIntent=1.0 (m/s). §3.2.</summary>
        public static readonly float VCeiling = Config.GetFloat("shot-mechanics", "VCeiling", 35.0f);

        /// <summary>[GT] Absolute minimum clamped kick speed (m/s). Prevents physically-impossible slow shots. §3.2.10.</summary>
        public static readonly float VAbsoluteMin = Config.GetFloat("shot-mechanics", "VAbsoluteMin", 8.0f);

        /// <summary>[GT] Absolute maximum clamped kick speed (m/s). Must be ≤ Ball Physics MAX_VELOCITY. §3.2.10, XC-4.2-02.</summary>
        public static readonly float VAbsoluteMax = Config.GetFloat("shot-mechanics", "VAbsoluteMax", 35.0f);

        /// <summary>[GT] Sigmoid midpoint distance (metres): equal blend of Finishing and LongShots. §3.2.3.</summary>
        public static readonly float DMid = Config.GetFloat("shot-mechanics", "DMid", 20.0f);

        /// <summary>[GT] Sigmoid scale factor (metres): controls blend sharpness around DMid. §3.2.3.</summary>
        public static readonly float DScale = Config.GetFloat("shot-mechanics", "DScale", 8.0f);

        /// <summary>[GT] ContactZone velocity modifier — Centre (clean ball strike). §3.2.5.</summary>
        public static readonly float ContactZoneModifierCentre = Config.GetFloat("shot-mechanics", "ContactZoneModifierCentre", 1.00f);

        /// <summary>[GT] ContactZone velocity modifier — OffCentre (curling strike). §3.2.5.</summary>
        public static readonly float ContactZoneModifierOffCentre = Config.GetFloat("shot-mechanics", "ContactZoneModifierOffCentre", 0.85f);

        /// <summary>[GT] ContactZone velocity modifier — BelowCentre (chip/loft strike). §3.2.5.</summary>
        public static readonly float ContactZoneModifierBelowCentre = Config.GetFloat("shot-mechanics", "ContactZoneModifierBelowCentre", 0.75f);

        /// <summary>[GT] Spin–velocity trade-off: fraction of velocity lost per unit of SpinIntent. §3.2.6.</summary>
        public static readonly float SpinVelocityTradeOff = Config.GetFloat("shot-mechanics", "SpinVelocityTradeOff", 0.25f);

        /// <summary>[GT] Fatigue-induced velocity reduction coefficient. At Fatigue=1.0 velocity is reduced by this fraction. §3.2.7.</summary>
        public static readonly float FatiguePowerReduction = Config.GetFloat("shot-mechanics", "FatiguePowerReduction", 0.20f);

        /// <summary>[GT] Minimum contact quality modifier (at BodyMechanicsScore=0). §3.2.8.</summary>
        public static readonly float ContactQualityModifierMin = Config.GetFloat("shot-mechanics", "ContactQualityModifierMin", 0.70f);

        /// <summary>[GT] Maximum contact quality modifier (at BodyMechanicsScore=1). §3.2.8.</summary>
        public static readonly float ContactQualityModifierMax = Config.GetFloat("shot-mechanics", "ContactQualityModifierMax", 1.00f);

        // ── §3.3 Launch Angle ───────────────────────────────────────────────────────

        /// <summary>[GT] Base launch angle for Centre contact zone (degrees). §3.3.3.</summary>
        public static readonly float BaseAngleCentre = Config.GetFloat("shot-mechanics", "BaseAngleCentre", 4.0f);

        /// <summary>[GT] Base launch angle for BelowCentre contact zone (degrees). §3.3.3.</summary>
        public static readonly float BaseAngleBelowCentre = Config.GetFloat("shot-mechanics", "BaseAngleBelowCentre", 18.0f);

        /// <summary>[GT] Base launch angle for OffCentre contact zone (degrees). §3.3.3.</summary>
        public static readonly float BaseAngleOffCentre = Config.GetFloat("shot-mechanics", "BaseAngleOffCentre", 8.0f);

        /// <summary>[GT] Maximum additional lift from low power intent (degrees). At PowerIntent=0, adds this full amount. §3.3.4.</summary>
        public static readonly float PowerLiftScale = Config.GetFloat("shot-mechanics", "PowerLiftScale", 4.0f);

        /// <summary>[GT] Maximum additional lift from high spin intent (degrees). At SpinIntent=1, adds this full amount. §3.3.5.</summary>
        public static readonly float SpinLiftScale = Config.GetFloat("shot-mechanics", "SpinLiftScale", 14.0f);

        /// <summary>[GT] Body lean forward transfers this fraction as upward angle penalty. §3.3.6.</summary>
        public static readonly float BodyLeanTransferCoefficient = Config.GetFloat("shot-mechanics", "BodyLeanTransferCoefficient", 0.60f);

        /// <summary>[GT] Maximum body-shape penalty (degrees) at BodyMechanicsScore=0. §3.3.7.</summary>
        public static readonly float BodyShapeMaxPenalty = Config.GetFloat("shot-mechanics", "BodyShapeMaxPenalty", 8.0f);

        /// <summary>[GT] Minimum launch angle — prevents drilling into ground (degrees). §3.3.8.</summary>
        public static readonly float LaunchAngleMin = Config.GetFloat("shot-mechanics", "LaunchAngleMin", -5.0f);

        /// <summary>[GT] Maximum launch angle — prevents physically impossible near-vertical shots (degrees). §3.3.8.</summary>
        public static readonly float LaunchAngleMax = Config.GetFloat("shot-mechanics", "LaunchAngleMax", 70.0f);

        // ── §3.4 Spin Vector ────────────────────────────────────────────────────────

        /// <summary>[GT] Topspin base magnitude for Centre contact (rad/s). §3.4.2.</summary>
        public static readonly float TopspinBaseCentre = Config.GetFloat("shot-mechanics", "TopspinBaseCentre", 25.0f);

        /// <summary>[GT] Topspin base magnitude for BelowCentre contact (rad/s). §3.4.2.</summary>
        public static readonly float TopspinBaseBelowCentre = Config.GetFloat("shot-mechanics", "TopspinBaseBelowCentre", 4.0f);

        /// <summary>[GT] Topspin base magnitude for OffCentre contact (rad/s). §3.4.2.</summary>
        public static readonly float TopspinBaseOffCentre = Config.GetFloat("shot-mechanics", "TopspinBaseOffCentre", 8.0f);

        /// <summary>[GT] Backspin base magnitude for Centre contact (rad/s). §3.4.3.</summary>
        public static readonly float BackspinBaseCentre = Config.GetFloat("shot-mechanics", "BackspinBaseCentre", 2.0f);

        /// <summary>[GT] Backspin base magnitude for BelowCentre contact (rad/s). §3.4.3.</summary>
        public static readonly float BackspinBaseBelowCentre = Config.GetFloat("shot-mechanics", "BackspinBaseBelowCentre", 30.0f);

        /// <summary>[GT] Backspin base magnitude for OffCentre contact (rad/s). §3.4.3.</summary>
        public static readonly float BackspinBaseOffCentre = Config.GetFloat("shot-mechanics", "BackspinBaseOffCentre", 6.0f);

        /// <summary>[GT] Sidespin base magnitude for Centre contact (rad/s). §3.4.4.</summary>
        public static readonly float SidespinBaseCentre = Config.GetFloat("shot-mechanics", "SidespinBaseCentre", 3.0f);

        /// <summary>[GT] Sidespin base magnitude for BelowCentre contact (rad/s). §3.4.4.</summary>
        public static readonly float SidespinBaseBelowCentre = Config.GetFloat("shot-mechanics", "SidespinBaseBelowCentre", 5.0f);

        /// <summary>[GT] Sidespin base magnitude for OffCentre contact (rad/s). §3.4.4.</summary>
        public static readonly float SidespinBaseOffCentre = Config.GetFloat("shot-mechanics", "SidespinBaseOffCentre", 28.0f);

        /// <summary>[GT] TechniqueScale lower bound (at Technique=1). §3.4.5.</summary>
        public static readonly float TechniqueSpinBase = Config.GetFloat("shot-mechanics", "TechniqueSpinBase", 0.6f);

        /// <summary>[GT] TechniqueScale upper bound (at Technique=20). §3.4.5.</summary>
        public static readonly float TechniqueSpinMax = Config.GetFloat("shot-mechanics", "TechniqueSpinMax", 1.0f);

        /// <summary>[GT] Absolute maximum spin magnitude (rad/s). Matches Ball Physics MAX_SPIN. §3.4.10, XC-4.2-02.</summary>
        public static readonly float SpinAbsoluteMax = Config.GetFloat("shot-mechanics", "SpinAbsoluteMax", 80.0f);

        // ── §3.6 Error Model ────────────────────────────────────────────────────────

        /// <summary>[GT] Base error at worst effective attribute (degrees). §3.6.3.</summary>
        public static readonly float BaseErrorMax = Config.GetFloat("shot-mechanics", "BaseErrorMax", 4.0f);

        /// <summary>[GT] Base error at best effective attribute (degrees). §3.6.3.</summary>
        public static readonly float BaseErrorMin = Config.GetFloat("shot-mechanics", "BaseErrorMin", 0.5f);

        /// <summary>[GT] Power penalty coefficient in quadratic power–accuracy trade-off. §3.6.5, FR-03.</summary>
        public static readonly float PowerPenaltyCoefficient = Config.GetFloat("shot-mechanics", "PowerPenaltyCoefficient", 1.5f);

        /// <summary>[GT] Maximum pressure penalty fraction (at full pressure, zero Composure). §3.6.5.</summary>
        public static readonly float PressureMaxPenalty = Config.GetFloat("shot-mechanics", "PressureMaxPenalty", 0.8f);

        /// <summary>[GT] Maximum fatigue-induced accuracy penalty fraction. §3.6.6.</summary>
        public static readonly float FatigueMaxPenalty = Config.GetFloat("shot-mechanics", "FatigueMaxPenalty", 0.4f);

        /// <summary>[GT] Body shape error coefficient; scales (1 - BMS)² penalty. §3.6.7.</summary>
        public static readonly float BodyShapeErrorCoefficient = Config.GetFloat("shot-mechanics", "BodyShapeErrorCoefficient", 1.0f);

        /// <summary>[GT] Minimum clamped error angle (degrees). No shot is laser-precise. §3.6.8.</summary>
        public static readonly float MinErrorAngle = Config.GetFloat("shot-mechanics", "MinErrorAngle", 0.15f);

        /// <summary>[GT] Maximum clamped error angle (degrees). Prevents multiplicative absurdities. §3.6.8.</summary>
        public static readonly float MaxErrorAngle = Config.GetFloat("shot-mechanics", "MaxErrorAngle", 25.0f);

        /// <summary>[GT] Spatial hash query radius for pressure detection (metres). §4.4.1.</summary>
        public static readonly float PressureRadiusMax = Config.GetFloat("shot-mechanics", "PressureRadiusMax", 3.0f);

        /// <summary>[GT] Match seed supplied to deterministic error direction hash. §3.6.9, KD-4.
        /// Stage 0: 0 (single-machine; no match context). Stage 1: wire from MatchContext.MatchSeed.
        /// Not a designer-tunable parameter — used only as a determinism seed.</summary>
        public static readonly int ErrorDirectionMatchSeed = 0; // TODO: wire from MatchContext.MatchSeed at Stage 1

        /// <summary>[GT] Horizontal error clamp margin as fraction of goal width. §3.6.9.
        /// Clamps post-error Y to [LeftPostY - GoalWidth × this, RightPostY + GoalWidth × this].</summary>
        public static readonly float PlacementErrorHClampFraction = Config.GetFloat("shot-mechanics", "PlacementErrorHClampFraction", 0.5f);

        /// <summary>[GT] Vertical error clamp ceiling as fraction of goal height. §3.6.9.
        /// Clamps post-error Z to [0, GoalHeight × this].</summary>
        public static readonly float PlacementErrorVClampFraction = Config.GetFloat("shot-mechanics", "PlacementErrorVClampFraction", 1.5f);

        // ── §3.5 Placement ──────────────────────────────────────────────────────────

        /// <summary>[GT] Epsilon for aim-direction magnitude (metres). Compared against
        /// squared magnitude (effective threshold 1e-8) in ShotExecutor FM-04a and
        /// ShotPlacementResolver; guards against shooter-at-goal-line singularity. §3.5.</summary>
        public static readonly float AimDirectionEpsilon = Config.GetFloat("shot-mechanics", "AimDirectionEpsilon", 1e-4f);

        /// <summary>[GT] Epsilon for aim-direction X-component clamp in ApplyErrorOffset (dimensionless). Prevents degenerate division when aim is nearly perpendicular to goal line. §3.5.</summary>
        public static readonly float AimDirectionComponentEpsilon = Config.GetFloat("shot-mechanics", "AimDirectionComponentEpsilon", 0.001f);

        /// <summary>[GT] Minimum effective distance to goal line used in ApplyErrorOffset (metres). Guards against shooter-at-or-past-goal-line degenerate case. §3.5.</summary>
        public static readonly float GoalLineDistanceFloor = Config.GetFloat("shot-mechanics", "GoalLineDistanceFloor", 0.1f);

        // ── §3.7 Body Mechanics ─────────────────────────────────────────────────────

        /// <summary>[GT] Speed threshold below which agent is considered stationary for run-up scoring (m/s). §3.7.3.</summary>
        public static readonly float StationarySpeedThreshold = Config.GetFloat("shot-mechanics", "StationarySpeedThreshold", 0.1f);

        /// <summary>[GT] Run-up score returned for stationary agent (speed &lt; StationarySpeedThreshold). Neutral centre value [0, 1]. §3.7.3.</summary>
        public static readonly float StationaryRunUpScore = Config.GetFloat("shot-mechanics", "StationaryRunUpScore", 0.5f);

        /// <summary>[GT] Ideal run-up approach angle relative to goal line (degrees). §3.7.3.</summary>
        public static readonly float IdealRunUpAngle = Config.GetFloat("shot-mechanics", "IdealRunUpAngle", 37.5f);

        /// <summary>[GT] Run-up angle tolerance before penalty kicks in (degrees). §3.7.3.</summary>
        public static readonly float RunUpTolerance = Config.GetFloat("shot-mechanics", "RunUpTolerance", 45.0f);

        /// <summary>[GT] Plant foot lateral offset tolerance (metres). Within this range = full score. §3.7.4.</summary>
        public static readonly float PlantFootTolerance = Config.GetFloat("shot-mechanics", "PlantFootTolerance", 0.35f);

        /// <summary>[GT] Agent velocity ideal range lower bound (m/s). §3.7.5.</summary>
        public static readonly float VelocityIdealMin = Config.GetFloat("shot-mechanics", "VelocityIdealMin", 1.0f);

        /// <summary>[GT] Agent velocity ideal range upper bound (m/s). §3.7.5.</summary>
        public static readonly float VelocityIdealMax = Config.GetFloat("shot-mechanics", "VelocityIdealMax", 5.0f);

        /// <summary>[GT] Velocity penalty scale for below-ideal range (m/s below VelocityIdealMin). §3.7.5.</summary>
        public static readonly float VelocityPenaltyScaleNegative = Config.GetFloat("shot-mechanics", "VelocityPenaltyScaleNegative", 3.0f);

        /// <summary>[GT] Velocity penalty scale for above-ideal range (m/s above VelocityIdealMax). §3.7.5.</summary>
        public static readonly float VelocityPenaltyScalePositive = Config.GetFloat("shot-mechanics", "VelocityPenaltyScalePositive", 4.0f);

        /// <summary>[GT] Body lean tolerance before penalty kicks in (degrees). §3.7.6.</summary>
        public static readonly float LeanTolerance = Config.GetFloat("shot-mechanics", "LeanTolerance", 20.0f);

        /// <summary>[GT] Weight of run-up angle component in composite BodyMechanicsScore. §3.7.7.</summary>
        public static readonly float WeightRunUp = Config.GetFloat("shot-mechanics", "WeightRunUp", 0.25f);

        /// <summary>[GT] Weight of plant foot component in composite BodyMechanicsScore. §3.7.7.</summary>
        public static readonly float WeightPlant = Config.GetFloat("shot-mechanics", "WeightPlant", 0.30f);

        /// <summary>[GT] Weight of agent velocity component in composite BodyMechanicsScore. §3.7.7.</summary>
        public static readonly float WeightVelocity = Config.GetFloat("shot-mechanics", "WeightVelocity", 0.20f);

        /// <summary>[GT] Weight of body lean component in composite BodyMechanicsScore. §3.7.7.</summary>
        public static readonly float WeightLean = Config.GetFloat("shot-mechanics", "WeightLean", 0.25f);

        /// <summary>[GT] BodyMechanicsScore threshold below which stumble is triggered (if PowerIntent also exceeds threshold). §3.7.8, FR-08.</summary>
        public static readonly float StumbleThreshold = Config.GetFloat("shot-mechanics", "StumbleThreshold", 0.25f);

        /// <summary>[GT] PowerIntent threshold above which stumble can trigger. §3.7.8, FR-08.</summary>
        public static readonly float StumblePowerThreshold = Config.GetFloat("shot-mechanics", "StumblePowerThreshold", 0.75f);

        // ── §3.8 Weak Foot ──────────────────────────────────────────────────────────

        /// <summary>[GT] Maximum error cone multiplier penalty at WeakFootRating=1. §3.8.3.</summary>
        public static readonly float WeakFootBaseErrorPenalty = Config.GetFloat("shot-mechanics", "WeakFootBaseErrorPenalty", 0.60f);

        /// <summary>[GT] Maximum velocity penalty fraction at WeakFootRating=1. §3.8.4.</summary>
        public static readonly float WeakFootVelocityPenalty = Config.GetFloat("shot-mechanics", "WeakFootVelocityPenalty", 0.20f);

        /// <summary>[GT] Maximum WeakFootRating value (no penalty at this rating). §3.8.3, §3.8.4.</summary>
        public static readonly int WeakFootRatingMax = Config.GetInt("shot-mechanics", "WeakFootRatingMax", 5);

        /// <summary>[DERIVED] Effective WeakFootRating range [1, 4]. Formula: WeakFootRatingMax - 1. §3.8.3, §3.8.4.
        /// Source constants: ShotMechanicsConstants.WeakFootRatingMax.
        /// Invariant: must remain > 0 (WeakFootPenaltyApplier divides by this value; WeakFootRatingMax must be ≥ 2).
        /// Placed in GT region after WeakFootRatingMax to satisfy C# static-readonly init order; semantically Derived.</summary>
        public static readonly int WeakFootRatingRange = WeakFootRatingMax - 1;

        // ── §3.3.6 Body Lean (Stage 0 approximation) ───────────────────────────────

        /// <summary>[GT] Maximum forward body lean angle (degrees) at full sprint. §3.3.6, §3.7.6.</summary>
        public static readonly float BodyLeanMaxDeg = Config.GetFloat("shot-mechanics", "BodyLeanMaxDeg", 20.0f);

        /// <summary>[GT] Agent speed (m/s) at which maximum body lean is reached. §3.3.6.</summary>
        public static readonly float BodyLeanMaxSpeed = Config.GetFloat("shot-mechanics", "BodyLeanMaxSpeed", 5.0f);

        // ── §3.9 State Machine Timing ───────────────────────────────────────────────

        /// <summary>[GT] Windup frames at PowerIntent ≥ 0.80 (full backswing, ~233ms at 60Hz). §3.9.</summary>
        public static readonly int WindupFramesHighPower = Config.GetInt("shot-mechanics", "WindupFramesHighPower", 14);

        /// <summary>[GT] Windup frames at PowerIntent ∈ [0.50, 0.80) (~167ms at 60Hz). §3.9.</summary>
        public static readonly int WindupFramesMedPower = Config.GetInt("shot-mechanics", "WindupFramesMedPower", 10);

        /// <summary>[GT] Windup frames at PowerIntent &lt; 0.50 (stabbed/quick shot, ~117ms at 60Hz). §3.9.</summary>
        public static readonly int WindupFramesLowPower = Config.GetInt("shot-mechanics", "WindupFramesLowPower", 7);

        /// <summary>[GT] PowerIntent threshold for high-power windup branch. §3.9.</summary>
        public static readonly float WindupPowerHighThreshold = Config.GetFloat("shot-mechanics", "WindupPowerHighThreshold", 0.80f);

        /// <summary>[GT] PowerIntent threshold for medium-power windup branch. §3.9.</summary>
        public static readonly float WindupPowerMedThreshold = Config.GetFloat("shot-mechanics", "WindupPowerMedThreshold", 0.50f);

        /// <summary>[GT] Maximum additional windup frames added by SpinIntent (full spin = +3 frames). §3.9.</summary>
        public static readonly int WindupSpinBonusMax = Config.GetInt("shot-mechanics", "WindupSpinBonusMax", 3);

        /// <summary>[GT] Fixed follow-through duration (frames, ~133ms at 60Hz). §3.9.</summary>
        public static readonly int FollowThroughFrames = Config.GetInt("shot-mechanics", "FollowThroughFrames", 8);

        #endregion

        // ── Helpers ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns the base launch angle (degrees) for the given ContactZone. §3.3.3.
        /// </summary>
        public static float GetBaseAngle(ContactZone zone)
        {
            switch (zone)
            {
                case ContactZone.Centre:      return BaseAngleCentre;
                case ContactZone.BelowCentre: return BaseAngleBelowCentre;
                case ContactZone.OffCentre:   return BaseAngleOffCentre;
                default:
                    Debug.LogError($"[ShotMechanics] GetBaseAngle: unknown ContactZone={zone}. Returning Centre default.");
                    return BaseAngleCentre;
            }
        }

        /// <summary>
        /// Returns the ContactZone velocity modifier for the given zone. §3.2.5.
        /// </summary>
        public static float GetContactZoneModifier(ContactZone zone)
        {
            switch (zone)
            {
                case ContactZone.Centre:      return ContactZoneModifierCentre;
                case ContactZone.BelowCentre: return ContactZoneModifierBelowCentre;
                case ContactZone.OffCentre:   return ContactZoneModifierOffCentre;
                default:
                    Debug.LogError($"[ShotMechanics] GetContactZoneModifier: unknown ContactZone={zone}. Returning Centre default.");
                    return ContactZoneModifierCentre;
            }
        }

        /// <summary>
        /// Returns windup frame count based on PowerIntent and SpinIntent. §3.9.
        /// </summary>
        public static int ComputeWindupFrames(float powerIntent, float spinIntent)
        {
            int baseFrames;
            if (powerIntent >= WindupPowerHighThreshold)
                baseFrames = WindupFramesHighPower;
            else if (powerIntent >= WindupPowerMedThreshold)
                baseFrames = WindupFramesMedPower;
            else
                baseFrames = WindupFramesLowPower;

            int spinBonus = Mathf.RoundToInt(spinIntent * WindupSpinBonusMax);
            return baseFrames + spinBonus;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                        |
// | 1.0     | 2026-05-27 | —      | Initial implementation.                                                                      |
// | 1.1     | 2026-05-28 | —      | H-3: Renamed underscore GT constants to PascalCase (V_Floor→VFloor, V_Ceiling→VCeiling,      |
// |         |            |        |   V_AbsoluteMin→VAbsoluteMin, V_AbsoluteMax→VAbsoluteMax, D_Mid→DMid, D_Scale→DScale).        |
// |         |            |        |   M-2: Added BodyLeanMaxDeg, BodyLeanMaxSpeed GT constants.                                  |
// |         |            |        |   M-3: Added WeakFootRatingMax, WeakFootRatingRange GT constants.                            |
// | 1.2     | 2026-05-28 | —      | M-5: Added Derived region with GoalCentreU/GoalCentreV (replaces 0.5f magic literals).        |
// | 1.3     | 2026-05-28 | —      | M-6: WeakFootRatingRange [GT]→[DERIVED]; value literal 4 → WeakFootRatingMax - 1.            |
// |         |            |        |   Header Purpose comment updated: region order Fixed→Cross→GT → Fixed→Derived→Cross→GT.  |
// | 1.4     | 2026-05-28 | —      | M-7: Added PlacementErrorHClampFraction/VClampFraction (replaces 0.5f/1.5f in             |
// |         |            |        |   ShotPlacementResolver.ApplyErrorOffset). §3.6.9.                                     |
// | 1.5     | 2026-05-28 | —      | M-8: WeakFootRatingRange XML doc: added Source constants field (FR-CS-021).               |
// |         |            |        |   L-1: Added AimDirectionEpsilon (1e-4f) and StationarySpeedThreshold (0.1f) GT         |
// |         |            |        |   constants (replaces magic literals in ShotPlacementResolver/BodyMechanicsEvaluator). |
// | 1.6     | 2026-05-28 | —      | GoalCentreU/V XML: added "Source constants: none" clarification (FR-CS-021).              |
// |         |            |        |   Header: added "(EST omitted)" note. WeakFootRatingRange: invariant (>0) note added.   |
// |         |            |        |   Added: AimDirectionComponentEpsilon (0.001f), GoalLineDistanceFloor (0.1f),           |
// |         |            |        |   ErrorDirectionMatchSeed (0), StationaryRunUpScore (0.5f) GT constants.               |
// | 1.7     | 2026-06-01 | —      | AR-3 L-2: AimDirectionEpsilon XML doc notes squared-magnitude comparison (effective       |
// |         |            |        |   1e-8) to prevent future re-scaling errors.                                            |
// | 1.8     | 2026-07-28 | —      | ERR-006-004 (shot-speed design KD-2): VFloor 10 → 20 — at 10 a neutral      |
// |         |            |        | full-power vBase capped at ~16 m/s before reducers (measured shot-tick      |
// |         |            |        | means 7–10 m/s). VCeiling/VAbsoluteMin/VAbsoluteMax unchanged (A.1.4        |
// |         |            |        | stacked-penalty visibility preserved).                                      |
#endregion
