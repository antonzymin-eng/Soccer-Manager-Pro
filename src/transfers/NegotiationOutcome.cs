// ============================================================================
// File:     src/transfers/NegotiationOutcome.cs
// Created:  2026-09-12
// Modified: 2026-10-20
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

        /// <summary>The destination roster is already at <c>CLUB_SQUAD_SIZE</c>, so it cannot take another player.</summary>
        SquadFull = 4,

        /// <summary>
        /// The releasing club could no longer name a matchday squad without the player (ERR-031-007) — the
        /// manager's sale, or an AI club's sale to the manager. An ordinary no-mutation outcome.
        /// </summary>
        SquadDepleted = 5
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 outcome enum. |
// | 1.1     | 2026-09-14 | —      | CounterOffered becomes the deterministic T0 near-value outcome. |
// | 1.2     | 2026-09-14 | —      | Add TransferSubmissionOutcome so player-reachable budget/full-squad results are typed, not exceptions. |
// | 1.3     | 2026-10-20 | —      | AR round 1 (ERR-031-006 cycle) L1: SquadFull doc reworded to destination-roster-capacity semantics (stable PlayerIds, ERR-031-005; no local index is allocated). |
// | 1.4     | 2026-10-20 | —      | #31 T2 (ERR-031-007): SquadDepleted = 5 — the releasing club would fall below a matchday squad. |
#endregion
