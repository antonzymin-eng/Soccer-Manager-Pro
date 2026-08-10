// File:     src/season-save/tests/SeasonLoopProgressionTests.cs
// Created:  2026-08-08
// Modified: 2026-08-10 (AR pass 5 — the progression-only advance/play/save/reload/resume lock — v1.5)
// Author:   —
// Spec:     Season & Competition Loop #30 §3.3 (KD-2 slot 1); Player Progression & Lifecycle #28
//           KD-4 / FR-PG-021 / FR-PG-022; ERR-029-006 (the batch entry point, closed here);
//           ERR-030-027 (the twice-per-fixture-day call); ERR-028-007 (the fourth persisted cursor,
//           checked at three boundaries — this file covers the SeasonLoop constructor boundary);
//           ERR-028-014 (the never-advanced sentinel retired from the legal store states, and from
//           the constructor's cursor-vs-clock exemption); Code Standards #20
// Purpose:  Locks that #30's slot 1 actually EXECUTES — the wiring proof — plus the single-roster-
//           authority refusals the constructor now enforces. A dead seam and a live one are otherwise
//           indistinguishable from outside, which is the ERR-030-014 failure mode one layer up. Also
//           the composition-boundary third of ERR-028-007's three cursor-vs-clock checks (the other
//           two — SeasonSaveManager.Save and .Load — live in SeasonSaveManagerTests.cs beside their
//           #29/#41 siblings). v1.3 (ERR-028-014) rewrites the bootstrap-league accrual count for the
//           anchored seed-day cursor and inverts the sentinel-exemption constructor case, which had
//           been locking the defect as intended behaviour. v1.5 (AR pass 5, High) adds the
//           save/reload/resume lock for a progression-only (no #29/#41 career) loop — the composition
//           SeasonSaveManager could not save at all before this pass's SeasonSaveManager.cs fix.

