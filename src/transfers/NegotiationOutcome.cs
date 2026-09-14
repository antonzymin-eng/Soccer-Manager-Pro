// ============================================================================
// File:     src/transfers/NegotiationOutcome.cs
// Created:  2026-09-12
// Modified: 2026-09-14
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #31 §2.3, §3.2 (negotiation outcomes)
// Purpose:  Declares the closed outcome set for #31's reusable negotiation seam.
// ============================================================================

namespace TacticalDirector.Transfers
{
    /// <summary>Result of evaluating one transfer offer against a counterparty.</summary>
    public enum NegotiationOutcome : byte
    {
        /// <summary>The counterparty rejects terms outside the configured synchronous negotiation band.</summary>
        Rejected = 0,

        /// <summary>The counterparty accepts terms meeting its deterministic valuation pivot.</summary>
        Accepted = 1,

        /// <summary>The terms are close enough to value to keep negotiation open without accepting.</summary>
        CounterOffered = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 outcome enum. |
// | 1.1     | 2026-09-14 | —      | CounterOffered becomes the deterministic T0 near-value outcome. |
#endregion
