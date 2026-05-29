// File:     src/decision-tree/DtState.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §3.7.1, Code Standards #20
// Purpose:  Four-state enum for the Decision Tree per-agent state machine (§3.7).

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Per-agent Decision Tree state machine states.
    /// Transitions defined in §3.7.2. Decision Tree #8 §3.7.1.
    /// </summary>
    public enum DtState
    {
        IDLE        = 0,
        EVALUATING  = 1,
        EXECUTING   = 2,
        INTERRUPTED = 3
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
