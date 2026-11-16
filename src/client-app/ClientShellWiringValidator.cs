// File:     src/client-app/ClientShellWiringValidator.cs
// Created:  2026-09-04
// Modified: 2026-11-03 (P7b: validate all seven screen roots)
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b,
//           Code Standards #20 §12 rule 1
// Purpose:  Pure P5b shell-structure validation. Unity collects ids/ancestor chains only; duplicate,
//           nesting, shell containment, ancestor-activation and initial-visibility decisions are made and
//           test-locked here.

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
            return ValidateRoots(in shell, new[] { mainMenu, tacticsSetup, matchView, postMatchReport });
        }

        /// <summary>
        /// Validates the P7b shell and all seven roots, including every match/career cross-pair.
        /// Validation occurs once during host bootstrap; all non-menu roots must be saved inactive.
        /// </summary>
        public static ClientShellWiringFault Validate(
            in ClientShellRootSnapshot shell,
            in ClientShellRootSnapshot mainMenu,
            in ClientShellRootSnapshot tacticsSetup,
            in ClientShellRootSnapshot matchView,
            in ClientShellRootSnapshot postMatchReport,
            in ClientShellRootSnapshot careerHome,
            in ClientShellRootSnapshot newCareer,
            in ClientShellRootSnapshot loadCareer)
        {
            return ValidateRoots(in shell,
                new[] { mainMenu, tacticsSetup, matchView, postMatchReport, careerHome, newCareer, loadCareer });
        }

        private static ClientShellWiringFault ValidateRoots(
            in ClientShellRootSnapshot shell, ClientShellRootSnapshot[] roots)
        {
            // Preserve the existing fault precedence across the entire root set.
            for (int i = 0; i < roots.Length; i++)
                if (roots[i].InstanceId == 0)
                    return ClientShellWiringFault.MissingRoot;

            for (int i = 0; i < roots.Length; i++)
                for (int j = i + 1; j < roots.Length; j++)
                    if (roots[i].InstanceId == roots[j].InstanceId)
                        return ClientShellWiringFault.DuplicateRoot;

            for (int i = 0; i < roots.Length; i++)
                for (int j = i + 1; j < roots.Length; j++)
                    if (IsNested(in roots[i], in roots[j]))
                        return ClientShellWiringFault.NestedRoot;

            for (int i = 0; i < roots.Length; i++)
                if (IsShellInside(in shell, in roots[i]))
                    return ClientShellWiringFault.ShellInsideScreenRoot;

            for (int i = 0; i < roots.Length; i++)
                if (!roots[i].AreAncestorsActive)
                    return ClientShellWiringFault.RootUnderInactiveAncestor;

            for (int i = 1; i < roots.Length; i++)
                if (roots[i].IsActiveSelf)
                    return ClientShellWiringFault.NonMainRootInitiallyActive;

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
// | 1.1     | 2026-10-05 | —      | PR #470 review (Codex P2): refuse any root beneath an inactive |
// |         |            |        | ancestor, checked after shell containment.                     |
// | 1.2     | 2026-11-03 | —      | P7b seven-root overload with shared fault precedence.         |
#endregion
