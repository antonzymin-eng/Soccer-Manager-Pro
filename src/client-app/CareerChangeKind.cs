// File:     src/client-app/CareerChangeKind.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (Career Home "what changed?"),
//           ux-detailed-plan.md §7.5, Code Standards #20
// Purpose:  What the most recent career operation was — the first question Career Home answers.

namespace TacticalDirector.ClientApp
{
    /// <summary>The kind of the most recent career operation (§5-P7, ux-detailed-plan §7.5 question 1).</summary>
    public enum CareerChangeKind
    {
        /// <summary>Zero-value sentinel.</summary>
        None = 0,

        /// <summary>A new career was created.</summary>
        Started = 1,

        /// <summary>A career was resumed from a save slot.</summary>
        Resumed = 2,

        /// <summary>The world advanced one day.</summary>
        DayAdvanced = 3,

        /// <summary>A round was resolved.</summary>
        RoundPlayed = 4,

        /// <summary>The season boundary was rolled.</summary>
        SeasonRolled = 5,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7).                                      |
#endregion
