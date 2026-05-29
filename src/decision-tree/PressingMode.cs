// File:     src/decision-tree/PressingMode.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §2.2.6, Code Standards #20
// Purpose:  Enum for team pressing instruction. Governs PRESS/INTERCEPT/HOLD utility
//           multipliers (§3.4.3). Stage 0 default: MEDIUM for both teams.

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Team pressing intensity instruction from the tactical layer.
    /// Stage 0: both teams use MEDIUM (§2.2.6 TacticalContext.Stage0Default).
    /// Decision Tree #8 §2.2.6.
    /// </summary>
    public enum PressingMode
    {
        HIGH   = 0,
        MEDIUM = 1,
        LOW    = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
