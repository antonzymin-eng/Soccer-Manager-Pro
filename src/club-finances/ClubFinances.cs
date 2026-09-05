// ============================================================================
// File:     src/club-finances/ClubFinances.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   Codex / Anton
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #40 §2.2 (per-club finance state)
// Purpose:  Declares Club Finances' integer-only per-club financial state and its initial identity.
// ============================================================================

using System;

namespace TacticalDirector.ClubFinances
{
    /// <summary>#40-owned per-club financial state, persisted from T1 onward.</summary>
    public struct ClubFinances
    {
        /// <summary>Signed cash balance; debt is representable.</summary>
        public long Balance;

        /// <summary>Non-negative season transfer-spending ceiling set only by settlement.</summary>
        public long TransferBudget;

        /// <summary>Non-negative season wage-spending ceiling set only by settlement.</summary>
        public long WageBudget;

        /// <summary>Non-negative current wage-liability aggregate.</summary>
        public long WageBillAggregate;

        /// <summary>Deep-tier revenue accumulator; zero and untouched at T0.</summary>
        public long SeasonRevenueAccrued;

        /// <summary>Deep-tier FFP accumulator; zero and untouched at T0.</summary>
        public long FfpBalanceWindow;

        /// <summary>Creates the pre-first-season identity state with only the supplied cash balance populated.</summary>
        /// <param name="startingBalance">Initial signed cash balance.</param>
        /// <returns>A coherent finance state with zero budgets, wage liability, and deep-tier accumulators.</returns>
        public static ClubFinances CreateInitial(long startingBalance)
        {
            return new ClubFinances
            {
                Balance = startingBalance,
                TransferBudget = 0,
                WageBudget = 0,
                WageBillAggregate = 0,
                SeasonRevenueAccrued = 0,
                FfpBalanceWindow = 0
            };
        }

        internal static void ValidateCoherence(in ClubFinances finances)
        {
            if (finances.TransferBudget < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(finances), finances.TransferBudget, "TransferBudget must be non-negative (F1).");
            }

            if (finances.WageBudget < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(finances), finances.WageBudget, "WageBudget must be non-negative (F1).");
            }

            if (finances.WageBillAggregate < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(finances), finances.WageBillAggregate, "WageBillAggregate must be non-negative (F1).");
            }
        }
    }
}

#region VersionHistory
// Version | Date       | Author        | Change
// --------|------------|---------------|----------------------------------------------
// 1.0     | 2026-09-04 | Codex / Anton | Initial #40 T0 per-club finance state.
#endregion
