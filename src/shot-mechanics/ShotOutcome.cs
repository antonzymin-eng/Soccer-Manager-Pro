// File:     src/shot-mechanics/ShotOutcome.cs
// Created:  2026-05-27
// Modified: 2026-05-28
// Author:   —
// Spec:     Shot Mechanics #6 §2.4.2, Code Standards #20
// Purpose:  Enum describing the outcome of a shot execution cycle.

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Execution outcome of a shot. Shot Mechanics #6 §2.4.2.
    /// </summary>
    public enum ShotOutcome
    {
        /// <summary>Ball.ApplyKick() was called; ball is in flight.</summary>
        Completed,

        /// <summary>Shot cancelled by a game event before the ball was kicked.
        /// Covers: tackle interrupt during WINDUP (ShotCancelledEvent published per §4.7.1);
        /// possession loss detected at CONTACT (FM-03, §4.2.4 — no ShotCancelledEvent; §4.7.1 restricts
        /// that event to WINDUP tackle interrupts only).</summary>
        Cancelled,

        /// <summary>ShotRequest failed validation. Programming error — log only.</summary>
        Invalid,

        /// <summary>Shot has been initiated; WINDUP is in progress.</summary>
        Initiated
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation.                                                |
// | 1.1     | 2026-05-28 | —      | L-1: Cancelled XML doc expanded to cover FM-03 (CONTACT possession loss) |
// |         |            |        |   in addition to WINDUP tackle interrupts; §4.7.1 note added.           |
#endregion
