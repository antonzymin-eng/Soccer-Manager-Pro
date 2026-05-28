// File:     src/goalkeeper-mechanics/AbortReason.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.7.3, KD-15, Code Standards #20
// Purpose:  Reason enumeration for GoalkeeperRushEvent when RushPhase == Aborted.

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Reason a GK rush was aborted. Published on GoalkeeperRushEvent when RushPhase = Aborted.
    /// The only runtime-triggered abort is BallIntercepted (F-08). The others are for edge-case telemetry.
    /// Goalkeeper Mechanics #11 §3.7.3 / KD-15.
    /// </summary>
    public enum AbortReason
    {
        /// <summary>Ball possession changed to a third agent (F-08). Only abort trigger per KD-15. §3.7.3.</summary>
        BallIntercepted,

        /// <summary>Ball cleared by a teammate or deflection during GK's rush. §3.7.3.</summary>
        BallCleared,

        /// <summary>Attacker passed the GK's X position while still in Rushing state. §3.7.3.</summary>
        AttackerBeatGK
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
