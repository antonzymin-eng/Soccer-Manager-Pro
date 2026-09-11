// File:     src/season-save/tests/SeasonFinancePersistenceTests.cs
// Created:  2026-09-10
// Modified: 2026-09-10
// Author:   —
// Spec:     Club Finances & Economy #40 FR-FN-020/021/025; Season & Competition Loop #30 Appendix B.1;
//           ERR-030-050; Code Standards #20
// Purpose:  Locks the T1b finance resume seam and the current-season ClubId coherence rule that
//           prevents both Save-As loss and partial/wrong-club finance persistence.

using System;
using System.IO;

using NUnit.Framework;

using TacticalDirector.ClubFinances;
using TacticalDirector.Discipline;
using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
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
#endregion
