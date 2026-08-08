// File:     src/season-save/tests/SeasonLoopProgressionTests.cs
// Created:  2026-08-08
// Modified: 2026-08-08
// Author:   —
// Spec:     Season & Competition Loop #30 §3.3 (KD-2 slot 1); Player Progression & Lifecycle #28
//           KD-4 / FR-PG-021 / FR-PG-022; ERR-029-006 (the batch entry point, closed here);
//           ERR-030-027 (the twice-per-fixture-day call); Code Standards #20
// Purpose:  Locks that #30's slot 1 actually EXECUTES — the wiring proof — plus the single-roster-
//           authority refusals the constructor now enforces. A dead seam and a live one are otherwise
//           indistinguishable from outside, which is the ERR-030-014 failure mode one layer up.

using System;

using NUnit.Framework;

using TacticalDirector.LivingWorld;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    internal sealed class SeasonLoopProgressionTests
    {
        private const ulong WorldSeed = 0x9E3779B97F4A7C15UL;
        private const int ManagerId = 1;
        private const int ClubCount = 4;

        // ── The wiring proof ──────────────────────────────────────────────────────────

        [Test]
        public void AdvanceDays_DrivesSlot1_AndTheCursorTracksTheClock()
        {
            // THE lock this landing exists for. Before #28 T2a slot 1 was a bare comment; if it reverts
            // to one, every other test here still passes because nothing else observes progression.
            // What proves the seam is live is that the store's per-player cursor moved BY THE NUMBER OF
            // DAYS the loop advanced — not merely that something changed.
            SeasonLoop loop = NewProgressionLoop(out _, out ProgressionEngine progression);

            long before = TotalCursor(progression);
            // Six days: the KD-4 cursor invariant refuses an advance past the pending round's fixture
            // day (day 7), which is itself the reason a longer run belongs in ProgressionEngineTests.
            loop.AdvanceDays(6);

            long after = TotalCursor(progression);
            Assert.AreNotEqual(before, after,
                "slot 1 must actually run — an unwired seam leaves the store untouched.");
            Assert.AreEqual(6 * PlayersInLeague(progression), Math.Abs(after - before),
                "one day of accrual per player per advanced day: the cursor tracks the world clock, so " +
                "a seam that ran once, twice, or not at all is all distinguishable here.");
        }

        [Test]
        public void AdvanceDays_LeavesTheWorldDigestUntouched()
        {
            // FR-SN-026 / KD-8: the career day steps mutate career state, never the world blob. #28's
            // block is its own sub-blob, so wiring slot 1 must not move the world snapshot.
            SeasonLoop wired = NewProgressionLoop(out WorldStore wiredWorld, out _);
            SeasonLoop bare = NewBareLoop(out WorldStore bareWorld);

            wired.AdvanceDays(6);
            bare.AdvanceDays(6);

            CollectionAssert.AreEqual(bareWorld.Snapshot(), wiredWorld.Snapshot(),
                "driving #28 must leave the world blob byte-identical — its state lives elsewhere.");
        }

        // ── Single roster authority (the constructor's refusals) ──────────────────────

        [Test]
        public void Constructor_WithBothAProviderAndAProgressionStore_IsRefused()
        {
            // Accepting both would let a caller hold the day-0 bootstrap beside the store that has been
            // evolving away from it — two surfaces agreeing only at the moment anything compares them.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(ManagerId, WorldSeed);
            SeasonState season = league.CreateSeason(managedClubId: 0);
            ProgressionEngine progression = SeedFor(league);
            var career = PlayerCareerStates.ForLeague(
                new ProgressionSquads(progression), ClubIds(), injuryOccurrenceEnabled: true);

            Assert.Throws<ArgumentException>(
                () => new SeasonLoop(
                    world, season, RoundResolutionMode.QuickSimAll, career, league, progression),
                "a progression store and a separate squad provider are two roster authorities.");
        }

        [Test]
        public void Constructor_WithAProgressionStoreMissingASeasonClub_IsRefused()
        {
            // A subset store would construct and advance days without complaint, then hand back a null
            // squad part-way through a round — after earlier fixtures had already hit the table.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(ManagerId, WorldSeed);
            SeasonState season = league.CreateSeason(managedClubId: 0);

            var subset = new Squad[ClubCount - 1];
            for (int c = 0; c < subset.Length; c++)
            {
                subset[c] = league.ResolveByClubId(c);
            }
            ProgressionEngine partial = ProgressionEngine.SeedFrom(subset, world.CurrentWorldTick);
            var career = PlayerCareerStates.ForLeague(
                new ProgressionSquads(partial), ClubIdsOf(subset.Length), injuryOccurrenceEnabled: true);

            Assert.Throws<ArgumentException>(
                () => new SeasonLoop(
                    world, season, RoundResolutionMode.QuickSimAll, career, null, partial),
                "the store must cover every club that plays in this season.");
        }

        // ── Save / resume through the frame ───────────────────────────────────────────

        [Test]
        public void SaveThenLoad_RestoresTheEvolvedRoster_NotTheBootstrap()
        {
            // The resume path the retired LeagueBootstrapTests rule used to cover. A save carries the
            // roster now (#28 KD-4), so a reload must bring back the attributes the career banked —
            // the whole reason SEASON_SAVE_FORMAT_VERSION went 4 -> 5.
            SeasonLoop loop = NewProgressionLoop(out WorldStore world, out ProgressionEngine progression);
            loop.AdvanceDays(6);
            long liveCursor = TotalCursor(progression);
            int liveSum = SumAttributes(progression.SquadFor(0));
            Assert.AreNotEqual(0L, liveCursor, "precondition: the career must have banked something.");

            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), "td-prog-" + Guid.NewGuid().ToString("N") + ".sav");
            try
            {
                SeasonSaveManager.Save(loop, null, path);
                SeasonSaveContents restored = SeasonSaveManager.Load(path);

                Assert.AreEqual(liveCursor, TotalCursor(restored.Progression),
                    "the reloaded career must carry the banked cursor — a seed-rebuilt roster would " +
                    "silently reset it to zero with every gate green.");
                Assert.AreEqual(liveSum, SumAttributes(restored.Progression.SquadFor(0)),
                    "…and the roster itself must come back off the file (#28 KD-4).");
            }
            finally
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
        }

        // ── Fixtures ──────────────────────────────────────────────────────────────────

        private static int[] ClubIds() => ClubIdsOf(ClubCount);

        private static int[] ClubIdsOf(int count)
        {
            var ids = new int[count];
            for (int c = 0; c < count; c++)
            {
                ids[c] = c;
            }
            return ids;
        }

        private static ProgressionEngine SeedFor(League league)
        {
            var squads = new Squad[league.ClubCount];
            for (int c = 0; c < squads.Length; c++)
            {
                squads[c] = league.ResolveByClubId(c);
            }
            return ProgressionEngine.SeedFrom(squads, 0u);
        }

        // A loop with #28 wired: the provider is the projection, never the bootstrap (KD-4).
        private static SeasonLoop NewProgressionLoop(
            out WorldStore world, out ProgressionEngine progression)
        {
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            world = new WorldStore(ManagerId, WorldSeed);
            progression = SeedFor(league);
            var career = PlayerCareerStates.ForLeague(
                new ProgressionSquads(progression), ClubIds(), injuryOccurrenceEnabled: true);
            return new SeasonLoop(
                world, league.CreateSeason(managedClubId: 0), RoundResolutionMode.QuickSimAll,
                career, null, progression);
        }

        private static SeasonLoop NewBareLoop(out WorldStore world)
        {
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            world = new WorldStore(ManagerId, WorldSeed);
            return new SeasonLoop(
                world, league.CreateSeason(managedClubId: 0), RoundResolutionMode.QuickSimAll);
        }

        private static int PlayersInLeague(ProgressionEngine progression)
        {
            int n = 0;
            ClubCareerStates[] blocks = progression.ToBlocks();
            for (int c = 0; c < blocks.Length; c++)
            {
                n += blocks[c].Count;
            }
            return n;
        }

        private static long TotalCursor(ProgressionEngine progression)
        {
            long sum = 0;
            ClubCareerStates[] blocks = progression.ToBlocks();
            for (int c = 0; c < blocks.Length; c++)
            {
                for (int p = 0; p < blocks[c].Count; p++)
                {
                    sum += blocks[c].Lifecycles[p].GrowthCursor;
                }
            }
            return sum;
        }

        private static int SumAttributes(Squad squad)
        {
            int sum = 0;
            for (int p = 0; p < squad.Count; p++)
            {
                int[] a = squad.GetPlayer(p).Attributes.ToArray();
                for (int i = 0; i < a.Length; i++)
                {
                    sum += a[i];
                }
            }
            return sum;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-08-08 | —      | #28 T2a: the slot-1 wiring proof (cursor tracks the clock),      |
// |         |            |        | world-digest invariance, the KD-4 authority observation, the     |
// |         |            |        | two constructor refusals, and the save/resume roster lock.       |
#endregion
