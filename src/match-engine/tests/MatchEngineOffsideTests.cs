// File:     src/match-engine/tests/MatchEngineOffsideTests.cs
// Created:  2026-07-14
// Modified: 2026-07-14
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-flow-completion-design.md) §4, Code Standards #20
// Purpose:  Locks OffsideEvaluator's pure geometry (second-nearest-to-goal line; own-half exemption;
//           NaN-line gate) and the Resolve-phase offside application (violation reverts the touch and
//           awards a free kick; a non-pass reception never triggers it).

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineOffsideTests
    {
        private const ulong MatchSeed = 0x00C0FFEE5EEDBA11UL;

        // ── Pure OffsideEvaluator tests ───────────────────────────────────────────────

        private static AgentState[] MakeAgents(params float[] xs)
        {
            var agents = new AgentState[xs.Length];
            for (int i = 0; i < xs.Length; i++)
            {
                agents[i] = AgentState.CreateAtPosition(new Vector2(xs[i], 10f), new Vector2(1f, 0f));
            }
            return agents;
        }

        [Test]
        public void ComputeOffsideLineX_TeamZero_ReturnsSecondNearestToOwnGoal()
        {
            // Team 0 defends x=0: nearest-to-goal = smallest X. GK at 2 (nearest), then 5 (second),
            // then 20 — the line is the SECOND-nearest, i.e. 5, not the GK's 2.
            AgentState[] agents = MakeAgents(2f, 5f, 20f);
            int[] teamIds = { 0, 0, 0 };
            bool[] sentOff = { false, false, false };

            float line = OffsideEvaluator.ComputeOffsideLineX(agents, teamIds, sentOff, defendingTeam: 0, squadSize: 3);
            Assert.AreEqual(5f, line, 1e-4f);
        }

        [Test]
        public void ComputeOffsideLineX_TeamOne_ReturnsSecondNearestToOwnGoal_HighX()
        {
            // Team 1 defends x=LENGTH: nearest-to-goal = largest X.
            AgentState[] agents = MakeAgents(60f, 100f, 95f);
            int[] teamIds = { 1, 1, 1 };
            bool[] sentOff = { false, false, false };

            float line = OffsideEvaluator.ComputeOffsideLineX(agents, teamIds, sentOff, defendingTeam: 1, squadSize: 3);
            Assert.AreEqual(95f, line, 1e-4f, "Second-nearest to x=105 among {60,100,95} is 95.");
        }

        [Test]
        public void ComputeOffsideLineX_SentOffAndOpponents_Excluded()
        {
            // Agents at x = {2, 5, 20, 3}. Index 2 (x=20) is the OPPONENT team (1), excluded regardless
            // of sentOff. Index 3 (x=3) is team 0 but sent off — excluded too, even though it would
            // otherwise be the single nearest-to-goal defender. The active team-0 set is therefore
            // just indices 0 and 1 (x = {2, 5}), so nearest=2, second-nearest=5 — the sent-off agent's
            // exclusion changes which value is "nearest" vs "second-nearest" among the remaining two.
            AgentState[] agents = MakeAgents(2f, 5f, 20f, 3f);
            int[] teamIds = { 0, 0, 1, 0 };
            bool[] sentOff = { false, false, false, true };

            float line = OffsideEvaluator.ComputeOffsideLineX(agents, teamIds, sentOff, defendingTeam: 0, squadSize: 4);
            Assert.AreEqual(5f, line, 1e-4f, "Active team-0 set is {2,5} once index 2 (opponent) and index 3 (sent off) are excluded.");
        }

        [Test]
        public void ComputeOffsideLineX_FewerThanTwoActiveDefenders_ReturnsNaN_TeamZero()
        {
            // AR-6: an earlier draft of this method left the accumulator at its initial +Infinity
            // sentinel for this input, which IsOffside's `toucherX < offsideLineX` (team-1 attacker)
            // comparison satisfies for EVERY finite toucherX — i.e. it called a team-1 attacker
            // offside ALWAYS, backwards from "too few defenders to be offside". The explicit
            // active-count gate below fixes this: the result must be NaN, and a deep team-1 attacker
            // (clearly in the opponent/team-0 half) must NOT be called offside against it.
            AgentState[] agents = MakeAgents(2f);
            int[] teamIds = { 0 };
            bool[] sentOff = { false };

            float line = OffsideEvaluator.ComputeOffsideLineX(agents, teamIds, sentOff, defendingTeam: 0, squadSize: 1);
            Assert.IsTrue(float.IsNaN(line));
            Assert.IsFalse(OffsideEvaluator.IsOffside(toucherX: 10f, toucherTeam: 1, line),
                "Team-1 attacker deep in the opponent (team 0) half must not be offside vs a degenerate line.");
        }

        [Test]
        public void ComputeOffsideLineX_FewerThanTwoActiveDefenders_ReturnsNaN_TeamOne()
        {
            AgentState[] agents = MakeAgents(100f);
            int[] teamIds = { 1 };
            bool[] sentOff = { false };

            float line = OffsideEvaluator.ComputeOffsideLineX(agents, teamIds, sentOff, defendingTeam: 1, squadSize: 1);
            Assert.IsTrue(float.IsNaN(line));
            Assert.IsFalse(OffsideEvaluator.IsOffside(toucherX: 95f, toucherTeam: 0, line),
                "Team-0 attacker deep in the opponent (team 1) half must not be offside vs a degenerate line.");
        }

        [Test]
        public void IsOffside_NaNLine_NeverOffside()
        {
            Assert.IsFalse(OffsideEvaluator.IsOffside(90f, toucherTeam: 0, float.NaN));
        }

        [Test]
        public void IsOffside_OwnHalf_NeverOffside()
        {
            // Team 0 attacks +X; x=40 is still in the own half (< 52.5) even with an aggressive line.
            Assert.IsFalse(OffsideEvaluator.IsOffside(40f, toucherTeam: 0, offsideLineX: 30f));
        }

        [Test]
        public void IsOffside_BeyondLineInOpponentHalf_IsOffside()
        {
            Assert.IsTrue(OffsideEvaluator.IsOffside(90f, toucherTeam: 0, offsideLineX: 80f));
            Assert.IsFalse(OffsideEvaluator.IsOffside(70f, toucherTeam: 0, offsideLineX: 80f), "Not beyond the line.");
        }

        [Test]
        public void IsOffside_AwayTeamAttacksNegativeX()
        {
            // Team 1 attacks -X; opponent half is x < 52.5. Beyond the line means SMALLER x than the line.
            Assert.IsTrue(OffsideEvaluator.IsOffside(10f, toucherTeam: 1, offsideLineX: 20f));
            Assert.IsFalse(OffsideEvaluator.IsOffside(30f, toucherTeam: 1, offsideLineX: 20f));
        }

        // ── MatchEngine integration (direct seam — design note §4 test plan) ──────────

        [Test]
        public void EvaluateAndApplyOffside_Violation_AwardsFreeKick_AndClearsPossession()
        {
            var engine = new MatchEngine(MatchSeed);

            // Push every away (team 1) agent deep into their own half so the offside line sits well
            // behind the toucher, then place the toucher deep in the away half.
            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                int away = MatchEngineConstants.PLAYERS_PER_TEAM + k;
                engine.TestOnly_SetAgent(away, AgentState.CreateAtPosition(new Vector2(60f, 10f + k), new Vector2(-1f, 0f)));
            }
            int toucher = 1; // a home outfield agent
            engine.TestOnly_SetAgent(toucher, AgentState.CreateAtPosition(new Vector2(95f, 34f), new Vector2(1f, 0f)));

            // OffsideCalledEvent's registered producer phase is Resolve; this direct seam call
            // bypasses RunTick's normal phase progression, so the phase must be set explicitly.
            EventBus.BeginPhase(PhaseId.Resolve);
            bool violation = engine.TestOnly_EvaluateAndApplyOffside(toucher);

            Assert.IsTrue(violation);
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId);
            BallState ball = engine.TestOnly_BallSnapshot;
            Assert.AreEqual(95f, ball.Position.x, 1e-4f, "Free kick placed at the toucher's position.");
            Assert.AreEqual(34f, ball.Position.y, 1e-4f);
        }

        [Test]
        public void EvaluateAndApplyOffside_OnsideReceiver_NoViolation()
        {
            var engine = new MatchEngine(MatchSeed);
            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                int away = MatchEngineConstants.PLAYERS_PER_TEAM + k;
                engine.TestOnly_SetAgent(away, AgentState.CreateAtPosition(new Vector2(60f, 10f + k), new Vector2(-1f, 0f)));
            }
            // Toucher level with (behind) the deepest away defender's line -> onside.
            int toucher = 1;
            engine.TestOnly_SetAgent(toucher, AgentState.CreateAtPosition(new Vector2(55f, 34f), new Vector2(1f, 0f)));

            bool violation = engine.TestOnly_EvaluateAndApplyOffside(toucher);
            Assert.IsFalse(violation);
        }

        [Test]
        public void EvaluateAndApplyOffside_DifferentTeamReceiver_NotSameTeamPassGuardIsCallerSide()
        {
            // The "same-team pass reception, not an interception" gate lives in RunFirstTouch's
            // caller code (comparing _teamIds[_lastHolderAgentId] vs _teamIds[newHolder]), not inside
            // EvaluateAndApplyOffside itself — EvaluateAndApplyOffside always evaluates the geometry
            // for whatever toucher it is given. This directly exercises that an AWAY toucher (attacks
            // -X) deep in the HOME half is correctly evaluated against the HOME defensive line, i.e.
            // the geometry is symmetric per attacking direction, not just team-0-shaped.
            var engine = new MatchEngine(MatchSeed);
            for (int k = 0; k < MatchEngineConstants.PLAYERS_PER_TEAM; k++)
            {
                int home = k;
                engine.TestOnly_SetAgent(home, AgentState.CreateAtPosition(new Vector2(45f, 10f + k), new Vector2(1f, 0f)));
            }
            int awayToucher = MatchEngineConstants.PLAYERS_PER_TEAM + 1;
            engine.TestOnly_SetAgent(awayToucher, AgentState.CreateAtPosition(new Vector2(10f, 34f), new Vector2(-1f, 0f)));

            // OffsideCalledEvent's registered producer phase is Resolve; this direct seam call
            // bypasses RunTick's normal phase progression, so the phase must be set explicitly.
            EventBus.BeginPhase(PhaseId.Resolve);
            bool violation = engine.TestOnly_EvaluateAndApplyOffside(awayToucher);
            Assert.IsTrue(violation, "Away toucher at x=10, home line's second-nearest ~45 -> beyond it toward x=0.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-14 | —      | Initial offside suite: OffsideEvaluator pure-function locks    |
// |         |            |        |   (second-nearest-to-goal both attack directions, sent-off/    |
// |         |            |        |   opponent exclusion, NaN-line degenerate input, own-half      |
// |         |            |        |   exemption both directions) + MatchEngine integration via the |
// |         |            |        |   direct TestOnly seam (violation applies the free kick;       |
// |         |            |        |   onside receiver is a no-op).                                 |
#endregion
