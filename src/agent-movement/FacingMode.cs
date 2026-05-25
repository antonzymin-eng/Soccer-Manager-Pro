// File:     src/agent-movement/FacingMode.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     Agent Movement #2 §3.3.4, Code Standards #20
// Purpose:  Controls how the agent's facing direction is updated each frame.

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// How the agent's facing direction is updated each frame (§3.3.4).
    /// </summary>
    public enum FacingMode
    {
        /// <summary>Facing auto-aligns to the movement velocity direction.</summary>
        AUTO_ALIGN,

        /// <summary>Facing locks to an explicit target while body moves in commanded direction.</summary>
        TARGET_LOCK
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                              |
// | 1.0     | 2026-05-25 | —      | Extracted from AgentMovementState.cs (one-public-type-per-file, FR-CS rule).       |
#endregion
