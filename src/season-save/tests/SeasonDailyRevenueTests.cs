// File:     src/season-save/tests/SeasonDailyRevenueTests.cs
// Created:  2026-10-13
// Modified: 2026-10-13
// Author:   —
// Spec:     Club Finances & Economy #40 §3.7 / §5.10; Season Loop #30 §3.3; Code Standards #20
// Purpose:  Locks non-zero daily totals, saved continuation, atomic retry and completed-season attribution.
//           Test-only gate parameters exercise the same guarded advance without storing an override.

using System;
using System.IO;
using System.Reflection;

using NUnit.Framework;

using TacticalDirector.ClubFinances;
using TacticalDirector.LivingWorld;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    public sealed class SeasonDailyRevenueTests
    {
        private string _tempDirectory;

        [SetUp]
        public void SetUp()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "td-revenue-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDirectory);
        }

        [TearDown]
        public void TearDown() => Directory.Delete(_tempDirectory, recursive: true);

        /// <summary>T-FN-REV-003: gate-off does not evaluate deep-only facts, even a refusing null season.</summary>
        [Test]
        public void DisabledGate_DoesNotReadAmountInputs()
        {
            var league = LeagueBootstrap.Generate(0x40B2UL, 4);
            var loop = league.CreateLoop(new WorldStore(0, 0x40B2UL), 0, RoundResolutionMode.QuickSimAll);
            ClubFinanceEntry[] before = loop.FinanceEntriesForSave();
            ClubFinanceEntry[] after = SeasonFinanceRuntime.PrepareDailyRevenue(before, false, null, uint.MaxValue);
            Assert.That(ClubFinancesSaveCodec.Encode(after), Is.EqualTo(ClubFinancesSaveCodec.Encode(before)));
        }

        /// <summary>T-FN-REV-004: independently counted days/home fixtures expose missing or duplicate accounting.</summary>
        [Test]
        public void MultipleRounds_AccrueExactlyOncePerCompletedInSeasonDay()
        {
            var league = LeagueBootstrap.Generate(0x40B2UL, 4);
            var loop = league.CreateLoop(new WorldStore(0, 0x40B2UL), 0, RoundResolutionMode.QuickSimAll);
            var expected = new long[4];
            AdvanceToFixture(loop, expected);
            AssertTotals(loop, expected); // All first-season pre-season days are zero.

            for (int round = 0; round < 3; round++)
            {
                loop.AdvanceAndPlayNextRound(league);
                AssertTotals(loop, expected); // Resolution itself earns nothing.
                Assert.Throws<InvalidOperationException>(() => loop.TestOnly_AdvanceDays(
                    checked((int)(loop.State.Calendar.NextFixtureDay() - loop.CurrentWorldDay + 1u)), true));
                loop.TestOnly_AdvanceDays(0, true);
                AssertTotals(loop, expected);
                AdvanceToFixture(loop, expected);
                AssertTotals(loop, expected);
            }

            Assert.That(expected[0], Is.GreaterThan(0L), "Count evidence must be non-zero.");
        }

        /// <summary>T-FN-REV-005: save/load/restore on either side of resolution and day completion continues byte-identically.</summary>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void SavedFixtureDay_ContinuesWithExactTotalsAndSaveBytes(int savePoint)
        {
            var league = LeagueBootstrap.Generate(0x40B2UL, 4);
            var straight = league.CreateLoop(new WorldStore(0, 0x40B2UL), 0, RoundResolutionMode.QuickSimAll);
            AdvanceToFixture(straight);
            if (savePoint >= 1) straight.AdvanceAndPlayNextRound(league);
            if (savePoint >= 2) straight.TestOnly_AdvanceDays(1, true);
            SeasonLoop resumed = Restore(straight, "checkpoint");

            if (savePoint == 0)
            {
                straight.AdvanceAndPlayNextRound(league);
                resumed.AdvanceAndPlayNextRound(league);
            }
            if (savePoint < 2)
            {
                straight.TestOnly_AdvanceDays(1, true);
                resumed.TestOnly_AdvanceDays(1, true);
            }

            AssertOpeningRoundTotals(straight);
            AssertOpeningRoundTotals(resumed);
            for (int round = 0; round < 2; round++)
            {
                AdvanceToFixture(straight);
                AdvanceToFixture(resumed);
                straight.AdvanceAndPlayNextRound(league);
                resumed.AdvanceAndPlayNextRound(league);
                straight.TestOnly_AdvanceDays(1, true);
                resumed.TestOnly_AdvanceDays(1, true);
            }
            Assert.That(SaveBytes(resumed, "resumed"), Is.EqualTo(SaveBytes(straight, "straight")));
        }

        /// <summary>T-FN-REV-006: late-club coherence/arithmetic failures publish nothing; repair retries earlier clubs once.</summary>
        [TestCase(false)]
        [TestCase(true)]
        public void LateClubFailure_LeavesClockAndAllFinancesUnchanged_ThenRetriesOnce(bool overflow)
        {
            var league = LeagueBootstrap.Generate(0x40B2UL, 4);
            var loop = league.CreateLoop(new WorldStore(0, 0x40B2UL), 0, RoundResolutionMode.QuickSimAll);
            AdvanceToFixture(loop);
            loop.AdvanceAndPlayNextRound(league);
            uint day = loop.CurrentWorldDay;
            ClubFinanceEntry[] published = LiveEntries(loop);
            ClubFinanceEntry validLast = published[3];
            var corrupt = validLast.Finances;
            if (overflow) corrupt.Balance = long.MaxValue;
            else corrupt.SeasonRevenueAccrued = -1L;
            published[3] = new ClubFinanceEntry(3, in corrupt);
            var before = (ClubFinanceEntry[])published.Clone();

            if (overflow) Assert.Throws<OverflowException>(() => loop.TestOnly_AdvanceDays(1, true));
            else Assert.Throws<ArgumentOutOfRangeException>(() => loop.TestOnly_AdvanceDays(1, true));
            Assert.That(loop.CurrentWorldDay, Is.EqualTo(day));
            Assert.That(LiveEntries(loop), Is.SameAs(published));
            CollectionAssert.AreEqual(before, published);

            published[3] = validLast;
            loop.TestOnly_AdvanceDays(1, true);
            Assert.That(LiveEntries(loop), Is.Not.SameAs(published));
            Assert.That(loop.CurrentWorldDay, Is.EqualTo(day + 1u));
            AssertOpeningRoundTotals(loop);
        }

        /// <summary>T-FN-REV-008: the completion helper refuses a scheduled fixture whose result is absent.</summary>
        [Test]
        public void UnplayedFixture_CannotBecomeRevenue()
        {
            var league = LeagueBootstrap.Generate(0x40B2UL, 4);
            var loop = league.CreateLoop(new WorldStore(0, 0x40B2UL), 0, RoundResolutionMode.QuickSimAll);
            AdvanceToFixture(loop);
            ClubFinanceEntry[] before = loop.FinanceEntriesForSave();
            var error = Assert.Throws<InvalidOperationException>(() => SeasonFinanceRuntime.PrepareDailyRevenue(
                before, true, loop.State, loop.CurrentWorldDay));
            Assert.That(error.Message, Is.EqualTo("Daily revenue cannot count an unplayed home fixture."));
            CollectionAssert.AreEqual(before, loop.FinanceEntriesForSave());
        }

        /// <summary>T-FN-REV-007/009: final-day cash reaches the completed season; roll timing cannot change attribution.</summary>
        [Test]
        public void FinalDayAndDelayedRoll_PreserveExactCompletedSeasonRevenue()
        {
            var league = LeagueBootstrap.Generate(0x40B2UL, 4);
            var immediate = league.CreateLoop(new WorldStore(0, 0x40B2UL), 0, RoundResolutionMode.QuickSimAll);
            var expected = new long[4];
            while (!immediate.IsSeasonComplete)
            {
                AdvanceToFixture(immediate, expected);
                immediate.AdvanceAndPlayNextRound(league);
            }
            Assert.Throws<InvalidOperationException>(() => immediate.RollToNextSeason());
            CountDay(immediate, expected);
            immediate.TestOnly_AdvanceDays(1, true);
            AssertTotals(immediate, expected); // Includes every final home fixture before the reset.
            ClubFinanceEntry[] completed = immediate.FinanceEntriesForSave();
            Assert.That(completed[0].Finances.SeasonRevenueAccrued, Is.GreaterThan(0L));
            SeasonLoop delayed = Restore(immediate, "final-completed");
            uint opening = immediate.State.Calendar.DayOfRound(immediate.State.Calendar.RoundCount - 1)
                + (uint)SeasonLoopConstants.SeasonBreakDays;
            int breakDays = checked((int)(opening - immediate.CurrentWorldDay));

            immediate.RollToNextSeason();
            delayed.TestOnly_AdvanceDays(breakDays, true);
            CollectionAssert.AreEqual(completed, delayed.FinanceEntriesForSave());
            delayed.RollToNextSeason();
            CollectionAssert.AreEqual(immediate.FinanceEntriesForSave(), delayed.FinanceEntriesForSave());
            AssertSettledTotals(immediate, completed);
            immediate.TestOnly_AdvanceDays(breakDays, true);
            AssertSettledTotals(immediate, completed);
            Assert.That(SaveBytes(immediate, "immediate"), Is.EqualTo(SaveBytes(delayed, "delayed")));
        }

        private static void AdvanceToFixture(SeasonLoop loop, long[] expected = null)
        {
            uint target = loop.State.Calendar.NextFixtureDay();
            while (loop.CurrentWorldDay < target)
            {
                if (expected != null) CountDay(loop, expected);
                loop.TestOnly_AdvanceDays(1, true);
            }
        }

        private static void CountDay(SeasonLoop loop, long[] expected)
        {
            uint day = loop.CurrentWorldDay;
            if (day < loop.State.Calendar.DayOfRound(0)
                || day > loop.State.Calendar.DayOfRound(loop.State.Calendar.RoundCount - 1)) return;
            for (int club = 0; club < 4; club++)
            {
                expected[club] += 1_000L;
                foreach (Fixture fixture in loop.State.Fixtures)
                {
                    if (fixture.HomeClubId == club && loop.State.Calendar.DayOfRound(fixture.RoundIndex) == day)
                    {
                        Assert.That(fixture.Played, Is.True);
                        expected[club] += MatchdayForPosition(loop.State.PositionOf(club));
                    }
                }
            }
        }

        // Independent default-catalogue oracle for a four-club league, including inexact integer division.
        private static long MatchdayForPosition(int position) => new[] { 38_000L, 33_360L, 28_680L, 24_000L }[position - 1];

        private static void AssertOpeningRoundTotals(SeasonLoop loop)
        {
            var expected = new[] { 1_000L, 1_000L, 1_000L, 1_000L };
            foreach (Fixture fixture in loop.State.Fixtures)
            {
                if (fixture.RoundIndex == 0) expected[fixture.HomeClubId] += MatchdayForPosition(loop.State.PositionOf(fixture.HomeClubId));
            }
            AssertTotals(loop, expected);
        }

        private static void AssertTotals(SeasonLoop loop, long[] expected)
        {
            ClubFinanceEntry[] entries = loop.FinanceEntriesForSave();
            for (int club = 0; club < 4; club++)
            {
                Assert.That(entries[club].Finances.SeasonRevenueAccrued, Is.EqualTo(expected[club]));
                Assert.That(entries[club].Finances.Balance, Is.EqualTo(500_000L + expected[club]));
                Assert.That(entries[club].Finances.TransferBudget, Is.Zero);
                Assert.That(entries[club].Finances.WageBudget, Is.Zero);
                Assert.That(entries[club].Finances.WageBillAggregate, Is.Zero);
                Assert.That(entries[club].Finances.FfpBalanceWindow, Is.Zero);
            }
        }

        private static void AssertSettledTotals(SeasonLoop loop, ClubFinanceEntry[] completed)
        {
            ClubFinanceEntry[] entries = loop.FinanceEntriesForSave();
            foreach (ClubFinanceEntry entry in entries)
            {
                Assert.That(entry.Finances.SeasonRevenueAccrued, Is.Zero);
                Assert.That(entry.Finances.Balance, Is.GreaterThan(completed[entry.ClubId].Finances.Balance));
            }
        }

        private static ClubFinanceEntry[] LiveEntries(SeasonLoop loop) => (ClubFinanceEntry[])typeof(SeasonLoop)
            .GetField("_finances", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(loop);

        private byte[] SaveBytes(SeasonLoop loop, string name)
        {
            string path = Path.Combine(_tempDirectory, name + ".season");
            SeasonSaveManager.Save(loop, null, path);
            return File.ReadAllBytes(path);
        }

        private SeasonLoop Restore(SeasonLoop loop, string name)
        {
            SaveBytes(loop, name);
            SeasonSaveContents contents = SeasonSaveManager.Load(Path.Combine(_tempDirectory, name + ".season"));
            return SeasonLoop.Restore(contents.World, SeasonStateCodec.Encode(contents.Season),
                RoundResolutionMode.QuickSimAll, financesOrNull: contents.Finances);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-13 | —      | T3b2 non-zero count, save/restore, retry, fixture and season-attribution locks. |
#endregion
