// File:     src/defensive-ai/MarkAssignment.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §2.2.2, Code Standards #20
// Purpose:  Per-agent mark assignment for one tick. Written by DefensiveAITick;
//           read by the match orchestrator and Decision Tree #8 §3.1.7/§3.1.9.

using UnityEngine;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Per-HOLD_SHAPE-agent assignment produced each 10 Hz tick.
    /// Written by <see cref="DefensiveAITick"/>; consumed read-only by the orchestrator
    /// and Decision Tree #8. Defensive AI #14 §2.2.2.
    ///
    /// Sentinel values: <c>TargetEntityId = -1</c> indicates no specific opponent (ZONAL /
    /// COVER_GK_ZONE). <c>TargetPosition</c> is always set; for ZONAL it holds the agent's
    /// Positioning AI #12 formation slot.
    ///
    /// <c>OverriddenThisTick</c> is a per-tick transient flag reset to false at tick start
    /// (§3.13 Step 3b). <c>IsManuallyAssigned</c> is always false at Stage 0–1 (§7.2).
    /// </summary>
    public struct MarkAssignment
    {
        /// <summary>EntityId of the HOLD_SHAPE agent this assignment applies to.</summary>
        public int AgentEntityId;

        /// <summary>Mark mode for this agent this tick.</summary>
        public MarkMode Mode;

        /// <summary>
        /// Opponent EntityId being marked. -1 when no specific opponent is assigned
        /// (mode is ZONAL or COVER_GK_ZONE).
        /// </summary>
        public int TargetEntityId;

        /// <summary>
        /// World-space target position (X, Y) for this assignment.
        /// For ZONAL: agent's Positioning AI #12 formation slot.
        /// For MAN_MARK / INTERCEPT_RUNNER: opponent's current perceived position from #7.
        /// For COVER_GK_ZONE: center of the abandoned GK zone.
        /// </summary>
        public Vector2 TargetPosition;

        /// <summary>
        /// Tick index this assignment was produced. Any consumer reading a
        /// MarkAssignment with ValidThroughTick &lt; currentTick must treat it as stale.
        /// </summary>
        public int ValidThroughTick;

        /// <summary>
        /// Per-tick transient flag set by emergency overrides (§3.8, §3.9).
        /// Reset to false at tick start (§3.13 Step 3b) before Steps 4/4a run.
        /// </summary>
        public bool OverriddenThisTick;

        /// <summary>Stage 2+ manual assignment override. Always false at Stage 0–1 (§7.2).</summary>
        public bool IsManuallyAssigned;

        /// <summary>Returns a ZONAL assignment with no opponent and the given formation slot as target.</summary>
        public static MarkAssignment MakeZonal(int agentEntityId, Vector2 formationSlot, int tick)
        {
            return new MarkAssignment
            {
                AgentEntityId      = agentEntityId,
                Mode               = MarkMode.Zonal,
                TargetEntityId     = -1,
                TargetPosition     = formationSlot,
                ValidThroughTick   = tick,
                OverriddenThisTick = false,
                IsManuallyAssigned = false,
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
