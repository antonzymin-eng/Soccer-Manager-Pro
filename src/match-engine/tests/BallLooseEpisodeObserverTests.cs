// File:     src/match-engine/tests/BallLooseEpisodeObserverTests.cs
// Created:  2026-11-09
// Modified: 2026-11-10
// Author:   —
// Spec:     w-440-ball-loose-design.md frozen Stage A definitions
// Purpose:  Outcome tests for chronology, horizon, censoring and diagnostic neutrality.

using System;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.DefensiveAI;

namespace TacticalDirector.MatchEngine
{
    /// <summary>Always-run tests; the long census is a separate environment-gated executable.</summary>
    [TestFixture, NonParallelizable]
    internal sealed class BallLooseEpisodeObserverTests
    {
        private static BallLooseStageAEvent Event(BallLooseStageAKind kind, int frame = 100,
            int actor = -1, int other = -1, int detail = 0, int previous = -1, int next = -1) =>
            new BallLooseStageAEvent(kind, frame, actor, other, detail, previous, next, Vector3.zero, Vector3.zero);
        private static void Start(BallLooseEpisodeObserver o, int carrier = 5, int tackler = 16, int frame = 100) =>
            o.Observe(Event(BallLooseStageAKind.Tackle, frame, tackler, carrier, (int)TackleOutcome.BallLoose));
        private static void Change(BallLooseEpisodeObserver o, int from, int to, int frame = 100) =>
            o.Observe(Event(BallLooseStageAKind.Possession, frame, previous: from, next: to));
        private static void End(BallLooseEpisodeObserver o, int frame, int owner) =>
            o.AfterTick(frame, owner, Vector3.zero, Vector3.zero);

        [TestCase(0)]
        [TestCase(1)]
        public void Chronology_SeparatesTeammateOpponentAndSameFrameRegain(int team)
        {
            int carrier = team * 11 + 5, mate = team * 11 + 6, opponent = (1 - team) * 11 + 5;
            var o = new BallLooseEpisodeObserver(id => id / 11, carrier);
            Start(o, carrier, opponent); Change(o, carrier, -1); Change(o, -1, mate);
            Change(o, mate, opponent); Change(o, opponent, carrier); End(o, 100, carrier);
            Change(o, carrier, mate, 160); End(o, 160, mate); o.Complete(160);
            var e = o.Episodes[0];
            Assert.IsTrue(e.SameFrameCarrierRegain);
            Assert.IsTrue(e.TeammateRecovery);
            Assert.IsTrue(e.OpponentRecovery);
            Assert.AreEqual(carrier, e.FrameEndOwner);
            Assert.AreEqual("carrier-teammate", e.Outcome);
            Assert.AreEqual(5, e.Ownership.Count);
            Assert.AreEqual(5, o.OwnershipChanges);
            Assert.Less(e.Ownership[1].Sequence, e.Ownership[2].Sequence);
        }

        [Test]
        public void SameFrameRegain_CanEndTheFrameLoose_AndTheHorizonLoose()
        {
            var o = new BallLooseEpisodeObserver(id => id / 11, 5);
            Start(o); Change(o, 5, -1); Change(o, -1, 5); Change(o, 5, -1);
            End(o, 100, -1); End(o, 160, -1); o.Complete(160);
            Assert.IsTrue(o.Episodes[0].SameFrameCarrierRegain);
            Assert.AreEqual(-1, o.Episodes[0].FrameEndOwner);
            Assert.AreEqual("still-loose", o.Episodes[0].Outcome);
        }

        [TestCase(5, "original-carrier")]
        [TestCase(6, "carrier-teammate")]
        [TestCase(16, "opponent")]
        [TestCase(-1, "still-loose")]
        public void Horizon_IncludesEndpointAcquisition_AndClassifiesFinalOwner(int owner, string expected)
        {
            var o = new BallLooseEpisodeObserver(id => id / 11, 5);
            Start(o); Change(o, 5, -1); End(o, 100, -1);
            Change(o, -1, 6, 110); End(o, 110, 6);
            Change(o, 6, -1, 159); End(o, 159, -1);
            if (owner >= 0) Change(o, -1, owner, 160);
            End(o, 160, owner); o.Complete(160);
            Assert.AreEqual(expected, o.Episodes[0].Outcome);
            Assert.AreEqual(160, o.Episodes[0].EndFrame);
        }

        [Test]
        public void SameFrameOverlap_CensorsOldEpisode_WithoutDuplicatingAcquisitions()
        {
            var o = new BallLooseEpisodeObserver(id => id / 11, 5);
            Start(o); Change(o, 5, -1); Change(o, -1, 16);
            Start(o, carrier: 16, tackler: 5); Change(o, 16, -1); Change(o, -1, 16);
            End(o, 100, 16); o.Complete(100);
            Assert.AreEqual(2, o.Episodes.Count);
            Assert.AreEqual("new-tackle", o.Episodes[0].CensorReason);
            Assert.AreEqual(2, o.Episodes[0].Ownership.Count);
            Assert.AreEqual(2, o.Episodes[1].Ownership.Count);
            Assert.IsTrue(o.Episodes[0].OpponentRecovery);
            Assert.IsFalse(o.Episodes[1].OpponentRecovery);
            Assert.IsTrue(o.Episodes[1].SameFrameCarrierRegain);
            Assert.AreEqual(4, o.OwnershipChanges);
        }

