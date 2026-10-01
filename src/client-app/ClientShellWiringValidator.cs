// File:     src/client-app/ClientShellWiringValidator.cs
// Created:  2026-09-04
// Modified: 2026-09-07
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b,
//           Code Standards #20 §12 rule 1
// Purpose:  Pure P5b shell-structure validation. Unity collects ids/ancestor chains only; duplicate,
//           nesting, shell containment, and initial-visibility decisions are made and test-locked here.

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Validates the P5b shell structure without referencing Unity. This is deliberately
    /// decision-bearing: the Unity-only binding is permanently outside the shim gate, so every branch
    /// that decides whether a shell/root arrangement is legal belongs here (§12 rule 1).
    /// </summary>
    public static class ClientShellWiringValidator
    {
        /// <summary>
        /// Validates one shell controller and the four catalogue screen roots. The three non-Main-Menu
        /// roots must be saved inactive as initial UI hygiene: the shell applies authoritative
        /// visibility during bootstrap, and a scene saved with multiple roots active is refused rather
        /// than briefly presenting stacked canvases. This validator intentionally knows nothing about
        /// any screen's implementation type or lifecycle host.
        /// </summary>
        public static ClientShellWiringFault Validate(
            in ClientShellRootSnapshot shell,
            in ClientShellRootSnapshot mainMenu,
            in ClientShellRootSnapshot tacticsSetup,
            in ClientShellRootSnapshot matchView,
            in ClientShellRootSnapshot postMatchReport)
        {
            if (mainMenu.InstanceId == 0 || tacticsSetup.InstanceId == 0 ||
                matchView.InstanceId == 0 || postMatchReport.InstanceId == 0)
            {
                return ClientShellWiringFault.MissingRoot;
            }

            if (mainMenu.InstanceId == tacticsSetup.InstanceId ||
                mainMenu.InstanceId == matchView.InstanceId ||
                mainMenu.InstanceId == postMatchReport.InstanceId ||
                tacticsSetup.InstanceId == matchView.InstanceId ||
                tacticsSetup.InstanceId == postMatchReport.InstanceId ||
                matchView.InstanceId == postMatchReport.InstanceId)
            {
                return ClientShellWiringFault.DuplicateRoot;
            }

            if (IsNested(in mainMenu, in tacticsSetup) ||
                IsNested(in mainMenu, in matchView) ||
                IsNested(in mainMenu, in postMatchReport) ||
                IsNested(in tacticsSetup, in matchView) ||
                IsNested(in tacticsSetup, in postMatchReport) ||
                IsNested(in matchView, in postMatchReport))
            {
                return ClientShellWiringFault.NestedRoot;
            }

            if (IsShellInside(in shell, in mainMenu) ||
                IsShellInside(in shell, in tacticsSetup) ||
                IsShellInside(in shell, in matchView) ||
                IsShellInside(in shell, in postMatchReport))
            {
                return ClientShellWiringFault.ShellInsideScreenRoot;
            }

            if (tacticsSetup.IsActiveSelf || matchView.IsActiveSelf || postMatchReport.IsActiveSelf)
            {
                return ClientShellWiringFault.NonMainRootInitiallyActive;
            }

            return ClientShellWiringFault.None;
        }

        private static bool IsNested(
            in ClientShellRootSnapshot first,
            in ClientShellRootSnapshot second) =>
            first.HasAncestor(second.InstanceId) || second.HasAncestor(first.InstanceId);

        private static bool IsShellInside(
            in ClientShellRootSnapshot shell,
            in ClientShellRootSnapshot screenRoot) =>
            shell.InstanceId == screenRoot.InstanceId || shell.HasAncestor(screenRoot.InstanceId);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-09-07 | —      | Final P5b foundation validator: four opaque roots + shell only;|
// |         |            |        | non-main activeSelf is UI hygiene, never a P4b lifecycle guard.|
#endregion
