// File:     src/client-app/tests/ClientShellWiringValidatorTests.cs
// Created:  2026-09-04
// Modified: 2026-09-07
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b,
//           Code Standards #20 §12 rule 1
// Purpose:  Locks every decision extracted from the gate-invisible P5b MonoBehaviour: missing and
//           duplicate roots, nesting, shell containment, saved-active state, and snapshot immutability.

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
            new ClientShellRootSnapshot(id, activeSelf, ancestors);

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
#endregion
