// ============================================================================
// File:     src/club-finances/FinanceTransactionKind.cs
// Created:  2026-09-04
// Modified: 2026-09-08
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #40 §2.2 (finance transaction direction)
// Purpose:  Declares the direction of a finance transaction; Amount carries magnitude only.
// ============================================================================

namespace TacticalDirector.ClubFinances
{
    /// <summary>Direction applied to a non-negative <see cref="FinanceTransaction.Amount"/> magnitude.</summary>
    public enum FinanceTransactionKind : byte
    {
        /// <summary>Consumes cash or increases a wage liability, depending on line item.</summary>
        Debit = 0,

        /// <summary>Adds cash or decreases a wage liability, depending on line item.</summary>
        Credit = 1
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-04 | —      | Initial #40 T0 transaction-direction enum. |
// | 1.1     | 2026-09-06 | —      | Header author attribution corrected to automated-agent placeholder. |
// | 1.3     | 2026-09-08 | —      | Corrected the version-history table to the required parseable pipe-row format. |
#endregion
