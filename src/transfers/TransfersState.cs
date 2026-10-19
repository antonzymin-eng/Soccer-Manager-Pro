// ============================================================================
// File:     src/transfers/TransfersState.cs
// Created:  2026-09-12
// Modified: 2026-10-18
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

        /// <summary>Inserts one validated managed-player contract and fails on duplicate/default records.</summary>
        public void InsertContract(in Contract contract)
        {
            Contract.Validate(in contract);
            RequireManagedPlayerId(contract.PlayerId, nameof(contract));
            if (_contracts.ContainsKey(contract.PlayerId))
            {
                throw new InvalidOperationException("A contract already exists for PlayerId " + contract.PlayerId + ".");
            }

            _contracts.Add(contract.PlayerId, contract);
        }

        /// <summary>Removes a managed-player contract and fails if no such contract exists.</summary>
        public void RemoveContract(int playerId)
        {
            RequireManagedPlayerId(playerId, nameof(playerId));
            if (!_contracts.Remove(playerId))
            {
                throw new InvalidOperationException("No contract exists for PlayerId " + playerId + ".");
            }
        }

        /// <summary>
        /// Migrates a contract only for an intra-managed re-key; managed↔external transfer re-keys are a no-op
        /// because <see cref="TransferCommands.SubmitBid"/> owns the explicit insert/remove.
        /// </summary>
        public void OnPlayerRekeyed(int oldPlayerId, int newPlayerId)
        {
            bool oldManaged = IsManagedPlayerId(oldPlayerId);
            bool newManaged = IsManagedPlayerId(newPlayerId);
            if (!oldManaged || !newManaged || oldPlayerId == newPlayerId)
            {
                return;
            }

            if (!_contracts.TryGetValue(oldPlayerId, out Contract contract))
            {
                return;
            }

            if (_contracts.ContainsKey(newPlayerId))
            {
                throw new InvalidOperationException("Cannot re-key onto an existing contract PlayerId.");
            }

            _contracts.Remove(oldPlayerId);
            contract.PlayerId = newPlayerId;
            Contract.Validate(in contract);
            _contracts.Add(newPlayerId, contract);
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

        private bool IsManagedPlayerId(int playerId)
        {
            return playerId >= 0
                && playerId / PlayerDatabaseConstants.CLUB_SQUAD_SIZE == ManagedClubId;
        }

        private void RequireManagedPlayerId(int playerId, string parameterName)
        {
            if (!IsManagedPlayerId(playerId))
            {
                throw new ArgumentOutOfRangeException(parameterName, playerId, "Contract PlayerId must belong to the managed club.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 managed contract/window/spend state. |
// | 1.1     | 2026-10-18 | —      | #31 T1: internal Restore (decode path) and public Clone (copy-on-compose). |
#endregion
