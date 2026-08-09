// File:     src/season-save/tests/SeasonLoopProgressionTests.cs
// Created:  2026-08-08
// Modified: 2026-08-08
// Author:   —
// Spec:     Season & Competition Loop #30 §3.3 (KD-2 slot 1); Player Progression & Lifecycle #28
//           KD-4 / FR-PG-021 / FR-PG-022; ERR-029-006 (the batch entry point, closed here);
//           ERR-030-027 (the twice-per-fixture-day call); ERR-028-007 (the fourth persisted cursor,
//           checked at three boundaries — this file covers the SeasonLoop constructor boundary);
//           Code Standards #20
// Purpose:  Locks that #30's slot 1 actually EXECUTES — the wiring proof — plus the single-roster-
//           authority refusals the constructor now enforces. A dead seam and a live one are otherwise
//           indistinguishable from outside, which is the ERR-030-014 failure mode one layer up. Also
//           the composition-boundary third of ERR-028-007's three cursor-vs-clock checks (the other
//           two — SeasonSaveManager.Save and .Load — live in SeasonSaveManagerTests.cs beside their
//           #29/#41 siblings).

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
        public void AdvanceDays_DrivesSlot1_AndEachPlayerAccruesHisOwnBandStep()
        {
            // THE lock this landing exists for. Before #28 T2a slot 1 was a bare comment; if it reverts
            // to one, every other test here still passes because nothing else observes progression.
            //
            // The expectation is built from the BOOTSTRAP age (#27's generated value), never from #28's
            // own derived age — otherwise this would re-derive the answer with the code under test and
            // pass no matter what that code did. An earlier version of this test asserted the far weaker
            // `6 x players == |sum of cursors|`, which was only ever satisfiable because a clamped birth
            // anchor had made EVERY player age 0 and therefore every band Growth (ERR-028-006). It went
            // green on a league where age was destroyed; this one cannot.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            SeasonLoop loop = NewProgressionLoop(out _, out ProgressionEngine progression);

            // Six days: the KD-4 cursor invariant refuses an advance past the pending round's fixture
            // day (day 7). Six days cannot cross a birthday, so each player's band is fixed throughout.
            const int Days = 6;
            loop.AdvanceDays(Days);

            int growth = 0, decline = 0, stable = 0;
            ClubCareerStates[] blocks = progression.ToBlocks();
            for (int c = 0; c < blocks.Length; c++)
            {
                Squad seeded = league.ResolveByClubId(blocks[c].ClubId);
                for (int p = 0; p < blocks[c].Count; p++)
                {
                    int playerId = blocks[c].Records[p].PlayerId;
                    int bootstrapAge = AgeOf(seeded, playerId);

                    long expected;
                    if (bootstrapAge < PlayerProgressionConstants.GROWTH_AGE) { expected = +Days; growth++; }
                    else if (bootstrapAge > PlayerProgressionConstants.DECLINE_AGE) { expected = -Days; decline++; }
                    else { expected = 0; stable++; }

                    Assert.AreEqual(expected, blocks[c].Lifecycles[p].GrowthCursor,
                        $"player {playerId} (bootstrap age {bootstrapAge}) must accrue his own band's "
                        + "step once per advanced day — slot 1 running not at all, twice, or on the "
                        + "wrong band is each distinguishable here.");
                }
            }

            // Guards the assertion above against a degenerate league: if every player landed in one band
            // the loop would still pass while proving far less. All three bands must be represented.
            Assert.Greater(growth, 0, "precondition: the league must contain Growth-band players.");
            Assert.Greater(decline, 0, "precondition: the league must contain Decline-band players.");
            Assert.Greater(stable, 0, "precondition: the league must contain Stable-band players.");
        }

        [Test]
        public void AdvanceDays_LeavesTheWorldDigestUntouched()
        {
            // FR-SN-026 / KD-8: the career day steps mutate career state, never the world blob.
            // Kept as a BOUNDARY guard, with its claim corrected (L2): #28's state lives in a separate
            // object with no path to WorldStore, so no change to slot 1 can turn this red — it proves
            // the separation holds, NOT that slot 1 does anything. The wiring proof is the per-band
            // cursor lock above; do not read this one as evidence for it.
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

            // The career covers the WHOLE season deliberately (M5). Building it over the same three
            // clubs as the store made this a tautology: the pre-existing career-covers-the-season check
            // fired first, so deleting the progression coverage block left the test green and the new
            // predicate had no isolating case at all. With a full career, only the progression
            // predicate can refuse this.
            var career = PlayerCareerStates.ForLeague(
                league, ClubIds(), injuryOccurrenceEnabled: true);

            ArgumentException ex = Assert.Throws<ArgumentException>(
                () => new SeasonLoop(
                    world, season, RoundResolutionMode.QuickSimAll, career, null, partial),
                "the store must cover every club that plays in this season.");
            Assert.AreEqual("progressionOrNull", ex.ParamName,
                "…and it must be the PROGRESSION predicate that refuses, not the career one — that "
                + "distinction is the whole point of this case.");
        }

        // ── ERR-028-007: the fourth persisted cursor, checked at composition ──────────

        [Test]
        public void Constructor_WithProgressionCursorAheadOfClock_IsRefused()
        {
            // The composition-boundary counterpart of the #29/#41 rule (AR pass 6 M3): a loop can be
            // built from a career and a world that never met a save file, and a cursor ahead of the
            // clock would silently freeze growth until the clock catches up. Mutated directly on the
            // engine, bypassing loop.AdvanceDays, so the world clock (still 0) and the career disagree.
            (WorldStore world, SeasonState season, ProgressionEngine progression, PlayerCareerStates career) =
                NewProgressionComponents();

            progression.AdvanceDay(3, TrainingInputBatch.Neutral);   // cursor = 3, world clock still 0

            Assert.Throws<InvalidOperationException>(
                () => new SeasonLoop(
                    world, season, RoundResolutionMode.QuickSimAll, career, null, progression),
                "ERR-028-007: a progression cursor ahead of the world clock must be refused at composition.");
        }

        [Test]
        public void Constructor_WithProgressionCursorLaggingByTwoOrMore_IsRefused()
        {
            // Worse than ahead (ERR-028-007's own doc): ProgressionEngine.AdvanceDay REPLAYS a gap, so a
            // mispaired file banks N days of growth in one call from a single day's inputs, invisibly.
            (WorldStore world, SeasonState season, ProgressionEngine progression, PlayerCareerStates career) =
                NewProgressionComponents();

            progression.AdvanceDay(0, TrainingInputBatch.Neutral);   // cursor = 0
            world.AdvanceDay();
            world.AdvanceDay();                                       // world clock = 2, lag = 2

            Assert.Throws<InvalidOperationException>(
                () => new SeasonLoop(
                    world, season, RoundResolutionMode.QuickSimAll, career, null, progression),
                "ERR-028-007: a lag of two or more must be refused.");
        }

        [Test]
        public void Constructor_WithProgressionCursorLaggingByOne_IsAccepted()
        {
            // The ordinary saved state: the career day-steps run BEFORE the world clock's increment
            // (KD-2), so a cursor exactly one behind the clock is the normal pairing, not a defect.
            (WorldStore world, SeasonState season, ProgressionEngine progression, PlayerCareerStates career) =
                NewProgressionComponents();

            progression.AdvanceDay(0, TrainingInputBatch.Neutral);   // cursor = 0
            world.AdvanceDay();                                       // world clock = 1, lag = 1

            Assert.DoesNotThrow(
                () => new SeasonLoop(
                    world, season, RoundResolutionMode.QuickSimAll, career, null, progression),
                "a lag of exactly one is the ordinary state between the day steps and the clock increment.");
        }

        [Test]
        public void Constructor_WithSentinelProgressionCursor_IsAcceptedAtAnyClock()
        {
            // A never-advanced career (every player still at PROGRESSION_NOT_ADVANCED_SENTINEL) is
            // coherent regardless of how far the world clock has moved — the sentinel exemption.
            (WorldStore world, SeasonState season, ProgressionEngine progression, PlayerCareerStates career) =
                NewProgressionComponents();

            world.AdvanceDay();
            world.AdvanceDay();
            world.AdvanceDay();   // world clock = 3, far from "never advanced" by either failure mode

            Assert.DoesNotThrow(
                () => new SeasonLoop(
                    world, season, RoundResolutionMode.QuickSimAll, career, null, progression),
                "the sentinel is exempt — a never-advanced career is coherent at any clock.");
        }

        // ── ERR-028-010: the wired configuration can actually play ───────────────────

        [Test]
        public void AProgressionWiredLoop_CanPlayARound_ThroughItsOwnProvider()
        {
            // The landing's headline configuration could advance days and save, and nothing else. The
            // constructor projects the provider from the store and keeps it private, while the
            // ISquadProvider overload demands reference-equality with that instance — which nothing
            // exposed. Every caller-constructible provider was refused; the working path was reachable
            // only by reflection. This is the lock on the no-argument overload that closes it.
            SeasonLoop loop = NewProgressionLoop(out _, out ProgressionEngine progression);

            loop.AdvanceToNextFixtureDay();
            MatchResult[] results = loop.AdvanceAndPlayNextRound();

            Assert.Greater(results.Length, 0, "a round must actually resolve fixtures.");
            Assert.AreEqual(ClubCount / 2, results.Length,
                "every club plays exactly once in a round.");
            Assert.IsTrue(progression.ClubCount > 0, "the store is still the loop's roster authority.");
        }

        [Test]
        public void PlayingARound_ResolvesAgainstTheStore_NotTheBootstrap()
        {
            // The other half of the same finding: the ISquadProvider overload resolved the round from
            // the CALLER's provider rather than the loop's, so relaxing the reference gate alone would
            // have played the round against the day-0 bootstrap. Driving enough days for growth to bank
            // and then playing proves the round sees the store's current roster.
            SeasonLoop loop = NewProgressionLoop(out _, out ProgressionEngine progression);

            // Mutate the store through its own step so the projection diverges from the bootstrap.
            loop.AdvanceDays(6);
            Squad projected = progression.SquadFor(0);

            loop.AdvanceToNextFixtureDay();
            Assert.DoesNotThrow(() => loop.AdvanceAndPlayNextRound(),
                "the loop resolves the round through the provider it owns.");

            // The store is the authority the round was resolved against: the same instance the loop
            // holds, still projecting the evolved records rather than the bootstrap's.
            Assert.AreEqual(
                SumAttributes(projected), SumAttributes(progression.SquadFor(0)),
                "the round must not have reset or bypassed the store's roster.");
        }

        [Test]
        public void ANoArgumentRoundPlay_OnALoopThatOwnsNoProvider_FailsLoud()
        {
            // The no-argument overload is not a universal replacement: a careerless loop owns no
            // provider and must still be told which rosters to use. Refusing by name beats a
            // NullReferenceException from inside the resolution.
            SeasonLoop bare = NewBareLoop(out _);
            bare.AdvanceToNextFixtureDay();

            Assert.Throws<InvalidOperationException>(() => bare.AdvanceAndPlayNextRound());
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

        // The un-constructed components NewProgressionLoop wires together, for the ERR-028-007 tests
        // that need to mutate the world clock or the progression cursor BEFORE handing them to the
        // SeasonLoop constructor under test — NewProgressionLoop itself already constructs a (valid)
        // loop, which is one call too late for these.
        private static (WorldStore world, SeasonState season, ProgressionEngine progression, PlayerCareerStates career)
            NewProgressionComponents()
        {
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(ManagerId, WorldSeed);
            SeasonState season = league.CreateSeason(managedClubId: 0);
            ProgressionEngine progression = SeedFor(league);
            var career = PlayerCareerStates.ForLeague(
                new ProgressionSquads(progression), ClubIds(), injuryOccurrenceEnabled: true);
            return (world, season, progression, career);
        }

        private static SeasonLoop NewBareLoop(out WorldStore world)
        {
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            world = new WorldStore(ManagerId, WorldSeed);
            return new SeasonLoop(
                world, league.CreateSeason(managedClubId: 0), RoundResolutionMode.QuickSimAll);
        }

        private static int AgeOf(Squad squad, int playerId)
        {
            for (int p = 0; p < squad.Count; p++)
            {
                if (squad.GetPlayer(p).PlayerId == playerId)
                {
                    return squad.GetPlayer(p).Age;
                }
            }
            throw new InvalidOperationException($"player {playerId} not in the bootstrap squad.");
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
// | 1.1     | 2026-08-08 | —      | ERR-028-007 lock at the SeasonLoop composition boundary: the     |
// |         |            |        | fourth persisted cursor refused when ahead of the clock or       |
// |         |            |        | lagging by two or more, accepted at the ordinary lag of one,     |
// |         |            |        | and accepted at any clock while still the sentinel. The other    |
// |         |            |        | two boundaries (SeasonSaveManager.Save / .Load) are locked in    |
// |         |            |        | SeasonSaveManagerTests.cs beside their #29/#41 siblings.         |
#endregion
