// ============================================================================
// File:     src/transfers/NegotiationEngine.cs
// Created:  2026-09-12
// Modified: 2026-09-12
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #31 §3.2, FR-TX-003/010 (draw-free reusable offer evaluation)
// Purpose:  Evaluates manager-initiated offers against deterministic counterparty valuation.
// ============================================================================

using System;

namespace TacticalDirector.Transfers
{
    /// <summary>Counterparty-generic synchronous negotiation rules for the minimal transfer tier.</summary>
    public static class NegotiationEngine
    {
        /// <summary>Accepts a buy at/above value and a sell at/below value; the boundary is inclusive.</summary>
        public static NegotiationOutcome EvaluateOffer(in Offer offer, long counterpartyValuation)
        {
            if (offer.Fee < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offer), offer.Fee, "Offer fee must be non-negative (F6).");
            }

            if (counterpartyValuation < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(counterpartyValuation),
                    counterpartyValuation,
                    "Counterparty valuation must be non-negative.");
            }

            bool accepted = offer.IsBuy
                ? offer.Fee >= counterpartyValuation
                : offer.Fee <= counterpartyValuation;
            return accepted ? NegotiationOutcome.Accepted : NegotiationOutcome.Rejected;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 accept/reject negotiation rule. |
#endregion
