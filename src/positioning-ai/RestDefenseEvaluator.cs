// File:     src/positioning-ai/RestDefenseEvaluator.cs
// Created:  2026-07-07
// Modified: 2026-07-07
// Author:   —
// Spec:     Positioning AI #12 §3.5, new §7.13 (cheap-item addition), Code Standards #20
// Purpose:  Pure static: counts outfield agents behind the rest-defense coverage line while the
//           team is in possession. Consumed by Decision Tree (#8) as a risk-dampening signal —
//           the "insurance policy" structure ("rest-defence"/"restverteidigung") a team leaves
//           behind while attacking, so a counter-attack loss finds cover rather than open space.

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Rest-defense coverage check (§3.5, new §7.13). Pure static; no side effects.
    /// Only meaningful while the team is IN_POSSESSION — out of possession every agent is already
    /// defensively engaged, so the concept does not apply and <see cref="Evaluate"/> returns
    /// <c>true</c> (no dampening) for any other phase.
    /// </summary>
    public static class RestDefenseEvaluator
    {
        /// <summary>
        /// Returns <c>true</c> when at least <see cref="PositioningAIConstants.REST_DEFENSE_MIN_COUNT"/>
        /// active outfield agents sit at or behind <see cref="PositioningAIConstants.REST_DEFENSE_DEPTH_M"/>
        /// (own-goal-relative, canonical attack-toward-+X frame). The goalkeeper is excluded — it is
        /// not part of the "insurance policy" structure this check models.
        /// </summary>
        /// <param name="snapshot">Current tick's positioning perception snapshot.</param>
        /// <param name="phase">The team's committed phase this tick (from PhaseClassifier).</param>
        public static bool Evaluate(PositioningPerceptionSnapshot snapshot, Phase phase)
        {
            if (phase != Phase.InPoss)
            {
                return true;
            }

            int count = 0;
            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly AgentPositioningData a = ref snapshot.Agents[i];
                if (!a.IsActive || a.IsGoalkeeper)
                {
                    continue;
                }
                if (a.Position.x <= PositioningAIConstants.REST_DEFENSE_DEPTH_M)
                {
                    count++;
                }
            }
            return count >= PositioningAIConstants.REST_DEFENSE_MIN_COUNT;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-07-07 | —      | Initial implementation (cheap-item addition). |
#endregion
