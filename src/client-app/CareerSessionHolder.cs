// File:     src/client-app/CareerSessionHolder.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7, UI / Client Framework #38 §3.2
//           (FR-UI-010 registration), Code Standards #20
// Purpose:  The one mutable slot holding the client's active career. Screen registrations are fixed at
//           flow construction, while careers come and go during a run (new, load, quit); the career
//           screens' sources and dispatcher read the career through this holder.

using System;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Holds the client's active <see cref="CareerSession"/>, or none (§5-P7). Constructor-injected into
    /// the career screens' view-model sources and the season dispatcher — an explicit owned object, not
    /// an ambient context or static. The client sets it when New/Load/Continue produce a session and
    /// clears it on Quit to Main Menu.
    /// </summary>
    public sealed class CareerSessionHolder
    {
        /// <summary>The active career, or null.</summary>
        public CareerSession Current { get; private set; }

        /// <summary>True when a career is active.</summary>
        public bool HasCareer => Current != null;

        /// <summary>Makes <paramref name="session"/> the active career.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="session"/> is null — use <see cref="Clear"/>.</exception>
        public void Set(CareerSession session)
        {
            Current = session ?? throw new ArgumentNullException(nameof(session));
        }

        /// <summary>Clears the active career (Quit to Main Menu).</summary>
        public void Clear() => Current = null;

        /// <summary>The active career.</summary>
        /// <exception cref="InvalidOperationException">No career is active.</exception>
        public CareerSession Require() =>
            Current ?? throw new InvalidOperationException("No career is active; start, continue or load one first.");
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7).                                      |
#endregion
