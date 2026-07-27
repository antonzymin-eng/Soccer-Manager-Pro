// File:     src/match-client-web/tests/MatchClientHostTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Path-to-playable roadmap §7 (B6); Match Analytics #37 §3.5 / §4.4; interactive Unity
//           client §5-P2; Code Standards #20
// Purpose:  Locks the composition itself — that the statistics pump really sees every tick of a
//           running match (#37 F6 makes that self-checking), that a fault in it cannot kill the sim
//           thread, and that a queued intent reaches the engine through the tick boundary.

using System;
using System.Diagnostics;
using System.Threading;

using NUnit.Framework;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.UiFramework;

namespace TacticalDirector.MatchClientWeb.Tests
{
    /// <summary>Composition tests over a really-running match (see file header).</summary>
    ///
    /// <remarks>
    /// These start the pacing thread, so they run at an elevated speed multiplier and poll for a tick
    /// count rather than sleeping a fixed time. The EventBus is process-static (#17 §3.2.1), so like
    /// every other composed match test these run sequentially, never interleaved.
    /// </remarks>
    [TestFixture]
    public sealed class MatchClientHostTests
    {
        private const ulong Seed = 0x00C11E7B6D0CUL;

        /// <summary>Runs the session until it has observed <paramref name="ticks"/> ticks, or fails.</summary>
        private static void RunUntilObserved(MatchClientHost host, long ticks, int timeoutMs = 20_000)
        {
            host.Session.Streamer.SetSpeedMultiplier(MatchViewerConstants.MaxLiveSpeedMultiplier);
            host.Start();
            try
            {
                var sw = Stopwatch.StartNew();
                while (host.ObservedTicks < ticks)
                {
                    if (sw.ElapsedMilliseconds > timeoutMs)
                    {
                        Assert.Fail(
                            "Only " + host.ObservedTicks + " of " + ticks + " ticks were observed in " +
                            timeoutMs + " ms. Fault: " + (host.AnalyticsFault?.Message ?? "none"));
                    }
                    Thread.Sleep(5);
                }
            }
            finally
            {
                host.Stop();
            }
        }

        [Test]
        public void AnalyticsPump_SeesEveryTick_WhichTheAggregatorItselfEnforces()
        {
            var host = new MatchClientHost(MatchSetup.NeutralDemo(Seed));
            RunUntilObserved(host, 300);

            // #37 F6 throws on a non-consecutive tick, and the streamer latches such a throw rather
            // than letting it kill the sim thread — so a null fault after 300 ticks IS the proof that
            // the observer fired exactly once per tick, in order, with no gap.
            Assert.IsNull(host.AnalyticsFault, "A gap or a double-fire would have latched here.");
            Assert.GreaterOrEqual(host.ObservedTicks, 300);
        }

        [Test]
        public void AnalyticsPump_ProducesRealStatistics_NotAnEmptyShell()
        {
            var host = new MatchClientHost(MatchSetup.NeutralDemo(Seed));
            RunUntilObserved(host, 900);

            MatchAnalyticsReport snapshot = host.BuildReport();
            var report = snapshot.Result;
            Assert.AreEqual(
                host.ObservedTicks >= snapshot.ObservedTicks, true,
                "The report's tick count is taken with the result, so it can only lag a later read.");
            Assert.Greater(
                report.Home.PossessionSharePercent + report.Away.PossessionSharePercent, 0f,
                "Non-vacuity: an observer that fired but routed nothing would still show no fault.");

            int homeBins = 0;
            foreach (int c in report.HomeAdvanced.HeatmapBins) { homeBins += c; }
            Assert.Greater(homeBins, 0, "Positional sampling reaches the bins through the live path.");
        }

        [Test]
        public void ServiceOnce_DoesNotAdvanceTheAnalyticsClock()
        {
            // ServiceOnce runs the command drain WITHOUT ticking. Had the pump been hung off the
            // pre-tick hook it would fire here too and immediately trip F6 on a repeated tick — this
            // is why the post-tick observer is a distinct seam rather than a chain on that hook.
            var host = new MatchClientHost(MatchSetup.NeutralDemo(Seed));

            host.Session.ServiceOnce();
            host.Session.ServiceOnce();

            Assert.AreEqual(0, host.ObservedTicks);
            Assert.IsNull(host.AnalyticsFault);
        }

