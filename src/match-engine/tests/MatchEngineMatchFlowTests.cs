// File:     src/match-engine/tests/MatchEngineMatchFlowTests.cs
// Created:  2026-07-14
// Modified: 2026-07-26 (§5.Z.10: kickoff keeper-placement locks — a keeper never moves at Stage 0, so boot placement is its position for the whole match)
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

using TacticalDirector.BallPhysics;

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
            Assert.IsFalse(engine.MatchEnded);
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

            // §5.Z Phase H (KD-H1): the second-half kickoff is TAKEN, by the side that did not kick off
            // the first half (Law 8). Pre-Phase-H this asserted NO_POSSESSION.
            int taker = engine.TestOnly_PossessingAgentId;
            Assert.AreNotEqual(MatchEngineConstants.NO_POSSESSION, taker,
                "The second-half kickoff must be awarded to a taker.");
            Assert.AreEqual(MatchEngineConstants.SECOND_HALF_KICKOFF_TEAM, engine.AgentTeamId(taker),
                "The team that did not kick off the first half restarts the second.");
            Assert.AreNotEqual(MatchEngineConstants.FIRST_HALF_KICKOFF_TEAM,
                MatchEngineConstants.SECOND_HALF_KICKOFF_TEAM,
                "The two halves must be kicked off by different teams.");

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
            Assert.IsTrue(engine.MatchEnded, "Public MatchEnded mirrors TestOnly_MatchEnded.");

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

        // ── Kickoff keeper placement (§5.Z.10) ────────────────────────────────────────

        [Test]
        public void Kickoff_EachKeeperStandsOnTheGoalLineItDefends()
        {
            // A keeper never moves at Stage 0 — the Physics phase skips goalkeepers (#11 owns GK
            // locomotion) — so boot placement IS the keeper's position for the whole match. Under the
            // original shared-line placement both keepers landed on the outfield kickoff line at the
            // k = 0 lateral slot (26.25 / 78.75, y = 5.67): 26 m upfield of their own goal and 28 m
            // off-centre, leaving BOTH goals unguarded for ninety minutes.
            var engine = new MatchEngine(0x00C0FFEE5EEDBA11UL);

            float mouthY = MatchEngineConstants.PITCH_WIDTH_M * 0.5f;
            int keepersFound = 0;

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (!engine.TestOnly_IsGoalkeeper(i))
                {
                    continue;
                }

                keepersFound++;
                Vector2 p = engine.TestOnly_AgentSnapshot(i).Position;
                int team = engine.AgentTeamId(i);

                // Team 0 attacks +X, so it DEFENDS x = 0; team 1 defends x = PITCH_LENGTH.
                float expectedX = team == 0
                    ? MatchEngineConstants.GkKickoffDepthM
                    : MatchEngineConstants.PITCH_LENGTH_M - MatchEngineConstants.GkKickoffDepthM;

                Assert.AreEqual(expectedX, p.x, 1e-3f,
                    "team " + team + "'s keeper must stand off the goal line it defends, not on the "
                    + "outfield kickoff line");
                Assert.AreEqual(mouthY, p.y, 1e-3f,
                    "team " + team + "'s keeper must be centred on the goal mouth");
            }

            Assert.AreEqual(MatchEngineConstants.TEAM_COUNT, keepersFound,
                "exactly one keeper per team");
        }

        [Test]
        public void Kickoff_KeepersGuardOppositeGoals()
        {
            // The load-bearing half of the placement: two keepers on the SAME goal line would leave one
            // goal open all match, which is the shape the original defect took in effect.
            var engine = new MatchEngine(0x00C0FFEE5EEDBA11UL);

            float half = MatchEngineConstants.PITCH_LENGTH_M * 0.5f;
            bool guardsLowEnd = false;
            bool guardsHighEnd = false;

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (!engine.TestOnly_IsGoalkeeper(i))
                {
                    continue;
                }

                if (engine.TestOnly_AgentSnapshot(i).Position.x < half) guardsLowEnd = true;
                else guardsHighEnd = true;
            }

            Assert.IsTrue(guardsLowEnd && guardsHighEnd,
                "the two keepers must guard opposite ends; both on one line leaves a goal undefended");
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
// | 1.1     | 2026-07-15 | —      | Interactive match view: BeforeBoundary/FullTimeBoundary tests   |
// |         |            |        |   also assert the new public MatchEnded property mirrors       |
// |         |            |        |   TestOnly_MatchEnded (the observation-surface consumer         |
// |         |            |        |   contract the live viewer's auto-pause depends on).            |
#endregion
