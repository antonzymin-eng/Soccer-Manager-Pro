// File:     src/match-analytics/Tests/MatchAnalyticsAggregatorTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Analytics & Statistics #37 §3.1–§3.5, §5 (T-AN-*), Code Standards #20
// Purpose:  Drives the §3.2 routing table, §3.1 possession weighting, §3.4 positional binning and
//           every failure mode from authored records, so each branch is exercised directly rather
//           than by contriving a match that happens to foul, card and substitute on cue.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.EventSystem;

namespace TacticalDirector.MatchAnalytics.Tests
{
    /// <summary>A tap holding authored records — the §3.2 routing table's direct input.</summary>
    internal sealed class FakeTap : ITickLedgerTap
    {
        private readonly List<byte>   _ordinals = new List<byte>();
        private readonly List<object> _records  = new List<object>();

        public int RecordCount => _ordinals.Count;

        public byte RecordOrdinal(int index) => _ordinals[index];

        public T ReadRecord<T>(int index) where T : struct => (T)_records[index];

        public FakeTap Add<T>(in T record) where T : struct
        {
            _ordinals.Add(EventRegistry.GetOrdinal<T>());
            _records.Add(record);
            return this;
        }

        /// <summary>Adds a record under an ordinal #37 does not aggregate (the F5 path).</summary>
        public FakeTap AddUnknown(byte ordinal)
        {
            _ordinals.Add(ordinal);
            _records.Add(default(GoalAwardedEvent));
            return this;
        }

        public void Clear()
        {
            _ordinals.Clear();
            _records.Clear();
        }
    }

    /// <summary>A positional sample with an authored ball position and a fixed 11-v-11 team map.</summary>
    internal sealed class FakeSample : IWorldStateSample
    {
        private readonly Vector2[] _agents;
        private readonly int[]     _teams;
        private readonly bool[]    _active;

        public FakeSample(Vector2 ball, int agentCount = 22)
        {
            BallPosition = ball;
            _agents = new Vector2[agentCount];
            _teams  = new int[agentCount];
            _active = new bool[agentCount];
            for (int i = 0; i < agentCount; i++)
            {
                _active[i] = true;
                // Every agent parked on one interior cell per team, so heatmap assertions read a
                // single known bin rather than a spread.
                _teams[i]  = i < agentCount / 2 ? 0 : 1;
                _agents[i] = _teams[i] == 0 ? new Vector2(10f, 10f) : new Vector2(95f, 60f);
            }
        }

        public Vector2 BallPosition { get; set; }

        public int AgentCount => _agents.Length;

        public Vector2 AgentPosition(int index) => _agents[index];

        public int AgentTeamId(int index) => _teams[index];

        public bool AgentIsActive(int index) => _active[index];

        public void SetAgent(int index, Vector2 position, int team)
        {
            _agents[index] = position;
            _teams[index]  = team;
        }

        /// <summary>Marks an agent dismissed — still on the pitch, no longer playing.</summary>
        public void SendOff(int index) => _active[index] = false;
    }

    /// <summary>§3.1–§3.5 aggregation-core tests (see file header).</summary>
    [TestFixture]
    public sealed class MatchAnalyticsAggregatorTests
    {
        private static readonly Vector2 MidfieldBall =
            new Vector2(MatchAnalyticsConstants.PITCH_LENGTH_M * 0.5f, 34f);

        private static Vector3 At(float x, float y) => new Vector3(x, y, 0.11f);

        /// <summary>Pumps <paramref name="ticks"/> consecutive ticks, replaying <paramref name="tap"/>
        /// on the first and an empty tap thereafter (so records land exactly once).</summary>
        private static void Pump(
            MatchAnalyticsAggregator agg, FakeTap tap, FakeSample sample, int ticks, ulong firstTick = 1UL)
        {
            var empty = new FakeTap();
            for (int i = 0; i < ticks; i++)
            {
                agg.ObserveTick(i == 0 ? tap : empty, sample, firstTick + (ulong)i);
            }
        }

        // ── §3.2 routing table — one test per row ────────────────────────────────────────────────

