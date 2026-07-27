// File:     src/ui-framework/Tests/MatchViewProjectionTests.cs
// Created:  2026-07-25
// Modified: 2026-07-27 (P1: gate probes routed through a local Frame helper; CaptureFrom samples the
//           P1 surface, now via the Scoreline / RestartBanner carriers. The P1 gates get their own
//           fixture — MatchViewCueProjectionTests.)
// Author:   —
// Spec:     UI / Client Framework #38 §3.1 / §3.4 / §5.1 (T-UI-MATCHVIEW-001/002, T-UI-FAIL-001/002,
//           T-UI-LAYER-002, FR-UI-002/005/006/007/008/015/016), Code Standards #20
// Purpose:  Locks the match-view projection: the view is a snapshot rather than a window onto the
//           producer's memory, malformed frames fail loud instead of reaching a renderer, and the
//           pre-first-frame case renders empty rather than throwing or forcing a tick.

using System;

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;
using TacticalDirector.UiFramework;

namespace TacticalDirector.UiFramework.Tests
{
    [TestFixture]
    public sealed class MatchViewProjectionTests
    {
        // ── T-UI-MATCHVIEW-002 / F5 — nothing published yet ──────────────────────────────────────

        [Test]
        public void ProjectBeforeAnyFrame_ReturnsEmpty_DoesNotThrow()
        {
            var frames = new StubFrameSource();
            var source = new MatchViewModelSource(frames);

            MatchFrameView view = source.Project();

            Assert.IsTrue(view.IsEmpty);
            Assert.AreEqual(0, view.AgentPositions.Count, "an empty view still has a usable positions list");
            Assert.AreEqual(0, frames.PublishCount, "projecting did not manufacture a frame");
        }

        [Test]
        public void ProjectAfterFramesStop_ReturnsLastKnown_NotEmpty()
        {
            var frames = new StubFrameSource();
            var source = new MatchViewModelSource(frames);
            frames.Publish(MakeFrame(tick: 42UL));

            MatchFrameView first = source.Project();
            frames.Clear();                       // producer went quiet
            MatchFrameView second = source.Project();

            Assert.IsFalse(second.IsEmpty);
            Assert.AreEqual(first.Tick, second.Tick, "last-known is retained (F5), not reset to empty");
        }

        // ── T-UI-MATCHVIEW-001 — pure observation ────────────────────────────────────────────────

        [Test]
        public void Project_DoesNotAdvanceTheSim()
        {
            var engine = new MatchEngine.MatchEngine(0xABCDEFUL);
            var frames = new StubFrameSource();
            var source = new MatchViewModelSource(frames);
            frames.Publish(CaptureFrom(engine));

            ulong before = engine.CurrentTick;
            for (int i = 0; i < 50; i++) { source.Project(); }

            Assert.AreEqual(before, engine.CurrentTick,
                "the source holds no engine and must not be able to advance one");
        }

        // ── T-UI-LAYER-002 / F4 — the view is a snapshot, not a window ───────────────────────────

        [Test]
        public void ViewModel_IsAValueType()
        {
            Assert.IsTrue(typeof(MatchFrameView).IsValueType);
        }

        [Test]
        public void MutatingTheProducerArray_DoesNotChangeAProjectedView()
        {
            var positions = NewPositions();
            var frame = Frame(1UL, new Vector3(50f, 34f, 0.11f), -1, positions, 0, 0, false);
            var view = new MatchFrameView(in frame);

            Vector2 before = view.AgentPositions[0];
            positions[0] = new Vector2(999f, 999f);      // the producer scribbles on its own array

            Assert.AreEqual(before, view.AgentPositions[0],
                "the view copied the array; it must not alias the producer's buffer (F4)");
        }

        // ── T-UI-FAIL-001 / 002 — F1 fail-loud gates ─────────────────────────────────────────────

