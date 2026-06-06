// File:     src/pass-mechanics/PassCancelledEvent.cs
// Created:  2026-05-26
// Modified: 2026-06-06
// Author:   —
// Spec:     Pass Mechanics #5 §3.9.3, Event System #17 §3.2.1, Code Standards #20
// Purpose:  PassCancelledEvent struct: published when a tackle interrupt cancels
//           the pass during WINDUP state. Tier A event; ordinal 0x0D.

using System.Runtime.InteropServices;

using TacticalDirector.EventSystem;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Published when the pass is cancelled before Ball.ApplyKick() succeeds.
    /// Triggers (§3.9.3): WINDUP tackle interrupt (TackleInterrupt), CONTACT-time
    /// possession loss (PossessionLost / FM-08), CONTACT-time NaN velocity
    /// (InvalidVelocity / FM-04). Invalid request rejection does NOT produce this
    /// event (KD-8, §3.9.3).
    /// Tier A: included in the per-tick digest. Ordinal 0x0D.
    /// Pass Mechanics #5 §3.9.3 / Event System #17 Appendix A.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PassCancelledEvent : IEventA
    {
        // ── 12-byte header (§2.4.1) — set by EventBus.Publish at enqueue time ────────
        /// <summary>Event type ordinal from Appendix A. Set by EventBus; do not set manually.</summary>
        public byte   EventTypeOrdinal;
        /// <summary>Payload schema version. Set by EventBus.</summary>
        public byte   PayloadVersion;
        /// <summary>Reserved padding; publishers MUST leave at default zero — canonical
        /// serialisation assumes zero and EventBus does not write this slot.</summary>
        public ushort Reserved;
        /// <summary>Physics tick at publish time. Set by EventBus.</summary>
        public uint   Tick;
        /// <summary>Producing subsystem ordinal (#16 §3.1.1). Set by EventBus.</summary>
        public ushort SubsystemOrdinal;
        /// <summary>Per-tick per-phase draw index (FM-017-002). Set by EventBus.</summary>
        public ushort IntraPhaseDrawIndex;

        // ── Payload fields ────────────────────────────────────────────────────────────
        /// <summary>ID of the agent whose pass was cancelled.</summary>
        public int AgentId;

        /// <summary>Team of the executing agent.</summary>
        public int TeamId;

        /// <summary>Cancellation reason: TackleInterrupt / PossessionLost / InvalidVelocity.</summary>
        public CancelReason Reason;

        /// <summary>Pass type that was in progress.</summary>
        public PassType PassType;

        /// <summary>Simulation frame at cancellation.</summary>
        public int Frame;

        /// <summary>Match time in seconds at cancellation.</summary>
        public float MatchTime;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-05-26 | —      | Extracted from PassEvents.cs per one-type-per-file rule (H4).    |
// | 1.1     | 2026-05-30 | —      | Stage 1: added IEventA, [StructLayout(Sequential)], 12-byte      |
// |         |            |        | header fields. Ordinal 0x0D. Event System #17 §3.2.1 wiring.    |
// | 1.2     | 2026-06-06 | —      | AR-2 M-6: 12-byte header public fields renamed camelCase →       |
// |         |            |        |     PascalCase per FR-CS-001 (parallel to PassAttemptEvent v1.2).|
// |         |            |        | AR-2 L-8: payload field CancelReason → Reason (field name no     |
// |         |            |        |     longer shadows the CancelReason enum type — reading hazard   |
// |         |            |        |     for tools that parse by symbol).                             |
// |         |            |        | XML doc expanded to cover the new TackleInterrupt /              |
// |         |            |        |     PossessionLost / InvalidVelocity cancellation surface         |
// |         |            |        |     (AR-2 H-1 / H-2).                                           |
// | 1.3     | 2026-06-06 | —      | AR-3 L-8: Reserved XML doc corrected (parallel to PassAttempt    |
// |         |            |        |     v1.3) — publishers leave at zero; EventBus does not write.    |
#endregion