        [Test]
        public void Goal_TalliesAndMapsToTheScoringTeam()
        {
            var agg = new MatchAnalyticsAggregator();
            var tap = new FakeTap()
                .Add(new GoalAwardedEvent(scorer: 3, assister: 5, scoringTeam: 1, ballPosition: At(104f, 34f)));

            Pump(agg, tap, new FakeSample(MidfieldBall), ticks: 1);
            MatchAnalyticsResult r = agg.Build();

            Assert.AreEqual(0, r.Home.Goals);
            Assert.AreEqual(1, r.Away.Goals);
            Assert.AreEqual(1, r.GoalMap.Count, "The goal appends exactly one map point.");
            Assert.AreEqual(1, r.GoalMap[0].TeamId);
            Assert.AreEqual(104f, r.GoalMap[0].X, 1e-4f);
            Assert.AreEqual(34f, r.GoalMap[0].Y, 1e-4f);
        }

        [Test]
        public void Foul_RoutesByOffenderAgentTeam_AndMapsLocation()
        {
            var agg = new MatchAnalyticsAggregator();
            // Agent 15 is on team 1 in FakeSample's 22-slot map — the KD-6 agentId→team lookup, which
            // is the only way a foul can be attributed at all.
            var tap = new FakeTap()
                .Add(new FoulCommittedEvent(offender: 15, victim: 2, location: At(40f, 20f), foulKind: 0));

            Pump(agg, tap, new FakeSample(MidfieldBall), ticks: 1);
            MatchAnalyticsResult r = agg.Build();

            Assert.AreEqual(0, r.Home.Fouls);
            Assert.AreEqual(1, r.Away.Fouls, "Offender 15 is an away agent.");
            Assert.AreEqual(1, r.FoulMap.Count);
            Assert.AreEqual(1, r.FoulMap[0].TeamId);
            Assert.AreEqual(40f, r.FoulMap[0].X, 1e-4f);
        }

        [Test]
        public void Cards_SplitYellowFromRedByCardKind()
        {
            var agg = new MatchAnalyticsAggregator();
            var tap = new FakeTap()
                .Add(new CardIssuedEvent(recipient: 4, cardKind: MatchAnalyticsConstants.CARD_KIND_RED, foulOrdinal: 0xFFFF))
                .Add(new CardIssuedEvent(recipient: 4, cardKind: 0, foulOrdinal: 0xFFFF))
                .Add(new CardIssuedEvent(recipient: 18, cardKind: 0, foulOrdinal: 0xFFFF));

            Pump(agg, tap, new FakeSample(MidfieldBall), ticks: 1);
            MatchAnalyticsResult r = agg.Build();

            Assert.AreEqual(1, r.Home.RedCards);
            Assert.AreEqual(1, r.Home.YellowCards);
            Assert.AreEqual(0, r.Away.RedCards);
            Assert.AreEqual(1, r.Away.YellowCards);
        }

        [Test]
        public void Offside_TalliesByCarriedTeam_AndMapsLocation()
        {
            var agg = new MatchAnalyticsAggregator();
            var tap = new FakeTap()
                .Add(new OffsideCalledEvent(offendingAgentId: 7, team: 0, location: At(80f, 12f)));

            Pump(agg, tap, new FakeSample(MidfieldBall), ticks: 1);
            MatchAnalyticsResult r = agg.Build();

            Assert.AreEqual(1, r.Home.Offsides);
            Assert.AreEqual(0, r.Away.Offsides);
            Assert.AreEqual(1, r.OffsideMap.Count);
            Assert.AreEqual(0, r.OffsideMap[0].TeamId);
        }

        [Test]
        public void Restarts_SplitByKind_AndKickoffIsCountedNowhere()
        {
            var agg = new MatchAnalyticsAggregator();
            var tap = new FakeTap()
                .Add(new RestartAwardedEvent(MatchAnalyticsConstants.RESTART_KIND_CORNER,    awardedTeam: 0, location: At(105f, 68f)))
                .Add(new RestartAwardedEvent(MatchAnalyticsConstants.RESTART_KIND_THROW_IN,  awardedTeam: 0, location: At(50f, 68f)))
                .Add(new RestartAwardedEvent(MatchAnalyticsConstants.RESTART_KIND_GOAL_KICK, awardedTeam: 1, location: At(5f, 34f)))
                .Add(new RestartAwardedEvent((byte)TacticalDirector.BallPhysics.RestartType.KickOff, awardedTeam: 1, location: At(52.5f, 34f)));

            Pump(agg, tap, new FakeSample(MidfieldBall), ticks: 1);
            MatchAnalyticsResult r = agg.Build();

            Assert.AreEqual(1, r.Home.Corners);
            Assert.AreEqual(1, r.Home.ThrowIns);
            Assert.AreEqual(0, r.Home.GoalKicks);
            Assert.AreEqual(1, r.Away.GoalKicks);
            Assert.AreEqual(0, r.Away.Corners, "Kick-off has no statline column and must not leak into one.");
            Assert.AreEqual(0, r.Away.ThrowIns);
        }

