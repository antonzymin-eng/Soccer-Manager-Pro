// File:     src/pressing-ai/PressAssignment.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.3–§3.6, Code Standards #20
// Purpose:  Per-agent role assignment struct produced by PressingAITick each tick.
//           Consumed by the match orchestrator to update agent movement targets.

using UnityEngine;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Per-agent output for one Pressing AI tick.
    /// Maps a single agent (EntityId) to its committed role and, when relevant,
    /// the world-space target position. Pressing AI #13 §3.3–§3.6.
    /// </summary>
    public struct PressAssignment
    {
        /// <summary>EntityId of the agent this assignment applies to.</summary>
        public int EntityId;

        /// <summary>Committed press role for this tick. §3.6.</summary>
        public PressRole Role;

        /// <summary>
        /// World-space (X, Y) movement target for this tick.
        /// Populated for PrimaryPress (projected interception point §3.3) and
        /// CoverShadow (shadow-lane position §3.5). Zero for HoldShape.
        /// </summary>
        public Vector2 TargetPosition;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