        [TestCase(BallLooseStageAKind.Restart, "restart")]
        [TestCase(BallLooseStageAKind.OutOfPlay, "out-of-play")]
        [TestCase(BallLooseStageAKind.Foul, "foul")]
        [TestCase(BallLooseStageAKind.HalfTime, "half-time")]
        [TestCase(BallLooseStageAKind.FullTime, "full-time")]
        public void Censor_PrecedesItsAward_AndRetainsOnlyObservedPrefix(BallLooseStageAKind kind, string reason)
        {
            var o = new BallLooseEpisodeObserver(id => id / 11, 5);
            Start(o); Change(o, 5, -1); End(o, 100, -1);
            o.Observe(Event(kind, 101)); Change(o, -1, 5, 101); End(o, 101, 5); o.Complete(101);
            Assert.AreEqual("censored", o.Episodes[0].Outcome);
            Assert.AreEqual(reason, o.Episodes[0].CensorReason);
            Assert.AreEqual(1, o.Episodes[0].Ownership.Count);
            Assert.IsFalse(o.Episodes[0].SameFrameCarrierRegain);
        }

        [Test]
        public void MissedTackle_SupersedesAnEpisode_WithoutCreatingOne()
        {
            var o = new BallLooseEpisodeObserver(id => id / 11, 5);
            Start(o); Change(o, 5, -1); Change(o, -1, 5); End(o, 100, 5);
            o.Observe(Event(BallLooseStageAKind.Tackle, 106, 16, 5, (int)TackleOutcome.Missed));
            End(o, 106, 5); o.Complete(106);
            Assert.AreEqual(1, o.Episodes.Count);
            Assert.AreEqual("new-tackle", o.Episodes[0].CensorReason);
        }

        [Test]
        public void CaptureEnd_CensorsUnfinishedHorizon()
        {
            var o = new BallLooseEpisodeObserver(id => id / 11, 5);
            Start(o); Change(o, 5, -1); End(o, 100, -1); o.Complete(100);
            Assert.AreEqual("capture-end", o.Episodes[0].CensorReason);
        }

        [Test]
        public void TackleFrameCensor_ExcludesTheFollowingRestartAwardFromRegains()
        {
            var o = new BallLooseEpisodeObserver(id => id / 11, 5);
            Start(o); Change(o, 5, -1);
            o.Observe(Event(BallLooseStageAKind.Restart)); Change(o, -1, 5);
            End(o, 100, 5); o.Complete(100);
            Assert.IsFalse(o.Episodes[0].SameFrameCarrierRegain);
            Assert.AreEqual(5, o.Episodes[0].FrameEndOwner);
            Assert.AreEqual("censored", o.Episodes[0].Outcome);
            Assert.AreEqual(1, o.Episodes[0].Ownership.Count);
        }

        [Test]
        public void CensorAtTheHorizon_PrecedesEndpointClassification()
        {
            var o = new BallLooseEpisodeObserver(id => id / 11, 5);
            Start(o); Change(o, 5, -1); End(o, 100, -1);
            o.Observe(Event(BallLooseStageAKind.Tackle, 160, 16, 5, (int)TackleOutcome.Missed));
            End(o, 160, -1); o.Complete(160);
            Assert.AreEqual("censored", o.Episodes[0].Outcome);
            Assert.AreEqual("new-tackle", o.Episodes[0].CensorReason);
        }

        [Test]
        public void MissingOwnershipTransition_FailsInsteadOfInventingChronology()
        {
            var o = new BallLooseEpisodeObserver(id => id / 11, 5);
            Start(o);
            Assert.Throws<InvalidOperationException>(() => End(o, 100, 16));
        }

        [Test]
        public void EnvironmentGate_OffDoesNotAttach()
        {
            string previous = Environment.GetEnvironmentVariable("TD_W440_OBSERVE");
            try
            {
                Environment.SetEnvironmentVariable("TD_W440_OBSERVE", null);
                var engine = new MatchEngine(1);
                Assert.IsNull(BallLooseEpisodeObserver.AttachFromEnvironment(engine));
                Assert.IsNull(engine.TestOnly_BallLooseStageAObserver);
            }
            finally { Environment.SetEnvironmentVariable("TD_W440_OBSERVE", previous); }
        }

        [Test]
        public void Observer_RecordsRealSeams_WithoutChangingAnyDigest()
        {
            const ulong seed = 0x0F1E2D3C4B5A6978UL;
            const int ticks = 600;
            var plain = new MatchEngine(seed);
            plain.TestOnly_ApplyRestart(new Vector2(52.5f, 34f), 1);
            var expected = new byte[ticks][];
            for (int i = 0; i < ticks; i++) { plain.RunTick(); expected[i] = plain.CurrentSnapshotDigest; }
            string previous = Environment.GetEnvironmentVariable("TD_W440_OBSERVE");
            try
            {
                Environment.SetEnvironmentVariable("TD_W440_OBSERVE", "1");
                var engine = new MatchEngine(seed);
                var observer = BallLooseEpisodeObserver.AttachFromEnvironment(engine);
                engine.TestOnly_ApplyRestart(new Vector2(52.5f, 34f), 1);
                for (int i = 0; i < ticks; i++)
                {
                    engine.RunTick(); observer.AfterTick(i + 1, engine.PossessingAgentId, engine.BallView.Position, engine.BallView.Velocity);
                    CollectionAssert.AreEqual(expected[i], engine.CurrentSnapshotDigest, "frame " + (i + 1));
                }
                observer.Complete(ticks);
                Assert.Greater(observer.Journal.Count, 0, "must exercise an actual callback");
                Assert.AreEqual(engine.TestOnly_TackleOutcomeCounts.Loose, observer.Episodes.Count);
            }
            finally { Environment.SetEnvironmentVariable("TD_W440_OBSERVE", previous); }
        }
    }
}

#region VersionHistory
// | Version | Date | Author | Notes |
// | 1.0 | 2026-11-10 | — | Always-run definition and real-seam neutrality tests; no opt-in skips. |
#endregion