        [Test]
        public void Substitution_TalliesByCarriedTeam()
        {
            var agg = new MatchAnalyticsAggregator();
            var tap = new FakeTap()
                .Add(new SubstitutionEvent(outgoing: 3, incoming: 12, team: 1, substitutionReason: 0));

            Pump(agg, tap, new FakeSample(MidfieldBall), ticks: 1);

            Assert.AreEqual(0, agg.Build().Home.Substitutions);
            Assert.AreEqual(1, agg.Build().Away.Substitutions);
        }

        [Test]
        public void UnknownOrdinal_IsIgnored_NotAnError()
        {
            var agg = new MatchAnalyticsAggregator();
            // MatchPhaseChangedEvent is registered but deliberately drives no tally (§3.2), and 0xF0 is
            // a stand-in for a producer landed after this aggregator was written (F5).
            var tap = new FakeTap()
                .Add(new MatchPhaseChangedEvent(newPhase: 1, homeScore: 0, awayScore: 0))
                .AddUnknown(0xF0)
                .Add(new GoalAwardedEvent(scorer: 1, assister: -1, scoringTeam: 0, ballPosition: At(1f, 34f)));

            Assert.DoesNotThrow(() => Pump(agg, tap, new FakeSample(MidfieldBall), ticks: 1));
            Assert.AreEqual(1, agg.Build().Home.Goals, "The recognized record either side of them still counts.");
        }

        // ── §3.1 possession ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Possession_IsTickWeighted_AndTheThreeSharesSumTo100()
        {
            var agg = new MatchAnalyticsAggregator();
            var sample = new FakeSample(MidfieldBall);
            var empty = new FakeTap();

            // Ticks 1–2 loose (no holder yet), 3–6 home (agent 1), 7–10 away (agent 15).
            agg.ObserveTick(empty, sample, 1UL);
            agg.ObserveTick(empty, sample, 2UL);
            agg.ObserveTick(new FakeTap().Add(new PossessionChangedEvent(-1, 1, 0)), sample, 3UL);
            for (ulong t = 4UL; t <= 6UL; t++) agg.ObserveTick(empty, sample, t);
            agg.ObserveTick(new FakeTap().Add(new PossessionChangedEvent(1, 15, 0)), sample, 7UL);
            for (ulong t = 8UL; t <= 10UL; t++) agg.ObserveTick(empty, sample, t);

            MatchAnalyticsResult r = agg.Build();
            Assert.AreEqual(40f, r.Home.PossessionSharePercent, 1e-3f, "4 of 10 ticks held by home.");
            Assert.AreEqual(40f, r.Away.PossessionSharePercent, 1e-3f, "4 of 10 ticks held by away.");
            Assert.AreEqual(
                20f, 100f - r.Home.PossessionSharePercent - r.Away.PossessionSharePercent, 1e-3f,
                "The remainder is the loose bucket — the two teams do NOT sum to 100.");
        }

        [Test]
        public void Possession_HolderMinusOne_IsAttributedToNeitherTeam()
        {
            var agg = new MatchAnalyticsAggregator();
            var sample = new FakeSample(MidfieldBall);

            agg.ObserveTick(new FakeTap().Add(new PossessionChangedEvent(-1, 1, 0)), sample, 1UL);
            agg.ObserveTick(new FakeTap().Add(new PossessionChangedEvent(1, -1, 0)), sample, 2UL);
            agg.ObserveTick(new FakeTap(), sample, 3UL);

            MatchAnalyticsResult r = agg.Build();
            Assert.AreEqual(100f / 3f, r.Home.PossessionSharePercent, 1e-3f, "Only tick 1 was held (F3).");
            Assert.AreEqual(0f, r.Away.PossessionSharePercent, 1e-4f);
        }

        [Test]
        public void Possession_BeforeAnyTick_IsZeroNotNaN()
        {
            MatchAnalyticsResult r = new MatchAnalyticsAggregator().Build();

            Assert.AreEqual(0f, r.Home.PossessionSharePercent, 1e-6f);
            Assert.AreEqual(0f, r.HomeAdvanced.TerritorialPercent, 1e-6f);
        }

        // ── §3.4 positional ─────────────────────────────────────────────────────────────────────

