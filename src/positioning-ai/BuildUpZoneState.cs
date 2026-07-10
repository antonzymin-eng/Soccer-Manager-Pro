// File:     src/positioning-ai/BuildUpZoneState.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Build-Up Structures #24 §2.2.2, Code Standards #20
// Purpose:  Per-team persistent build-up state: the committed hysteresis zone and the post-regain
//           suppression countdown. Serialized per FR-BU-011 at wiring (#24 Appendix B order).

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Per-team persistent build-up state (#24 §2.2.2). Zero-init (<see cref="CommittedZone"/> =
    /// OwnThird, <see cref="SuppressTicksRemaining"/> = 0) is a valid kickoff-adjacent default; boot
    /// seeds the zone from actual ball X anyway. Restore validates per F2 (SuppressTicksRemaining ∈
    /// [0, REGAIN_SUPPRESS_TICKS]; CommittedZone a defined <see cref="BuildUpZone"/> ordinal).
    /// </summary>
    public struct BuildUpZoneState
    {
        /// <summary>The committed hysteresis zone (#24 §3.1, FM-BU-01).</summary>
        public BuildUpZone CommittedZone;

        /// <summary>Post-regain suppression countdown (heartbeats); 0 = window closed (#24 §3.3).</summary>
        public int SuppressTicksRemaining;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial implementation (#24 T0 scaffolding). |
#endregion
