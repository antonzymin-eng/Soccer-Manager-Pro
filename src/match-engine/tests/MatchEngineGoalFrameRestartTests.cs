// File:     src/match-engine/tests/MatchEngineGoalFrameRestartTests.cs
// Created:  2026-10-11
// Modified: 2026-10-11
// Author:   —
// Spec:     Ball Physics #1 §3.1.10.2 / §3.1.10.3 (ERR-001-007); match-engine design note §5 (restarts);
//           Code Standards #20
// Purpose:  Composed lock for ERR-001-007 through the real RunPhysicsPhase → CheckRestartAndApply seam. A
//           post strike that integration carried past the goal line must not leave the ball OutOfPlay on
//           the pitch side of the line: before the fix the restart check read an in-play position,
//           awarded nothing, and OutOfPlay froze the ball until the next period reset (29 minutes on the
//           Unity run of sim_match_engine_inposs_gate, seed 0x0F1E2D3C4B5A6978). Mirrored for both goals.

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineGoalFrameRestartTests
    {
        private const ulong MatchSeed = 0x474F414C46524D45UL;

        /// <summary>Ticks watched after the strike — one second at 60 Hz.</summary>
        private const int WatchTicks = 60;

        [Test]
        public void PostStrikeAcrossTheHomeGoalLine_NeverLeavesAnUnrestartedOutOfPlayBall() =>
            AssertNoUnrestartedOutOfPlay(home: true);

        [Test]
        public void PostStrikeAcrossTheAwayGoalLine_NeverLeavesAnUnrestartedOutOfPlayBall() =>
            AssertNoUnrestartedOutOfPlay(home: false);

        private static void AssertNoUnrestartedOutOfPlay(bool home)
        {
            var engine = new MatchEngine(MatchSeed ^ (home ? 0UL : 1UL));
            engine.TestOnly_SetPossession(MatchEngineConstants.NO_POSSESSION);

            // The captured Unity strike (tick 56,955), point-mirrored for the away goal.
            float length = MatchEngineConstants.PITCH_LENGTH_M;
            float width = BallPhysicsConstants.Pitch.WIDTH;
            var position = new Vector3(0.0341889262f, 30.1042576f, 0.468117356f);
            var velocity = new Vector3(-12.580143f, 14.4362392f, -2.21744323f);
            if (!home)
            {
                position = new Vector3(length - position.x, width - position.y, position.z);
                velocity = new Vector3(-velocity.x, -velocity.y, velocity.z);
            }

            BallState ball = BallState.CreateAtPosition(position);
            ball.Velocity = velocity;
            ball.State = BallStateType.Airborne;
            engine.TestOnly_SetBall(ball);

            int strikesBefore = engine.TestOnly_WoodworkStrikes;
            string side = home ? "home" : "away";

            for (int tick = 0; tick < WatchTicks; tick++)
            {
                engine.RunTick();
                BallState now = engine.BallView;

                bool unrestartedOutOfPlay =
                    now.State == BallStateType.OutOfPlay
                    && engine.RestartAppliedThisTick == RestartCue.None;
                Assert.IsFalse(unrestartedOutOfPlay,
                    side + " goal, tick " + tick + ": the ball is OutOfPlay at " + now.Position
                    + " and no restart was applied — the ERR-001-007 deadlock");

                if (tick == 0)
                {
                    // Non-vacuity: the lock only means something if the strike actually happened.
                    Assert.AreEqual(strikesBefore + 1, engine.TestOnly_WoodworkStrikes,
                        side + " goal: the first tick must strike the post");
                }
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-10-11 | —      | Initial: ERR-001-007 composed lock, both goals — a post strike     |
// |         |            |        | that crossed the goal line within the tick never leaves the ball   |
// |         |            |        | OutOfPlay without a restart, with a strike-count non-vacuity check.|
#endregion
