// File:     src/client-app/CareerHomeSource.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (Career Home), UI / Client Framework
//           #38 §3.1 (FR-UI-002/006/007/008), Season & Competition Loop #30 FR-SN-033, Injuries &
//           Medical #41 FR-MD-023, Code Standards #20
// Purpose:  Career Home's view-model source: a pure read of the active career, cached on the session's
//           revision so a per-frame poll allocates nothing between operations.

using System;
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
    /// <b>Allocation.</b> A fresh projection allocates (table copy, names). It is rebuilt only when the
    /// active session or its <see cref="CareerSession.Revision"/> changes; otherwise <see cref="Project"/>
    /// returns the cached value. #30 has no other writer than the session (FR-SN-032), so the revision
    /// is a complete change signal.
    /// </para>
    /// </summary>
    public sealed class CareerHomeSource : IViewModelSource<CareerHomeViewModel>
    {
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
            int opponent = 0;
            bool isHome = false;
            int nextRound = calendar.NextRoundIndex;
            uint nextDay = 0;
            if (!calendar.IsSeasonComplete)
            {
                ReadOnlyCollection<Fixture> fixtures = state.Fixtures;
                for (int i = 0; i < fixtures.Count; i++)
                {
                    Fixture f = fixtures[i];
                    if (f.RoundIndex == nextRound && !f.Played && f.Involves(managed))
                    {
                        hasNext = true;
                        isHome = f.HomeClubId == managed;
                        opponent = isHome ? f.AwayClubId : f.HomeClubId;
                        nextDay = calendar.DayOfRound(nextRound);
                        break;
                    }
                }
            }

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
                opponent,
                hasNext ? session.ClubName(opponent) : string.Empty,
                isHome,
                nextRound,
                nextDay,
                CareerProgress.DaysUntilNextMatchday(loop),
                CareerProgress.NextAction(loop),
                session.IsBusy,
                in change,
                lastOpponent,
                session.LastAutosaveError,
                table,
                new ReadOnlyCollection<string>(names));
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
#endregion
