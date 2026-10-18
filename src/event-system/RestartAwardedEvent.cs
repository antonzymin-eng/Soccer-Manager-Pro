// File:     src/event-system/RestartAwardedEvent.cs
// Modified: 2026-10-16 (owner-approved re-handling fix)
// Created:  2026-07-13
// Modified: 2026-07-13
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-flow-completion-design.md) §5/§8, Code Standards #20
// Purpose:  Tier A event published when a throw-in, corner, or goal kick is awarded and applied in
//           the Resolve phase. Ordinal 0x19; produced from the Resolve phase; owned by the
//           match-engine composition root. Goals publish GoalAwardedEvent (0x07) instead.

using System.Runtime.InteropServices;

using UnityEngine;

namespace TacticalDirector.EventSystem
{
    /// <summary>
    /// Published when a boundary restart or goalkeeper re-handling free kick is awarded and the
    /// restart is applied. Tier A: included in the per-tick digest and SnapshotPayload (FR-EVT-011/012).
    /// Ordinal <c>0x19</c>. Produced from the <c>Resolve</c> phase.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct RestartAwardedEvent : IEventA
    {
        /// <summary>[FIXED] Law 12/13 indirect free kick. Event-only appended code;
        /// boundary RestartType ordinals 0..4 remain unchanged.</summary>
        public const byte INDIRECT_FREE_KICK = 5;
        /// <summary>[FIXED] Outside-area re-handling: ordinary handball direct free kick.</summary>
        public const byte DIRECT_FREE_KICK = 6;

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
        /// <summary>Restart kind ordinal — mirrors <c>TacticalDirector.BallPhysics.RestartType</c>
        /// (ThrowIn=1, GoalKick=2, Corner=3; KickOff/goals use GoalAwardedEvent instead).
        /// Codes 5/6 identify indirect/direct free kicks from the separate re-handling law path.</summary>
        public readonly byte RestartKind;
        /// <summary>Team byte awarded the restart: 0=Home, 1=Away.</summary>
        public readonly byte AwardedTeam;
        /// <summary>World-space position the ball is placed at (m).</summary>
        public readonly Vector3 Location;

        /// <summary>
        /// Constructs a <see cref="RestartAwardedEvent"/> with payload fields only.
        /// Header fields are left at zero — EventBus.Publish will overwrite them.
        /// </summary>
        public RestartAwardedEvent(byte restartKind, byte awardedTeam, Vector3 location)
        {
            eventTypeOrdinal    = 0;
            payloadVersion      = 0;
            _reserved           = 0;
            tick                = 0;
            subsystemOrdinal    = 0;
            intraPhaseDrawIndex = 0;
            RestartKind         = restartKind;
            AwardedTeam         = awardedTeam;
            Location            = location;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-13 | —      | Initial implementation. |
// | 1.1 | 2026-10-16 | — | Own-release re-handling: physical touch / law contact gate and canonical v27 restart history; focused regression coverage. |
#endregion
