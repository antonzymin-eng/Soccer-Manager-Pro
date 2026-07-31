// File:     src/training-system/TrainingSystemConstants.cs
// Created:  2026-07-30
// Modified: 2026-07-31
// Author:   —
// Spec:     Training System #29 Appendix A; Code Standards #20 §3.2.3 (FR-CS-019), §4.2
// Purpose:  Constant catalogue for the deterministic daily training step.

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// Constant catalogue for Training System #29's Stage-2 daily core (Appendix A).
    /// <para>
    /// Magnitudes tagged <c>[GT]</c> are illustrative pending the Stage-2/3 balance pass (Appendix A /
    /// the #21 §9.2 precedent): the spec's contract is the shapes and directions, the numbers are
    /// tunable. Every scalar <c>[GT]</c> row is a <c>public static readonly</c> field reading
    /// <c>Config.GetInt/GetFloat("training-system", …)</c> with the current literal as its fallback,
    /// per FR-CS-019 and the June-30 tree-wide catalogue migration — a balance change must be a config
    /// change, not a code change, and a <c>const</c> inlines into consumers and cannot be bound.
    /// <c>[FIXED]</c> rows stay ALL_CAPS <c>const</c>.
    /// </para>
    /// <para>
    /// The two per-focus tables are array-valued and stay in-code literals per the documented
    /// array-table carve-out (<c>GameplayConfig</c> has no array getter; the
    /// <c>TacticalInstructionsConstants</c> precedent). They live here rather than inside
    /// <see cref="TrainingStep"/> so the designer-facing surface is one catalogue (FR-CS-016 — no magic
    /// numbers in formula files) and so the per-focus coverage lock (table length ==
    /// <see cref="TrainingFocus"/> member count — the <c>POSITION_COUNT</c> precedent) has something to
    /// bind to.
    /// </para>
    /// </summary>
    public static class TrainingSystemConstants
    {
        #region Fixed

        /// <summary>[FIXED] Future training sub-blob version; persistence lands at T1.</summary>
        public const uint TRAINING_SAVE_FORMAT_VERSION = 1;

        /// <summary>[FIXED] Informational weekly focus-selection cadence; training itself advances daily.</summary>
        public const int DAYS_PER_WEEK = 7;

        /// <summary>[FIXED] Fresh-state cursor value that cannot collide with legitimate world day zero.</summary>
        public const uint TRAINING_NOT_ADVANCED_SENTINEL = uint.MaxValue;

        #endregion

        #region GT

        /// <summary>[GT] Lower conditioning cursor bound (the F1 clamp floor).
        /// Config key <c>[training-system] ConditionMin</c>.</summary>
        public static readonly int ConditionMin = Config.GetInt("training-system", "ConditionMin", 0);

        /// <summary>[GT] Upper conditioning cursor bound (the F1 clamp ceiling).
        /// Config key <c>[training-system] ConditionMax</c>.</summary>
        public static readonly int ConditionMax = Config.GetInt("training-system", "ConditionMax", 10000);

        /// <summary>[GT] Initial conditioning cursor for a newly created training state.
        /// Config key <c>[training-system] ConditionStart</c>.</summary>
        public static readonly int ConditionStart = Config.GetInt("training-system", "ConditionStart", 7000);

        /// <summary>[GT] World-tick training-fatigue ceiling — distinct from match-tick fatigue (FR-TR-011).
        /// Config key <c>[training-system] TrainingFatigueMax</c>.</summary>
        public static readonly int TrainingFatigueMax =
            Config.GetInt("training-system", "TrainingFatigueMax", 10000);

        /// <summary>[GT] Passive world-day fatigue recovery, applied EVERY day regardless of focus (§3.1),
        /// so a non-Rest regime reaches a sub-max equilibrium instead of saturating to 1.0 match-entry
        /// fatigue. Config key <c>[training-system] FatigueDailyRecovery</c>.</summary>
        public static readonly int FatigueDailyRecovery =
            Config.GetInt("training-system", "FatigueDailyRecovery", 200);

        /// <summary>[GT] Multiplier from the training-fatigue fraction to match-entry fatigue (KD-1).
        /// Config key <c>[training-system] MatchEntryFatigueScale</c>.</summary>
        public static readonly float MatchEntryFatigueScale =
            Config.GetFloat("training-system", "MatchEntryFatigueScale", 1.0f);

        /// <summary>[GT] Injury-risk output upper bound (KD-5).
        /// Config key <c>[training-system] InjuryRiskMax</c>.</summary>
        public static readonly int InjuryRiskMax = Config.GetInt("training-system", "InjuryRiskMax", 10000);

        /// <summary>[GT] Training-fatigue contribution to the injury-risk input (§3.4).
        /// Config key <c>[training-system] FatigueRiskWeight</c>.</summary>
        public static readonly int FatigueRiskWeight = Config.GetInt("training-system", "FatigueRiskWeight", 1);

        /// <summary>[GT] Low-conditioning contribution to the injury-risk input (§3.4).
        /// Config key <c>[training-system] LowConditionRiskWeight</c>.</summary>
        public static readonly int LowConditionRiskWeight =
            Config.GetInt("training-system", "LowConditionRiskWeight", 1);

        // ── Own-attribute weight rows (the Appendix A "weights table" entries) ───────────────────
        // Named even at weight 1: without them the two sums in TrainingStep are implicit ×1 raw
        // additions a designer can neither see nor tune, and the two independent sums drift silently
        // the moment either is reweighted.

        /// <summary>[GT] <c>WorkRate</c> weight in the deterministic own-attribute conditioning bonus
        /// (§3.1 <c>AttributeConditioningBonus</c> — a function of the player's own attributes, never
        /// RNG, FR-TR-009). Config key <c>[training-system] ConditioningBonusWorkRateWeight</c>.</summary>
        public static readonly int ConditioningBonusWorkRateWeight =
            Config.GetInt("training-system", "ConditioningBonusWorkRateWeight", 1);

        /// <summary>[GT] <c>Stamina</c> weight in the deterministic own-attribute conditioning bonus (§3.1).
        /// Config key <c>[training-system] ConditioningBonusStaminaWeight</c>.</summary>
        public static readonly int ConditioningBonusStaminaWeight =
            Config.GetInt("training-system", "ConditioningBonusStaminaWeight", 1);

        /// <summary>[GT] <c>Strength</c> weight in the deterministic own-attribute injury mitigation
        /// (§3.4 <c>RobustnessMitigation</c>).
        /// Config key <c>[training-system] RobustnessStrengthWeight</c>.</summary>
        public static readonly int RobustnessStrengthWeight =
            Config.GetInt("training-system", "RobustnessStrengthWeight", 1);

        /// <summary>[GT] <c>Stamina</c> weight in the deterministic own-attribute injury mitigation (§3.4).
        /// Config key <c>[training-system] RobustnessStaminaWeight</c>.</summary>
        public static readonly int RobustnessStaminaWeight =
            Config.GetInt("training-system", "RobustnessStaminaWeight", 1);

        // ── Per-focus daily tables (Appendix A; array-table carve-out — see the class remarks) ───
        // Indexed by the TrainingFocus ORDINAL: Balanced, Rest, Fitness, Technical, Physical, Tactical.
        // Both tables MUST carry one row per enum member — TrainingSystemConstantsTests locks that, so
        // a new focus cannot be added without its rows.

        /// <summary>
        /// [GT] Per-focus daily conditioning delta, indexed by <c>(int)</c><see cref="TrainingFocus"/>
        /// (Appendix A <c>FocusConditionDelta[Focus]</c>). Rest is a small positive (recovery still
        /// conditions), Fitness the largest, Balanced/Tactical mid.
        /// </summary>
        public static readonly int[] FocusConditionDelta =
        {
            70,   // Balanced
            30,   // Rest
            120,  // Fitness
            80,   // Technical
            100,  // Physical
            70,   // Tactical
        };

        /// <summary>
        /// [GT] Per-focus daily training <b>load</b> BEFORE recovery, indexed by
        /// <c>(int)</c><see cref="TrainingFocus"/> (Appendix A <c>FocusFatigueDelta[Focus]</c>). Net
        /// daily fatigue is this minus <see cref="FatigueDailyRecovery"/>, so Rest (load 0) nets
        /// strongly negative while Fitness/Physical net positive (§3.1).
        /// </summary>
        public static readonly int[] FocusFatigueDelta =
        {
            220,  // Balanced
            0,    // Rest
            300,  // Fitness
            200,  // Technical
            280,  // Physical
            180,  // Tactical
        };

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-30 | —      | Initial Stage-2 core.   |
// | 1.1     | 2026-07-31 | —      | AR-1 M-2/M-3/L-4. M-2: every scalar [GT] row converted from ALL_CAPS
// |         |            |        | `const` to PascalCase `static readonly` reading
// |         |            |        | `Config.GetInt/GetFloat("training-system", …)` with the current literals
// |         |            |        | as fallbacks (behaviour-neutral; FR-CS-019 + the June-30 tree-wide
// |         |            |        | migration — a `const` inlines into consumers and is structurally locked
// |         |            |        | out of the config surface). [FIXED] rows unchanged. M-3: the Appendix A
// |         |            |        | per-focus tables moved here from `TrainingStep`'s switch statements as
// |         |            |        | focus-ordinal-indexed arrays (values unchanged; array-table carve-out),
// |         |            |        | plus the four named own-attribute weight rows the implicit ×1 sums
// |         |            |        | lacked. L-4: `Author:` header line added.
#endregion
