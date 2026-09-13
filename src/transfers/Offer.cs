// ============================================================================
// File:     src/transfers/Offer.cs
// Created:  2026-09-12
// Modified: 2026-09-12
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #31 §2.3, §3.3 (counterparty-generic offer seam)
// Purpose:  Declares the immutable manager-initiated transfer offer consumed by #31.
// ============================================================================

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
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 offer value. |
#endregion
