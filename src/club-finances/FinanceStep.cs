// ============================================================================
// File:     src/club-finances/FinanceStep.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   Codex / Anton
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #40 §3.1, FR-FN-001/005-008/011/018/028 (season settlement)
// Purpose:  Implements the pure deterministic season-boundary finance projection and prize interpolation.
// ============================================================================

using System;

namespace TacticalDirector.ClubFinances
{
    /// <summary>Pure T0 season-boundary finance calculations; no clock, world tick, or RNG dependency.</summary>
    public static class FinanceStep
    {
        /// <summary>Adds position prize money and overwrites the next season's transfer and wage ceilings.</summary>
        /// <param name="prior">Existing coherent club finance state.</param>
        /// <param name="finalTablePosition">One-based final league position.</param>
        /// <param name="clubCount">Number of clubs in the division; must be at least two.</param>
        /// <param name="board">Board multiplier; use <see cref="BoardModifier.Identity"/> for no adjustment.</param>
        /// <returns>A new settled value; wage liability and deep-tier accumulators are carried unchanged.</returns>
        public static ClubFinances SettleFinances(
            in ClubFinances prior,
            int finalTablePosition,
            int clubCount,
            in BoardModifier board)
        {
            ClubFinances.ValidateCoherence(in prior);
            ValidatePosition(finalTablePosition, clubCount);

            if (board.BudgetMultiplierMillPermille == 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(board),
                    board.BudgetMultiplierMillPermille,
                    "BoardModifier multiplier 0 is invalid; use BoardModifier.Identity for no adjustment (F4).");
            }

            long prizeMoney = PrizeMoneyForPosition(finalTablePosition, clubCount);
            ClubFinances result = prior;

            checked
            {
                result.Balance += prizeMoney;

                long baseTransferCeiling =
                    ClubFinancesConstants.BaseTransferBudget
                    + (prizeMoney * ClubFinancesConstants.TransferBudgetPrizeSharePermille
                       / ClubFinancesConstants.PERMILLE_DENOM);

                long transferWithBoard =
                    baseTransferCeiling * board.BudgetMultiplierMillPermille
                    / ClubFinancesConstants.PERMILLE_DENOM;

                result.TransferBudget = ClampBudget(transferWithBoard);

                long baseWageCeiling =
                    ClubFinancesConstants.BaseWageBudget
                    + (prizeMoney * ClubFinancesConstants.WageBudgetPrizeSharePermille
                       / ClubFinancesConstants.PERMILLE_DENOM);

                long wageWithBoard =
                    baseWageCeiling * board.BudgetMultiplierMillPermille
                    / ClubFinancesConstants.PERMILLE_DENOM;

                result.WageBudget = ClampBudget(wageWithBoard);
            }

            ClubFinances.ValidateCoherence(in result);
            return result;
        }

        /// <summary>Linearly interpolates integer prize money between winner and last-place endpoints.</summary>
        /// <param name="position">One-based final position.</param>
        /// <param name="clubCount">Division club count; must be at least two.</param>
        /// <returns>Position-keyed integer prize money.</returns>
        public static long PrizeMoneyForPosition(int position, int clubCount)
        {
            ValidatePosition(position, clubCount);

            if (clubCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(clubCount), clubCount, "clubCount must be at least 2 (F7).");
            }

            long span =
                ClubFinancesConstants.PrizeMoneyWinner
                - ClubFinancesConstants.PrizeMoneyLastPlace;

            if (span < 0)
            {
                throw new InvalidOperationException("PrizeMoneyLastPlace must not exceed PrizeMoneyWinner.");
            }

            checked
            {
                return ClubFinancesConstants.PrizeMoneyWinner
                    - span * (position - 1L) / (clubCount - 1L);
            }
        }

        private static void ValidatePosition(int position, int clubCount)
        {
            if (clubCount <= 0 || position < 1 || position > clubCount)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(position),
                    position,
                    "finalTablePosition must be in [1, clubCount] and clubCount must be positive (F7).");
            }
        }

        private static long ClampBudget(long value)
        {
            if (value < 0)
            {
                return 0;
            }

            if (value > ClubFinancesConstants.ClubFinancesBudgetCeilingMax)
            {
                return ClubFinancesConstants.ClubFinancesBudgetCeilingMax;
            }

            return value;
        }
    }
}

#region VersionHistory
// Version | Date       | Author        | Change
// --------|------------|---------------|----------------------------------------------
// 1.0     | 2026-09-04 | Codex / Anton | Initial #40 T0 settlement and prize interpolation.
#endregion
