// File:     src/client-app/TransferResult.cs
// Created:  2026-11-05
// Modified: 2026-11-05
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

        /// <summary>True when #31 answered (then <see cref="Outcome"/> is meaningful).</summary>
        public readonly bool HasOutcome;

        /// <summary>#31's typed outcome; meaningful only when <see cref="HasOutcome"/>.</summary>
        public readonly TransferSubmissionOutcome Outcome;

        /// <summary>The client refusal; <see cref="TransferOfferIssue.None"/> unless the offer never reached #31.</summary>
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

        /// <summary>A result carrying the client's refusal of <paramref name="offer"/>; it never reached #31.</summary>
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
#endregion
