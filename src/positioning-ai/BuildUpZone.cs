// File:     src/positioning-ai/BuildUpZone.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Build-Up Structures #24 §2.2.2, §3.1 (FM-BU-01), Code Standards #20
// Purpose:  Team-relative ball-progression zone for the build-up overlay (thirds of the pitch,
//           from the team's own goal line, attack toward +X).

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Ball-progression zone (#24 §3.1), classified from team-relative ball X with hysteresis.
    /// The zero value <see cref="OwnThird"/> is a valid kickoff-adjacent default, but state is
    /// seeded from actual ball X at boot anyway (#24 §2.2.2).
    /// ORDINAL STABILITY: byte-backed, APPEND-only — the ordinal is serialized in
    /// <c>BuildUpZoneState.CommittedZone</c> (FR-BU-011); append at the end only.
    /// </summary>
    public enum BuildUpZone : byte
    {
        /// <summary>Team-relative x &lt; 35.0 m (BUILDUP_ZONE_THRESHOLD_1_M).</summary>
        OwnThird = 0,

        /// <summary>35.0 m ≤ x &lt; 70.0 m (BUILDUP_ZONE_THRESHOLD_2_M).</summary>
        MiddleThird = 1,

        /// <summary>x ≥ 70.0 m. The overlay is all-zero here by rule (FR-BU-004).</summary>
        FinalThird = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial implementation (#24 T0 scaffolding). |
#endregion
