// File:     src/client-app/tests/ClientScreenVisibilityTests.cs
// Created:  2026-09-04
// Modified: 2026-11-03
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b,
//           UI / Client Framework #38 §3.2, Code Standards #20 §12 rule 1
// Purpose:  Locks seven-screen visibility and the career graph's presentation transitions.

using System;

using NUnit.Framework;

using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class ClientScreenVisibilityTests
    {
        [Test]
        public void EachCatalogueScreenMapsToExactlyItsOwnRoot()
        {
            AssertVisibility(ClientScreens.MainMenu, true, false, false, false);
            AssertVisibility(ClientScreens.TacticsSetup, false, true, false, false);
            AssertVisibility(ClientScreens.MatchView, false, false, true, false);
            AssertVisibility(ClientScreens.PostMatchReport, false, false, false, true);
            AssertVisibility(ClientScreens.CareerHome, false, false, false, false, careerHome: true);
            AssertVisibility(ClientScreens.NewCareer, false, false, false, false, newCareer: true);
            AssertVisibility(ClientScreens.LoadCareer, false, false, false, false, loadCareer: true);
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(999)]
        public void UnknownScreen_IsRefused(int id)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ClientScreenVisibility.From(new ScreenId(id)));
        }

        [Test]
        public void CareerNavigation_ReplacesFormsAndReturnsToOnlyMainMenu()
        {
            var flow = new ClientScreenFlow(
                Reg(ClientScreens.MainMenu), Reg(ClientScreens.TacticsSetup),
                Reg(ClientScreens.MatchView), Reg(ClientScreens.PostMatchReport),
                Reg(ClientScreens.CareerHome), Reg(ClientScreens.NewCareer), Reg(ClientScreens.LoadCareer));
            flow.OpenNewCareer();
            AssertVisibility(flow.Current, false, false, false, false, newCareer: true);
            flow.StartCareer();
            AssertVisibility(flow.Current, false, false, false, false, careerHome: true);
            flow.QuitToMainMenu();
            AssertVisibility(flow.Current, true, false, false, false);
            flow.OpenLoadCareer();
            AssertVisibility(flow.Current, false, false, false, false, loadCareer: true);
            flow.EnterLoadedCareer();
            AssertVisibility(flow.Current, false, false, false, false, careerHome: true);
            flow.QuitToMainMenu();
            AssertVisibility(flow.Current, true, false, false, false);
            flow.ContinueCareer();
            AssertVisibility(flow.Current, false, false, false, false, careerHome: true);
            flow.QuitToMainMenu();
            flow.OpenNewCareer();
            flow.CancelNewCareer();
            AssertVisibility(flow.Current, true, false, false, false);
            flow.OpenLoadCareer();
            flow.CancelLoadCareer();
            AssertVisibility(flow.Current, true, false, false, false);
        }

        private static ScreenRegistration Reg(ScreenId id) => new ScreenRegistration(id, null, null);

        private static void AssertVisibility(
            ScreenId screen,
            bool mainMenu,
            bool tacticsSetup,
            bool matchView,
            bool postMatchReport,
            bool careerHome = false,
            bool newCareer = false,
            bool loadCareer = false)
        {
            ClientScreenVisibility visibility = ClientScreenVisibility.From(screen);

            Assert.AreEqual(mainMenu, visibility.MainMenu);
            Assert.AreEqual(tacticsSetup, visibility.TacticsSetup);
            Assert.AreEqual(matchView, visibility.MatchView);
            Assert.AreEqual(postMatchReport, visibility.PostMatchReport);
            Assert.AreEqual(careerHome, visibility.CareerHome);
            Assert.AreEqual(newCareer, visibility.NewCareer);
            Assert.AreEqual(loadCareer, visibility.LoadCareer);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-09-04 | —      | Initial exhaustive screen-visibility mapping locks.            |
// | 1.1     | 2026-09-06 | —      | Remove redundant same-namespace using after PR review.         |
// | 1.2     | 2026-11-03 | —      | P7b: career visibility through actual navigation; invalid ids. |
#endregion
