// File:     src/season-save/tests/SeasonBoundaryRegenTests.cs
// Created:  2026-10-19
// Modified: 2026-10-19
// Author:   —
// Spec:     Season & Competition Loop #30 §3.5 step (d)/(d′); Player Progression & Lifecycle #28 §3.4
//           (as amended by ERR-028-024 / ERR-028-025), FR-PG-011/015/019/024; Discipline #44 FR-DC-013;
//           Code Standards #20
// Purpose:  Locks the #28 season boundary as #30 drives it: retirees leave every career set (progression,
//           #29/#41/#30 rows, #44 tally) and are replaced 1:1, in position, by fresh-id regens with fresh
//           career rows; a save taken before the roll reproduces the same regens; the rolled career saves
//           and reloads; and across several boundaries every club keeps its squad shape.

using System;
using System.IO;

using NUnit.Framework;

using TacticalDirector.ClubFinances;
using TacticalDirector.Discipline;
using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    public sealed class SeasonBoundaryRegenTests
    {
        private const ulong WorldSeed = 0x28B0DA25UL;
        private const int ClubCount = 4;
        private const int RetiredAge = 45;

        private string _tempDir;

        [SetUp]
        public void SetUp()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "td-regen-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempDir);
        }

        [TearDown]
        public void TearDown()
        {
            try { Directory.Delete(_tempDir, recursive: true); } catch (Exception) { }
        }

        [Test]
        public void ARolledSeason_ReplacesEveryRetiree_InEveryCareerSet()
        {
            Career c = NewCareer(out int[] retirees, out PlayerPosition[] retireePositions);
            int cursorBefore = c.Progression.NextPlayerId;
            PlayWholeSeason(c.Loop);

            Assert.That(c.Discipline.HasEntry(retirees[0], DisciplineConstants.LeagueCompetitionKey), Is.True,
                "precondition: the banned retiree still carries his unserved ban at the boundary");

            c.Loop.RollToNextSeason();

            Assert.That(c.Progression.NextPlayerId, Is.GreaterThanOrEqualTo(cursorBefore + retirees.Length),
                "at least the three forced retirees were replaced with fresh ids (FR-PG-011)");
            for (int r = 0; r < retirees.Length; r++)
            {
                int club = ClubOfRetiree(r);
                Squad squad = c.Progression.SquadFor(club);
                Assert.That(IndexOf(squad, retirees[r]), Is.LessThan(0), "the retiree left the roster");
                Assert.Throws<ArgumentException>(() => c.Loop.Career.IsAvailable(club, retirees[r]),
                    "…and the #29/#41/#30 career rows");
                Assert.That(c.Discipline.HasEntry(retirees[r], DisciplineConstants.LeagueCompetitionKey), Is.False,
                    "…and the #44 tally (FR-DC-013's retirement drop)");
            }

            for (int club = 0; club < ClubCount; club++)
            {
                Squad squad = c.Progression.SquadFor(club);
                Assert.That(squad.Count, Is.EqualTo(23), "1:1 replacement keeps the bootstrap size (FR-PG-019)");
                Assert.That(PositionCounts(squad), Is.EqualTo(new[] { 3, 8, 8, 4 }),
                    "every regen took its retiree's position (ERR-028-024), so the KD-6 shape survives");
                for (int i = 0; i < squad.Count; i++)
                {
                    int id = squad.GetPlayer(i).PlayerId;
                    if (id >= cursorBefore)
                    {
                        Assert.That(c.Loop.Career.IsAvailable(club, id), Is.True,
                            "a regen arrives with a fresh (uninjured) career row");
                        Assert.That(c.Discipline.HasEntry(id, DisciplineConstants.LeagueCompetitionKey), Is.False,
                            "a regen inherits no ban");
                    }
                }
            }

            Assert.That(retireePositions[0], Is.EqualTo(PlayerPosition.Goalkeeper), "fixture sanity");
        }

        [Test]
        public void ASaveTakenBeforeTheRoll_ReproducesTheSameRegens()
        {
            Career original = NewCareer(out _, out _);
            PlayWholeSeason(original.Loop);
            string path = Path.Combine(_tempDir, "pre-roll.season");
            SeasonSaveManager.Save(original.Loop, matchOrNull: null, path);

            original.Loop.RollToNextSeason();

            SeasonSaveContents contents = SeasonSaveManager.Load(path);
            var career = PlayerCareerStates.FromBlocks(
                contents.TrainingClubs, contents.MedicalClubs, contents.AppearanceClubs, injuryOccurrenceEnabled: false);
            SeasonLoop resumed = SeasonLoop.Restore(
                contents.World, SeasonStateCodec.Encode(contents.Season), RoundResolutionMode.QuickSimAll,
                career, null, contents.Progression, contents.Discipline, contents.Finances);
            resumed.RollToNextSeason();

            Assert.That(contents.Progression.Snapshot(), Is.EqualTo(original.Progression.Snapshot()),
                "the boundary-scoped regen stream carries no cursor: the restored roll draws the identical regens");
        }

        [Test]
        public void TheRolledCareer_SavesAndReloads()
        {
            Career c = NewCareer(out _, out _);
            PlayWholeSeason(c.Loop);
            c.Loop.RollToNextSeason();
            string path = Path.Combine(_tempDir, "post-roll.season");

            SeasonSaveManager.Save(c.Loop, matchOrNull: null, path);
            SeasonSaveContents contents = SeasonSaveManager.Load(path);

            Assert.That(contents.Progression.Snapshot(), Is.EqualTo(c.Progression.Snapshot()));
            Assert.That(contents.TrainingClubs.Length, Is.EqualTo(ClubCount));
        }

        [Test]
        public void AcrossThreeBoundaries_EveryClubKeepsItsShape_AndCanFieldAnEleven()
        {
            Career c = NewCareer(out _, out _);
            int totalRetired = 0;
            for (int season = 0; season < 3; season++)
            {
                int before = c.Progression.NextPlayerId;
                PlayWholeSeason(c.Loop);
                c.Loop.RollToNextSeason();
                totalRetired += c.Progression.NextPlayerId - before;

                for (int club = 0; club < ClubCount; club++)
                {
                    Squad squad = c.Progression.SquadFor(club);
                    Assert.That(PositionCounts(squad), Is.EqualTo(new[] { 3, 8, 8, 4 }), $"season {season} club {club}");
                    Assert.That(SquadRating.CanFieldStartingEleven(squad), Is.True);
                }
            }

            // Non-vacuity, with a football sanity band: three forced retirees plus natural ones from a
            // 17–35 bootstrap, over 4 clubs × 3 boundaries. Football's ~1–2 per senior squad per season
            // would be 12–24; this generous band only catches a boundary that retires nobody or everybody.
            Assert.That(totalRetired, Is.InRange(3, 4 * 23));
        }

        // ── fixture ──────────────────────────────────────────────────────────────────

        private sealed class Career
        {
            internal SeasonLoop Loop;
            internal ProgressionEngine Progression;
            internal DisciplineState Discipline;
        }

        // Three forced retirees (age 45 ⇒ flagged on the first lived day): club 0's first goalkeeper and
        // first defender, club 1's first forward. The goalkeeper also carries a 50-match ban — longer than
        // the 6-round season, so it is still unserved at the boundary.
        private static readonly int[][] RetireeSlots = { new[] { 0, 0 }, new[] { 0, 3 }, new[] { 1, 19 } };

        private static int ClubOfRetiree(int r) => RetireeSlots[r][0];

        private static Career NewCareer(out int[] retirees, out PlayerPosition[] positions)
        {
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(1, WorldSeed);
            SeasonState season = league.CreateSeason(managedClubId: 0);

            var squads = new Squad[ClubCount];
            for (int club = 0; club < ClubCount; club++)
            {
                squads[club] = league.ResolveByClubId(club);
            }

            retirees = new int[RetireeSlots.Length];
            positions = new PlayerPosition[RetireeSlots.Length];
            for (int r = 0; r < RetireeSlots.Length; r++)
            {
                int club = RetireeSlots[r][0];
                var players = new PlayerRecord[squads[club].Count];
                for (int i = 0; i < players.Length; i++)
                {
                    players[i] = squads[club].GetPlayer(i);
                }

                players[RetireeSlots[r][1]].Age = RetiredAge;
                retirees[r] = players[RetireeSlots[r][1]].PlayerId;
                positions[r] = players[RetireeSlots[r][1]].Position;
                squads[club] = new Squad(club, players);
            }

            ProgressionEngine progression = ProgressionEngine.SeedFrom(squads, world.CurrentWorldTick);
            var career = PlayerCareerStates.ForLeague(
                new ProgressionSquads(progression), ClubIds(), injuryOccurrenceEnabled: false);
            DisciplineState discipline = DisciplineState.FromEntries(new[]
            {
                new DisciplineEntry(retirees[0], DisciplineConstants.LeagueCompetitionKey, 0, 50)
            });

            var loop = new SeasonLoop(
                world, season, RoundResolutionMode.QuickSimAll, career, null, progression, discipline,
                ClubFinanceEntry.CreateInitialForSquads(squads));
            return new Career { Loop = loop, Progression = progression, Discipline = discipline };
        }

        private static void PlayWholeSeason(SeasonLoop loop)
        {
            while (!loop.IsSeasonComplete)
            {
                loop.AdvanceToNextFixtureDay();
                loop.AdvanceAndPlayNextRound();
            }

            loop.AdvanceDays(1);
        }

        private static int[] ClubIds()
        {
            var ids = new int[ClubCount];
            for (int c = 0; c < ClubCount; c++)
            {
                ids[c] = c;
            }

            return ids;
        }

        private static int[] PositionCounts(Squad squad)
        {
            var counts = new int[PlayerDatabaseConstants.POSITION_COUNT];
            for (int i = 0; i < squad.Count; i++)
            {
                counts[(int)squad.GetPlayer(i).Position]++;
            }

            return counts;
        }

        private static int IndexOf(Squad squad, int playerId)
        {
            for (int i = 0; i < squad.Count; i++)
            {
                if (squad.GetPlayer(i).PlayerId == playerId)
                {
                    return i;
                }
            }

            return -1;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-10-19 | —      | Initial implementation (ERR-028-024 / ERR-028-025): #30-driven    |
// |         |            |        | season-boundary regen locks.                                     |
#endregion
