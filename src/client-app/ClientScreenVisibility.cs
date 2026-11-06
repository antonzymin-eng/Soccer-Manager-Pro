// File:     src/client-app/ClientScreenVisibility.cs
// Created:  2026-09-04
// Modified: 2026-11-05
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b,
//           UI / Client Framework #38 §3.2, Code Standards #20 §12 rule 1
// Purpose:  Host-free exhaustive mapping from the nine-screen catalogue to root visibility. The Unity
//           binding applies these booleans and contains no screen-identity decision branch.

using System;

using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// The nine mutually-exclusive root visibility flags for one <see cref="ClientScreens"/> state.
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

        /// <summary>Whether the Career Home root is visible.</summary>
        public readonly bool CareerHome;

        /// <summary>Whether the New Career root is visible.</summary>
        public readonly bool NewCareer;

        /// <summary>Whether the Load Career root is visible.</summary>
        public readonly bool LoadCareer;

        /// <summary>Whether the Transfers root is visible (§5-P8).</summary>
        public readonly bool Transfers;

        /// <summary>Whether the Club Finances root is visible (§5-P8).</summary>
        public readonly bool ClubFinances;

        private ClientScreenVisibility(bool mainMenu, bool tacticsSetup, bool matchView, bool postMatchReport,
            bool careerHome = false, bool newCareer = false, bool loadCareer = false,
            bool transfers = false, bool clubFinances = false)
        {
            MainMenu = mainMenu;
            TacticsSetup = tacticsSetup;
            MatchView = matchView;
            PostMatchReport = postMatchReport;
            CareerHome = careerHome;
            NewCareer = newCareer;
            LoadCareer = loadCareer;
            Transfers = transfers;
            ClubFinances = clubFinances;
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

            if (screen == ClientScreens.CareerHome)
            {
                return new ClientScreenVisibility(false, false, false, false, careerHome: true);
            }

            if (screen == ClientScreens.NewCareer)
            {
                return new ClientScreenVisibility(false, false, false, false, newCareer: true);
            }

            if (screen == ClientScreens.LoadCareer)
            {
                return new ClientScreenVisibility(false, false, false, false, loadCareer: true);
            }

            if (screen == ClientScreens.Transfers)
            {
                return new ClientScreenVisibility(false, false, false, false, transfers: true);
            }

            if (screen == ClientScreens.ClubFinances)
            {
                return new ClientScreenVisibility(false, false, false, false, clubFinances: true);
            }

            throw new ArgumentOutOfRangeException(
                nameof(screen), screen.Value,
                "ScreenId is not one of the nine ClientScreens catalogue entries.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-09-04 | —      | Extracted exhaustive screen→visibility decision from P5b bind. |
// | 1.1     | 2026-11-03 | —      | P7b foundation: three career roots; PM-1 mapping unchanged.    |
// | 1.2     | 2026-11-05 | —      | §5-P8: Transfers and Club Finances flags; earlier mappings unchanged. |
#endregion