        [Test]
        public void WrongAgentCount_Throws()
        {
            var frame = Frame(1UL, Vector3.zero, -1, new Vector2[3], 0, 0, false);
            Assert.Throws<ArgumentException>(() => { var _ = new MatchFrameView(in frame); });
        }

        [Test]
        public void NullAgentArray_Throws()
        {
            var frame = Frame(1UL, Vector3.zero, -1, null, 0, 0, false);
            Assert.Throws<ArgumentException>(() => { var _ = new MatchFrameView(in frame); });
        }

        [Test]
        public void PossessingAgentIdOutOfRange_Throws()
        {
            var tooHigh = Frame(1UL, Vector3.zero, MatchEngineConstants.SQUAD_SIZE, NewPositions(), 0, 0, false);
            var tooLow = Frame(1UL, Vector3.zero, -2, NewPositions(), 0, 0, false);

            Assert.Throws<ArgumentException>(() => { var _ = new MatchFrameView(in tooHigh); });
            Assert.Throws<ArgumentException>(() => { var _ = new MatchFrameView(in tooLow); });
        }

        [Test]
        public void LooseBallSentinel_IsAccepted()
        {
            var frame = Frame(1UL, Vector3.zero, -1, NewPositions(), 0, 0, false);
            Assert.DoesNotThrow(() => { var _ = new MatchFrameView(in frame); });
        }

        [Test]
        public void NonFiniteBallCoordinate_Throws()
        {
            var nan = Frame(1UL, new Vector3(float.NaN, 0f, 0f), -1, NewPositions(), 0, 0, false);
            var inf = Frame(1UL, new Vector3(0f, float.PositiveInfinity, 0f), -1, NewPositions(), 0, 0, false);

            Assert.Throws<ArgumentException>(() => { var _ = new MatchFrameView(in nan); });
            Assert.Throws<ArgumentException>(() => { var _ = new MatchFrameView(in inf);  });
        }

        [Test]
        public void NonFiniteAgentCoordinate_Throws()
        {
            var positions = NewPositions();
            positions[13] = new Vector2(float.NaN, 0f);
            var frame = Frame(1UL, Vector3.zero, -1, positions, 0, 0, false);

            Assert.Throws<ArgumentException>(() => { var _ = new MatchFrameView(in frame); });
        }

        [Test]
        public void NegativeScore_Throws()
        {
            // AR-1 M-6: the gate moved into Scoreline, so a negative score can no longer reach a frame
            // at all — it is refused one layer earlier, which is strictly better than re-checking it here.
            Assert.Throws<ArgumentOutOfRangeException>(() => new Scoreline(-1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new Scoreline(0, -1));
        }

        [Test]
        public void MalformedFrame_DoesNotPoisonTheLastKnownCache()
        {
            var frames = new StubFrameSource();
            var source = new MatchViewModelSource(frames);
            frames.Publish(MakeFrame(tick: 7UL));
            source.Project();

            var bad = NewPositions();
            bad[0] = new Vector2(float.NaN, 0f);
            frames.Publish(Frame(8UL, Vector3.zero, -1, bad, 0, 0, false));

            Assert.Throws<ArgumentException>(() => source.Project());

            frames.Clear();
            MatchFrameView after = source.Project();
            Assert.AreEqual(7UL, after.Tick, "the cache still holds the last GOOD frame");
        }

        [Test]
        public void Constructor_RejectsNullSources()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchViewModelSource((ILiveFrameSource)null));
            Assert.Throws<ArgumentNullException>(() => new MatchViewModelSource((LiveMatchStreamer)null));
            Assert.Throws<ArgumentNullException>(() => new LiveMatchStreamerFrameSource(null));
        }

        // ── helpers ──────────────────────────────────────────────────────────────────────────────

        internal static Vector2[] NewPositions() => new Vector2[MatchEngineConstants.SQUAD_SIZE];

