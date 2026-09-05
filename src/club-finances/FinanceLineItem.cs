// ============================================================================
// File:     src/club-finances/FinanceLineItem.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   Codex / Anton
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #40 §2.2 (finance line-item classification)
// Purpose:  Declares the classifications consumed by the single Club Finances ledger mutation path.
// ============================================================================

namespace TacticalDirector.ClubFinances
{
    /// <summary>Classifies whether a transaction moves cash or the ongoing wage liability.</summary>
    public enum FinanceLineItem : byte
    {
        /// <summary>General cash movement.</summary>
        General = 0,

        /// <summary>Transfer-fee cash movement.</summary>
        TransferFee = 1,

        /// <summary>Player-wage liability movement.</summary>
        PlayerWage = 2,

        /// <summary>Staff-wage liability movement.</summary>
        StaffWage = 3
    }
}

#region VersionHistory
// Version | Date       | Author        | Change
// --------|------------|---------------|----------------------------------------------
// 1.0     | 2026-09-04 | Codex / Anton | Initial #40 T0 line-item catalogue.
#endregion
