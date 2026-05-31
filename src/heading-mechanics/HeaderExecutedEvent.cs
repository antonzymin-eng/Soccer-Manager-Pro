// File:     src/heading-mechanics/HeaderExecutedEvent.cs
// Created:  2026-05-28
// Modified: 2026-05-30
// Author:   —
// Spec:     Heading Mechanics #10 §2.2, §4.3, KD-2, KD-6, KD-13, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Tier B event published on every successfully contacted header. Ordinal 0x12.

using System.Runtime.InteropServices;

using UnityEngine;

using TacticalDirector.BallPhysics;
using TacticalDirector.EventSystem;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Published on every successfully contacted header (winner and disturbed-but-executed losers).
    /// Tier B: included in the determinism digest (outgoing velocity / spin modify BallState). Ordinal 0x12.
    /// Heading Mechanics #10 §2.2 / §4.3 / Event System #17 §3.2.1.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct HeaderExecutedEvent : IEventB
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
        /// <summary>Agent that made the header contact.</summary>
        public int AgentId;

        /// <summary>Match time in seconds from kickoff at the contact frame. §2.2.</summary>
        public float MatchTime;

        /// <summary>Continuous contact quality scalar [0, 1]. §3.4 / KD-2.</summary>
        public float ContactQualityScalar;

        /// <summary>
        /// Telemetry-only timing bucket. NEVER consumed by physics formulas (KD-2 / FR-HE-002).
        /// Centred bucket = OnTime (FR-HE-020).
        /// </summary>
        public ContactQualityLabel ContactQualityLabel;

        /// <summary>
        /// Actual 2-D contact point in head-local coordinates (m).
        /// Origin = head centre. +x = agent.facing forward; +y = agent-left lateral.
        /// </summary>
        public Vector2 ContactPoint;

        /// <summary>Ball physics state at the contact frame (snapshot). §3.4 / §4.3.</summary>
        public BallState IncomingBallState;

        /// <summary>Outgoing ball velocity applied via Ball.ApplyKick (m/s). §3.5 / §4.3.</summary>
        public Vector3 OutgoingVelocity;

        /// <summary>Outgoing ball angular velocity applied via Ball.ApplyKick (rad/s). §3.6 / §4.3.</summary>
        public Vector3 OutgoingSpin;

        /// <summary>Duel ID when header was contested; -1 when uncontested. §3.7.</summary>
        public int ContestedDuelId;

        /// <summary>True when outgoing trajectory projects through the agent's own goal bounding box. KD-6 / FR-HE-007.</summary>
        public bool OwnGoalShapedTrajectory;

        /// <summary>
        /// Delivery type for telemetry. NEVER consumed by physics formulas (KD-13 / FR-HE-016).
        /// Physics is uniform across OpenPlay / Corner / FreeKick deliveries.
        /// </summary>
        public SetPieceContext SetPieceContext;

        /// <summary>Sentinel value for ContestedDuelId when the header was uncontested.</summary>
        public const int UncontestedDuelId = -1;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                         |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                       |
// | 1.1     | 2026-05-30 | —      | Stage 1: added IEventB, [StructLayout(Sequential)], 12-byte   |
// |         |            |        | header fields. Ordinal 0x12. Event System #17 §3.2.1 wiring.  |
#endregion
