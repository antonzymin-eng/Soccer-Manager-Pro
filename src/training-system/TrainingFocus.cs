// File:     src/training-system/TrainingFocus.cs
// Created:  2026-07-30
// Modified: 2026-07-31
// Author:   —
// Spec:     Training System #29 §2.2; Code Standards #20
// Purpose:  The persistent per-player focus selection for the daily training step.

namespace TacticalDirector.TrainingSystem
{
    /// <summary>Per-player training emphasis. Ordinals are persisted by the future training save codec.</summary>
    public enum TrainingFocus : byte
    {
        Balanced = 0,
        Rest = 1,
        Fitness = 2,
        Technical = 3,
        Physical = 4,
        Tactical = 5
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-30 | —      | Initial Stage-2 core.   |
// | 1.1     | 2026-07-31 | —      | AR-1 L-4: `Author:` header line added (FR-CS-056). |
#endregion