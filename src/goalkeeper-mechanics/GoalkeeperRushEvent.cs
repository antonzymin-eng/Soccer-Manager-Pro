// File:     src/goalkeeper-mechanics/GoalkeeperRushEvent.cs
// Created:  2026-05-28
// Modified: 2026-05-30
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.4, §3.7, §4.3, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Tier C event published at each lifecycle phase of a GK rush. Ordinal 0x17.
//           Immediate dispatch via CosmeticChannel; excluded from digest (animation/telemetry only).

using System.Runtime.InteropServices;

using UnityEngine;

using TacticalDirector.EventSystem;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Published at each lifecycle phase change of a GK rush / sweep.
    /// Tier C: cosmetic/telemetry; excluded from digest. Ordinal 0x17.
    /// Goalkeeper Mechanics #11 §2.2.4 / §3.7 / Event System #17 Appendix A.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct GoalkeeperRushEvent : IEventC
    {
        /// <summary>Unique GK agent ID. §2.2.4.</summary>
        public int AgentId;

        /// <summary>Match time (ms) at the phase-change frame. §2.2.4.</summary>
        public float MatchTimeMs;

        /// <summary>Rush lifecycle phase being reported. §3.7 / KD-15.</summary>
        public RushPhase RushPhase;

        /// <summary>Reason the rush was aborted. Meaningful only when RushPhase == Aborted. §3.7.3.</summary>
        public AbortReason AbortReason;

        /// <summary>World-space rush target that was locked at commit time (KD-15). §3.7.2.</summary>
        public Vector3 RushTarget;

        /// <summary>GK world-space position at the phase-change frame. §3.7.</summary>
        public Vector3 GkPosition;

        /// <summary>Rush launch speed (m/s) computed at launch frame. §3.7.1.</summary>
        public float RushLaunchMps;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                         |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                       |
// | 1.1     | 2026-05-30 | —      | Stage 1: added IEventC, [StructLayout(Sequential)].           |
// |         |            |        | Ordinal 0x17. Event System #17 §3.2.1 wiring.                 |
#endregion