        /// <summary>
        /// Builds a frame with the pre-P1 fields the gate probes below actually vary, filling the P1
        /// additions (per-agent cues, substitution counts, period, last restart) with valid neutral
        /// values. Keeping them in one place is why extending the frame again costs one line here
        /// rather than one per probe — and each P1 gate gets its own explicit test instead of riding
        /// along in a probe about something else.
        /// </summary>
        private static LiveMatchFrame Frame(
            ulong tick, Vector3 ball, int possessing, Vector2[] positions,
            int homeScore, int awayScore, bool matchEnded)
        {
            int cueCount = positions == null ? MatchEngineConstants.SQUAD_SIZE : positions.Length;
            return new LiveMatchFrame(
                tick, ball, possessing, positions,
                new LiveAgentCue[cueCount],
                new int[MatchEngineConstants.TEAM_COUNT],
                new Scoreline(homeScore, awayScore),
                matchEnded,
                MatchPeriod.FirstHalf,
                RestartBanner.None);
        }

        internal static LiveMatchFrame MakeFrame(ulong tick) =>
            Frame(tick, new Vector3(52.5f, 34f, 0.11f), -1, NewPositions(), 0, 0, false);

        /// <summary>Builds a frame from the engine's PUBLIC observation surface — the same values the streamer samples.</summary>
        internal static LiveMatchFrame CaptureFrom(MatchEngine.MatchEngine engine)
        {
            var positions = new Vector2[MatchEngineConstants.SQUAD_SIZE];
            var cues      = new LiveAgentCue[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < positions.Length; i++)
            {
                positions[i] = engine.AgentView(i).Position;
                cues[i]      = new LiveAgentCue(
                    engine.AgentYellowCards(i), engine.AgentIsSentOff(i), engine.AgentBenchSlot(i));
            }

            var subs = new int[MatchEngineConstants.TEAM_COUNT];
            for (int t = 0; t < subs.Length; t++) { subs[t] = engine.SubstitutionsUsed(t); }

            RestartBanner restart = engine.RestartAppliedThisTick == RestartCue.None
                ? RestartBanner.None
                : new RestartBanner(engine.RestartAppliedThisTick, engine.RestartAwardedTeam, engine.CurrentTick);

            return new LiveMatchFrame(
                engine.CurrentTick,
                engine.BallView.Position,
                engine.PossessingAgentId,
                positions,
                cues,
                subs,
                new Scoreline(engine.HomeScore, engine.AwayScore),
                engine.MatchEnded,
                engine.CurrentPeriod,
                restart);
        }

        /// <summary>A deterministic stand-in for the streamer's publish/read handoff — no threads, no pacing.</summary>
        internal sealed class StubFrameSource : ILiveFrameSource
        {
            private LiveMatchFrame _frame;
            private bool _has;

            public int PublishCount { get; private set; }

            public void Publish(in LiveMatchFrame frame)
            {
                _frame = frame;
                _has = true;
                PublishCount++;
            }

            public void Clear() => _has = false;

            public bool TryGetLatestFrame(out LiveMatchFrame frame)
            {
                frame = _frame;
                return _has;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-25 | —      | Initial creation: T-UI-MATCHVIEW-001/002, T-UI-FAIL-001/002,   |
// |         |            |        | T-UI-LAYER-002 — snapshot-not-window, every F1 gate, the F5    |
// |         |            |        | empty/last-known path, and the cache-not-poisoned-by-a-bad-    |
// |         |            |        | frame lock.                                                    |
// | 1.1     | 2026-07-27 | —      | P1 + AR-1 M-6. Gate probes route through a local Frame helper  |
// |         |            |        | that fills the P1 additions with valid neutral values, so      |
// |         |            |        | extending the frame costs one line here rather than one per    |
// |         |            |        | probe. NegativeScore_Throws now tests Scoreline directly — the |
// |         |            |        | gate moved there, so a negative score cannot reach a frame.    |
#endregion
