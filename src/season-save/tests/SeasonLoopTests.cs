// File:     src/season-save/tests/SeasonLoopTests.cs
// Created:  2026-07-26
// Modified: 2026-08-13 (#44 C1/C2 AR round 4, H4/ERR-030-039 — call site updated for the required
//           disciplineWired parameter now on the public Save long form — v1.4)
// Author:   —
// Spec:     Season & Competition Loop #30 §5.4 (T-SN-CAL-001..006), §5.6 (T-SN-DET-001/002),
//           FR-SN-010..013b / 016 / 024 / 026 / 030 / 032 / 033; path-to-playable A4; Code Standards #20
// Purpose:  Locks the #30 T2 composition root: the KD-2 day-advance order and its behaviour-neutral
//           floor, whole-round resolution with all results applied, order-independence, the F4/F5/F6
//           fail-loud gates, mid-sequence restore equality, and two-run season determinism.
//
// Season-scale tests run in RoundResolutionMode.QuickSimAll so a whole 38-round season resolves in
// milliseconds. The managed-fixture-through-the-real-engine path (FR-SN-013b) is proven in the
// Simulation-layer capstone (SeasonLoopScenarios.season-multi-fixture), where a ~2-minute real match is
// idiomatic; asserting it here would put a full 90-minute simulation in the unit suite.

using System;
using System.Collections.ObjectModel;
using System.IO;

using NUnit.Framework;

