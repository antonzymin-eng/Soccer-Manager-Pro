// File:     src/training-system/TrainingState.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Training System #29 §2.2 (FR-TR-002/003/010/011/015); Code Standards #20
// Purpose:  The #29-owned per-player world-tick training state — the single source of truth for a
//           player's focus, conditioning cursor and training-fatigue accumulator. Serialized at T1.

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// Per-player world-tick training state (§2.2). Mutated only by
    /// <see cref="TrainingStep.AdvanceTrainingDay"/> (the day step) and
    /// <see cref="TrainingSchedule.TrySetFocus"/> (the focus command, FR-TR-023).
    /// <para>
    /// <b><c>default(TrainingState)</c> is NOT a valid runtime state.</b> Its
    /// <see cref="LastAdvancedWorldDay"/> would be 0 — a legitimate world day — so the first advance
    /// of world day 0 would read as "already advanced" and the player would silently skip a day's
    /// accrual forever after. Always construct through <see cref="Create"/>, which seeds
    /// <see cref="TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL"/>. This is the day-0 trap
    /// the #28 lifecycle precedent guards against (F6/F7), and it is why FR-TR-025 requires a regen's
    /// state to be inserted via <c>Create</c> rather than defaulted.
    /// </para>
    /// </summary>
    public struct TrainingState
    {
        /// <summary>
        /// The persistent training focus — the SINGLE source of truth (FR-TR-003). Changed only by
        /// <see cref="TrainingSchedule.TrySetFocus"/>. <see cref="TrainingSchedule"/> is the club-scoped
        /// handle over this field; it stores no copy of its own.
        /// </summary>
        public TrainingFocus Focus;

        /// <summary>
        /// The one conditioning cursor, in
        /// <c>[TrainingSystemConstants.ConditionMin, TrainingSystemConstants.ConditionMax]</c>
        /// (FR-TR-010). Integer; clamped at both bounds (F1).
        /// </summary>
        public int Condition;

        /// <summary>
        /// The world-tick training-fatigue accumulator, in
        /// <c>[0, TrainingSystemConstants.TrainingFatigueMax]</c> (FR-TR-011). <b>Distinct from
        /// match-tick fatigue</b> (<c>1 − AerobicPool</c>, owned by the match engine): the two never
        /// share a counter, and match-tick fatigue never writes back here (FR-TR-012/013).
        /// </summary>
        public int TrainingFatigue;

        /// <summary>
        /// The idempotency cursor (F6): the last world day
        /// <see cref="TrainingStep.AdvanceTrainingDay"/> advanced, or
        /// <see cref="TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL"/> when the state has
        /// never been advanced.
        /// </summary>
        public uint LastAdvancedWorldDay;

        /// <summary>
        /// The only valid way to construct a runtime state (§2.2): the focus as given, conditioning at
        /// <see cref="TrainingSystemConstants.ConditionStart"/>, zero training-fatigue, and the
        /// never-advanced sentinel. FR-TR-025 requires this — never <c>default</c> — when #30 inserts
        /// a regen's state at the season boundary.
        /// </summary>
        /// <param name="focus">The focus the player starts on (Balanced for a regen).</param>
        public static TrainingState Create(TrainingFocus focus)
        {
            return new TrainingState
            {
                Focus = focus,
                Condition = TrainingSystemConstants.ConditionStart,
                TrainingFatigue = 0,
                LastAdvancedWorldDay = TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL,
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                  |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#29 T0, §2.2). |
#endregion
