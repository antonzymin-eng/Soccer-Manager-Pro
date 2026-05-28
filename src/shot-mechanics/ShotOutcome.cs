// File:     src/shot-mechanics/ShotOutcome.cs
// Created:  2026-05-27
// Modified: 2026-05-27
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

        /// <summary>Tackle interrupt during WINDUP; ball was not kicked.</summary>
        Cancelled,

        /// <summary>ShotRequest failed validation. Programming error — log only.</summary>
        Invalid,

        /// <summary>Shot has been initiated; WINDUP is in progress.</summary>
        Initiated
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
