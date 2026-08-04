// File:     src/ui-framework/Tests/MatchViewCueProjectionTests.cs
// Created:  2026-07-27
// Modified: 2026-08-03 (P4a: cue constructions carry the new IsGoalkeeper argument)
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
            RestartBanner restart = default) =>
            new LiveMatchFrame(
                1UL, new Vector3(52.5f, 34f, 0.11f), -1,
                positions ?? Positions(), cues ?? Cues(), subs ?? Subs(),
                new Scoreline(0, 0), false, period, restart);

        // ── F4 — the view is a snapshot, not a window ────────────────────────────────────────────

        [Test]
        public void MutatingTheProducerCueArray_DoesNotChangeAProjectedView()
        {
            var cues = Cues();
            cues[3] = new LiveAgentCue(1, false, -1, false);
            var frame = Frame(cues: cues);
            var view = new MatchFrameView(in frame);

            int before = view.AgentCues[3].YellowCards;
            cues[3] = new LiveAgentCue(99, true, 7, true);     // the producer scribbles on its own array

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
                1UL, Vector3.zero, -1, Positions(), null, Subs(),
                new Scoreline(0, 0), false, MatchPeriod.FirstHalf, RestartBanner.None);

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
                1UL, Vector3.zero, -1, Positions(),
                new LiveAgentCue[MatchEngineConstants.SQUAD_SIZE - 1], Subs(),
                new Scoreline(0, 0), false, MatchPeriod.FirstHalf, RestartBanner.None);

            Assert.Throws<ArgumentException>(() => { var _ = new MatchFrameView(in frame); });
        }

        [Test]
        public void NullSubstitutionArray_Throws()
        {
            var frame = new LiveMatchFrame(
                1UL, Vector3.zero, -1, Positions(), Cues(), null,
                new Scoreline(0, 0), false, MatchPeriod.FirstHalf, RestartBanner.None);

            Assert.Throws<ArgumentException>(() => { var _ = new MatchFrameView(in frame); });
        }

        [Test]
        public void WrongSubstitutionCount_Throws()
        {
            var frame = new LiveMatchFrame(
                1UL, Vector3.zero, -1, Positions(), Cues(),
                new int[MatchEngineConstants.TEAM_COUNT + 1],
                new Scoreline(0, 0), false, MatchPeriod.FirstHalf, RestartBanner.None);

            Assert.Throws<ArgumentException>(() => { var _ = new MatchFrameView(in frame); });
        }

        // ── The values a HUD actually binds ──────────────────────────────────────────────────────

        [Test]
        public void PeriodAndRestart_FlowThroughToTheView()
        {
            var frame = Frame(
                period: MatchPeriod.SecondHalf,
                restart: new RestartBanner(RestartCue.Corner, awardedTeam: 1, tick: 4242UL));

            var view = new MatchFrameView(in frame);

            Assert.AreEqual(MatchPeriod.SecondHalf, view.Period);
            Assert.IsTrue(view.Restart.HasRestart);
            Assert.AreEqual(RestartCue.Corner, view.Restart.Cue);
            Assert.AreEqual(1, view.Restart.AwardedTeam);
            Assert.AreEqual(4242UL, view.Restart.Tick);
        }

        /// <summary>
        /// AR-1 M-3, closed structurally. The empty view is <c>default(MatchFrameView)</c>, so its banner
        /// is <c>default(RestartBanner)</c> — and because the banner DERIVES its team and tick from its
        /// own cue rather than storing them bare, a zeroed struct reports −1 / 0 instead of "home team,
        /// tick 0". The v1.1 loose-field form got this wrong one layer up from where it had already been
        /// found and fixed once.
        /// </summary>
        [Test]
        public void EmptyView_ReportsTheNoRestartSentinel_NotTeamZero()
        {
            MatchFrameView empty = MatchFrameView.Empty;

            Assert.IsFalse(empty.Restart.HasRestart);
            Assert.AreEqual(RestartCue.None, empty.Restart.Cue);
            Assert.AreEqual(MatchEngineConstants.NO_RESTART_TEAM, empty.Restart.AwardedTeam);
            Assert.AreEqual(0UL, empty.Restart.Tick);
        }

        [Test]
        public void RestartBanner_RefusesTheNoneCueAndAnUnknownTeam()
        {
            Assert.Throws<ArgumentException>(
                () => new RestartBanner(RestartCue.None, 0, 1UL),
                "None must have exactly one representation — RestartBanner.None");
            Assert.Throws<ArgumentOutOfRangeException>(() => new RestartBanner(RestartCue.ThrowIn, -1, 1UL));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new RestartBanner(RestartCue.ThrowIn, MatchEngineConstants.TEAM_COUNT, 1UL));

            // AR-2: a cast-from-int cue clears the None check but names no restart kind, so a View would
            // caption something that does not exist. Rejected rather than stored.
            Assert.Throws<ArgumentOutOfRangeException>(() => new RestartBanner((RestartCue)99, 0, 1UL));
        }

        [Test]
        public void CueValues_SurviveTheProjection()
        {
            var cues = Cues();
            cues[0] = new LiveAgentCue(1, false, -1, false);   // booked, still on, original starter
            cues[1] = new LiveAgentCue(1, true, -1, false);    // sent off
            cues[2] = new LiveAgentCue(0, false, 3, false);    // substitute from bench slot 3
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
// | 1.1     | 2026-07-27 | —      | AR-1 M-3 / M-6: + EmptyView_ReportsTheNoRestartSentinel (the   |
// |         |            |        | zero-value trap, one layer up from where it had already been   |
// |         |            |        | found and fixed once) and RestartBanner_RefusesTheNoneCueAnd-  |
// |         |            |        | AnUnknownTeam. Gate probes route through a local Frame helper  |
// |         |            |        | so the carrier change cost one line, not one per probe.        |
// | 1.2     | 2026-08-03 | —      | P4a: the five LiveAgentCue constructions carry the new         |
// |         |            |        | IsGoalkeeper argument (KD-P4a-1). The scribble-on-the-         |
// |         |            |        | producer-array case flips it too, so the copy lock covers the  |
// |         |            |        | new field rather than only the ones it already had.            |
#endregion
