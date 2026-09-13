// ============================================================================
// File:     src/transfers/NegotiationOutcome.cs
// Created:  2026-09-12
// Modified: 2026-09-12
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
        /// <summary>The counterparty rejects the terms.</summary>
        Rejected = 0,

        /// <summary>The counterparty accepts the terms.</summary>
        Accepted = 1,

        /// <summary>Reserved for deep-tier multi-step negotiation; never emitted by T0.</summary>
        CounterOffered = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 outcome enum. |
#endregion