using TacticalDirector.Discipline;
using TacticalDirector.InjuriesMedical;
using TacticalDirector.LivingWorld;
using TacticalDirector.PlayerProgression;
using TacticalDirector.TrainingSystem;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    internal class SeasonLoopTests
    {
        private const ulong WorldSeed = 0x5EED1EA6D0DEC0DEUL;
        private const int ManagerId = 1;

        private static League FourClubLeague() => LeagueBootstrap.Generate(WorldSeed, 4);

        /// <summary>A fresh world at day 0 plus a season over <paramref name="league"/>.</summary>
        private static SeasonLoop NewLoop(
            League league,
            out WorldStore world,
            RoundResolutionMode mode = RoundResolutionMode.QuickSimAll,
            int managedClubId = 0)
        {
            world = new WorldStore(ManagerId, WorldSeed);
            return new SeasonLoop(world, league.CreateSeason(managedClubId), mode);
        }

        // ── T-SN-CAL-001: day advance ──────────────────────────────────────────────────────

        [Test]
        public void AdvanceToNextFixtureDay_AdvancesExactlyToTheFixtureDay()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out WorldStore world);

            uint targetDay = loop.State.Calendar.NextFixtureDay();
            Assert.AreEqual(0u, world.CurrentWorldTick, "Precondition: a fresh world starts at day 0.");

            int advanced = loop.AdvanceToNextFixtureDay();

            Assert.AreEqual((int)targetDay, advanced);
            Assert.AreEqual(targetDay, world.CurrentWorldTick);
        }

        [Test]
        public void AdvanceToNextFixtureDay_WhenAlreadyOnTheDay_AdvancesNothing()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out WorldStore world);
            loop.AdvanceToNextFixtureDay();
            uint day = world.CurrentWorldTick;

            Assert.AreEqual(0, loop.AdvanceToNextFixtureDay());
            Assert.AreEqual(day, world.CurrentWorldTick);
        }

        [Test]
        public void AdvanceToNextFixtureDay_SeasonComplete_FailsLoud()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);
            PlayWholeSeason(loop, league);

            Assert.IsTrue(loop.IsSeasonComplete);
            Assert.Throws<System.InvalidOperationException>(() => loop.AdvanceToNextFixtureDay());
        }

        // ── T-SN-CAL-002: the KD-8 / FR-SN-026 behaviour-neutral floor ─────────────────────

        [Test]
        public void NoFixtureDay_AdvancesTheWorldByteIdenticallyToABareAdvanceDay()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out WorldStore throughLoop);
            var bare = new WorldStore(ManagerId, WorldSeed);

            Assert.AreEqual(bare.Snapshot(), throughLoop.Snapshot(),
                "Precondition: the two worlds start equal.");

            loop.AdvanceDays(1);
            bare.AdvanceDay();

            Assert.AreEqual(bare.Snapshot(), throughLoop.Snapshot(),
                "With only the world-day tick live, one loop day must equal one bare AdvanceDay (FR-SN-026).");
        }

        [Test]
        public void ManyNoFixtureDays_StayByteIdenticalToBareAdvances()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out WorldStore throughLoop);
            var bare = new WorldStore(ManagerId, WorldSeed);

            loop.AdvanceToNextFixtureDay();
            for (int i = 0; i < (int)loop.State.Calendar.DayOfRound(0); i++)
            {
                bare.AdvanceDay();
            }

            Assert.AreEqual(bare.Snapshot(), throughLoop.Snapshot());
        }

        [Test]
        public void AdvanceDays_PastThePendingFixtureDay_FailsLoud()
        {
            // The KD-4 invariant is a MUST (FR-SN-011): the loop must refuse to walk the clock past a
            // round it has not played, rather than leave a stuck-or-skipped round to surface later.
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);

            uint target = loop.State.Calendar.NextFixtureDay();
            Assert.Throws<System.InvalidOperationException>(() => loop.AdvanceDays((int)target + 1));
        }

        [Test]
        public void AdvanceDays_NegativeDays_FailsLoud()
        {
            SeasonLoop loop = NewLoop(FourClubLeague(), out _);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => loop.AdvanceDays(-1));
        }

        // ── T-SN-CAL-003 / 003a: whole-round resolution ────────────────────────────────────

        [Test]
        public void AdvanceAndPlayNextRound_ResolvesEveryFixtureAndAdvancesTheCursor()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);
            loop.AdvanceToNextFixtureDay();

            int expectedFixtures = league.ClubCount / 2;
            MatchResult[] results = loop.AdvanceAndPlayNextRound(league);

            Assert.AreEqual(expectedFixtures, results.Length, "All N/2 fixtures resolve (FR-SN-012).");
            Assert.AreEqual(1, loop.NextRoundIndex, "The cursor advances exactly one round.");
            Assert.AreEqual(expectedFixtures, loop.MatchOutcomes.Count,
                "One match-outcome event per fixture (FR-SN-016).");

            foreach (MatchResult r in results)
            {
                Assert.AreEqual(0, r.RoundIndex);
            }
        }

        [Test]
        public void AdvanceAndPlayNextRound_EveryClubInTheRoundGainsExactlyOnePlayed()
        {
            // T-SN-CAL-003a — the N>2 broken-table regression lock: resolving a strict subset would leave
            // some clubs' rows undefined for the round.
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);
            loop.AdvanceToNextFixtureDay();
            loop.AdvanceAndPlayNextRound(league);

            ReadOnlyCollection<LeagueTableRow> rows = loop.State.TableRowsInClubIdOrder();
            foreach (LeagueTableRow row in rows)
            {
                Assert.AreEqual(1, row.Played, $"Club {row.ClubId} must have played exactly once.");
            }
        }

        [Test]
        public void AdvanceAndPlayNextRound_MarksFixturesPlayedAndAppliesGoalsToTheTable()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);
            loop.AdvanceToNextFixtureDay();
            MatchResult[] results = loop.AdvanceAndPlayNextRound(league);

            int goalsScored = 0;
            foreach (MatchResult r in results)
            {
                goalsScored += r.HomeGoals + r.AwayGoals;
            }

            int goalsFor = 0;
            foreach (LeagueTableRow row in loop.State.TableRowsInClubIdOrder())
            {
                goalsFor += row.GoalsFor;
            }

            Assert.AreEqual(goalsScored, goalsFor, "Every result reaches the table (FR-SN-013).");

            foreach (Fixture f in loop.State.Fixtures)
            {
                if (f.RoundIndex == 0)
                {
                    Assert.IsTrue(f.Played, "Round-0 fixtures are marked played.");
                }
            }

            Assert.AreEqual(0, loop.State.UnplayedFixtureIndicesInRound(0).Length);
        }

        [Test]
        public void QuickSimAll_RoutesTheManagedFixtureThroughTheModel()
        {
            // The routing half that costs no engine time: under QuickSimAll the managed club's own fixture
            // must come from RoundResolutionModel, bit-for-bit. The complementary proof — that
            // ManagedThroughEngine routes it through a real MatchEngine instead — is the capstone's.
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _, RoundResolutionMode.QuickSimAll, managedClubId: 0);
            loop.AdvanceToNextFixtureDay();

            uint day = loop.CurrentWorldDay;
            ulong seed = loop.State.Seed;
            int seasonNumber = loop.State.SeasonNumber;

            Fixture managedFixture = default;
            bool found = false;
            foreach (Fixture f in loop.State.Fixtures)
            {
                if (f.RoundIndex == 0 && f.Involves(0))
                {
                    managedFixture = f;
                    found = true;
                    break;
                }
            }

            Assert.IsTrue(found, "The managed club must have a round-0 fixture.");

            MatchResult expected = RoundResolutionModel.Resolve(
                in managedFixture,
                seed,
                seasonNumber,
                TacticalDirector.MatchEngine.SquadRating.StartingElevenMean(
                    league.ResolveByClubId(managedFixture.HomeClubId)),
                TacticalDirector.MatchEngine.SquadRating.StartingElevenMean(
                    league.ResolveByClubId(managedFixture.AwayClubId)),
                day);

            MatchResult[] results = loop.AdvanceAndPlayNextRound(league);

            bool matched = false;
            foreach (MatchResult r in results)
            {
                if (r.HomeClubId == managedFixture.HomeClubId && r.AwayClubId == managedFixture.AwayClubId)
                {
                    Assert.AreEqual(expected.HomeGoals, r.HomeGoals);
                    Assert.AreEqual(expected.AwayGoals, r.AwayGoals);
                    matched = true;
                }
            }

            Assert.IsTrue(matched);
            Assert.IsNull(loop.ActiveMatch, "No engine is booted on the quick-sim path.");
        }

        [Test]
        public void AdvanceAndPlayNextRound_BeforeTheFixtureDay_FailsLoud()
        {
            // §3.3's contract is that the cursor is AT the fixture day when the round is played. Without
            // this gate a client could skip the day-advance entirely: every result would carry a WorldDay
            // that is not the fixture's day, and a whole career would run on a clock that never moved —
            // plausible-looking and wrong. The KD-4 invariant only bounds the other side.
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out WorldStore world);

            Assert.AreEqual(0u, world.CurrentWorldTick);
            Assert.Greater(loop.State.Calendar.NextFixtureDay(), 0u,
                "A round-0 fixture on day 0 would make this assertion vacuous.");

            Assert.Throws<System.InvalidOperationException>(
                () => loop.AdvanceAndPlayNextRound(league));

            // And it is not a blanket refusal: one day short still throws, exactly on the day succeeds.
            loop.AdvanceDays((int)loop.State.Calendar.NextFixtureDay() - 1);
            Assert.Throws<System.InvalidOperationException>(
                () => loop.AdvanceAndPlayNextRound(league));

            loop.AdvanceDays(1);
            Assert.DoesNotThrow(() => loop.AdvanceAndPlayNextRound(league));
        }

        // ── FR-SN-013b: the routing decision, all six combinations ────────────────────────

        [Test]
        public void ShouldPlayThroughEngine_CoversEveryModeAndBothFixtureKinds()
        {
            // Extracted from ResolveFixture precisely so FullEngine is covered: inline, the only way to
            // exercise that branch would be to run two real 90-minute matches, so a typo in it would have
            // shipped as "FullEngine quietly behaves like ManagedThroughEngine".
            Assert.IsTrue(SeasonLoop.ShouldPlayThroughEngine(RoundResolutionMode.FullEngine, managed: true));
            Assert.IsTrue(SeasonLoop.ShouldPlayThroughEngine(RoundResolutionMode.FullEngine, managed: false));

            Assert.IsTrue(
                SeasonLoop.ShouldPlayThroughEngine(RoundResolutionMode.ManagedThroughEngine, managed: true));
            Assert.IsFalse(
                SeasonLoop.ShouldPlayThroughEngine(RoundResolutionMode.ManagedThroughEngine, managed: false));

            Assert.IsFalse(SeasonLoop.ShouldPlayThroughEngine(RoundResolutionMode.QuickSimAll, managed: true));
            Assert.IsFalse(SeasonLoop.ShouldPlayThroughEngine(RoundResolutionMode.QuickSimAll, managed: false));
        }

        [Test]
        public void ShouldPlayThroughEngine_UndefinedMode_FailsLoud()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => SeasonLoop.ShouldPlayThroughEngine((RoundResolutionMode)99, managed: true));
        }

        // ── T-SN-CAL-003c: order independence ─────────────────────────────────────────────

        [Test]
        public void PermutedFixtureOrderWithinARound_YieldsAnIdenticalTable()
        {
            League league = FourClubLeague();

            SeasonLoop straight = NewLoop(league, out _);
            straight.AdvanceToNextFixtureDay();
            straight.AdvanceAndPlayNextRound(league);

            // Same season, but round 0's fixtures reversed in array order, so the loop resolves them in the
            // opposite sequence. Keyed draws ⇒ the table must be identical.
            SeasonState permuted = PermuteFirstRound(league.CreateSeason(0));
            var world = new WorldStore(ManagerId, WorldSeed);
            var permutedLoop = new SeasonLoop(world, permuted, RoundResolutionMode.QuickSimAll);
            permutedLoop.AdvanceToNextFixtureDay();
            permutedLoop.AdvanceAndPlayNextRound(league);

            AssertTablesEqual(straight, permutedLoop);
        }

        // ── T-SN-CAL-005 / 006 + F4: fail-loud gates ──────────────────────────────────────

        [Test]
        public void AdvanceAndPlayNextRound_PastTheLastRound_FailsLoud()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);
            PlayWholeSeason(loop, league);

            Assert.Throws<System.InvalidOperationException>(() => loop.AdvanceAndPlayNextRound(league));
        }

        [Test]
        public void AdvanceAndPlayNextRound_UnresolvableClub_FailsLoud()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);
            loop.AdvanceToNextFixtureDay();

            Assert.Throws<System.ArgumentException>(
                () => loop.AdvanceAndPlayNextRound(new EmptySquadProvider()));
        }

        [Test]
        public void AdvanceAndPlayNextRound_NullProvider_FailsLoud()
        {
            SeasonLoop loop = NewLoop(FourClubLeague(), out _);
            Assert.Throws<System.ArgumentNullException>(() => loop.AdvanceAndPlayNextRound(null));
        }

        [Test]
        public void Constructor_NullArguments_FailLoud()
        {
            League league = FourClubLeague();
            var world = new WorldStore(ManagerId, WorldSeed);

            Assert.Throws<System.ArgumentNullException>(
                () => new SeasonLoop(null, league.CreateSeason(0)));
            Assert.Throws<System.ArgumentNullException>(() => new SeasonLoop(world, null));
        }

        [Test]
        public void Constructor_UndefinedMode_FailsLoud()
        {
            League league = FourClubLeague();
            var world = new WorldStore(ManagerId, WorldSeed);

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new SeasonLoop(world, league.CreateSeason(0), (RoundResolutionMode)99));
        }

        [Test]
        public void Constructor_WorldPastThePendingFixtureDay_FailsLoud()
        {
            // F4 / FR-SN-011 at composition time — a loop built from a world that has already run past the
            // season's pending round would present a stuck or skipped round with nothing to detect it.
            League league = FourClubLeague();
            var world = new WorldStore(ManagerId, WorldSeed);
            SeasonState season = league.CreateSeason(0);
            for (uint d = 0; d <= season.Calendar.NextFixtureDay(); d++)
            {
                world.AdvanceDay();
            }

            Assert.Throws<System.ArgumentException>(() => new SeasonLoop(world, season));
        }

        [Test]
        public void Constructor_CompletedSeason_IsAcceptedAtAnyWorldDay()
        {
            // The vacuous half of the invariant: a completed season has no next fixture, so the gate must
            // not become a spurious refusal (the #30 T1 lock, re-asserted at this new call site).
            League league = FourClubLeague();
            SeasonLoop played = NewLoop(league, out WorldStore world);
            PlayWholeSeason(played, league);

            for (int i = 0; i < 50; i++)
            {
                world.AdvanceDay();
            }

            Assert.DoesNotThrow(() => new SeasonLoop(world, played.State, RoundResolutionMode.QuickSimAll));
        }

        // ── T-SN-DET-001: mid-sequence save/restore ───────────────────────────────────────

        [Test]
        public void MidSequenceRestore_EqualsAnUninterruptedRun()
        {
            League league = FourClubLeague();

            // Uninterrupted: advance 3 days, then to the fixture day, then play the round.
            SeasonLoop straight = NewLoop(league, out WorldStore straightWorld);
            straight.AdvanceDays(3);
            straight.AdvanceToNextFixtureDay();
            straight.AdvanceAndPlayNextRound(league);

            // Interrupted at the same mid-advance point, through a real file.
            SeasonLoop first = NewLoop(league, out WorldStore firstWorld);
            first.AdvanceDays(3);

            string path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".tdsave");
            try
            {
                SeasonSaveManager.Save(firstWorld, first.State, null,
                    path, Array.Empty<ClubTrainingStates>(), Array.Empty<ClubInjuryStates>(),
                    Array.Empty<ClubAppearanceStates>(), ProgressionEngine.Empty, new DisciplineState(), disciplineWired: true);
                SeasonSaveContents contents = SeasonSaveManager.Load(path, league);
                var resumed = new SeasonLoop(
                    contents.World, contents.Season, RoundResolutionMode.QuickSimAll);

                resumed.AdvanceToNextFixtureDay();
                resumed.AdvanceAndPlayNextRound(league);

                Assert.AreEqual(straightWorld.Snapshot(), contents.World.Snapshot(),
                    "The world must resume byte-identically (FR-SN-024).");
                Assert.IsTrue(straight.State.FieldsEqual(resumed.State),
                    "The season must resume field-identically (FR-SN-024).");
            }
            finally
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
        }

        [Test]
        public void SnapshotRestore_RoundTripsTheSeasonSubBlob()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out WorldStore world);
            loop.AdvanceToNextFixtureDay();
            loop.AdvanceAndPlayNextRound(league);

            byte[] blob = loop.Snapshot();
            SeasonLoop restored = SeasonLoop.Restore(world, blob, RoundResolutionMode.QuickSimAll);

            Assert.IsTrue(loop.State.FieldsEqual(restored.State));
            Assert.AreEqual(loop.NextRoundIndex, restored.NextRoundIndex);
        }

        // ── T-SN-DET-002: end-to-end determinism ──────────────────────────────────────────

        [Test]
        public void TwoRunSeason_ReachesAByteIdenticalFinalTable()
        {
            League league = FourClubLeague();

            SeasonLoop a = NewLoop(league, out _);
            PlayWholeSeason(a, league);

            SeasonLoop b = NewLoop(league, out _);
            PlayWholeSeason(b, league);

            AssertTablesEqual(a, b);
            Assert.IsTrue(a.State.FieldsEqual(b.State));
        }

        [Test]
        public void FullSeason_PopulatesEveryClubsRowForEveryRound()
        {
            // The KD-9 completeness lock at season scale: a double round-robin gives every club
            // 2*(N-1) played fixtures, and nothing may be left unresolved.
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);
            PlayWholeSeason(loop, league);

            int expectedPlayed = 2 * (league.ClubCount - 1);
            foreach (LeagueTableRow row in loop.State.TableRowsInClubIdOrder())
            {
                Assert.AreEqual(expectedPlayed, row.Played, $"Club {row.ClubId}");
                Assert.AreEqual(row.Played, row.Won + row.Drawn + row.Lost);
                Assert.AreEqual(row.GoalsFor - row.GoalsAgainst, row.GoalDifference);
            }

            foreach (Fixture f in loop.State.Fixtures)
            {
                Assert.IsTrue(f.Played, "Every fixture in the schedule is played.");
            }
        }

        [Test]
        public void FullTwentyClubSeason_CompletesAndBalances()
        {
            // The roadmap PM-2 shape: 20 clubs, 38 rounds, 380 fixtures — resolved head-lessly. This is
            // the test that would have taken >= 16 hours through the real engine (roadmap C1) and is the
            // reason the round-resolution model exists.
            League league = LeagueBootstrap.Generate(WorldSeed, 20);
            SeasonLoop loop = NewLoop(league, out _);
            PlayWholeSeason(loop, league);

            Assert.AreEqual(380, loop.MatchOutcomes.Count, "20 clubs ⇒ N*(N-1) = 380 fixtures.");

            int totalPlayed = 0;
            int goalsFor = 0;
            int goalsAgainst = 0;
            foreach (LeagueTableRow row in loop.State.TableRowsInClubIdOrder())
            {
                Assert.AreEqual(38, row.Played, $"Club {row.ClubId}");
                totalPlayed += row.Played;
                goalsFor += row.GoalsFor;
                goalsAgainst += row.GoalsAgainst;
            }

            Assert.AreEqual(760, totalPlayed, "Each of 380 fixtures is played by two clubs.");
            Assert.AreEqual(goalsFor, goalsAgainst, "Every goal scored is a goal conceded.");
        }

        [Test]
        public void FullSeason_LeavesTheCursorAtTheEndAndTheViewComplete()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);
            PlayWholeSeason(loop, league);

            SeasonViewModel view = loop.View();
            Assert.IsTrue(view.IsSeasonComplete);
            Assert.AreEqual(view.RoundCount, view.NextRoundIndex);
            Assert.AreEqual(league.ClubCount, view.Table.Count);
        }

        [Test]
        public void View_IsObserverNeutral()
        {
            League league = FourClubLeague();
            SeasonLoop loop = NewLoop(league, out _);
            loop.AdvanceToNextFixtureDay();
            loop.AdvanceAndPlayNextRound(league);

            byte[] before = loop.Snapshot();
            for (int i = 0; i < 5; i++)
            {
                loop.View();
            }

            Assert.AreEqual(before, loop.Snapshot(), "Reading the view must not mutate season state.");
        }

        // ── Helpers ───────────────────────────────────────────────────────────────────────

        /// <summary>Advances and plays until the season is complete.</summary>
        private static void PlayWholeSeason(SeasonLoop loop, League league)
        {
            while (!loop.IsSeasonComplete)
            {
                loop.AdvanceToNextFixtureDay();
                loop.AdvanceAndPlayNextRound(league);
            }
        }

        /// <summary>
        /// The same season with round 0's fixtures reversed in array order — everything else identical, so
        /// the only difference is the sequence <c>AdvanceAndPlayNextRound</c> resolves them in.
        /// </summary>
        private static SeasonState PermuteFirstRound(SeasonState source)
        {
            var fixtures = new Fixture[source.FixtureCount];
            var firstRoundSlots = new System.Collections.Generic.List<int>();
            for (int i = 0; i < fixtures.Length; i++)
            {
                fixtures[i] = source.FixtureAt(i);
                if (fixtures[i].RoundIndex == 0)
                {
                    firstRoundSlots.Add(i);
                }
            }

            Assert.Greater(firstRoundSlots.Count, 1,
                "The permutation is vacuous with fewer than two fixtures in the round.");

            // Reverse the round-0 fixtures among THEIR OWN slots, found by round index rather than assumed
            // contiguous at the head — so the helper cannot silently start permuting a later round's
            // fixtures if the generator's emission order ever changes.
            for (int a = 0, b = firstRoundSlots.Count - 1; a < b; a++, b--)
            {
                int i = firstRoundSlots[a];
                int j = firstRoundSlots[b];
                Fixture tmp = fixtures[i];
                fixtures[i] = fixtures[j];
                fixtures[j] = tmp;
            }

            ReadOnlyCollection<int> sourceIds = source.ClubIds;   // allocates a copy per access — hoist
            var clubIds = new int[sourceIds.Count];
            for (int i = 0; i < clubIds.Length; i++)
            {
                clubIds[i] = sourceIds[i];
            }

            return new SeasonState(
                source.Seed,
                source.SeasonNumber,
                source.ManagedClubId,
                clubIds,
                fixtures,
                LeagueTable.Empty(clubIds),
                source.Calendar,
                source.Board);
        }

        private static void AssertTablesEqual(SeasonLoop a, SeasonLoop b)
        {
            ReadOnlyCollection<LeagueTableRow> left = a.State.TableRowsInClubIdOrder();
            ReadOnlyCollection<LeagueTableRow> right = b.State.TableRowsInClubIdOrder();

            Assert.AreEqual(left.Count, right.Count);
            for (int i = 0; i < left.Count; i++)
            {
                Assert.AreEqual(left[i].ClubId, right[i].ClubId);
                Assert.AreEqual(left[i].Played, right[i].Played, $"Played, club {left[i].ClubId}");
                Assert.AreEqual(left[i].Won, right[i].Won, $"Won, club {left[i].ClubId}");
                Assert.AreEqual(left[i].Drawn, right[i].Drawn, $"Drawn, club {left[i].ClubId}");
                Assert.AreEqual(left[i].Lost, right[i].Lost, $"Lost, club {left[i].ClubId}");
                Assert.AreEqual(left[i].GoalsFor, right[i].GoalsFor, $"GF, club {left[i].ClubId}");
                Assert.AreEqual(left[i].GoalsAgainst, right[i].GoalsAgainst, $"GA, club {left[i].ClubId}");
                Assert.AreEqual(left[i].GoalDifference, right[i].GoalDifference, $"GD, club {left[i].ClubId}");
                Assert.AreEqual(left[i].Points, right[i].Points, $"Pts, club {left[i].ClubId}");
            }
        }

        /// <summary>An <c>ISquadProvider</c> that resolves nothing — the F6 gate's input.</summary>
        private sealed class EmptySquadProvider : TacticalDirector.MatchEngine.ISquadProvider
        {
            public TacticalDirector.PlayerDatabase.Squad ResolveByClubId(int clubId) => null;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial suite (#30 T2): day-advance exactness + the FR-SN-026        |
// |         |            |        | behaviour-neutral floor, whole-round resolution + per-club           |
// |         |            |        | completeness, quick-sim managed routing, order-independence,         |
// |         |            |        | F4/F5/F6 gates, mid-sequence restore equality through a real file,   |
// |         |            |        | two-run determinism, and a 20-club 380-fixture season.               |
// | 1.1     | 2026-08-07 | —      | Balance pass D2: the Save call site carries the required (empty)   |
// |         |            |        | appearance set — frame v4.                                         |
// | 1.2     | 2026-08-08 | —      | #28 T1: call sites updated for SeasonSaveManager.Save's / Season-  |
// |         |            |        | SaveCodec.Encode's new required progression-block parameter. No    |
// |         |            |        | assertion or intent change.                                        |
// | 1.3     | 2026-08-13 | —      | #44 T1 (roadmap C1): the Save call site carries the required       |
// |         |            |        | (empty) discipline block. No assertion or intent change.           |
// | 1.4     | 2026-08-13 | —      | #44 C1/C2 AR round 4 (H4, ERR-030-039): the Save call site passes  |
// |         |            |        | the now-required disciplineWired: true, which is what the deleted  |
// |         |            |        | forwarding overload used to assert for it. No assertion or intent  |
// |         |            |        | change.                                                            |
#endregion
