// File:     src/client-app/TransferResult.cs
// Created:  2026-11-05
// Modified: 2026-11-05 (AR-2: T1 owner diagnosis and budget headroom; ERR-031-017 / ERR-030-059 / ERR-038-006.)
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P8, Transfers #31 FR-TX-025,
//           UI / Client Framework #38 KD-5, Code Standards #20
// Purpose:  The one published result line of a submitted offer: the offer plus either #31's typed outcome or
//           the client's typed refusal, so the page needs no copy of the offer's direction.

using TacticalDirector.Transfers;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// The result of the last <see cref="ClientCareerCoordinator.SubmitTransfer"/> (M3): the submitted
    /// <see cref="Offer"/> and exactly one of #31's <see cref="TransferSubmissionOutcome"/> or a client
    /// <see cref="TransferOfferIssue"/> refusal. A <c>default</c> value has <see cref="HasValue"/> false.
    /// </summary>
    public readonly struct TransferResult
    {
        /// <summary>True when this holds a result (false for <c>default</c>).</summary>
        public readonly bool HasValue;

        /// <summary>The offer that was submitted or refused.</summary>
        public readonly Offer Offer;

        /// <summary>True when SubmitBid returned an outcome (then <see cref="Outcome"/> is meaningful).</summary>
        public readonly bool HasOutcome;

        /// <summary>#31's typed outcome; meaningful only when <see cref="HasOutcome"/>.</summary>
        public readonly TransferSubmissionOutcome Outcome;

        /// <summary>The mapped refusal; <see cref="TransferOfferIssue.None"/> when the mutation seam returned an outcome.</summary>
        public readonly TransferOfferIssue Issue;

        private TransferResult(in Offer offer, bool hasOutcome, TransferSubmissionOutcome outcome, TransferOfferIssue issue)
        {
            HasValue = true;
            Offer = offer;
            HasOutcome = hasOutcome;
            Outcome = outcome;
            Issue = issue;
        }

        /// <summary>A result carrying #31's typed outcome for <paramref name="offer"/>.</summary>
        public static TransferResult FromOutcome(in Offer offer, TransferSubmissionOutcome outcome) =>
            new TransferResult(in offer, true, outcome, TransferOfferIssue.None);

        /// <summary>Maps an owner diagnosis into localized client refusal categories.</summary>
        public static TransferResult FromDiagnosis(in Offer offer, TransferBidIssue issue)
        {
            TransferOfferIssue mapped;
            switch (issue)
            {
                case TransferBidIssue.WindowClosed: mapped = TransferOfferIssue.WindowClosed; break;
                case TransferBidIssue.ManagedCounterparty:
                case TransferBidIssue.UnknownCounterparty: mapped = TransferOfferIssue.WrongCounterparty; break;
                case TransferBidIssue.UnknownPlayer: mapped = TransferOfferIssue.UnknownPlayer; break;
                case TransferBidIssue.WrongDirection: mapped = TransferOfferIssue.WrongDirection; break;
                case TransferBidIssue.FeeNegative: mapped = TransferOfferIssue.FeeNegative; break;
                case TransferBidIssue.WageNegative: mapped = TransferOfferIssue.WageOutOfRange; break;
                case TransferBidIssue.LengthTooShort: mapped = TransferOfferIssue.LengthTooShort; break;
                default: throw new System.ArgumentOutOfRangeException(nameof(issue));
            }
            return FromRefusal(in offer, mapped);
        }

        /// <summary>A refusal of <paramref name="offer"/> before the mutation seam.</summary>
        /// <exception cref="System.ArgumentException"><paramref name="issue"/> is <see cref="TransferOfferIssue.None"/>.</exception>
        public static TransferResult FromRefusal(in Offer offer, TransferOfferIssue issue)
        {
            if (issue == TransferOfferIssue.None) throw new System.ArgumentException("A refusal needs a reason.", nameof(issue));
            return new TransferResult(in offer, false, default, issue);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-05 | —      | Initial creation (M3): submitted offer + #31 outcome or typed client refusal. |
// | 1.1 | 2026-11-05 | — | T1 owner diagnosis and budget headroom; ERR-031-017 / ERR-030-059 / ERR-038-006. |
#endregion
