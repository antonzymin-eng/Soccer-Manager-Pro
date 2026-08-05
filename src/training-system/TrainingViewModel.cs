// File:     src/training-system/TrainingViewModel.cs
// Created:  2026-08-05
// Modified: 2026-08-05
// Author:   —
// Spec:     Training System #29 §2.2 KD-7 / FR-TR-022; Code Standards #20
// Purpose:  The read-only observer surface #31/#38 pull. Value copies only — constructing one cannot
//           mutate the TrainingState it was read from.

namespace TacticalDirector.TrainingSystem
{
    /// <summary>
    /// A read-only value-copy view of one player's training state (KD-7 / FR-TR-022), for #31 and #38.
    /// It carries no reference to the underlying <see cref="TrainingState"/>, so an observer cannot
    /// reach a mutation path — <see cref="TrainingStep.AdvanceTrainingDay"/> and
    /// <see cref="TrainingSchedule.TrySetFocus"/> remain the only writers (FR-TR-004).
    /// </summary>
    public readonly struct TrainingViewModel
    {
        /// <summary>The player's persistent focus.</summary>
        public readonly TrainingFocus Focus;

        /// <summary>The conditioning cursor, on the <c>[ConditionMin, ConditionMax]</c> scale.</summary>
        public readonly int Condition;

        /// <summary>The world-tick training-fatigue accumulator, on the <c>[0, TrainingFatigueMax]</c> scale.</summary>
        public readonly int TrainingFatigue;

        /// <summary>Copies the observable fields out of a training state.</summary>
        /// <param name="focus">The player's persistent focus.</param>
        /// <param name="condition">The conditioning cursor.</param>
        /// <param name="trainingFatigue">The training-fatigue accumulator.</param>
        public TrainingViewModel(TrainingFocus focus, int condition, int trainingFatigue)
        {
            Focus = focus;
            Condition = condition;
            TrainingFatigue = trainingFatigue;
        }

        /// <summary>Reads a value-copy view of <paramref name="state"/>. Pure — it never mutates the state.</summary>
        /// <param name="state">The state to observe.</param>
        public static TrainingViewModel Create(in TrainingState state) =>
            new TrainingViewModel(state.Focus, state.Condition, state.TrainingFatigue);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-05 | —      | Initial implementation (#29 T0, KD-7).                             |
// | 1.1     | 2026-08-05 | —      | AR pass 1 (H): the writer citation re-pointed from                 |
// |         |            |        | TrainingStep.SetFocus to TrainingSchedule.TrySetFocus.             |
// | 1.2     | 2026-08-05 | —      | AR pass 4 (L): v1.1 landed with no version row; recorded here.     |
#endregion
