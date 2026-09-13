// ============================================================================
// File:     src/transfers/TransferCommands.cs
// Created:  2026-09-12
// Modified: 2026-09-12
// Author:   —
// Specs:    Spec #20 §3.5/§3.6.2 (constructor injection, style/docs)
//           Spec #31 §3.3-§3.4, FR-TX-004..010/020..025 (atomic SubmitBid pipeline)
// Purpose:  Implements the explicit manager transfer command with validate-all-first atomic semantics.
// ============================================================================

using System;

using TacticalDirector.ClubFinances;
using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.Transfers
{
    /// <summary>Manager-initiated T0 transfer commands; no autonomous producer or RNG exists.</summary>
    public sealed class TransferCommands
    {
        private readonly ITransferRosterPort _roster;

        /// <summary>Creates the command surface over the specified future-#30 roster port.</summary>
        public TransferCommands(ITransferRosterPort roster)
        {
            _roster = roster ?? throw new ArgumentNullException(nameof(roster));
        }

        /// <summary>
        /// Evaluates and, when accepted, atomically commits one buy or sell after all fallible gates pass.
        /// Rejection is a normal no-mutation result; invalid state/terms fail loud.
        /// </summary>
        public NegotiationOutcome SubmitBid(
            in Offer offer,
            uint worldDay,
            ref ClubFinances finances,
            TransfersState state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state));
            }

            ValidateOffer(in offer, state.ManagedClubId);
            if (!state.ActiveWindow.IsWindowOpen(worldDay))
            {
                throw new InvalidOperationException("Transfer command is outside the active transfer window (F4).");
            }

            if (!_roster.TryGetPlayer(offer.PlayerId, out PlayerRecord player)
                || player.PlayerId != offer.PlayerId)
            {
                throw new ArgumentOutOfRangeException(nameof(offer), offer.PlayerId, "PlayerId is outside the current #27 club universe (F6).");
            }

            int fromClubId = offer.IsBuy ? offer.CounterpartyClubId : state.ManagedClubId;
            int toClubId = offer.IsBuy ? state.ManagedClubId : offer.CounterpartyClubId;
            int ownerClubId = offer.PlayerId / PlayerDatabaseConstants.CLUB_SQUAD_SIZE;
            if (ownerClubId != fromClubId)
            {
                throw new InvalidOperationException("Offer direction does not match the player's current club ownership (F6).");
            }

            long counterpartyValue = PlayerValuation.ValuePlayerPermille(in player.Attributes, player.Age);
            NegotiationOutcome outcome = NegotiationEngine.EvaluateOffer(in offer, counterpartyValue);
            if (outcome != NegotiationOutcome.Accepted)
            {
                return outcome;
            }

            FinanceTransaction transaction = new FinanceTransaction(
                offer.IsBuy ? FinanceTransactionKind.Debit : FinanceTransactionKind.Credit,
                FinanceLineItem.TransferFee,
                offer.Fee);

            // Validate the canonical #40 mutation on a copy before any real #31/#40/#30 state changes.
            ClubFinances stagedFinances = finances;
            FinanceLedger.ApplyTransaction(ref stagedFinances, in transaction);

            if (offer.IsBuy)
            {
                long budget = FinanceLedger.AvailableTransferBudget(in finances);
                long committed = state.CommittedSpendThisWindow;
                if (committed < 0 || committed > budget || offer.Fee > budget - committed)
                {
                    throw new InvalidOperationException("Accepted buy exceeds AvailableTransferBudget minus committed spend (F1).");
                }
            }
            else if (!state.TryGetContract(offer.PlayerId, out Contract existingContract))
            {
                throw new InvalidOperationException("Cannot sell a managed player without an active #31 contract.");
            }
            else
            {
                Contract.Validate(in existingContract);
            }

            if (!_roster.TryPreviewRosterCommit(fromClubId, toClubId, offer.PlayerId, out int newPlayerId))
            {
                throw new InvalidOperationException("Destination squad has no free local index (F5).");
            }

            ValidatePreviewId(newPlayerId, toClubId);
            if (offer.IsBuy && state.TryGetContract(newPlayerId, out Contract duplicateContract))
            {
                Contract.Validate(in duplicateContract);
                throw new InvalidOperationException("Destination PlayerId already has a managed contract.");
            }

            if (offer.IsBuy)
            {
                CommitBuy(in offer, fromClubId, toClubId, newPlayerId, ref finances, state, in transaction);
            }
            else
            {
                CommitSell(in offer, fromClubId, toClubId, newPlayerId, ref finances, state, in transaction);
            }

            return NegotiationOutcome.Accepted;
        }

        private void CommitBuy(
            in Offer offer,
            int fromClubId,
            int toClubId,
            int previewPlayerId,
            ref ClubFinances finances,
            TransfersState state,
            in FinanceTransaction transaction)
        {
            FinanceLedger.ApplyTransaction(ref finances, in transaction);
            state.AddCommittedSpend(offer.Fee);
            int committedPlayerId = _roster.RequestRosterCommit(fromClubId, toClubId, offer.PlayerId);
            RequirePreviewMatch(previewPlayerId, committedPlayerId);

            Contract contract = new Contract(committedPlayerId, offer.WagePerPeriod, offer.LengthSeasons);
            state.InsertContract(in contract);
        }

        private void CommitSell(
            in Offer offer,
            int fromClubId,
            int toClubId,
            int previewPlayerId,
            ref ClubFinances finances,
            TransfersState state,
            in FinanceTransaction transaction)
        {
            // FR-TX-023: remove before the infallible re-key so no old-id contract can survive the move.
            state.RemoveContract(offer.PlayerId);
            FinanceLedger.ApplyTransaction(ref finances, in transaction);
            int committedPlayerId = _roster.RequestRosterCommit(fromClubId, toClubId, offer.PlayerId);
            RequirePreviewMatch(previewPlayerId, committedPlayerId);
        }

        private static void ValidateOffer(in Offer offer, int managedClubId)
        {
            if (offer.PlayerId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offer), offer.PlayerId, "PlayerId must be non-negative (F6).");
            }

            if (offer.CounterpartyClubId < 0 || offer.CounterpartyClubId == managedClubId)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offer),
                    offer.CounterpartyClubId,
                    "CounterpartyClubId must name a different non-negative club (F6).");
            }

            if (offer.Fee < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offer), offer.Fee, "Fee must be non-negative (F6).");
            }

            if (offer.WagePerPeriod < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offer), offer.WagePerPeriod, "WagePerPeriod must be non-negative (F6).");
            }

            if (offer.LengthSeasons <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offer), offer.LengthSeasons, "LengthSeasons must be positive (F6/F7).");
            }
        }

        private static void ValidatePreviewId(int newPlayerId, int toClubId)
        {
            if (newPlayerId < 0
                || newPlayerId / PlayerDatabaseConstants.CLUB_SQUAD_SIZE != toClubId)
            {
                throw new InvalidOperationException("Roster preview returned a PlayerId outside the destination club's deterministic id range.");
            }
        }

        private static void RequirePreviewMatch(int previewPlayerId, int committedPlayerId)
        {
            if (committedPlayerId != previewPlayerId)
            {
                throw new InvalidOperationException("Roster commit violated its successful preview contract.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 atomic SubmitBid command pipeline. |
#endregion
