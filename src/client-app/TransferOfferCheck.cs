// File:     src/client-app/TransferOfferCheck.cs
// Created:  2026-11-05
// Modified: 2026-11-05
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P8, Transfers #31 FR-TX-025 (F4/F6),
//           UI / Client Framework #38 KD-5 / FR-UI-004, Code Standards #20
// Purpose:  Pure, typed pre-check of a manager's offer against the cached Transfers view, so a mistake the
//           player can make never reaches #31 as an exception.

using TacticalDirector.Transfers;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Checks an <see cref="Offer"/> against a <see cref="TransferMarketViewModel"/> (M2). Pure and allocation
    /// free: the Unity page calls it to disable the action and explain why; the coordinator calls the same
    /// method before queueing any work, so the two can never disagree.
    /// </summary>
    public static class TransferOfferCheck
    {
        /// <summary>
        /// Returns <see cref="TransferOfferIssue.None"/> when the offer may be submitted, else the first
        /// failing reason in this order: transfers available, window open, player listed, direction matches
        /// who holds him, counterparty, fee, wage, length. A buy's counterparty is the player's club; a sale's
        /// is a listed club other than the managed one. Wage must lie in
        /// [0, <see cref="ClientAppConstants.MAX_OFFER_WAGE_PER_PERIOD"/>].
        /// </summary>
        public static TransferOfferIssue Check(in TransferMarketViewModel market, in Offer offer)
        {
            if (!market.HasCareer || !market.IsTransfersWired) return TransferOfferIssue.TransfersUnavailable;
            if (!market.IsWindowOpen) return TransferOfferIssue.WindowClosed;
            if (!TryFindPlayer(in market, offer.PlayerId, out TransferPlayerRow player)) return TransferOfferIssue.UnknownPlayer;
            if (player.IsManaged == offer.IsBuy) return TransferOfferIssue.WrongDirection;
            if (offer.IsBuy ? offer.CounterpartyClubId != player.ClubId : !IsOtherListedClub(in market, offer.CounterpartyClubId))
                return TransferOfferIssue.WrongCounterparty;
            if (offer.Fee < 0) return TransferOfferIssue.FeeNegative;
            if (offer.WagePerPeriod < 0 || offer.WagePerPeriod > ClientAppConstants.MAX_OFFER_WAGE_PER_PERIOD)
                return TransferOfferIssue.WageOutOfRange;
            if (offer.LengthSeasons < 1) return TransferOfferIssue.LengthTooShort;
            return TransferOfferIssue.None;
        }

        private static bool TryFindPlayer(in TransferMarketViewModel market, int playerId, out TransferPlayerRow found)
        {
            found = default;
            if (market.Players == null) return false;
            for (int i = 0; i < market.Players.Count; i++)
            {
                if (market.Players[i].PlayerId == playerId)
                {
                    found = market.Players[i];
                    return true;
                }
            }

            return false;
        }

        private static bool IsOtherListedClub(in TransferMarketViewModel market, int clubId)
        {
            if (market.Clubs == null) return false;
            for (int i = 0; i < market.Clubs.Count; i++)
            {
                if (market.Clubs[i].ClubId == clubId) return !market.Clubs[i].IsManaged;
            }

            return false;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-05 | —      | Initial creation (M2): typed offer pre-check (window, player, direction, counterparty, fee, wage, length). |
#endregion