        [Test]
        public void AFailingObserver_IsDisarmedAndLatched_NotFatalToTheMatch()
        {
            // The pacing loop does not guard TickOnce, so an unhandled observer exception would end
            // the process. A derived statistic must never be able to kill a match in progress — but
            // it must not be swallowed either.
            var engine = new MatchEngine.MatchEngine(Seed);
            var streamer = new LiveMatchStreamer(engine);
            int calls = 0;
            streamer.SetPostTickObserver(() =>
            {
                calls++;
                throw new InvalidOperationException("boom");
            });

            streamer.SetSpeedMultiplier(MatchViewerConstants.MaxLiveSpeedMultiplier);
            streamer.Start();
            var sw = Stopwatch.StartNew();
            while (streamer.PostTickObserverFault == null && sw.ElapsedMilliseconds < 10_000)
            {
                Thread.Sleep(5);
            }
            Thread.Sleep(200);   // let several more ticks go by after the fault
            streamer.Stop();

            Assert.IsNotNull(streamer.PostTickObserverFault, "The cause must be visible, not swallowed.");
            Assert.AreEqual("boom", streamer.PostTickObserverFault.Message);
            Assert.AreEqual(1, calls, "Disarmed after the first failure — not re-thrown sixty times a second.");
            Assert.IsTrue(streamer.TryGetLatestFrame(out LiveMatchFrame frame), "The match kept playing.");
            Assert.Greater(frame.Tick, 1UL);
        }

        [Test]
        public void QueuedIntent_ReachesTheEngineOnATickBoundary()
        {
            var host = new MatchClientHost(MatchSetup.NeutralDemo(Seed));
            TeamTactic attacking = new TeamTactic(
                mentality: Mentality.VeryAttacking,
                formation: TeamTactic.Balanced.Formation,
                tempo: TeamTactic.Balanced.Tempo,
                width: TeamTactic.Balanced.Width,
                passing: TeamTactic.Balanced.Passing,
                pressing: TeamTactic.Balanced.Pressing,
                lineOfEngagement: TeamTactic.Balanced.LineOfEngagement,
                defensiveLine: TeamTactic.Balanced.DefensiveLine,
                defensiveWidth: TeamTactic.Balanced.DefensiveWidth,
                transitionWon: TeamTactic.Balanced.TransitionWon,
                transitionLost: TeamTactic.Balanced.TransitionLost,
                offsideTrap: TeamTactic.Balanced.OffsideTrap,
                triggerPressMask: TeamTactic.Balanced.TriggerPressMask,
                focusPlay: TeamTactic.Balanced.FocusPlay,
                gkDistribution: TeamTactic.Balanced.GkDistribution,
                timeWasting: TeamTactic.Balanced.TimeWasting);

            host.Dispatch(ManagerIntent.SetTeamTactic(0, in attacking));
            Assert.AreEqual(1, host.Session.Commands.Count, "Queued, not applied.");

            host.Session.ServiceOnce();

            Assert.AreEqual(0, host.Session.Commands.Count);
            Assert.AreEqual(1, host.Session.Driver.Log.Count, "…and it is in the replay record.");
            Assert.AreEqual(0, host.Session.Driver.FailedCommands.Count, "…and the engine accepted it.");
        }

        [Test]
        public void NullSetup_FailsLoud()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchClientHost(null));
        }

        [Test]
        public void AttachTickObserver_IsSetOnce_AndPreStart()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            session.AttachTickObserver(_ => () => { });

            Assert.Throws<InvalidOperationException>(
                () => session.AttachTickObserver(_ => () => { }),
                "A second observer would silently displace or shadow the first.");
            Assert.Throws<InvalidOperationException>(
                () => new MatchSession(MatchSetup.NeutralDemo(Seed)).AttachTickObserver(_ => null),
                "A factory returning null looks wired and sees nothing.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (B6): the every-tick pump proven against a    |
// |         |            |        | really-running match (F6 self-checks it), the disarm-and-latch |
// |         |            |        | fault path, ServiceOnce not advancing it, and intent delivery. |
#endregion
