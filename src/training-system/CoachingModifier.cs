// File:     src/training-system/CoachingModifier.cs
// Created:  2026-07-30
// Modified: 2026-07-31
// Author:   —
// Spec:     Training System #29 §2.2 / §3.1; Code Standards #20
// Purpose:  Identity routing seam for the future Staff & Backroom producer.

namespace TacticalDirector.TrainingSystem
{
    /// <summary>Identity-only coaching routing seam until Staff &amp; Backroom #34 becomes a producer.</summary>
    public readonly struct CoachingModifier
    {
        /// <summary>The current neutral modifier; its exact multiplier is one.</summary>
        public static CoachingModifier Identity => default;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-30 | —      | Initial Stage-2 core.   |
// | 1.1     | 2026-07-31 | —      | AR-1 L-4: `Author:` header line added (FR-CS-056). |
#endregion