// File:     src/client-app/TransferMarketSource.cs
// Created:  2026-11-05
// Modified: 2026-11-05 (H2: club rows carry squad counts)
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P8 (D-P8-1),
//           docs/design/ux-s2-transfers-finances.md §2 (R1–R8), UI / Client Framework #38 §3.1
//           (FR-UI-006) and KD-5, Transfers #31 FR-TX-025, Code Standards #20
// Purpose:  The Transfers screen's view-model source: a pure read of the active career, cached on the
//           session's revision. SIM-THREAD ONLY — the P7b coordinator calls it on its career worker.

using System;
using System.Collections.ObjectModel;

using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;
using TacticalDirector.SeasonSave;
using TacticalDirector.Transfers;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Projects <see cref="TransferMarketViewModel"/> from the career in a <see cref="CareerSessionHolder"/>
    /// (§5-P8). Every value is an owner's read: #28 rosters and ability, #31's detached snapshot and
    /// <c>PlayerValuation.ValuePlayer</c>, #40's ceiling, #41's availability. Reading never mutates.
    /// Rebuilt only when the session, its <see cref="CareerSession.Revision"/> or its busy flag changes,
    /// because #31/#40/#28 have no writer but the session.
    /// </summary>
    public sealed class TransferMarketSource : IViewModelSource<TransferMarketViewModel>
    {
        private readonly CareerSessionHolder _holder;
        private CareerSession _cachedSession;
        private int _cachedRevision;
        private bool _cachedBusy;
        private TransferMarketViewModel _cached;

        /// <summary>Creates the source over the client's career holder.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="holder"/> is null.</exception>
        public TransferMarketSource(CareerSessionHolder holder)
        {
            _holder = holder ?? throw new ArgumentNullException(nameof(holder));
        }

        /// <inheritdoc />
        public TransferMarketViewModel Project()
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

        private static TransferMarketViewModel Build(CareerSession session)
        {
            SeasonLoop loop = session.Loop;
            int managed = session.ManagedClubId;
            int[] clubIds = session.League.ClubIds();
            Array.Sort(clubIds);

            var clubs = new TransferClubRow[clubIds.Length];
            TransfersState transfers = loop.TransfersSnapshot();
            ProgressionEngine progression = loop.Progression;
            if (transfers == null || progression == null)
            {
                for (int c = 0; c < clubIds.Length; c++)
                {
                    clubs[c] = new TransferClubRow(clubIds[c], session.ClubName(clubIds[c]), clubIds[c] == managed, 0);
                }

                return new TransferMarketViewModel(
                    false, managed, loop.CurrentWorldDay, false, 0, 0, 0, 0, 0,
                    PlayerDatabaseConstants.CLUB_SQUAD_SIZE, 0, 0,
                    new ReadOnlyCollection<TransferPlayerRow>(new TransferPlayerRow[0]),
                    new ReadOnlyCollection<TransferClubRow>(clubs));
            }

            PlayerCareerStates career = loop.Career;
            int total = 0;
            var squads = new Squad[clubIds.Length];
            for (int c = 0; c < clubIds.Length; c++)
            {
                squads[c] = progression.SquadFor(clubIds[c]);
                total += squads[c].Count;
                clubs[c] = new TransferClubRow(clubIds[c], session.ClubName(clubIds[c]), clubIds[c] == managed, squads[c].Count);
            }

            var rows = new TransferPlayerRow[total];
            int managedCount = 0;
            int r = 0;
            for (int c = 0; c < clubIds.Length; c++)
            {
                int clubId = clubIds[c];
                bool isManaged = clubId == managed;
                for (int p = 0; p < squads[c].Count; p++)
                {
                    PlayerRecord record = squads[c].GetPlayer(p);
                    Contract contract = default;
                    bool hasContract = isManaged && transfers.TryGetContract(record.PlayerId, out contract);

                    rows[r++] = new TransferPlayerRow(
                        record.PlayerId,
                        clubId,
                        clubs[c].Name,
                        record.FirstName + " " + record.LastName,
                        record.Position,
                        record.Age,
                        progression.LifecycleView(clubId, record.PlayerId).CurrentAbility,
                        career == null || career.IsAvailable(clubId, record.PlayerId),
                        PlayerValuation.ValuePlayer(in record.Attributes, record.Age),
                        isManaged,
                        hasContract,
                        hasContract ? contract.WagePerPeriod : 0,
                        hasContract ? contract.LengthSeasons : 0);
                }

                if (isManaged)
                {
                    managedCount = squads[c].Count;
                }
            }

            TransferWindow window = transfers.ActiveWindow;
            uint day = loop.CurrentWorldDay;
            return new TransferMarketViewModel(
                true,
                managed,
                day,
                window.IsWindowOpen(day),
                window.OpenWorldDay,
                window.CloseWorldDay,
                loop.AvailableTransferBudget(managed),
                transfers.CommittedSpendThisWindow,
                managedCount,
                PlayerDatabaseConstants.CLUB_SQUAD_SIZE,
                TransfersConstants.DefaultWagePerPeriod,
                TransfersConstants.DefaultContractSeasons,
                new ReadOnlyCollection<TransferPlayerRow>(rows),
                new ReadOnlyCollection<TransferClubRow>(clubs));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-05 | —      | Initial creation (§5-P8): revision-cached Transfers source. |
// | 1.1     | 2026-11-05 | —      | H2: each TransferClubRow carries its #28 squad count, read from the same squads as the player rows. |
#endregion
