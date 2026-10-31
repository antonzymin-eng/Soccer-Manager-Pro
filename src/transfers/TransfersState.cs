// ============================================================================
// File:     src/transfers/TransfersState.cs
// Created:  2026-09-12
// Modified: 2026-10-20 (AR round 3 — v1.6)
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #31 §2.3, §3.3-§3.4 (managed contracts + season-scoped transfer state)
// Purpose:  Owns #31 state without duplicating #40 cash or directly mutating #27 roster data.
// ============================================================================

using System;
using System.Collections.Generic;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.Transfers
{
    /// <summary>#31-owned state for the managed club: contracts plus window/spend state.</summary>
    public sealed class TransfersState
    {
        private readonly SortedDictionary<int, Contract> _contracts;
        private ClubTransferState _clubState;

        /// <summary>Managed club whose contracts and command state this instance owns.</summary>
        public int ManagedClubId { get; }

        /// <summary>Current accepted-buy spend accumulated against the static season ceiling.</summary>
        public long CommittedSpendThisWindow => _clubState.CommittedSpendThisWindow;

        /// <summary>Current #31-owned inclusive transfer window.</summary>
        public TransferWindow ActiveWindow => _clubState.ActiveWindow;

        /// <summary>Number of managed-player contracts currently held.</summary>
        public int ContractCount => _contracts.Count;

        /// <summary>Creates empty T0 transfer state for one managed club and active window.</summary>
        public TransfersState(int managedClubId, in TransferWindow activeWindow)
        {
            if (managedClubId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(managedClubId), managedClubId, "ManagedClubId must be non-negative.");
            }

            ManagedClubId = managedClubId;
            _clubState = new ClubTransferState(0, in activeWindow);
            _contracts = new SortedDictionary<int, Contract>();
        }

        /// <summary>Returns a managed contract by player id when one exists.</summary>
        public bool TryGetContract(int playerId, out Contract contract)
        {
            return _contracts.TryGetValue(playerId, out contract);
        }

        /// <summary>
        /// Inserts one validated managed-player contract and fails on duplicate/default records, and when
        /// the state already holds <see cref="PlayerDatabaseConstants.CLUB_SQUAD_SIZE"/> contracts (a squad
        /// cannot hold more, and <see cref="TransfersSaveCodec.Decode"/> refuses a larger block, so the
        /// live state must never be able to encode one).
        /// <para>
        /// Membership in the managed club is NOT checked here: since ERR-031-005 PlayerIds are stable
        /// across transfers and post-genesis ids come from #28's monotonic cursor, so an id no longer
        /// encodes its club. The roster is the only membership authority — #31's command checks it
        /// through <see cref="ITransferRosterPort"/>, and the season root checks a restored set
        /// against the roster it rides with.
        /// </para>
        /// </summary>
        internal void InsertContract(in Contract contract)
        {
            Contract.Validate(in contract);
            if (_contracts.ContainsKey(contract.PlayerId))
            {
                throw new InvalidOperationException("A contract already exists for PlayerId " + contract.PlayerId + ".");
            }

            if (_contracts.Count >= PlayerDatabaseConstants.CLUB_SQUAD_SIZE)
            {
                throw new InvalidOperationException(
                    "The managed club already holds " + _contracts.Count + " contracts; a squad holds at most " +
                    PlayerDatabaseConstants.CLUB_SQUAD_SIZE + " players.");
            }

            _contracts.Add(contract.PlayerId, contract);
        }

        /// <summary>Removes a managed-player contract and fails if no such contract exists.</summary>
        internal void RemoveContract(int playerId)
        {
            if (!_contracts.Remove(playerId))
            {
                throw new InvalidOperationException("No contract exists for PlayerId " + playerId + ".");
            }
        }

        /// <summary>Returns contracts in canonical PlayerId order (the T1 codec's write order).</summary>
        public Contract[] ContractsSnapshot()
        {
            Contract[] snapshot = new Contract[_contracts.Count];
            int index = 0;
            foreach (KeyValuePair<int, Contract> pair in _contracts)
            {
                snapshot[index++] = pair.Value;
            }

            return snapshot;
        }

        /// <summary>
        /// Rebuilds persisted state (Spec #31 §4.4 / FR-TX-027): the T1 decode path and
        /// <see cref="Clone"/>. Every contract passes the same <see cref="InsertContract"/> validation a live
        /// insert does, so a decoded block can never hold a contract a running career could not.
        /// </summary>
        internal static TransfersState Restore(
            int managedClubId, long committedSpendThisWindow, in TransferWindow activeWindow, Contract[] contracts)
        {
            if (contracts == null)
            {
                throw new ArgumentNullException(nameof(contracts));
            }

            var state = new TransfersState(managedClubId, in activeWindow);
            state._clubState = new ClubTransferState(committedSpendThisWindow, in activeWindow);
            for (int i = 0; i < contracts.Length; i++)
            {
                state.InsertContract(in contracts[i]);
            }

            return state;
        }

        /// <summary>
        /// A deep copy, so a composition root can hold state no outside caller also holds a write
        /// reference to (the <c>ProgressionEngine.FromBlocks</c> "copied, not borrowed" rule).
        /// </summary>
        public TransfersState Clone()
        {
            TransferWindow window = _clubState.ActiveWindow;
            return Restore(ManagedClubId, _clubState.CommittedSpendThisWindow, in window, ContractsSnapshot());
        }

        /// <summary>
        /// New-career genesis (FR-TX-028 / §3.8): the managed club's state with one contract per rostered
        /// player at the <c>[GT]</c> genesis terms (<see cref="TransfersConstants.DefaultWagePerPeriod"/>,
        /// <see cref="TransfersConstants.DefaultContractSeasons"/>) and zero committed spend. Called by #30's
        /// new-game composition ONLY — a load decodes the persisted block and never seeds.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="managedPlayerIds"/> is null.</exception>
        public static TransfersState CreateAtGenesis(
            int managedClubId, in TransferWindow window, IReadOnlyList<int> managedPlayerIds)
        {
            if (managedPlayerIds == null)
            {
                throw new ArgumentNullException(nameof(managedPlayerIds));
            }

            var state = new TransfersState(managedClubId, in window);
            for (int i = 0; i < managedPlayerIds.Count; i++)
            {
                state.InsertContract(new Contract(
                    managedPlayerIds[i], TransfersConstants.DefaultWagePerPeriod, TransfersConstants.DefaultContractSeasons));
            }

            return state;
        }

        /// <summary>
        /// The season boundary (FR-TX-028 / §3.7, G14), as a NEW state — pure, so #30 can stage it before
        /// its roll's one fallible write and install it after. Every carried contract ages one season and a
        /// contract that would reach 0 is removed (F7: never stored as 0); each retiree's contract leaves
        /// with him; each player arriving at the managed club through #28's regen is seeded the genesis
        /// terms; committed spend resets to 0; the window becomes <paramref name="nextWindow"/>.
        /// </summary>
        /// <param name="retiredPlayerIds">#28's retirees this boundary (any club; only managed contracts exist).</param>
        /// <param name="arrivingManagedPlayerIds">#28's regens joining the MANAGED club.</param>
        /// <param name="nextWindow">The next season's summer window (#30 derives it).</param>
        /// <exception cref="ArgumentNullException">A list is null.</exception>
        /// <exception cref="ArgumentException">An arrival already holds a contract (even one expiring this
        /// boundary), is among <paramref name="retiredPlayerIds"/>, or is listed twice; or the surviving aged
        /// contracts plus the arrivals would exceed <see cref="PlayerDatabaseConstants.CLUB_SQUAD_SIZE"/> — a
        /// regen is a new id (#28) and the squad is capped, so each is an integration defect, never silently
        /// re-seeded or surfaced later as an <see cref="InvalidOperationException"/> from the insert (AR round 3).</exception>
        public TransfersState PrepareSeasonBoundary(
            IReadOnlyList<int> retiredPlayerIds, IReadOnlyList<int> arrivingManagedPlayerIds, in TransferWindow nextWindow)
        {
            if (retiredPlayerIds == null)
            {
                throw new ArgumentNullException(nameof(retiredPlayerIds));
            }

            if (arrivingManagedPlayerIds == null)
            {
                throw new ArgumentNullException(nameof(arrivingManagedPlayerIds));
            }

            // Checked against the SOURCE set before anything is built: an arrival that holds a contract
            // expiring this boundary, or that retires in the same boundary, would otherwise vanish from the
            // aged set and be re-seeded without a trace.
            for (int i = 0; i < arrivingManagedPlayerIds.Count; i++)
            {
                int arriving = arrivingManagedPlayerIds[i];
                if (_contracts.ContainsKey(arriving) || Contains(retiredPlayerIds, arriving))
                {
                    throw new ArgumentException(
                        "Arriving player " + arriving + " already holds a managed contract or retires this boundary.",
                        nameof(arrivingManagedPlayerIds));
                }

                for (int j = 0; j < i; j++)
                {
                    if (arrivingManagedPlayerIds[j] == arriving)
                    {
                        throw new ArgumentException(
                            "Arriving player " + arriving + " is listed more than once.", nameof(arrivingManagedPlayerIds));
                    }
                }
            }

            int surviving = 0;
            foreach (KeyValuePair<int, Contract> pair in _contracts)
            {
                if (!Contains(retiredPlayerIds, pair.Key) && pair.Value.LengthSeasons > 1)
                {
                    surviving++;
                }
            }

            if (surviving + arrivingManagedPlayerIds.Count > PlayerDatabaseConstants.CLUB_SQUAD_SIZE)
            {
                throw new ArgumentException(
                    "The aged contracts (" + surviving + ") plus the arrivals (" + arrivingManagedPlayerIds.Count
                    + ") would exceed the squad cap of " + PlayerDatabaseConstants.CLUB_SQUAD_SIZE + ".",
                    nameof(arrivingManagedPlayerIds));
            }

            var next = new TransfersState(ManagedClubId, in nextWindow);
            foreach (KeyValuePair<int, Contract> pair in _contracts)
            {
                if (Contains(retiredPlayerIds, pair.Key) || pair.Value.LengthSeasons <= 1)
                {
                    continue;
                }

                next.InsertContract(new Contract(pair.Key, pair.Value.WagePerPeriod, pair.Value.LengthSeasons - 1));
            }

            for (int i = 0; i < arrivingManagedPlayerIds.Count; i++)
            {
                next.InsertContract(new Contract(
                    arrivingManagedPlayerIds[i], TransfersConstants.DefaultWagePerPeriod, TransfersConstants.DefaultContractSeasons));
            }

            return next;
        }

        private static bool Contains(IReadOnlyList<int> ids, int id)
        {
            for (int i = 0; i < ids.Count; i++)
            {
                if (ids[i] == id)
                {
                    return true;
                }
            }

            return false;
        }

        internal void AddCommittedSpend(long amount)
        {
            if (amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Committed spend increment must be non-negative.");
            }

            checked
            {
                _clubState.CommittedSpendThisWindow += amount;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 managed contract/window/spend state. |
// | 1.1     | 2026-10-18 | —      | #31 T1: internal Restore (decode path) and public Clone (copy-on-compose). |
// | 1.2     | 2026-10-19 | —      | ERR-031-005: stable PlayerIds — the packed-id managed-membership check and the OnPlayerRekeyed handler are deleted (an id no longer encodes its club, and nothing is ever re-keyed). |
// | 1.3     | 2026-10-20 | —      | AR round 1 (ERR-031-006 cycle) M1: InsertContract refuses a CLUB_SQUAD_SIZE+1-th contract, so the live state can never encode a block Decode refuses. |
// | 1.4     | 2026-10-20 | —      | #31 T2: CreateAtGenesis (FR-TX-028 / §3.8 genesis seeding at the [GT] terms) and PrepareSeasonBoundary (§3.7 aging + G14 retiree/regen lockstep + spend reset + next window, as a NEW state for #30's staged roll). |
// | 1.5     | 2026-10-20 | —      | AR round 2 (L4/L2): PrepareSeasonBoundary refuses an arrival that holds a contract (incl. one expiring now) or is retired, checked against the source before building; InsertContract/RemoveContract are internal (the only production writers are #31's). |
// | 1.6     | 2026-10-20 | —      | AR round 3 T-L8 (ERR-031-013): PrepareSeasonBoundary also refuses a duplicated arrival and aged contracts plus arrivals above CLUB_SQUAD_SIZE with ArgumentException in the pre-build guard (previously InvalidOperationException from InsertContract). |
#endregion
