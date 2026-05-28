// File:     src/goalkeeper-mechanics/HandEnum.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.1, KD-1, Code Standards #20
// Purpose:  Anatomy-lookup label for GK hand choice in SaveIntent and GkContactState.
//           KD-1 carve-out: this is anatomy lookup, NOT a physics input that gates formulas.

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// GK hand choice for a save attempt. KD-1 carve-out: this is an anatomy-lookup label
    /// used to select the correct hand capsule for collision queries; it does NOT gate any
    /// physics formula (no type enum in the physics layer per CLAUDE.md / KD-1).
    /// Goalkeeper Mechanics #11 §2.2.1.
    /// </summary>
    public enum HandEnum
    {
        /// <summary>GK intends to use the left hand for the save. §2.2.1.</summary>
        Left,

        /// <summary>GK intends to use the right hand for the save. §2.2.1.</summary>
        Right,

        /// <summary>Either hand is acceptable; closest-hand heuristic applies. §2.2.1.</summary>
        Either
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
