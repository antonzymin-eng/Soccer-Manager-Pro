// File:     src/training-system/TrainingSystemConstants.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Training System #29 Appendix A (constant catalogue) + §3.1/§3.3/§3.4; Code Standards #20
// Purpose:  Every numeric constant for #29 conditioning, training-fatigue accrual, the match-entry
//           fatigue projection and the injury-risk output. No magic literals in TrainingStep.

using System;

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// Constant catalogue for Training System #29 (Appendix A). Region order (Code Standards #20):
    /// Fixed → GT. The <c>[GT]</c> magnitudes are illustrative pending the Stage-2/3 balance pass —
    /// the shapes and directions are the reviewed contract (the #21 G2 / #26 precedent).
    /// <para>
    /// <b>No RNG constant appears here.</b> #29 registers no stream and issues no draw (FR-TR-008 /
    /// KD-6), so <c>_RESERVED_0x21_</c> and <c>SubsystemOrdinals</c> 83 stay reserved in #16 §3.4 —
    /// mirroring either here would be the phantom-surface class FR-LW-031 forbids.
    /// </para>
    /// <para>
    /// The two per-focus tables are array-valued, and <c>GameplayConfig</c> has no array getter, so
    /// they stay compile-time literals under the documented <c>src/CLAUDE.md</c> carve-out rather than
    /// taking a <c>Config.GetInt</c> call each. They are reached through
    /// <see cref="ConditionDeltaFor"/> / <see cref="FatigueLoadFor"/> so no caller can hold a mutable
    /// reference to the backing array.
    /// </para>
    /// </summary>
    public static class TrainingSystemConstants
    {
        #region Fixed

        /// <summary>[FIXED] The #29 sub-blob version (KD-7 / FR-TR-018). Independently gated from every other format version. Declared now, consumed at T1.</summary>
        public const uint TRAINING_SAVE_FORMAT_VERSION = 1;

        /// <summary>[FIXED] World-days per focus-selection cycle (Appendix A). Informational — the step itself is daily (KD-4, FR-TR-014); nothing in #29 batches on a week boundary.</summary>
        public const int DAYS_PER_WEEK = 7;

        /// <summary>
        /// [FIXED] "Never advanced" seed for <see cref="TrainingState.LastAdvancedWorldDay"/>.
        /// <c>uint.MaxValue</c> and NOT 0, so a legitimate world-day 0 cannot collide with the
        /// fresh-state value (the day-0 double-accrual trap, F6 — the #28 lifecycle precedent).
        /// </summary>
        public const uint TRAINING_NOT_ADVANCED_SENTINEL = uint.MaxValue;

        #endregion

        #region GT

        /// <summary>[GT] Conditioning cursor floor (Appendix A). Config key [training-system] ConditionMin.</summary>
        public static readonly int ConditionMin = Config.GetInt("training-system", "ConditionMin", 0);

        /// <summary>[GT] Conditioning cursor ceiling (Appendix A, F1 clamp). Config key [training-system] ConditionMax.</summary>
        public static readonly int ConditionMax = Config.GetInt("training-system", "ConditionMax", 10000);

        /// <summary>[GT] New-game / new-player conditioning seed (Appendix A). Config key [training-system] ConditionStart.</summary>
        public static readonly int ConditionStart = Config.GetInt("training-system", "ConditionStart", 7000);

        /// <summary>[GT] World-tick training-fatigue accumulator ceiling — distinct from match-tick fatigue (FR-TR-011). Config key [training-system] TrainingFatigueMax.</summary>
        public static readonly int TrainingFatigueMax = Config.GetInt("training-system", "TrainingFatigueMax", 10000);

        /// <summary>
        /// [GT] Passive daily fatigue recovery, applied EVERY day regardless of focus (§3.1 step 2), so
        /// net daily fatigue is <c>FocusFatigueDelta − FatigueDailyRecovery</c> and a Rest regime nets
        /// strongly negative. Config key [training-system] FatigueDailyRecovery.
        /// </summary>
        public static readonly int FatigueDailyRecovery = Config.GetInt("training-system", "FatigueDailyRecovery", 200);

        /// <summary>[GT] KD-1 projection scale: training-fatigue fraction → match-entry starting-fatigue offset (§3.3). Config key [training-system] MatchEntryFatigueScale.</summary>
        public static readonly float MatchEntryFatigueScale = Config.GetFloat("training-system", "MatchEntryFatigueScale", 1.0f);

        /// <summary>
        /// [GT] Weight on the mean of the player's own <c>WorkRate</c>/<c>Stamina</c> in
        /// <c>AttributeConditioningBonus</c> (§3.1 step 1) — a deterministic own-attribute term, never
        /// RNG (FR-TR-009). Config key [training-system] ConditioningAttributeWeight.
        /// </summary>
        public static readonly int ConditioningAttributeWeight = Config.GetInt("training-system", "ConditioningAttributeWeight", 2);

        /// <summary>[GT] KD-5 injury-risk weight on the training-fatigue accumulator (§3.4). Config key [training-system] FatigueRiskWeight.</summary>
        public static readonly int FatigueRiskWeight = Config.GetInt("training-system", "FatigueRiskWeight", 1);

        /// <summary>[GT] KD-5 injury-risk weight on the conditioning shortfall <c>(ConditionMax − Condition)</c> (§3.4). Config key [training-system] LowConditionRiskWeight.</summary>
        public static readonly int LowConditionRiskWeight = Config.GetInt("training-system", "LowConditionRiskWeight", 1);

        /// <summary>
        /// [GT] Risk-scalar clamp ceiling for <see cref="InjuryRiskContribution.RiskScore"/> (§3.4).
        /// <para>
        /// <b>This catalogue is the sole owner of the scale, and this is its only config key.</b> #41
        /// assembles its occurrence risk on the same scale and derives its draw denominator from it
        /// (#41 §3.4), so it <c>[CROSS]</c>-mirrors this field rather than declaring one of its own —
        /// a second key under <c>[injuries-medical]</c> would let one side be set without the other,
        /// silently rescaling every occurrence probability (ERR-041-003). Changing the value here
        /// changes both sides at once, which is the property the mirror exists to give.
        /// </para>
        /// Config key [training-system] InjuryRiskMax.
        /// </summary>
        public static readonly int InjuryRiskMax = Config.GetInt("training-system", "InjuryRiskMax", 10000);

        /// <summary>
        /// [GT] Injury-risk mitigation per point of the player's mean robustness attribute
        /// (<c>Strength</c>/<c>Stamina</c>/<c>Balance</c>) — deterministic, never RNG (FR-TR-009).
        /// <para>
        /// <b>This value cannot be tuned in isolation.</b> #41 subtracts its own mitigation term, over
        /// the same three attributes, from the scalar this one has already reduced (#41 FR-MD-015), so
        /// a player's robustness is priced into the occurrence probability twice and the two
        /// <c>[GT]</c> tables compose multiplicatively in effect. Each spec mandates its own term, so
        /// the duplication is spec-faithful rather than a defect — but it is a fact the balance pass
        /// inherits, not one it should rediscover. Recorded under ERR-041-003 and pinned by
        /// <c>MedicalStepTests.TrainingRiskFlowsFromTheProducerIntoTheOccurrenceRisk_TTMDFAT001</c>,
        /// which asserts the consequence: #29's saturated maximum never reaches #41's ceiling, so
        /// "maximum risk" here never means certain occurrence there.
        /// </para>
        /// Config key [training-system] RobustnessMitigationPerPoint.
        /// </summary>
        public static readonly int RobustnessMitigationPerPoint = Config.GetInt("training-system", "RobustnessMitigationPerPoint", 40);

        // [GT] Per-focus daily conditioning delta (Appendix A). Indexed by TrainingFocus ordinal.
        // Rest is a small positive (recovery still conditions a little); Fitness is the largest;
        // Balanced sits between. TODO: replace with config loader (Stage 1) — array-valued, see the
        // type doc's carve-out note.
        private static readonly int[] s_focusConditionDelta = { 60, 30, 120, 40, 100, 40 };

        // [GT] Per-focus daily training LOAD, before FatigueDailyRecovery is subtracted (Appendix A).
        // Indexed by TrainingFocus ordinal. Rest is 0 (pure recovery); Physical is the heaviest.
        // TODO: replace with config loader (Stage 1) — array-valued, see the type doc's carve-out note.
        private static readonly int[] s_focusFatigueDelta = { 200, 0, 300, 180, 320, 180 };

        #endregion

        /// <summary>The number of defined <see cref="TrainingFocus"/> ordinals — the length both per-focus tables MUST have.</summary>
        public static int FocusCount => s_focusConditionDelta.Length;

        /// <summary>
        /// [GT] The daily conditioning delta for a focus (§3.1 step 1).
        /// </summary>
        /// <param name="focus">The player's persistent focus.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="focus"/> is not a defined ordinal — an out-of-contract focus fails loud
        /// rather than being clamped to Balanced (F4 / FR-TR-021).
        /// </exception>
        public static int ConditionDeltaFor(TrainingFocus focus)
        {
            int ordinal = (int)focus;
            if ((uint)ordinal >= (uint)s_focusConditionDelta.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(focus), focus, "Undefined TrainingFocus ordinal (F4).");
            }

            return s_focusConditionDelta[ordinal];
        }

        /// <summary>
        /// [GT] The daily training LOAD for a focus, before <see cref="FatigueDailyRecovery"/> is
        /// subtracted (§3.1 step 2).
        /// </summary>
        /// <param name="focus">The player's persistent focus.</param>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="focus"/> is not a defined ordinal (F4 / FR-TR-021).
        /// </exception>
        public static int FatigueLoadFor(TrainingFocus focus)
        {
            int ordinal = (int)focus;
            if ((uint)ordinal >= (uint)s_focusFatigueDelta.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(focus), focus, "Undefined TrainingFocus ordinal (F4).");
            }

            return s_focusFatigueDelta[ordinal];
        }

        /// <summary>True iff <paramref name="focus"/> is a defined <see cref="TrainingFocus"/> ordinal (the F4 predicate).</summary>
        public static bool IsDefinedFocus(TrainingFocus focus) => (uint)focus < (uint)s_focusConditionDelta.Length;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                               |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#29 T0): Appendix A catalogue.              |
// | 1.1     | 2026-08-05 | —      | AR pass 4 (L): two docs corrected to the post-ERR-041-003 design.    |
// |         |            |        | InjuryRiskMax still described itself as one of two catalogue values |
// |         |            |        | checked for equality; it is now the sole owner of the scale.        |
// |         |            |        | RobustnessMitigationPerPoint claimed #41's term was independently   |
// |         |            |        | tuned — the two compound over the same attributes.                  |
#endregion
