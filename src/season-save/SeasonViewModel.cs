// File:     src/season-save/SeasonViewModel.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 §2.2, §4.3, FR-SN-033, KD-7; Code Standards #20
// Purpose:  The read-only, value-copy observation surface over SeasonState for Match Analytics #37 and
//           the UI #38 — the match-viewer / MatchEngine.BallView observer-neutral posture. Building or
//           reading one never mutates season state and never perturbs the save digest.

using System.Collections.ObjectModel;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// A read-only snapshot of the season for presentation and analytics (#30 §2.2 / FR-SN-033).
    /// <para>
    /// <b>Observer-neutral by construction.</b> Every field is a value copy taken at construction —
    /// the view holds no reference into the live <see cref="SeasonState"/>, so a consumer cannot reach
    /// through it to mutate season state, and a later mutation of the season does not retroactively
    /// change a view already handed out. Consumers mutate only through <c>SeasonLoop</c>'s public
    /// command API (FR-SN-032).
    /// </para>
    /// </summary>
    public readonly struct SeasonViewModel
    {
        /// <summary>The league table in FR-SN-007 tie-break order (a copy).</summary>
        public readonly ReadOnlyCollection<LeagueTableRow> Table;

        /// <summary>The full schedule (a copy), including each fixture's played flag.</summary>
        public readonly ReadOnlyCollection<Fixture> Fixtures;

        /// <summary>The index of the next round to be played; equals <see cref="RoundCount"/> when the
        /// season is complete.</summary>
        public readonly int NextRoundIndex;

        /// <summary>The number of rounds in the season.</summary>
        public readonly int RoundCount;

        /// <summary>The multi-season counter.</summary>
        public readonly int SeasonNumber;

        /// <summary>The human manager's club.</summary>
        public readonly int ManagedClubId;

        /// <summary>The managed club's current 1-based league position.</summary>
        public readonly int ManagedClubPosition;

        /// <summary>The board's objective target position.</summary>
        public readonly int ObjectiveTargetPosition;

        /// <summary>Whether the managed club's current position meets the board objective (FR-SN-015).</summary>
        public readonly bool IsOnTrack;

        /// <summary>Board job security, per-mille (see <see cref="BoardState"/>).</summary>
        public readonly int JobSecurityPerMille;

        private SeasonViewModel(
            ReadOnlyCollection<LeagueTableRow> table,
            ReadOnlyCollection<Fixture> fixtures,
            int nextRoundIndex,
            int roundCount,
            int seasonNumber,
            int managedClubId,
            int managedClubPosition,
            int objectiveTargetPosition,
            bool isOnTrack,
            int jobSecurityPerMille)
        {
            Table = table;
            Fixtures = fixtures;
            NextRoundIndex = nextRoundIndex;
            RoundCount = roundCount;
            SeasonNumber = seasonNumber;
            ManagedClubId = managedClubId;
            ManagedClubPosition = managedClubPosition;
            ObjectiveTargetPosition = objectiveTargetPosition;
            IsOnTrack = isOnTrack;
            JobSecurityPerMille = jobSecurityPerMille;
        }

        /// <summary>
        /// Builds the view from a season state. Pure: <paramref name="state"/> is only read
        /// (<see cref="LeagueTable.OrderedView"/> sorts a copy, never the stored rows).
        /// </summary>
        /// <exception cref="System.ArgumentNullException"><paramref name="state"/> is null.</exception>
        public static SeasonViewModel From(SeasonState state)
        {
            if (state == null)
            {
                throw new System.ArgumentNullException(nameof(state));
            }

            int position = state.Table.PositionOf(state.ManagedClubId);

            return new SeasonViewModel(
                state.Table.OrderedView(),
                state.Fixtures,
                state.Calendar.NextRoundIndex,
                state.Calendar.RoundCount,
                state.SeasonNumber,
                state.ManagedClubId,
                position,
                state.Board.Objective.TargetPositionOrBetter,
                state.Board.IsOnTrack(position),
                state.Board.JobSecurityPerMille);
        }

        /// <summary>True once every round has been played.</summary>
        public bool IsSeasonComplete => NextRoundIndex >= RoundCount;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T0): value-copy observation surface    |
// |         |            |        | for #37/#38 with the managed club's position + on-track read.      |
#endregion
