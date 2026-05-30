// File:     src/goalkeeper-mechanics/DistributionExecutedEvent.cs
// Created:  2026-05-28
// Modified: 2026-05-30
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.4, §3.8, §4.3, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Tier A event published when the GK releases the ball during distribution. Ordinal 0x16.

using System.Runtime.InteropServices;

using UnityEngine;

using TacticalDirector.EventSystem;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Published when a GK distribution release is executed (windup elapsed, ball released).
    /// Tier A: included in the per-tick digest (Ball.ApplyKick precedes this event). Ordinal 0x16.
    /// Goalkeeper Mechanics #11 §2.2.4 / §3.8 / Event System #17 Appendix A.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct DistributionExecutedEvent : IEventA
    {
        // ── 12-byte header (§2.4.1) — set by EventBus.Publish at enqueue time ────────
        /// <summary>Event type ordinal from Appendix A. Set by EventBus; do not set manually.</summary>
        public byte   eventTypeOrdinal;
        /// <summary>Payload schema version. Set by EventBus.</summary>
        public byte   payloadVersion;
        /// <summary>Reserved padding; canonical zero. Set by EventBus.</summary>
        public ushort _reserved;
        /// <summary>Physics tick at publish time. Set by EventBus.</summary>
        public uint   tick;
        /// <summary>Producing subsystem ordinal (#16 §3.1.1). Set by EventBus.</summary>
        public ushort subsystemOrdinal;
        /// <summary>Per-tick per-phase draw index (FM-017-002). Set by EventBus.</summary>
        public ushort intraPhaseDrawIndex;

        // ── Payload fields ────────────────────────────────────────────────────────────
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
// | Version | Date       | Author | Notes                                                         |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                       |
// | 1.1     | 2026-05-30 | —      | Stage 1: added IEventA, [StructLayout(Sequential)], 12-byte   |
// |         |            |        | header fields. Ordinal 0x16. Event System #17 §3.2.1 wiring.  |
#endregion
