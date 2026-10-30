// ============================================================================
// File:     src/club-finances/FinanceStep.cs
// Created:  2026-09-04
// Modified: 2026-10-13
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #40 §3.1/§3.7/§7.1, FR-FN-001/005-008/011/018/028 (settlement + daily revenue)
// Purpose:  Implements the pure deterministic season-boundary finance projection, prize interpolation,
//           and deterministic daily sponsorship/matchday amounts and accounting.
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

            // Future FFP logic must consume prior.SeasonRevenueAccrued before this reset. Wage aggregate
            // and FFP-window lifecycle are separate; the new season always starts this accumulator at zero.
            result.SeasonRevenueAccrued = 0L;

            ClubFinances.ValidateCoherence(in result);
            return result;
        }

        /// <summary>
        /// Applies one T3a calendar day's already-derived sponsorship and matchday revenue to the club's
        /// accounting state. DailyRevenue owns deterministic amounts; #30 owns daily invocation.
        /// Stochastic variance remains deferred to T3c+.
        /// </summary>
        /// <param name="prior">Existing coherent club finance state. Coherence is validated even when the deep gate is off.</param>
        /// <param name="sponsorshipRevenue">Non-negative sponsorship cash attributable to this day.</param>
        /// <param name="matchdayRevenue">Non-negative matchday cash for this day; zero on non-match days.</param>
        /// <param name="deepRevenueEnabled">
        /// Behaviour-neutral T3 gate. For a coherent <paramref name="prior"/>, <c>false</c> returns it
        /// field-identically and does not interpret the deep-only revenue amounts.
        /// </param>
        /// <returns>
        /// A detached value with the day's total added to both <see cref="ClubFinances.Balance"/> and
        /// <see cref="ClubFinances.SeasonRevenueAccrued"/>. Budgets, wage liability and
        /// <see cref="ClubFinances.FfpBalanceWindow"/> are unchanged in T3a.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="prior"/> is incoherent; or the deep path is enabled and either revenue component is negative.
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

        /// <summary>Derives draw-free daily sponsorship and home-fixture income (#40 §3.7.2).</summary>
        /// <param name="homeFixturesCompleted">Non-negative completed home-fixture count for this day.</param>
        /// <param name="leaguePosition">One-based live league position after the day's results.</param>
        /// <param name="clubCount">Number of clubs in the division, at least two.</param>
        /// <param name="inSeasonDay">Whether the day lies between the live season's first and last round days, inclusive.</param>
        /// <returns>Sponsorship and matchday amounts in integer currency units; break days return zero.</returns>
        /// <exception cref="ArgumentOutOfRangeException">A count/position is invalid or a home fixture falls outside the season.</exception>
        /// <exception cref="InvalidOperationException">The revenue catalogue has invalid magnitudes or fill endpoints.</exception>
        /// <exception cref="OverflowException">Checked signed-64-bit arithmetic overflows.</exception>
        public static (long Sponsorship, long Matchday) DailyRevenue(
            long homeFixturesCompleted, int leaguePosition, int clubCount, bool inSeasonDay)
        {
            if (homeFixturesCompleted < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(homeFixturesCompleted), homeFixturesCompleted,
                    "Completed home-fixture count must be non-negative.");
            }

            if (clubCount < 2)
            {
                throw new ArgumentOutOfRangeException(nameof(clubCount), clubCount, "clubCount must be at least 2.");
            }

            if (leaguePosition < 1 || leaguePosition > clubCount)
            {
                throw new ArgumentOutOfRangeException(nameof(leaguePosition), leaguePosition,
                    "Live league position must be in [1, clubCount].");
            }

            if (!inSeasonDay && homeFixturesCompleted != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(homeFixturesCompleted), homeFixturesCompleted,
                    "Home fixtures must fall on an in-season day.");
            }

            if (ClubFinancesConstants.DailySponsorshipRevenue < 0
                || ClubFinancesConstants.MatchdayRevenuePerSpectator < 0
                || ClubFinancesConstants.MatchdayFillBottomPermille < 0
                || ClubFinancesConstants.MatchdayFillTopPermille < ClubFinancesConstants.MatchdayFillBottomPermille
                || ClubFinancesConstants.MatchdayFillTopPermille > ClubFinancesConstants.PERMILLE_DENOM)
            {
                throw new InvalidOperationException("Revenue amounts must be non-negative and fill endpoints must satisfy 0 <= bottom <= top <= 1000.");
            }

            checked
            {
                long sponsorship = inSeasonDay ? ClubFinancesConstants.DailySponsorshipRevenue : 0L;
                long fillSpan = (long)ClubFinancesConstants.MatchdayFillTopPermille
                    - ClubFinancesConstants.MatchdayFillBottomPermille;
                long fillPermille = ClubFinancesConstants.MatchdayFillTopPermille
                    - fillSpan * (leaguePosition - 1L) / (clubCount - 1L);
                long attendance = ClubFinancesConstants.MATCHDAY_STADIUM_CAPACITY * fillPermille
                    / ClubFinancesConstants.PERMILLE_DENOM;
                long matchday = homeFixturesCompleted * attendance * ClubFinancesConstants.MatchdayRevenuePerSpectator;
                return (sponsorship, matchday);
            }
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
// | 1.7     | 2026-09-11 | OpenAI | Review: name the completed-season revenue handoff and clarify coherence-before-gate semantics. |
// | 1.8     | 2026-09-11 | OpenAI | Review close-out: remove the misleading reset helper and name prior-season FFP consumption ordering at the direct zero reset. |
// | 1.9     | 2026-10-13 | —      | T3b2: checked integer sponsorship/attendance amounts and catalogue/input validation. |
#endregion
