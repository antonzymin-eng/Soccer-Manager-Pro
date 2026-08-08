// File:     src/decision-tree/PossessionState.cs
// Created:  2026-05-29
// Modified: 2026-06-11
// Author:   —
// Spec:     Decision Tree #8 §2.2.5, Code Standards #20
// Purpose:  Enum describing which team has possession in ABSOLUTE terms
//           (HOME_TEAM / AWAY_TEAM / CONTESTED) per §2.2.5. The §3.1.1.2 perspective
//           form (own team vs opponent) is derived per agent — see
//           DecisionContext.OpponentHasBall (AR-2 M-1).

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
// | 1.1     | 2026-06-11 | —      | Audit AR-2 L: header doc corrected — values are absolute (§2.2.5), not       |
// |         |            |        |   agent-perspective; the perspective claim fed the M-1 §3.4.6 defect.         |
#endregion
