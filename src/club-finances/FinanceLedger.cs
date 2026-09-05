// ============================================================================
// File:     src/club-finances/FinanceLedger.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   Codex / Anton
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #40 §3.2-§3.3, FR-FN-003/004/012-016 (ledger semantics)
// Purpose:  Implements Club Finances' single between-boundary mutation path and transfer-budget query.
// ============================================================================

using System;

namespace TacticalDirector.ClubFinances
{
    /// <summary>Owns all T0 ledger mutations and the read-only transfer-budget constraint query.</summary>
    public static class FinanceLedger
    {
        /// <summary>Applies one validated cash or wage-liability transaction without changing budget ceilings.</summary>
        /// <param name="finances">Finance state to mutate.</param>
        /// <param name="transaction">Transaction command to apply.</param>
        public static void ApplyTransaction(ref ClubFinances finances, in FinanceTransaction transaction)
        {
            ClubFinances.ValidateCoherence(in finances);
            ValidateTransaction(in transaction);

            bool isWage =
                transaction.LineItem == FinanceLineItem.PlayerWage
                || transaction.LineItem == FinanceLineItem.StaffWage;

            checked
            {
                if (isWage)
                {
                    if (transaction.Kind == FinanceTransactionKind.Debit)
                    {
                        finances.WageBillAggregate += transaction.Amount;
                    }
                    else
                    {
                        if (transaction.Amount > finances.WageBillAggregate)
                        {
                            throw new ArgumentOutOfRangeException(
                                nameof(transaction),
                                transaction.Amount,
                                "Wage credit exceeds WageBillAggregate (F1).");
                        }

                        finances.WageBillAggregate -= transaction.Amount;
                    }
                }
                else
                {
                    long signedAmount =
                        transaction.Kind == FinanceTransactionKind.Debit
                            ? -transaction.Amount
                            : transaction.Amount;

                    finances.Balance += signedAmount;
                }
            }

            ClubFinances.ValidateCoherence(in finances);
        }

        /// <summary>Returns the current season's transfer-spending ceiling without mutating finance state.</summary>
        public static long AvailableTransferBudget(in ClubFinances finances)
        {
            ClubFinances.ValidateCoherence(in finances);
            return finances.TransferBudget;
        }

        private static void ValidateTransaction(in FinanceTransaction transaction)
        {
            if (transaction.Amount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(transaction), transaction.Amount, "FinanceTransaction.Amount must be non-negative (F2).");
            }

            if ((uint)transaction.Kind > (uint)FinanceTransactionKind.Credit)
            {
                throw new ArgumentOutOfRangeException(nameof(transaction), transaction.Kind, "Undefined FinanceTransactionKind (F2).");
            }

            if ((uint)transaction.LineItem > (uint)FinanceLineItem.StaffWage)
            {
                throw new ArgumentOutOfRangeException(nameof(transaction), transaction.LineItem, "Undefined FinanceLineItem (F2).");
            }
        }
    }
}

#region VersionHistory
// Version | Date       | Author        | Change
// --------|------------|---------------|----------------------------------------------
// 1.0     | 2026-09-04 | Codex / Anton | Initial #40 T0 finance ledger and query.
#endregion
