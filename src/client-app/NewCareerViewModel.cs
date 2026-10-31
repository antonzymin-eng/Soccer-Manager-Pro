// File:     src/client-app/NewCareerViewModel.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-1 new-game options),
//           league-bootstrap-design.md KD-2/KD-3 (club identity), UI / Client Framework #38 §3.1,
//           Code Standards #20
// Purpose:  The New Career screen's projection: the generated league's clubs and the player's choice.
//           D-P7-1 admits exactly one option — the club — so this carries nothing else to configure.

using System.Collections.ObjectModel;

using TacticalDirector.SeasonSave;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// New Career's view model (§5-P7 D-P7-1, owner decision October 18, 2026: the world seed is
    /// generated, the player picks a club, the board objective is the bootstrap default). A
    /// <c>default</c> value has no clubs and no selection.
    /// </summary>
    public readonly struct NewCareerViewModel
    {
        /// <summary>The prepared world's seed (shown for support/reproduction, not editable in EA).</summary>
        public readonly ulong WorldSeed;

        /// <summary>The generated league's clubs, in club-id order.</summary>
        public readonly ReadOnlyCollection<Club> Clubs;

        /// <summary>True when a club is selected — Start is enabled only then.</summary>
        public readonly bool HasSelection;

        /// <summary>The selected club's id (valid when <see cref="HasSelection"/>).</summary>
        public readonly int SelectedClubId;

        /// <summary>The board objective a new career starts with (finish at or above this position).</summary>
        public readonly int DefaultObjectivePosition;

        /// <summary>Constructs the view model.</summary>
        public NewCareerViewModel(
            ulong worldSeed,
            ReadOnlyCollection<Club> clubs,
            bool hasSelection,
            int selectedClubId,
            int defaultObjectivePosition)
        {
            WorldSeed = worldSeed;
            Clubs = clubs;
            HasSelection = hasSelection;
            SelectedClubId = selectedClubId;
            DefaultObjectivePosition = defaultObjectivePosition;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7).                                      |
#endregion
