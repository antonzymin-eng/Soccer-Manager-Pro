// File:     src/match-engine/tests/MatchEngineRestartTests.cs
// Created:  2026-07-14
// Modified: 2026-07-14
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-flow-completion-design.md) §5, Code Standards #20
// Purpose:  Locks RestartResolver's pure position/awarded-team resolution and the Resolve-phase
//           application of throw-in/corner/goal-kick restarts (previously ignored entirely).

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineRestartTests
    {
        private const ulong MatchSeed = 0x00C0FFEE5EEDBA11UL;

        // ── Pure RestartResolver tests ────────────────────────────────────────────────

        [Test]
        public void Resolve_AllThreeTypes_AwardTheTeamThatDidNotTouchLast()
        {
            Vector2 anyPos = new Vector2(10f, 10f);
            Assert.AreEqual(1, RestartResolver.Resolve(RestartType.ThrowIn, anyPos, lastTouchTeam: 0).awardedTeam);
            Assert.AreEqual(0, RestartResolver.Resolve(RestartType.ThrowIn, anyPos, lastTouchTeam: 1).awardedTeam);
            Assert.AreEqual(1, RestartResolver.Resolve(RestartType.Corner, anyPos, lastTouchTeam: 0).awardedTeam);
            Assert.AreEqual(0, RestartResolver.Resolve(RestartType.GoalKick, anyPos, lastTouchTeam: 1).awardedTeam);
        }

        [Test]
        public void Resolve_ThrowIn_ClampsXAndSnapsYToNearerTouchline()
        {
            Vector2 pos = RestartResolver.Resolve(RestartType.ThrowIn, new Vector2(52.5f, -0.5f), 0).position;
            Assert.AreEqual(52.5f, pos.x, 1e-4f);
            Assert.AreEqual(0f, pos.y, 1e-4f);

            Vector2 farSide = RestartResolver.Resolve(RestartType.ThrowIn, new Vector2(52.5f, 70f), 0).position;
            Assert.AreEqual(MatchEngineConstants.PITCH_WIDTH_M, farSide.y, 1e-4f);
        }

        [Test]
        public void Resolve_Corner_PicksNearestCorner_InsetByBallRadius()
        {
            Vector2 pos = RestartResolver.Resolve(RestartType.Corner, new Vector2(-0.5f, 10f), 0).position;
            Assert.AreEqual(BallPhysicsConstants.Ball.RADIUS, pos.x, 1e-4f, "Nearest to the home goal line.");
            Assert.AreEqual(BallPhysicsConstants.Ball.RADIUS, pos.y, 1e-4f, "Nearest to the near touchline.");
        }

        [Test]
        public void Resolve_GoalKick_CentresSixYardBoxOnExitedGoalLine()
        {
            Vector2 pos = RestartResolver.Resolve(RestartType.GoalKick, new Vector2(-0.5f, 10f), 1).position;
            Assert.AreEqual(MatchEngineConstants.GOAL_AREA_DEPTH_M, pos.x, 1e-4f);
            Assert.AreEqual(MatchEngineConstants.PITCH_WIDTH_M * 0.5f, pos.y, 1e-4f);

            Vector2 awayPos = RestartResolver.Resolve(
                RestartType.GoalKick, new Vector2(MatchEngineConstants.PITCH_LENGTH_M + 0.5f, 10f), 0).position;
            Assert.AreEqual(MatchEngineConstants.PITCH_LENGTH_M - MatchEngineConstants.GOAL_AREA_DEPTH_M, awayPos.x, 1e-4f);
        }

        // ── MatchEngine integration ───────────────────────────────────────────────────

        private static MatchEngine RunOneTickWithBallAtAndLastTouch(Vector3 pos, int lastTouchTeam)
        {
            var engine = new MatchEngine(MatchSeed);
            int agent = lastTouchTeam * MatchEngineConstants.PLAYERS_PER_TEAM;
            engine.TestOnly_SetPossession(agent);
            engine.RunTick(); // settles _lastHolderAgentId = agent
            engine.TestOnly_SetBall(BallState.CreateAtPosition(pos));
            engine.RunTick();
            return engine;
        }

        [Test]
        public void ThrowIn_PlacesBallOnTouchline_AndAwardsPossessionToTheOtherTeam()
        {
            var engine = RunOneTickWithBallAtAndLastTouch(new Vector3(52.5f, -0.5f, MatchEngineConstants.BALL_REST_HEIGHT_M), lastTouchTeam: 0);

            BallState ball = engine.TestOnly_BallSnapshot;
            Assert.AreEqual(52.5f, ball.Position.x, 1e-4f);
            Assert.AreEqual(0f, ball.Position.y, 1e-4f);
            Assert.AreEqual(0f, ball.Velocity.magnitude, 1e-4f);

            // §5.Z Phase H (KD-H1): the restart is TAKEN, not merely placed — team 0 touched last, so
            // team 1 is awarded it, and its nearest eligible agent to the throw-in spot is the taker.
            // Before Phase H this asserted NO_POSSESSION, which is precisely the contract that made
            // ERR-030-014 possible: nothing in production ever granted possession.
            int taker = engine.TestOnly_PossessingAgentId;
            Assert.AreNotEqual(MatchEngineConstants.NO_POSSESSION, taker,
                "A throw-in must be awarded to a taker, not left loose.");
            Assert.AreEqual(1, engine.AgentTeamId(taker),
                "Team 0 touched the ball out, so team 1 takes the throw-in.");
            AssertIsNearestOfTeam(engine, taker, new Vector2(52.5f, 0f), team: 1);
        }

        /// <summary>Asserts <paramref name="taker"/> is the nearest agent of <paramref name="team"/> to
        /// <paramref name="spot"/> — the KD-H1 taker-selection rule.</summary>
        private static void AssertIsNearestOfTeam(MatchEngine engine, int taker, Vector2 spot, int team)
        {
            float takerDist = Vector2.Distance(engine.AgentView(taker).Position, spot);
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (engine.AgentTeamId(i) != team)
                {
                    continue;
                }
                Assert.LessOrEqual(takerDist, Vector2.Distance(engine.AgentView(i).Position, spot) + 1e-4f,
                    $"Agent {i} is closer to the restart spot than the selected taker {taker}.");
            }
        }

        [Test]
        public void Corner_PlacesBallAtCornerArc()
        {
            // Home (team 0) touches last and it exits behind their own goal line -> corner for away.
            var engine = RunOneTickWithBallAtAndLastTouch(new Vector3(-0.5f, 10f, MatchEngineConstants.BALL_REST_HEIGHT_M), lastTouchTeam: 0);

            BallState ball = engine.TestOnly_BallSnapshot;
            Assert.AreEqual(BallPhysicsConstants.Ball.RADIUS, ball.Position.x, 1e-4f);
            Assert.AreEqual(BallPhysicsConstants.Ball.RADIUS, ball.Position.y, 1e-4f);
        }

        [Test]
        public void GoalKick_PlacesBallInSixYardBox()
        {
            // Away (team 1) touches last (a shot) that exits behind the home goal line untouched by
            // the defence -> goal kick for home.
            var engine = RunOneTickWithBallAtAndLastTouch(new Vector3(-0.5f, 10f, MatchEngineConstants.BALL_REST_HEIGHT_M), lastTouchTeam: 1);

            BallState ball = engine.TestOnly_BallSnapshot;
            Assert.AreEqual(MatchEngineConstants.GOAL_AREA_DEPTH_M, ball.Position.x, 1e-4f);
            Assert.AreEqual(MatchEngineConstants.PITCH_WIDTH_M * 0.5f, ball.Position.y, 1e-4f);
        }

        [Test]
        public void GoalPath_IsUnaffectedByTheRestartWiring()
        {
            // Regression lock: KickOff (a goal) still takes the pre-existing centre-spot path, not
            // RestartResolver.
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                MatchEngineConstants.PITCH_LENGTH_M + 0.5f,
                MatchEngineConstants.PITCH_WIDTH_M / 2f,
                MatchEngineConstants.BALL_REST_HEIGHT_M)));
            engine.RunTick();

            Assert.AreEqual(1, engine.TestOnly_Goals(0));
            BallState ball = engine.TestOnly_BallSnapshot;
            Assert.AreEqual(MatchEngineConstants.KickoffBallXM, ball.Position.x, 1e-4f);
            Assert.AreEqual(MatchEngineConstants.KickoffBallYM, ball.Position.y, 1e-4f);
        }

        [Test]
        public void TwoRunsWithAThrowIn_BitwiseIdenticalDigests()
        {
            byte[] a = RunWithThrowIn();
            byte[] b = RunWithThrowIn();
            CollectionAssert.AreEqual(a, b);
        }

        private static byte[] RunWithThrowIn()
        {
            var engine = new MatchEngine(MatchSeed);
            for (int i = 0; i < 5; i++) engine.RunTick();
            engine.TestOnly_SetBall(BallState.CreateAtPosition(
                new Vector3(52.5f, -0.5f, MatchEngineConstants.BALL_REST_HEIGHT_M)));
            for (int i = 0; i < 5; i++) engine.RunTick();
            return engine.CurrentSnapshotDigest;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-14 | —      | Initial restart-model suite: RestartResolver pure-function     |
// |         |            |        |   locks (award-team unification, position by type/side) +      |
// |         |            |        |   MatchEngine integration (ball placement, possession clear,   |
// |         |            |        |   goal path untouched) + two-run determinism.                  |
#endregion
