// File: src/positioning-ai/Phase.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI §3.0
// Purpose: Tactical phase enum used by positioning AI to determine formation compactness and pull.

namespace TacticalDirector.PositioningAI
{
    /// <summary>Local four-value tactical phase; computed from possession state and ball velocity.</summary>
    public enum Phase : byte
    {
        /// <summary>Own team in possession.</summary>
        InPoss = 0,

        /// <summary>Opponent team in possession.</summary>
        OutOfPoss = 1,

        /// <summary>Ball loose, moving toward opponent goal (positive X velocity).</summary>
        TransToAtk = 2,

        /// <summary>Ball loose, moving toward own goal (negative X velocity).</summary>
        TransToDef = 3
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
