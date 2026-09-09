// ============================================================================
// File:     src/club-finances/FinanceStep.cs
// Created:  2026-09-04
// Modified: 2026-09-08
// Author:   —
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
        /// <param name="board">Board multiplier; <see cref="BoardModifier.BudgetMultiplierMillPermille"/> must be positive; use <see cref="BoardModifier.Identity"/> for no adjustment.</param>
        /// <returns>A new settled value; wage liability and deep-tier accumulators are carried unchanged.</returns>
        public static ClubFinances SettleFinances(
            in ClubFinances prior,
            int finalTablePosition,
            int clubCount,
            in BoardModifier board)
        {
            ClubFinances.ValidateCoherence(in prior);
            ValidatePosition(finalTablePosition, clubCount);

            if (board.BudgetMultiplierMillPermille <= 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(board),
                    board.BudgetMultiplierMillPermille,
                    "BoardModifier multiplier must be positive; use BoardModifier.Identity for no adjustment (F4).");
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

                result.TransferBudget = ScaleAndClampBudget(
                    baseTransferCeiling,
                    board.BudgetMultiplierMillPermille);

                long baseWageCeiling =
                    ClubFinancesConstants.BaseWageBudget
                    + (prizeMoney * ClubFinancesConstants.WageBudgetPrizeSharePermille
                       / ClubFinancesConstants.PERMILLE_DENOM);

                result.WageBudget = ScaleAndClampBudget(
                    baseWageCeiling,
                    board.BudgetMultiplierMillPermille);
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

        private static long ScaleAndClampBudget(long baseCeiling, int multiplier)
        {
            if (baseCeiling <= 0)
            {
                return 0;
            }

            long ceiling = ClubFinancesConstants.ClubFinancesBudgetCeilingMax;
            if (ceiling <= 0)
            {
                return ceiling;
            }

            long wholeUnits = baseCeiling / ClubFinancesConstants.PERMILLE_DENOM;
            long remainderUnits = baseCeiling % ClubFinancesConstants.PERMILLE_DENOM;

            if (wholeUnits > ceiling / multiplier)
            {
                return ceiling;
            }

            long scaledWhole = wholeUnits * multiplier;
            long scaledRemainder =
                remainderUnits * (long)multiplier
                / ClubFinancesConstants.PERMILLE_DENOM;

            if (scaledRemainder >= ceiling - scaledWhole)
            {
                return ceiling;
            }

            return scaledWhole + scaledRemainder;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-04 | —      | Initial #40 T0 settlement and prize interpolation. |
// | 1.1     | 2026-09-06 | —      | Header author attribution corrected to automated-agent placeholder. |
// | 1.2     | 2026-09-07 | —      | F4 widened from zero-only to all non-positive board multipliers. |
// | 1.3     | 2026-09-07 | —      | Board scaling now caps before any multiplication that could overflow accepted tuning ranges. |
// | 1.4     | 2026-09-08 | —      | Corrected the version-history table to the required parseable pipe-row format. |
#endregion
