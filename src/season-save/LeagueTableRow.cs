// File:     src/season-save/LeagueTableRow.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 §2.2 (data structures), §3.2 (table update / tie-break),
//           Appendix B row 10 (byte layout), Appendix D (tie-break worked example), FR-SN-005/007;
//           Code Standards #20
// Purpose:  One club's row in the league table. A readonly value: LeagueTable replaces the slot rather
//           than mutating in place, so a row handed to a caller can never alias stored state.

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// One club's league-table row (#30 §2.2 / FR-SN-005).
    /// <para>
    /// <see cref="GoalDifference"/> is always <c>GoalsFor − GoalsAgainst</c>, RECOMPUTED on every
    /// update rather than accumulated (§3.2), so it cannot drift out of agreement with the goal
    /// columns. It is stored (not a computed property) because Appendix B row 10 serializes it as its
    /// own field; <see cref="Create"/> is the only way to build one, and it derives the value, so the
    /// stored and derived forms cannot diverge.
    /// </para>
    /// </summary>
    public readonly struct LeagueTableRow
    {
        /// <summary>The club this row belongs to. Unique within a table — the FR-SN-007 final
        /// tie-break key depends on it.</summary>
        public readonly int ClubId;

        /// <summary>Matches played.</summary>
        public readonly int Played;

        /// <summary>Matches won.</summary>
        public readonly int Won;

        /// <summary>Matches drawn.</summary>
        public readonly int Drawn;

        /// <summary>Matches lost.</summary>
        public readonly int Lost;

        /// <summary>Goals scored.</summary>
        public readonly int GoalsFor;

        /// <summary>Goals conceded.</summary>
        public readonly int GoalsAgainst;

        /// <summary>Goal difference — always <c>GoalsFor − GoalsAgainst</c> (§3.2).</summary>
        public readonly int GoalDifference;

        /// <summary>Points accumulated under the Appendix A points scheme.</summary>
        public readonly int Points;

        private LeagueTableRow(
            int clubId, int played, int won, int drawn, int lost,
            int goalsFor, int goalsAgainst, int points)
        {
            ClubId = clubId;
            Played = played;
            Won = won;
            Drawn = drawn;
            Lost = lost;
            GoalsFor = goalsFor;
            GoalsAgainst = goalsAgainst;
            GoalDifference = goalsFor - goalsAgainst; // §3.2 — recomputed, never accumulated
            Points = points;
        }

        /// <summary>An empty row for <paramref name="clubId"/> — a club that has played nothing yet.</summary>
        public static LeagueTableRow Empty(int clubId) =>
            new LeagueTableRow(clubId, 0, 0, 0, 0, 0, 0, 0);

        /// <summary>
        /// Builds a row from explicit column values, deriving <see cref="GoalDifference"/>. Used by the
        /// table's update path and (at #30 T1) by the season-state decoder.
        /// <para>
        /// <b>This is the fail-loud seam for decoded data (F3 / FR-SN-023).</b> #30 T1 reads nine
        /// integers per row straight out of the save blob and lands them here, so the coherence gate
        /// belongs at this boundary: without it a corrupt file would produce a table with (say)
        /// <c>Played = -5</c> that renders as nonsense instead of failing loud.
        /// </para>
        /// <para>
        /// Points are checked only for non-negativity, deliberately NOT against
        /// <c>3·Won + Drawn</c>: the points scheme is a <c>[GT]</c> catalogue value (Appendix A), so a
        /// rules variant must not fail this gate. Should a later spec introduce points deductions, the
        /// non-negative bound is the one line to revisit.
        /// </para>
        /// </summary>
        /// <exception cref="System.ArgumentOutOfRangeException">Any count is negative.</exception>
        /// <exception cref="System.ArgumentException"><c>won + drawn + lost != played</c>.</exception>
        public static LeagueTableRow Create(
            int clubId, int played, int won, int drawn, int lost,
            int goalsFor, int goalsAgainst, int points)
        {
            RequireNonNegative(played, nameof(played));
            RequireNonNegative(won, nameof(won));
            RequireNonNegative(drawn, nameof(drawn));
            RequireNonNegative(lost, nameof(lost));
            RequireNonNegative(goalsFor, nameof(goalsFor));
            RequireNonNegative(goalsAgainst, nameof(goalsAgainst));
            RequireNonNegative(points, nameof(points));

            if (won + drawn + lost != played)
            {
                throw new System.ArgumentException(
                    $"Incoherent row for club {clubId}: won({won}) + drawn({drawn}) + lost({lost}) = " +
                    $"{won + drawn + lost}, but played = {played}.",
                    nameof(played));
            }

            return new LeagueTableRow(clubId, played, won, drawn, lost, goalsFor, goalsAgainst, points);
        }

        private static void RequireNonNegative(int value, string name)
        {
            if (value < 0)
            {
                throw new System.ArgumentOutOfRangeException(
                    name, value, "League-table counts must be non-negative.");
            }
        }

        /// <summary>
        /// Returns this row updated with one result: <paramref name="scored"/> goals scored,
        /// <paramref name="conceded"/> conceded (§3.2). Venue-agnostic — the caller applies it once per
        /// club with that club's perspective.
        /// </summary>
        public LeagueTableRow WithResult(int scored, int conceded)
        {
            int won = Won;
            int drawn = Drawn;
            int lost = Lost;
            int points = Points;

            if (scored > conceded)
            {
                won++;
                points += SeasonLoopConstants.WinPoints;
            }
            else if (scored < conceded)
            {
                lost++;
                points += SeasonLoopConstants.LossPoints;
            }
            else
            {
                drawn++;
                points += SeasonLoopConstants.DrawPoints;
            }

            return new LeagueTableRow(
                ClubId,
                Played + 1,
                won,
                drawn,
                lost,
                GoalsFor + scored,
                GoalsAgainst + conceded,
                points);
        }

        /// <summary>
        /// The FR-SN-007 tie-break comparison: Points desc → GoalDifference desc → GoalsFor desc →
        /// ClubId asc. Returns a negative value when <paramref name="a"/> orders ABOVE
        /// <paramref name="b"/>.
        /// <para>
        /// This is a <b>total order</b>: club ids are unique within a table (F2 keeps each club to one
        /// row), so the final key never ties and no two rows ever compare equal. That is what makes the
        /// ordering independent of sort stability — see <see cref="LeagueTable.OrderedView"/>.
        /// </para>
        /// </summary>
        public static int CompareByTieBreak(LeagueTableRow a, LeagueTableRow b)
        {
            if (a.Points != b.Points)
            {
                return b.Points.CompareTo(a.Points);
            }

            if (a.GoalDifference != b.GoalDifference)
            {
                return b.GoalDifference.CompareTo(a.GoalDifference);
            }

            if (a.GoalsFor != b.GoalsFor)
            {
                return b.GoalsFor.CompareTo(a.GoalsFor);
            }

            return a.ClubId.CompareTo(b.ClubId); // ascending — the total-order guarantee
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T0): readonly row, derived-on-build    |
// |         |            |        | GoalDifference, WithResult update, CompareByTieBreak total order.  |
#endregion
