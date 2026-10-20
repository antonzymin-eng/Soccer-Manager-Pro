// File:     src/client-app/CareerHomeViewModel.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (Career Home), ux-detailed-plan.md
//           §7.5 (information hierarchy), ux-experience-architecture.md §5–§7, Season & Competition
//           Loop #30 FR-SN-033 (SeasonViewModel), UI / Client Framework #38 §3.1, Code Standards #20
// Purpose:  The immutable Career Home projection: what changed, what needs attention, what is next,
//           and how to progress — every field read from a LIVE #30/#41 surface, nothing synthesized.

using System.Collections.ObjectModel;

using TacticalDirector.SeasonSave;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Career Home's view model (§5-P7). Answers ux-detailed-plan §7.5's questions in order:
    /// <b>what changed</b> (<see cref="LastChange"/>), <b>what needs attention</b>
    /// (<see cref="InjuredPlayerCount"/>, <see cref="IsObjectiveOnTrack"/>, <see cref="AutosaveError"/>),
    /// <b>what is next</b> (the next-fixture fields), and <b>how to progress</b>
    /// (<see cref="NextAction"/> / <see cref="IsProgressBlocked"/>). A <c>default</c> value has
    /// <see cref="HasCareer"/> false and <see cref="NextAction"/> <see cref="CareerProgressAction.None"/>.
    /// </summary>
    public readonly struct CareerHomeViewModel
    {
        /// <summary>True when a career is active; every other field is meaningful only then.</summary>
        public readonly bool HasCareer;

        /// <summary>The managed club's id.</summary>
        public readonly int ManagedClubId;

        /// <summary>The managed club's display name.</summary>
        public readonly string ManagedClubName;

        /// <summary>#30's season counter (0 = the career's first season); a display adds 1.</summary>
        public readonly int SeasonNumber;

        /// <summary>The current world day.</summary>
        public readonly uint WorldDay;

        /// <summary>Rounds played this season.</summary>
        public readonly int RoundsPlayed;

        /// <summary>Rounds in the season.</summary>
        public readonly int RoundCount;

        /// <summary>The managed club's 1-based league position.</summary>
        public readonly int LeaguePosition;

        /// <summary>The managed club's table row.</summary>
        public readonly LeagueTableRow ManagedRow;

        /// <summary>The board's target ("finish at or above this position").</summary>
        public readonly int ObjectiveTargetPosition;

        /// <summary>True when the current position meets the board objective.</summary>
        public readonly bool IsObjectiveOnTrack;

        /// <summary>Job security, per mille.</summary>
        public readonly int JobSecurityPerMille;

        /// <summary>Managed-club players currently injured (#41 <c>IsAvailable</c> false).</summary>
        public readonly int InjuredPlayerCount;

        /// <summary>True when the season has an unplayed round for the managed club.</summary>
        public readonly bool HasNextFixture;

        /// <summary>The next opponent's club id (valid when <see cref="HasNextFixture"/>).</summary>
        public readonly int NextOpponentId;

        /// <summary>The next opponent's display name (valid when <see cref="HasNextFixture"/>).</summary>
        public readonly string NextOpponentName;

        /// <summary>True when the managed club is at home in the next fixture.</summary>
        public readonly bool NextFixtureIsHome;

        /// <summary>The next fixture's 0-based round index.</summary>
        public readonly int NextFixtureRoundIndex;

        /// <summary>The next fixture's world day.</summary>
        public readonly uint NextFixtureDay;

        /// <summary>Days until the next matchday (0 on matchday, -1 once the season is complete).</summary>
        public readonly int DaysUntilNextMatchday;

        /// <summary>The single legal progress action.</summary>
        public readonly CareerProgressAction NextAction;

        /// <summary>True while an operation is running — progress is disabled with that reason (§6).</summary>
        public readonly bool IsProgressBlocked;

        /// <summary>The most recent operation.</summary>
        public readonly CareerChange LastChange;

        /// <summary>The opponent's name in <see cref="LastChange"/>'s managed result, or empty.</summary>
        public readonly string LastResultOpponentName;

        /// <summary>The last autosave failure's message, or null — persistent until a save succeeds (§7 rule 5).</summary>
        public readonly string AutosaveError;

        /// <summary>The league table in position order.</summary>
        public readonly ReadOnlyCollection<LeagueTableRow> Table;

        /// <summary>Display names parallel to <see cref="Table"/> (row i's club name).</summary>
        public readonly ReadOnlyCollection<string> TableClubNames;

        /// <summary>Constructs a populated view model.</summary>
        public CareerHomeViewModel(
            int managedClubId,
            string managedClubName,
            int seasonNumber,
            uint worldDay,
            int roundsPlayed,
            int roundCount,
            int leaguePosition,
            in LeagueTableRow managedRow,
            int objectiveTargetPosition,
            bool isObjectiveOnTrack,
            int jobSecurityPerMille,
            int injuredPlayerCount,
            bool hasNextFixture,
            int nextOpponentId,
            string nextOpponentName,
            bool nextFixtureIsHome,
            int nextFixtureRoundIndex,
            uint nextFixtureDay,
            int daysUntilNextMatchday,
            CareerProgressAction nextAction,
            bool isProgressBlocked,
            in CareerChange lastChange,
            string lastResultOpponentName,
            string autosaveError,
            ReadOnlyCollection<LeagueTableRow> table,
            ReadOnlyCollection<string> tableClubNames)
        {
            HasCareer = true;
            ManagedClubId = managedClubId;
            ManagedClubName = managedClubName ?? string.Empty;
            SeasonNumber = seasonNumber;
            WorldDay = worldDay;
            RoundsPlayed = roundsPlayed;
            RoundCount = roundCount;
            LeaguePosition = leaguePosition;
            ManagedRow = managedRow;
            ObjectiveTargetPosition = objectiveTargetPosition;
            IsObjectiveOnTrack = isObjectiveOnTrack;
            JobSecurityPerMille = jobSecurityPerMille;
            InjuredPlayerCount = injuredPlayerCount;
            HasNextFixture = hasNextFixture;
            NextOpponentId = nextOpponentId;
            NextOpponentName = nextOpponentName ?? string.Empty;
            NextFixtureIsHome = nextFixtureIsHome;
            NextFixtureRoundIndex = nextFixtureRoundIndex;
            NextFixtureDay = nextFixtureDay;
            DaysUntilNextMatchday = daysUntilNextMatchday;
            NextAction = nextAction;
            IsProgressBlocked = isProgressBlocked;
            LastChange = lastChange;
            LastResultOpponentName = lastResultOpponentName ?? string.Empty;
            AutosaveError = autosaveError;
            Table = table;
            TableClubNames = tableClubNames;
        }

        /// <summary>True once every round is played (the boundary roll is next).</summary>
        public bool IsSeasonComplete => HasCareer && RoundsPlayed >= RoundCount;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): the Career Home projection.          |
#endregion
