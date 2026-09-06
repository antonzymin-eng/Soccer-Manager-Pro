// ============================================================================
// File:     src/club-finances/FinancesViewModel.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   Codex / Anton
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #40 FR-FN-026, KD-8 (read-only finance observer)
// Purpose:  Declares the value-copy observer surface that a future client may read without mutating state.
// ============================================================================

namespace TacticalDirector.ClubFinances
{
    /// <summary>Read-only value-copy view of the four Stage-2 finance fields exposed to the client.</summary>
    public readonly struct FinancesViewModel
    {
        /// <summary>Signed cash balance.</summary>
        public readonly long Balance;

        /// <summary>Current transfer-spending ceiling.</summary>
        public readonly long TransferBudget;

        /// <summary>Current wage-spending ceiling.</summary>
        public readonly long WageBudget;

        /// <summary>Current aggregate wage liability.</summary>
        public readonly long WageBillAggregate;

        private FinancesViewModel(
            long balance,
            long transferBudget,
            long wageBudget,
            long wageBillAggregate)
        {
            Balance = balance;
            TransferBudget = transferBudget;
            WageBudget = wageBudget;
            WageBillAggregate = wageBillAggregate;
        }

        /// <summary>Copies observer fields from a coherent finance state.</summary>
        /// <param name="finances">Finance state to observe.</param>
        /// <returns>A detached immutable value copy.</returns>
        public static FinancesViewModel From(in ClubFinances finances)
        {
            ClubFinances.ValidateCoherence(in finances);
            return new FinancesViewModel(
                finances.Balance,
                finances.TransferBudget,
                finances.WageBudget,
                finances.WageBillAggregate);
        }
    }
}

#region VersionHistory
// Version | Date       | Author        | Change
// --------|------------|---------------|----------------------------------------------
// 1.0     | 2026-09-04 | Codex / Anton | Initial #40 T0 observer surface.
#endregion
