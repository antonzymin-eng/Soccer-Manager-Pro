// File:     src/decision-tree/ActionType.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §2.2.1, Code Standards #20
// Purpose:  Enum of all Stage 0 action types. Ordinal values are stable; used
//           as hash inputs by the composure noise function (§3.3.3). Do not reorder.

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// All action types available in the Stage 0 Decision Tree pipeline.
    /// Ordinals (PASS=0 … INTERCEPT=6) are canonical hash inputs — do not renumber.
    /// Decision Tree #8 §2.2.1.
    /// </summary>
    public enum ActionType
    {
        PASS            = 0,
        SHOOT           = 1,
        DRIBBLE         = 2,
        HOLD            = 3,
        MOVE_TO_POSITION = 4,
        PRESS           = 5,
        INTERCEPT       = 6
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
