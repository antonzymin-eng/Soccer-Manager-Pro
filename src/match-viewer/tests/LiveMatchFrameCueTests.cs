// File:     src/match-viewer/tests/LiveMatchFrameCueTests.cs
// Created:  2026-07-27
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md) §5-P1,
//           Testing Strategy #19 (unit layer), Code Standards #20
// Purpose:  Locks the P1 half of the captured frame: per-agent cues sampled in lockstep with the
//           positions they annotate, the per-team substitution counts, the derived period, and — the
//           load-bearing one — the streamer-side restart LATCH, which is what lets the engine forget a
//           restart the tick after it happens and therefore keeps it out of the snapshot entirely.

using NUnit.Framework;

using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchViewer.Tests
{
    [TestFixture]
    public class LiveMatchFrameCueTests
    {
        private const ulong Seed = 0x0B5E12EDF00D5EEDUL;

        // Restarts are sparse in live play (measured on this seed: the first lands around tick 3 900,
        // ~65 s in). The bound is generous enough to absorb engine tuning without becoming vacuous —
        // and RestartLatch_* asserts a restart really was observed, so a bound that stopped working
        // fails the test rather than silently skipping the property under check.
        private const int RestartSearchTicks = 8000;

        [Test]
        public void CapturedFrame_CarriesOneCuePerAgent_InLockstepWithPositions()
        {
            var engine   = new MatchEngine.MatchEngine(Seed);
            var streamer = new LiveMatchStreamer(engine);

            LiveMatchFrame frame = streamer.TickOnce();

            Assert.AreEqual(MatchEngineConstants.SQUAD_SIZE, frame.AgentCues.Length,
                "one cue per agent — a View indexes cues and positions with the same roster index");
            Assert.AreEqual(frame.AgentPositions.Length, frame.AgentCues.Length);

            for (int i = 0; i < frame.AgentCues.Length; i++)
            {
                Assert.AreEqual(engine.AgentYellowCards(i), frame.AgentCues[i].YellowCards, "agent " + i);
                Assert.AreEqual(engine.AgentIsSentOff(i),   frame.AgentCues[i].IsSentOff,   "agent " + i);
                Assert.AreEqual(engine.AgentBenchSlot(i),   frame.AgentCues[i].BenchSlot,   "agent " + i);
                Assert.AreEqual(engine.AgentIsGoalkeeper(i), frame.AgentCues[i].IsGoalkeeper, "agent " + i);
            }
        }

        [Test]
        public void TheCueConstructorMapsItsArgumentsPositionally()
        {
            // Two ints and two bools: the shape AR-1 M-6 called out on LiveMatchFrame's ctor, here at
            // four parameters where collapsing them into carriers would cost more than it buys. The
            // cheap defence is to assert the mapping once, so a transposed pair fails here rather
            // than as a sent-off keeper somewhere on screen.
            var cue = new LiveAgentCue(yellowCards: 1, isSentOff: false, benchSlot: 5, isGoalkeeper: true);

            Assert.AreEqual(1, cue.YellowCards);
            Assert.IsFalse(cue.IsSentOff);
            Assert.AreEqual(5, cue.BenchSlot);
            Assert.IsTrue(cue.IsSubstitute);
            Assert.IsTrue(cue.IsGoalkeeper);
        }

        [Test]
        public void AGoalkeeperSubstitution_MovesTheFrameCue_WhileTheBootAccessorGoesStale()
        {
            // The defect the per-frame flag exists for. A default engine's bench players are all
            // outfielders, so substituting a keeper off leaves that slot no longer a goalkeeper —
            // and the streamer's boot-time roster cache cannot know. Run for BOTH teams: a home-only
            // check would pass on an implementation that only ever re-sampled team 0.
            for (int team = 0; team < MatchEngineConstants.TEAM_COUNT; team++)
            {
                var engine   = new MatchEngine.MatchEngine(Seed);
                var streamer = new LiveMatchStreamer(engine);
                int keeperSlot = team * MatchEngineConstants.PLAYERS_PER_TEAM;

                LiveMatchFrame before = streamer.TickOnce();
                Assert.IsTrue(before.AgentCues[keeperSlot].IsGoalkeeper, "team " + team + " starts with a keeper");

                engine.SubstitutePlayer(team, keeperSlot, benchIndex: 0, SubstitutionReason.Tactical);
                LiveMatchFrame after = streamer.TickOnce();

                Assert.IsFalse(engine.AgentIsGoalkeeper(keeperSlot), "team " + team + ": the engine moved the flag");
                Assert.IsFalse(after.AgentCues[keeperSlot].IsGoalkeeper,
                    "team " + team + ": the frame cue must follow the engine, not the boot snapshot");
                Assert.IsTrue(streamer.IsGoalkeeper(keeperSlot),
                    "team " + team + ": the boot accessor is documented as boot-time and is expected to be stale here");
            }
        }

        [Test]
        public void CapturedFrame_CarriesPerTeamSubstitutionCounts()
        {
            var engine   = new MatchEngine.MatchEngine(Seed);
            var streamer = new LiveMatchStreamer(engine);

            LiveMatchFrame frame = streamer.TickOnce();

            Assert.AreEqual(MatchEngineConstants.TEAM_COUNT, frame.SubstitutionsUsed.Length);
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                Assert.AreEqual(engine.SubstitutionsUsed(t), frame.SubstitutionsUsed[t], "team " + t);
            }
        }

        [Test]
        public void CapturedFrame_CarriesTheDerivedPeriod()
        {
            var engine   = new MatchEngine.MatchEngine(Seed);
            var streamer = new LiveMatchStreamer(engine);

            LiveMatchFrame frame = streamer.TickOnce();

            Assert.AreEqual(MatchPeriod.FirstHalf, frame.Period);
            Assert.AreEqual(engine.CurrentPeriod, frame.Period);
        }

        [Test]
        public void BeforeAnyRestart_TheFrameReportsNone()
        {
            var streamer = new LiveMatchStreamer(new MatchEngine.MatchEngine(Seed));

            LiveMatchFrame frame = streamer.TickOnce();

            Assert.IsFalse(frame.Restart.HasRestart);
            Assert.AreEqual(RestartCue.None, frame.Restart.Cue);
            Assert.AreEqual(MatchEngineConstants.NO_RESTART_TEAM, frame.Restart.AwardedTeam);
            Assert.AreEqual(0UL, frame.Restart.Tick);
        }

        /// <summary>
        /// KD-P1-3, the property the design turns on. The engine reports a restart for exactly one tick;
        /// the streamer must hold it afterwards, or every consumer polling below the tick rate would see
        /// nothing at all. If this fails, the cross-tick memory has to move into the engine — and with it
        /// into the snapshot.
        /// </summary>
        [Test]
        public void RestartLatch_HoldsTheRestartAfterTheTickItHappenedOn()
        {
            var engine   = new MatchEngine.MatchEngine(Seed);
            var streamer = new LiveMatchStreamer(engine);

            RestartCue observedCue  = RestartCue.None;
            int        observedTeam = MatchEngineConstants.NO_RESTART_TEAM;
            ulong      observedTick = 0UL;

            for (int t = 0; t < RestartSearchTicks; t++)
            {
                streamer.TickOnce();
                if (engine.RestartAppliedThisTick != RestartCue.None)
                {
                    observedCue  = engine.RestartAppliedThisTick;
                    observedTeam = engine.RestartAwardedTeam;
                    observedTick = engine.CurrentTick;
                    break;
                }
            }

            Assert.AreNotEqual(RestartCue.None, observedCue,
                "no restart occurred within " + RestartSearchTicks + " ticks — this test would otherwise " +
                "pass vacuously; raise the bound or check the engine still produces restarts");

            // The engine has already forgotten it by the next tick...
            streamer.TickOnce();
            Assert.AreEqual(RestartCue.None, engine.RestartAppliedThisTick, "engine state is within-tick");

            // ...but the streamer has not, and keeps serving it on every later frame.
            Assert.IsTrue(streamer.TryGetLatestFrame(out LiveMatchFrame after));
            Assert.AreEqual(observedCue,  after.Restart.Cue,         "the latched cue must survive later ticks");
            Assert.AreEqual(observedTeam, after.Restart.AwardedTeam, "the awarded team is latched with the cue");
            Assert.AreEqual(observedTick, after.Restart.Tick,        "the latched tick is when it happened");
            Assert.Greater(after.Tick, after.Restart.Tick,
                "the frame is from a later tick than the restart it still reports");

            // A dozen more quiet ticks must not erode it — this is what a HUD fade timer reads.
            for (int t = 0; t < 12; t++) { streamer.TickOnce(); }
            Assert.IsTrue(streamer.TryGetLatestFrame(out LiveMatchFrame later));
            Assert.AreEqual(observedCue,  later.Restart.Cue);
            Assert.AreEqual(observedTick, later.Restart.Tick);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (P1 richer observation frame): per-agent cue  |
// |         |            |        | lockstep, per-team substitution counts, derived period, and    |
// |         |            |        | the streamer restart latch incl. its non-vacuity guard.        |
// | 1.1     | 2026-07-27 | —      | AR-1 M-6: the latch assertions read frame.Restart.Cue /        |
// |         |            |        | .AwardedTeam / .Tick off the RestartBanner carrier instead of  |
// |         |            |        | three loose frame fields. The no-restart case now asserts the  |
// |         |            |        | NO_RESTART_TEAM sentinel rather than a bare 0, which the       |
// |         |            |        | loose-field form could not distinguish from the home team.     |
// | 1.2     | 2026-08-03 | —      | P4a: the lockstep test also asserts the new cue                 |
// |         |            |        | IsGoalkeeper against the live engine; + a ctor field-mapping    |
// |         |            |        | guard (two ints and two bools) and the substitution test that   |
// |         |            |        | shows the cue following the engine while the streamer's         |
// |         |            |        | boot-time accessor goes stale — run for both teams.             |
#endregion
