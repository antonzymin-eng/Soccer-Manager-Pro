// File:     src/goalkeeper-mechanics/BallClaimedEvent.cs
// Created:  2026-05-28
// Modified: 2026-05-30
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.4, §4.3, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Tier A event published when the GK successfully catches the ball. Ordinal 0x15.

using System.Runtime.InteropServices;

using UnityEngine;

using TacticalDirector.EventSystem;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Published when a GK successfully gains possession (catches ball or wins cross-claim duel).
    /// Tier A: included in the per-tick digest (Ball.SetPossessor follows). Ordinal 0x15.
    /// Goalkeeper Mechanics #11 §2.2.4 / §4.3 / Event System #17 Appendix A.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BallClaimedEvent : IEventA
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

        /// <summary>Match time (ms) at the claim frame. §2.2.4.</summary>
        public float MatchTimeMs;

        /// <summary>Handling quality scalar [0, 1] that triggered the catch band. §3.5.</summary>
        public float HandlingQualityScalar;

        /// <summary>Telemetry classification of how the claim was made. Not consumed by physics (KD-2). §2.2.4.</summary>
        public ClaimType ClaimType;

        /// <summary>World-space position of the hand at the claim frame. §2.2.4.</summary>
        public Vector3 ClaimPosition;

        /// <summary>Body part used for the claim (normally Hand; Head routes through Heading #10). §3.6.1.</summary>
        public BodyPartEnum ContactBodyPart;

        /// <summary>Duel ID if this claim resolved a contested duel; -1 for uncontested. §3.6.</summary>
        public int ContestedDuelId;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                         |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                       |
// | 1.1     | 2026-05-30 | —      | Stage 1: added IEventA, [StructLayout(Sequential)], 12-byte   |
// |         |            |        | header fields. Ordinal 0x15. Event System #17 §3.2.1 wiring.  |
#endregion
