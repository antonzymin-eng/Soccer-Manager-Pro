// File:     src/client-app/ClubFinancesSource.cs
// Created:  2026-11-05
// Modified: 2026-11-05 (H1: total over every legal #31 wage)
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P8, docs/design/ux-s2-transfers-finances.md
//           §2 (R1, R2, R9, R12), Club Finances #40 FR-FN-026, UI / Client Framework #38 FR-UI-006,
//           Code Standards #20
// Purpose:  The Club Finances screen's view-model source: a pure read of the managed club's #40 view and
//           #31 contracts, cached on the session's revision. SIM-THREAD ONLY.

using System;

using TacticalDirector.ClubFinances;
using TacticalDirector.SeasonSave;
using TacticalDirector.Transfers;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Projects <see cref="ClubFinancesViewModel"/> for the managed club (§5-P8): #40's
    /// <c>FinanceView</c> plus #31's committed spend and contract wages. Reading never mutates; rebuilt only
    /// when the session, its <see cref="CareerSession.Revision"/> or its busy flag changes. Total over every
    /// state #31 accepts: the wage sum saturates instead of overflowing, so projection never throws on it.
    /// </summary>
    public sealed class ClubFinancesSource : IViewModelSource<ClubFinancesViewModel>
    {
        private readonly CareerSessionHolder _holder;
        private CareerSession _cachedSession;
        private int _cachedRevision;
        private bool _cachedBusy;
        private ClubFinancesViewModel _cached;

        /// <summary>Creates the source over the client's career holder.</summary>
        /// <exception cref="ArgumentNullException"><paramref name="holder"/> is null.</exception>
        public ClubFinancesSource(CareerSessionHolder holder)
        {
            _holder = holder ?? throw new ArgumentNullException(nameof(holder));
        }

        /// <inheritdoc />
        public ClubFinancesViewModel Project()
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

        private static ClubFinancesViewModel Build(CareerSession session)
        {
            SeasonLoop loop = session.Loop;
            FinancesViewModel finances = loop.FinanceView(session.ManagedClubId);
            TransfersState transfers = loop.TransfersSnapshot();
            long committed = 0;
            long wages = 0;
            bool saturated = false;
            int count = 0;
            if (transfers != null)
            {
                committed = transfers.CommittedSpendThisWindow;
                Contract[] contracts = transfers.ContractsSnapshot();
                count = contracts.Length;
                for (int i = 0; i < contracts.Length; i++)
                {
                    // #31 accepts any non-negative wage (F6), so the exact sum can exceed long. A throwing
                    // projection would wedge the coordinator's publication (H1); saturate and say so instead.
                    long wage = contracts[i].WagePerPeriod;
                    if (wage > long.MaxValue - wages)
                    {
                        wages = long.MaxValue;
                        saturated = true;
                    }
                    else
                    {
                        wages += wage;
                    }
                }
            }

            return new ClubFinancesViewModel(
                session.ManagedClubName,
                finances.Balance,
                finances.TransferBudget,
                committed,
                finances.WageBudget,
                finances.WageBillAggregate,
                wages,
                count,
                saturated);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-05 | —      | Initial creation (§5-P8): revision-cached Club Finances source. |
// | 1.1     | 2026-11-05 | —      | H1: contract-wage sum saturates at long.MaxValue (flagged) instead of a checked overflow that wedged publication. |
#endregion
