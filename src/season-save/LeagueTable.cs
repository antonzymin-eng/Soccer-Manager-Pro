// File:     src/season-save/LeagueTable.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 §2.2 (data structures), §3.2 (ApplyResult / tie-break),
//           Appendix B row 10, Appendix D, FR-SN-005..008 / FR-SN-033; Code Standards #20
// Purpose:  The live league table — one row per club, updated by ApplyResult, read in tie-break order
//           through OrderedView. Rows are stored in ascending ClubId order (Appendix B row 10) so the
//           serialized layout is canonical regardless of the order results arrived in.

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The league table (#30 §2.2 / §3.2). Holds exactly one <see cref="LeagueTableRow"/> per club,
    /// stored in ascending <c>ClubId</c> order (Appendix B row 10 — a canonical storage order, so two
    /// tables that received the same results in different orders serialize identically).
    /// <para>
    /// <b>Observer-neutrality (FR-SN-033).</b> <see cref="OrderedView"/> sorts and returns a COPY;
    /// reading the table never reorders or mutates stored rows, so a read can never perturb the save
    /// digest (the <c>match-viewer</c> / <c>MatchEngine.BallView</c> posture).
    /// </para>
    /// </summary>
    public sealed class LeagueTable
    {
        private readonly int[] _clubIds;      // ascending; the row index key
        private readonly LeagueTableRow[] _rows;

        private LeagueTable(int[] ascendingClubIds, LeagueTableRow[] rows)
        {
            _clubIds = ascendingClubIds;
            _rows = rows;
        }

        /// <summary>The number of clubs in the table.</summary>
        public int ClubCount => _clubIds.Length;

        /// <summary>
        /// Creates an empty table over <paramref name="clubIds"/> — every club present with a zeroed
        /// row (the §3.5 step (e) "reset table" shape).
        /// </summary>
        /// <exception cref="System.ArgumentNullException"><paramref name="clubIds"/> is null.</exception>
        /// <exception cref="System.ArgumentException">Fewer than 2 clubs, or a duplicate id (which would
        /// break the FR-SN-007 total order).</exception>
        public static LeagueTable Empty(int[] clubIds)
        {
            if (clubIds == null)
            {
                throw new System.ArgumentNullException(nameof(clubIds));
            }

            if (clubIds.Length < 2)
            {
                throw new System.ArgumentException(
                    $"A league table needs at least 2 clubs; got {clubIds.Length}.", nameof(clubIds));
            }

            // Snapshot-copy before sorting: never reorder the caller's array (the recurring
            // "ctor retained the caller's LIVE array" defect class — living-world slice-2 AR-1 M-1,
            // match-viewer AR-3 M-1).
            var ids = new int[clubIds.Length];
            System.Array.Copy(clubIds, ids, clubIds.Length);
            System.Array.Sort(ids);

            for (int i = 1; i < ids.Length; i++)
            {
                if (ids[i] == ids[i - 1])
                {
                    throw new System.ArgumentException(
                        $"Duplicate ClubId {ids[i]} in the league table club set.", nameof(clubIds));
                }
            }

            var rows = new LeagueTableRow[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                rows[i] = LeagueTableRow.Empty(ids[i]);
            }

            return new LeagueTable(ids, rows);
        }

        /// <summary>
        /// Rebuilds a table from already-populated rows — the #30 T1 decode path. Rows are stored in
        /// ascending <c>ClubId</c> order regardless of the order supplied.
        /// </summary>
        /// <exception cref="System.ArgumentNullException"><paramref name="rows"/> is null.</exception>
        /// <exception cref="System.ArgumentException">Fewer than 2 rows, or a duplicate <c>ClubId</c>.</exception>
        public static LeagueTable FromRows(LeagueTableRow[] rows)
        {
            if (rows == null)
            {
                throw new System.ArgumentNullException(nameof(rows));
            }

            if (rows.Length < 2)
            {
                throw new System.ArgumentException(
                    $"A league table needs at least 2 rows; got {rows.Length}.", nameof(rows));
            }

            var copy = new LeagueTableRow[rows.Length];
            System.Array.Copy(rows, copy, rows.Length);
            System.Array.Sort(copy, static (a, b) => a.ClubId.CompareTo(b.ClubId));

            var ids = new int[copy.Length];
            for (int i = 0; i < copy.Length; i++)
            {
                ids[i] = copy[i].ClubId;
                if (i > 0 && ids[i] == ids[i - 1])
                {
                    throw new System.ArgumentException(
                        $"Duplicate ClubId {ids[i]} in the supplied table rows.", nameof(rows));
                }
            }

            return new LeagueTable(ids, copy);
        }

        /// <summary>
        /// Applies one result to both clubs' rows (#30 §3.2 / FR-SN-006).
        /// </summary>
        /// <exception cref="System.ArgumentException">Self-fixture, or a club not in this table (F2).</exception>
        /// <exception cref="System.ArgumentOutOfRangeException">A negative goal count (F2).</exception>
        public void ApplyResult(int homeClubId, int awayClubId, int homeGoals, int awayGoals)
        {
            if (homeClubId == awayClubId)
            {
                throw new System.ArgumentException(
                    $"A result cannot be a self-fixture; got club {homeClubId} against itself.",
                    nameof(awayClubId));
            }

            if (homeGoals < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(homeGoals), homeGoals, "Goal counts must be non-negative.");
            }

            if (awayGoals < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(awayGoals), awayGoals, "Goal counts must be non-negative.");
            }

            // Resolve BOTH rows before writing EITHER — an unknown away club must not leave the home
            // club's row already updated (the ConfigureSquads validate-both-before-write discipline;
            // F2 says "throw; table unchanged").
            int homeIndex = IndexOf(homeClubId);
            if (homeIndex < 0)
            {
                throw new System.ArgumentException(
                    $"Club {homeClubId} is not in this league table.", nameof(homeClubId));
            }

            int awayIndex = IndexOf(awayClubId);
            if (awayIndex < 0)
            {
                throw new System.ArgumentException(
                    $"Club {awayClubId} is not in this league table.", nameof(awayClubId));
            }

            _rows[homeIndex] = _rows[homeIndex].WithResult(homeGoals, awayGoals);
            _rows[awayIndex] = _rows[awayIndex].WithResult(awayGoals, homeGoals);
        }

        /// <summary>Applies one <see cref="MatchResult"/> to the table (the §3.4 call shape).</summary>
        public void ApplyResult(in MatchResult result) =>
            ApplyResult(result.HomeClubId, result.AwayClubId, result.HomeGoals, result.AwayGoals);

        /// <summary>
        /// The table in FR-SN-007 tie-break order (Points → GD → GF → ClubId asc), as a read-only COPY.
        /// <para>
        /// The comparator is a total order (unique <c>ClubId</c> as the final key), so the result does
        /// not depend on sort stability — which matters because <c>Array.Sort</c> is an unstable
        /// introsort. Ordering never touches the stored rows (FR-SN-033).
        /// </para>
        /// </summary>
        public ReadOnlyCollection<LeagueTableRow> OrderedView()
        {
            var ordered = new LeagueTableRow[_rows.Length];
            System.Array.Copy(_rows, ordered, _rows.Length);
            System.Array.Sort(ordered, static (a, b) => LeagueTableRow.CompareByTieBreak(a, b));
            return new ReadOnlyCollection<LeagueTableRow>(ordered);
        }

        /// <summary>
        /// The stored rows in canonical ascending-<c>ClubId</c> order, as a read-only copy — the
        /// serialization order (Appendix B row 10), used by #30 T1's encoder.
        /// </summary>
        public ReadOnlyCollection<LeagueTableRow> RowsInClubIdOrder()
        {
            var copy = new LeagueTableRow[_rows.Length];
            System.Array.Copy(_rows, copy, _rows.Length);
            return new ReadOnlyCollection<LeagueTableRow>(copy);
        }

        /// <summary>Returns a copy of <paramref name="clubId"/>'s row.</summary>
        /// <exception cref="System.ArgumentException">The club is not in this table.</exception>
        public LeagueTableRow Row(int clubId)
        {
            int index = IndexOf(clubId);
            if (index < 0)
            {
                throw new System.ArgumentException(
                    $"Club {clubId} is not in this league table.", nameof(clubId));
            }

            return _rows[index];
        }

        /// <summary>True when <paramref name="clubId"/> has a row in this table.</summary>
        public bool Contains(int clubId) => IndexOf(clubId) >= 0;

        /// <summary>
        /// The 1-based finishing position of <paramref name="clubId"/> in tie-break order — the input
        /// to the board's objective read (FR-SN-015).
        /// </summary>
        /// <exception cref="System.ArgumentException">The club is not in this table.</exception>
        public int PositionOf(int clubId)
        {
            if (!Contains(clubId))
            {
                throw new System.ArgumentException(
                    $"Club {clubId} is not in this league table.", nameof(clubId));
            }

            ReadOnlyCollection<LeagueTableRow> ordered = OrderedView();
            for (int i = 0; i < ordered.Count; i++)
            {
                if (ordered[i].ClubId == clubId)
                {
                    return i + 1;
                }
            }

            // Unreachable: Contains() passed, so the club has exactly one row in the ordered copy.
            throw new System.InvalidOperationException(
                $"Club {clubId} passed the containment check but is absent from the ordered view.");
        }

        /// <summary>Binary search over the ascending club-id key; −1 when absent.</summary>
        private int IndexOf(int clubId)
        {
            int index = System.Array.BinarySearch(_clubIds, clubId);
            return index < 0 ? -1 : index;
        }

        /// <summary>
        /// A deep copy of this table — used where a value copy must not alias the live table
        /// (the <see cref="SeasonViewModel"/> observation surface).
        /// </summary>
        public LeagueTable Clone()
        {
            var ids = new int[_clubIds.Length];
            System.Array.Copy(_clubIds, ids, _clubIds.Length);
            var rows = new LeagueTableRow[_rows.Length];
            System.Array.Copy(_rows, rows, _rows.Length);
            return new LeagueTable(ids, rows);
        }

        /// <summary>
        /// Field-identity comparison over the stored rows — the round-trip assertion #30 T1's codec
        /// tests need (and, at T2, the two-run season determinism lock).
        /// </summary>
        public bool FieldsEqual(LeagueTable other)
        {
            if (other == null || other._rows.Length != _rows.Length)
            {
                return false;
            }

            for (int i = 0; i < _rows.Length; i++)
            {
                LeagueTableRow a = _rows[i];
                LeagueTableRow b = other._rows[i];
                if (a.ClubId != b.ClubId || a.Played != b.Played || a.Won != b.Won ||
                    a.Drawn != b.Drawn || a.Lost != b.Lost || a.GoalsFor != b.GoalsFor ||
                    a.GoalsAgainst != b.GoalsAgainst || a.GoalDifference != b.GoalDifference ||
                    a.Points != b.Points)
                {
                    return false;
                }
            }

            return true;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T0): ascending-ClubId canonical row    |
// |         |            |        | storage, ApplyResult with resolve-both-before-write F2 gating,     |
// |         |            |        | OrderedView read-only copy, PositionOf, Clone, FieldsEqual.        |
#endregion
