// File:     src/client-app/tests/ClientScreenVisibilityTests.cs
// Created:  2026-09-04
// Modified: 2026-09-06
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b,
//           UI / Client Framework #38 §3.2, Code Standards #20 §12 rule 1
// Purpose:  Locks the exhaustive four-screen visibility mapping extracted from the Unity-only shell.

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
        }

        [Test]
        public void UnknownScreen_IsRefused()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                ClientScreenVisibility.From(new ScreenId(999)));
        }

        private static void AssertVisibility(
            ScreenId screen,
            bool mainMenu,
            bool tacticsSetup,
            bool matchView,
            bool postMatchReport)
        {
            ClientScreenVisibility visibility = ClientScreenVisibility.From(screen);

            Assert.AreEqual(mainMenu, visibility.MainMenu);
            Assert.AreEqual(tacticsSetup, visibility.TacticsSetup);
            Assert.AreEqual(matchView, visibility.MatchView);
            Assert.AreEqual(postMatchReport, visibility.PostMatchReport);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-09-04 | —      | Initial exhaustive screen-visibility mapping locks.            |
// | 1.1     | 2026-09-06 | —      | Remove redundant same-namespace using after PR review.         |
#endregion
