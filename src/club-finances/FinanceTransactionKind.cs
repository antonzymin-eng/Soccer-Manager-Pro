// ============================================================================
// File:     src/club-finances/FinanceTransactionKind.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   Codex / Anton
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
// Version | Date       | Author        | Change
// --------|------------|---------------|----------------------------------------------
// 1.0     | 2026-09-04 | Codex / Anton | Initial #40 T0 transaction-direction enum.
#endregion
