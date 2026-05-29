// File:     src/decision-tree/DecisionMadeEvent.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §2.2.7, Code Standards #20
// Purpose:  Event struct published after each successful decision. Stage 0: consumed
//           by EventBusStub (no-op). Stage 1+: wired to Event System #17.

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Published by ActionSelector after each agent decision.
    /// Stage 0: consumed by EventBusStub (no-op). Stage 1+: wired to Event System #17.
    /// Decision Tree #8 §2.2.7.
    /// </summary>
    public readonly struct DecisionMadeEvent
    {
        /// <summary>AgentId of the deciding agent.</summary>
        public readonly int AgentId;

        /// <summary>The action that was selected.</summary>
        public readonly AgentAction SelectedAction;

        /// <summary>EffectiveUtility of the selected action (after composure noise).</summary>
        public readonly float UtilityScore;

        /// <summary>Number of candidates evaluated before selection.</summary>
        public readonly int CandidateCount;

        /// <summary>Heartbeat tick at which the decision was made.</summary>
        public readonly int HeartbeatTick;

        /// <summary>True when two or more options had equal EffectiveUtility within TIEBREAK_EPSILON.</summary>
        public readonly bool TiebreakerApplied;

        /// <summary>True when option generation returned no candidates and HOLD was injected as fallback.</summary>
        public readonly bool FallbackToHold;

        public DecisionMadeEvent(
            int agentId,
            AgentAction selectedAction,
            float utilityScore,
            int candidateCount,
            int heartbeatTick,
            bool tiebreakerApplied,
            bool fallbackToHold)
        {
            AgentId          = agentId;
            SelectedAction   = selectedAction;
            UtilityScore     = utilityScore;
            CandidateCount   = candidateCount;
            HeartbeatTick    = heartbeatTick;
            TiebreakerApplied = tiebreakerApplied;
            FallbackToHold   = fallbackToHold;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
