// File:     src/season-save/SeasonState.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 §2.2 (data structures), §3.4/§3.5, Appendix B (byte layout),
//           FR-SN-013b/019/028/032, KD-5/KD-7; Code Standards #20
// Purpose:  The season sub-blob's serialized surface — seed, managed club, club set, the SERIALIZED
//           schedule (KD-5), the live table, the calendar cursor, the board, and the season number.
//
// KD-7 single-writer: every mutator here is `internal`, so the only production writer is SeasonLoop
// (#30 T2) inside this assembly. Callers outside it (UI, #37/#38) read through SeasonViewModel and
// mutate only via the loop's public command API (FR-SN-032). Tests reach the internals through
// AssemblyInfo's InternalsVisibleTo.
//
// THREAD SAFETY: none of the #30 T0 types are thread-safe. LeagueTable mutates its rows in place and
// SeasonState carries mutable auto-properties, so a concurrent read during a mutation can observe a
// torn table. Single-threaded access is the T0 contract. This matters at T2, not now: #38's client
// already runs a sim thread alongside a UI thread (FR-UI-023/F6 cross-thread command marshaling), so
// the SeasonLoop command API must be the marshaling point — the UI thread reads a SeasonViewModel
// snapshot (which copies, see SeasonViewModel.From) and never touches SeasonState directly.

