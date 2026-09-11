// ============================================================================
// File:     src/club-finances/FinanceStep.cs
// Created:  2026-09-04
// Modified: 2026-09-11
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #40 §3.1/§7.1, FR-FN-001/005-008/011/018/028 (season settlement + T3a daily accrual)
// Purpose:  Implements the pure deterministic season-boundary finance projection, prize interpolation,
//           and the T3a accounting primitive for daily sponsorship/matchday revenue accrual.
// ============================================================================

using System;

namespace TacticalDirector.ClubFinances
{
    /// <summary>Pure finance calculations; no clock, world tick, or RNG dependency.</summary>
    public static class FinanceStep
    {
        /// <summary>Adds position prize money, overwrites next-season budget ceilings, and closes the prior season revenue accumulator.</summary>
        /// <param name="prior">Existing coherent club finance state.</param>
        /// <param name="finalTablePosition">One-based final league position.</param>
        /// <param name="clubCount">Number of clubs in the division; must be at least two.</param>
        /// <param name="board">Board multiplier; <see cref="BoardModifier.BudgetMultiplierMillPermille"/> must be positive; use <see cref="BoardModifier.Identity"/> for no adjustment.</param>
        /// <returns>
        /// A new settled value. Wage liability and <see cref="ClubFinances.FfpBalanceWindow"/> carry forward;
        /// <see cref="ClubFinances.SeasonRevenueAccrued"/> resets to zero for the new season after the prior
        /// season state has reached this boundary.
        /// </returns>
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

            // T3a lifecycle closure: this is a CURRENT-season accumulator. The season boundary is the
            // single point at which it becomes prior-season history, so the next season starts at zero.
            // The future FFP slice may consume prior.SeasonRevenueAccrued before this reset as part of
            // this same pure settlement calculation; FfpBalanceWindow itself carries unchanged today.
            result.SeasonRevenueAccrued = 0L;

            ClubFinances.ValidateCoherence(in result);
            return result;
        }

        /// <summary>
        /// Applies one T3a calendar day's already-derived sponsorship and matchday revenue to the club's
        /// accounting state. This method owns the mutation semantics only: later T3 slices own the
        /// sponsorship model, matchday model, stochastic variance and #30 daily invocation.
        /// </summary>
        /// <param name="prior">Existing coherent club finance state.</param>
        /// <param name="sponsorshipRevenue">Non-negative sponsorship cash attributable to this day.</param>
        /// <param name="matchdayRevenue">Non-negative matchday cash for this day; zero on non-match days.</param>
        /// <param name="deepRevenueEnabled">
        /// Behaviour-neutral T3 gate. <c>false</c> returns <paramref name="prior"/> field-identically.
        /// </param>
        /// <returns>
        /// A detached value with the day's total added to both <see cref="ClubFinances.Balance"/> and
        /// <see cref="ClubFinances.SeasonRevenueAccrued"/>. Budgets, wage liability and
        /// <see cref="ClubFinances.FfpBalanceWindow"/> are unchanged in T3a.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// The deep path is enabled and either revenue component is negative.
        /// </exception>
        /// <exception cref="OverflowException">
        /// The component sum, resulting balance, or season accumulator is outside signed 64-bit range.
        /// No caller-visible state is mutated because the operation returns a value copy.
        /// </exception>
        public static ClubFinances AccrueDailyRevenue(
            in ClubFinances prior,
            long sponsorshipRevenue,
            long matchdayRevenue,
            bool deepRevenueEnabled)
        {
            ClubFinances.ValidateCoherence(in prior);

            if (!deepRevenueEnabled)
            {
                return prior;
            }

            if (sponsorshipRevenue < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(sponsorshipRevenue),
                    sponsorshipRevenue,
                    "Daily sponsorship revenue must be non-negative.");
            }

            if (matchdayRevenue < 0)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(matchdayRevenue),
                    matchdayRevenue,
                    "Daily matchday revenue must be non-negative.");
            }

            ClubFinances result = prior;
            checked
            {
                long dailyRevenue = sponsorshipRevenue + matchdayRevenue;
                result.Balance += dailyRevenue;
                result.SeasonRevenueAccrued += dailyRevenue;
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
// | 1.5     | 2026-09-11 | OpenAI | T3a: add pure identity-gated daily sponsorship/matchday revenue accrual primitive. |
// | 1.6     | 2026-09-11 | OpenAI | T3a: reset current-season revenue at settlement while carrying the future FFP window. |
#endregion
