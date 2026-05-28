// File:     src/shot-mechanics/ShotMechanicsConstants.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §3.2–§3.9, §6.1, Code Standards #20
// Purpose:  All constants for the shot mechanics system. No magic literals in formula code.
//           Region order: Fixed → Cross → GT.

using UnityEngine;

using TacticalDirector.BallPhysics;

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

        /// <summary>[GT] Velocity floor: minimum kick speed before contact zone / fatigue modifiers (m/s). §3.2.</summary>
        public static readonly float VFloor = 10.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Velocity ceiling: maximum kick speed at KickPower=20, PowerIntent=1.0 (m/s). §3.2.</summary>
        public static readonly float VCeiling = 35.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Absolute minimum clamped kick speed (m/s). Prevents physically-impossible slow shots. §3.2.10.</summary>
        public static readonly float VAbsoluteMin = 8.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Absolute maximum clamped kick speed (m/s). Must be ≤ Ball Physics MAX_VELOCITY. §3.2.10, XC-4.2-02.</summary>
        public static readonly float VAbsoluteMax = 35.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Sigmoid midpoint distance (metres): equal blend of Finishing and LongShots. §3.2.3.</summary>
        public static readonly float DMid = 20.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Sigmoid scale factor (metres): controls blend sharpness around DMid. §3.2.3.</summary>
        public static readonly float DScale = 8.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] ContactZone velocity modifier — Centre (clean ball strike). §3.2.5.</summary>
        public static readonly float ContactZoneModifierCentre = 1.00f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] ContactZone velocity modifier — OffCentre (curling strike). §3.2.5.</summary>
        public static readonly float ContactZoneModifierOffCentre = 0.85f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] ContactZone velocity modifier — BelowCentre (chip/loft strike). §3.2.5.</summary>
        public static readonly float ContactZoneModifierBelowCentre = 0.75f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Spin–velocity trade-off: fraction of velocity lost per unit of SpinIntent. §3.2.6.</summary>
        public static readonly float SpinVelocityTradeOff = 0.25f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Fatigue-induced velocity reduction coefficient. At Fatigue=1.0 velocity is reduced by this fraction. §3.2.7.</summary>
        public static readonly float FatiguePowerReduction = 0.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum contact quality modifier (at BodyMechanicsScore=0). §3.2.8.</summary>
        public static readonly float ContactQualityModifierMin = 0.70f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum contact quality modifier (at BodyMechanicsScore=1). §3.2.8.</summary>
        public static readonly float ContactQualityModifierMax = 1.00f; // TODO: replace with config loader (Stage 1)

        // ── §3.3 Launch Angle ───────────────────────────────────────────────────────

        /// <summary>[GT] Base launch angle for Centre contact zone (degrees). §3.3.3.</summary>
        public static readonly float BaseAngleCentre = 4.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Base launch angle for BelowCentre contact zone (degrees). §3.3.3.</summary>
        public static readonly float BaseAngleBelowCentre = 18.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Base launch angle for OffCentre contact zone (degrees). §3.3.3.</summary>
        public static readonly float BaseAngleOffCentre = 8.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum additional lift from low power intent (degrees). At PowerIntent=0, adds this full amount. §3.3.4.</summary>
        public static readonly float PowerLiftScale = 4.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum additional lift from high spin intent (degrees). At SpinIntent=1, adds this full amount. §3.3.5.</summary>
        public static readonly float SpinLiftScale = 14.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Body lean forward transfers this fraction as upward angle penalty. §3.3.6.</summary>
        public static readonly float BodyLeanTransferCoefficient = 0.60f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum body-shape penalty (degrees) at BodyMechanicsScore=0. §3.3.7.</summary>
        public static readonly float BodyShapeMaxPenalty = 8.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum launch angle — prevents drilling into ground (degrees). §3.3.8.</summary>
        public static readonly float LaunchAngleMin = -5.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum launch angle — prevents physically impossible near-vertical shots (degrees). §3.3.8.</summary>
        public static readonly float LaunchAngleMax = 70.0f; // TODO: replace with config loader (Stage 1)

        // ── §3.4 Spin Vector ────────────────────────────────────────────────────────

        /// <summary>[GT] Topspin base magnitude for Centre contact (rad/s). §3.4.2.</summary>
        public static readonly float TopspinBaseCentre = 25.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Topspin base magnitude for BelowCentre contact (rad/s). §3.4.2.</summary>
        public static readonly float TopspinBaseBelowCentre = 4.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Topspin base magnitude for OffCentre contact (rad/s). §3.4.2.</summary>
        public static readonly float TopspinBaseOffCentre = 8.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Backspin base magnitude for Centre contact (rad/s). §3.4.3.</summary>
        public static readonly float BackspinBaseCentre = 2.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Backspin base magnitude for BelowCentre contact (rad/s). §3.4.3.</summary>
        public static readonly float BackspinBaseBelowCentre = 30.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Backspin base magnitude for OffCentre contact (rad/s). §3.4.3.</summary>
        public static readonly float BackspinBaseOffCentre = 6.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Sidespin base magnitude for Centre contact (rad/s). §3.4.4.</summary>
        public static readonly float SidespinBaseCentre = 3.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Sidespin base magnitude for BelowCentre contact (rad/s). §3.4.4.</summary>
        public static readonly float SidespinBaseBelowCentre = 5.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Sidespin base magnitude for OffCentre contact (rad/s). §3.4.4.</summary>
        public static readonly float SidespinBaseOffCentre = 28.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] TechniqueScale lower bound (at Technique=1). §3.4.5.</summary>
        public static readonly float TechniqueSpinBase = 0.6f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] TechniqueScale upper bound (at Technique=20). §3.4.5.</summary>
        public static readonly float TechniqueSpinMax = 1.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Absolute maximum spin magnitude (rad/s). Matches Ball Physics MAX_SPIN. §3.4.10, XC-4.2-02.</summary>
        public static readonly float SpinAbsoluteMax = 80.0f; // TODO: replace with config loader (Stage 1)

        // ── §3.6 Error Model ────────────────────────────────────────────────────────

        /// <summary>[GT] Base error at worst effective attribute (degrees). §3.6.3.</summary>
        public static readonly float BaseErrorMax = 4.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Base error at best effective attribute (degrees). §3.6.3.</summary>
        public static readonly float BaseErrorMin = 0.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Power penalty coefficient in quadratic power–accuracy trade-off. §3.6.4, FR-03.</summary>
        public static readonly float PowerPenaltyCoefficient = 1.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum pressure penalty fraction (at full pressure, zero Composure). §3.6.5.</summary>
        public static readonly float PressureMaxPenalty = 0.8f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum fatigue-induced accuracy penalty fraction. §3.6.6.</summary>
        public static readonly float FatigueMaxPenalty = 0.4f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Body shape error coefficient; scales (1 - BMS)² penalty. §3.6.7.</summary>
        public static readonly float BodyShapeErrorCoefficient = 1.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum clamped error angle (degrees). No shot is laser-precise. §3.6.8.</summary>
        public static readonly float MinErrorAngle = 0.15f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum clamped error angle (degrees). Prevents multiplicative absurdities. §3.6.8.</summary>
        public static readonly float MaxErrorAngle = 25.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Spatial hash query radius for pressure detection (metres). §4.4.1.</summary>
        public static readonly float PressureRadiusMax = 3.0f; // TODO: replace with config loader (Stage 1)

        // ── §3.7 Body Mechanics ─────────────────────────────────────────────────────

        /// <summary>[GT] Ideal run-up approach angle relative to goal line (degrees). §3.7.3.</summary>
        public static readonly float IdealRunUpAngle = 37.5f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Run-up angle tolerance before penalty kicks in (degrees). §3.7.3.</summary>
        public static readonly float RunUpTolerance = 45.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Plant foot lateral offset tolerance (metres). Within this range = full score. §3.7.4.</summary>
        public static readonly float PlantFootTolerance = 0.35f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Agent velocity ideal range lower bound (m/s). §3.7.5.</summary>
        public static readonly float VelocityIdealMin = 1.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Agent velocity ideal range upper bound (m/s). §3.7.5.</summary>
        public static readonly float VelocityIdealMax = 5.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Velocity penalty scale for below-ideal range (m/s below VelocityIdealMin). §3.7.5.</summary>
        public static readonly float VelocityPenaltyScaleNegative = 3.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Velocity penalty scale for above-ideal range (m/s above VelocityIdealMax). §3.7.5.</summary>
        public static readonly float VelocityPenaltyScalePositive = 4.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Body lean tolerance before penalty kicks in (degrees). §3.7.6.</summary>
        public static readonly float LeanTolerance = 20.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Weight of run-up angle component in composite BodyMechanicsScore. §3.7.7.</summary>
        public static readonly float WeightRunUp = 0.25f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Weight of plant foot component in composite BodyMechanicsScore. §3.7.7.</summary>
        public static readonly float WeightPlant = 0.30f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Weight of agent velocity component in composite BodyMechanicsScore. §3.7.7.</summary>
        public static readonly float WeightVelocity = 0.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Weight of body lean component in composite BodyMechanicsScore. §3.7.7.</summary>
        public static readonly float WeightLean = 0.25f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] BodyMechanicsScore threshold below which stumble is triggered (if PowerIntent also exceeds threshold). §3.7.8, FR-08.</summary>
        public static readonly float StumbleThreshold = 0.25f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] PowerIntent threshold above which stumble can trigger. §3.7.8, FR-08.</summary>
        public static readonly float StumblePowerThreshold = 0.75f; // TODO: replace with config loader (Stage 1)

        // ── §3.8 Weak Foot ──────────────────────────────────────────────────────────

        /// <summary>[GT] Maximum error cone multiplier penalty at WeakFootRating=1. §3.8.3.</summary>
        public static readonly float WeakFootBaseErrorPenalty = 0.60f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum velocity penalty fraction at WeakFootRating=1. §3.8.4.</summary>
        public static readonly float WeakFootVelocityPenalty = 0.20f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum WeakFootRating value (no penalty at this rating). §3.8.3, §3.8.4.</summary>
        public static readonly int WeakFootRatingMax = 5; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Effective WeakFootRating range (WeakFootRatingMax - 1). §3.8.3, §3.8.4.</summary>
        public static readonly int WeakFootRatingRange = 4; // TODO: replace with config loader (Stage 1)

        // ── §3.3.6 Body Lean (Stage 0 approximation) ───────────────────────────────

        /// <summary>[GT] Maximum forward body lean angle (degrees) at full sprint. §3.3.6, §3.7.6.</summary>
        public static readonly float BodyLeanMaxDeg = 20.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Agent speed (m/s) at which maximum body lean is reached. §3.3.6.</summary>
        public static readonly float BodyLeanMaxSpeed = 5.0f; // TODO: replace with config loader (Stage 1)

        // ── §3.9 State Machine Timing ───────────────────────────────────────────────

        /// <summary>[GT] Windup frames at PowerIntent ≥ 0.80 (full backswing, ~233ms at 60Hz). §3.9.</summary>
        public static readonly int WindupFramesHighPower = 14; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Windup frames at PowerIntent ∈ [0.50, 0.80) (~167ms at 60Hz). §3.9.</summary>
        public static readonly int WindupFramesMedPower = 10; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Windup frames at PowerIntent &lt; 0.50 (stabbed/quick shot, ~117ms at 60Hz). §3.9.</summary>
        public static readonly int WindupFramesLowPower = 7; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] PowerIntent threshold for high-power windup branch. §3.9.</summary>
        public static readonly float WindupPowerHighThreshold = 0.80f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] PowerIntent threshold for medium-power windup branch. §3.9.</summary>
        public static readonly float WindupPowerMedThreshold = 0.50f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum additional windup frames added by SpinIntent (full spin = +3 frames). §3.9.</summary>
        public static readonly int WindupSpinBonusMax = 3; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Fixed follow-through duration (frames, ~133ms at 60Hz). §3.9.</summary>
        public static readonly int FollowThroughFrames = 8; // TODO: replace with config loader (Stage 1)

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
#endregion
