// File:     src/match-engine/OffsideEvaluator.cs
// Created:  2026-07-14
// Modified: 2026-07-14
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-flow-completion-design.md) §4, Code Standards #20
// Purpose:  Pure Stage-0 offside evaluation from live agent positions. Deliberately NOT the full
//           Laws-of-the-Game freeze-at-the-pass model — see the design note §4 for the documented
//           reception-time approximation and its rationale (no Referee System spec exists yet).

using System;

using TacticalDirector.AgentMovement;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Computes the defending team's offside line from live agent positions (NOT the Positioning AI's
    /// normalised <c>DefensiveLineDepth</c> formation-target value, which is a target anchor, not a
    /// scan of actual defender positions — using it here would misrepresent the law) and evaluates
    /// whether a receiving agent is offside at the moment of reception (Stage-0 approximation; the
    /// true law freezes positions at the instant the ball is played, not received — design note §4).
    /// Pure and allocation-free.
    /// </summary>
    public static class OffsideEvaluator
    {
        /// <summary>
        /// Second-nearest-to-own-goal X among <paramref name="defendingTeam"/>'s active (non-sent-off)
        /// agents — the Laws' "second-last opponent" (the goalkeeper is usually nearest, making this
        /// the last outfield defender; no GK special-case is needed). Team 0 defends x=0 (nearest-to-
        /// own-goal = smallest X); team 1 defends x=PITCH_LENGTH_M (nearest-to-own-goal = largest X) —
        /// the project's fixed attack-direction convention (mirrors <c>MatchEngine.MirrorPitchIfAway</c>).
        /// Returns <see cref="float.NaN"/> if fewer than two active agents exist on the defending team
        /// (a practically unreachable Stage-0 degenerate input — two-plus dismissals on one team).
        /// AR-6 finding: the ±Infinity sentinel the accumulator initialises to does NOT naturally
        /// resolve this case — e.g. team 0 down to one defender leaves the accumulator at
        /// <c>+Infinity</c>, and <see cref="IsOffside"/>'s comparison for a team-1 attacker is
        /// <c>toucherX &lt; offsideLineX</c>, which is true for EVERY finite <c>toucherX</c> against
        /// <c>+Infinity</c> — i.e. it would call a team-1 attacker offside ALWAYS, backwards from the
        /// intended "too few defenders to be offside" rule. An explicit active-agent count below
        /// gates the NaN return so <see cref="IsOffside"/>'s existing NaN guard handles it correctly.
        /// </summary>
        /// <param name="agents">All agents' state (position read from <c>Position.x</c>), indexed by roster id.</param>
        /// <param name="teamIds">All agents' team ids (0/1), indexed by roster id.</param>
        /// <param name="isSentOff">All agents' sent-off flags, indexed by roster id.</param>
        /// <param name="defendingTeam">The team whose offside line to compute (0/1).</param>
        /// <param name="squadSize">Number of roster entries in the three spans above.</param>
        public static float ComputeOffsideLineX(
            ReadOnlySpan<AgentState> agents,
            ReadOnlySpan<int> teamIds,
            ReadOnlySpan<bool> isSentOff,
            int defendingTeam,
            int squadSize)
        {
            bool defendsHomeGoal = defendingTeam == 0;
            float nearest       = defendsHomeGoal ? float.PositiveInfinity : float.NegativeInfinity;
            float secondNearest = nearest;
            int   activeCount   = 0;

            for (int i = 0; i < squadSize; i++)
            {
                if (teamIds[i] != defendingTeam || isSentOff[i])
                {
                    continue;
                }
                activeCount++;

                float x = agents[i].Position.x;
                if (defendsHomeGoal)
                {
                    if (x < nearest)
                    {
                        secondNearest = nearest;
                        nearest       = x;
                    }
                    else if (x < secondNearest)
                    {
                        secondNearest = x;
                    }
                }
                else
                {
                    if (x > nearest)
                    {
                        secondNearest = nearest;
                        nearest       = x;
                    }
                    else if (x > secondNearest)
                    {
                        secondNearest = x;
                    }
                }
            }

            return activeCount < 2 ? float.NaN : secondNearest;
        }

        /// <summary>
        /// True if a reception at <paramref name="toucherX"/> by a <paramref name="toucherTeam"/>
        /// agent is offside against <paramref name="offsideLineX"/> (from
        /// <see cref="ComputeOffsideLineX"/> for the OPPONENT of <paramref name="toucherTeam"/>).
        /// Never offside in the toucher's own half (Laws of the Game), and never offside when
        /// <paramref name="offsideLineX"/> is NaN (degenerate input — see
        /// <see cref="ComputeOffsideLineX"/>). Dropped nuance (documented, Stage-0): the "or level
        /// with the ball" alternative offside line, and freeze-at-pass-time positions.
        /// </summary>
        public static bool IsOffside(float toucherX, int toucherTeam, float offsideLineX)
        {
            if (float.IsNaN(offsideLineX))
            {
                return false;
            }

            bool  attacksPositiveX = toucherTeam == 0;
            float halfwayX         = MatchEngineConstants.PITCH_LENGTH_M * 0.5f;
            bool  inOpponentHalf   = attacksPositiveX ? toucherX > halfwayX : toucherX < halfwayX;
            if (!inOpponentHalf)
            {
                return false;
            }

            return attacksPositiveX ? toucherX > offsideLineX : toucherX < offsideLineX;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-14 | —      | Initial implementation. |
#endregion
