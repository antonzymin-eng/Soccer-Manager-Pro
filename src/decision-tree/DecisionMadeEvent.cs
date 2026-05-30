// File:     src/decision-tree/DecisionMadeEvent.cs
// Created:  2026-05-29
// Modified: 2026-05-30
// Author:   —
// Spec:     Decision Tree #8 §2.2.7, Event System #17 §3.2.1, Code Standards #20
// Purpose:  Tier C telemetry event published after each agent decision. Ordinal 0x11.
//           Immediate dispatch via CosmeticChannel; excluded from determinism digest. §2.2.7.

using System.Runtime.InteropServices;

using TacticalDirector.EventSystem;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Published by ActionSelector after each agent decision.
    /// Tier C: immediate dispatch via CosmeticChannel; excluded from digest. Ordinal 0x11.
    /// Decision Tree #8 §2.2.7 / Event System #17 Appendix A.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct DecisionMadeEvent : IEventC
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
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                          |
// | 1.1     | 2026-05-30 | —      | Stage 1: added IEventC, [StructLayout(Sequential)].              |
// |         |            |        | Ordinal 0x11. Event System #17 §3.2.1 wiring.                   |
#endregion
