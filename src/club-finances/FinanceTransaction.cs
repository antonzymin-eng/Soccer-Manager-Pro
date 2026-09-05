// ============================================================================
// File:     src/club-finances/FinanceTransaction.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   Codex / Anton
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #40 §2.2, §3.2 (ledger transaction value)
// Purpose:  Declares the immutable transaction value accepted by Club Finances' ledger mutation path.
// ============================================================================

namespace TacticalDirector.ClubFinances
{
    /// <summary>Immutable transaction command; direction carries sign and <see cref="Amount"/> is a magnitude.</summary>
    public readonly struct FinanceTransaction
    {
        /// <summary>Debit or credit direction.</summary>
        public readonly FinanceTransactionKind Kind;

        /// <summary>Classification that selects cash versus wage-liability semantics.</summary>
        public readonly FinanceLineItem LineItem;

        /// <summary>Non-negative transaction magnitude.</summary>
        public readonly long Amount;

        /// <summary>Creates a transaction value; validity is enforced by <see cref="FinanceLedger.ApplyTransaction"/>.</summary>
        public FinanceTransaction(FinanceTransactionKind kind, FinanceLineItem lineItem, long amount)
        {
            Kind = kind;
            LineItem = lineItem;
            Amount = amount;
        }
    }
}

#region VersionHistory
// Version | Date       | Author        | Change
// --------|------------|---------------|----------------------------------------------
// 1.0     | 2026-09-04 | Codex / Anton | Initial #40 T0 transaction value.
#endregion
