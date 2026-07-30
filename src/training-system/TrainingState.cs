// File:     src/training-system/TrainingState.cs
// Created:  2026-07-30
// Modified: 2026-07-30
// Spec:     Training System #29 §2.2; Code Standards #20
// Purpose:  Serialized per-player training state owned exclusively by #29.

namespace TacticalDirector.TrainingSystem
{
    /// <summary>Per-player world-tick training state. Create instances through <see cref="Create"/>.</summary>
    public struct TrainingState
    {
        /// <summary>Persistent training focus.</summary>
        public TrainingFocus Focus;

        /// <summary>Training-driven conditioning cursor.</summary>
        public int Condition;

        /// <summary>World-tick training-fatigue accumulator; never match-tick fatigue.</summary>
        public int TrainingFatigue;

        /// <summary>The last world day advanced, or the never-advanced sentinel.</summary>
        public uint LastAdvancedWorldDay;

        /// <summary>Creates valid initial state without colliding with legitimate world day zero.</summary>
        public static TrainingState Create(TrainingFocus focus)
        {
            TrainingStep.ValidateFocus(focus);
            return new TrainingState
            {
                Focus = focus,
                Condition = TrainingSystemConstants.CONDITION_START,
                TrainingFatigue = 0,
                LastAdvancedWorldDay = TrainingSystemConstants.TRAINING_NOT_ADVANCED_SENTINEL
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-30 | —      | Initial Stage-2 core.   |
#endregion