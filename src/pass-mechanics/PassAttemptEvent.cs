// File:     src/pass-mechanics/PassAttemptEvent.cs
// Created:  2026-05-26
// Modified: 2026-05-30
// Author:   —
// Spec:     Pass Mechanics #5 §3.9.2, Event System #17 §3.2.1, Code Standards #20
// Purpose:  PassAttemptEvent struct: published at CONTACT immediately after
//           Ball.ApplyKick() succeeds. Tier A event; ordinal 0x0C. All value types.

using System.Runtime.InteropServices;

using UnityEngine;

using TacticalDirector.EventSystem;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Published at CONTACT state immediately after Ball.ApplyKick() succeeds.
    /// Tier A: included in the per-tick digest (FR-EVT-011/012). Ordinal 0x0C.
    /// Pass Mechanics #5 §3.9.2 / Event System #17 Appendix A.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct PassAttemptEvent : IEventA
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
        /// <summary>ID of the passing agent.</summary>
        public int AgentId;

        /// <summary>Team of the passing agent.</summary>
        public int TeamId;

        /// <summary>Pass type executed.</summary>
        public PassType PassType;

        /// <summary>Cross sub-type (Flat if non-cross).</summary>
        public CrossSubType CrossSubType;

        /// <summary>Pre-error aim point (world space).</summary>
        public Vector3 TargetPosition;

        /// <summary>Final velocity vector passed to ApplyKick().</summary>
        public Vector3 FinalVelocity;

        /// <summary>Final spin vector passed to ApplyKick().</summary>
        public Vector3 FinalSpin;

        /// <summary>Error magnitude applied in degrees.</summary>
        public float ErrorAngleDeg;

        /// <summary>Scalar launch speed in m/s.</summary>
        public float KickSpeed;

        /// <summary>Through-ball lead distance in metres (0 for player-targeted).</summary>
        public float LeadDistance;

        /// <summary>True if the non-preferred foot was used.</summary>
        public bool IsWeakFoot;

        /// <summary>Target agent ID; -1 for space-targeted passes.</summary>
        public int TargetAgentId;

        /// <summary>Simulation frame at CONTACT.</summary>
        public int Frame;

        /// <summary>Match time in seconds at CONTACT.</summary>
        public float MatchTime;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-05-26 | —      | Extracted from PassEvents.cs per one-type-per-file rule (H4).   |
// | 1.1     | 2026-05-30 | —      | Stage 1: added IEventA, [StructLayout(Sequential)], 12-byte      |
// |         |            |        | header fields. Ordinal 0x0C. Event System #17 §3.2.1 wiring.    |
#endregion
