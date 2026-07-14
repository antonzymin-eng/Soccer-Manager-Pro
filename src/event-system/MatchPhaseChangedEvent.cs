// File:     src/event-system/MatchPhaseChangedEvent.cs
// Created:  2026-07-14
// Modified: 2026-07-14
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-flow-completion-design.md) §7/§8, Code Standards #20
// Purpose:  Tier A event published when the match transitions to the second half or to full time.
//           Ordinal 0x1A; produced from the Input phase (where the transition is detected); owned
//           by the match-engine composition root.

using System.Runtime.InteropServices;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Published once when the match reaches the half-time boundary (ends swap) or full time
    /// (gameplay freeze). Tier A: included in the per-tick digest and SnapshotPayload
    /// (FR-EVT-011/012). Ordinal <c>0x1A</c>. Produced from the <c>Input</c> phase.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct MatchPhaseChangedEvent : IEventA
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

        // ── Payload fields ────────────────────────────────────────────────────────
        /// <summary>New phase ordinal: 0 = SecondHalf, 1 = FullTime.</summary>
        public readonly byte NewPhase;
        /// <summary>Home team goal count at the moment of transition.</summary>
        public readonly int HomeScore;
        /// <summary>Away team goal count at the moment of transition.</summary>
        public readonly int AwayScore;

        /// <summary>
        /// Constructs a <see cref="MatchPhaseChangedEvent"/> with payload fields only.
        /// Header fields are left at zero — EventBus.Publish will overwrite them.
        /// </summary>
        public MatchPhaseChangedEvent(byte newPhase, int homeScore, int awayScore)
        {
            eventTypeOrdinal    = 0;
            payloadVersion      = 0;
            _reserved           = 0;
            tick                = 0;
            subsystemOrdinal    = 0;
            intraPhaseDrawIndex = 0;
            NewPhase            = newPhase;
            HomeScore           = homeScore;
            AwayScore           = awayScore;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-14 | —      | Initial implementation. |
#endregion
