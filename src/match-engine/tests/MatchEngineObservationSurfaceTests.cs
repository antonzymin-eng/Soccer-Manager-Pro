// File:     src/match-engine/tests/MatchEngineObservationSurfaceTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md) §5-P1,
//           Code Standards #20
// Purpose:  Locks the P1 richer-observation-frame surface: the per-agent discipline / substitution
//           accessors, the derived MatchPeriod, and the within-tick restart cue — including the two
//           properties that make the surface safe to add at all, namely that reading it perturbs
//           nothing (digest-identical to an unobserved run) and that the restart cue is genuinely
//           within-tick rather than a latch that would have to be serialized.

using System;

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineObservationSurfaceTests
    {
        private const ulong MatchSeed = 0x0B5E12EDF00D5EEDUL;

        // ── Per-agent discipline / substitution accessors ────────────────────────────────────────

        [Test]
        public void FreshEngine_ReportsNoCardsNoSendingsOffNoSubstitutes()
        {
            var engine = new MatchEngine(MatchSeed);

            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                Assert.AreEqual(0, engine.AgentYellowCards(i), "agent " + i + " starts unbooked");
                Assert.IsFalse(engine.AgentIsSentOff(i), "agent " + i + " starts on the pitch");
                Assert.AreEqual(-1, engine.AgentBenchSlot(i), "agent " + i + " starts as the original starter");
            }

            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                Assert.AreEqual(0, engine.SubstitutionsUsed(t));
            }
        }

        [Test]
        public void SentOffAgent_IsReportedByThePublicAccessor()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetIsSentOff(7, true);

            Assert.IsTrue(engine.AgentIsSentOff(7));
            Assert.IsFalse(engine.AgentIsSentOff(6), "the flag must not bleed to a neighbouring slot");
            Assert.AreEqual(engine.TestOnly_IsSentOff(7), engine.AgentIsSentOff(7),
                "the public accessor and the internal seam must agree");
        }

        [Test]
        public void Substitution_IsReportedByBenchSlotAndTeamCount()
        {
            var engine = new MatchEngine(MatchSeed);

            // Slot 5 belongs to some team; substitute into it and read the cue back.
            int slot = 5;
            int teamId = engine.AgentTeamId(slot);
            engine.SubstitutePlayer(teamId, slot, benchIndex: 0, SubstitutionReason.Tactical);

            Assert.AreEqual(0, engine.AgentBenchSlot(slot), "the slot now carries bench player 0");
            Assert.AreEqual(1, engine.SubstitutionsUsed(teamId));
            Assert.AreEqual(0, engine.SubstitutionsUsed(1 - teamId),
                "the other team's count must be untouched");
        }

        [Test]
        public void RosterAndTeamGuards_RefuseOutOfRange()
        {
            var engine = new MatchEngine(MatchSeed);
            int n = MatchEngineConstants.SQUAD_SIZE;

            Assert.Throws<ArgumentOutOfRangeException>(() => engine.AgentYellowCards(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.AgentYellowCards(n));
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.AgentIsSentOff(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.AgentIsSentOff(n));
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.AgentBenchSlot(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.AgentBenchSlot(n));

            Assert.Throws<ArgumentOutOfRangeException>(() => engine.SubstitutionsUsed(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.SubstitutionsUsed(MatchEngineConstants.TEAM_COUNT));
        }

        // ── Derived match period (KD-P1-2) ───────────────────────────────────────────────────────

        [Test]
        public void Period_TracksTheTransitionsTheEngineActuallyFired()
        {
            var engine = new MatchEngine(MatchSeed);
            Assert.AreEqual(MatchPeriod.FirstHalf, engine.CurrentPeriod);

            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.HALF_TIME_BOUNDARY_TICK - 1);
            Assert.AreEqual(MatchPeriod.FirstHalf, engine.CurrentPeriod,
                "one tick short of the boundary is still the first half");

            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.HALF_TIME_BOUNDARY_TICK);
            Assert.AreEqual(MatchPeriod.SecondHalf, engine.CurrentPeriod);

            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.MATCH_TICKS_TOTAL);
            Assert.AreEqual(MatchPeriod.FullTime, engine.CurrentPeriod);
        }

        [Test]
        public void FullTime_OutranksSecondHalf()
        {
            // Both flags set: the report must be the later of the two, not whichever is checked first.
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.HALF_TIME_BOUNDARY_TICK);
            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.MATCH_TICKS_TOTAL);

            Assert.IsTrue(engine.TestOnly_SecondHalfStarted);
            Assert.IsTrue(engine.MatchEnded);
            Assert.AreEqual(MatchPeriod.FullTime, engine.CurrentPeriod);
        }

        // ── Within-tick restart cue (KD-P1-3 / KD-P1-4) ──────────────────────────────────────────

        [Test]
        public void FreshEngine_ReportsNoRestart()
        {
            var engine = new MatchEngine(MatchSeed);

            Assert.AreEqual(RestartCue.None, engine.RestartAppliedThisTick);
            Assert.AreEqual(MatchEngineConstants.NO_RESTART_TEAM, engine.RestartAwardedTeam);
        }

        [Test]
        public void HalfTimeKickoff_ReportsAKickOffCueForTheAwayTeam()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.HALF_TIME_BOUNDARY_TICK);

            Assert.AreEqual(RestartCue.KickOff, engine.RestartAppliedThisTick);
            Assert.AreEqual(MatchEngineConstants.SECOND_HALF_KICKOFF_TEAM, engine.RestartAwardedTeam,
                "Law 8 — the second half is kicked off by the side that did not start the first");
        }

        /// <summary>
        /// The KD-P1-3 property the whole design rests on: the cue describes the tick it happened on and
        /// nothing more. If this ever fails the field has become cross-tick state, and it would then have
        /// to be serialized — so this test is what keeps the snapshot out of it.
        /// </summary>
        [Test]
        public void RestartCue_IsClearedByTheNextTick()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.HALF_TIME_BOUNDARY_TICK);
            Assert.AreEqual(RestartCue.KickOff, engine.RestartAppliedThisTick, "precondition");

            engine.RunTick();

            Assert.AreEqual(RestartCue.None, engine.RestartAppliedThisTick,
                "a restart must be reported for its own tick only — otherwise it is cross-tick state");
            Assert.AreEqual(MatchEngineConstants.NO_RESTART_TEAM, engine.RestartAwardedTeam);
        }

        /// <summary>
        /// KD-P1-4 — each restart kind reaches the View as itself. Drives a real boundary exit over a
        /// touchline, which the engine classifies as a throw-in, and checks the cue is not simply
        /// whatever the previous restart happened to be.
        /// </summary>
        [Test]
        public void BoundaryExit_ReportsTheThrowInCue()
        {
            var engine = new MatchEngine(MatchSeed);

            // Park the ball beyond a touchline at rest, then run one tick so the Resolve-phase
            // restart check classifies it.
            engine.TestOnly_SetBall(BallState.CreateAtPosition(
                new Vector3(50f, MatchEngineConstants.PITCH_WIDTH_M + 2f, MatchEngineConstants.BALL_REST_HEIGHT_M)));
            engine.RunTick();

            Assert.AreEqual(RestartCue.ThrowIn, engine.RestartAppliedThisTick,
                "a ball out over a touchline is a throw-in, not the KickOff the previous restart was");
            Assert.IsTrue(
                engine.RestartAwardedTeam == 0 || engine.RestartAwardedTeam == 1,
                "a throw-in is always awarded to one of the two teams");
        }

        [Test]
        public void GoalLineExit_ReportsAGoalKickOrCornerCue()
        {
            var engine = new MatchEngine(MatchSeed);

            engine.TestOnly_SetBall(BallState.CreateAtPosition(
                new Vector3(MatchEngineConstants.PITCH_LENGTH_M + 2f, 5f, MatchEngineConstants.BALL_REST_HEIGHT_M)));
            engine.RunTick();

            // Which of the two depends on who touched last; both are goal-line exits and neither is a
            // throw-in or a free kick, which is what this locks.
            Assert.That(engine.RestartAppliedThisTick,
                Is.EqualTo(RestartCue.GoalKick).Or.EqualTo(RestartCue.Corner).Or.EqualTo(RestartCue.KickOff),
                "an exit over a goal line is a goal kick, a corner, or (between the posts) a goal");
        }

        // ── The property that makes the whole surface safe (observer neutrality) ─────────────────

        /// <summary>
        /// Reading the P1 surface every tick must be byte-identical to not reading it. This is the
        /// same lock the match viewer carries for its own observation surface, re-taken for the fields
        /// P1 adds — including the two new engine fields, which a mistake could easily have let leak
        /// into gameplay.
        /// </summary>
        [Test]
        public void ReadingTheP1Surface_IsObserverNeutral()
        {
            const int Ticks = 400;

            // Each engine is constructed AND run to completion before the next is constructed. The
            // EventBus is process-static (#17 §3.2.1), so interleaving two engines' ticks diverges them
            // at tick 1 regardless of what is being observed — the same sequential discipline every
            // determinism test in this assembly follows.
            byte[] observedDigest = RunObserving(Ticks);
            byte[] quietDigest    = RunQuiet(Ticks);

            CollectionAssert.AreEqual(quietDigest, observedDigest,
                "observing the P1 surface must not perturb the simulation by one bit");
        }

        private static byte[] RunObserving(int ticks)
        {
            var engine = new MatchEngine(MatchSeed);
            for (int t = 0; t < ticks; t++)
            {
                engine.RunTick();

                // Touch every new accessor, exactly as the streamer does.
                for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
                {
                    _ = engine.AgentYellowCards(i);
                    _ = engine.AgentIsSentOff(i);
                    _ = engine.AgentBenchSlot(i);
                }
                _ = engine.SubstitutionsUsed(0);
                _ = engine.SubstitutionsUsed(1);
                _ = engine.CurrentPeriod;
                _ = engine.RestartAppliedThisTick;
                _ = engine.RestartAwardedTeam;
            }
            return engine.CurrentSnapshotDigest;
        }

        private static byte[] RunQuiet(int ticks)
        {
            var engine = new MatchEngine(MatchSeed);
            for (int t = 0; t < ticks; t++)
            {
                engine.RunTick();
            }
            return engine.CurrentSnapshotDigest;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (P1 richer observation frame): discipline /   |
// |         |            |        | substitution accessors + guards, derived MatchPeriod, the      |
// |         |            |        | within-tick restart cue (incl. the cleared-next-tick lock that |
// |         |            |        | keeps it out of the snapshot, and per-kind cue locks), and the |
// |         |            |        | observer-neutrality digest lock over the whole new surface.    |
#endregion
