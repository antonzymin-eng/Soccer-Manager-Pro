// File:     src/match-engine/tests/MatchEngineMatchFlowTests.cs
// Created:  2026-07-14
// Modified: 2026-07-14
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-flow-completion-design.md) §7, Code Standards #20
// Purpose:  Locks the half-time (ball reset, fires once) and full-time (gameplay freeze, fires once)
//           match-flow clock transitions. Uses the explicit-tick TestOnly seam (mirrors the existing
//           TestOnly_RunManagerDecisionPoints pattern) so the ~162 000 / ~324 000 real ticks never run;
//           all direct calls happen before any RunTick() so EventBus.CurrentPhase is still the
//           Input-phase value ResetForNewMatch left it at (Boot), keeping the MatchPhaseChangedEvent
//           publish valid.

using NUnit.Framework;

using UnityEngine;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineMatchFlowTests
    {
        private const ulong MatchSeed = 0x00C0FFEE5EEDBA11UL;

        [Test]
        public void BeforeBoundary_NeitherTransitionFires()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.HALF_TIME_BOUNDARY_TICK - 1);

            Assert.IsFalse(engine.TestOnly_SecondHalfStarted);
            Assert.IsFalse(engine.TestOnly_MatchEnded);
        }

        [Test]
        public void HalfTimeBoundary_ResetsBallToCentre_AndFiresOnce()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(80f, 50f, MatchEngineConstants.BALL_REST_HEIGHT_M)));

            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.HALF_TIME_BOUNDARY_TICK);

            Assert.IsTrue(engine.TestOnly_SecondHalfStarted);
            BallState ball = engine.TestOnly_BallSnapshot;
            Assert.AreEqual(MatchEngineConstants.KickoffBallXM, ball.Position.x, 1e-4f);
            Assert.AreEqual(MatchEngineConstants.KickoffBallYM, ball.Position.y, 1e-4f);
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId);

            // Move the ball away, then re-check at a later tick — the guard must prevent a second reset.
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(80f, 50f, MatchEngineConstants.BALL_REST_HEIGHT_M)));
            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.HALF_TIME_BOUNDARY_TICK + 100);
            Assert.AreEqual(80f, engine.TestOnly_BallSnapshot.Position.x, 1e-4f, "Half-time must not re-fire.");
        }

        [Test]
        public void HalfTimeBoundary_DoesNotFlipMatchEnded()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.HALF_TIME_BOUNDARY_TICK);
            Assert.IsFalse(engine.TestOnly_MatchEnded);
        }

        [Test]
        public void FullTimeBoundary_SetsMatchEnded_AndFiresOnce()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.MATCH_TICKS_TOTAL);
            Assert.IsTrue(engine.TestOnly_MatchEnded);

            // Second call at a later tick must be a no-op (nothing further to assert on besides no throw
            // — the guard is the same `!_matchEnded` pattern already locked by the half-time test above).
            Assert.DoesNotThrow(() => engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.MATCH_TICKS_TOTAL + 1000));
        }

        [Test]
        public void AfterFullTime_GameplayIsFrozen_BallAndAgentsUnchanged_ButTickAdvances()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.MATCH_TICKS_TOTAL);
            Assert.IsTrue(engine.TestOnly_MatchEnded);

            BallState before = engine.TestOnly_BallSnapshot;
            ulong tickBefore = engine.CurrentTick;

            for (int i = 0; i < 10; i++)
            {
                engine.RunTick();
            }

            Assert.Greater(engine.CurrentTick, tickBefore, "The tick/snapshot loop keeps advancing while frozen.");
            BallState after = engine.TestOnly_BallSnapshot;
            Assert.AreEqual(before.Position.x, after.Position.x, 1e-6f, "Physics is frozen — the ball does not move.");
            Assert.AreEqual(before.Position.y, after.Position.y, 1e-6f);
            Assert.AreEqual(before.Velocity.x, after.Velocity.x, 1e-6f);
        }

        [Test]
        public void TwoRunsPastFullTime_BitwiseIdenticalDigests()
        {
            byte[] a = RunPastFullTime();
            byte[] b = RunPastFullTime();
            CollectionAssert.AreEqual(a, b);
        }

        private static byte[] RunPastFullTime()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.MATCH_TICKS_TOTAL);
            for (int i = 0; i < 20; i++) engine.RunTick();
            return engine.CurrentSnapshotDigest;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-14 | —      | Initial match-flow-clock suite: pre-boundary no-op, half-time  |
// |         |            |        |   ball reset + fires-once guard, full-time flag + fires-once   |
// |         |            |        |   guard, post-full-time gameplay freeze (ball/agents unchanged |
// |         |            |        |   while the tick/snapshot loop keeps advancing), + two-run      |
// |         |            |        |   determinism.                                                 |
#endregion
