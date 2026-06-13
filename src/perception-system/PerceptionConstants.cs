// File:     src/perception-system/PerceptionConstants.cs
// Created:  2026-05-28
// Modified: 2026-06-12
// Author:   —
// Spec:     Perception System #7 §3.10, Code Standards #20
// Purpose:  All numeric constants for the perception system. No magic literals in formula files.
//           Region order: Fixed → Derived → Cross → GT.
//           18 constants from §3.10 + system-sizing constants.

using TacticalDirector.FirstTouch;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// All constants for the Perception System. Every symbol used in pipeline calculations
    /// appears here with its source tag per §3.10 / CLAUDE.md Constant Tags.
    /// No magic literals permitted in any formula or system file.
    /// Perception System #7 §3.10.
    /// </summary>
    public static class PerceptionConstants
    {
        #region Fixed

        /// <summary>[FIXED] Sentinel value indicating no agent (matches First Touch / Collision System convention). §4.6.3.</summary>
        public const int AGENT_ID_NONE = -1;

        /// <summary>[FIXED] Maximum value of any player attribute [1–20]. Agent Movement #2 §3.5.6.</summary>
        public const float ATTR_MAX = 20.0f;

        /// <summary>[FIXED] Minimum value of any player attribute [1–20]. Agent Movement #2 §3.5.6.</summary>
        public const float ATTR_MIN = 1.0f;

        /// <summary>[FIXED] Attribute range denominator for linear interpolation across [1–20]. (ATTR_MAX - ATTR_MIN) = 19. §3.3.2.</summary>
        public const float ATTR_RANGE = 19.0f;

        /// <summary>[FIXED] Conversion factor: multiply radians by this to get degrees. Mathf.Rad2Deg equivalent.</summary>
        public const float RAD_TO_DEG = 57.29578f;

        /// <summary>[FIXED] Full circle in degrees. Used in bearing wrap arithmetic.</summary>
        public const float FULL_CIRCLE_DEG = 360.0f;

        /// <summary>[FIXED] Half circle in degrees. Used in bearing wrap normalisation to [−180°, +180°].</summary>
        public const float HALF_CIRCLE_DEG = 180.0f;

        /// <summary>[FIXED] Guard value added to occluder distance denominator to prevent division by zero in shadow-cone formula. §3.2.3.</summary>
        public const float OCCLUDER_DIST_GUARD = 0.1f;

        /// <summary>[FIXED] Square-magnitude threshold below which a facing direction is treated as the zero vector. FM-AM-03 guard. §4.2.4.</summary>
        public const float FACING_DIR_ZERO_GUARD_SQ = 1e-6f;

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Base FoV half-angle (degrees). Formula: BASE_FOV_ANGLE / 2 = 80°.
        /// Perception System #7 §3.1.2. Source constants: BASE_FOV_ANGLE (GT, below).
        /// Expression-bodied property, NOT a static readonly field: C# initialises static
        /// fields in textual order, and the documented region order (Fixed → Derived → Cross → GT)
        /// places this declaration BEFORE its GT source — a readonly field initialiser here
        /// reads BASE_FOV_ANGLE as 0 (CI gate 2026-06-12; sibling of the EventRegistry
        /// static-init-order defect). The property defers evaluation past static init.
        /// </summary>
        public static float BASE_FOV_HALF_ANGLE => BASE_FOV_ANGLE / 2.0f;

        /// <summary>
        /// [DERIVED] Peripheral arc inner bound (degrees). Formula: BASE_FOV_HALF_ANGLE / 2 = 40°.
        /// Entities at angular separation [40°, 80°] from facing are in the peripheral arc.
        /// Perception System #7 §3.3.3. Source constants: BASE_FOV_HALF_ANGLE (Derived, above).
        /// Expression-bodied property for the same static-init-order reason as BASE_FOV_HALF_ANGLE.
        /// </summary>
        public static float PERIPHERAL_ARC_INNER_BOUND => BASE_FOV_HALF_ANGLE / 2.0f;

        /// <summary>
        /// [DERIVED] Confirmation expiry window (heartbeat ticks). Formula: minimum window = 1 tick.
        /// Shadow cone boundary crossings resolve in ≤1 tick; no GT tuning required.
        /// Perception System #7 §3.3.6. Source constant: tick system.
        /// </summary>
        public static readonly int CONFIRMATION_EXPIRY_TICKS = 1;

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS] Pressure radius (m). Authoritative source: FirstTouchConstants.PressureRadius.
        /// First Touch Mechanics #4 §3.5.1. Value: 3.0m.
        /// </summary>
        public static readonly float PressureRadius = FirstTouchConstants.PressureRadius;

        /// <summary>
        /// [CROSS] Minimum pressure distance (m) — prevents division by zero in pressure formula.
        /// Authoritative source: FirstTouchConstants.MinPressureDistance.
        /// First Touch Mechanics #4 §3.5.2. Value: 0.3m.
        /// </summary>
        public static readonly float MinPressureDistance = FirstTouchConstants.MinPressureDistance;

        /// <summary>
        /// [CROSS] Pressure saturation value — denominator for PressureScalar clamp.
        /// Authoritative source: FirstTouchConstants.PressureSaturation.
        /// First Touch Mechanics #4 §3.5.3. Value: 1.5.
        /// </summary>
        public static readonly float PressureSaturation = FirstTouchConstants.PressureSaturation;

        /// <summary>
        /// [CROSS] Half-turn L_rec reduction multiplier (15% reduction = ×0.85).
        /// Authoritative source: FirstTouchConstants.HalfTurnLRecReduction.
        /// First Touch Mechanics #4 §3.3.2. Consumed read-only here.
        /// Perception System #7 §3.3.3.
        /// </summary>
        public static readonly float HalfTurnLRecReduction = FirstTouchConstants.HalfTurnLRecReduction;

        /// <summary>
        /// [CROSS] Tactical AI loop tick rate (Hz). Declared const (not static readonly) so Derived
        /// constants that depend on it evaluate correctly regardless of static-init order.
        /// Authoritative source: CLAUDE.md Heartbeat Tick Rate.
        /// Value: 10 Hz. TODO: mirror from ProjectConstants.TickRateTacticalHz when created.
        /// </summary>
        public const float TickRateTacticalHz = 10.0f;

        #endregion

        #region GT

        // ── §3.1 Field of View ───────────────────────────────────────────────────────

        /// <summary>[GT] Base field-of-view full angle (degrees). Within Franks 1985 plausibility range 140°–180°.
        /// Perception System #7 §3.1.2.</summary>
        public static readonly float BASE_FOV_ANGLE = 160.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum FoV bonus from Decisions attribute (degrees). Williams &amp; Davids 1998 expert–novice data.
        /// Perception System #7 §3.1.3.</summary>
        public static readonly float MAX_FOV_BONUS_ANGLE = 10.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum FoV reduction under full pressure (degrees). Beilock 2010: 18% of 170° ≈ 30°.
        /// Perception System #7 §3.1.4.</summary>
        public static readonly float MAX_FOV_PRESSURE_REDUCTION = 30.0f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Absolute minimum effective FoV angle (degrees). Safety floor; prevents degenerate near-zero cone.
        /// Perception System #7 §3.1.4.</summary>
        public static readonly float MIN_FOV_ANGLE = 120.0f; // TODO: replace with config loader (Stage 1)

        // ── §3.2 Occlusion ───────────────────────────────────────────────────────────

        /// <summary>[GT] Agent body radius for shadow cone projection (m). Adult shoulder half-width; consistent with Agent Movement §3.2.
        /// Perception System #7 §3.2.2.</summary>
        public static readonly float AGENT_BODY_RADIUS = 0.4f; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum shadow cone half-angle (degrees). Prevents zero-width cones at extreme range.
        /// Perception System #7 §3.2.3.</summary>
        public static readonly float MIN_SHADOW_HALF_ANGLE = 5.0f; // TODO: replace with config loader (Stage 1)

        // ── §3.3 Recognition Latency ─────────────────────────────────────────────────

        /// <summary>[GT] Maximum recognition latency (heartbeat ticks). 500ms for Decisions=1.
        /// Franks 1985 upper bracket for novice recognition. Perception System #7 §3.3.2.</summary>
        public static readonly int L_MAX = 5; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum recognition latency (heartbeat ticks). 100ms for Decisions=20.
        /// Helsen 1999 expert lower bracket. Perception System #7 §3.3.2.</summary>
        public static readonly int L_MIN = 1; // TODO: replace with config loader (Stage 1)

        // ── §3.4 Shoulder Check ──────────────────────────────────────────────────────

        /// <summary>[GT] Maximum shoulder check interval (heartbeat ticks). 3.0s for Anticipation=1.
        /// Franks 1985: average player scans every 3–5s. Perception System #7 §3.4.2.</summary>
        public static readonly int CHECK_MAX_TICKS = 30; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Minimum shoulder check interval (heartbeat ticks). 0.6s for Anticipation=20.
        /// Master Vol 1 §3.1: 6–8 elite scans per possession. Perception System #7 §3.4.2.</summary>
        public static readonly int CHECK_MIN_TICKS = 6; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Shoulder check window duration (heartbeat ticks). 300ms.
        /// Master Vol 1 §3.1: ~0.3s for a shoulder check movement. Perception System #7 §3.4.3.</summary>
        public static readonly int SHOULDER_CHECK_DURATION = 3; // TODO: replace with config loader (Stage 1)

        // ── §3.1 / §3.5 Range ───────────────────────────────────────────────────────

        /// <summary>[GT] Maximum perception range (m). Full pitch diagonal; effectively uncapped.
        /// Stage 2 weather/fog effects will reduce this dynamically (OQ-5 resolution).
        /// Perception System #7 §3.1.2, §3.5.1.</summary>
        public static readonly float MaxPerceptionRange = 120.0f; // TODO: replace with config loader (Stage 1)

        // ── System sizing ────────────────────────────────────────────────────────────

        /// <summary>[GT] Total number of active agents per match (22 = 11 per team). §2.4.8.</summary>
        public static readonly int MaxAgents = 22; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum teammates visible per agent (10 outfield + 1 GK = 11 minus self). §2.3.1.</summary>
        public static readonly int MaxVisibleTeammates = 11; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum opponents visible per agent (11 total). §2.3.1.</summary>
        public static readonly int MaxVisibleOpponents = 11; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Maximum agents perceived via shoulder check window. Bounded by MaxAgents. §3.4.3.</summary>
        public static readonly int MaxBlindSideAgents = 22; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Jitter range (ticks) for shoulder check scheduling. DeterministicHash % (2*JITTER_RANGE+1) - JITTER_RANGE. §3.4.2.</summary>
        public static readonly int ShoulderCheckJitterRange = 2; // TODO: replace with config loader (Stage 1)

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                                                                      |
// | 1.1     | 2026-05-28 | —      | AR-1 fix M-3: HalfTurnLRecReduction now sourced from FirstTouchConstants (CROSS tag contract satisfied).      |
// | 1.2     | 2026-06-12 | —      | Dotnet-CI quarantine adjudication (PRODUCTION-DEFECT, static-init-order class — EventRegistry sibling): BASE_FOV_HALF_ANGLE and PERIPHERAL_ARC_INNER_BOUND were static readonly fields initialised in textual region order (Fixed → Derived → Cross → GT) BEFORE their [GT] source BASE_FOV_ANGLE, so both read 0 at runtime; converted to expression-bodied properties (evaluation deferred past static init). Values unchanged: 80° / 40°. |
#endregion
