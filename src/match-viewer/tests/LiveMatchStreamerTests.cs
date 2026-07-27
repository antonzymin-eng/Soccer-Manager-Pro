// File:     src/match-viewer/tests/LiveMatchStreamerTests.cs
// Created:  2026-07-15
// Modified: 2026-07-27 (P1: the hand-built full-time frame carries the P1 fields. Cue/latch coverage
//           lives in its own fixture — LiveMatchFrameCueTests. AR-1 M-6: score reads move onto the
//           Scoreline carrier.)
// Author:   —
// Spec:     Interactive match view (docs/tracking/interactive-match-view-design.md), Testing Strategy #19 (unit layer), Code Standards #20
// Purpose:  Contract tests for LiveMatchStreamer: latest-frame handoff, observer neutrality
//           (streaming does not perturb the engine digest), full-time auto-pause, speed-multiplier
//           guards, single-use lifecycle guards, roster accessors, and a minimal threaded
//           start/stop smoke test. Deterministic tests drive TickOnce() directly (no wall-clock
//           dependency); only the lifecycle smoke test tolerates real timing.

using System;
using System.Threading;

using NUnit.Framework;

using TacticalDirector.MatchEngine;
using UnityEngine;

namespace TacticalDirector.MatchViewer.Tests
{
    [TestFixture]
    public class LiveMatchStreamerTests
    {
        private const ulong Seed = 777UL;

