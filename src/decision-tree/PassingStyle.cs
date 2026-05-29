// File:     src/decision-tree/PassingStyle.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §2.2.6, Code Standards #20
// Purpose:  Enum for team passing instruction. Modifies long/short PASS utility
//           and HOLD utility (§3.4.4). Stage 0 default: MIXED for both teams.

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Team passing style instruction from the tactical layer.
    /// Stage 0: both teams use MIXED (§2.2.6 TacticalContext.Stage0Default).
    /// Decision Tree #8 §2.2.6.
    /// </summary>
    public enum PassingStyle
    {
        DIRECT = 0,
        MIXED  = 1,
        SHORT  = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
