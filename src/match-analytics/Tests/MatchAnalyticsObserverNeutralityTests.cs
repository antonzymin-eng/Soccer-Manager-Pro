// File:     src/match-analytics/Tests/MatchAnalyticsObserverNeutralityTests.cs
// Created:  2026-07-27
// Modified: 2026-07-28
// Author:   —
// Spec:     Match Analytics & Statistics #37 §4.3 / §4.4, FR-AN-016 / FR-AN-017 (T-AN-NEU-001,
//           T-AN-DET-001), Code Standards #20
// Purpose:  Locks the two properties the whole tap design exists to guarantee — observing a match
//           does not change it, and the same match yields the same statistics twice — against a
//           real MatchEngine rather than a stub.

using NUnit.Framework;

using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchAnalytics.Tests
{
    /// <summary>Observer-neutrality and determinism over a live engine (see file header).</summary>
    ///
    /// <remarks>
    /// The EventBus is process-static (#17 §3.2.1), so these engines are run SEQUENTIALLY, never
    /// interleaved — the same constraint every other composed match-engine test carries.
    /// </remarks>
    [TestFixture]
    public sealed class MatchAnalyticsObserverNeutralityTests
    {
        private const ulong MatchSeed = 0x00A0FF1CE5EED37UL;
        private const int   Ticks     = 240;

        /// <summary>60 seconds — long enough that BOTH teams have demonstrably held possession.
        /// Re-measured for the keeper contact-rate pass (§5.Z.22), which moved this seed's opening
        /// trajectory: away possession now first accrues between ticks 1 800 and 2 400 (measured
        /// 0.000% at 1 800 → 3.625% at 2 400; at 3 600 home 9.4% / away 3.4%), so the original
        /// 30-second window rode on exactly the transition it was written not to ride on.</summary>
        private const int   LivenessTicks = 3_600;

        /// <summary>Runs a match with analytics attached, returning the digest and the result.</summary>
        private static (byte[] digest, MatchAnalyticsResult result) RunObserved(ulong seed, int ticks)
        {
            var engine = new MatchEngine.MatchEngine(seed);
            var obs    = new MatchEngineObservation(engine);
            var agg    = new MatchAnalyticsAggregator();

            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
                agg.ObserveTick(obs, obs, obs.CurrentTick);
            }

            return ((byte[])engine.CurrentSnapshotDigest.Clone(), agg.Build());
        }

        /// <summary>Runs the same match with nothing attached.</summary>
        private static byte[] RunUnobserved(ulong seed, int ticks)
        {
            var engine = new MatchEngine.MatchEngine(seed);
            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
            }

            return (byte[])engine.CurrentSnapshotDigest.Clone();
        }

        [Test]
        public void TAnNeu001_ObservingAMatchDoesNotPerturbItsDigest()
        {
            byte[] unobserved = RunUnobserved(MatchSeed, Ticks);
            (byte[] observed, MatchAnalyticsResult result) = RunObserved(MatchSeed, Ticks);

            CollectionAssert.AreEqual(
                unobserved, observed,
                "FR-AN-017: the digest chain must be identical whether or not analytics run.");

            // Non-vacuity: a run that observed nothing would satisfy the equality above trivially.
            Assert.Greater(
                result.Home.PossessionSharePercent + result.Away.PossessionSharePercent, 0f,
                "The observed run must actually have derived something, or the neutrality claim is empty.");
        }

        [Test]
        public void TAnDet001_SameMatchYieldsByteIdenticalStatistics()
        {
            (byte[] digestA, MatchAnalyticsResult a) = RunObserved(MatchSeed, Ticks);
            (byte[] digestB, MatchAnalyticsResult b) = RunObserved(MatchSeed, Ticks);

            CollectionAssert.AreEqual(digestA, digestB, "Two same-seed runs must agree tick-for-tick.");

            AssertStatlinesEqual(a, b);
        }

        [Test]
        public void TapIsLive_PossessionRecordsReachTheAggregatorAndResolveBothTeams()
        {
            // The authored-record tests prove the routing table; this proves the WIRING — that real
            // ledger records survive the engine's capture, the canonical-order walk, and the adapter,
            // and that the KD-6 agentId→team map resolves them to both sides. Possession is the signal
            // to assert on — where fouls (~0.35/min) would be a coin flip — but the window must carry
            // BOTH teams' possession, and that is a measured property of the seed, not a rate estimate:
            // the keeper contact-rate pass (§5.Z.22) moved this seed's away-possession onset past the
            // original 30-second window (see the LivenessTicks doc for the measurement).
            (byte[] _, MatchAnalyticsResult r) = RunObserved(MatchSeed, LivenessTicks);

            Assert.Greater(
                r.Home.PossessionSharePercent, 0f,
                "No home possession in " + LivenessTicks + " ticks — records are not reaching the aggregator.");
            Assert.Greater(
                r.Away.PossessionSharePercent, 0f,
                "No away possession — either the tap or the KD-6 team resolution is one-sided.");
            Assert.Less(
                r.Home.PossessionSharePercent + r.Away.PossessionSharePercent, 100f,
                "Some of a real match is dead-ball time, which belongs to the loose bucket (F3).");
        }

        [Test]
        public void HeatmapAccrues_OverARealRun()
        {
            (byte[] _, MatchAnalyticsResult r) = RunObserved(MatchSeed, Ticks);

            int homeTotal = 0;
            foreach (int c in r.HomeAdvanced.HeatmapBins) homeTotal += c;

            Assert.Greater(homeTotal, 0, "Positional sampling must be reaching the bins.");
            Assert.AreEqual(
                100f,
                r.HomeAdvanced.TerritorialPercent + r.AwayAdvanced.TerritorialPercent, 1e-3f,
                "Territorial share stays a total split over a real run too.");
        }

        private static void AssertStatlinesEqual(MatchAnalyticsResult a, MatchAnalyticsResult b)
        {
            AssertTeamEqual(a.Home, b.Home, "home");
            AssertTeamEqual(a.Away, b.Away, "away");

            Assert.AreEqual(a.HomeAdvanced.TerritorialPercent, b.HomeAdvanced.TerritorialPercent, 0f);
            Assert.AreEqual(a.AwayAdvanced.TerritorialPercent, b.AwayAdvanced.TerritorialPercent, 0f);
            CollectionAssert.AreEqual(a.HomeAdvanced.HeatmapBins, b.HomeAdvanced.HeatmapBins);
            CollectionAssert.AreEqual(a.AwayAdvanced.HeatmapBins, b.AwayAdvanced.HeatmapBins);

            Assert.AreEqual(a.GoalMap.Count,    b.GoalMap.Count);
            Assert.AreEqual(a.FoulMap.Count,    b.FoulMap.Count);
            Assert.AreEqual(a.OffsideMap.Count, b.OffsideMap.Count);
            for (int i = 0; i < a.FoulMap.Count; i++)
            {
                Assert.AreEqual(a.FoulMap[i].TeamId, b.FoulMap[i].TeamId);
                Assert.AreEqual(a.FoulMap[i].X, b.FoulMap[i].X, 0f, "Exact equality — no tolerance.");
                Assert.AreEqual(a.FoulMap[i].Y, b.FoulMap[i].Y, 0f);
            }
        }

        private static void AssertTeamEqual(in MatchStatline a, in MatchStatline b, string what)
        {
            Assert.AreEqual(a.Goals,                  b.Goals,                  what + " goals");
            Assert.AreEqual(a.PossessionSharePercent, b.PossessionSharePercent, 0f, what + " possession");
            Assert.AreEqual(a.Fouls,                  b.Fouls,                  what + " fouls");
            Assert.AreEqual(a.YellowCards,            b.YellowCards,            what + " yellows");
            Assert.AreEqual(a.RedCards,               b.RedCards,               what + " reds");
            Assert.AreEqual(a.Offsides,               b.Offsides,               what + " offsides");
            Assert.AreEqual(a.Corners,                b.Corners,                what + " corners");
            Assert.AreEqual(a.ThrowIns,               b.ThrowIns,               what + " throw-ins");
            Assert.AreEqual(a.GoalKicks,              b.GoalKicks,              what + " goal kicks");
            Assert.AreEqual(a.Substitutions,          b.Substitutions,          what + " substitutions");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T1): T-AN-NEU-001 observer-neutrality    |
// |         |            |        | digest lock (with a non-vacuity guard) and T-AN-DET-001        |
// |         |            |        | two-run determinism, over a real 240-tick match.               |
// | 1.1     | 2026-07-28 | —      | Liveness window re-measured 1 800 → 3 600 ticks: the keeper    |
// |         |            |        | contact-rate pass (§5.Z.22) moved this seed's away-possession  |
// |         |            |        | onset to ~tick 2 400 (was inside 1 800), so the 30 s window    |
// |         |            |        | measured away possession at exactly 0. Assertions unchanged —  |
// |         |            |        | the gk-contact-rate AR-4 window-re-measure class, third case.  |
#endregion
