// File:     src/event-system/CardIssuedEvent.cs
// Created:  2026-05-30
// Modified: 2026-06-02
// Author:   —
// Spec:     Event System #17 §2.4.1, Appendix A row 0x06, Code Standards #20
// Purpose:  Tier A event published when a yellow or red card is issued in the Resolve phase.
//           Ordinal 0x06; produced from Resolve phase; owned by Spec #17 (default owner).

using System.Runtime.InteropServices;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Published when a disciplinary card is issued by the referee.
    /// Tier A: included in the per-tick digest and SnapshotPayload (FR-EVT-011/012).
    /// Ordinal <c>0x06</c>. Produced from the <c>Resolve</c> phase.
    /// Event System #17 Appendix A / §2.4.1.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct CardIssuedEvent : IEventA
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

        // ── Payload fields (Appendix A row 0x06) ─────────────────────────────────────
        /// <summary>EntityId of the agent receiving the card.</summary>
        public readonly int Recipient;
        /// <summary>Card kind: 0=Yellow, 1=Red, 2=SecondYellow (domain ordinal).</summary>
        public readonly byte CardKind;
        /// <summary>IntraPhaseDrawIndex of the FoulCommittedEvent that triggered this card; 0xFFFF if procedural (no associated foul).
        /// AR-5 L-1: widened from byte to ushort to match the ushort intraPhaseDrawIndex
        /// header field (range 0..65535) — a byte sentinel could not represent draw indices &gt; 254.</summary>
        public readonly ushort FoulOrdinal;

        /// <summary>
        /// Constructs a <see cref="CardIssuedEvent"/> with payload fields only.
        /// Header fields are left at zero — EventBus.Publish will overwrite them.
        /// </summary>
        public CardIssuedEvent(int recipient, byte cardKind, ushort foulOrdinal)
        {
            eventTypeOrdinal    = 0;
            payloadVersion      = 0;
            _reserved           = 0;
            tick                = 0;
            subsystemOrdinal    = 0;
            intraPhaseDrawIndex = 0;
            Recipient           = recipient;
            CardKind            = cardKind;
            FoulOrdinal         = foulOrdinal;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation.                                                 |
// | 1.1     | 2026-05-30 | —      | AR-1 L-1: FoulOrdinal doc corrected (-1 invalid for byte; now 0xFF).   |
// | 1.2     | 2026-06-02 | —      | AR-5 L-1: FoulOrdinal widened from byte to ushort. Doc claimed it      |
// |         |            |        | stored "IntraPhaseDrawIndex of the FoulCommittedEvent that triggered   |
// |         |            |        | this card", but intraPhaseDrawIndex is a ushort (range 0..65535) per   |
// |         |            |        | §2.4.1 — a byte field could not represent draw indices > 254.         |
// |         |            |        | Sentinel updated 0xFF → 0xFFFF. No call sites exist yet (Stage 0       |
// |         |            |        | wiring not implemented), so widening is non-breaking.                 |
// | 1.2.1   | 2026-06-02 | —      | AR-6 M-1: header Modified date refreshed to match the latest          |
// |         |            |        | version-history row (FR-CS-057). AR-5 added the v1.2 row but left     |
// |         |            |        | the Modified header at 2026-05-30. No code change in this revision.   |
#endregion
