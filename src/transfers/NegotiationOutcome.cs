// ============================================================================
// File:     src/transfers/NegotiationOutcome.cs
// Created:  2026-09-12
// Modified: 2026-09-14
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #31 §2.2, §3.2-§3.3 (negotiation + command outcomes)
// Purpose:  Declares the closed negotiation and transfer-submission outcome sets for #31 T0.
// ============================================================================

namespace TacticalDirector.Transfers
{
    /// <summary>Result of evaluating one offer against a counterparty valuation.</summary>
    public enum NegotiationOutcome : byte
    {
        /// <summary>The counterparty rejects terms outside the configured synchronous negotiation band.</summary>
        Rejected = 0,

        /// <summary>The counterparty accepts terms meeting its deterministic valuation pivot.</summary>
        Accepted = 1,

        /// <summary>The terms are close enough to value to keep negotiation open without accepting.</summary>
        CounterOffered = 2
    }

    /// <summary>
    /// Result of submitting one manager transfer command. Ordinary player-reachable resource/capacity
    /// failures are represented here rather than thrown; malformed/invariant-breaking inputs still fail loud.
    /// </summary>
    public enum TransferSubmissionOutcome : byte
    {
        /// <summary>The counterparty rejected the offered terms.</summary>
        Rejected = 0,

        /// <summary>The offer was accepted and the transfer committed.</summary>
        Accepted = 1,

        /// <summary>The counterparty kept negotiation open without committing a transfer.</summary>
        CounterOffered = 2,

        /// <summary>The managed club cannot fund the accepted fee within its remaining transfer ceiling.</summary>
        InsufficientBudget = 3,

        /// <summary>The destination squad has no free deterministic local index.</summary>
        SquadFull = 4
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 outcome enum. |
// | 1.1     | 2026-09-14 | —      | CounterOffered becomes the deterministic T0 near-value outcome. |
// | 1.2     | 2026-09-14 | —      | Add TransferSubmissionOutcome so player-reachable budget/full-squad results are typed, not exceptions. |
#endregion
