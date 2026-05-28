// File:     src/goalkeeper-mechanics/RushPhase.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.7, KD-15, Code Standards #20
// Purpose:  Telemetry-only phase label for GoalkeeperRushEvent, indicating where in
//           the rush lifecycle the event was fired.

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Phase of the GK rush lifecycle at which a GoalkeeperRushEvent is emitted.
    /// Used on GoalkeeperRushEvent.RushPhase for trace-pipeline discrimination.
    /// Goalkeeper Mechanics #11 §3.7 / KD-15.
    /// </summary>
    public enum RushPhase
    {
        /// <summary>Rush launch impulse applied; GK beginning sprint toward rush target. §3.7.1.</summary>
        Launched,

        /// <summary>GK is actively moving toward rush target each physics frame. §3.7.2.</summary>
        InFlight,

        /// <summary>GK has reached the rush target or made contact with the ball. §3.7.</summary>
        Reached,

        /// <summary>Rush was aborted before completion. See AbortReason for cause. §3.7.3.</summary>
        Aborted
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
