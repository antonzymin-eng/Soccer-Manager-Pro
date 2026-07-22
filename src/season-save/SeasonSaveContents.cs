// File:     src/season-save/SeasonSaveContents.cs
// Created:  2026-07-22
// Modified: 2026-07-22
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §4 / G4 / KD-3;
//           Code Standards #20
// Purpose:  The reconstructed contents of a season save: the living-world WorldStore (always) and the
//           in-progress MatchEngine (null when the season carried no match). SeasonSaveManager.Load
//           returns this.

using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The two halves a season save reconstructs (unified-season-save-design.md §4): the living-world
    /// <see cref="World"/> (never null — a season always has a world) and the in-progress
    /// <see cref="Match"/> (<c>null</c> when the save carried no match, KD-3). Returned by
    /// <see cref="SeasonSaveManager.Load"/>; the caller checks <see cref="Match"/> for null before using it.
    /// </summary>
    public readonly struct SeasonSaveContents
    {
        /// <summary>The reconstructed living-world store. Never null.</summary>
        public readonly WorldStore World;

        /// <summary>The reconstructed in-progress match, or <c>null</c> if the season had no match.</summary>
        public readonly MatchEngine.MatchEngine Match;

        /// <summary>Constructs the reconstructed season contents.</summary>
        public SeasonSaveContents(WorldStore world, MatchEngine.MatchEngine match)
        {
            World = world;
            Match = match;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-22 | —      | Initial implementation. |
#endregion
