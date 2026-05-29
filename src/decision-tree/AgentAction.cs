// File:     src/decision-tree/AgentAction.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §2.2.2, Code Standards #20
// Purpose:  The selected action output of the Decision Tree pipeline. Readonly struct
//           passed by value to ActionDispatcher and published in DecisionMadeEvent.
//           PassParams/ShotParams carry pre-populated request structs; AgentId and
//           FrameNumber are filled by ActionDispatcher at dispatch time (§3.5.2/3).

using UnityEngine;
using TacticalDirector.PassMechanics;
using TacticalDirector.ShotMechanics;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// The single action selected by SelectAction() for one agent at one heartbeat.
    /// Consumed by ActionDispatcher (Step 6) and published via DecisionMadeEvent.
    /// Decision Tree #8 §2.2.2.
    /// </summary>
    public readonly struct AgentAction
    {
        /// <summary>AgentId of the agent who made this decision.</summary>
        public readonly int AgentId;

        /// <summary>Selected action type.</summary>
        public readonly ActionType Type;

        /// <summary>Target agent ID (PASS: teammate, PRESS: opponent); -1 if not applicable.</summary>
        public readonly int TargetAgentId;

        /// <summary>World-space XY target position for positional actions.</summary>
        public readonly Vector2 TargetPosition;

        /// <summary>
        /// Pre-populated pass request (valid when Type == PASS).
        /// ActionDispatcher fills AgentId and FrameNumber before calling PassExecutor (§3.5.2).
        /// </summary>
        public readonly PassRequest PassParams;

        /// <summary>
        /// Pre-populated shot request (valid when Type == SHOOT).
        /// ActionDispatcher fills AgentId and FrameNumber before calling ShotExecutor (§3.5.3).
        /// </summary>
        public readonly ShotRequest ShotParams;

        /// <summary>EffectiveUtility of the selected option (after composure noise injection). §3.3.</summary>
        public readonly float UtilityScore;

        /// <summary>10Hz heartbeat tick at which this action was decided.</summary>
        public readonly int HeartbeatTick;

        public AgentAction(
            int agentId,
            ActionType type,
            int targetAgentId,
            Vector2 targetPosition,
            PassRequest passParams,
            ShotRequest shotParams,
            float utilityScore,
            int heartbeatTick)
        {
            AgentId       = agentId;
            Type          = type;
            TargetAgentId = targetAgentId;
            TargetPosition = targetPosition;
            PassParams    = passParams;
            ShotParams    = shotParams;
            UtilityScore  = utilityScore;
            HeartbeatTick = heartbeatTick;
        }

        /// <summary>HOLD fallback with minimum utility. Used when option generation fails.</summary>
        public static AgentAction HoldFallback(int agentId, int heartbeatTick)
        {
            return new AgentAction(agentId, ActionType.HOLD, -1, Vector2.zero,
                default, default, UtilityWeights.UTILITY_FLOOR, heartbeatTick);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
