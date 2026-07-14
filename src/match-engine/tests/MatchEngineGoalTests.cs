// File:     src/match-engine/tests/MatchEngineGoalTests.cs
// Created:  2026-07-11
// Modified: 2026-07-14
// Author:   —
// Spec:     Match Engine design note (goal-detection substrate — the #26 §9.3 upstream
//           deliverable); Match Engine design note (match-flow-completion — RestartResolver);
//           Ball Physics #1 §3 (CheckBoundaries); Code Standards #20
// Purpose:  Locks the Resolve-phase goal check: goal-mouth crossings score for the correct team
//           (classified by exit geometry, so own goals credit the right side), restart the ball
//           at the centre spot, and update the v14 score state; airborne exits are untouched.
//           Non-goal exits now route through RestartResolver (throw-in/corner/goal-kick) per the
//           July 14, 2026 match-flow completion — see MatchEngineRestartTests.cs for the full
//           restart-model coverage. Determinism re-locked with a goal in the run.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.MatchEngine
{
    /// <summary>Engine-substrate goal-detection tests (see file header).</summary>
    [TestFixture]
    public sealed class MatchEngineGoalTests
    {
        private const ulong MatchSeed = 0x00C0FFEE5EEDBA11UL;

        private const float GoalMouthY = MatchEngineConstants.PITCH_WIDTH_M / 2f;  // 34 m — between the posts

        /// <summary>Places a stationary ball at <paramref name="pos"/> and runs one tick, so the
        /// Resolve-phase goal check classifies exactly that position (a stationary ball does not
        /// move in the Physics phase).</summary>
        private static MatchEngine RunOneTickWithBallAt(Vector3 pos)
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetBall(BallState.CreateAtPosition(pos));
            engine.RunTick();
            return engine;
        }

        [Test]
        public void Goal_AwayGoalMouthCrossing_ScoresHome_AndRestartsAtCentre()
        {
            // Fully across the away goal line (x > LENGTH + r), between the posts, under the
            // crossbar, ground-level: home (team 0, attacking +X) scores; centre-spot restart.
            var engine = RunOneTickWithBallAt(new Vector3(
                MatchEngineConstants.PITCH_LENGTH_M + 0.5f, GoalMouthY, MatchEngineConstants.BALL_REST_HEIGHT_M));

            Assert.AreEqual(1, engine.TestOnly_Goals(0), "Home scores into the away goal (+X).");
            Assert.AreEqual(0, engine.TestOnly_Goals(1));

            BallState ball = engine.TestOnly_BallSnapshot;
            Assert.AreEqual(MatchEngineConstants.KickoffBallXM, ball.Position.x, 1e-4f, "Centre-spot restart (X).");
            Assert.AreEqual(MatchEngineConstants.KickoffBallYM, ball.Position.y, 1e-4f, "Centre-spot restart (Y).");
        }

        [Test]
        public void Goal_HomeGoalMouthCrossing_ScoresAway()
        {
            // Fully across the home goal line (x < −r): away (team 1) scores — geometry classifies
            // the TEAM, so an own-goal deflection still credits the attacking side correctly.
            var engine = RunOneTickWithBallAt(new Vector3(
                -0.5f, GoalMouthY, MatchEngineConstants.BALL_REST_HEIGHT_M));

            Assert.AreEqual(0, engine.TestOnly_Goals(0));
            Assert.AreEqual(1, engine.TestOnly_Goals(1), "Away scores into the home goal (−X).");
        }

        [Test]
        public void Goal_OutsideThePosts_IsNotAGoal_AndRestartsAsGoalKick()
        {
            // Behind the goal line but outside the goal mouth: CheckBoundaries classifies a
            // goal-kick (default lastTouchTeam is 0/home on a fresh engine, so the away-side exit
            // is credited to the defending team per RestartResolver) — no goal, and the ball
            // restarts in the six-yard box per MatchEngineRestartTests.GoalKick_PlacesBallInSixYardBox,
            // superseding the pre-substrate "ball stays where it went out" behaviour this test
            // locked before the match-flow restart model existed.
            Vector3 pos = new Vector3(
                MatchEngineConstants.PITCH_LENGTH_M + 0.5f, 10f, MatchEngineConstants.BALL_REST_HEIGHT_M);
            var engine = RunOneTickWithBallAt(pos);

            Assert.AreEqual(0, engine.TestOnly_Goals(0));
            Assert.AreEqual(0, engine.TestOnly_Goals(1));
            BallState ball = engine.TestOnly_BallSnapshot;
            Assert.AreEqual(
                MatchEngineConstants.PITCH_LENGTH_M - MatchEngineConstants.GOAL_AREA_DEPTH_M,
                ball.Position.x, 1e-4f, "Goal-kick restart in the six-yard box on the exited (away) goal line.");
            Assert.AreEqual(MatchEngineConstants.PITCH_WIDTH_M * 0.5f, ball.Position.y, 1e-4f);
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId);
        }

        [Test]
        public void Goal_AirborneCrossing_NotDetectedUntilGround()
        {
            // The Stage-0 z-gate is CheckBoundaries' own documented scope: a ball above
            // Ball.Diameter is not classified as out, so no goal fires while it is airborne.
            var engine = RunOneTickWithBallAt(new Vector3(
                MatchEngineConstants.PITCH_LENGTH_M + 0.5f, GoalMouthY, 1.0f));

            Assert.AreEqual(0, engine.TestOnly_Goals(0));
            Assert.AreEqual(0, engine.TestOnly_Goals(1));
        }

        [Test]
        public void Goal_ScorerCredit_TracksLastSettledHolder()
        {
            // The v14 last-holder tracker (the GoalAwardedEvent scorer credit): an agent who held
            // settled possession stays recorded after the ball goes loose and into the goal.
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetPossession(3);
            engine.RunTick();  // settles: _lastHolderAgentId = 3
            Assert.AreEqual(3, engine.TestOnly_LastHolderAgentId);

            engine.TestOnly_SetPossession(MatchEngineConstants.NO_POSSESSION);
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                MatchEngineConstants.PITCH_LENGTH_M + 0.5f, GoalMouthY, MatchEngineConstants.BALL_REST_HEIGHT_M)));
            engine.RunTick();

            Assert.AreEqual(1, engine.TestOnly_Goals(0));
            Assert.AreEqual(3, engine.TestOnly_LastHolderAgentId,
                "The tracker is only ever overwritten by a real holder — at goal time it still names the kicker.");
        }

        [Test]
        public void Goal_TwoRunsWithAGoal_BitwiseIdenticalDigests()
        {
            // Determinism re-lock across the new producer: a run containing a goal (score
            // increment + Tier A GoalAwardedEvent in the ledger + restart) chains byte-identical
            // digests across two same-seed runs.
            byte[] a = RunWithGoal();
            byte[] b = RunWithGoal();
            CollectionAssert.AreEqual(a, b);
        }

        private static byte[] RunWithGoal()
        {
            var engine = new MatchEngine(MatchSeed);
            for (int i = 0; i < 10; i++)
            {
                engine.RunTick();
            }
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                MatchEngineConstants.PITCH_LENGTH_M + 0.5f, GoalMouthY, MatchEngineConstants.BALL_REST_HEIGHT_M)));
            for (int i = 0; i < 20; i++)
            {
                engine.RunTick();
            }
            Assert.AreEqual(1, engine.TestOnly_Goals(0), "The scripted crossing must register exactly one goal.");
            return engine.CurrentSnapshotDigest;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-11 | —      | Initial goal-detection substrate suite (6 tests): both goal    |
// |         |            |        |   mouths, non-goal + airborne exits untouched, last-holder     |
// |         |            |        |   scorer credit, two-run determinism with a goal in the run.   |
// | 1.1     | 2026-07-14 | —      | Goal_OutsideThePosts_IsNotAGoal_AndBallIsUntouched renamed to  |
// |         |            |        |   Goal_OutsideThePosts_IsNotAGoal_AndRestartsAsGoalKick and     |
// |         |            |        |   re-derived: it asserted the pre-substrate "ball stays put"   |
// |         |            |        |   behaviour, which the same-day match-flow-completion landing  |
// |         |            |        |   (RestartResolver) superseded without this v1.0 test being    |
// |         |            |        |   updated — caught by the real CI test run once the compile    |
// |         |            |        |   error blocking it was fixed. Now asserts the goal-kick        |
// |         |            |        |   restart position, matching MatchEngineRestartTests.          |
#endregion
