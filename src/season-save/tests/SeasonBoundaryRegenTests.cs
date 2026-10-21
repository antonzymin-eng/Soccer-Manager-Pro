// File:     src/season-save/tests/SeasonBoundaryRegenTests.cs
// Created:  2026-10-19
// Modified: 2026-10-20 (AR round 4 L16 — the regen cursor is also compared with a non-regen peer's on the same club — v1.2)
//           2026-10-20 (P-M6 3/8/7/5 counts; P-M2(c) regen cursor/age lock — v1.1)
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
                Assert.That(PositionCounts(squad), Is.EqualTo(new[] { 3, 8, 7, 5 }),
                    "every regen took its retiree's position (ERR-028-024), so the KD-6 shape (3/8/7/5) survives");
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
        public void EveryRegen_IsAnchoredOnTheLastLivedDay_AndKeepsItsDrawnAge()
        {
            // P-M2(c). RollToNextSeason constructs regens on the last LIVED day (clock − 1), where every
            // carried player's progression cursor sits (ERR-030-053 guarantees the clock is past the final
            // fixture day, so the subtraction cannot wrap). A mutant passing CurrentWorldTick instead
            // puts the cursor one day ahead of the world's last lived day and, because BirthWorldDay =
            // anchor − age × 365, derives every regen one year younger on that last lived day.
            Career c = NewCareer(out int[] retirees, out _);
            int cursorBefore = c.Progression.NextPlayerId;
            PlayWholeSeason(c.Loop);

            c.Loop.RollToNextSeason();

            uint lastLivedDay = c.Loop.CurrentWorldDay - 1u;
            int regens = 0;
            int peerComparisons = 0;
            ClubCareerStates[] blocks = c.Progression.ToBlocks();
            for (int b = 0; b < blocks.Length; b++)
            {
                ClubCareerStates block = blocks[b];

                // L16 (AR round 4): the M12 MUST is that a regen's cursor EQUALS his peers' — the next day
                // step advances all of them together. Asserting against the stand-in 'CurrentWorldDay - 1'
                // alone would stay green if the peers' cursor and that stand-in ever diverged, so compare
                // against a non-regen peer on the same club directly.
                bool havePeer = false;
                uint peerCursor = 0u;
                for (int i = 0; i < block.Records.Length && !havePeer; i++)
                {
                    if (block.Records[i].PlayerId < cursorBefore)
                    {
                        havePeer = true;
                        peerCursor = block.Lifecycles[i].LastAdvancedWorldDay;
                    }
                }

                for (int i = 0; i < block.Records.Length; i++)
                {
                    PlayerRecord record = block.Records[i];
                    if (record.PlayerId < cursorBefore)
                    {
                        continue;
                    }

                    regens++;
                    PlayerLifecycle life = block.Lifecycles[i];
                    Assert.That(life.LastAdvancedWorldDay, Is.EqualTo(lastLivedDay),
                        $"regen {record.PlayerId}: progression cursor must be the roll day minus one");
                    if (havePeer)
                    {
                        peerComparisons++;
                        Assert.That(life.LastAdvancedWorldDay, Is.EqualTo(peerCursor),
                            $"regen {record.PlayerId}: progression cursor must equal a non-regen peer's on club {block.ClubId}");
                    }

                    // Age is DERIVED as (day − BirthWorldDay) / DAYS_PER_YEAR (KD-A); on the last lived day
                    // it must equal the drawn age the record carries.
                    long derivedAge = ((long)lastLivedDay - life.BirthWorldDay)
                        / PlayerProgressionConstants.DAYS_PER_YEAR;
                    Assert.That(derivedAge, Is.EqualTo((long)record.Age),
                        $"regen {record.PlayerId}: derived age on the last lived day must equal his drawn age");
                }
            }

            Assert.That(regens, Is.GreaterThanOrEqualTo(retirees.Length),
                "non-vacuity: at least the three forced retirees were replaced by regens");
            Assert.That(peerComparisons, Is.EqualTo(regens),
                "non-vacuity: every regen was compared with a surviving non-regen peer on his club");
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
                    Assert.That(PositionCounts(squad), Is.EqualTo(new[] { 3, 8, 7, 5 }), $"season {season} club {club}");
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
        // first defender, club 1's index-19 forward (FW slots are indices 18-22 of the 3/8/7/5 template). The goalkeeper also carries a 50-match ban — longer than
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
// | 1.1     | 2026-10-20 | —      | P-M6: position-shape expectations {3,8,8,4} -> {3,8,7,5}; club 1 index 19 is still a forward. |
// |         |            |        | P-M2(c): EveryRegen_IsAnchoredOnTheLastLivedDay_AndKeepsItsDrawnAge — every regen's cursor is |
// |         |            |        | clock - 1 and his age derived on that day equals his drawn age (kills a CurrentWorldTick mutant). |
// | 1.2     | 2026-10-20 | —      | AR round 4 L16: EveryRegen_IsAnchoredOnTheLastLivedDay_AndKeepsItsDrawnAge also asserts each regen's LastAdvancedWorldDay equals a non-regen peer's on the same club (the M12 MUST itself, not only its clock-1 stand-in), with a non-vacuity count. |
#endregion
