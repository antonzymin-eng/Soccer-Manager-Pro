// File:     src/season-save/tests/SeasonFinanceLoadCoherenceTests.cs
// Created:  2026-09-10
// Modified: 2026-09-10
// Author:   —
// Spec:     Club Finances & Economy #40 FR-FN-020/021/025; Season & Competition Loop #30 Appendix B.1;
//           ERR-030-050; Code Standards #20
// Purpose:  Proves the season-save load boundary rejects a structurally valid finance sub-blob whose
//           ClubId set disagrees with the SeasonState sub-blob.

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
    public sealed class SeasonFinanceLoadCoherenceTests
    {
        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "td-finance-load-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch (Exception) { }
        }

        private static ClubFinanceEntry Entry(int clubId, long balance)
        {
            CFinances finances = CFinances.CreateInitial(balance);
            return new ClubFinanceEntry(clubId, in finances);
        }

        [Test]
        public void Load_NonEmptyFinanceBlockMissingCurrentSeasonClub_FailsLoud()
        {
            int[] clubs = { 10, 11, 12, 13 };
            SeasonState season = SeasonState.CreateNew(
                clubs,
                managedClubId: 11,
                seed: 0xF1A4CEUL,
                objective: new BoardObjective(2),
                firstRoundDay: 5u,
                daysBetweenRounds: 7u,
                seasonNumber: 3);
            var world = new WorldStore(managerId: 0);

            ClubFinanceEntry[] incomplete =
            {
                Entry(10, 100),
                Entry(11, 200),
                Entry(13, 400)
            };

            var training = new TrainingBlock(
                TrainingSaveCodec.Encode(Array.Empty<ClubTrainingStates>()));
            var medical = new MedicalBlock(
                MedicalSaveCodec.Encode(Array.Empty<ClubInjuryStates>()));
            var appearance = new AppearanceBlock(
                AppearanceSaveCodec.Encode(Array.Empty<ClubAppearanceStates>()));
            var progression = new ProgressionBlock(ProgressionEngine.Empty.Snapshot());
            var discipline = new DisciplineBlock(DisciplineSaveCodec.Encode(new DisciplineState()));
            var finance = new FinanceBlock(ClubFinancesSaveCodec.Encode(incomplete));

            byte[] frame = SeasonSaveCodec.Encode(
                world.Snapshot(),
                SeasonStateCodec.Encode(season),
                in training,
                in medical,
                in appearance,
                in progression,
                in discipline,
                in finance,
                matchBlobOrNull: null);

            string path = Path.Combine(_tempDir, "mispaired.season");
            File.WriteAllBytes(path, frame);

            var ex = Assert.Throws<ArgumentException>(() => SeasonSaveManager.Load(path));
            Assert.AreEqual("finances", ex.ParamName);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                        |
// | 1.0     | 2026-09-10 | —      | ERR-030-050: raw-frame Load-side ClubId coherence lock.      |
#endregion
