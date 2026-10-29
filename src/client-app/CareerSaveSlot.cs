// File:     src/client-app/CareerSaveSlot.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (D-P7-2 save promise), Code Standards #20
// Purpose:  The career save slots the client offers: one autosave slot written after every round and
//           every season roll, and three manual slots the player writes explicitly.

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// A career save slot (§5-P7 D-P7-2, owner decision October 18, 2026: autosave + 3 manual slots).
    /// The ordinals double as the slot index in <see cref="CareerSaveSlots.List"/>; they are append-only.
    /// </summary>
    public enum CareerSaveSlot
    {
        /// <summary>The autosave slot, overwritten by the client after every played round and season roll.</summary>
        Autosave = 0,

        /// <summary>Manual slot 1.</summary>
        Manual1 = 1,

        /// <summary>Manual slot 2.</summary>
        Manual2 = 2,

        /// <summary>Manual slot 3.</summary>
        Manual3 = 3,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): autosave + three manual slots.       |
#endregion
