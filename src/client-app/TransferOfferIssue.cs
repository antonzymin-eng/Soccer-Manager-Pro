// File:     src/client-app/TransferOfferIssue.cs
// Created:  2026-11-05
// Modified: 2026-11-05
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P8, Transfers #31 FR-TX-025,
//           UI / Client Framework #38 KD-5 / FR-UI-004, Code Standards #20
// Purpose:  The typed reasons the client refuses a player-composed transfer offer before it reaches #31.

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// Why <see cref="TransferOfferCheck.Check"/> refuses an offer (M2). Each value is a player-reachable
    /// mistake: #31 would throw an F6 caller-contract exception for it, so the client names it instead and the
    /// copy layer (<see cref="CareerScreenText.Issue"/>) supplies the localized sentence.
    /// </summary>
    public enum TransferOfferIssue
    {
        /// <summary>Zero value: the offer is acceptable to submit.</summary>
        None = 0,

        /// <summary>No career, or the career carries no #31 state.</summary>
        TransfersUnavailable = 1,

        /// <summary>The cached transfer window is closed.</summary>
        WindowClosed = 2,

        /// <summary>The market does not list the player.</summary>
        UnknownPlayer = 3,

        /// <summary>A buy of a managed player, or a sale of another club's player.</summary>
        WrongDirection = 4,

        /// <summary>A buy whose counterparty is not the player's club, or a sale to the managed or an unlisted club.</summary>
        WrongCounterparty = 5,

        /// <summary>The fee is negative.</summary>
        FeeNegative = 6,

        /// <summary>The wage is negative or above <see cref="ClientAppConstants.MAX_OFFER_WAGE_PER_PERIOD"/>.</summary>
        WageOutOfRange = 7,

        /// <summary>The contract lasts less than one season.</summary>
        LengthTooShort = 8,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-05 | —      | Initial creation (M2): typed client-side offer refusal reasons. |
#endregion
