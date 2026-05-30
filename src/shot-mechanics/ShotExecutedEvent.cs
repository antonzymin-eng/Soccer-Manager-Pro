// File:     src/shot-mechanics/ShotExecutedEvent.cs
// Created:  2026-05-27
// Modified: 2026-05-30
// Author:   —
// Spec:     Shot Mechanics #6 §2.4.3, §4.5.2, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Struct event published at CONTACT state completion (Ball.ApplyKick() called).
//           Tier A event; ordinal 0x01 (pre-reserved in EventRegistry Appendix A). No ShotType field (KD-3).

using System.Runtime.InteropServices;

using UnityEngine;

using TacticalDirector.EventSystem;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Published to the event bus at CONTACT state completion, after Ball.ApplyKick() is called.
    /// Tier A: included in the per-tick digest (FR-EVT-011/012). Ordinal 0x01 (pre-reserved).
    /// No ShotType field — physical vectors carry all information needed. §2.4.3, §4.5.2, KD-3.
    /// Shot Mechanics #6 §2.4.3 / Event System #17 Appendix A.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct ShotExecutedEvent : IEventA
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
        /// <summary>Agent who took the shot. Identifies shooter for GK target tracking.</summary>
        public int ShootingAgentId;

        /// <summary>Team of shooting agent. Provided for event filtering by subscribers.</summary>
        public int TeamId;

        /// <summary>
        /// Final kick velocity (world space, m/s). GK Mechanics derives shot speed and trajectory
        /// from this alone; no separate speed field needed. §4.5.2.
        /// </summary>
        public Vector3 KickVelocity;

        /// <summary>Final spin vector (world space, rad/s). GK uses this to anticipate trajectory deviation.</summary>
        public Vector3 KickSpin;

        /// <summary>
        /// Intended goal-relative placement target (before error). (u, v) ∈ [0.0, 1.0]².
        /// Used for xG analytics and GK anticipation. Distinct from actual trajectory.
        /// </summary>
        public Vector2 IntendedTarget;

        /// <summary>Actual kick direction (unit vector) after error application. §3.6.</summary>
        public Vector3 FinalDirection;

        /// <summary>
        /// Body mechanics quality [0.0, 1.0]. GK difficulty estimation: low BMS → irregular trajectory.
        /// §3.7.
        /// </summary>
        public float BodyMechanicsScore;

        /// <summary>Power intent from ShotRequest [0.0, 1.0]. Preserved for xG model (Stage 1+).</summary>
        public float PowerIntent;

        /// <summary>Contact zone used. Preserved for trajectory class inference.</summary>
        public ContactZone ContactZone;

        /// <summary>Distance to goal (metres) at shot time.</summary>
        public float DistanceToGoal;

        /// <summary>Match time in seconds at Ball.ApplyKick() call. Replay and statistics.</summary>
        public float MatchTime;

        /// <summary>Frame number at Ball.ApplyKick(). Replay determinism.</summary>
        public int ContactFrame;

        /// <summary>
        /// True if shooter stumbled. Agent Movement subscribes and transitions to STUMBLING.
        /// GK: irregular trajectory possible. §4.3.3 Mechanism C.
        /// </summary>
        public bool StumbleTriggered;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                         |
// | 1.0     | 2026-05-27 | —      | Initial implementation.                                       |
// | 1.1     | 2026-05-30 | —      | Stage 1: added IEventA, [StructLayout(Sequential)], 12-byte   |
// |         |            |        | header fields. Ordinal 0x01. Event System #17 §3.2.1 wiring.  |
#endregion
