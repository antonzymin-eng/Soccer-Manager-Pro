// File:     src/match-engine/tests/MatchEngineGoalTests.cs
// Created:  2026-07-11
// Modified: 2026-07-11
// Author:   —
// Spec:     Match Engine design note (goal-detection substrate — the #26 §9.3 upstream
//           deliverable); Ball Physics #1 §3 (CheckBoundaries); Code Standards #20
// Purpose:  Locks the Resolve-phase goal check: goal-mouth crossings score for the correct team
//           (classified by exit geometry, so own goals credit the right side), restart the ball
//           at the centre spot, and update the v14 score state; non-goal and airborne exits are
//           untouched (Stage 0 has no throw-in/corner restart model). Determinism re-locked with
//           a goal in the run.

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
        public void Goal_OutsideThePosts_IsNotAGoal_AndBallIsUntouched()
        {
            // Behind the goal line but outside the goal mouth: CheckBoundaries classifies a
            // corner/goal-kick, which Stage 0 has no restart model for — score and ball unchanged
            // (the pre-substrate out-of-play behaviour, preserved exactly).
            Vector3 pos = new Vector3(
                MatchEngineConstants.PITCH_LENGTH_M + 0.5f, 10f, MatchEngineConstants.BALL_REST_HEIGHT_M);
            var engine = RunOneTickWithBallAt(pos);

            Assert.AreEqual(0, engine.TestOnly_Goals(0));
            Assert.AreEqual(0, engine.TestOnly_Goals(1));
            Assert.AreEqual(pos.x, engine.TestOnly_BallSnapshot.Position.x, 1e-4f,
                "No restart for a non-goal exit — the ball stays where it went out.");
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
#endregion