        [Test]
        public void Territorial_CreditsTheTeamWhoseOpponentHalfHoldsTheBall_AndTheSplitIsTotal()
        {
            var agg = new MatchAnalyticsAggregator();
            var sample = new FakeSample(new Vector2(90f, 34f));   // deep in team 0's attacking half

            for (ulong t = 1UL; t <= 3UL; t++) agg.ObserveTick(new FakeTap(), sample, t);
            sample.BallPosition = new Vector2(15f, 34f);          // team 1's attacking half
            agg.ObserveTick(new FakeTap(), sample, 4UL);

            MatchAnalyticsResult r = agg.Build();
            Assert.AreEqual(75f, r.HomeAdvanced.TerritorialPercent, 1e-3f);
            Assert.AreEqual(25f, r.AwayAdvanced.TerritorialPercent, 1e-3f);
            Assert.AreEqual(
                100f, r.HomeAdvanced.TerritorialPercent + r.AwayAdvanced.TerritorialPercent, 1e-3f,
                "§3.4 requires the split to be total — no gap, no double-count.");
        }

        [Test]
        public void Territorial_HalfwayLineSampleIsStillAttributed_SoNoSampleIsLost()
        {
            var agg = new MatchAnalyticsAggregator();
            agg.ObserveTick(new FakeTap(), new FakeSample(MidfieldBall), 1UL);

            MatchAnalyticsResult r = agg.Build();
            Assert.AreEqual(
                100f, r.HomeAdvanced.TerritorialPercent + r.AwayAdvanced.TerritorialPercent, 1e-3f,
                "x == L/2 exactly: the strict > sends it to team 1, and the split stays total.");
        }

        [Test]
        public void Heatmap_BinsPerTeam_AndDropsOffPitchAgents()
        {
            var agg = new MatchAnalyticsAggregator();
            var sample = new FakeSample(MidfieldBall);
            sample.SetAgent(0, new Vector2(-5f, 34f), 0);      // off the pitch behind the goal line

            agg.ObserveTick(new FakeTap(), sample, 1UL);
            MatchAnalyticsResult r = agg.Build();

            int homeTotal = 0;
            foreach (int c in r.HomeAdvanced.HeatmapBins) homeTotal += c;
            int awayTotal = 0;
            foreach (int c in r.AwayAdvanced.HeatmapBins) awayTotal += c;

            Assert.AreEqual(10, homeTotal, "11 home agents minus the one off the pitch.");
            Assert.AreEqual(11, awayTotal);
            Assert.AreEqual(
                MatchAnalyticsConstants.HEATMAP_BINS_PER_TEAM, r.HomeAdvanced.HeatmapBins.Count);
        }

        [Test]
        public void Heatmap_AllOnOneSpot_LandsInOneBin()
        {
            var agg = new MatchAnalyticsAggregator();
            var sample = new FakeSample(MidfieldBall);
            agg.ObserveTick(new FakeTap(), sample, 1UL);

            int occupied = 0;
            foreach (int c in agg.Build().HomeAdvanced.HeatmapBins)
            {
                if (c > 0) occupied++;
            }

            Assert.AreEqual(1, occupied, "All 11 home agents are parked on one cell.");
        }

        // ── xG (F4 / KD-2) ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Xg_IsUnavailableAtStage1_BecauseNoProducerSuppliesAShotOrigin()
        {
            var agg = new MatchAnalyticsAggregator();
            agg.ObserveTick(
                new FakeTap().Add(new GoalAwardedEvent(1, -1, 0, At(104f, 34f))),
                new FakeSample(MidfieldBall), 1UL);

            MatchAnalyticsResult r = agg.Build();
            Assert.IsFalse(r.HomeAdvanced.LiveXgAvailable, "A goal's crossing point is not a shot origin (§3.3).");
            Assert.AreEqual(0f, r.HomeAdvanced.XgSum, 1e-6f);
        }

        // ── Build contract ──────────────────────────────────────────────────────────────────────

        [Test]
        public void Build_IsIdempotent_SoAHudMayCallItEveryFrame()
        {
            var agg = new MatchAnalyticsAggregator();
            var tap = new FakeTap().Add(new GoalAwardedEvent(1, -1, 0, At(104f, 34f)));
            Pump(agg, tap, new FakeSample(MidfieldBall), ticks: 5);

            MatchAnalyticsResult first  = agg.Build();
            MatchAnalyticsResult second = agg.Build();

            Assert.AreEqual(first.Home.Goals, second.Home.Goals);
            Assert.AreEqual(first.Home.PossessionSharePercent, second.Home.PossessionSharePercent, 1e-6f);
            Assert.AreEqual(first.GoalMap.Count, second.GoalMap.Count, "Build does not consume the accumulators.");
        }

