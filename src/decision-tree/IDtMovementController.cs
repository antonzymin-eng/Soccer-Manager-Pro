// File:     src/decision-tree/IDtMovementController.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §3.5 (XC-3.5-10), Code Standards #20
// Purpose:  Dispatch boundary to Agent Movement #2. Provisional entry point per
//           §3.5 cross-reference XC-3.5-10: MovementController.SubmitCommand().
//           Stage 0: injected into ActionDispatcher. Stage 1+: implement against
//           AgentMovementSystem or a dedicated movement command queue.

using TacticalDirector.AgentMovement;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Dispatch boundary between the Decision Tree and Agent Movement #2.
    /// Provisional interface matching XC-3.5-10: MovementController.SubmitCommand().
    /// Stage 0: one implementation required per game loop integration point.
    /// Decision Tree #8 §3.5 (XC-3.5-10).
    /// </summary>
    public interface IDtMovementController
    {
        /// <summary>
        /// Submit a movement command for a given agent.
        /// Called once per heartbeat for each agent whose selected action is a
        /// movement action (DRIBBLE, HOLD, MOVE_TO_POSITION, PRESS, INTERCEPT).
        /// </summary>
        void SubmitCommand(int agentId, MovementCommand command);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
