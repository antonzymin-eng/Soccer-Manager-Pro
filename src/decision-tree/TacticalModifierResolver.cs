// File:     src/decision-tree/TacticalModifierResolver.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §3.4, Code Standards #20
// Purpose:  Translates TacticalContext instructions into utility multipliers applied
//           by UtilityScorer. Pure function: no side effects. §3.4.1.

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Computes the TacticalModifier for each action type given TacticalContext.
    /// Applied as a multiplicative post-factor in the utility formula (§3.4.1):
    ///   ScoredUtility = Base × AM × ContextM × TacticalModifier × (1 − Risk)
    /// Decision Tree #8 §3.4.
    /// </summary>
    internal static class TacticalModifierResolver
    {
        /// <summary>
        /// Returns the TacticalModifier for a given action type given the current
        /// TacticalContext and possession state. Always ≥ neutral for at least one mode.
        /// §3.4.2.
        /// </summary>
        /// <param name="opponentHasBall">
        /// DecisionContext.OpponentHasBall — true when the team OPPOSING the evaluated
        /// agent possesses (§3.1.1.2 perspective form). AR-2 M-1: this gate previously
        /// compared the absolute enum against AWAY_TEAM, which is "opponent" only for
        /// home agents — away agents received press urgency while their OWN team
        /// possessed and never while defending.
        /// </param>
        internal static float Resolve(
            ActionType type,
            in TacticalContext ctx,
            bool opponentHasBall,
            float intendedPassDistanceM)
        {
            float mod = 1.0f;

            mod *= ResolvePressingMode(type, ctx.Pressing);
            mod *= ResolvePassingStyle(type, ctx.Passing, intendedPassDistanceM);

            // Possession-phase PRESS urgency (§3.4.6): applies under OPPONENT possession.
            if (type == ActionType.PRESS && opponentHasBall)
                mod *= TacticalWeights.PressUrgencyFactor;

            return mod;
        }

        // ── PressingMode multipliers (§3.4.3) ────────────────────────────────

        private static float ResolvePressingMode(ActionType type, PressingMode pressing)
        {
            switch (pressing)
            {
                case PressingMode.HIGH:
                    switch (type)
                    {
                        case ActionType.PRESS:     return TacticalWeights.PressingHighPressMod;
                        case ActionType.INTERCEPT: return TacticalWeights.PressingHighInterceptMod;
                        case ActionType.HOLD:      return TacticalWeights.PressingHighHoldMod;
                        case ActionType.DRIBBLE:   return TacticalWeights.PressingHighDribbleMod;
                        default:                   return 1.0f;
                    }

                case PressingMode.LOW:
                    switch (type)
                    {
                        case ActionType.PRESS:     return TacticalWeights.PressingLowPressMod;
                        case ActionType.INTERCEPT: return TacticalWeights.PressingLowInterceptMod;
                        case ActionType.HOLD:      return TacticalWeights.PressingLowHoldMod;
                        default:                   return 1.0f;
                    }

                default: // MEDIUM
                    return 1.0f;
            }
        }

        // ── PassingStyle multipliers (§3.4.4) ────────────────────────────────

        private static float ResolvePassingStyle(
            ActionType type,
            PassingStyle passing,
            float intendedDistM)
        {
            switch (passing)
            {
                case PassingStyle.DIRECT:
                    switch (type)
                    {
                        case ActionType.PASS:
                            return intendedDistM >= TacticalWeights.PassLongShortThreshold
                                ? TacticalWeights.PassingDirectLongMod
                                : TacticalWeights.PassingDirectShortMod;
                        case ActionType.HOLD:    return TacticalWeights.PassingDirectHoldMod;
                        default:                 return 1.0f;
                    }

                case PassingStyle.SHORT:
                    switch (type)
                    {
                        case ActionType.PASS:
                            return intendedDistM >= TacticalWeights.PassLongShortThreshold
                                ? TacticalWeights.PassingShortLongMod
                                : TacticalWeights.PassingShortShortMod;
                        case ActionType.HOLD:    return TacticalWeights.PassingShortHoldMod;
                        default:                 return 1.0f;
                    }

                default: // MIXED
                    return 1.0f;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-06-11 | —      | Audit AR-2 M-1: §3.4.6 press urgency now keyed to OpponentHasBall            |
// |         |            |        |   (perspective form) instead of the absolute AWAY_TEAM literal, which        |
// |         |            |        |   inverted the urgency boost for away-team agents.                            |
#endregion
