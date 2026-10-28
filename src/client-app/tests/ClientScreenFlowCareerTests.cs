// File:     src/client-app/tests/ClientScreenFlowCareerTests.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (career moves), UI / Client
//           Framework #38 §3.2 (FR-UI-011), Code Standards #20
// Purpose:  Locks the career moves: every legal edge, fail-loud from every wrong screen, the two
//           Replace decisions (observable as where Quit lands), the four-screen flow refusing career
//           moves, and the career constructor's transposition guard.

using System;

using NUnit.Framework;

using TacticalDirector.ClientApp;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class ClientScreenFlowCareerTests
    {
        private static ScreenRegistration Reg(ScreenId id) => new ScreenRegistration(id, null, null);

        private static ClientScreenFlow NewCareerFlow() => new ClientScreenFlow(
            Reg(ClientScreens.MainMenu),
            Reg(ClientScreens.TacticsSetup),
            Reg(ClientScreens.MatchView),
            Reg(ClientScreens.PostMatchReport),
            Reg(ClientScreens.CareerHome),
            Reg(ClientScreens.NewCareer),
            Reg(ClientScreens.LoadCareer));

        [Test]
        public void NewCareer_StartReplacesTheForm_SoQuitLandsOnMainMenu()
        {
            ClientScreenFlow flow = NewCareerFlow();
            Assert.IsTrue(flow.HasCareerScreens);

            flow.OpenNewCareer();
            Assert.AreEqual(ClientScreens.NewCareer, flow.Current);
            flow.StartCareer();
            Assert.AreEqual(ClientScreens.CareerHome, flow.Current);

            // A Push in StartCareer's place would leave New Career beneath, and Quit would land there.
            flow.QuitToMainMenu();
            Assert.AreEqual(ClientScreens.MainMenu, flow.Current);
        }

        [Test]
        public void LoadCareer_EnterReplacesThePicker_SoQuitLandsOnMainMenu()
        {
            ClientScreenFlow flow = NewCareerFlow();

            flow.OpenLoadCareer();
            Assert.AreEqual(ClientScreens.LoadCareer, flow.Current);
            flow.EnterLoadedCareer();
            Assert.AreEqual(ClientScreens.CareerHome, flow.Current);
            flow.QuitToMainMenu();
            Assert.AreEqual(ClientScreens.MainMenu, flow.Current);
        }

        [Test]
        public void ContinueAndCancelMoves_ReturnToMainMenu()
        {
            ClientScreenFlow flow = NewCareerFlow();

            flow.ContinueCareer();
            Assert.AreEqual(ClientScreens.CareerHome, flow.Current);
            flow.QuitToMainMenu();

            flow.OpenNewCareer();
            flow.CancelNewCareer();
            Assert.AreEqual(ClientScreens.MainMenu, flow.Current);

            flow.OpenLoadCareer();
            flow.CancelLoadCareer();
            Assert.AreEqual(ClientScreens.MainMenu, flow.Current);

            // The PM-1 path is untouched alongside the career screens.
            flow.OpenTacticsSetup();
            flow.StartMatch();
            flow.ShowPostMatchReport();
            flow.ReturnToMainMenu();
            Assert.AreEqual(ClientScreens.MainMenu, flow.Current);
        }

        [Test]
        public void CareerMoves_FromTheWrongScreen_Throw()
        {
            ClientScreenFlow flow = NewCareerFlow();

            Assert.Throws<InvalidOperationException>(flow.StartCareer);
            Assert.Throws<InvalidOperationException>(flow.CancelNewCareer);
            Assert.Throws<InvalidOperationException>(flow.EnterLoadedCareer);
            Assert.Throws<InvalidOperationException>(flow.CancelLoadCareer);
            Assert.Throws<InvalidOperationException>(flow.QuitToMainMenu);

            flow.ContinueCareer();
            Assert.Throws<InvalidOperationException>(flow.OpenNewCareer);
            Assert.Throws<InvalidOperationException>(flow.OpenLoadCareer);
            Assert.Throws<InvalidOperationException>(flow.ContinueCareer);
            // D-P7-6: no Career Home → Tactics edge exists; the PM-1 move stays guarded to Main Menu.
            Assert.Throws<InvalidOperationException>(flow.OpenTacticsSetup);
            Assert.AreEqual(ClientScreens.CareerHome, flow.Current, "a refused move must not navigate.");
        }

        [Test]
        public void FourScreenFlow_RefusesEveryCareerMove()
        {
            var flow = new ClientScreenFlow(
                Reg(ClientScreens.MainMenu),
                Reg(ClientScreens.TacticsSetup),
                Reg(ClientScreens.MatchView),
                Reg(ClientScreens.PostMatchReport));

            Assert.IsFalse(flow.HasCareerScreens);
            Assert.Throws<InvalidOperationException>(flow.OpenNewCareer);
            Assert.Throws<InvalidOperationException>(flow.OpenLoadCareer);
            Assert.Throws<InvalidOperationException>(flow.ContinueCareer);
            Assert.Throws<ArgumentException>(() => flow.GetRegistration(ClientScreens.CareerHome));
            Assert.AreEqual(ClientScreens.MainMenu, flow.Current);
        }

        [Test]
        public void CareerConstructor_RefusesTransposedRegistrations()
        {
            Assert.Throws<ArgumentException>(() => new ClientScreenFlow(
                Reg(ClientScreens.MainMenu),
                Reg(ClientScreens.TacticsSetup),
                Reg(ClientScreens.MatchView),
                Reg(ClientScreens.PostMatchReport),
                Reg(ClientScreens.NewCareer),
                Reg(ClientScreens.CareerHome),
                Reg(ClientScreens.LoadCareer)));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): 6 career-flow locks.                 |
#endregion
