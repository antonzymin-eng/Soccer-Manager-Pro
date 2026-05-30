// File:     src/event-system/SubstitutionEvent.cs
// Created:  2026-05-30
// Modified: 2026-05-30
// Author:   —
// Spec:     Event System #17 §2.4.1, Appendix A row 0x08, Code Standards #20
// Purpose:  Tier A event published when a substitution is made during the Resolve phase.
//           Ordinal 0x08; produced from Resolve phase; owned by Spec #17 (default owner).

using System.Runtime.InteropServices;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Published when a player substitution is processed by the match rules engine.
    /// Tier A: included in the per-tick digest and SnapshotPayload (FR-EVT-011/012).
    /// Ordinal <c>0x08</c>. Produced from the <c>Resolve</c> phase.
    /// Event System #17 Appendix A / §2.4.1.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct SubstitutionEvent : IEventA
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

        // ── Payload fields (Appendix A row 0x08) ─────────────────────────────────────
        /// <summary>EntityId of the agent being substituted off.</summary>
        public readonly int Outgoing;
        /// <summary>EntityId of the agent being substituted on.</summary>
        public readonly int Incoming;
        /// <summary>Team byte: 0=Home, 1=Away.</summary>
        public readonly byte Team;
        /// <summary>Substitution reason ordinal (domain-specific enum; e.g., Tactical/Injury).</summary>
        public readonly byte SubstitutionReason;

        /// <summary>
        /// Constructs a <see cref="SubstitutionEvent"/> with payload fields only.
        /// Header fields are left at zero — EventBus.Publish will overwrite them.
        /// </summary>
        public SubstitutionEvent(int outgoing, int incoming, byte team, byte substitutionReason)
        {
            eventTypeOrdinal    = 0;
            payloadVersion      = 0;
            _reserved           = 0;
            tick                = 0;
            subsystemOrdinal    = 0;
            intraPhaseDrawIndex = 0;
            Outgoing            = outgoing;
            Incoming            = incoming;
            Team                = team;
            SubstitutionReason  = substitutionReason;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-30 | —      | Initial implementation. |
#endregion
