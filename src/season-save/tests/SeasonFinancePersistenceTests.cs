// File:     src/season-save/tests/SeasonFinancePersistenceTests.cs
// Created:  2026-09-10
// Modified: 2026-10-08 (#40 T2a — prove bootstrap Squad.ClubId universe composes with SeasonState.ClubIds)
// Author:   —
// Spec:     Club Finances & Economy #40 FR-FN-020/021/025; Season & Competition Loop #30 Appendix B.1;
//           ERR-030-050; Code Standards #20
// Purpose:  Locks the T1b finance resume seam, the current-season ClubId coherence rule, and T2a's
//           canonical #27 bootstrap universe so save/composition cannot drift between club identities.

using System;
using System.IO;

using NUnit.Framework;

using TacticalDirector.ClubFinances;
using TacticalDirector.Discipline;
using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;
using TacticalDirector.TrainingSystem;

using CFinances = TacticalDirector.ClubFinances.ClubFinances;

namespace TacticalDirector.SeasonSave
{
    [TestFixture]
    public sealed class SeasonFinancePersistenceTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "td-finance-resume-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch (Exception) { }
        }

        private string TempPath(string name) => Path.Combine(_tempDir, name);

        private static int[] Clubs => new[] { 10, 11, 12, 13 };

        private static SeasonState Season() => SeasonState.CreateNew(
            Clubs,
            managedClubId: 11,
            seed: 0xF1A4CEUL,
            objective: new BoardObjective(2),
            firstRoundDay: 5u,
            daysBetweenRounds: 7u,
            seasonNumber: 3);

        private static Squad SquadFor(int clubId)
        {
            return new Squad(
                clubId,
                new[] { PlayerRecord.CreateDefault(clubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE) });
        }

        private static ClubFinanceEntry Entry(int clubId, long balance)
        {
            CFinances finances = CFinances.CreateInitial(balance);
            return new ClubFinanceEntry(clubId, in finances);
        }

        private static ClubFinanceEntry[] FullFinances() => new[]
        {
            Entry(13, -400),
            Entry(11, 2200),
            Entry(10, 1100),
            Entry(12, 3300)
        };

        private static void SaveLong(
            WorldStore world,
            SeasonState season,
            string path,
            ClubFinanceEntry[] finances)
        {
            SeasonSaveManager.Save(
                world,
                season,
                matchOrNull: null,
                path,
                Array.Empty<ClubTrainingStates>(),
                Array.Empty<ClubInjuryStates>(),
                Array.Empty<ClubAppearanceStates>(),
                ProgressionEngine.Empty,
                new DisciplineState(),
                disciplineWired: false,
                finances);
        }

        private static void AssertSameFinances(ClubFinanceEntry[] expected, ClubFinanceEntry[] actual)
        {
            ClubFinanceEntry[] left = (ClubFinanceEntry[])expected.Clone();
            ClubFinanceEntry[] right = (ClubFinanceEntry[])actual.Clone();
            Array.Sort(left, (x, y) => x.ClubId.CompareTo(y.ClubId));
            Array.Sort(right, (x, y) => x.ClubId.CompareTo(y.ClubId));

            Assert.AreEqual(left.Length, right.Length);
            for (int i = 0; i < left.Length; i++)
            {
                Assert.AreEqual(left[i].ClubId, right[i].ClubId, $"ClubId at index {i}");
                Assert.AreEqual(left[i].Finances.Balance, right[i].Finances.Balance, $"Balance for club {left[i].ClubId}");
                Assert.AreEqual(left[i].Finances.TransferBudget, right[i].Finances.TransferBudget, $"TransferBudget for club {left[i].ClubId}");
                Assert.AreEqual(left[i].Finances.WageBudget, right[i].Finances.WageBudget, $"WageBudget for club {left[i].ClubId}");
                Assert.AreEqual(left[i].Finances.WageBillAggregate, right[i].Finances.WageBillAggregate, $"WageBillAggregate for club {left[i].ClubId}");
                Assert.AreEqual(left[i].Finances.SeasonRevenueAccrued, right[i].Finances.SeasonRevenueAccrued, $"SeasonRevenueAccrued for club {left[i].ClubId}");
                Assert.AreEqual(left[i].Finances.FfpBalanceWindow, right[i].Finances.FfpBalanceWindow, $"FfpBalanceWindow for club {left[i].ClubId}");
            }
        }

        /// <summary>T-FN-LIFE-001: #27 bootstrap identities and #30's season club universe compose exactly.</summary>
        [Test]
        public void BootstrapSquadUniverse_ExactlyMatchesSeasonFinanceCoherenceUniverse()
        {
            Squad[] squads =
            {
                SquadFor(13),
                SquadFor(10),
                SquadFor(12),
                SquadFor(11)
            };

            ClubFinanceEntry[] bootstrap = ClubFinanceEntry.CreateInitialForSquads(squads);
            ClubFinanceEntry[] canonical = SeasonFinanceCoherence.Normalize(Season(), bootstrap, "finances");

            Assert.AreEqual(4, canonical.Length);
            Assert.AreEqual(10, canonical[0].ClubId);
            Assert.AreEqual(11, canonical[1].ClubId);
            Assert.AreEqual(12, canonical[2].ClubId);
            Assert.AreEqual(13, canonical[3].ClubId);
        }

        [Test]
        public void Load_RestoreLoop_SaveAs_PreservesPopulatedFinances()
        {
            WorldStore world = new WorldStore(managerId: 0);
            SeasonState season = Season();
            ClubFinanceEntry[] expected = FullFinances();
            string sourcePath = TempPath("source.season");
            string copyPath = TempPath("copy.season");

            SaveLong(world, season, sourcePath, expected);
            SeasonSaveContents loaded = SeasonSaveManager.Load(sourcePath);
            Assert.AreEqual(4, loaded.Finances.Length, "The source must be non-empty or this regression is vacuous.");

            SeasonLoop resumed = SeasonLoop.Restore(
                loaded.World,
                SeasonStateCodec.Encode(loaded.Season),
                RoundResolutionMode.QuickSimAll,
                financesOrNull: loaded.Finances);

            SeasonSaveManager.Save(resumed, matchOrNull: null, copyPath);
            SeasonSaveContents copied = SeasonSaveManager.Load(copyPath);

            AssertSameFinances(expected, copied.Finances);
        }

        /// <summary>T-FN-DAY-005: daily identity resumes across pre/post-fixture saves without a finance cursor.</summary>
        [Test]
        public void DailyAccounting_SaveRestoreBeforeAndAfterFixture_EqualsUninterruptedContinuation()
        {
            const ulong seed = 0x40F1AACEUL;
            League league = LeagueBootstrap.Generate(seed, 4);
            var world = new WorldStore(0, seed);
            SeasonLoop straight = league.CreateLoop(world, 0, RoundResolutionMode.QuickSimAll);
            var fee = new FinanceTransaction(FinanceTransactionKind.Debit, FinanceLineItem.General, 123L);
            var wage = new FinanceTransaction(FinanceTransactionKind.Debit, FinanceLineItem.PlayerWage, 456L);
            straight.ApplyTransaction(1, in fee);
            straight.ApplyTransaction(2, in wage);
            ClubFinanceEntry[] expected = straight.FinanceEntriesForSave();

            straight.AdvanceDays(2);
            SeasonLoop resumed = SaveAndRestore(straight, "off-day.season");
            AssertSameFinances(expected, resumed.FinanceEntriesForSave());
            straight.AdvanceToNextFixtureDay();
            resumed.AdvanceToNextFixtureDay();
            resumed = SaveAndRestore(resumed, "pre-fixture.season");
            straight.AdvanceAndPlayNextRound(league);
            resumed.AdvanceAndPlayNextRound(league);
            resumed = SaveAndRestore(resumed, "post-fixture.season");
            straight.AdvanceDays(1);
            resumed.AdvanceDays(1);

            AssertSameFinances(expected, resumed.FinanceEntriesForSave());
            Assert.That(resumed.Snapshot(), Is.EqualTo(straight.Snapshot()));
            SeasonSaveManager.Save(straight, null, TempPath("straight.season"));
            SeasonSaveManager.Save(resumed, null, TempPath("resumed.season"));
            Assert.That(File.ReadAllBytes(TempPath("resumed.season")),
                Is.EqualTo(File.ReadAllBytes(TempPath("straight.season"))),
                "The complete save, including the world and RNG state, must continue identically.");
        }

        private SeasonLoop SaveAndRestore(SeasonLoop loop, string name)
        {
            string path = TempPath(name);
            SeasonSaveManager.Save(loop, null, path);
            SeasonSaveContents contents = SeasonSaveManager.Load(path);
            return SeasonLoop.Restore(contents.World, SeasonStateCodec.Encode(contents.Season),
                RoundResolutionMode.QuickSimAll, financesOrNull: contents.Finances);
        }

        [Test]
        public void Save_NonEmptyFinancesMissingCurrentSeasonClub_FailsLoud()
        {
            WorldStore world = new WorldStore(managerId: 0);
            SeasonState season = Season();
            ClubFinanceEntry[] missing = new[] { Entry(10, 1), Entry(11, 2), Entry(13, 4) };

            var ex = Assert.Throws<ArgumentException>(
                () => SaveLong(world, season, TempPath("missing.season"), missing));

            Assert.AreEqual("finances", ex.ParamName);
        }

        [Test]
        public void Save_NonEmptyFinancesContainingForeignClub_FailsLoud()
        {
            WorldStore world = new WorldStore(managerId: 0);
            SeasonState season = Season();
            ClubFinanceEntry[] foreign = new[]
            {
                Entry(10, 1), Entry(11, 2), Entry(12, 3), Entry(99, 4)
            };

            var ex = Assert.Throws<ArgumentException>(
                () => SaveLong(world, season, TempPath("foreign.season"), foreign));

            Assert.AreEqual("finances", ex.ParamName);
        }

        [Test]
        public void Restore_NonEmptyFinancesMissingCurrentSeasonClub_FailsAtComposition()
        {
            WorldStore world = new WorldStore(managerId: 0);
            SeasonState season = Season();
            ClubFinanceEntry[] missing = new[] { Entry(10, 1), Entry(11, 2), Entry(13, 4) };

            var ex = Assert.Throws<ArgumentException>(
                () => SeasonLoop.Restore(
                    world,
                    SeasonStateCodec.Encode(season),
                    RoundResolutionMode.QuickSimAll,
                    financesOrNull: missing));

            Assert.AreEqual("financesOrNull", ex.ParamName);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                        |
// | 1.0     | 2026-09-10 | —      | ERR-030-050 regression locks: populated load→loop→Save As;   |
// |         |            |        | missing/foreign ClubId refusal at save and composition.      |
// | 1.1     | 2026-09-11 | —      | T2a: #27 Squad.ClubId bootstrap universe proven compatible with #30 SeasonState.ClubIds. |
// | 1.2     | 2026-10-08 | —      | T3b1: daily continuation through off-day and pre/post-fixture saves. |
#endregion
