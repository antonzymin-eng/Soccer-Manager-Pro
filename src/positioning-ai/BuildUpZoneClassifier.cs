// File:     src/positioning-ai/BuildUpZoneClassifier.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Build-Up Structures #24 §3.1 (FM-BU-01), §3.3 (FM-BU-03), FR-BU-002/003/006, Code Standards #20
// Purpose:  Pure static build-up zone classification with committed-zone-expansion hysteresis, plus
//           the post-regain suppression-window arithmetic. No side effects, no RNG (FR-BU-013).

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Pure static build-up classifier (#24 §3.1/§3.3). Zone X is TEAM-RELATIVE (from the team's own
    /// goal line, attack toward +X — FR-BU-002): the same physical ball position classifies
    /// mirror-symmetrically for the two teams; the caller performs the frame mirror.
    /// </summary>
    public static class BuildUpZoneClassifier
    {
        /// <summary>
        /// Raw (hysteresis-free) zone from team-relative ball X (#24 §3.1):
        /// x &lt; T1 → OwnThird; x &lt; T2 → MiddleThird; else FinalThird.
        /// </summary>
        /// <param name="teamRelativeBallX">Ball X in the team's canonical attack-toward-+X frame (m).</param>
        public static BuildUpZone RawZone(float teamRelativeBallX)
        {
            if (teamRelativeBallX < PositioningAIConstants.BUILDUP_ZONE_THRESHOLD_1_M)
            {
                return BuildUpZone.OwnThird;
            }
            if (teamRelativeBallX < PositioningAIConstants.BUILDUP_ZONE_THRESHOLD_2_M)
            {
                return BuildUpZone.MiddleThird;
            }
            return BuildUpZone.FinalThird;
        }

        /// <summary>
        /// Committed-zone-expansion hysteresis (#24 §3.1, PASS-1 M-2 formulation): the committed
        /// zone's own interval expands by <see cref="PositioningAIConstants.BUILDUP_ZONE_HYSTERESIS_M"/>
        /// at each of its boundaries; inside the expanded interval the committed zone holds,
        /// otherwise the raw zone commits (well-defined for non-adjacent long-ball jumps).
        /// F1: a non-finite x holds the committed zone (decay-free no-op — never classify from NaN).
        /// Worked example (§3.1): committed OwnThird claims [0, 37.0) — 35.5 holds, 37.2 commits
        /// MiddleThird; MiddleThird claims [33.0, 72.0) — 34.1 holds, 32.9 commits OwnThird.
        /// </summary>
        /// <param name="committed">The zone committed on the previous heartbeat.</param>
        /// <param name="teamRelativeBallX">Ball X in the team's attack-toward-+X frame (m, [0, 105]).</param>
        public static BuildUpZone Classify(BuildUpZone committed, float teamRelativeBallX)
        {
            if (float.IsNaN(teamRelativeBallX) || float.IsInfinity(teamRelativeBallX))
            {
                return committed; // F1.
            }

            float h = PositioningAIConstants.BUILDUP_ZONE_HYSTERESIS_M;
            float t1 = PositioningAIConstants.BUILDUP_ZONE_THRESHOLD_1_M;
            float t2 = PositioningAIConstants.BUILDUP_ZONE_THRESHOLD_2_M;

            switch (committed)
            {
                case BuildUpZone.OwnThird:
                    if (teamRelativeBallX < t1 + h)
                    {
                        return committed;
                    }
                    break;
                case BuildUpZone.MiddleThird:
                    if (teamRelativeBallX >= t1 - h && teamRelativeBallX < t2 + h)
                    {
                        return committed;
                    }
                    break;
                case BuildUpZone.FinalThird:
                    if (teamRelativeBallX >= t2 - h)
                    {
                        return committed;
                    }
                    break;
                default:
                    break; // Undefined committed byte: fall through to the raw commit (restore seams refuse it earlier, F2).
            }
            return RawZone(teamRelativeBallX);
        }

        /// <summary>
        /// Arms the post-regain suppression window on a TEAM-LEVEL regain (#24 §3.3, FM-BU-03 /
        /// FR-BU-006): the settled possessing team transitioned opponent → this team. The CALLER
        /// derives that transition from the possession-changed signal's holder ids — intra-team
        /// possessor changes MUST NOT reach this method (the raw event fires on teammate receptions
        /// too, PASS-1 M-1). Arms only when <paramref name="transitionWon"/> ∈
        /// {CounterAttack, CounterPress}; {HoldShape, Regroup} opens no window.
        /// </summary>
        /// <param name="state">The team's state entering this heartbeat.</param>
        /// <param name="transitionWon">The team's <c>TeamTactic.TransitionWon</c> dial.</param>
        public static BuildUpZoneState ArmOnTeamRegain(in BuildUpZoneState state, TransitionPlan transitionWon)
        {
            BuildUpZoneState next = state;
            bool arms = transitionWon == TransitionPlan.CounterAttack
                     || transitionWon == TransitionPlan.CounterPress;
            next.SuppressTicksRemaining = arms ? PositioningAIConstants.REGAIN_SUPPRESS_TICKS : 0;
            return next;
        }

        /// <summary>Per-heartbeat suppression decrement (#24 §3.3): max(0, remaining − 1).</summary>
        /// <param name="state">The team's state entering this heartbeat.</param>
        public static BuildUpZoneState TickSuppression(in BuildUpZoneState state)
        {
            BuildUpZoneState next = state;
            next.SuppressTicksRemaining = state.SuppressTicksRemaining > 0
                ? state.SuppressTicksRemaining - 1
                : 0;
            return next;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial implementation (#24 T0 scaffolding): FM-BU-01 hysteresis + FM-BU-03 window. |
#endregion
