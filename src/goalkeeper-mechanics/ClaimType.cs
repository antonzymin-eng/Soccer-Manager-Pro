// File:     src/goalkeeper-mechanics/ClaimType.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.4, §3.6, Code Standards #20
// Purpose:  Telemetry-only categorisation of the type of ball-claim event.
//           Used on BallClaimedEvent for trace-pipeline discrimination.

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Telemetry label identifying what type of ball claim the GK completed.
    /// Published on BallClaimedEvent for trace-pipeline discrimination; not used by physics formulas.
    /// Goalkeeper Mechanics #11 §2.2.4 / §3.6.
    /// </summary>
    public enum ClaimType
    {
        /// <summary>GK claimed the ball from a cross or corner delivery. §3.6.</summary>
        Cross,

        /// <summary>GK won an aerial contest (non-cross). §3.6.</summary>
        Aerial,

        /// <summary>GK claimed the ball in a one-vs-one situation. §3.7 / KD-20.</summary>
        OneOnOne,

        /// <summary>GK caught a shot on target directly. §3.5.</summary>
        ShotCatch
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
