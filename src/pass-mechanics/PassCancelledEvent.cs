// File:     src/pass-mechanics/PassCancelledEvent.cs
// Created:  2026-05-26
// Modified: 2026-05-30
// Author:   —
// Spec:     Pass Mechanics #5 §3.9.3, Event System #17 §3.2.1, Code Standards #20
// Purpose:  PassCancelledEvent struct: published when a tackle interrupt cancels
//           the pass during WINDUP state. Tier A event; ordinal 0x0D.

using System.Runtime.InteropServices;

using TacticalDirector.EventSystem;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Published when a tackle interrupt cancels the pass during WINDUP state.
    /// Invalid request rejection does NOT produce this event (KD-8, §3.9.3).
    /// Tier A: included in the per-tick digest. Ordinal 0x0D.
    /// Pass Mechanics #5 §3.9.3 / Event System #17 Appendix A.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PassCancelledEvent : IEventA
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
        /// <summary>ID of the agent whose pass was cancelled.</summary>
        public int AgentId;

        /// <summary>Team of the executing agent.</summary>
        public int TeamId;

        /// <summary>Reason for cancellation (TackleInterrupt in Stage 0).</summary>
        public CancelReason CancelReason;

        /// <summary>Pass type that was in progress.</summary>
        public PassType PassType;

        /// <summary>Simulation frame at cancellation.</summary>
        public int Frame;

        /// <summary>Match time in seconds at cancellation.</summary>
        public float MatchTime;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                           |
// | 1.0     | 2026-05-26 | —      | Extracted from PassEvents.cs per one-type-per-file rule (H4).  |
// | 1.1     | 2026-05-30 | —      | Stage 1: added IEventA, [StructLayout(Sequential)], 12-byte    |
// |         |            |        | header fields. Ordinal 0x0D. Event System #17 §3.2.1 wiring.   |
#endregion
