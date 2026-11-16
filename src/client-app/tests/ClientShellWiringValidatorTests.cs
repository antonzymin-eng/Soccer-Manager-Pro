// File:     src/client-app/tests/ClientShellWiringValidatorTests.cs
// Created:  2026-09-04
// Modified: 2026-11-03 (P7b: seven-root structural locks)
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b,
//           Code Standards #20 §12 rule 1
// Purpose:  Locks every decision extracted from the gate-invisible P5b MonoBehaviour: missing and
//           duplicate roots, nesting, shell containment, inactive ancestors, saved-active state, and
//           snapshot immutability.

using NUnit.Framework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class ClientShellWiringValidatorTests
    {
        private static ClientShellRootSnapshot Root(
            int id,
            bool activeSelf = false,
            params int[] ancestors) =>
            new ClientShellRootSnapshot(id, activeSelf, true, ancestors);

        private static ClientShellRootSnapshot UnderInactiveAncestor(int id) =>
            new ClientShellRootSnapshot(id, false, false, new[] { 50 });

        private static ClientShellWiringFault Validate(
            ClientShellRootSnapshot shell,
            ClientShellRootSnapshot mainMenu,
            ClientShellRootSnapshot tacticsSetup,
            ClientShellRootSnapshot matchView,
            ClientShellRootSnapshot postMatchReport) =>
            ClientShellWiringValidator.Validate(
                in shell,
                in mainMenu,
                in tacticsSetup,
                in matchView,
                in postMatchReport);

        private static ClientShellWiringFault ValidateDefault(
            ClientShellRootSnapshot mainMenu,
            ClientShellRootSnapshot tacticsSetup,
            ClientShellRootSnapshot matchView,
            ClientShellRootSnapshot postMatchReport) =>
            Validate(Root(100), mainMenu, tacticsSetup, matchView, postMatchReport);

        [Test]
        public void IndependentRoots_WithOnlyMainMenuSavedActive_AreValid()
        {
            Assert.AreEqual(
                ClientShellWiringFault.None,
                ValidateDefault(Root(1, true), Root(2), Root(3), Root(4)));
        }

        [Test]
        public void MainMenuMayAlsoBeSavedInactive_TheBindingWillApplyInitialVisibility()
        {
            Assert.AreEqual(
                ClientShellWiringFault.None,
                ValidateDefault(Root(1), Root(2), Root(3), Root(4)));
        }

        [Test]
        public void MissingRoot_IsRefused()
        {
            Assert.AreEqual(
                ClientShellWiringFault.MissingRoot,
                ValidateDefault(Root(1), Root(2), Root(3), default));
        }

        [Test]
        public void DuplicateRoot_IsRefused()
        {
            Assert.AreEqual(
                ClientShellWiringFault.DuplicateRoot,
                ValidateDefault(Root(1), Root(2), Root(2), Root(4)));
        }

        [Test]
        public void NestedRoots_AreRefusedInEitherDirection()
        {
            Assert.AreEqual(
                ClientShellWiringFault.NestedRoot,
                ValidateDefault(Root(1), Root(2, false, 1), Root(3), Root(4)));

            Assert.AreEqual(
                ClientShellWiringFault.NestedRoot,
                ValidateDefault(Root(1, false, 2), Root(2), Root(3), Root(4)));
        }

        [Test]
        public void ShellOnOrBelowScreenRoot_IsRefused()
        {
            Assert.AreEqual(
                ClientShellWiringFault.ShellInsideScreenRoot,
                Validate(Root(100, false, 3), Root(1), Root(2), Root(3), Root(4)));

            Assert.AreEqual(
                ClientShellWiringFault.ShellInsideScreenRoot,
                Validate(Root(3), Root(1), Root(2), Root(3), Root(4)));
        }

        [Test]
        public void AnyNonMainRootSavedActive_IsRefused()
        {
            Assert.AreEqual(
                ClientShellWiringFault.NonMainRootInitiallyActive,
                ValidateDefault(Root(1, true), Root(2, true), Root(3), Root(4)));
            Assert.AreEqual(
                ClientShellWiringFault.NonMainRootInitiallyActive,
                ValidateDefault(Root(1, true), Root(2), Root(3, true), Root(4)));
            Assert.AreEqual(
                ClientShellWiringFault.NonMainRootInitiallyActive,
                ValidateDefault(Root(1, true), Root(2), Root(3), Root(4, true)));
        }

        [Test]
        public void AnyRootBeneathAnInactiveAncestor_IsRefused()
        {
            Assert.AreEqual(
                ClientShellWiringFault.RootUnderInactiveAncestor,
                ValidateDefault(UnderInactiveAncestor(1), Root(2), Root(3), Root(4)));
            Assert.AreEqual(
                ClientShellWiringFault.RootUnderInactiveAncestor,
                ValidateDefault(Root(1), UnderInactiveAncestor(2), Root(3), Root(4)));
            Assert.AreEqual(
                ClientShellWiringFault.RootUnderInactiveAncestor,
                ValidateDefault(Root(1), Root(2), UnderInactiveAncestor(3), Root(4)));
            Assert.AreEqual(
                ClientShellWiringFault.RootUnderInactiveAncestor,
                ValidateDefault(Root(1), Root(2), Root(3), UnderInactiveAncestor(4)));
        }

        private static ClientShellRootSnapshot[] SevenRoots() =>
            new[] { Root(1, true), Root(2), Root(3), Root(4), Root(5), Root(6), Root(7) };

        private static ClientShellWiringFault ValidateSeven(ClientShellRootSnapshot[] roots,
            ClientShellRootSnapshot shell) => ClientShellWiringValidator.Validate(
                shell, roots[0], roots[1], roots[2], roots[3], roots[4], roots[5], roots[6]);

        [Test]
        public void SevenRoots_AdmitsIndependentScreens_AndChecksEveryMissingRoot()
        {
            Assert.AreEqual(ClientShellWiringFault.None, ValidateSeven(SevenRoots(), Root(100)));
            for (int i = 0; i < SevenRoots().Length; i++)
            {
                ClientShellRootSnapshot[] roots = SevenRoots();
                roots[i] = default;
                Assert.AreEqual(ClientShellWiringFault.MissingRoot, ValidateSeven(roots, Root(100)));
            }
        }

        [Test]
        public void SevenRoots_RefusesEveryDuplicateAndNestedPair_InBothDirections()
        {
            for (int i = 0; i < SevenRoots().Length; i++)
                for (int j = i + 1; j < SevenRoots().Length; j++)
                {
                    ClientShellRootSnapshot[] roots = SevenRoots();
                    roots[j] = Root(roots[i].InstanceId);
                    Assert.AreEqual(ClientShellWiringFault.DuplicateRoot, ValidateSeven(roots, Root(100)));
                    roots = SevenRoots();
                    roots[j] = Root(j + 1, false, i + 1);
                    Assert.AreEqual(ClientShellWiringFault.NestedRoot, ValidateSeven(roots, Root(100)));
                    roots = SevenRoots();
                    roots[i] = Root(i + 1, i == 0, j + 1);
                    Assert.AreEqual(ClientShellWiringFault.NestedRoot, ValidateSeven(roots, Root(100)));
                }
        }

        [Test]
        public void SevenRoots_RefusesShellContainmentAndInactiveAncestors_ForEveryRoot()
        {
            for (int i = 0; i < SevenRoots().Length; i++)
            {
                Assert.AreEqual(ClientShellWiringFault.ShellInsideScreenRoot,
                    ValidateSeven(SevenRoots(), Root(i + 1)));
                Assert.AreEqual(ClientShellWiringFault.ShellInsideScreenRoot,
                    ValidateSeven(SevenRoots(), Root(100, false, i + 1)));
                ClientShellRootSnapshot[] roots = SevenRoots();
                roots[i] = UnderInactiveAncestor(i + 1);
                Assert.AreEqual(ClientShellWiringFault.RootUnderInactiveAncestor, ValidateSeven(roots, Root(100)));
            }
        }

        [Test]
        public void SevenRoots_RefusesEveryActiveNonMenuRoot_AndRetainsFaultPrecedence()
        {
            for (int i = 1; i < SevenRoots().Length; i++)
            {
                ClientShellRootSnapshot[] roots = SevenRoots();
                roots[i] = Root(i + 1, true);
                Assert.AreEqual(ClientShellWiringFault.NonMainRootInitiallyActive, ValidateSeven(roots, Root(100)));
                roots[0] = default;
                Assert.AreEqual(ClientShellWiringFault.MissingRoot, ValidateSeven(roots, Root(i + 1)));
            }
        }

        [Test]
        public void SnapshotCopiesAncestorIds()
        {
            int[] ancestors = { 10 };
            ClientShellRootSnapshot snapshot = Root(20, false, ancestors);

            ancestors[0] = 99;

            Assert.IsTrue(snapshot.HasAncestor(10));
            Assert.IsFalse(snapshot.HasAncestor(99));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-09-07 | —      | Final locks for P5b shell/root structure and initial UI hygiene;|
// |         |            |        | no P4b implementation-placement contract.                      |
// | 1.1     | 2026-10-05 | —      | PR #470 review: inactive-ancestor refusal for all four roots.  |
// | 1.2     | 2026-11-03 | —      | Exhaust seven-root cross-pairs and root-specific faults.      |
#endregion
