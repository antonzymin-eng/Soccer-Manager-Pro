// File: src/positioning-ai/RoleId.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI §3.2, §6.1
// Purpose: 13-role taxonomy driving the 13×4 pull-factor table in PositioningAIConstants.

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Thirteen positional roles used as the row index in the 13×4 pull-factor table.
    /// Values 0–12 are stable; do not reorder.
    /// </summary>
    public enum RoleId : byte
    {
        GK = 0,   // Goalkeeper
        LB = 1,   // Left Back
        CB = 2,   // Centre Back (both CB1 and CB2 use this row)
        RB = 3,   // Right Back
        LM = 4,   // Left Midfielder (wide)
        CM = 5,   // Central Midfielder
        RM = 6,   // Right Midfielder (wide)
        DM = 7,   // Defensive Midfielder
        LW = 8,   // Left Winger (attacking)
        CF = 9,   // Centre Forward
        RW = 10,  // Right Winger (attacking)
        AM = 11,  // Attacking Midfielder
        ST = 12   // Striker (lone forward)
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
