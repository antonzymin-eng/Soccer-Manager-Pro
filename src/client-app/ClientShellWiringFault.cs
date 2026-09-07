// File:     src/client-app/ClientShellWiringFault.cs
// Created:  2026-09-04
// Modified: 2026-09-06
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P5a / §5-P5b,
//           Code Standards #20 §12 rule 1
// Purpose:  Closed, host-free result set for P5b shell structural validation.

namespace TacticalDirector.ClientApp
{
    /// <summary>The complete fail-loud result set for <see cref="ClientShellWiringValidator"/>.</summary>
    public enum ClientShellWiringFault
    {
        /// <summary>All P5b shell structural rules hold.</summary>
        None = 0,

        /// <summary>At least one required screen root was not assigned.</summary>
        MissingRoot = 1,

        /// <summary>Two catalogue screens resolve to the same host object.</summary>
        DuplicateRoot = 2,

        /// <summary>One screen root is nested beneath another screen root.</summary>
        NestedRoot = 3,

        /// <summary>The always-active shell controller lives on or beneath a screen root.</summary>
        ShellInsideScreenRoot = 4,

        /// <summary>A non-Main-Menu root is saved active before the shell has taken control.</summary>
        NonMainRootInitiallyActive = 5,

        /// <summary>The P4b match binding is missing or is not hosted on/beneath Match View.</summary>
        MatchBindingOutsideMatchViewRoot = 6
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-09-04 | —      | Initial closed fault vocabulary for extracted P5b validation.  |
// | 1.1     | 2026-09-06 | —      | Require the P4b match binding to live on/beneath Match View.   |
#endregion