using System.Collections.ObjectModel;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The whole state of one season (#30 §2.2) — exactly the surface the season sub-blob serializes
    /// (Appendix B).
    /// <para>
    /// <b>KD-5 (serialize, don't regenerate).</b> <see cref="Fixtures"/> holds the CONCRETE schedule.
    /// <see cref="FixtureScheduler"/> runs once at season creation; a loaded season trusts the
    /// serialized list and never re-runs the generator, so it is independent of generator-version
    /// drift (a #50 Save-Migration concern).
    /// </para>
    /// </summary>
    public sealed class SeasonState
    {
        private readonly int[] _clubIds;   // ascending, canonical (Appendix B row 5)
        private readonly Fixture[] _fixtures;

        /// <summary>The season seed — the permutation input the schedule was generated from.</summary>
        public ulong Seed { get; private set; }

        /// <summary>The multi-season counter; 0 for the first season of a career.</summary>
        public int SeasonNumber { get; private set; }

        /// <summary>
        /// The human manager's club (FR-SN-013b / KD-9). Selects which of a round's fixtures runs
        /// through the full <c>MatchEngine</c> under the human's tactical influence; the rest resolve
        /// through the round-resolution model.
        /// </summary>
        public int ManagedClubId { get; }

        /// <summary>
        /// The live league table. <b>Internal by design (KD-7 / FR-SN-032)</b> — <see cref="LeagueTable"/>
        /// is a mutable object, so exposing it publicly would let any consumer rewrite season state
        /// without going through the command API, defeating the single-writer contract that every other
        /// mutator here honours. The only production writer is <c>SeasonLoop</c> (#30 T2), inside this
        /// assembly; outside callers read through <see cref="TableOrdered"/> /
        /// <see cref="TableRowsInClubIdOrder"/> / <see cref="TableRow"/> / <see cref="PositionOf"/> or
        /// <see cref="SeasonViewModel"/>.
        /// </summary>
        internal LeagueTable Table { get; private set; }

        /// <summary>The fixture-round cursor (KD-4).</summary>
        public SeasonCalendar Calendar { get; private set; }

        /// <summary>The board's objective and job security.</summary>
        public BoardState Board { get; private set; }

        /// <summary>The number of fixtures in the schedule.</summary>
        public int FixtureCount => _fixtures.Length;

        /// <summary>The number of clubs in the competition.</summary>
        public int ClubCount => _clubIds.Length;

        /// <summary>
        /// Constructs a season state from its full serialized surface — used both by season creation
        /// and (at #30 T1) by the season-state decoder.
        /// </summary>
        /// <exception cref="System.ArgumentNullException">A required reference argument is null.</exception>
        /// <exception cref="System.ArgumentException">
        /// Fewer than 2 clubs; a duplicate club id; a negative season number; a
        /// <paramref name="managedClubId"/> absent from the club set; or a fixture naming a club
        /// outside the club set (each an incoherent-state gate — the fail-loud posture of F2/F3).
        /// </exception>
        public SeasonState(
            ulong seed,
            int seasonNumber,
            int managedClubId,
            int[] clubIds,
            Fixture[] fixtures,
            LeagueTable table,
            SeasonCalendar calendar,
            BoardState board)
        {
            if (clubIds == null)
            {
                throw new System.ArgumentNullException(nameof(clubIds));
            }

            if (fixtures == null)
            {
                throw new System.ArgumentNullException(nameof(fixtures));
            }

            if (table == null)
            {
                throw new System.ArgumentNullException(nameof(table));
            }

            if (seasonNumber < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(seasonNumber), seasonNumber, "seasonNumber must be non-negative.");
            }

            if (clubIds.Length < 2)
            {
                throw new System.ArgumentException(
                    $"A season needs at least 2 clubs; got {clubIds.Length}.", nameof(clubIds));
            }

            // Snapshot-copy BEFORE validating, then validate the copy — so a caller mutating its array
            // after construction cannot invalidate the checks we just passed (the recurring live-array
            // aliasing defect class: living-world slice-2 AR-1 M-1, match-viewer AR-3 M-1, #26 T0 AR-1).
            var ids = new int[clubIds.Length];
            System.Array.Copy(clubIds, ids, clubIds.Length);
            System.Array.Sort(ids);
            for (int i = 1; i < ids.Length; i++)
            {
                if (ids[i] == ids[i - 1])
                {
                    throw new System.ArgumentException(
                        $"Duplicate ClubId {ids[i]} in the season club set.", nameof(clubIds));
                }
            }

            if (System.Array.BinarySearch(ids, managedClubId) < 0)
            {
                throw new System.ArgumentException(
                    $"ManagedClubId {managedClubId} is not in the season's club set.",
                    nameof(managedClubId));
            }

            var sched = new Fixture[fixtures.Length];
            System.Array.Copy(fixtures, sched, fixtures.Length);
            for (int i = 0; i < sched.Length; i++)
            {
                if (System.Array.BinarySearch(ids, sched[i].HomeClubId) < 0 ||
                    System.Array.BinarySearch(ids, sched[i].AwayClubId) < 0)
                {
                    throw new System.ArgumentException(
                        $"Fixture {i} ({sched[i].HomeClubId} v {sched[i].AwayClubId}) names a club " +
                        "outside the season's club set.",
                        nameof(fixtures));
                }
            }

            // The calendar must cover every round the schedule uses, or the cursor can never reach the
            // trailing rounds (and DayOfRound would throw mid-season). A default(SeasonCalendar) has
            // RoundCount 0, so this also rejects an unset calendar alongside a real schedule.
            int maxRound = -1;
            for (int i = 0; i < sched.Length; i++)
            {
                if (sched[i].RoundIndex > maxRound)
                {
                    maxRound = sched[i].RoundIndex;
                }
            }

            if (maxRound >= calendar.RoundCount)
            {
                throw new System.ArgumentException(
                    $"The schedule spans {maxRound + 1} rounds but the calendar maps only " +
                    $"{calendar.RoundCount}.",
                    nameof(calendar));
            }

            if (table.ClubCount != ids.Length)
            {
                throw new System.ArgumentException(
                    $"The table holds {table.ClubCount} clubs but the season has {ids.Length}.",
                    nameof(table));
            }

            for (int i = 0; i < ids.Length; i++)
            {
                if (!table.Contains(ids[i]))
                {
                    throw new System.ArgumentException(
                        $"The table has no row for club {ids[i]}.", nameof(table));
                }
            }

            Seed = seed;
            SeasonNumber = seasonNumber;
            ManagedClubId = managedClubId;
            _clubIds = ids;
            _fixtures = sched;

            // Snapshot-copy the table for the same reason the arrays above are copied: LeagueTable is a
            // mutable reference type, so storing the caller's instance would let them keep a handle and
            // mutate season state from outside the KD-7 write path after construction validated it.
            Table = table.Clone();
            Calendar = calendar;
            Board = board;
        }

        /// <summary>
        /// Creates a fresh season: generates the schedule from <paramref name="seed"/> (KD-5, once),
        /// builds a linear calendar over it, and starts every club on an empty table row.
        /// </summary>
        /// <exception cref="System.ArgumentException">Fewer than 2 clubs, a duplicate id, or a
        /// <paramref name="managedClubId"/> outside the club set.</exception>
        public static SeasonState CreateNew(
            int[] clubIds,
            int managedClubId,
            ulong seed,
            BoardObjective objective,
            uint firstRoundDay,
            uint daysBetweenRounds,
            int seasonNumber = 0)
        {
            Fixture[] fixtures = FixtureScheduler.Generate(clubIds, seed);
            int roundCount = FixtureScheduler.RoundCount(clubIds.Length);

            return new SeasonState(
                seed,
                seasonNumber,
                managedClubId,
                clubIds,
                fixtures,
                LeagueTable.Empty(clubIds),
                SeasonCalendar.Linear(roundCount, firstRoundDay, daysBetweenRounds),
                BoardState.Fresh(objective));
        }

        /// <summary>The club ids, ascending, as a read-only copy.</summary>
        public ReadOnlyCollection<int> ClubIds
        {
            get
            {
                var copy = new int[_clubIds.Length];
                System.Array.Copy(_clubIds, copy, _clubIds.Length);
                return new ReadOnlyCollection<int>(copy);
            }
        }

        /// <summary>The concrete schedule, as a read-only copy (KD-5 — this is serialized verbatim).</summary>
        public ReadOnlyCollection<Fixture> Fixtures
        {
            get
            {
                var copy = new Fixture[_fixtures.Length];
                System.Array.Copy(_fixtures, copy, _fixtures.Length);
                return new ReadOnlyCollection<Fixture>(copy);
            }
        }

        /// <summary>
        /// The league table in FR-SN-007 tie-break order, as read-only value copies — the public read
        /// path onto the table (KD-7: reading never mutates, FR-SN-033).
        /// </summary>
        public ReadOnlyCollection<LeagueTableRow> TableOrdered() => Table.OrderedView();

        /// <summary>
        /// The league table in canonical ascending-<c>ClubId</c> order, as read-only value copies —
        /// the serialization order (#30 T1's encoder reads through here).
        /// </summary>
        public ReadOnlyCollection<LeagueTableRow> TableRowsInClubIdOrder() => Table.RowsInClubIdOrder();

        /// <summary>A copy of <paramref name="clubId"/>'s table row.</summary>
        /// <exception cref="System.ArgumentException">The club is not in this season.</exception>
        public LeagueTableRow TableRow(int clubId) => Table.Row(clubId);

        /// <summary>The 1-based tie-break position of <paramref name="clubId"/>.</summary>
        /// <exception cref="System.ArgumentException">The club is not in this season.</exception>
        public int PositionOf(int clubId) => Table.PositionOf(clubId);

        /// <summary>
        /// Applies one fixture result to the table (the §3.4 step-(1) write). KD-7: internal —
        /// <c>SeasonLoop</c> is the only production caller.
        /// </summary>
        /// <exception cref="System.ArgumentException">A club is not in this season, or a self-fixture (F2).</exception>
        internal void ApplyResult(in MatchResult result) => Table.ApplyResult(result);

        /// <summary>The fixture at <paramref name="index"/> (a value copy).</summary>
        /// <exception cref="System.ArgumentOutOfRangeException">Index out of range.</exception>
        public Fixture FixtureAt(int index)
        {
            if (index < 0 || index >= _fixtures.Length)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(index), index, $"index must be in [0, {_fixtures.Length - 1}].");
            }

            return _fixtures[index];
        }

        /// <summary>
        /// The indices of every UNPLAYED fixture in <paramref name="roundIndex"/> — the §3.4 round
        /// working set (FR-SN-012 resolves all of them).
        /// </summary>
        public int[] UnplayedFixtureIndicesInRound(int roundIndex)
        {
            int count = 0;
            for (int i = 0; i < _fixtures.Length; i++)
            {
                if (_fixtures[i].RoundIndex == roundIndex && !_fixtures[i].Played)
                {
                    count++;
                }
            }

            var indices = new int[count];
            int w = 0;
            for (int i = 0; i < _fixtures.Length; i++)
            {
                if (_fixtures[i].RoundIndex == roundIndex && !_fixtures[i].Played)
                {
                    indices[w++] = i;
                }
            }

            return indices;
        }

        /// <summary>
        /// Marks fixture <paramref name="index"/> played (the §3.4 <c>f.Played := true</c> step, by slot
        /// replacement). KD-7: internal — SeasonLoop is the only production writer.
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">Index out of range.</exception>
        internal void MarkFixturePlayed(int index)
        {
            if (index < 0 || index >= _fixtures.Length)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(index), index, $"index must be in [0, {_fixtures.Length - 1}].");
            }

            _fixtures[index] = _fixtures[index].MarkPlayed();
        }

        /// <summary>Advances the calendar cursor one round (§3.4 tail). KD-7: internal.</summary>
        internal void AdvanceCursorOneRound() => Calendar = Calendar.AdvancedOneRound();

        /// <summary>Replaces the board state (the §3.5 step (b) evaluation writes here). KD-7: internal.</summary>
        internal void SetBoard(BoardState board) => Board = board;

        /// <summary>
        /// Applies the schedule/table/seed half of the §3.5 season-boundary roll. KD-7: internal;
        /// the roll itself (with its (a')/(b') insertion points) is #30 T3's <c>RollToNextSeason</c>.
        /// </summary>
        internal void BeginNextSeason(ulong nextSeed, Fixture[] nextFixtures, SeasonCalendar nextCalendar)
        {
            if (nextFixtures == null)
            {
                throw new System.ArgumentNullException(nameof(nextFixtures));
            }

            if (nextFixtures.Length != _fixtures.Length)
            {
                throw new System.ArgumentException(
                    $"The next season's schedule has {nextFixtures.Length} fixtures but this one has " +
                    $"{_fixtures.Length}; the club set has not changed, so the counts must match.",
                    nameof(nextFixtures));
            }

            // Same coherence gates the constructor applies — a roll must not be able to install a
            // schedule the reset table cannot score (#43's promotion/relegation transform will change
            // the club set at insertion point (a'), and this is what catches a mismatched pairing).
            int maxRound = -1;
            for (int i = 0; i < nextFixtures.Length; i++)
            {
                if (System.Array.BinarySearch(_clubIds, nextFixtures[i].HomeClubId) < 0 ||
                    System.Array.BinarySearch(_clubIds, nextFixtures[i].AwayClubId) < 0)
                {
                    throw new System.ArgumentException(
                        $"Next-season fixture {i} ({nextFixtures[i].HomeClubId} v " +
                        $"{nextFixtures[i].AwayClubId}) names a club outside the season's club set.",
                        nameof(nextFixtures));
                }

                if (nextFixtures[i].RoundIndex > maxRound)
                {
                    maxRound = nextFixtures[i].RoundIndex;
                }
            }

            if (maxRound >= nextCalendar.RoundCount)
            {
                throw new System.ArgumentException(
                    $"The next season's schedule spans {maxRound + 1} rounds but its calendar maps " +
                    $"only {nextCalendar.RoundCount}.",
                    nameof(nextCalendar));
            }

            System.Array.Copy(nextFixtures, _fixtures, nextFixtures.Length);
            Calendar = nextCalendar;
            Table = LeagueTable.Empty(_clubIds);
            Seed = nextSeed;
            SeasonNumber++;
        }

        /// <summary>A deep copy — nothing in the copy aliases this instance.</summary>
        public SeasonState Clone()
        {
            var fixtures = new Fixture[_fixtures.Length];
            System.Array.Copy(_fixtures, fixtures, _fixtures.Length);

            // The constructor snapshot-copies every reference it is handed (arrays and the table), so
            // passing the live members here still yields a fully independent state.
            return new SeasonState(
                Seed, SeasonNumber, ManagedClubId, _clubIds, fixtures,
                Table, Calendar, Board);
        }

        /// <summary>
        /// Field-identity comparison — the #30 T1 round-trip assertion (FR-SN-022) and the T2 two-run
        /// season determinism lock (FR-SN-030).
        /// </summary>
        public bool FieldsEqual(SeasonState other)
        {
            if (other == null ||
                Seed != other.Seed ||
                SeasonNumber != other.SeasonNumber ||
                ManagedClubId != other.ManagedClubId ||
                _clubIds.Length != other._clubIds.Length ||
                _fixtures.Length != other._fixtures.Length ||
                Board.JobSecurityPerMille != other.Board.JobSecurityPerMille ||
                Board.Objective.TargetPositionOrBetter != other.Board.Objective.TargetPositionOrBetter ||
                !Calendar.FieldsEqual(other.Calendar) ||
                !Table.FieldsEqual(other.Table))
            {
                return false;
            }

            for (int i = 0; i < _clubIds.Length; i++)
            {
                if (_clubIds[i] != other._clubIds[i])
                {
                    return false;
                }
            }

            for (int i = 0; i < _fixtures.Length; i++)
            {
                Fixture a = _fixtures[i];
                Fixture b = other._fixtures[i];
                if (a.RoundIndex != b.RoundIndex || a.HomeClubId != b.HomeClubId ||
                    a.AwayClubId != b.AwayClubId || a.Played != b.Played)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The read-only observation surface for #37 / #38 (FR-SN-033).</summary>
        public SeasonViewModel View() => SeasonViewModel.From(this);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T0): full serialized surface with      |
// |         |            |        | copy-then-validate coherence gates, CreateNew, read-only           |
// |         |            |        | accessors, KD-7 internal mutators, Clone/FieldsEqual.              |
// | 1.2     | 2026-07-25 | —      | AR pass 3: recorded the single-threaded-access contract and the    |
// |         |            |        | T2 marshaling obligation (#38 sim/UI thread split).                |
#endregion
