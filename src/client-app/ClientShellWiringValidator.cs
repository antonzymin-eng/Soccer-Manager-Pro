// File:     src/client-app/ClientShellWiringValidator.cs
// Created:  2026-09-04
// Modified: 2026-09-06
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b,
//           Code Standards #20 §12 rule 1
// Purpose:  Pure P5b shell-structure validation. Unity collects ids/ancestor chains only; duplicate,
//           nesting, containment, lifecycle-host placement, and initial-visibility decisions are made
//           and test-locked here.

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Validates the P5b host structure without referencing Unity. This is deliberately
    /// decision-bearing: the Unity-only binding is permanently outside the shim gate, so every branch
    /// that decides whether a scene arrangement is legal belongs here (§12 rule 1).
    /// </summary>
    public static class ClientShellWiringValidator
    {
        /// <summary>
        /// Validates one shell controller, the four catalogue screen roots, and the P4b match binding host.
        /// <para>
        /// The three non-Main-Menu roots must be saved inactive, and the P4b match binding must live on or
        /// beneath Match View. Together those rules make the temporary lifecycle boundary structural:
        /// P4b's current <c>MatchClientBehaviour</c> still constructs its demo session in <c>Awake</c> and
        /// starts it from <c>Start</c>, so the shell must be able to deactivate the binding by deactivating
        /// Match View before that host can run.
        /// </para>
        /// </summary>
        public static ClientShellWiringFault Validate(
            in ClientShellRootSnapshot shell,
            in ClientShellRootSnapshot mainMenu,
            in ClientShellRootSnapshot tacticsSetup,
            in ClientShellRootSnapshot matchView,
            in ClientShellRootSnapshot postMatchReport,
            in ClientShellRootSnapshot matchBinding)
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

            if (!IsOnOrInside(in matchBinding, in matchView))
            {
                return ClientShellWiringFault.MatchBindingOutsideMatchViewRoot;
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

        private static bool IsOnOrInside(
            in ClientShellRootSnapshot node,
            in ClientShellRootSnapshot requiredRoot) =>
            node.InstanceId != 0 &&
            (node.InstanceId == requiredRoot.InstanceId || node.HasAncestor(requiredRoot.InstanceId));
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-09-04 | —      | Extracted all P5b structure/initial-visibility decisions from  |
// |         |            |        | the gate-invisible Unity binding after PR #361 H1/H2 review.  |
// | 1.1     | 2026-09-06 | —      | H2 follow-up: require P4b host on/beneath Match View so root   |
// |         |            |        | deactivation structurally suppresses its Awake/Start path.    |
#endregion
