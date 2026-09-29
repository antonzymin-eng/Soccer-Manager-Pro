// ============================================================================
// File:     src/transfers/NegotiationEngine.cs
// Created:  2026-09-12
// Modified: 2026-09-14
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
        /// <summary>
        /// Resolves accepted / counter-offered / rejected deterministically around counterparty value.
        /// Every malformed term fails loud here because this evaluator is itself a reusable consuming seam.
        /// </summary>
        public static NegotiationOutcome EvaluateOffer(in Offer offer, long counterpartyValuation)
        {
            Offer.ValidateTerms(in offer);
            if (counterpartyValuation < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(counterpartyValuation),
                    counterpartyValuation,
                    "Counterparty valuation must be non-negative.");
            }

            long band = CounterBandAmount(counterpartyValuation);
            if (offer.IsBuy)
            {
                if (offer.Fee >= counterpartyValuation)
                {
                    return NegotiationOutcome.Accepted;
                }

                long counterFloor = counterpartyValuation - band;
                return offer.Fee >= counterFloor
                    ? NegotiationOutcome.CounterOffered
                    : NegotiationOutcome.Rejected;
            }

            if (offer.Fee <= counterpartyValuation)
            {
                return NegotiationOutcome.Accepted;
            }

            long counterCeiling = checked(counterpartyValuation + band);
            return offer.Fee <= counterCeiling
                ? NegotiationOutcome.CounterOffered
                : NegotiationOutcome.Rejected;
        }

        /// <summary>Returns the deterministic inclusive counter-offer half-band in currency units.</summary>
        public static long CounterBandAmount(long counterpartyValuation)
        {
            if (counterpartyValuation < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(counterpartyValuation),
                    counterpartyValuation,
                    "Counterparty valuation must be non-negative.");
            }

            if (counterpartyValuation == 0)
            {
                return 0;
            }

            long quotient = counterpartyValuation / TransfersConstants.PERMILLE_DENOM;
            long remainder = counterpartyValuation % TransfersConstants.PERMILLE_DENOM;
            long band = checked(
                quotient * TransfersConstants.NegotiationCounterBandPermille
                + remainder * TransfersConstants.NegotiationCounterBandPermille / TransfersConstants.PERMILLE_DENOM);
            return Math.Max(1L, band);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 accept/reject negotiation rule. |
// | 1.1     | 2026-09-14 | —      | Codex P2 full-term validation + deterministic counter-offer band. |
#endregion
