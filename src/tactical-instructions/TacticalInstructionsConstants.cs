// File:     src/tactical-instructions/TacticalInstructionsConstants.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 Appendix A, §3.2–§3.4, §5.6, Code Standards #20
// Modified: 2026-06-30 (§5.6 / G2 balance pass — [GT] magnitudes pinned)
// Modified: 2026-07-07 (+ MarkingOrientationScalar[3])
// Purpose:  Single constant catalogue for the Tactical Instructions layer. The [GT]
//           magnitudes are the §5.6 (G2) balance-pass-PINNED Stage-1 defaults (June 30, 2026);
//           the identity rows are exact 1.0 / 0.0 so a default tactic reproduces today's
//           behaviour (FR-TI-031). No magic numbers live elsewhere in this assembly (FR-CS-016).

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Constant catalogue for #21 (Appendix A). Region order Fixed → Derived → GT (FR-TI-010).
    /// The multiplier/scalar/table values are the §5.6 / G2 balance-pass-PINNED Stage-1 defaults
    /// (pinned June 30, 2026). The identity rows (index that maps to "no instruction") are exact
    /// 1.0 / 0.0 so a default tactic reproduces today's behaviour (FR-TI-031).
    ///
    /// BALANCE PASS (§5.6 / G2) — pinned June 30, 2026. The pinned values satisfy the numerical-mirror
    /// invariants the balance review checked, and <c>BalancePassInvariantsTests</c> locks them:
    ///   • every identity row is EXACT (Mentality.Balanced ⇒ 1.0/0.0; InstrBias.Default ⇒ 1.0;
    ///     Tempo.Standard / TacticWidth.Standard / … / PlayerRole.Default rows ⇒ 1.0; Duty.Support ⇒ 0.0),
    ///     so a default tactic is byte-identical to pre-#21 (FR-TI-031);
    ///   • the Mentality risk/line and width/line-of-engagement tables are STRICTLY MONOTONIC in their
    ///     dial direction (bolder ⇒ higher risk + higher line; wider ⇒ higher compactness scalar), so the
    ///     7-/5-/3-step gradation is not collapsed (the §3.2 "gradation carrier" requirement);
    ///   • every RoleWeightModifiers cell ∈ [0.5, 2.0] (T-TI-U-029) with the §3.3 directional shapes
    ///     (Poacher SHOOT↑/HOLD↓; DeepLyingPlaymaker PASS↑/DRIBBLE↓; BallWinningMid PRESS+INTERCEPT↑).
    /// The shapes/directions remain the normative reviewable contract; the magnitudes are now committed
    /// (no longer "illustrative"). The Stage-1 [GT] config-loader (FR-CS-019) may still surface these as
    /// tunables, but the in-code defaults are the pinned values below.
    ///
    /// COLUMN ORDER for the per-ActionType tables (<see cref="TempoActionBias"/>,
    /// <see cref="RoleWeightModifiers"/>): the column index is the #8 <c>ActionType</c> ordinal —
    /// 0 PASS, 1 SHOOT, 2 DRIBBLE, 3 HOLD, 4 MOVE_TO_POSITION, 5 PRESS, 6 INTERCEPT. The consumer
    /// indexes with <c>(int)opt.Type</c>; this layer stores raw float rows and never references the
    /// #8 enum (KD-2).
    /// </summary>
    public static class TacticalInstructionsConstants
    {
        #region Fixed

        /// <summary>[FIXED] Cardinality of <see cref="Mentality"/> (VeryDefensive…VeryAttacking). A.1.</summary>
        public const int MENTALITY_LEVELS = 7;

        /// <summary>[FIXED] Cardinality of <see cref="InstrBias"/> (Less/Default/More). A.1.</summary>
        public const int INSTR_BIAS_LEVELS = 3;

        /// <summary>[FIXED] Upper bound of the TimeWasting dial [0..4]. A.1.</summary>
        public const int TIME_WASTING_MAX = 4;

        /// <summary>
        /// [FIXED] Sentinel for "no man-mark target" in
        /// <see cref="PlayerInstructions.MarkTargetEntityId"/> (≥0 = a man-mark request). §2.2.2.
        /// </summary>
        public const int MARK_TARGET_NONE = -1;

        #endregion

        #region Derived

        // NOTE: these are expression-bodied properties, NOT static readonly fields, so they are
        // computed on access rather than at static-init time. The #20 region order (Fixed → Derived →
        // GT) places these BEFORE their GT source arrays textually; a static field initializer would
        // run while MentalityRiskMult/MentalityLineBias are still null → TypeInitializationException.
        // This is the same static-init-order trap fixed in Perception (BASE_FOV_HALF_ANGLE) and
        // EventRegistry; expression-bodied properties dodge it entirely.

        /// <summary>
        /// [DERIVED] Mentality risk multiplier of the identity row.
        /// Formula: <c>MentalityRiskMult[(int)Mentality.Balanced]</c> = 1.0 (FR-CS-021). A.2 / FR-TI-031.
        /// Source: <see cref="MentalityRiskMult"/>, <see cref="Mentality.Balanced"/>.
        /// </summary>
        public static float RiskMultBalanced => MentalityRiskMult[(int)Mentality.Balanced];

        /// <summary>
        /// [DERIVED] Mentality defensive-line bias of the identity row.
        /// Formula: <c>MentalityLineBias[(int)Mentality.Balanced]</c> = 0.0 (FR-CS-021). A.2.
        /// Source: <see cref="MentalityLineBias"/>, <see cref="Mentality.Balanced"/>.
        /// </summary>
        public static float LineBiasBalanced => MentalityLineBias[(int)Mentality.Balanced];

        #endregion

        #region GT

        /// <summary>[GT] Per-<see cref="Mentality"/> utility multiplier (×#8 utility, before clamp). §3.2.</summary>
        public static readonly float[] MentalityRiskMult =
            { 0.80f, 0.88f, 0.94f, 1.00f, 1.06f, 1.14f, 1.20f }; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Per-<see cref="Mentality"/> additive defensive-line bias (+DefensiveLine, then Clamp01). §3.2.</summary>
        public static readonly float[] MentalityLineBias =
            { -0.20f, -0.12f, -0.05f, 0.00f, 0.05f, 0.12f, 0.20f }; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Per-<see cref="InstrBias"/> multiplier {Less, Default, More}; Default = identity. §3.4.</summary>
        public static readonly float[] InstrBiasMult =
            { 0.85f, 1.00f, 1.15f }; // TODO: replace with config loader (Stage 1)

        /// <summary>
        /// [GT] Per-(<see cref="Tempo"/>, ActionType) forward-vs-retain weighting (§3.3). Rows indexed by
        /// <c>(int)Tempo</c>; columns by ActionType ordinal (see class remarks). The Standard row
        /// (index 2) is exact identity (all 1.0); higher tempo raises PASS/SHOOT and lowers HOLD.
        /// </summary>
        public static readonly float[][] TempoActionBias =
        { // TODO: replace with config loader (Stage 1)
            //  PASS  SHOOT DRIB  HOLD  MOVE  PRESS INTER
            new[] { 0.90f, 0.90f, 0.95f, 1.15f, 1.00f, 1.00f, 1.00f }, // VerySlow
            new[] { 0.95f, 0.95f, 0.97f, 1.08f, 1.00f, 1.00f, 1.00f }, // Slow
            new[] { 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f }, // Standard (identity)
            new[] { 1.05f, 1.05f, 1.03f, 0.92f, 1.00f, 1.00f, 1.00f }, // Fast
            new[] { 1.10f, 1.10f, 1.05f, 0.85f, 1.00f, 1.00f, 1.00f }  // VeryFast
        };

        /// <summary>[GT] Per-<see cref="Tempo"/> #8 OptionGenerator breadth scalar; Standard = identity. §3.3.</summary>
        public static readonly float[] TempoBreadthScalar =
            { 0.90f, 0.95f, 1.00f, 1.05f, 1.10f }; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Per-<see cref="TacticWidth"/> #12 compactness scalar; Standard = identity. §3.4.</summary>
        public static readonly float[] WidthScalar =
            { 0.85f, 0.92f, 1.00f, 1.08f, 1.15f }; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Per-<see cref="TacticDefWidth"/> #12 OOP compactness scalar; Standard = identity. §3.4.</summary>
        public static readonly float[] DefWidthScalar =
            { 0.90f, 1.00f, 1.10f }; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Per-<see cref="LineOfEngagement"/> scalar on #13 trigger radius; Standard = identity. §3.4.</summary>
        public static readonly float[] LineOfEngagementScalar =
            { 0.80f, 0.90f, 1.00f, 1.10f, 1.20f }; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Per-<see cref="MarkingOrientation"/> scalar on #14 MAN_MARK candidate radius; Balanced = identity. §3.4.</summary>
        public static readonly float[] MarkingOrientationScalar =
            { 0.80f, 1.00f, 1.20f }; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Per-<see cref="Duty"/> fore/aft positioning offset (m) {Defend, Support, Attack}; Support = identity. §3.4 / #12.</summary>
        public static readonly float[] DutyForeOffsetM =
            { -3.0f, 0.0f, 3.0f }; // TODO: replace with config loader (Stage 1)

        /// <summary>[GT] Per-<see cref="Duty"/> additive #8 aggression bias {Defend, Support, Attack}; Support = identity. §3.4.</summary>
        public static readonly float[] DutyAggressionBias =
            { -0.05f, 0.0f, 0.05f }; // TODO: replace with config loader (Stage 1)

        /// <summary>
        /// [GT] Per-(<see cref="PlayerRole"/>, ActionType) #8 utility multiplier (§3.3 / A.4). Rows indexed
        /// by <c>(int)PlayerRole</c>; columns by ActionType ordinal (see class remarks). The Default row
        /// (index 0) is exact identity (all 1.0; FR-TI-031). All cells ∈ [0.5, 2.0] (T-TI-U-029).
        /// Magnitudes illustrative; directions are the reviewable contract.
        /// </summary>
        public static readonly float[][] RoleWeightModifiers =
        { // TODO: replace with config loader (Stage 1)
            //  PASS  SHOOT DRIB  HOLD  MOVE  PRESS INTER
            new[] { 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f, 1.00f }, // Default (identity)
            new[] { 0.95f, 1.25f, 1.00f, 0.80f, 1.05f, 0.90f, 0.95f }, // Poacher
            new[] { 1.20f, 0.90f, 0.90f, 1.05f, 1.00f, 0.95f, 1.05f }, // DeepLyingPlaymaker
            new[] { 0.95f, 0.90f, 0.90f, 1.00f, 1.00f, 1.25f, 1.20f }, // BallWinningMid
            new[] { 1.00f, 1.15f, 1.15f, 0.85f, 1.05f, 0.95f, 0.95f }, // InsideForward
            new[] { 1.05f, 1.10f, 0.85f, 1.10f, 0.90f, 0.90f, 0.95f }  // TargetMan
        };

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
// | 1.1     | 2026-06-30 | —      | §5.6 / G2 balance pass: [GT] magnitudes PINNED (values unchanged — |
// |         |            |        |   already spec-aligned + monotonic). Header/class doc reframed     |
// |         |            |        |   illustrative → pinned; numerical-mirror invariants documented    |
// |         |            |        |   (identity-row exactness, strict monotonicity, [0.5,2.0] role     |
// |         |            |        |   bounds). Locked by BalancePassInvariantsTests.                   |
// | 1.2     | 2026-07-07 | —      | Cheap-item addition: + MarkingOrientationScalar[3] (§3.4, #14 MAN_ |
// |         |            |        |   MARK candidate radius); Balanced row = identity ×1.00.           |
#endregion
