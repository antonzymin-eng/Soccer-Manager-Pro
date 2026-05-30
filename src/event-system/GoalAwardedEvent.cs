// File:     src/event-system/GoalAwardedEvent.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Event System #17 §2.4.1, Appendix A row 0x07, Code Standards #20
// Purpose:  Tier A event published when a goal is awarded in the Resolve phase.
//           Ordinal 0x07; produced from Resolve phase; owned by Spec #17 (default owner).

using System.Runtime.InteropServices;

using UnityEngine;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Published when a goal is awarded by the match rules engine.
    /// Tier A: included in the per-tick digest and SnapshotPayload (FR-EVT-011/012).
    /// Ordinal <c>0x07</c>. Produced from the <c>Resolve</c> phase.
    /// Event System #17 Appendix A / §2.4.1.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct GoalAwardedEvent : IEventA
    {
        // ── 12-byte header (§2.4.1) — set by EventBus.Publish at enqueue time ────────
        /// <summary>Event type ordinal from Appendix A. Set by EventBus.</summary>
        public readonly byte   eventTypeOrdinal;
        /// <summary>Payload schema version. Set by EventBus.</summary>
        public readonly byte   payloadVersion;
        /// <summary>Reserved padding; canonical zero. Set by EventBus.</summary>
        public readonly ushort _reserved;
        /// <summary>Physics tick at publish time. Set by EventBus.</summary>
        public readonly uint   tick;
        /// <summary>Producing subsystem ordinal. Set by EventBus.</summary>
        public readonly ushort subsystemOrdinal;
        /// <summary>Per-tick per-phase draw index. Set by EventBus.</summary>
        public readonly ushort intraPhaseDrawIndex;

        // ── Payload fields (Appendix A row 0x07) ─────────────────────────────────────
        /// <summary>EntityId of the agent credited with the goal; -1 if own goal with no clear scorer.</summary>
        public readonly int Scorer;
        /// <summary>EntityId of the last agent to assist; -1 if no assist.</summary>
        public readonly int Assister;
        /// <summary>Team byte: 0=Home, 1=Away.</summary>
        public readonly byte ScoringTeam;
        /// <summary>World-space position of the ball at the moment it crossed the goal line (m).</summary>
        public readonly Vector3 BallPosition;

        /// <summary>
        /// Constructs a <see cref="GoalAwardedEvent"/> with payload fields only.
        /// Header fields are left at zero — EventBus.Publish will overwrite them.
        /// </summary>
        public GoalAwardedEvent(int scorer, int assister, byte scoringTeam, Vector3 ballPosition)
        {
            eventTypeOrdinal    = 0;
            payloadVersion      = 0;
            _reserved           = 0;
            tick                = 0;
            subsystemOrdinal    = 0;
            intraPhaseDrawIndex = 0;
            Scorer              = scorer;
            Assister            = assister;
            ScoringTeam         = scoringTeam;
            BallPosition        = ballPosition;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
