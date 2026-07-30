// File:     src/training-system/TrainingSystemConstants.cs
// Created:  2026-07-30
// Modified: 2026-07-30
// Spec:     Training System #29 Appendix A; Code Standards #20
// Purpose:  Constant catalogue for the deterministic daily training step.

namespace TacticalDirector.TrainingSystem
{
    /// <summary>Constant catalogue for Training System #29's Stage-2 daily core.</summary>
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

        /// <summary>[GT] Lower conditioning cursor bound.</summary>
        public const int CONDITION_MIN = 0;

        /// <summary>[GT] Upper conditioning cursor bound.</summary>
        public const int CONDITION_MAX = 10000;

        /// <summary>[GT] Initial conditioning cursor for a newly created training state.</summary>
        public const int CONDITION_START = 7000;

        /// <summary>[GT] World-tick training-fatigue ceiling.</summary>
        public const int TRAINING_FATIGUE_MAX = 10000;

        /// <summary>[GT] Passive world-day fatigue recovery, applied regardless of focus.</summary>
        public const int FATIGUE_DAILY_RECOVERY = 200;

        /// <summary>[GT] Multiplier from training-fatigue fraction to match-entry fatigue.</summary>
        public const float MATCH_ENTRY_FATIGUE_SCALE = 1.0f;

        /// <summary>[GT] Injury-risk output upper bound.</summary>
        public const int INJURY_RISK_MAX = 10000;

        /// <summary>[GT] Training-fatigue contribution to the injury-risk input.</summary>
        public const int FATIGUE_RISK_WEIGHT = 1;

        /// <summary>[GT] Low-conditioning contribution to the injury-risk input.</summary>
        public const int LOW_CONDITION_RISK_WEIGHT = 1;

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-30 | —      | Initial Stage-2 core.   |
#endregion