        [Test]
        public void Build_MapsAreSnapshots_NotWindowsOntoTheLiveAccumulator()
        {
            var agg = new MatchAnalyticsAggregator();
            agg.ObserveTick(
                new FakeTap().Add(new GoalAwardedEvent(1, -1, 0, At(104f, 34f))),
                new FakeSample(MidfieldBall), 1UL);

            MatchAnalyticsResult before = agg.Build();
            agg.ObserveTick(
                new FakeTap().Add(new GoalAwardedEvent(1, -1, 0, At(1f, 34f))),
                new FakeSample(MidfieldBall), 2UL);

            Assert.AreEqual(1, before.GoalMap.Count, "An earlier result must not grow behind the caller's back.");
            Assert.AreEqual(2, agg.Build().GoalMap.Count);
        }

        // ── Failure modes ───────────────────────────────────────────────────────────────────────

        [Test]
        public void F6_SkippedTick_FailsLoud()
        {
            var agg = new MatchAnalyticsAggregator();
            var sample = new FakeSample(MidfieldBall);
            agg.ObserveTick(new FakeTap(), sample, 1UL);

            Assert.Throws<InvalidOperationException>(
                () => agg.ObserveTick(new FakeTap(), sample, 3UL),
                "A dropped tick loses that tick's records outright — it must not be counted through.");
        }

        [Test]
        public void F6_RepeatedTick_FailsLoud()
        {
            var agg = new MatchAnalyticsAggregator();
            var sample = new FakeSample(MidfieldBall);
            agg.ObserveTick(new FakeTap(), sample, 1UL);

            Assert.Throws<InvalidOperationException>(() => agg.ObserveTick(new FakeTap(), sample, 1UL));
        }

        [Test]
        public void F6_FirstObservationMayStartAtAnyTick()
        {
            var agg = new MatchAnalyticsAggregator();
            Assert.DoesNotThrow(
                () => agg.ObserveTick(new FakeTap(), new FakeSample(MidfieldBall), 9_000UL),
                "The aggregator may be attached mid-match; only gaps AFTER that are a defect.");
        }

        [Test]
        public void F1_OutOfRangeAgentInARecord_FailsLoud()
        {
            var agg = new MatchAnalyticsAggregator();
            var tap = new FakeTap()
                .Add(new FoulCommittedEvent(offender: 99, victim: 1, location: At(40f, 20f), foulKind: 0));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => agg.ObserveTick(tap, new FakeSample(MidfieldBall), 1UL));
        }

        [Test]
        public void F1_UnknownTeamOnATeamCarryingRecord_FailsLoud()
        {
            var agg = new MatchAnalyticsAggregator();
            var tap = new FakeTap()
                .Add(new GoalAwardedEvent(scorer: 1, assister: -1, scoringTeam: 7, ballPosition: At(1f, 34f)));

            Assert.Throws<ArgumentOutOfRangeException>(
                () => agg.ObserveTick(tap, new FakeSample(MidfieldBall), 1UL));
        }

        [Test]
        public void F2_NonFiniteRecordLocation_FailsLoud()
        {
            var agg = new MatchAnalyticsAggregator();
            var tap = new FakeTap()
                .Add(new GoalAwardedEvent(1, -1, 0, new Vector3(float.NaN, 34f, 0.11f)));

            Assert.Throws<ArgumentException>(
                () => agg.ObserveTick(tap, new FakeSample(MidfieldBall), 1UL));
        }

        [Test]
        public void F2_NonFiniteBallSample_FailsLoud()
        {
            var agg = new MatchAnalyticsAggregator();
            var sample = new FakeSample(new Vector2(float.NaN, 34f));

            Assert.Throws<ArgumentException>(() => agg.ObserveTick(new FakeTap(), sample, 1UL));
        }

        [Test]
        public void F2_NonFiniteAgentSample_FailsLoud()
        {
            var agg = new MatchAnalyticsAggregator();
            var sample = new FakeSample(MidfieldBall);
            sample.SetAgent(3, new Vector2(10f, float.PositiveInfinity), 0);

            Assert.Throws<ArgumentException>(() => agg.ObserveTick(new FakeTap(), sample, 1UL));
        }

        [Test]
        public void NullTaps_FailLoud()
        {
            var agg = new MatchAnalyticsAggregator();
            Assert.Throws<ArgumentNullException>(() => agg.ObserveTick(null, new FakeSample(MidfieldBall), 1UL));
            Assert.Throws<ArgumentNullException>(() => agg.ObserveTick(new FakeTap(), null, 1UL));
        }

