// File:     src/decision-tree/MatchPhase.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §2.2.5, Code Standards #20
// Purpose:  Enum for the current phase of the match (open play vs. set pieces).

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Current phase of the match. Gates which action types can be generated.
    /// Stage 0: only OPEN_PLAY actions are permitted (§3.1.3.1, §3.1.4.1, §3.1.8.1).
    /// Decision Tree #8 §2.2.5.
    /// </summary>
    public enum MatchPhase
    {
        OPEN_PLAY       = 0,
        SET_PIECE_HOME  = 1,
        SET_PIECE_AWAY  = 2,
        KICK_OFF        = 3
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
