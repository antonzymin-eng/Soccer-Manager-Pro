// File:     src/client-app/tests/ClientScreenFlowTests.cs
// Created:  2026-08-07
// Modified: 2026-08-07
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b (navigation graph),
//           UI / Client Framework #38 §3.2 (FR-UI-011), Code Standards #20
// Purpose:  Locks the five-edge navigation graph: every legal move, every illegal invocation
//           fail-loud, the two Replace decisions (observable as where a later Pop lands), flow
//           reusability for a second match, and the transposed-registration constructor guard.

using System;

using NUnit.Framework;

using TacticalDirector.ClientApp;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class ClientScreenFlowTests
    {
        private sealed class StubSource : IViewModelSource
        {
        }

        private sealed class StubDispatcher : ICommandDispatcher
        {
            public void Dispatch(in ManagerIntent intent) =>
                throw new InvalidOperationException("Stub — these tests never dispatch.");
        }

        private static ScreenRegistration Reg(ScreenId id, IViewModelSource source = null, ICommandDispatcher dispatcher = null) =>
            new ScreenRegistration(id, source, dispatcher);

        private static ClientScreenFlow NewFlow() => new ClientScreenFlow(
            Reg(ClientScreens.MainMenu),
            Reg(ClientScreens.TacticsSetup),
            Reg(ClientScreens.MatchView),
            Reg(ClientScreens.PostMatchReport));

        [Test]
        public void BootShowsMainMenu()
        {
            Assert.AreEqual(ClientScreens.MainMenu, NewFlow().Current);
        }

        // The full match flow. The final assertion is what locks BOTH Replace decisions: if
        // StartMatch pushed instead of replacing, ReturnToMainMenu would land on TacticsSetup;
        // if ShowPostMatchReport pushed, it would land on MatchView.
        [Test]
        public void FullMatchFlow_EndsBackAtMainMenu()
        {
            ClientScreenFlow flow = NewFlow();

            flow.OpenTacticsSetup();
            Assert.AreEqual(ClientScreens.TacticsSetup, flow.Current);

            flow.StartMatch();
            Assert.AreEqual(ClientScreens.MatchView, flow.Current);

            flow.ShowPostMatchReport();
            Assert.AreEqual(ClientScreens.PostMatchReport, flow.Current);

            flow.ReturnToMainMenu();
            Assert.AreEqual(ClientScreens.MainMenu, flow.Current,
                "A Pop from the report must land on Main Menu — anything else means a Replace " +
                "edge was built as a Push and a stale screen survived beneath the stack.");
        }

        [Test]
        public void CancelTacticsSetup_ReturnsToMainMenu()
        {
            ClientScreenFlow flow = NewFlow();
            flow.OpenTacticsSetup();

            flow.CancelTacticsSetup();

            Assert.AreEqual(ClientScreens.MainMenu, flow.Current);
        }

        [Test]
        public void TheFlowIsReusableForASecondMatch()
        {
            ClientScreenFlow flow = NewFlow();
            flow.OpenTacticsSetup();
            flow.StartMatch();
            flow.ShowPostMatchReport();
            flow.ReturnToMainMenu();

            flow.OpenTacticsSetup();
            flow.StartMatch();

            Assert.AreEqual(ClientScreens.MatchView, flow.Current,
                "The first match's flow must leave the stack exactly as booted, or the second " +
                "match starts over a polluted stack.");
        }

        // Every move, invoked from a screen it does not depart from, must throw rather than
        // navigate — the guard IS the graph. One representative wrong state per move, plus the
        // two-Pop distinction (CancelTacticsSetup and ReturnToMainMenu are different edges, not
        // one shared "back").
        [Test]
        public void OpenTacticsSetup_OffMainMenu_Throws()
        {
            ClientScreenFlow flow = NewFlow();
            flow.OpenTacticsSetup();

            Assert.Throws<InvalidOperationException>(() => flow.OpenTacticsSetup());
        }

        [Test]
        public void StartMatch_OffTacticsSetup_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => NewFlow().StartMatch());
        }

        [Test]
        public void ShowPostMatchReport_OffMatchView_Throws()
        {
            ClientScreenFlow flow = NewFlow();
            flow.OpenTacticsSetup();

            Assert.Throws<InvalidOperationException>(() => flow.ShowPostMatchReport());
        }

        [Test]
        public void ReturnToMainMenu_OffPostMatchReport_Throws()
        {
            ClientScreenFlow flow = NewFlow();
            flow.OpenTacticsSetup();

            // TacticsSetup pops via CancelTacticsSetup, not ReturnToMainMenu — two edges, not one.
            Assert.Throws<InvalidOperationException>(() => flow.ReturnToMainMenu());
        }

        [Test]
        public void CancelTacticsSetup_OffTacticsSetup_Throws()
        {
            ClientScreenFlow flow = NewFlow();
            flow.OpenTacticsSetup();
            flow.StartMatch();
            flow.ShowPostMatchReport();

            // PostMatchReport pops via ReturnToMainMenu, not CancelTacticsSetup.
            Assert.Throws<InvalidOperationException>(() => flow.CancelTacticsSetup());
        }

        [Test]
        public void TransposedRegistrations_AreRefusedAtConstruction()
        {
            Assert.Throws<ArgumentException>(() => new ClientScreenFlow(
                Reg(ClientScreens.TacticsSetup),
                Reg(ClientScreens.MainMenu),
                Reg(ClientScreens.MatchView),
                Reg(ClientScreens.PostMatchReport)),
                "A registration under the wrong parameter must fail at construction, not present " +
                "the wrong screen's source/dispatcher at runtime.");
        }

        [Test]
        public void RegistrationsSurviveIntoTheFlowByIdentity()
        {
            var matchViewSource = new StubSource();
            var matchViewDispatcher = new StubDispatcher();
            var flow = new ClientScreenFlow(
                Reg(ClientScreens.MainMenu),
                Reg(ClientScreens.TacticsSetup),
                Reg(ClientScreens.MatchView, matchViewSource, matchViewDispatcher),
                Reg(ClientScreens.PostMatchReport));

            ScreenRegistration registration = flow.GetRegistration(ClientScreens.MatchView);

            Assert.AreSame(matchViewSource, registration.ViewModelSource);
            Assert.AreSame(matchViewDispatcher, registration.Dispatcher);
        }

        [Test]
        public void CurrentRegistrationTracksTheCurrentScreen()
        {
            var menuSource = new StubSource();
            var flow = new ClientScreenFlow(
                Reg(ClientScreens.MainMenu, menuSource),
                Reg(ClientScreens.TacticsSetup),
                Reg(ClientScreens.MatchView),
                Reg(ClientScreens.PostMatchReport));

            Assert.AreSame(menuSource, flow.CurrentRegistration.ViewModelSource);

            flow.OpenTacticsSetup();
            Assert.AreEqual(ClientScreens.TacticsSetup, flow.CurrentRegistration.Id);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-07 | —      | Initial creation: legal edges, illegal invocations, the        |
// |         |            |        | Replace-vs-Push locks, reusability, transposition guard,       |
// |         |            |        | registration identity.                                         |
#endregion
