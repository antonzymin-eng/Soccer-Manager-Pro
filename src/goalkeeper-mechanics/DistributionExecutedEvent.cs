// File:     src/goalkeeper-mechanics/DistributionExecutedEvent.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.4, §3.8, §4.3, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Struct event published when the GK releases the ball during distribution.
//           Published at the release frame after windup elapses.

using UnityEngine;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Published when a GK distribution release is executed (windup elapsed, ball released).
    /// Ball.ApplyKick is invoked immediately before this event. §3.8 / §4.3.
    /// Goalkeeper Mechanics #11 §2.2.4 / §3.8.
    /// </summary>
    public struct DistributionExecutedEvent
    {
        /// <summary>Unique GK agent ID. §2.2.4.</summary>
        public int AgentId;

        /// <summary>Match time (ms) at the release frame. §2.2.4.</summary>
        public float MatchTimeMs;

        /// <summary>Distribution method executed. Telemetry label only (KD-1). §3.8.</summary>
        public DeliveryKind DeliveryKind;

        /// <summary>Actual world-space release point (gkPosition + Vector3(0, 0, releaseHeight)). §3.8.1.</summary>
        public Vector3 ReleasePoint;

        /// <summary>Optional intended receiver agent ID. Null if targeting a zone. §3.8.2.</summary>
        public int? TargetReceiverId;

        /// <summary>World-space target point from the DistributeIntent. §3.8.1.</summary>
        public Vector3 TargetPoint;

        /// <summary>Emitted power intent after accuracy-coeff scaling. §3.8.1.</summary>
        public float EmittedPowerIntent;

        /// <summary>Windup duration that elapsed (ms). §3.8.1.</summary>
        public float WindupMs;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
