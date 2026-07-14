// File:     src/event-system/OffsideCalledEvent.cs
// Created:  2026-07-13
// Modified: 2026-07-13
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-flow-completion-design.md) §4/§8, Code Standards #20
// Purpose:  Tier A event published when an offside violation is adjudicated in the Resolve phase.
//           Ordinal 0x18; produced from the Resolve phase; owned by the match-engine composition root.

using System.Runtime.InteropServices;

using UnityEngine;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Published when a reception is called offside (the Stage-0 reception-time approximation —
    /// see the match-flow-completion design note §4 for the model this stands in for).
    /// Tier A: included in the per-tick digest and SnapshotPayload (FR-EVT-011/012).
    /// Ordinal <c>0x18</c>. Produced from the <c>Resolve</c> phase.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct OffsideCalledEvent : IEventA
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
        /// <summary>EntityId of the agent whose reception was called offside.</summary>
        public readonly int OffendingAgentId;
        /// <summary>Team byte of the offending agent: 0=Home, 1=Away.</summary>
        public readonly byte Team;
        /// <summary>World-space position of the offence (the toucher's position at reception; m).</summary>
        public readonly Vector3 Location;

        /// <summary>
        /// Constructs an <see cref="OffsideCalledEvent"/> with payload fields only.
        /// Header fields are left at zero — EventBus.Publish will overwrite them.
        /// </summary>
        public OffsideCalledEvent(int offendingAgentId, byte team, Vector3 location)
        {
            eventTypeOrdinal    = 0;
            payloadVersion      = 0;
            _reserved           = 0;
            tick                = 0;
            subsystemOrdinal    = 0;
            intraPhaseDrawIndex = 0;
            OffendingAgentId    = offendingAgentId;
            Team                = team;
            Location            = location;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-13 | —      | Initial implementation. |
#endregion
