// File:     src/client-app/tests/S0DemoFixtureComparisonTests.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §7, S0 binding contracts §2.1, Code Standards #20
// Purpose:  Complete-match fixture repeatability, observer neutrality and former no-squad comparison.

using System;
using System.Globalization;

using NUnit.Framework;

using TacticalDirector.MatchAnalytics;
using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    [NonParallelizable]
    public sealed class S0DemoFixtureComparisonTests
    {
        private readonly struct Outcome
        {
            internal readonly LiveMatchFrame FinalFrame;
            internal readonly byte[] Digest;
            internal readonly MatchAnalyticsPublication Analytics;

            internal Outcome(in LiveMatchFrame frame, byte[] digest, in MatchAnalyticsPublication analytics)
            {
                FinalFrame = frame;
                Digest = digest;
                Analytics = analytics;
            }
        }

        [Test]
        public void sim_ApprovedFixtureFullMatchIsRepeatableAndObserverNeutral()
        {
            S0DemoFixture fixture = S0DemoFixture.CreateApproved();
            MatchSetup setup = fixture.BuildSetup(Mentality.Balanced);
            TestContext.WriteLine("P5B_FULL_MATCH fixture=" + S0DemoConstants.FIXTURE_REVISION +
                " sha256=" + fixture.ContentSha256 +
                " seed=1 homeMentality=Balanced homeManager=Human awayManager=AI awayProfile=0 heading=false commands=none");

            Outcome first = Run(setup, false);
            WriteOutcome("approved-unobserved", in first);
            Outcome repeat = Run(S0DemoFixture.CreateApproved().BuildSetup(Mentality.Balanced), false);
            WriteOutcome("approved-repeat", in repeat);
            AssertSameMatch(in first, in repeat, "independently constructed approved fixture");

            Outcome observed = Run(setup, true);
            WriteOutcome("approved-observed", in observed);
            AssertSameMatch(in first, in observed, "analytics observation");

            var formerSetup = new MatchSetup(setup.Seed, homeTactic: setup.HomeTactic,
                awayTactic: setup.AwayTactic, homeManagerMode: setup.HomeManagerMode,
                awayManagerMode: setup.AwayManagerMode, homeManagerProfile: setup.HomeManagerProfile,
                awayManagerProfile: setup.AwayManagerProfile, gkHeadingEnabled: setup.GkHeadingEnabled);
            Outcome former = Run(formerSetup, true);
            WriteOutcome("former-no-squad-observed", in former);
            Assert.AreEqual(first.FinalFrame.Tick, former.FinalFrame.Tick, "equivalent complete-match horizon");
            // Different squads are deliberately different inputs. Record outcomes without imposing
            // a goal-rate target, rebasing a frozen digest, or weakening any calibration predicate.
            TestContext.WriteLine("P5B_FULL_MATCH PASS repeatability=true observerNeutrality=true comparisonComplete=true");
        }

        private static Outcome Run(MatchSetup setup, bool observe)
        {
            var session = new MatchSession(setup);
            SessionMatchAnalytics adapter = observe ? SessionMatchAnalytics.Attach(session) : null;
            try
            {
                LiveMatchFrame frame = default;
                // Full time is evaluated against the entering tick; its captured frame is the
                // next completed tick. This guard detects a missing real end transition.
                ulong lastTick = (ulong)MatchEngineConstants.MATCH_TICKS_TOTAL + 1UL;
                while (session.CurrentTick < lastTick && !frame.MatchEnded)
                {
                    frame = session.TickOnce();
                }

                Assert.IsTrue(frame.MatchEnded, "real full time must occur without a test-only transition");
                Assert.AreEqual(lastTick, frame.Tick);
                Assert.AreEqual(0, session.Driver.Log.Count, "comparison adds no manager commands");
                Assert.AreEqual(0, session.Driver.FailedCommands.Count);
                Assert.IsNull(session.Streamer.PostTickObserverFault);
                MatchAnalyticsPublication analytics = adapter == null ? default : adapter.Publish();
                if (observe)
                {
                    Assert.IsFalse(analytics.IsIncomplete);
                    Assert.IsTrue(analytics.Result.HasValue);
                    Assert.AreEqual(frame.Tick, analytics.AttemptedTick);
                    Assert.AreEqual(frame.Tick, analytics.CompletedTick);
                    Assert.AreEqual(frame.Tick, analytics.ResultThroughTick);
                    Assert.AreEqual((long)frame.Tick, analytics.ObservedTicks);
                    Assert.AreEqual(frame.Score.Home, analytics.Result.Value.Home.Goals);
                    Assert.AreEqual(frame.Score.Away, analytics.Result.Value.Away.Goals);
                }
                return new Outcome(in frame, session.CurrentSnapshotDigest, in analytics);
            }
            finally
            {
                session.Stop();
            }
        }

        private static void AssertSameMatch(in Outcome expected, in Outcome actual, string reason)
        {
            CollectionAssert.AreEqual(expected.Digest, actual.Digest, reason + " must preserve the final chained digest");
            Assert.AreEqual(expected.FinalFrame.Tick, actual.FinalFrame.Tick, reason);
            Assert.AreEqual(expected.FinalFrame.Score.Home, actual.FinalFrame.Score.Home, reason);
            Assert.AreEqual(expected.FinalFrame.Score.Away, actual.FinalFrame.Score.Away, reason);
        }

        private static void WriteOutcome(string label, in Outcome outcome)
        {
            TestContext.WriteLine("P5B_FULL_MATCH run=" + label +
                " ticks=" + outcome.FinalFrame.Tick.ToString(CultureInfo.InvariantCulture) +
                " score=" + outcome.FinalFrame.Score.Home.ToString(CultureInfo.InvariantCulture) + "-" +
                outcome.FinalFrame.Score.Away.ToString(CultureInfo.InvariantCulture) +
                " digest=" + BitConverter.ToString(outcome.Digest).Replace("-", ""));
            if (!outcome.Analytics.Result.HasValue) return;
            MatchAnalyticsResult result = outcome.Analytics.Result.Value;
            WriteTeam(label, in result.Home, in result.HomeAdvanced);
            WriteTeam(label, in result.Away, in result.AwayAdvanced);
        }

        private static void WriteTeam(string label, in MatchStatline basic, in AdvancedStatline advanced)
        {
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "P5B_FULL_MATCH run={0} team={1} goals={2} possessionPercent={3:R} fouls={4} yellow={5} red={6} " +
                "offsides={7} corners={8} throwIns={9} goalKicks={10} substitutions={11} territoryPercent={12:R} " +
                "liveXgAvailable={13} xg={14:R}", label, basic.TeamId, basic.Goals, basic.PossessionSharePercent,
                basic.Fouls, basic.YellowCards, basic.RedCards, basic.Offsides, basic.Corners, basic.ThrowIns,
                basic.GoalKicks, basic.Substitutions, advanced.TerritorialPercent, advanced.LiveXgAvailable,
                advanced.XgSum));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Real full matches for fixture repeatability, observer neutrality and prior setup outcomes. |
#endregion
