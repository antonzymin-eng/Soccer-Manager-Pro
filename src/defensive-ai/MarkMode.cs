// File:     src/defensive-ai/MarkMode.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §2.2, §3.3, FR-DA-011, Code Standards #20
// Purpose:  Enum declaring the four mark-mode values used by MarkAssignment.
//           Exactly four values per FR-DA-011.

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Mark mode assigned to each HOLD_SHAPE pool agent per tick.
    /// Exactly four values per FR-DA-011. Defensive AI #14 §2.2 / §3.3.
    /// </summary>
    public enum MarkMode : byte
    {
        /// <summary>Agent holds its #12 formation slot; no specific opponent assigned.</summary>
        Zonal = 0,

        /// <summary>Agent tracks a specific nearby opponent within MAN_MARK_CANDIDATE_RADIUS_M.</summary>
        ManMark = 1,

        /// <summary>Agent intercepts a running attacker threatening the own half.</summary>
        InterceptRunner = 2,

        /// <summary>Emergency mode: agent covers the abandoned GK zone when GK is out of position.</summary>
        CoverGkZone = 3,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
