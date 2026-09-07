// File:     src/client-app/tests/ClientScreensTests.cs
// Created:  2026-08-07
// Modified: 2026-09-06
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b, Code Standards #20
// Purpose:  Locks the screen catalogue's identity properties and the shell bootstrap execution-order
//           constant's only mechanically testable requirement: it is earlier than Unity default zero.

using NUnit.Framework;

using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class ClientScreensTests
    {
        private static ScreenId[] AllScreens() => new[]
        {
            ClientScreens.MainMenu,
            ClientScreens.TacticsSetup,
            ClientScreens.MatchView,
            ClientScreens.PostMatchReport,
        };

        [Test]
        public void TheFourScreenIdsAreDistinct()
        {
            ScreenId[] screens = AllScreens();
            for (int i = 0; i < screens.Length; i++)
            {
                for (int j = i + 1; j < screens.Length; j++)
                {
                    Assert.AreNotEqual(screens[i], screens[j],
                        "Two catalogue screens share an id — every registration and navigation " +
                        "keyed on the second would silently hit the first.");
                }
            }
        }

        // Zero-value safety: NavigationShell's un-rooted Current guard exists because
        // default(ScreenId) is id 0 — so no real screen may occupy 0, or an uninitialized
        // ScreenId field would silently alias it.
        [Test]
        public void NoScreenAliasesTheZeroValue()
        {
            foreach (ScreenId screen in AllScreens())
            {
                Assert.AreNotEqual(default(ScreenId), screen,
                    "A catalogue screen occupies id 0, which default(ScreenId) aliases.");
            }
        }

        [Test]
        public void EachScreenReadsItsCatalogueConstant()
        {
            Assert.AreEqual(ClientAppConstants.SCREEN_ID_MAIN_MENU, ClientScreens.MainMenu.Value);
            Assert.AreEqual(ClientAppConstants.SCREEN_ID_TACTICS_SETUP, ClientScreens.TacticsSetup.Value);
            Assert.AreEqual(ClientAppConstants.SCREEN_ID_MATCH_VIEW, ClientScreens.MatchView.Value);
            Assert.AreEqual(ClientAppConstants.SCREEN_ID_POST_MATCH_REPORT, ClientScreens.PostMatchReport.Value);
        }

        [Test]
        public void ShellBootstrapExecutionOrderIsBeforeDefaultZero()
        {
            Assert.Less(ClientAppConstants.SHELL_BOOTSTRAP_EXECUTION_ORDER, 0,
                "The P5b shell bootstrap execution-order constant must remain earlier than Unity's " +
                "default script execution order of zero.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-07 | —      | Initial creation: distinctness, zero-value safety, constants.  |
// | 1.1     | 2026-09-04 | —      | P5b H2: add shell bootstrap execution-order constant lock.     |
// | 1.2     | 2026-09-06 | —      | Rename the ordering test to match exactly what its body locks; |
// |         |            |        | remove redundant same-namespace using.                         |
#endregion
