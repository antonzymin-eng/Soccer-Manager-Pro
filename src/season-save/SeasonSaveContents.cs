// File:     src/season-save/SeasonSaveContents.cs
// Created:  2026-07-22
// Modified: 2026-07-25 (#30 T1: carries the reconstructed SeasonState)
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §4 / G4 / KD-3;
//           Season & Competition Loop #30 FR-SN-021; Code Standards #20
// Purpose:  The reconstructed contents of a season save: the living-world WorldStore (always), the
//           season state (always), and the in-progress MatchEngine (null when the season carried no
//           match). SeasonSaveManager.Load returns this.

using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The three parts a season save reconstructs (unified-season-save-design.md §4 + #30 FR-SN-021):
    /// the living-world <see cref="World"/> and the <see cref="Season"/> (never null — a season save
    /// always carries both), and the in-progress <see cref="Match"/> (<c>null</c> when the save carried
    /// no match, KD-3). Returned by <see cref="SeasonSaveManager.Load"/>; the caller checks
    /// <see cref="Match"/> for null before using it.
    /// </summary>
    public readonly struct SeasonSaveContents
    {
        /// <summary>The reconstructed living-world store. Never null.</summary>
        public readonly WorldStore World;

        /// <summary>The reconstructed season state (fixtures, table, calendar cursor, board). Never null.</summary>
        public readonly SeasonState Season;

        /// <summary>The reconstructed in-progress match, or <c>null</c> if the season had no match.</summary>
        public readonly MatchEngine.MatchEngine Match;

        /// <summary>Constructs the reconstructed season contents.</summary>
        public SeasonSaveContents(WorldStore world, SeasonState season, MatchEngine.MatchEngine match)
        {
            World = world;
            Season = season;
            Match = match;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-07-22 | —      | Initial implementation.                                          |
// | 1.1     | 2026-07-25 | —      | #30 T1 (FR-SN-021): gains the reconstructed Season (never null). |
#endregion
