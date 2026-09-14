// ============================================================================
// File:     src/transfers/TransferCommands.cs
// Created:  2026-09-12
// Modified: 2026-09-14
// Author:   —
// Specs:    Spec #20 §3.5/§3.6.2 (constructor injection, style/docs)
//           Spec #31 §3.1-§3.4, FR-TX-001..010/020..025 (atomic SubmitBid pipeline)
// Purpose:  Implements the explicit manager transfer command with validate-all-first atomic semantics.
// ============================================================================

using System;

using TacticalDirector.ClubFinances;
using TacticalDirector.PlayerDatabase;

using ClubFinancesState = TacticalDirector.ClubFinances.ClubFinances;

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
        /// Player-reachable negotiation/budget/capacity results are typed no-mutation outcomes; malformed or
        /// invariant-breaking state still fails loud.
        /// </summary>
        public TransferSubmissionOutcome SubmitBid(
            in Offer offer,
            uint worldDay,
            ref ClubFinancesState finances,
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

            int rawSamePositionCount = _roster.CountPlayersAtPosition(offer.CounterpartyClubId, player.Position);
            if (rawSamePositionCount < 0 || rawSamePositionCount > PlayerDatabaseConstants.CLUB_SQUAD_SIZE)
            {
                throw new InvalidOperationException("Roster port returned an invalid positional-stock count.");
            }

            // Compare prospective counterparty stock on the same basis in both directions: exclude the player
            // under negotiation. A selling club's current count includes him; a buying club's current count does not.
            int samePositionCountExcludingPlayer = rawSamePositionCount;
            if (offer.IsBuy)
            {
                if (rawSamePositionCount == 0)
                {
                    throw new InvalidOperationException("Roster port positional stock is inconsistent with player ownership.");
                }

                samePositionCountExcludingPlayer--;
            }

            long counterpartyValue = PlayerValuation.CounterpartyValue(
                in player.Attributes,
                player.Age,
                samePositionCountExcludingPlayer);
            NegotiationOutcome negotiation = NegotiationEngine.EvaluateOffer(in offer, counterpartyValue);
            if (negotiation != NegotiationOutcome.Accepted)
            {
                return ToSubmissionOutcome(negotiation);
            }

            FinanceTransaction transaction = new FinanceTransaction(
                offer.IsBuy ? FinanceTransactionKind.Debit : FinanceTransactionKind.Credit,
                FinanceLineItem.TransferFee,
                offer.Fee);

            // Validate the canonical #40 mutation on a copy before any real #31/#40/#30 state changes.
            ClubFinancesState stagedFinances = finances;
            FinanceLedger.ApplyTransaction(ref stagedFinances, in transaction);

            if (offer.IsBuy)
            {
                long budget = FinanceLedger.AvailableTransferBudget(in finances);
                long committed = state.CommittedSpendThisWindow;
                if (committed < 0 || committed > budget)
                {
                    throw new InvalidOperationException("Committed transfer spend is outside the #40 budget invariant.");
                }

                if (offer.Fee > budget - committed)
                {
                    return TransferSubmissionOutcome.InsufficientBudget;
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
                return TransferSubmissionOutcome.SquadFull;
            }

            ValidatePreviewId(newPlayerId, toClubId);
            if (offer.IsBuy && state.TryGetContract(newPlayerId, out Contract duplicateContract))
            {
                Contract.Validate(in duplicateContract);
                throw new InvalidOperationException("Destination PlayerId already has a managed contract.");
            }

            if (offer.IsBuy)
            {
                CommitBuy(
                    in offer,
                    fromClubId,
                    toClubId,
                    newPlayerId,
                    ref finances,
                    state,
                    in stagedFinances);
            }
            else
            {
                CommitSell(
                    in offer,
                    fromClubId,
                    toClubId,
                    newPlayerId,
                    ref finances,
                    state,
                    in stagedFinances);
            }

            return TransferSubmissionOutcome.Accepted;
        }

        private void CommitBuy(
            in Offer offer,
            int fromClubId,
            int toClubId,
            int previewPlayerId,
            ref ClubFinancesState finances,
            TransfersState state,
            in ClubFinancesState stagedFinances)
        {
            int committedPlayerId = _roster.RequestRosterCommit(fromClubId, toClubId, offer.PlayerId);
            RequirePreviewMatch(previewPlayerId, committedPlayerId);

            finances = stagedFinances;
            state.AddCommittedSpend(offer.Fee);
            Contract contract = new Contract(previewPlayerId, offer.WagePerPeriod, offer.LengthSeasons);
            state.InsertContract(in contract);
        }

        private void CommitSell(
            in Offer offer,
            int fromClubId,
            int toClubId,
            int previewPlayerId,
            ref ClubFinancesState finances,
            TransfersState state,
            in ClubFinancesState stagedFinances)
        {
            // Commit the preflighted roster move first. #31's managed↔external re-key hook is a specified no-op,
            // so retaining the old contract until this infallible port call succeeds cannot double-handle it.
            int committedPlayerId = _roster.RequestRosterCommit(fromClubId, toClubId, offer.PlayerId);
            RequirePreviewMatch(previewPlayerId, committedPlayerId);

            state.RemoveContract(offer.PlayerId);
            finances = stagedFinances;
        }

        private static TransferSubmissionOutcome ToSubmissionOutcome(NegotiationOutcome negotiation)
        {
            switch (negotiation)
            {
                case NegotiationOutcome.Rejected:
                    return TransferSubmissionOutcome.Rejected;
                case NegotiationOutcome.CounterOffered:
                    return TransferSubmissionOutcome.CounterOffered;
                case NegotiationOutcome.Accepted:
                    return TransferSubmissionOutcome.Accepted;
                default:
                    throw new ArgumentOutOfRangeException(nameof(negotiation), negotiation, "Unknown negotiation outcome.");
            }
        }

        private static void ValidateOffer(in Offer offer, int managedClubId)
        {
            Offer.ValidateTerms(in offer);
            if (offer.CounterpartyClubId == managedClubId)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offer),
                    offer.CounterpartyClubId,
                    "CounterpartyClubId must name a different club (F6).");
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
// | 1.1     | 2026-09-14 | —      | Fix finance type alias; share offer validation; apply always-on counterparty positional need. |
// | 1.2     | 2026-09-14 | —      | Type normal budget/full-squad outcomes; exclude negotiated player from need; commit staged finance and move port check ahead of local mutation. |
#endregion
