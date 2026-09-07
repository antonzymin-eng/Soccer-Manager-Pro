// File:     src/client-app/ClientScreenVisibility.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b,
//           UI / Client Framework #38 §3.2, Code Standards #20 §12 rule 1
// Purpose:  Host-free exhaustive mapping from the four-screen catalogue to root visibility. The Unity
//           binding applies these booleans and contains no screen-identity decision branch.

using System;

using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// The four mutually-exclusive root visibility flags for one <see cref="ClientScreens"/> state.
    /// The exhaustive ScreenId decision lives here because <c>match-client-unity</c> is not gate-compiled.
    /// </summary>
    public readonly struct ClientScreenVisibility
    {
        /// <summary>Whether the Main Menu root is visible.</summary>
        public readonly bool MainMenu;

        /// <summary>Whether the Tactics Setup root is visible.</summary>
        public readonly bool TacticsSetup;

        /// <summary>Whether the Match View root is visible.</summary>
        public readonly bool MatchView;

        /// <summary>Whether the Post-Match Report root is visible.</summary>
        public readonly bool PostMatchReport;

        private ClientScreenVisibility(bool mainMenu, bool tacticsSetup, bool matchView, bool postMatchReport)
        {
            MainMenu = mainMenu;
            TacticsSetup = tacticsSetup;
            MatchView = matchView;
            PostMatchReport = postMatchReport;
        }

        /// <summary>Maps a catalogue screen to exactly one visible root and refuses unknown ids.</summary>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="screen"/> is not in the client catalogue.</exception>
        public static ClientScreenVisibility From(ScreenId screen)
        {
            if (screen == ClientScreens.MainMenu)
            {
                return new ClientScreenVisibility(true, false, false, false);
            }

            if (screen == ClientScreens.TacticsSetup)
            {
                return new ClientScreenVisibility(false, true, false, false);
            }

            if (screen == ClientScreens.MatchView)
            {
                return new ClientScreenVisibility(false, false, true, false);
            }

            if (screen == ClientScreens.PostMatchReport)
            {
                return new ClientScreenVisibility(false, false, false, true);
            }

            throw new ArgumentOutOfRangeException(
                nameof(screen), screen.Value,
                "ScreenId is not one of the four ClientScreens catalogue entries.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-09-04 | —      | Extracted exhaustive screen→visibility decision from P5b bind. |
#endregion
