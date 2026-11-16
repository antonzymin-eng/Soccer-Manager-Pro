// File:     src/transfers/TransferBidIssue.cs
// Created:  2026-11-05
// Modified: 2026-11-05
// Author:   —
// Spec:     Transfers #31 FR-TX-030, ERR-031-017; Code Standards #20
// Purpose:  Append-only owner diagnosis of malformed or stale bids, without mutation.

namespace TacticalDirector.Transfers
{
    /// <summary>Caller-contract bid issues; append-only byte ordinals, separate from negotiation outcomes.</summary>
    public enum TransferBidIssue : byte
    {
        /// <summary>The bid passes preflight.</summary>
        None = 0,
        /// <summary>The active transfer window is closed.</summary>
        WindowClosed = 1,
        /// <summary>The counterparty is the managed club.</summary>
        ManagedCounterparty = 2,
        /// <summary>The counterparty is outside the league.</summary>
        UnknownCounterparty = 3,
        /// <summary>The player is outside the current roster universe.</summary>
        UnknownPlayer = 4,
        /// <summary>Ownership disagrees with the direction or named seller.</summary>
        WrongDirection = 5,
        /// <summary>The fee is negative.</summary>
        FeeNegative = 6,
        /// <summary>The wage is negative; #31 sets no upper bound.</summary>
        WageNegative = 7,
        /// <summary>The length is less than one season.</summary>
        LengthTooShort = 8,
    }
}

#region VersionHistory
// | Version | Date | Author | Notes |
// | 1.0 | 2026-11-05 | — | ERR-031-017: owner preflight issue ordinals. |
#endregion