        [Test]
        public void Ctor_GuardsFailLoud()
        {
            Assert.Throws<ArgumentNullException>(() => new LiveMatchStreamer(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new LiveMatchStreamer(new MatchEngine.MatchEngine(Seed), 0));
        }

        [Test]
        public void TryGetLatestFrame_FalseBeforeFirstTick()
        {
            var streamer = new LiveMatchStreamer(new MatchEngine.MatchEngine(Seed));
            Assert.IsFalse(streamer.TryGetLatestFrame(out _));
        }

        [Test]
        public void TickOnce_CapturesFrame_MatchingEngineObservationSurface()
        {
            var engine = new MatchEngine.MatchEngine(Seed);
            var streamer = new LiveMatchStreamer(engine);

            LiveMatchFrame frame = streamer.TickOnce();

            Assert.AreEqual(engine.CurrentTick, frame.Tick);
            Assert.AreEqual(engine.PossessingAgentId, frame.PossessingAgentId);
            Assert.AreEqual(engine.HomeScore, frame.Score.Home);
            Assert.AreEqual(engine.AwayScore, frame.Score.Away);
            Assert.AreEqual(engine.MatchEnded, frame.MatchEnded);
            Assert.AreEqual(MatchEngineConstants.SQUAD_SIZE, frame.AgentPositions.Length);

            Assert.IsTrue(streamer.TryGetLatestFrame(out LiveMatchFrame latest));
            Assert.AreEqual(frame.Tick, latest.Tick);
        }

        [Test]
        public void TickOnce_IsObserverNeutral_DigestMatchesUnobservedRun()
        {
            // The streamer's TickOnce() must read only the public observation surface — a streamed
            // run's final digest must be byte-identical to an unobserved run of the same seed
            // (the same invariant MatchReplayRecorder already locks).
            var observed = new MatchEngine.MatchEngine(Seed);
            var streamer = new LiveMatchStreamer(observed);
            for (int i = 0; i < 120; i++) { streamer.TickOnce(); }

            var unobserved = new MatchEngine.MatchEngine(Seed);
            for (int i = 0; i < 120; i++) { unobserved.RunTick(); }

            CollectionAssert.AreEqual(unobserved.CurrentSnapshotDigest, observed.CurrentSnapshotDigest);
        }

        [Test]
        public void ApplyCapturedFrame_AtFullTime_AutoPauses()
        {
            // Reaching a real MatchEnded=true requires ~324,000 real ticks (MATCH_TICKS_TOTAL) or
            // an engine-internal test seam this assembly does not have InternalsVisibleTo access to
            // — ApplyCapturedFrame lets this unit test the streamer's own auto-pause decision
            // directly against a hand-built frame instead.
            var streamer = new LiveMatchStreamer(new MatchEngine.MatchEngine(Seed));
            Assert.IsFalse(streamer.IsPaused);

            var endedFrame = new LiveMatchFrame(
                1UL, Vector3.zero, -1,
                new Vector2[MatchEngineConstants.SQUAD_SIZE],
                new LiveAgentCue[MatchEngineConstants.SQUAD_SIZE],
                new int[MatchEngineConstants.TEAM_COUNT],
                new Scoreline(0, 0),
                matchEnded: true,
                period:  MatchPeriod.FullTime,
                restart: RestartBanner.None);
            streamer.ApplyCapturedFrame(endedFrame);

            Assert.IsTrue(streamer.IsPaused, "MatchEnded must auto-pause the streamer.");

            // Resume() clears the auto-pause too (harmless — the engine stays frozen regardless).
            streamer.Resume();
            Assert.IsFalse(streamer.IsPaused);
        }

        [Test]
        public void SetSpeedMultiplier_RejectsNonFiniteAndOutOfRange()
        {
            var streamer = new LiveMatchStreamer(new MatchEngine.MatchEngine(Seed));

            Assert.Throws<ArgumentOutOfRangeException>(() => streamer.SetSpeedMultiplier(0f));
            Assert.Throws<ArgumentOutOfRangeException>(() => streamer.SetSpeedMultiplier(-1f));
            Assert.Throws<ArgumentOutOfRangeException>(() => streamer.SetSpeedMultiplier(float.NaN));
            Assert.Throws<ArgumentOutOfRangeException>(() => streamer.SetSpeedMultiplier(float.PositiveInfinity));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => streamer.SetSpeedMultiplier(MatchViewerConstants.MaxLiveSpeedMultiplier + 1f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => streamer.SetSpeedMultiplier(MatchViewerConstants.MinLiveSpeedMultiplier - 0.01f));

            streamer.SetSpeedMultiplier(2f);
            Assert.AreEqual(2f, streamer.SpeedMultiplier);
        }

        [Test]
        public void RosterAccessors_MirrorEngine_AndGuardFailLoud()
        {
            var engine = new MatchEngine.MatchEngine(Seed);
            var streamer = new LiveMatchStreamer(engine);

            Assert.AreEqual(MatchEngineConstants.SQUAD_SIZE, streamer.AgentCount);
            for (int i = 0; i < streamer.AgentCount; i++)
            {
                Assert.AreEqual(engine.AgentTeamId(i), streamer.TeamId(i));
                Assert.AreEqual(engine.AgentIsGoalkeeper(i), streamer.IsGoalkeeper(i));
            }
            Assert.AreEqual(MatchEngineConstants.PITCH_LENGTH_M, streamer.PitchLengthM);
            Assert.AreEqual(MatchEngineConstants.PITCH_WIDTH_M, streamer.PitchWidthM);

            Assert.Throws<ArgumentOutOfRangeException>(() => streamer.TeamId(-1));
            Assert.Throws<ArgumentOutOfRangeException>(() => streamer.IsGoalkeeper(streamer.AgentCount));
        }

        [Test]
        public void Start_TwiceIsNoOp_AndAfterStopThrows()
        {
            var streamer = new LiveMatchStreamer(new MatchEngine.MatchEngine(Seed));
            streamer.Start();
            Assert.DoesNotThrow(() => streamer.Start(), "A second Start() while running must be a no-op.");
            streamer.Stop();
            Assert.Throws<InvalidOperationException>(() => streamer.Start());
        }

        [Test]
        public void Stop_BeforeStart_IsNoOp()
        {
            var streamer = new LiveMatchStreamer(new MatchEngine.MatchEngine(Seed));
            Assert.DoesNotThrow(() => streamer.Stop());
            Assert.DoesNotThrow(() => streamer.Stop(), "Stop() must be idempotent.");
        }

        [Test]
        public void ThreadedLifecycle_StartRunsThenStopJoinsCleanly()
        {
            // Wall-clock-tolerant smoke test: asserts the pacing thread actually advances the
            // engine and that Stop() returns promptly (bounded by LivePausedPollIntervalMs), not
            // an exact tick count (real timing is inherently non-deterministic in CI).
            var streamer = new LiveMatchStreamer(new MatchEngine.MatchEngine(Seed));
            streamer.Start();
            Thread.Sleep(200);
            streamer.Stop();

            Assert.IsTrue(streamer.TryGetLatestFrame(out LiveMatchFrame frame));
            Assert.Greater(frame.Tick, 0UL, "The pacing thread must have advanced the engine at least once in 200 ms.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-15 | —      | Initial creation: latest-frame handoff, observer-neutrality     |
// |         |            |        | digest lock, full-time auto-pause, speed-multiplier guards,     |
// |         |            |        | single-use lifecycle guards, roster accessors, threaded         |
// |         |            |        | start/stop smoke test.                                          |
// | 1.1     | 2026-07-27 | —      | P1 + AR-1 M-6: the hand-built full-time frame carries the P1   |
// |         |            |        | fields, and the score assertions read frame.Score.Home /       |
// |         |            |        | .Away off the Scoreline carrier. Cue and restart-latch         |
// |         |            |        | coverage lives in LiveMatchFrameCueTests.                      |
#endregion
