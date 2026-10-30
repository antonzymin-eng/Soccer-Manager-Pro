// File:     src/client-app/CareerSlotState.cs
// Created:  2026-10-18
// Modified: 2026-10-27
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-2 save promise, D-10 breakable
//           saves through PM-3), ux-experience-architecture.md §7 (save/load transition rules),
//           Code Standards #20
// Purpose:  What the Load Career picker may say about a slot without opening the save itself.

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// A save slot's state as the picker reports it (§5-P7). Only <see cref="Ready"/> is loadable.
    /// <see cref="Incompatible"/> is the D-10 promise made visible: saves are declared breakable through
    /// PM-3, so a slot written under another season-save format version is shown as such and never
    /// offered as Continue — it is not silently hidden and not attempted.
    /// </summary>
    public enum CareerSlotState
    {
        /// <summary>Nothing has been saved to this slot.</summary>
        Empty = 0,

        /// <summary>A save written by this build's season-save format; loadable.</summary>
        Ready = 1,

        /// <summary>
        /// A save written under a different season-save, nested sub-blob or slot format version (D-10:
        /// not migrated).
        /// </summary>
        Incompatible = 2,

        /// <summary>
        /// The save or its sidecar is missing, truncated or unreadable — including a slot invalidated
        /// after its save failed to load (<see cref="CareerSaveSlots.MarkDamaged"/>).
        /// </summary>
        Damaged = 3,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): Empty / Ready / Incompatible / Damaged. |
// | 1.1     | 2026-10-27 | —      | PR 519 review: Incompatible covers nested sub-blob versions;   |
// |         |            |        | Damaged covers a slot invalidated after a failed load. Docs only. |
#endregion