        // ── AR-1 M-1: a tick that throws part-way through must not be silently survivable ────────

        [Test]
        public void PartiallyAppliedTick_LatchesAndRefusesEveryFurtherTick()
        {
            // The hole this locks: F6 checks tick CONTINUITY, which a half-applied tick satisfies —
            // the guard advances before the tick's records are routed. So without the latch, a run
            // that lost half of tick 1 kept counting from tick 2 and produced a statline that looks
            // plausible and is wrong, which is the exact outcome F6 exists to prevent.
            var agg    = new MatchAnalyticsAggregator();
            var sample = new FakeSample(MidfieldBall);

            agg.ObserveTick(new FakeTap(), sample, 0UL);

            // Two records: a valid goal, then a foul naming an agent outside the roster (F1).
            var bad = new FakeTap();
            bad.Add(new GoalAwardedEvent(scorer: 0, assister: -1, scoringTeam: 0, ballPosition: At(90f, 34f)));
            bad.Add(new FoulCommittedEvent(offender: 999, victim: 1, location: At(50f, 34f), foulKind: 0));

            Assert.Throws<ArgumentOutOfRangeException>(() => agg.ObserveTick(bad, sample, 1UL));

            // The first record landed and the tick itself never completed — the state IS partial.
            Assert.AreEqual(1, agg.Build().Home.Goals, "the record before the throw was applied");
            Assert.AreEqual(1L, agg.ObservedTicks, "…and the tick was never counted");

            // The lock: pumping on is refused, rather than compounding the partial tick.
            Assert.Throws<InvalidOperationException>(
                () => agg.ObserveTick(new FakeTap(), sample, 2UL),
                "a consecutive tick after a partial one must be refused, not accepted");
        }

        [Test]
        public void CleanTick_DoesNotLatch()
        {
            // Non-vacuity for the lock above: the latch must not fire on the ordinary path.
            var agg    = new MatchAnalyticsAggregator();
            var sample = new FakeSample(MidfieldBall);

            for (ulong t = 0; t < 5UL; t++)
            {
                Assert.DoesNotThrow(() => agg.ObserveTick(new FakeTap(), sample, t));
            }
            Assert.AreEqual(5L, agg.ObservedTicks);
        }

        // ── AR-1 M-2: a dismissed agent stops contributing to the heatmap ───────────────────────

        [Test]
        public void SentOffAgent_StopsAccruingHeatmapSamples()
        {
            // A dismissed agent is left standing on the pitch as a collision body (§5.Z Phase H) and
            // stops moving, so binning them would pour the rest of the match into one cell.
            var agg    = new MatchAnalyticsAggregator();
            var sample = new FakeSample(MidfieldBall);

            for (ulong t = 0; t < 10UL; t++) { agg.ObserveTick(new FakeTap(), sample, t); }
            long beforeRate = SumBins(agg, home: true) / 10L;

            sample.SendOff(0);
            for (ulong t = 10UL; t < 20UL; t++) { agg.ObserveTick(new FakeTap(), sample, t); }
            long afterRate = (SumBins(agg, home: true) - beforeRate * 10L) / 10L;

            Assert.AreEqual(11L, beforeRate, "eleven home agents binned per sampled tick");
            Assert.AreEqual(10L, afterRate, "the dismissed agent must drop out of the sample");
        }

        private static long SumBins(MatchAnalyticsAggregator agg, bool home)
        {
            IReadOnlyList<int> bins = home
                ? agg.Build().HomeAdvanced.HeatmapBins
                : agg.Build().AwayAdvanced.HeatmapBins;
            long total = 0;
            foreach (int c in bins) { total += c; }
            return total;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T1): §3.2 routing table row-by-row, §3.1 |
// |         |            |        | tick-weighted possession incl. the loose bucket, §3.4          |
// |         |            |        | territorial totality + heatmap binning, Build idempotence and  |
// |         |            |        | snapshot semantics, and F1/F2/F3/F4/F5/F6.                     |
// | 1.1     | 2026-07-27 | —      | AR-1 locks (+3): the partial-tick latch and its non-vacuity    |
// |         |            |        | control, and a dismissed agent dropping out of the §3.4        |
// |         |            |        | sample. Both fail against the pre-fix aggregator (verified by  |
// |         |            |        | perturbing each fix); FakeSample gains SendOff.                |
#endregion
