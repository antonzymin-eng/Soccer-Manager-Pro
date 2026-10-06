// File:     src/match-client-core/tests/SessionMatchAnalyticsTests.cs
// Created:  2026-10-06
// Modified: 2026-10-06
// Author:   —
// Spec:     P5b lifecycle/identity plan §§4–7, S0 binding contracts §2, Code Standards #20
// Purpose:  Controlled mid-observation fault locks incomplete/cutoff publication before streamer disarm.

using System;
using System.Threading;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.MatchAnalytics;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class SessionMatchAnalyticsTests
    {
        private sealed class EmptyTap : ITickLedgerTap
        {
            public int RecordCount => 0;
            public byte RecordOrdinal(int index) => throw new ArgumentOutOfRangeException();
            public T ReadRecord<T>(int index) where T : struct => throw new ArgumentOutOfRangeException();
        }
        private sealed class FaultingSample : IWorldStateSample
        {
            internal bool ShouldFail;
            internal readonly ManualResetEventSlim Entered = new ManualResetEventSlim();
            internal readonly ManualResetEventSlim Release = new ManualResetEventSlim();
            public Vector2 BallPosition
            {
                get
                {
                    if (ShouldFail)
                    {
                        Entered.Set();
                        if (!Release.Wait(5000)) throw new TimeoutException();
                        throw new ArgumentException("mid-tick sample fault");
                    }
                    return Vector2.zero;
                }
            }
            public int AgentCount => 0;
            public Vector2 AgentPosition(int index) => throw new ArgumentOutOfRangeException();
            public int AgentTeamId(int index) => throw new ArgumentOutOfRangeException();
            public bool AgentIsActive(int index) => throw new ArgumentOutOfRangeException();
        }

        [Test]
        public void StreamerDisarmsFailedAdapterWhileFramesContinueAndCutoffStaysIncomplete()
        {
            var sample = new FaultingSample();
            var session = new MatchSession(MatchSetup.NeutralDemo(1));
            SessionMatchAnalytics adapter = null;
            session.AttachTickObserver(engine =>
            {
                adapter = new SessionMatchAnalytics(new EmptyTap(), sample, () => engine.CurrentTick);
                return adapter.ObserveTick;
            });
            session.TickOnce();
            MatchAnalyticsPublication healthy = adapter.Publish();
            Assert.IsFalse(healthy.IsIncomplete);
            sample.ShouldFail = true;
            sample.Release.Set();
            session.TickOnce();
            Assert.IsInstanceOf<ArgumentException>(session.Streamer.PostTickObserverFault);
            MatchAnalyticsPublication failed = adapter.Publish();
            Assert.IsTrue(failed.IsIncomplete);
            Assert.AreEqual(2UL, failed.AttemptedTick);
            Assert.AreEqual(1UL, failed.CompletedTick);
            Assert.AreEqual(healthy.ResultThroughTick, failed.ResultThroughTick);
            session.TickOnce();
            Assert.IsTrue(session.TryGetLatestFrame(out var frame));
            Assert.AreEqual(3UL, frame.Tick);
            Assert.AreEqual(2UL, adapter.Publish().AttemptedTick, "disarmed adapter is never invoked again");
            sample.Entered.Dispose();
            sample.Release.Dispose();
        }

        [Test]
        public void MidTickFailureNeverPublishesPartiallyMutatedAggregationAsHealthy()
        {
            var sample = new FaultingSample();
            ulong tick = 1;
            var adapter = new SessionMatchAnalytics(new EmptyTap(), sample, () => tick);
            adapter.ObserveTick();
            MatchAnalyticsPublication previous = adapter.Publish();
            sample.ShouldFail = true;
            tick = 1UL + (ulong)MatchAnalyticsConstants.TERRITORIAL_SAMPLE_STRIDE;
            // Fill all earlier ticks, then block/throw after possession accrual in the stride sample.
            sample.ShouldFail = false;
            for (ulong t = 2; t < tick; t++)
            {
                ulong last = tick; tick = t; adapter.ObserveTick(); tick = last;
            }
            sample.ShouldFail = true;
            Exception fault = null;
            var observe = new Thread(() => { try { adapter.ObserveTick(); } catch (Exception ex) { fault = ex; } });
            observe.Start();
            Assert.IsTrue(sample.Entered.Wait(5000));
            MatchAnalyticsPublication publication = default;
            var reader = new Thread(() => publication = adapter.Publish());
            reader.Start();
            sample.Release.Set();
            Assert.IsTrue(observe.Join(5000));
            Assert.IsTrue(reader.Join(5000));
            Assert.IsInstanceOf<ArgumentException>(fault);
            Assert.IsTrue(publication.IsIncomplete);
            Assert.AreEqual(tick, publication.AttemptedTick);
            Assert.AreEqual(tick - 1, publication.CompletedTick);
            Assert.AreEqual((long)tick - 1, publication.ObservedTicks);
            Assert.AreEqual(previous.ResultThroughTick, publication.ResultThroughTick);
            Assert.AreEqual(previous.Result.Value.Home.PossessionSharePercent, publication.Result.Value.Home.PossessionSharePercent);
            sample.Entered.Dispose();
            sample.Release.Dispose();
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-06 | —      | Consumed P5b lifecycle/identity implementation. |
#endregion
