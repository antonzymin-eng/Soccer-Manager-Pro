// File:     src/client-app/ClientScreenFlow.cs
// Created:  2026-08-07
// Modified: 2026-08-07
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b (navigation graph),
//           UI / Client Framework #38 §3.2 (FR-UI-009/010/011), Code Standards #20
// Purpose:  The client's navigation graph, encoded as guarded moves over a privately-owned
//           NavigationShell. An edge not encoded here does not exist; the P5b binding forwards button
//           clicks to these moves and decides nothing itself (§12 rule 1).

using System;

using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// The navigation graph over the <see cref="ClientScreens"/> catalogue:
    /// <code>
    /// MainMenu ──OpenTacticsSetup (Push)──▶ TacticsSetup ──StartMatch (Replace)──▶ MatchView
    ///    ▲  ▲                                   │                                     │
    ///    │  └────CancelTacticsSetup (Pop)───────┘             ShowPostMatchReport (Replace)
    ///    │                                                                            ▼
    ///    └──────────────────ReturnToMainMenu (Pop)────────────────────────── PostMatchReport
    /// </code>
    /// <para>
    /// The shell is privately owned, so these five moves are the <b>only</b> navigation surface — the
    /// graph is enforced by encapsulation, not convention. Each move guards on the screen it departs
    /// from and throws otherwise (the <c>NavigationShell</c> fail-loud posture, FR-UI-011): a binding
    /// that wires a button to the wrong move fails at the click, not by silently landing on a wrong
    /// screen.
    /// </para>
    /// <para>
    /// Two edges are <c>Replace</c>, deliberately. TacticsSetup → MatchView replaces because a running
    /// match must not sit above a stale setup screen a "back" could return to — the setup's job ended
    /// when the <c>MatchSetup</c> was handed off, and a fresh setup starts from Main Menu. MatchView →
    /// PostMatchReport replaces because the match is over and frozen (§6.2's full-time freeze): there
    /// is no live match to go back to, so a Pop from the report lands on Main Menu, never on a dead
    /// match view.
    /// </para>
    /// <para>
    /// Deliberately absent: an abandon-match edge out of MatchView. §5-P5b specifies no quit control on
    /// the Match View screen, and an edge without a specified consumer is the phantom this project
    /// refuses (FR-CS-049 posture). If the owner adds one, it is a new named move here, not a shell
    /// poke in the binding.
    /// </para>
    /// <para>
    /// NOT thread-safe: navigation is driven from the UI/render thread alone, as
    /// <see cref="NavigationShell"/> already states.
    /// </para>
    /// </summary>
    public sealed class ClientScreenFlow
    {
        private readonly NavigationShell _shell = new NavigationShell();

        /// <summary>
        /// Registers the four screens and roots the shell at <see cref="ClientScreens.MainMenu"/>.
        /// <para>
        /// Each registration's <see cref="ScreenRegistration.Id"/> must equal its parameter's catalogue
        /// id. A transposed argument pair would otherwise register each screen under the other's
        /// identity and every later navigation would present the wrong source/dispatcher with all
        /// gates green — the silent-transposition class ERR-029-005 documents — so a mismatch throws.
        /// </para>
        /// </summary>
        /// <exception cref="ArgumentException">A registration's id does not match its parameter's
        /// catalogue screen.</exception>
        public ClientScreenFlow(
            in ScreenRegistration mainMenu,
            in ScreenRegistration tacticsSetup,
            in ScreenRegistration matchView,
            in ScreenRegistration postMatchReport)
        {
            RequireCatalogueId(mainMenu, ClientScreens.MainMenu, nameof(mainMenu));
            RequireCatalogueId(tacticsSetup, ClientScreens.TacticsSetup, nameof(tacticsSetup));
            RequireCatalogueId(matchView, ClientScreens.MatchView, nameof(matchView));
            RequireCatalogueId(postMatchReport, ClientScreens.PostMatchReport, nameof(postMatchReport));

            _shell.Register(in mainMenu);
            _shell.Register(in tacticsSetup);
            _shell.Register(in matchView);
            _shell.Register(in postMatchReport);

            _shell.Push(ClientScreens.MainMenu);
        }

        /// <summary>The screen the client is currently showing. Never throws: the constructor roots the shell.</summary>
        public ScreenId Current => _shell.Current;

        /// <summary>The current screen's registration — the { source, dispatcher } pair the binding reads.</summary>
        public ScreenRegistration CurrentRegistration => _shell.GetRegistration(_shell.Current);

        /// <summary>Returns the registration for <paramref name="id"/> (any of the four catalogue screens).</summary>
        /// <exception cref="ArgumentException">The id is not one of the four registered screens.</exception>
        public ScreenRegistration GetRegistration(ScreenId id) => _shell.GetRegistration(id);

        /// <summary>Main Menu → Tactics Setup (Push — cancelling pops back to the menu).</summary>
        /// <exception cref="InvalidOperationException">The current screen is not Main Menu.</exception>
        public void OpenTacticsSetup()
        {
            RequireCurrent(ClientScreens.MainMenu, nameof(OpenTacticsSetup));
            _shell.Push(ClientScreens.TacticsSetup);
        }

        /// <summary>Tactics Setup → Main Menu (Pop — the setup is discarded without starting a match).</summary>
        /// <exception cref="InvalidOperationException">The current screen is not Tactics Setup.</exception>
        public void CancelTacticsSetup()
        {
            RequireCurrent(ClientScreens.TacticsSetup, nameof(CancelTacticsSetup));
            _shell.Pop();
        }

        /// <summary>Tactics Setup → Match View (Replace — see the class summary for why not Push).</summary>
        /// <exception cref="InvalidOperationException">The current screen is not Tactics Setup.</exception>
        public void StartMatch()
        {
            RequireCurrent(ClientScreens.TacticsSetup, nameof(StartMatch));
            _shell.Replace(ClientScreens.MatchView);
        }

        /// <summary>Match View → Post-Match Report (Replace — the finished match cannot be returned to).</summary>
        /// <exception cref="InvalidOperationException">The current screen is not Match View.</exception>
        public void ShowPostMatchReport()
        {
            RequireCurrent(ClientScreens.MatchView, nameof(ShowPostMatchReport));
            _shell.Replace(ClientScreens.PostMatchReport);
        }

        /// <summary>Post-Match Report → Main Menu (Pop — the flow is then ready for another match).</summary>
        /// <exception cref="InvalidOperationException">The current screen is not Post-Match Report.</exception>
        public void ReturnToMainMenu()
        {
            RequireCurrent(ClientScreens.PostMatchReport, nameof(ReturnToMainMenu));
            _shell.Pop();
        }

        private void RequireCurrent(ScreenId expected, string move)
        {
            if (_shell.Current != expected)
            {
                throw new InvalidOperationException(
                    move + " is only legal from " + expected + "; the current screen is " +
                    _shell.Current + ". The graph has no such edge (see ClientScreenFlow).");
            }
        }

        private static void RequireCatalogueId(in ScreenRegistration registration, ScreenId expected, string parameterName)
        {
            if (registration.Id != expected)
            {
                throw new ArgumentException(
                    "Registration id " + registration.Id + " does not match the catalogue id " +
                    expected + " for parameter '" + parameterName +
                    "' — the arguments are transposed or the registration was minted off-catalogue.",
                    parameterName);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-07 | —      | Initial creation: the five-edge navigation graph as guarded    |
// |         |            |        | moves over a privately-owned shell; catalogue-id validation    |
// |         |            |        | at construction; two Replace edges documented; no             |
// |         |            |        | abandon-match edge by design.                                  |
#endregion
