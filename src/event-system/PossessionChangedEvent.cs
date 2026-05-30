// File:     src/event-system/PossessionChangedEvent.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Event System #17 §2.4.1, Appendix A row 0x04, Code Standards #20
// Purpose:  Tier A event published when ball possession changes between agents/teams.
//           Ordinal 0x04; produced from Resolve phase; owned by Spec #17 (default owner).

using System.Runtime.InteropServices;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Published when ball possession transitions from one holder to another.
    /// Tier A: included in the per-tick digest and SnapshotPayload (FR-EVT-011/012).
    /// Ordinal <c>0x04</c>. Produced from the <c>Resolve</c> phase.
    /// Event System #17 Appendix A / §2.4.1.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct PossessionChangedEvent : IEventA
    {
        // ── 12-byte header (§2.4.1) — set by EventBus.Publish at enqueue time ────────
        /// <summary>Event type ordinal from Appendix A. Set by EventBus; do not set in constructor.</summary>
        public readonly byte   eventTypeOrdinal;
        /// <summary>Payload schema version. Set by EventBus.</summary>
        public readonly byte   payloadVersion;
        /// <summary>Reserved padding; canonical zero. Set by EventBus.</summary>
        public readonly ushort _reserved;
        /// <summary>Physics tick at publish time. Set by EventBus.</summary>
        public readonly uint   tick;
        /// <summary>Producing subsystem ordinal (#16 §3.1.1). Set by EventBus.</summary>
        public readonly ushort subsystemOrdinal;
        /// <summary>Per-tick per-phase draw index (FM-017-002). Set by EventBus.</summary>
        public readonly ushort intraPhaseDrawIndex;

        // ── Payload fields (canonical declaration order; Appendix A row 0x04) ─────────
        /// <summary>EntityId of the agent who previously held possession; -1 if none (loose ball).</summary>
        public readonly int PreviousHolder;
        /// <summary>EntityId of the agent who now holds possession; -1 if loose ball.</summary>
        public readonly int NewHolder;
        /// <summary>Reason code for the possession change (domain-specific enum ordinal).</summary>
        public readonly byte Reason;

        /// <summary>
        /// Constructs a <see cref="PossessionChangedEvent"/> with payload fields only.
        /// Header fields are left at zero — EventBus.Publish will overwrite them. §2.4.1.
        /// </summary>
        public PossessionChangedEvent(int previousHolder, int newHolder, byte reason)
        {
            eventTypeOrdinal    = 0;
            payloadVersion      = 0;
            _reserved           = 0;
            tick                = 0;
            subsystemOrdinal    = 0;
            intraPhaseDrawIndex = 0;
            PreviousHolder      = previousHolder;
            NewHolder           = newHolder;
            Reason              = reason;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
