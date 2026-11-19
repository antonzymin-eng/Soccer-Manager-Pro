// File:     src/client-app/CareerHomeSource.cs
// Created:  2026-10-18
// Modified: 2026-11-19
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (Career Home), UI / Client Framework
//           #38 §3.1 (FR-UI-002/006/007/008), Season & Competition Loop #30 FR-SN-033, Injuries &
//           Medical #41 FR-MD-023, Code Standards #20
// Purpose:  Career Home's view-model source: a pure read of the active career, cached on the session's
//           revision so a per-frame poll allocates nothing between operations.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using TacticalDirector.PlayerDatabase;
using TacticalDirector.SeasonSave;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Projects <see cref="CareerHomeViewModel"/> from the career in a <see cref="CareerSessionHolder"/>
    /// (§5-P7). Reading never mutates (FR-UI-006): every value comes from #30's read surface
    /// (<see cref="SeasonLoop.State"/>, <see cref="SeasonLoop.CurrentWorldDay"/>), #41's availability
    /// read, and the session's own change record.
    /// <para>
    /// <b>Allocation.</b> A fresh projection allocates (table, fixtures, and scores). It is rebuilt when the
    /// active session, its <see cref="CareerSession.Revision"/>, or its busy state changes. Otherwise <see cref="Project"/>
    /// returns the cached value. #30 has no other writer than the session (FR-SN-032), so the revision
    /// is a complete change signal.
    /// </para>
    /// </summary>
    public sealed class CareerHomeSource : IViewModelSource<CareerHomeViewModel>
    {
        private static readonly ReadOnlyCollection<CareerFixtureResult> EMPTY_RESULTS =
            Array.AsReadOnly(Array.Empty<CareerFixtureResult>());

        private readonly CareerSessionHolder _holder;
        private CareerSession _cachedSession;
        private int _cachedRevision;
        private bool _cachedBusy;
        private CareerHomeViewModel _cached;

        /// <summary>Creates the source over the client's career holder.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="holder"/> is null.</exception>
        public CareerHomeSource(CareerSessionHolder holder)
        {
            _holder = holder ?? throw new ArgumentNullException(nameof(holder));
        }

        /// <inheritdoc />
        public CareerHomeViewModel Project()
        {
            CareerSession session = _holder.Current;
            if (session == null)
            {
                _cachedSession = null;
                return default;
            }

            if (!ReferenceEquals(session, _cachedSession) || session.Revision != _cachedRevision || session.IsBusy != _cachedBusy)
            {
                _cached = Build(session);
                _cachedSession = session;
                _cachedRevision = session.Revision;
                _cachedBusy = session.IsBusy;
            }

            return _cached;
        }

        private static CareerHomeViewModel Build(CareerSession session)
        {
            SeasonLoop loop = session.Loop;
            SeasonState state = loop.State;
            SeasonCalendar calendar = state.Calendar;
            int managed = state.ManagedClubId;

            ReadOnlyCollection<LeagueTableRow> table = state.TableOrdered();
            var names = new string[table.Count];
            for (int i = 0; i < names.Length; i++)
            {
                names[i] = session.ClubName(table[i].ClubId);
            }

            bool hasNext = false;
            CareerFixtureView next = default;
            var fixtures = new List<CareerFixtureView>(calendar.RoundCount);
            for (int i = 0; i < state.FixtureCount; i++)
            {
                Fixture fixture = state.FixtureAt(i);
                if (!fixture.Involves(managed))
                {
                    continue;
                }

                bool isHome = fixture.HomeClubId == managed;
                int opponent = isHome ? fixture.AwayClubId : fixture.HomeClubId;
                var row = new CareerFixtureView(fixture.RoundIndex, calendar.DayOfRound(fixture.RoundIndex),
                    opponent, session.ClubName(opponent), isHome, fixture.Played);
                fixtures.Add(row);

                if (!hasNext && row.RoundIndex == calendar.NextRoundIndex && !row.IsPlayed)
                {
                    hasNext = true;
                    next = row;
                }
            }

            fixtures.Sort(static (left, right) => left.RoundIndex.CompareTo(right.RoundIndex));

            CareerChange change = session.LastChange;
            string lastOpponent = string.Empty;
            if (change.HasManagedResult)
            {
                MatchResult r = change.ManagedResult;
                lastOpponent = session.ClubName(r.HomeClubId == managed ? r.AwayClubId : r.HomeClubId);
            }

            int position = state.PositionOf(managed);
            BoardState board = state.Board;

            return new CareerHomeViewModel(
                managed,
                session.ManagedClubName,
                state.SeasonNumber,
                loop.CurrentWorldDay,
                calendar.NextRoundIndex,
                calendar.RoundCount,
                position,
                state.TableRow(managed),
                board.Objective.TargetPositionOrBetter,
                board.IsOnTrack(position),
                board.JobSecurityPerMille,
                CountInjured(loop, managed),
                hasNext,
                next.OpponentId,
                next.OpponentName,
                next.IsHome,
                calendar.NextRoundIndex,
                next.WorldDay,
                CareerProgress.DaysUntilNextMatchday(loop),
                CareerProgress.NextAction(loop),
                session.IsBusy,
                in change,
                lastOpponent,
                session.LastAutosaveError,
                table,
                new ReadOnlyCollection<string>(names),
                ProjectRoundResults(session.League, loop),
                fixtures.AsReadOnly(),
                calendar);
        }

        internal static ReadOnlyCollection<CareerFixtureResult> ProjectRoundResults(League league, SeasonLoop loop)
        {
            SeasonCalendar calendar = loop.State.Calendar;
            int round = calendar.NextRoundIndex - 1;
            if (round < 0)
            {
                return EMPTY_RESULTS;
            }

            ReadOnlyCollection<MatchResult> outcomes = loop.MatchOutcomes;
            if (outcomes.Count == 0)
            {
                return EMPTY_RESULTS;
            }

            uint matchday = calendar.DayOfRound(round);
            var results = new List<CareerFixtureResult>();
            // The cursor excludes an unfinished round. The day distinguishes repeated round indices
            // because the outcome stream retains previous seasons and is ordered by world day.
            for (int i = outcomes.Count - 1; i >= 0; i--)
            {
                MatchResult result = outcomes[i];
                if (result.WorldDay < matchday)
                {
                    break;
                }

                if (result.WorldDay == matchday && result.RoundIndex == round)
                {
                    results.Add(new CareerFixtureResult(in result,
                        league.ClubAt(result.HomeClubId).Name, league.ClubAt(result.AwayClubId).Name));
                }
            }

            results.Reverse();
            return results.AsReadOnly();
        }

        private static int CountInjured(SeasonLoop loop, int clubId)
        {
            PlayerCareerStates career = loop.Career;
            if (career == null || loop.Progression == null)
            {
                return 0;
            }

            Squad squad = new ProgressionSquads(loop.Progression).ResolveByClubId(clubId);
            int injured = 0;
            for (int p = 0; p < squad.Count; p++)
            {
                if (!career.IsAvailable(clubId, squad.GetPlayer(p).PlayerId))
                {
                    injured++;
                }
            }

            return injured;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): revision-cached Career Home source.  |
// | 1.1     | 2026-11-19 | —      | Project the managed club's dated fixture list and calendar.    |
// | 1.2     | 2026-11-19 | —      | Project scores from the latest completed round.                |
#endregion