using System;
using System.IO;

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

        // The attribute floor SeedForWithClubStrengthened raises one club to. High enough that the
        // club's SquadRating must move, well inside the [1,20] contract the codec range-gates.
        private const int StrengthenedFloor = 18;

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

            // Six advances: the KD-4 cursor invariant refuses an advance past the pending round's
            // fixture day (day 7). Six days cannot cross a birthday, so each player's band is fixed
            // throughout.
            //
            // FIVE of those six accrue (ERR-028-014). The store is seeded on world day 0 and the seed
            // day IS the cursor now, so slot 1's day-0 call is a no-op — the generated roster already
            // describes the league as of day 0 — and days 1..5 are the days actually lived. The
            // distinction is the whole content of the fix: before it, "never advanced" meant the store
            // could not tell day 0 from day 3650, and the first advance banked one day either way.
            const int Advances = 6;
            const int AccruingDays = Advances - 1;
            loop.AdvanceDays(Advances);

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
                    if (bootstrapAge < PlayerProgressionConstants.GROWTH_AGE) { expected = +AccruingDays; growth++; }
                    else if (bootstrapAge > PlayerProgressionConstants.DECLINE_AGE) { expected = -AccruingDays; decline++; }
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

        // ── ERR-028-013: an EMPTY store is the absence of #28, not a wired one ───────

        [Test]
        public void Constructor_WithAnEmptyProgressionStore_BesideABootstrapProvider_IsAccepted()
        {
            // The pre-#28 composition, which SeasonSaveManager.Save deliberately writes and its own
            // refusal message tells the caller to resume from. SeasonSaveContents.Progression is NEVER
            // null — a careerless save carries a well-formed ZERO-club block — so a caller threading it
            // back in used to hit the two-authorities refusal here, and passing it alone instead failed
            // the season-coverage check. The only way through was to pass null INSTEAD of the loaded
            // store, which is the opposite of what the save root advises.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(ManagerId, WorldSeed);
            SeasonState season = league.CreateSeason(managedClubId: 0);
            var career = PlayerCareerStates.ForLeague(league, ClubIds(), injuryOccurrenceEnabled: true);

            Assert.DoesNotThrow(
                () => new SeasonLoop(
                    world, season, RoundResolutionMode.QuickSimAll, career, league,
                    ProgressionEngine.Empty),
                "an empty store carries no rosters, so it cannot be the second authority the "
                + "two-authorities refusal exists to prevent.");
        }

        [Test]
        public void SaveThenResume_OfACareerWithNoProgression_RoundTripsThroughTheLoadedContents()
        {
            // The end-to-end shape the finding was really about: nothing anywhere reconstructed a
            // SeasonLoop from SeasonSaveManager.Load's output, which is the one thing a real game does
            // on every load. Threading contents.Progression back in verbatim — exactly as the Save-side
            // guard instructs — must compose, and the resumed loop must still run.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(ManagerId, WorldSeed);
            SeasonState season = league.CreateSeason(managedClubId: 0);
            var career = PlayerCareerStates.ForLeague(league, ClubIds(), injuryOccurrenceEnabled: true);
            var loop = new SeasonLoop(
                world, season, RoundResolutionMode.QuickSimAll, career, league);
            loop.AdvanceDays(3);

            string path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(), $"td-prog-resume-{Guid.NewGuid():N}.tdsave");
            try
            {
                SeasonSaveManager.Save(loop, null, path);
                SeasonSaveContents restored = SeasonSaveManager.Load(path);

                Assert.AreEqual(0, restored.Progression.ClubCount,
                    "precondition: this save's progression block is the empty one, which is the whole "
                    + "case under test.");

                SeasonLoop resumed = null;
                Assert.DoesNotThrow(
                    () => resumed = new SeasonLoop(
                        restored.World, restored.Season, RoundResolutionMode.QuickSimAll,
                        PlayerCareerStates.FromBlocks(
                            restored.TrainingClubs, restored.MedicalClubs, restored.AppearanceClubs,
                            injuryOccurrenceEnabled: true),
                        league,
                        restored.Progression),
                    "contents.Progression must be threadable back verbatim — that is what the save "
                    + "root's own refusal message instructs the caller to do.");

                Assert.DoesNotThrow(() => resumed.AdvanceDays(1),
                    "…and the resumed loop must actually run.");
            }
            finally
            {
                if (System.IO.File.Exists(path))
                {
                    System.IO.File.Delete(path);
                }
            }
        }

        [Test]
        public void AProgressionStoreWithNoCareer_DrivesSlot1_OnTheNeutralBatch()
        {
            // #28 does not depend on #29 or #41, and a progression store is its own squad provider — so
            // demanding career state beside it made #28 unusable without two unrelated subsystems, and
            // made slot 1's `_career == null` branch (the FR-PG-009 "no training anywhere" path, and
            // the only production consumer of TrainingInputBatch.Neutral) provably unreachable. A guard
            // on a branch nothing can execute ships green forever, which is exactly the class this
            // project keeps filing. This is the lock that the branch now executes.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(ManagerId, WorldSeed);
            ProgressionEngine progression = SeedFor(league);

            SeasonLoop loop = null;
            Assert.DoesNotThrow(
                () => loop = new SeasonLoop(
                    world, league.CreateSeason(managedClubId: 0), RoundResolutionMode.QuickSimAll,
                    careerOrNull: null, careerSquadsOrNull: null, progressionOrNull: progression),
                "a populated progression store drives slot 1 on its own.");

            long before = TotalCursor(progression);
            loop.AdvanceDays(6);

            Assert.AreNotEqual(before, TotalCursor(progression),
                "slot 1 must have run on the Neutral batch — if this branch were still unreachable the "
                + "constructor above would have thrown, and if it were reachable but dead the cursor "
                + "would not move.");
        }

        [Test]
        public void AProgressionStoreWithNoCareer_CanPlayARound_AndRefusesAForeignProvider()
        {
            // ERR-028-015, and the reason the hole existed. ERR-028-013 made progression-without-career
            // a legal composition, but the only case covering it called AdvanceDays — so the new
            // configuration could advance days and save, and nothing else was ever exercised. That is
            // verbatim the ERR-028-010 shape, in the fix that cited it.
            //
            // The second half is the defect it hid: the two-provider reference gate was keyed on
            // `_career != null`, which had been equivalent to `_careerSquads != null` until this very
            // relaxation broke the biconditional. A progression-only loop skipped the gate, so the
            // day-0 bootstrap could be handed in and the round resolved against attributes the store
            // had already grown away from — silently.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            ProgressionEngine progression = SeedFor(league);
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(managedClubId: 0),
                RoundResolutionMode.QuickSimAll,
                careerOrNull: null, careerSquadsOrNull: null, progressionOrNull: progression);

            loop.AdvanceToNextFixtureDay();

            Assert.Throws<ArgumentException>(
                () => loop.AdvanceAndPlayNextRound(league),
                "the loop OWNS a provider projected from its store, so a foreign one — here the day-0 "
                + "bootstrap — must be refused even though no #29/#41 career is wired.");

            MatchResult[] results = loop.AdvanceAndPlayNextRound();
            Assert.AreEqual(ClubCount / 2, results.Length,
                "…and the configuration must actually be able to play, through the provider it owns.");
        }

        [Test]
        public void AProgressionStoreWithNoCareer_SavesAndReloadsAndResumes()
        {
            // AR pass 5, and it is the SAME shape a third time — the one the case above names and then
            // itself demonstrates. Its comment claimed the progression-only composition "could advance
            // days and save"; the first half was true and the second was never once executed. Save's
            // career-coherence gate carved out an empty STORE (the honest pre-#28 wiring) and never the
            // mirror: an empty CAREER beside a populated store. Save(SeasonLoop, …) feeds
            // Array.Empty<ClubTrainingStates>() for a careerless loop, so the length compare fired
            // unconditionally and this blessed composition threw:
            //     "The progression set carries 4 clubs but the three career sets carry 0"
            // A career that advances, plays, and then cannot be persisted — with the loss landing only
            // when the player saves, and silently for any caller catching broadly.
            //
            // The operation set for a supported composition is advance / play / SAVE / RESUME, so this
            // asserts all four. It fails at Save against the pre-fix gate.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            ProgressionEngine progression = SeedFor(league);
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(managedClubId: 0),
                RoundResolutionMode.QuickSimAll,
                careerOrNull: null, careerSquadsOrNull: null, progressionOrNull: progression);

            loop.AdvanceToNextFixtureDay();
            loop.AdvanceAndPlayNextRound();

            string dir = Path.Combine(Path.GetTempPath(), "td-prog-only-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                string path = Path.Combine(dir, "progression-only.save");

                Assert.DoesNotThrow(
                    () => SeasonSaveManager.Save(loop, matchOrNull: null, path),
                    "a composition ERR-028-013 blessed, and that this suite locks can advance and play, "
                    + "must be persistable — the career sets being empty is the ABSENCE of #29/#41, not "
                    + "a disagreement with the store.");

                SeasonSaveContents contents = SeasonSaveManager.Load(path);
                Assert.AreEqual(ClubCount, contents.Progression.ClubCount,
                    "the roster is #28's block (KD-4), so a save that drops it loses the career.");

                // Resume into a live loop and keep going: a file that loads but cannot be run forward is
                // not a saved career either.
                //
                // The world MUST come from the file, not from a fresh WorldStore on the same seed. The
                // first draft of this test built a new one and the composition gate refused it — "club 0
                // player 0's progression cursor (7) is ahead of the world clock (0)" — which is the
                // cursor-vs-clock invariant doing precisely its job: a career's lived history and the
                // clock it was lived against travel together or not at all. Worth keeping as a comment
                // because a fresh-store resume is the obvious thing to write and it is wrong.
                var resumed = new SeasonLoop(
                    contents.World, contents.Season,
                    RoundResolutionMode.QuickSimAll,
                    careerOrNull: null, careerSquadsOrNull: null, progressionOrNull: contents.Progression);
                resumed.AdvanceToNextFixtureDay();
                MatchResult[] resumedResults = resumed.AdvanceAndPlayNextRound();
                Assert.AreEqual(ClubCount / 2, resumedResults.Length,
                    "the resumed progression-only loop must play through the provider it owns.");
            }
            finally
            {
                Directory.Delete(dir, recursive: true);
            }
        }

        [Test]
        public void Constructor_WithABareProviderAndNoCareer_IsStillRefused()
        {
            // The relaxation above must not become "anything goes": a bare ISquadProvider with neither
            // a career nor a progression store behind it still drives nothing, and the original pairing
            // rule is right for that case. Without this the fix would have deleted a real guard.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(ManagerId, WorldSeed);

            Assert.Throws<ArgumentException>(
                () => new SeasonLoop(
                    world, league.CreateSeason(managedClubId: 0), RoundResolutionMode.QuickSimAll,
                    careerOrNull: null, careerSquadsOrNull: league),
                "a provider on its own, with nothing behind it, still drives nothing.");
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
        public void Constructor_WithAStoreSeededFarBehindTheClock_IsRefused()
        {
            // INVERTED at ERR-028-014. This case previously asserted that a never-advanced store is
            // "coherent at any clock" and passed — it was locking the defect as intended behaviour,
            // the second test in this landing to do so.
            //
            // The exemption it enforced was copied from #29/#41, where it is sound: their fresh states
            // carry no clock-anchored quantity, so "never advanced" means the same thing on every day.
            // #28's does — age is DERIVED from BirthWorldDay — so a fresh #28 state means something
            // different at every clock value. Composed three days on, this store's players would each
            // have aged three days while banking a single day of growth; at a decade the same
            // construction ages the whole league ten years, silently. The store is now seeded with its
            // cursor anchored, so the ordinary lag rule refuses the pairing with no exemption to carve.
            (WorldStore world, SeasonState season, ProgressionEngine progression, PlayerCareerStates career) =
                NewProgressionComponents();

            world.AdvanceDay();
            world.AdvanceDay();
            world.AdvanceDay();   // world clock = 3; the store is anchored on day 0, so the lag is 3

            InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                () => new SeasonLoop(
                    world, season, RoundResolutionMode.QuickSimAll, career, null, progression),
                "a store whose lived history starts three days before the clock is not coherent — the "
                + "next advance would replay the gap from one day's inputs, or, before the anchor "
                + "existed, collapse it to a single day while every derived age jumped the whole span.");
            StringAssert.Contains("more than one day behind", ex.Message,
                "…and it must be the LAG half of the rule that refuses it, not the ahead half.");
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
            // The other half of the pass-1 finding: the ISquadProvider overload resolved the round from
            // the CALLER's provider rather than the loop's, so relaxing the reference gate alone would
            // have played the round against the day-0 bootstrap.
            //
            // AR pass 2: this test previously advanced six days, played, and asserted the store's
            // attribute sum was unchanged across the round. That holds whichever provider resolved the
            // fixtures — playing a round mutates no attributes — so it could not distinguish the two
            // cases its own name claims to separate. Worse, six days bank six cursor points and
            // POINT_COST is a whole year, so the store and the bootstrap were still attribute-identical
            // at the moment of the assertion: there was nothing to tell apart.
            //
            // The discrimination has to come from a store that genuinely differs from the bootstrap.
            // Seeding one club's squad at a raised attribute floor does that at day 0, and the round is
            // then resolved by SquadRating — so if the loop reads the bootstrap, the diverged club's
            // fixture must come out as the control's does.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            ProgressionEngine strengthened = SeedForWithClubStrengthened(league, clubId: 0);

            Squad fromStore = strengthened.SquadFor(0);
            Squad fromBootstrap = league.ResolveByClubId(0);
            Assert.Greater(SumAttributes(fromStore), SumAttributes(fromBootstrap),
                "precondition: the store's club 0 must actually differ from the bootstrap's, or this "
                + "test proves nothing about which of the two the round read.");

            MatchResult[] viaStore = PlayFirstRoundThroughTheStore(league, strengthened);
            MatchResult[] viaBootstrap = PlayFirstRoundThroughAProvider(league);

            Assert.AreEqual(viaBootstrap.Length, viaStore.Length,
                "precondition: the same fixtures in both runs.");
            Assert.IsFalse(ResultsMatch(viaStore, viaBootstrap),
                "the round must resolve against the STORE's roster: an identical result set means it "
                + "read the day-0 bootstrap instead, which is the silent divergence KD-4 exists to "
                + "remove.");
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

        // A store seeded from the bootstrap with ONE club's attributes raised to a floor, so the store
        // and the bootstrap genuinely disagree about that club at day 0. Growth cannot produce the
        // divergence inside a test: POINT_COST is a whole year and the KD-4 cursor invariant refuses
        // an advance past the pending round's fixture day, so nothing observable moves in six days.
        private static ProgressionEngine SeedForWithClubStrengthened(League league, int clubId)
        {
            var squads = new Squad[league.ClubCount];
            for (int c = 0; c < squads.Length; c++)
            {
                Squad source = league.ResolveByClubId(c);
                if (c != clubId)
                {
                    squads[c] = source;
                    continue;
                }

                var raised = new PlayerRecord[source.Count];
                for (int p = 0; p < source.Count; p++)
                {
                    PlayerRecord rec = source.GetPlayer(p);
                    int[] attrs = rec.Attributes.ToArray();
                    for (int a = 0; a < attrs.Length; a++)
                    {
                        if (attrs[a] < StrengthenedFloor)
                        {
                            attrs[a] = StrengthenedFloor;
                        }
                    }
                    var lifted = new PlayerAttributes();
                    lifted.FromArray(attrs);
                    lifted.WeakFootRating = rec.Attributes.WeakFootRating;
                    rec.Attributes = lifted;
                    raised[p] = rec;
                }
                squads[c] = new Squad(source.ClubId, raised);
            }
            return ProgressionEngine.SeedFrom(squads, 0u);
        }

        // The round as the loop's OWN provider resolves it (the store's projection).
        private static MatchResult[] PlayFirstRoundThroughTheStore(
            League league, ProgressionEngine progression)
        {
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(managedClubId: 0),
                RoundResolutionMode.QuickSimAll,
                careerOrNull: null, careerSquadsOrNull: null, progressionOrNull: progression);
            loop.AdvanceToNextFixtureDay();
            return loop.AdvanceAndPlayNextRound();
        }

        // The control: the same round resolved from the day-0 bootstrap, through a loop that owns no
        // provider and is handed one at the call.
        private static MatchResult[] PlayFirstRoundThroughAProvider(League league)
        {
            var loop = new SeasonLoop(
                new WorldStore(ManagerId, WorldSeed), league.CreateSeason(managedClubId: 0),
                RoundResolutionMode.QuickSimAll);
            loop.AdvanceToNextFixtureDay();
            return loop.AdvanceAndPlayNextRound(league);
        }

        private static bool ResultsMatch(MatchResult[] a, MatchResult[] b)
        {
            for (int i = 0; i < a.Length; i++)
            {
                if (a[i].HomeGoals != b[i].HomeGoals || a[i].AwayGoals != b[i].AwayGoals)
                {
                    return false;
                }
            }
            return true;
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
// | 1.2     | 2026-08-09 | —      | AR pass 2. ERR-028-013 locks: an empty store composes beside a   |
// |         |            |        | provider; contents.Progression threads back verbatim through a   |
// |         |            |        | real save/load/resume; a store with no career drives slot 1 on   |
// |         |            |        | the Neutral batch (the branch that was unreachable); and a bare  |
// |         |            |        | provider with nothing behind it is STILL refused, so the         |
// |         |            |        | relaxation did not delete a real guard. Also rewrites            |
// |         |            |        | PlayingARound_ResolvesAgainstTheStore_NotTheBootstrap, whose     |
// |         |            |        | assertions held whichever provider resolved the round — six      |
// |         |            |        | days bank six cursor points against a POINT_COST of a year, so   |
// |         |            |        | store and bootstrap were still attribute-identical when it       |
// |         |            |        | compared them. It now seeds one club above the bootstrap and     |
// |         |            |        | requires the round to differ from a bootstrap-resolved control.  |
// | 1.3     | 2026-08-09 | —      | ERR-028-014: two tests changed meaning, one of them INVERTED —   |
// |         |            |        | it had been locking the defect as intended behaviour.            |
// |         |            |        | AdvanceDays_DrivesSlot1_AndEachPlayerAccruesHisOwnBandStep now    |
// |         |            |        | expects 5 accruing days out of 6 advances, since the seed day     |
// |         |            |        | (day 0) is now a cursor no-op — the generated league already      |
// |         |            |        | describes itself as of day 0, so only days 1..5 are actually      |
// |         |            |        | lived. Constructor_WithSentinelProgressionCursor_IsAcceptedAt-    |
// |         |            |        | AnyClock renamed Constructor_WithAStoreSeededFarBehindTheClock_   |
// |         |            |        | IsRefused: it asserted the sentinel exemption was correct, which  |
// |         |            |        | it was not — the exemption is #29/#41's (their fresh states carry |
// |         |            |        | no clock-anchored quantity); #28's fresh state DOES (age is       |
// |         |            |        | derived from BirthWorldDay), so the same construction now         |
// |         |            |        | refuses, and the assertion checks it is refused on the LAG half   |
// |         |            |        | of the predicate specifically.                                    |
// | 1.4     | 2026-08-10 | —      | AR pass 3 (ERR-028-015): + AProgressionStoreWithNoCareer_Can-    |
// |         |            |        | PlayARound_AndRefusesAForeignProvider. The composition v1.17     |
// |         |            |        | created was only ever exercised through AdvanceDays — a config   |
// |         |            |        | that could advance days and save and nothing else, verbatim the  |
// |         |            |        | ERR-028-010 shape — and that gap is what hid the reference-gate  |
// |         |            |        | hole this case now locks.                                        |
// | 1.5     | 2026-08-10 | —      | AR pass 5 (High): + AProgressionStoreWithNoCareer_               |
// |         |            |        | SavesAndReloadsAndResumes — the progression-only loop advances,   |
// |         |            |        | plays a round, SAVES, reloads and RESUMES; the fix this pass      |
// |         |            |        | proves (SeasonSaveManager's carve-out made symmetric). The        |
// |         |            |        | test's own comment records that a fresh WorldStore on the same    |
// |         |            |        | seed is refused as a resume source by the cursor-vs-clock         |
// |         |            |        | invariant — the world MUST come from the loaded file, not be      |
// |         |            |        | rebuilt from the seed (roadmap A3's retired property).             |
#endregion
