// File:     src/season-save/tests/SeasonStateTests.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 §2.2, §3.3, §5.4/§5.6, FR-SN-009/011/013b/033, KD-4/KD-7;
//           Testing Strategy #19
// Purpose:  Unit locks for the T0 season value types: the calendar cursor + KD-4 invariant, the board
//           objective / on-track projection, the match-outcome payload, SeasonState coherence gates,
//           and the observer-neutral SeasonViewModel.

using NUnit.Framework;
using TacticalDirector.SeasonSave;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    public sealed class SeasonCalendarTests
    {
        [Test]
        public void Linear_ProducesEvenlySpacedAscendingDays()
        {
            SeasonCalendar calendar = SeasonCalendar.Linear(roundCount: 6, firstRoundDay: 10, daysBetweenRounds: 7);

            Assert.That(calendar.RoundCount, Is.EqualTo(6));
            Assert.That(calendar.NextRoundIndex, Is.EqualTo(0));
            Assert.That(calendar.DayOfRound(0), Is.EqualTo(10u));
            Assert.That(calendar.DayOfRound(1), Is.EqualTo(17u));
            Assert.That(calendar.DayOfRound(5), Is.EqualTo(10u + 5u * 7u));
            Assert.That(calendar.NextFixtureDay(), Is.EqualTo(10u));
            Assert.That(calendar.IsSeasonComplete, Is.False);
        }

        [Test]
        public void Create_NonAscendingDays_Throws()
        {
            Assert.Throws<System.ArgumentException>(
                () => SeasonCalendar.Create(0, new uint[] { 5, 5 }), "equal days are ambiguous");
            Assert.Throws<System.ArgumentException>(
                () => SeasonCalendar.Create(0, new uint[] { 9, 3 }), "days must not go backwards");
            Assert.Throws<System.ArgumentException>(
                () => SeasonCalendar.Create(0, System.Array.Empty<uint>()));
        }

        [Test]
        public void Create_CursorOutOfRange_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => SeasonCalendar.Create(-1, new uint[] { 1, 2 }));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => SeasonCalendar.Create(3, new uint[] { 1, 2 }), "cursor may equal length, not exceed");
        }

        [Test]
        public void Create_CursorAtLength_IsSeasonComplete()
        {
            SeasonCalendar calendar = SeasonCalendar.Create(2, new uint[] { 1, 2 });

            Assert.That(calendar.IsSeasonComplete, Is.True);
            Assert.Throws<System.InvalidOperationException>(() => calendar.NextFixtureDay());
            Assert.Throws<System.InvalidOperationException>(() => calendar.AdvancedOneRound());
        }

        [Test]
        public void Create_DoesNotAliasCallerArray()
        {
            var days = new uint[] { 1, 2, 3 };
            SeasonCalendar calendar = SeasonCalendar.Create(0, days);

            days[1] = 999;

            Assert.That(calendar.DayOfRound(1), Is.EqualTo(2u),
                "the calendar must snapshot-copy the day mapping");
        }

        /// <summary>KD-4 / FR-SN-011: the next fixture day is never behind the current world day.</summary>
        [Test]
        public void SatisfiesCursorInvariant_HoldsAtOrAheadOfWorldDay_FailsBehind()
        {
            SeasonCalendar calendar = SeasonCalendar.Create(1, new uint[] { 10, 20, 30 });

            Assert.That(calendar.SatisfiesCursorInvariant(20u), Is.True, "equal day satisfies");
            Assert.That(calendar.SatisfiesCursorInvariant(19u), Is.True, "world behind fixture satisfies");
            Assert.That(calendar.SatisfiesCursorInvariant(21u), Is.False, "world past the fixture violates");
        }

        [Test]
        public void SatisfiesCursorInvariant_CompletedSeason_IsVacuouslyTrue()
        {
            SeasonCalendar calendar = SeasonCalendar.Create(2, new uint[] { 10, 20 });
            Assert.That(calendar.SatisfiesCursorInvariant(9999u), Is.True);
        }

        [Test]
        public void AdvancedOneRound_MovesCursorAndLeavesOriginalUnchanged()
        {
            SeasonCalendar calendar = SeasonCalendar.Create(0, new uint[] { 1, 2, 3 });
            SeasonCalendar advanced = calendar.AdvancedOneRound();

            Assert.That(advanced.NextRoundIndex, Is.EqualTo(1));
            Assert.That(calendar.NextRoundIndex, Is.EqualTo(0), "the value type must not mutate in place");
        }

        [Test]
        public void Linear_OverflowingSchedule_Throws()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => SeasonCalendar.Linear(roundCount: 3, firstRoundDay: uint.MaxValue - 1, daysBetweenRounds: 10));
        }
    }

    [TestFixture]
    public sealed class BoardStateTests
    {
        [Test]
        public void IsOnTrack_MeetsObjectiveAtOrAboveTarget()
        {
            var board = BoardState.Fresh(new BoardObjective(4));

            Assert.That(board.IsOnTrack(1), Is.True);
            Assert.That(board.IsOnTrack(4), Is.True, "at the target counts as met");
            Assert.That(board.IsOnTrack(5), Is.False);
        }

        [Test]
        public void Fresh_StartsFullySecure()
        {
            var board = BoardState.Fresh(new BoardObjective(1));
            Assert.That(board.JobSecurityPerMille, Is.EqualTo(SeasonLoopConstants.JOB_SECURITY_SCALE));
        }

        [Test]
        public void Constructor_RejectsOutOfRangeJobSecurity()
        {
            var objective = new BoardObjective(1);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new BoardState(objective, -1));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new BoardState(objective, SeasonLoopConstants.JOB_SECURITY_SCALE + 1));
        }

        [Test]
        public void BoardObjective_RejectsNonPositiveTarget()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new BoardObjective(0));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new BoardObjective(-3));
        }
    }

    [TestFixture]
    public sealed class MatchResultTests
    {
        [Test]
        public void WinnerAndDraw_ReflectTheScoreline()
        {
            var homeWin = new MatchResult(10, 11, 2, 0, 0, 5u);
            Assert.That(homeWin.WinnerClubId, Is.EqualTo(10));
            Assert.That(homeWin.IsDraw, Is.False);

            var awayWin = new MatchResult(10, 11, 0, 3, 0, 5u);
            Assert.That(awayWin.WinnerClubId, Is.EqualTo(11));

            var draw = new MatchResult(10, 11, 1, 1, 0, 5u);
            Assert.That(draw.IsDraw, Is.True);
            Assert.That(draw.WinnerClubId, Is.Null);
        }

        [Test]
        public void Constructor_RejectsIncoherentResults()
        {
            Assert.Throws<System.ArgumentException>(() => new MatchResult(10, 10, 1, 0, 0, 0u));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new MatchResult(10, 11, -1, 0, 0, 0u));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new MatchResult(10, 11, 0, -1, 0, 0u));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new MatchResult(10, 11, 0, 0, -1, 0u));
        }
    }

    [TestFixture]
    public sealed class FixtureTests
    {
        [Test]
        public void MarkPlayed_ReturnsNewValueLeavingOriginalUnplayed()
        {
            var fixture = new Fixture(0, 10, 11);
            Fixture played = fixture.MarkPlayed();

            Assert.That(fixture.Played, Is.False, "the readonly original must be unchanged");
            Assert.That(played.Played, Is.True);
            Assert.That(played.HomeClubId, Is.EqualTo(10));
            Assert.That(played.AwayClubId, Is.EqualTo(11));
            Assert.That(played.RoundIndex, Is.EqualTo(0));
            Assert.That(played.MarkPlayed().Played, Is.True, "idempotent");
        }

        [Test]
        public void Constructor_RejectsSelfFixtureAndNegativeRound()
        {
            Assert.Throws<System.ArgumentException>(() => new Fixture(0, 10, 10));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => new Fixture(-1, 10, 11));
        }

        [Test]
        public void Involves_MatchesEitherClub()
        {
            var fixture = new Fixture(2, 10, 11);
            Assert.That(fixture.Involves(10), Is.True);
            Assert.That(fixture.Involves(11), Is.True);
            Assert.That(fixture.Involves(12), Is.False);
        }
    }

    [TestFixture]
    public sealed class SeasonStateTests
    {
        private static readonly int[] FourClubs = { 10, 11, 12, 13 };

        private static SeasonState NewSeason(int managedClubId = 10) =>
            SeasonState.CreateNew(
                FourClubs,
                managedClubId,
                SeasonLoopConstants.IDENTITY_PERMUTATION_SEED,
                new BoardObjective(2),
                firstRoundDay: 1,
                daysBetweenRounds: 7);

        [Test]
        public void CreateNew_BuildsScheduleTableAndCalendarCoherently()
        {
            SeasonState state = NewSeason();

            Assert.That(state.ClubCount, Is.EqualTo(4));
            Assert.That(state.FixtureCount, Is.EqualTo(4 * 3), "N*(N-1) fixtures");
            Assert.That(state.Calendar.RoundCount, Is.EqualTo(FixtureScheduler.RoundCount(4)));
            Assert.That(state.Calendar.NextRoundIndex, Is.EqualTo(0));
            Assert.That(state.SeasonNumber, Is.EqualTo(0));
            Assert.That(state.ManagedClubId, Is.EqualTo(10));
            Assert.That(state.ClubCount, Is.EqualTo(4));
            Assert.That(state.TableRow(10).Played, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_RejectsManagedClubOutsideClubSet()
        {
            Assert.Throws<System.ArgumentException>(
                () => SeasonState.CreateNew(
                    FourClubs, 99, 0UL, new BoardObjective(1), 1, 7),
                "the managed club must be in the competition");
        }

        [Test]
        public void Constructor_RejectsFixtureNamingAClubOutsideTheSet()
        {
            var fixtures = new[] { new Fixture(0, 10, 99) };

            Assert.Throws<System.ArgumentException>(
                () => new SeasonState(
                    0UL, 0, 10, FourClubs, fixtures,
                    LeagueTable.Empty(FourClubs),
                    SeasonCalendar.Linear(1, 1, 7),
                    BoardState.Fresh(new BoardObjective(1))));
        }

        [Test]
        public void Constructor_RejectsTableThatDoesNotCoverTheClubSet()
        {
            Assert.Throws<System.ArgumentException>(
                () => new SeasonState(
                    0UL, 0, 10, FourClubs, System.Array.Empty<Fixture>(),
                    LeagueTable.Empty(new[] { 10, 11 }),   // only 2 of the 4 clubs
                    SeasonCalendar.Linear(1, 1, 7),
                    BoardState.Fresh(new BoardObjective(1))));
        }

        [Test]
        public void Constructor_RejectsDuplicateClubIdAndNegativeSeasonNumber()
        {
            Assert.Throws<System.ArgumentException>(
                () => new SeasonState(
                    0UL, 0, 10, new[] { 10, 10, 11 }, System.Array.Empty<Fixture>(),
                    LeagueTable.Empty(new[] { 10, 11 }),
                    SeasonCalendar.Linear(1, 1, 7),
                    BoardState.Fresh(new BoardObjective(1))));

            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => new SeasonState(
                    0UL, -1, 10, FourClubs, System.Array.Empty<Fixture>(),
                    LeagueTable.Empty(FourClubs),
                    SeasonCalendar.Linear(1, 1, 7),
                    BoardState.Fresh(new BoardObjective(1))));
        }

        /// <summary>
        /// The copy-then-validate discipline: mutating the caller's arrays after construction must not
        /// reach inside the state (the recurring live-array aliasing defect class).
        /// </summary>
        [Test]
        public void Constructor_DoesNotAliasCallerArrays()
        {
            var clubs = new[] { 10, 11, 12, 13 };
            Fixture[] fixtures = FixtureScheduler.Generate(clubs, 0UL);

            var state = new SeasonState(
                0UL, 0, 10, clubs, fixtures,
                LeagueTable.Empty(clubs),
                SeasonCalendar.Linear(FixtureScheduler.RoundCount(4), 1, 7),
                BoardState.Fresh(new BoardObjective(1)));

            int originalHome = state.FixtureAt(0).HomeClubId;
            fixtures[0] = new Fixture(0, 12, 13);
            clubs[0] = 999;

            Assert.That(state.FixtureAt(0).HomeClubId, Is.EqualTo(originalHome),
                "the schedule must be snapshot-copied");
            Assert.That(state.ClubIds[0], Is.EqualTo(10), "the club set must be snapshot-copied");
        }

        /// <summary>
        /// KD-7 / FR-SN-032: the table is season state, so construction must snapshot-copy it. A caller
        /// retaining the <see cref="LeagueTable"/> it passed in must not be able to mutate the season
        /// afterwards — the reference-type half of the copy-then-validate discipline.
        /// </summary>
        [Test]
        public void Constructor_SnapshotCopiesTheTable_CallerCannotMutateSeasonState()
        {
            LeagueTable caller = LeagueTable.Empty(FourClubs);
            var state = new SeasonState(
                0UL, 0, 10, FourClubs, FixtureScheduler.Generate(FourClubs, 0UL),
                caller,
                SeasonCalendar.Linear(FixtureScheduler.RoundCount(4), 1, 7),
                BoardState.Fresh(new BoardObjective(1)));

            caller.ApplyResult(10, 11, 5, 0);

            Assert.That(state.TableRow(10).Played, Is.EqualTo(0),
                "mutating the caller's table must not reach the season");
            Assert.That(state.TableRow(10).Points, Is.EqualTo(0));
        }

        /// <summary>The public read path exposes the table without exposing a mutation path.</summary>
        [Test]
        public void TableProjections_ExposeReadOnlyViews()
        {
            SeasonState state = NewSeason();
            state.ApplyResult(new MatchResult(13, 10, 3, 0, 0, 0u));

            Assert.That(state.TableOrdered()[0].ClubId, Is.EqualTo(13), "ordered by tie-break");
            Assert.That(state.TableRowsInClubIdOrder()[0].ClubId, Is.EqualTo(10), "canonical order");
            Assert.That(state.TableRow(13).Won, Is.EqualTo(1));
            Assert.That(state.PositionOf(13), Is.EqualTo(1));
            Assert.That(state.PositionOf(10), Is.EqualTo(4));
        }

        [Test]
        public void UnplayedFixtureIndicesInRound_ReturnsWholeRoundThenShrinksAsPlayed()
        {
            SeasonState state = NewSeason();

            int[] round0 = state.UnplayedFixtureIndicesInRound(0);
            Assert.That(round0.Length, Is.EqualTo(2), "a 4-club round holds N/2 = 2 fixtures");

            state.MarkFixturePlayed(round0[0]);

            Assert.That(state.UnplayedFixtureIndicesInRound(0).Length, Is.EqualTo(1));
            Assert.That(state.FixtureAt(round0[0]).Played, Is.True);
            Assert.That(state.FixtureAt(round0[1]).Played, Is.False);
        }

        [Test]
        public void Fixtures_AccessorReturnsCopy_NotALiveView()
        {
            SeasonState state = NewSeason();
            var snapshot = state.Fixtures;

            state.MarkFixturePlayed(0);

            Assert.That(snapshot[0].Played, Is.False,
                "a previously handed-out fixture collection must not see later mutations");
            Assert.That(state.Fixtures[0].Played, Is.True);
        }

        [Test]
        public void Clone_IsDeepAcrossFixturesAndTable()
        {
            SeasonState state = NewSeason();
            SeasonState clone = state.Clone();

            Assert.That(state.FieldsEqual(clone), Is.True, "a fresh clone is field-identical");

            clone.MarkFixturePlayed(0);
            clone.ApplyResult(new MatchResult(10, 11, 3, 0, 0, 0u));

            Assert.That(state.FixtureAt(0).Played, Is.False, "original schedule untouched");
            Assert.That(state.TableRow(10).Played, Is.EqualTo(0), "original table untouched");
            Assert.That(state.FieldsEqual(clone), Is.False);
        }

        [Test]
        public void BeginNextSeason_ResetsTableAndAdvancesCounters()
        {
            SeasonState state = NewSeason();
            state.ApplyResult(new MatchResult(10, 11, 4, 0, 0, 0u));
            state.MarkFixturePlayed(0);
            state.AdvanceCursorOneRound();

            ulong nextSeed = 777UL;
            Fixture[] nextFixtures = FixtureScheduler.Generate(FourClubs, nextSeed);
            SeasonCalendar nextCalendar = SeasonCalendar.Linear(FixtureScheduler.RoundCount(4), 400, 7);

            state.BeginNextSeason(nextSeed, nextFixtures, nextCalendar);

            Assert.That(state.SeasonNumber, Is.EqualTo(1));
            Assert.That(state.Seed, Is.EqualTo(nextSeed));
            Assert.That(state.Calendar.NextRoundIndex, Is.EqualTo(0), "cursor reset");
            Assert.That(state.TableRow(10).Played, Is.EqualTo(0), "table reset");
            Assert.That(state.FixtureAt(0).Played, Is.False, "fresh schedule is unplayed");
        }

        [Test]
        public void BeginNextSeason_RejectsMismatchedFixtureCount()
        {
            SeasonState state = NewSeason();

            Assert.Throws<System.ArgumentException>(
                () => state.BeginNextSeason(1UL, new[] { new Fixture(0, 10, 11) },
                    SeasonCalendar.Linear(1, 1, 7)));
        }

        [Test]
        public void BeginNextSeason_RejectsFixtureNamingAClubOutsideTheSet()
        {
            SeasonState state = NewSeason();
            Fixture[] next = FixtureScheduler.Generate(FourClubs, 5UL);
            next[0] = new Fixture(0, 10, 99);

            Assert.Throws<System.ArgumentException>(
                () => state.BeginNextSeason(
                    5UL, next, SeasonCalendar.Linear(FixtureScheduler.RoundCount(4), 400, 7)));
        }

        [Test]
        public void BeginNextSeason_RejectsCalendarShorterThanTheSchedule()
        {
            SeasonState state = NewSeason();
            Fixture[] next = FixtureScheduler.Generate(FourClubs, 5UL);

            Assert.Throws<System.ArgumentException>(
                () => state.BeginNextSeason(5UL, next, SeasonCalendar.Linear(2, 400, 7)),
                "a 6-round schedule cannot ride a 2-round calendar");
        }

        /// <summary>
        /// A calendar that does not cover the schedule's rounds would strand the cursor mid-season
        /// (and throw from DayOfRound), so the constructor rejects it — including the degenerate
        /// default(SeasonCalendar), whose RoundCount is 0.
        /// </summary>
        [Test]
        public void Constructor_RejectsCalendarShorterThanTheSchedule()
        {
            Fixture[] fixtures = FixtureScheduler.Generate(FourClubs, 0UL);

            Assert.Throws<System.ArgumentException>(
                () => new SeasonState(
                    0UL, 0, 10, FourClubs, fixtures,
                    LeagueTable.Empty(FourClubs),
                    SeasonCalendar.Linear(2, 1, 7),   // schedule spans 6 rounds
                    BoardState.Fresh(new BoardObjective(1))));

            Assert.Throws<System.ArgumentException>(
                () => new SeasonState(
                    0UL, 0, 10, FourClubs, fixtures,
                    LeagueTable.Empty(FourClubs),
                    default,                           // RoundCount 0
                    BoardState.Fresh(new BoardObjective(1))));
        }

        /// <summary>
        /// Two seasons created from identical inputs are field-identical — the FR-SN-030 shape at the
        /// value-type level (the full two-run season lock lands with the T2 loop).
        /// </summary>
        [Test]
        public void CreateNew_TwiceWithSameInputs_IsFieldIdentical()
        {
            Assert.That(NewSeason().FieldsEqual(NewSeason()), Is.True);
        }
    }

    [TestFixture]
    public sealed class SeasonViewModelTests
    {
        private static readonly int[] FourClubs = { 10, 11, 12, 13 };

        private static SeasonState NewSeason() =>
            SeasonState.CreateNew(
                FourClubs, 10, SeasonLoopConstants.IDENTITY_PERMUTATION_SEED,
                new BoardObjective(2), firstRoundDay: 1, daysBetweenRounds: 7);

        [Test]
        public void From_ProjectsTableCalendarAndBoard()
        {
            SeasonState state = NewSeason();
            state.ApplyResult(new MatchResult(10, 11, 3, 0, 0, 0u));

            SeasonViewModel view = state.View();

            Assert.That(view.ManagedClubId, Is.EqualTo(10));
            Assert.That(view.Table.Count, Is.EqualTo(4));
            Assert.That(view.Table[0].ClubId, Is.EqualTo(10), "the winner tops the ordered table");
            Assert.That(view.ManagedClubPosition, Is.EqualTo(1));
            Assert.That(view.ObjectiveTargetPosition, Is.EqualTo(2));
            Assert.That(view.IsOnTrack, Is.True, "1st meets a top-2 objective");
            Assert.That(view.RoundCount, Is.EqualTo(FixtureScheduler.RoundCount(4)));
            Assert.That(view.NextRoundIndex, Is.EqualTo(0));
            Assert.That(view.IsSeasonComplete, Is.False);
            Assert.That(view.Fixtures.Count, Is.EqualTo(12));
        }

        /// <summary>
        /// FR-SN-033 observer-neutrality: building and reading a view leaves the season state
        /// field-identical.
        /// </summary>
        [Test]
        public void From_IsObserverNeutral()
        {
            SeasonState state = NewSeason();
            state.ApplyResult(new MatchResult(12, 13, 1, 1, 0, 0u));
            SeasonState before = state.Clone();

            SeasonViewModel view = state.View();
            _ = view.Table;
            _ = view.Fixtures;
            _ = view.IsOnTrack;
            _ = state.View();

            Assert.That(state.FieldsEqual(before), Is.True,
                "reading the view model must not perturb season state");
        }

        /// <summary>The view is a snapshot — later season mutation does not reach a handed-out view.</summary>
        [Test]
        public void View_IsASnapshotNotALiveWindow()
        {
            SeasonState state = NewSeason();
            SeasonViewModel view = state.View();

            state.ApplyResult(new MatchResult(10, 11, 5, 0, 0, 0u));
            state.MarkFixturePlayed(0);

            Assert.That(view.Table[0].Played, Is.EqualTo(0),
                "the previously taken view must still show the pre-result table");
            Assert.That(view.Fixtures[0].Played, Is.False);
        }

        [Test]
        public void From_NullState_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => SeasonViewModel.From(null));
        }
    }

    [TestFixture]
    public sealed class SeasonStateCouplingTests
    {
        [Test]
        public void SeasonState_InstanceFieldCount_MatchesFieldsEqualSet()
        {
            // Mechanical coupling guard, the PassExecutorStateTests.cs:348 / ShotExecutorStateTests.cs:376
            // analogue. FieldsEqual is the equality oracle the T1 codec round-trip (FR-SN-022) and the T2
            // two-run determinism lock (FR-SN-030) assert against. A field added here but omitted from
            // BOTH the codec and FieldsEqual makes the round-trip pass while the save silently drops it —
            // invisible in-process, surfacing only as a corrupted season after a reload. This count trips
            // first. (8 = _clubIds + _fixtures + the six auto-property backing fields: Seed, SeasonNumber,
            // ManagedClubId, Table, Calendar, Board.)
            int fieldCount = typeof(SeasonState)
                .GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                .Length;

            Assert.AreEqual(8, fieldCount,
                "SeasonState instance field count changed. If you added serialized season state, extend " +
                "FieldsEqual + Clone + the T1 SeasonStateCodec, then update this count.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-07-25 | —      | Initial suite (#30 T0): calendar/KD-4 invariant, board,   |
// |         |            |        | match result, fixture, SeasonState coherence + aliasing,  |
// |         |            |        | and SeasonViewModel observer-neutrality locks.            |
// | 1.1     | 2026-07-25 | —      | AR pass 3: added the SeasonState instance-field-count     |
// |         |            |        | coupling guard (FieldsEqual/Clone/T1-codec omission lock).|
#endregion
