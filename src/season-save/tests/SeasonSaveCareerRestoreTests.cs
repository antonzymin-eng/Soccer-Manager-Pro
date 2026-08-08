// File:     src/season-save/tests/SeasonSaveCareerRestoreTests.cs
// Created:  2026-08-06
// Modified: 2026-08-07
// Author:   —
// Spec:     Unified season save file (docs/tracking/unified-season-save-design.md) §4 / KD-3 / KD-6;
//           Injuries & Medical #41 FR-MD-023; Squad/Player Data Layer #27 T3 (KD-T3-3, restore
//           fidelity); path-to-playable D2/D3 (T2); Code Standards #20
// Purpose:  Locks the two save-root consequences of the #29/#41 T2 wiring: the Save(SeasonLoop, …)
//           overload that lets the career's block accessors stay internal, and — the one that matters —
//           that a mid-match save whose squad was availability-filtered restores THE SAME eleven,
//           rather than an eleven re-selected from the unfiltered roster.

using System;
using System.IO;

using NUnit.Framework;

using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    internal class SeasonSaveCareerRestoreTests
    {
        private const ulong WorldSeed = 0x5EED1EA6D0DEC0DEUL;
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;
        private const int ManagerId = 1;
        private const int ClubCount = 4;

        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "td-seasoncareer-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch (Exception) { }
        }

        private string TempPath(string name) => Path.Combine(_tempDir, name);

        private static League FourClubLeague() => LeagueBootstrap.Generate(WorldSeed, ClubCount);

        private static CareerTestRoster.MutableSquadProvider ProviderOver(League league)
        {
            var provider = new CareerTestRoster.MutableSquadProvider();
            int[] ids = league.ClubIds();
            for (int i = 0; i < ids.Length; i++)
            {
                provider.Set(league.ResolveByClubId(ids[i]));
            }

            return provider;
        }

        // ── the Save(SeasonLoop, …) overload ───────────────────────────────────────────────

        [Test]
        public void SaveThroughTheLoop_RoundTripsTheCareer()
        {
            League league = FourClubLeague();
            CareerTestRoster.MutableSquadProvider provider = ProviderOver(league);
            PlayerCareerStates career = PlayerCareerStates.ForLeague(provider, league.ClubIds(), injuryOccurrenceEnabled: false);
            var world = new WorldStore(ManagerId, WorldSeed);
            var loop = new SeasonLoop(
                world, league.CreateSeason(0), RoundResolutionMode.QuickSimAll, career, provider);

            loop.AdvanceDays(5);

            int playerId = provider.ResolveByClubId(0).GetPlayer(0).PlayerId;
            int conditioned = career.TrainingView(0, playerId).Condition;

            string path = TempPath("season.save");
            SeasonSaveManager.Save(loop, matchOrNull: null, path);

            SeasonSaveContents contents = SeasonSaveManager.Load(path, provider);
            PlayerCareerStates restored =
                PlayerCareerStates.FromBlocks(
                    contents.TrainingClubs, contents.MedicalClubs, contents.AppearanceClubs,
                    injuryOccurrenceEnabled: false);

            Assert.AreEqual(conditioned, restored.TrainingView(0, playerId).Condition,
                "The loop overload must write the career the loop actually drives — not an empty block.");
        }

        [Test]
        public void SaveThroughAnUnwiredLoop_WritesWellFormedEmptyBlocks()
        {
            League league = FourClubLeague();
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(0),
                RoundResolutionMode.QuickSimAll);

            string path = TempPath("empty.save");
            Assert.DoesNotThrow(() => SeasonSaveManager.Save(loop, matchOrNull: null, path));

            SeasonSaveContents contents = SeasonSaveManager.Load(path, league);
            Assert.AreEqual(0, contents.TrainingClubs.Length);
            Assert.AreEqual(0, contents.MedicalClubs.Length);
        }

        // ── the restore-fidelity lock ──────────────────────────────────────────────────────

        [Test]
        public void AMidMatchSave_RestoresTheElevenThatActuallyPlayed()
        {
            // The defect this locks: the match is configured with the AVAILABILITY-FILTERED squad, and
            // the snapshot records only each team's ClubId — it cannot record WHICH eighteen of the
            // twenty-five. Restoring through the raw provider therefore re-ran LineupSelector over the
            // full roster and put a different eleven's canonical attribute records on the pitch, with
            // every gate green (ClubId matches, size check passes) and no way to notice.
            //
            // Club 0's roster is engineered so the filter changes the answer: the seven best-rated
            // players are injured, so an unfiltered re-selection picks several of them and a filtered
            // one cannot.
            League league = FourClubLeague();
            CareerTestRoster.MutableSquadProvider provider = ProviderOver(league);
            provider.Set(CareerTestRoster.Build(0, PlayerDatabaseConstants.CLUB_SQUAD_SIZE));

            PlayerCareerStates career = PlayerCareerStates.ForLeague(provider, league.ClubIds(), injuryOccurrenceEnabled: false);
            Squad full = provider.ResolveByClubId(0);
            for (int local = full.Count - 1; local >= full.Count - 7; local--)
            {
                var injured = InjuryState.Create();
                injured.Severity = InjurySeverity.Serious;
                injured.RecoveryRemaining = 60;
                career.SetMedicalState(0, full.GetPlayer(local).PlayerId, in injured);
            }

            Squad fielded = career.SelectAvailable(full);
            Assert.AreNotSame(full, fielded, "Precondition: the filter must have removed somebody.");
            Assert.AreNotEqual(
                SquadRating.StartingElevenMean(full), SquadRating.StartingElevenMean(fielded),
                "Precondition: the filter must change WHICH eleven is selected, or this test cannot "
                + "distinguish a correct restore from a broken one.");

            var engine = new MatchEngine.MatchEngine(MatchSeed);
            engine.ConfigureSquads(fielded, provider.ResolveByClubId(1));
            for (int t = 0; t < 10; t++)
            {
                engine.RunTick();
            }

            var world = new WorldStore(ManagerId, WorldSeed);
            var loop = new SeasonLoop(
                world, league.CreateSeason(0), RoundResolutionMode.QuickSimAll, career, provider);

            string path = TempPath("midmatch.save");
            SeasonSaveManager.Save(loop, engine, path);

            // The reference continuation: what the un-saved match goes on to do. The canonical
            // attribute records are not serialized (they are re-derived from the roster at restore),
            // so the only way to see WHICH eleven came back is to keep simulating — a different eleven
            // moves differently and the digest chain separates. The G4 disk-round-trip pattern.
            const int Continue = 60;
            var reference = new byte[Continue][];
            for (int t = 0; t < Continue; t++)
            {
                engine.RunTick();
                reference[t] = engine.CurrentSnapshotDigest;
            }

            // Loaded with the UNFILTERED provider — which is all any caller has.
            SeasonSaveContents contents = SeasonSaveManager.Load(path, provider);
            Assert.IsNotNull(contents.Match, "Precondition: the save carried a match.");

            for (int t = 0; t < Continue; t++)
            {
                contents.Match.RunTick();
                CollectionAssert.AreEqual(reference[t], contents.Match.CurrentSnapshotDigest,
                    $"Digest diverged {t + 1} ticks after restore. The match was configured with the "
                    + "availability-FILTERED squad and the snapshot records only the ClubId, so a "
                    + "restore that re-selects from the unfiltered roster puts a different eleven's "
                    + "attribute records on the pitch — silently, with every gate green.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-08-06 | —      | Initial (T2 AR pass 1): the Save(SeasonLoop, …) overload's      |
// |         |            |        | round trip and its empty-block floor, plus the restore-fidelity |
// |         |            |        | lock — a mid-match save whose squad was availability-filtered   |
// |         |            |        | must restore the same eleven, not one re-selected from the      |
// |         |            |        | unfiltered roster.                                              |
// | 1.1     | 2026-08-07 | —      | Balance pass D2/D4: FromBlocks carries the appearance block and    |
// |         |            |        | the now-required dial argument (false here — these locks isolate   |
// |         |            |        | restore fidelity, not occurrence).                                 |
#endregion
