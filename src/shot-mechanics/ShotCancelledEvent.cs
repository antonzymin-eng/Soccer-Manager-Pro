// File:     src/shot-mechanics/ShotCancelledEvent.cs
// Created:  2026-05-27
// Modified: 2026-05-30
// Author:   —
// Spec:     Shot Mechanics #6 §2.4.3, §4.7.1, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Struct event published when a shot is cancelled by tackle interrupt during WINDUP.
//           Tier A event; ordinal 0x0E. NOT published for ShotOutcome.Invalid. §4.7.1.

using System.Runtime.InteropServices;

using TacticalDirector.EventSystem;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Published when a tackle interrupt fires during WINDUP, cancelling the shot.
    /// Not published for ShotOutcome.Invalid. Tier A; ordinal 0x0E. §2.4.3, §4.7.1.
    /// Shot Mechanics #6 §2.4.3 / Event System #17 Appendix A.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ShotCancelledEvent : IEventA
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
        /// <summary>Agent whose shot was cancelled.</summary>
        public int AgentId;

        /// <summary>Team of the shooting agent. For event filtering.</summary>
        public int TeamId;

        /// <summary>Simulation frame on which the tackle interrupt fired.</summary>
        public int CancelFrame;

        /// <summary>Reason for cancellation. TackleInterrupt is the only valid reason at Stage 0.</summary>
        public ShotCancelReason Reason;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                         |
// | 1.0     | 2026-05-27 | —      | Initial implementation.                                       |
// | 1.1     | 2026-05-30 | —      | Stage 1: added IEventA, [StructLayout(Sequential)], 12-byte   |
// |         |            |        | header fields. Ordinal 0x0E. Event System #17 §3.2.1 wiring.  |
#endregion
