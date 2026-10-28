// File:     src/client-app/ClientScreenFlow.cs
// Created:  2026-08-07
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b (navigation graph),
//           §5-P7 (career entry/exit moves),
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
    /// <b>Career moves (§5-P7).</b> Built with the seven-registration constructor, the graph also has
    /// the career entry/exit edges:
    /// <code>
    /// MainMenu ──OpenNewCareer (Push)───▶ NewCareer  ──StartCareer (Replace)───────▶ CareerHome
    ///          ◀─CancelNewCareer (Pop)───┘
    /// MainMenu ──OpenLoadCareer (Push)──▶ LoadCareer ──EnterLoadedCareer (Replace)─▶ CareerHome
    ///          ◀─CancelLoadCareer (Pop)──┘
    /// MainMenu ──ContinueCareer (Push)─────────────────────────────────────────────▶ CareerHome
    /// MainMenu ◀─QuitToMainMenu (Pop)──────────────────────────────────────────────── CareerHome
    /// </code>
    /// The career root is always exactly one screen above Main Menu: both form screens are
    /// <c>Replace</c>d by Career Home, so a quit from the career lands on Main Menu and never on a
    /// stale New/Load form. Deliberately absent (§5-P7 D-P7-6): Career Home → Tactics Setup and
    /// Post-Match → Career Home. #30 has no seam that plays the managed fixture interactively and folds
    /// its result back into the round, so those edges would have no consumer (the FR-CS-049 phantom);
    /// matchday resolves on Career Home through the season dispatcher until that seam exists. The
    /// four-registration constructor builds the PM-1 graph alone, and every career move then throws.
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

        /// <summary>
        /// Registers the four match screens and the three career screens (§5-P7), rooting the shell at
        /// <see cref="ClientScreens.MainMenu"/>. Every registration is catalogue-checked exactly as the
        /// four-screen constructor checks its own.
        /// </summary>
        /// <exception cref="ArgumentException">A registration's id does not match its parameter's
        /// catalogue screen.</exception>
        public ClientScreenFlow(
            in ScreenRegistration mainMenu,
            in ScreenRegistration tacticsSetup,
            in ScreenRegistration matchView,
            in ScreenRegistration postMatchReport,
            in ScreenRegistration careerHome,
            in ScreenRegistration newCareer,
            in ScreenRegistration loadCareer)
            : this(in mainMenu, in tacticsSetup, in matchView, in postMatchReport)
        {
            RequireCatalogueId(careerHome, ClientScreens.CareerHome, nameof(careerHome));
            RequireCatalogueId(newCareer, ClientScreens.NewCareer, nameof(newCareer));
            RequireCatalogueId(loadCareer, ClientScreens.LoadCareer, nameof(loadCareer));

            _shell.Register(in careerHome);
            _shell.Register(in newCareer);
            _shell.Register(in loadCareer);
            HasCareerScreens = true;
        }

        /// <summary>True when the flow was built with the career screens (the seven-registration constructor).</summary>
        public bool HasCareerScreens { get; }

        /// <summary>The screen the client is currently showing. Never throws: the constructor roots the shell.</summary>
        public ScreenId Current => _shell.Current;

        /// <summary>The current screen's registration — the { source, dispatcher } pair the binding reads.</summary>
        public ScreenRegistration CurrentRegistration => _shell.GetRegistration(_shell.Current);

        /// <summary>Returns the registration for <paramref name="id"/> (any registered catalogue screen).</summary>
        /// <exception cref="ArgumentException">The id is not a registered screen.</exception>
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

        /// <summary>Main Menu → New Career (Push — cancelling pops back to the menu).</summary>
        /// <exception cref="InvalidOperationException">No career screens, or the current screen is not Main Menu.</exception>
        public void OpenNewCareer()
        {
            RequireCareer(nameof(OpenNewCareer));
            RequireCurrent(ClientScreens.MainMenu, nameof(OpenNewCareer));
            _shell.Push(ClientScreens.NewCareer);
        }

        /// <summary>New Career → Main Menu (Pop — no career is created).</summary>
        /// <exception cref="InvalidOperationException">No career screens, or the current screen is not New Career.</exception>
        public void CancelNewCareer()
        {
            RequireCareer(nameof(CancelNewCareer));
            RequireCurrent(ClientScreens.NewCareer, nameof(CancelNewCareer));
            _shell.Pop();
        }

        /// <summary>New Career → Career Home (Replace — the setup form is not a "back" target once the career exists).</summary>
        /// <exception cref="InvalidOperationException">No career screens, or the current screen is not New Career.</exception>
        public void StartCareer()
        {
            RequireCareer(nameof(StartCareer));
            RequireCurrent(ClientScreens.NewCareer, nameof(StartCareer));
            _shell.Replace(ClientScreens.CareerHome);
        }

        /// <summary>Main Menu → Load Career (Push — cancelling pops back to the menu).</summary>
        /// <exception cref="InvalidOperationException">No career screens, or the current screen is not Main Menu.</exception>
        public void OpenLoadCareer()
        {
            RequireCareer(nameof(OpenLoadCareer));
            RequireCurrent(ClientScreens.MainMenu, nameof(OpenLoadCareer));
            _shell.Push(ClientScreens.LoadCareer);
        }

        /// <summary>Load Career → Main Menu (Pop — nothing is loaded).</summary>
        /// <exception cref="InvalidOperationException">No career screens, or the current screen is not Load Career.</exception>
        public void CancelLoadCareer()
        {
            RequireCareer(nameof(CancelLoadCareer));
            RequireCurrent(ClientScreens.LoadCareer, nameof(CancelLoadCareer));
            _shell.Pop();
        }

        /// <summary>Load Career → Career Home (Replace — the picker is not a "back" target once a career is loaded).</summary>
        /// <exception cref="InvalidOperationException">No career screens, or the current screen is not Load Career.</exception>
        public void EnterLoadedCareer()
        {
            RequireCareer(nameof(EnterLoadedCareer));
            RequireCurrent(ClientScreens.LoadCareer, nameof(EnterLoadedCareer));
            _shell.Replace(ClientScreens.CareerHome);
        }

        /// <summary>Main Menu → Career Home (Push — Continue resumes the latest save directly, D-P7-2).</summary>
        /// <exception cref="InvalidOperationException">No career screens, or the current screen is not Main Menu.</exception>
        public void ContinueCareer()
        {
            RequireCareer(nameof(ContinueCareer));
            RequireCurrent(ClientScreens.MainMenu, nameof(ContinueCareer));
            _shell.Push(ClientScreens.CareerHome);
        }

        /// <summary>Career Home → Main Menu (Pop — the career root sits directly above the menu by construction).</summary>
        /// <exception cref="InvalidOperationException">No career screens, or the current screen is not Career Home.</exception>
        public void QuitToMainMenu()
        {
            RequireCareer(nameof(QuitToMainMenu));
            RequireCurrent(ClientScreens.CareerHome, nameof(QuitToMainMenu));
            _shell.Pop();
        }

        private void RequireCareer(string move)
        {
            if (!HasCareerScreens)
            {
                throw new InvalidOperationException(
                    move + " is a career move, and this flow was built without the career screens " +
                    "(use the seven-registration constructor, §5-P7).");
            }
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
// | 1.1     | 2026-10-18 | —      | §5-P7: seven-registration constructor and the eight career     |
// |         |            |        | moves (New/Load/Continue entry, Quit exit); career edges into  |
// |         |            |        | Tactics/Match deliberately absent (D-P7-6). The PM-1 graph and |
// |         |            |        | its four-registration constructor are unchanged.               |
#endregion
