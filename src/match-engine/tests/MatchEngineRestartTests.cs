// File:     src/match-engine/tests/MatchEngineRestartTests.cs
// Created:  2026-07-14
// Modified: 2026-10-12 (ERR-001-007: goal-frame/state ordering)
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

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void int_OutsidePostRebound_DoesNotFreezeBeforeTheNextPeriod(bool awayGoal, bool secondHalf)
        {
            // PR #499 tick 56,955: reproduce the actual outside-post approach, independently of
            // the full-match trajectory and runtime. Mirror X to lock the other goal as well.
            var engine = new MatchEngine(MatchSeed);
            if (secondHalf)
            {
                engine.TestOnly_SetPhysicsFrame((int)MatchEngineConstants.HALF_TIME_BOUNDARY_TICK);
                engine.RunTick();
            }
            Assert.AreEqual(secondHalf ? MatchPeriod.SecondHalf : MatchPeriod.FirstHalf, engine.CurrentPeriod);

            int lastTouchTeam = awayGoal ? 0 : 1;
            engine.TestOnly_SetPossession(lastTouchTeam * MatchEngineConstants.PLAYERS_PER_TEAM);
            engine.RunTick(); // establish the last settled holder used by restart classification

            float x = awayGoal ? MatchEngineConstants.PITCH_LENGTH_M - 0.0341889262f : 0.0341889262f;
            float vx = awayGoal ? 12.580143f : -12.580143f;
            engine.TestOnly_ForceBallLoose(new Vector3(x, 30.1042576f, 0.468117356f),
                new Vector3(vx, 14.4362392f, -2.21744323f));

            engine.RunTick();
            BallState rebound = engine.TestOnly_BallSnapshot;
            Assert.AreEqual(1, engine.TestOnly_WoodworkStrikes, "The fixture must actually strike the post.");
            Assert.IsFalse(BallStateMachine.IsOutOfBounds(rebound.Position));
            Assert.AreEqual(RestartCue.None, engine.RestartAppliedThisTick,
                "Contact before a full crossing must not manufacture a restart.");
            Assert.AreEqual(BallStateType.Airborne, rebound.State,
                "The pre-fix engine left this in-play rebound OutOfPlay until half-time.");

            engine.RunTick();
            Assert.AreNotEqual(rebound.Position, engine.TestOnly_BallSnapshot.Position,
                "Play must progress on the very next tick, without waiting for a period reset.");
            // This glancing rebound still travels outward in X. It leaves the field a few ticks
            // later, outside the mouth, and must receive an ordinary restart rather than freeze.
            for (int i = 0; i < 4 && engine.RestartAppliedThisTick == RestartCue.None; i++)
                engine.RunTick();
            Assert.AreEqual(RestartCue.GoalKick, engine.RestartAppliedThisTick);
            Assert.AreEqual(1 - lastTouchTeam, engine.RestartAwardedTeam);
            Assert.AreNotEqual(BallStateType.OutOfPlay, engine.TestOnly_BallSnapshot.State);
            Assert.AreEqual(secondHalf ? MatchPeriod.SecondHalf : MatchPeriod.FirstHalf, engine.CurrentPeriod);
        }

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void int_FullCrossingBeforeRearPostContact_AwardsTheProperRestart(bool awayGoal, bool defenderLast)
        {
            var engine = new MatchEngine(MatchSeed);
            int defender = awayGoal ? 1 : 0;
            int lastTouchTeam = defenderLast ? defender : 1 - defender;
            engine.TestOnly_SetPossession(lastTouchTeam * MatchEngineConstants.PLAYERS_PER_TEAM);
            engine.RunTick();
            float goalX = awayGoal ? MatchEngineConstants.PITCH_LENGTH_M : 0f;
            float direction = awayGoal ? 1f : -1f;
            float postY = BallPhysicsConstants.Pitch.WIDTH * 0.5f + BallPhysicsConstants.GoalFrame.PostAxisOffsetY;
            engine.TestOnly_ForceBallLoose(new Vector3(goalX + direction * 0.05f, postY + 0.4f, 1f),
                new Vector3(direction * 8.4f, -30f, 0f));

            engine.RunTick();

            Assert.AreEqual(0, engine.TestOnly_WoodworkStrikes,
                "A rear-side contact after the full crossing must not override the exit.");
            Assert.AreEqual(defenderLast ? RestartCue.Corner : RestartCue.GoalKick, engine.RestartAppliedThisTick);
            Assert.AreEqual(1 - lastTouchTeam, engine.RestartAwardedTeam);
            Assert.AreEqual(0, engine.HomeScore);
            Assert.AreEqual(0, engine.AwayScore);
            Assert.AreEqual(BallStateType.Stationary, engine.TestOnly_BallSnapshot.State);
            Assert.AreEqual(1 - lastTouchTeam, engine.AgentTeamId(engine.TestOnly_PossessingAgentId));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void int_OutsidePostRebound_SaveRestorePreservesProgressAndDigest(bool awayGoal)
        {
            var engine = new MatchEngine(MatchSeed);
            float x = awayGoal ? MatchEngineConstants.PITCH_LENGTH_M - 0.0341889262f : 0.0341889262f;
            float vx = awayGoal ? 12.580143f : -12.580143f;
            engine.TestOnly_ForceBallLoose(new Vector3(x, 30.1042576f, 0.468117356f),
                new Vector3(vx, 14.4362392f, -2.21744323f));
            engine.RunTick();
            Assert.AreEqual(BallStateType.Airborne, engine.TestOnly_BallSnapshot.State);
            var header = engine.CaptureDurableHeader();
            var payload = engine.CaptureDurablePayload();
            var digests = new byte[5][];
            var cues = new RestartCue[5];
            // Sequential references follow the engine's established restore-test protocol;
            // concurrent engines share the EventBus phase lifecycle and are not independent.
            for (int i = 0; i < digests.Length; i++)
            {
                engine.RunTick();
                digests[i] = engine.CurrentSnapshotDigest;
                cues[i] = engine.RestartAppliedThisTick;
            }
            var restored = MatchEngine.RestoreFromSnapshot(in header, payload, MatchSeed);
            Vector3 rebound = restored.TestOnly_BallSnapshot.Position;
            for (int i = 0; i < digests.Length; i++)
            {
                restored.RunTick();
                CollectionAssert.AreEqual(digests[i], restored.CurrentSnapshotDigest,
                    "The correction must survive the existing canonical snapshot codec.");
                Assert.AreEqual(cues[i], restored.RestartAppliedThisTick);
                if (i == 0)
                    Assert.AreNotEqual(rebound, restored.TestOnly_BallSnapshot.Position);
            }
        }

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
// | 1.1 | 2026-10-12 | — | ERR-001-007: ten cases reproduce PR #499 at both goals/in both halves, lock crossing-before-contact corner/goal-kick awards and digest-identical restore continuation. |
#endregion
