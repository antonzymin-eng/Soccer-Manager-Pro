// File:     src/decision-tree/PossessionState.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §2.2.5, Code Standards #20
// Purpose:  Enum describing which team has possession from the perspective of a
//           given agent (HOME_TEAM / AWAY_TEAM / CONTESTED).

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Ball possession state as reported in MatchContext.
    /// Decision Tree #8 §2.2.5.
    /// </summary>
    public enum PossessionState
    {
        HOME_TEAM  = 0,
        AWAY_TEAM  = 1,
        CONTESTED  = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
