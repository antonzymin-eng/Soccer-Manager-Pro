// File:     src/shot-mechanics/ShotCancelReason.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §2.4.3, Code Standards #20
// Purpose:  Enum describing why a shot was cancelled.

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Reason a shot was cancelled. Shot Mechanics #6 §2.4.3.
    /// </summary>
    public enum ShotCancelReason
    {
        /// <summary>Collision System reported a tackle contact during WINDUP. §4.4.2.</summary>
        TackleInterrupt
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
