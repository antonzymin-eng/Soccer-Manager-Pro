// ============================================================================
// File:     src/transfers/Offer.cs
// Created:  2026-09-12
// Modified: 2026-09-14
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #31 §2.3, §3.2-§3.3 (counterparty-generic offer seam)
// Purpose:  Declares the immutable manager-initiated transfer offer consumed by #31.
// ============================================================================

using System;

namespace TacticalDirector.Transfers
{
    /// <summary>Immutable bid/listing terms for one synchronous minimal-tier negotiation.</summary>
    public readonly struct Offer
    {
        /// <summary>Current player identifier before any accepted roster re-key.</summary>
        public readonly int PlayerId;

        /// <summary>The non-managed counterparty club participating in the offer.</summary>
        public readonly int CounterpartyClubId;

        /// <summary>Non-negative transfer-fee magnitude.</summary>
        public readonly long Fee;

        /// <summary>Non-negative agreed wage recorded in the resulting buy contract.</summary>
        public readonly long WagePerPeriod;

        /// <summary>Strictly positive agreed contract length.</summary>
        public readonly int LengthSeasons;

        /// <summary>True when the managed club buys; false when it sells.</summary>
        public readonly bool IsBuy;

        /// <summary>Creates offer terms; consuming seams perform fail-loud validation.</summary>
        public Offer(
            int playerId,
            int counterpartyClubId,
            long fee,
            long wagePerPeriod,
            int lengthSeasons,
            bool isBuy)
        {
            PlayerId = playerId;
            CounterpartyClubId = counterpartyClubId;
            Fee = fee;
            WagePerPeriod = wagePerPeriod;
            LengthSeasons = lengthSeasons;
            IsBuy = isBuy;
        }

        /// <summary>Fail-loud shared validation used by every consuming offer seam.</summary>
        internal static void ValidateTerms(in Offer offer)
        {
            if (offer.PlayerId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offer), offer.PlayerId, "PlayerId must be non-negative (F6).");
            }

            if (offer.CounterpartyClubId < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(offer),
                    offer.CounterpartyClubId,
                    "CounterpartyClubId must be non-negative (F6).");
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
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 offer value. |
// | 1.1     | 2026-09-14 | —      | Codex P2: centralize fail-loud term validation for every consuming seam. |
#endregion
