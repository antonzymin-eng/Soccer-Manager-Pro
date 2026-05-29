// File:     src/decision-tree/FieldZone.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §2.2.5, Code Standards #20
// Purpose:  Enum partitioning the pitch into three longitudinal zones used by the
//           utility zone-modifier table (§3.2.1.3).

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Three-zone pitch partition. Zone boundaries defined in §3.2.1.3:
    /// DEFENSIVE = x ∈ [0, 35m], MIDFIELD = x ∈ (35, 65m], ATTACKING = x ∈ (65, 105m].
    /// Decision Tree #8 §2.2.5.
    /// </summary>
    public enum FieldZone
    {
        DEFENSIVE  = 0,
        MIDFIELD   = 1,
        ATTACKING  = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
