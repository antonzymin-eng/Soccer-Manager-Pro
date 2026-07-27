// File:     src/ui-framework/Tests/MatchViewCueProjectionTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     UI / Client Framework #38 §2.2 (FR-UI-002/007/008, failure modes F1/F4/F5) +
//           interactive Unity client (docs/tracking/interactive-unity-client-design.md) §5-P1,
//           Code Standards #20
// Purpose:  Locks the P1 additions to the match view model to the same standard as the fields that
//           preceded them: the cue and substitution arrays are snapshots rather than windows onto the
//           producer's memory (F4), an incoherent frame fails loud rather than reaching a renderer
//           (F1), and the empty view stays usable (F5).

using System;

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;

namespace TacticalDirector.UiFramework.Tests
{
    [TestFixture]
    public sealed class MatchViewCueProjectionTests
    {
        private static Vector2[] Positions() => new Vector2[MatchEngineConstants.SQUAD_SIZE];

        private static LiveAgentCue[] Cues() => new LiveAgentCue[MatchEngineConstants.SQUAD_SIZE];

        private static int[] Subs() => new int[MatchEngineConstants.TEAM_COUNT];

        private static LiveMatchFrame Frame(
            Vector2[] positions = null,
            LiveAgentCue[] cues = null,
            int[] subs = null,
            MatchPeriod period = MatchPeriod.FirstHalf,
            RestartCue restart = RestartCue.None,
            int restartTeam = MatchEngineConstants.NO_RESTART_TEAM,
            ulong restartTick = 0UL) =>
            new LiveMatchFrame(
                1UL, new Vector3(52.5f, 34f, 0.11f), -1,
                positions ?? Positions(), 0, 0, false,
                cues ?? Cues(), subs ?? Subs(), period, restart, restartTeam, restartTick);

        // ── F4 — the view is a snapshot, not a window ────────────────────────────────────────────

        [Test]
        public void MutatingTheProducerCueArray_DoesNotChangeAProjectedView()
        {
            var cues = Cues();
            cues[3] = new LiveAgentCue(1, false, -1);
            var frame = Frame(cues: cues);
            var view = new MatchFrameView(in frame);

            int before = view.AgentCues[3].YellowCards;
            cues[3] = new LiveAgentCue(99, true, 7);     // the producer scribbles on its own array

            Assert.AreEqual(before, view.AgentCues[3].YellowCards,
                "the view copied the cue array; it must not alias the producer's buffer (F4)");
            Assert.IsFalse(view.AgentCues[3].IsSentOff);
        }

        [Test]
        public void MutatingTheProducerSubstitutionArray_DoesNotChangeAProjectedView()
        {
            var subs = Subs();
            subs[0] = 2;
            var frame = Frame(subs: subs);
            var view = new MatchFrameView(in frame);

            subs[0] = 99;

            Assert.AreEqual(2, view.SubstitutionsUsed[0],
                "the view copied the substitution counts; it must not alias the producer's buffer (F4)");
        }

        // ── F1 — incoherent frames fail loud ─────────────────────────────────────────────────────

        [Test]
        public void NullCueArray_Throws()
        {
            var frame = new LiveMatchFrame(
                1UL, Vector3.zero, -1, Positions(), 0, 0, false,
                null, Subs(), MatchPeriod.FirstHalf, RestartCue.None, -1, 0UL);

            Assert.Throws<ArgumentException>(() => { var _ = new MatchFrameView(in frame); });
        }

        /// <summary>
        /// The gate that matters most here: cues are indexed by the same roster index as positions, so a
        /// frame whose two arrays disagree cannot be drawn coherently. Discovering that in the render
        /// loop — as an IndexOutOfRange on some agent halfway through a repaint — is exactly the F1
        /// failure this constructor exists to convert into a loud one.
        /// </summary>
        [Test]
        public void CueCountNotMatchingPositionCount_Throws()
        {
            var frame = new LiveMatchFrame(
                1UL, Vector3.zero, -1, Positions(), 0, 0, false,
                new LiveAgentCue[MatchEngineConstants.SQUAD_SIZE - 1], Subs(),
                MatchPeriod.FirstHalf, RestartCue.None, -1, 0UL);

            Assert.Throws<ArgumentException>(() => { var _ = new MatchFrameView(in frame); });
        }

        [Test]
        public void NullSubstitutionArray_Throws()
        {
            var frame = new LiveMatchFrame(
                1UL, Vector3.zero, -1, Positions(), 0, 0, false,
                Cues(), null, MatchPeriod.FirstHalf, RestartCue.None, -1, 0UL);

            Assert.Throws<ArgumentException>(() => { var _ = new MatchFrameView(in frame); });
        }

        [Test]
        public void WrongSubstitutionCount_Throws()
        {
            var frame = new LiveMatchFrame(
                1UL, Vector3.zero, -1, Positions(), 0, 0, false,
                Cues(), new int[MatchEngineConstants.TEAM_COUNT + 1],
                MatchPeriod.FirstHalf, RestartCue.None, -1, 0UL);

            Assert.Throws<ArgumentException>(() => { var _ = new MatchFrameView(in frame); });
        }

        // ── The values a HUD actually binds ──────────────────────────────────────────────────────

        [Test]
        public void PeriodAndRestart_FlowThroughToTheView()
        {
            var frame = Frame(
                period: MatchPeriod.SecondHalf,
                restart: RestartCue.Corner,
                restartTeam: 1,
                restartTick: 4242UL);

            var view = new MatchFrameView(in frame);

            Assert.AreEqual(MatchPeriod.SecondHalf, view.Period);
            Assert.AreEqual(RestartCue.Corner, view.LastRestart);
            Assert.AreEqual(1, view.LastRestartTeam);
            Assert.AreEqual(4242UL, view.LastRestartTick);
        }

        [Test]
        public void CueValues_SurviveTheProjection()
        {
            var cues = Cues();
            cues[0] = new LiveAgentCue(1, false, -1);   // booked, still on, original starter
            cues[1] = new LiveAgentCue(1, true, -1);    // sent off
            cues[2] = new LiveAgentCue(0, false, 3);    // substitute from bench slot 3
            var frame = Frame(cues: cues);

            var view = new MatchFrameView(in frame);

            Assert.AreEqual(1, view.AgentCues[0].YellowCards);
            Assert.IsFalse(view.AgentCues[0].IsSubstitute);
            Assert.IsTrue(view.AgentCues[1].IsSentOff);
            Assert.IsTrue(view.AgentCues[2].IsSubstitute, "a non-negative bench slot means substituted on");
            Assert.AreEqual(3, view.AgentCues[2].BenchSlot);
        }

        // ── F5 — the empty view stays usable ─────────────────────────────────────────────────────

        [Test]
        public void EmptyView_ExposesEmptyCollections_NotNull()
        {
            MatchFrameView empty = MatchFrameView.Empty;

            Assert.IsTrue(empty.IsEmpty);
            Assert.IsNotNull(empty.AgentCues, "a screen binds these before the first frame arrives");
            Assert.AreEqual(0, empty.AgentCues.Count);
            Assert.IsNotNull(empty.SubstitutionsUsed);
            Assert.AreEqual(0, empty.SubstitutionsUsed.Count);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (P1 richer observation frame): F4 copy locks  |
// |         |            |        | for the cue / substitution arrays, F1 gates (null + length     |
// |         |            |        | coherence for both), period / restart pass-through, and the    |
// |         |            |        | F5 empty-view collections.                                     |
#endregion
