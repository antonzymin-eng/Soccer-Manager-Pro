// ============================================================================
// File:     src/transfers/PlayerValuation.cs
// Created:  2026-09-12
// Modified: 2026-09-14
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #31 §3.1, FR-TX-001/002 (deterministic valuation + positional need)
// Purpose:  Computes draw-free integer player value from #27 attributes, age, and valuing-club stock.
// ============================================================================

using System;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.Transfers
{
    /// <summary>Pure minimal-tier valuation functions; no personality, CA/PA, staff input, or RNG.</summary>
    public static class PlayerValuation
    {
        /// <summary>Returns the integer mean of #27's 31 canonical [1,20] attributes, excluding weak foot.</summary>
        public static int MeanAttributeRating(in PlayerAttributes attributes)
        {
            int[] values = attributes.ToArray();
            if (values.Length != PlayerDatabaseConstants.ATTRIBUTE_COUNT)
            {
                throw new InvalidOperationException("PlayerAttributes canonical array width does not match ATTRIBUTE_COUNT.");
            }

            long sum = 0;
            for (int i = 0; i < values.Length; i++)
            {
                int value = values[i];
                if (value < PlayerDatabaseConstants.ATTRIBUTE_MIN || value > PlayerDatabaseConstants.ATTRIBUTE_MAX)
                {
                    throw new ArgumentOutOfRangeException(nameof(attributes), value, "Canonical player attributes must remain in [1,20].");
                }

                sum += value;
            }

            return (int)(sum / PlayerDatabaseConstants.ATTRIBUTE_COUNT);
        }

        /// <summary>Returns the deterministic age-curve multiplier in per-mille units.</summary>
        public static int AgeCurvePermille(int age)
        {
            if (age < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(age), age, "Age must be non-negative.");
            }

            if (age < TransfersConstants.PeakAgeMin)
            {
                return TransfersConstants.YoungDiscountPermille;
            }

            if (age <= TransfersConstants.PeakAgeMax)
            {
                return TransfersConstants.PERMILLE_DENOM;
            }

            long yearsPastPeak = (long)age - TransfersConstants.PeakAgeMax;
            long declined = TransfersConstants.PERMILLE_DENOM
                - yearsPastPeak * TransfersConstants.DeclinePerYearPermille;
            if (declined < TransfersConstants.MinimumAgeMultiplierPermille)
            {
                return TransfersConstants.MinimumAgeMultiplierPermille;
            }

            return (int)declined;
        }

        /// <summary>
        /// Returns the valuing club's deterministic positional-scarcity multiplier. A club below the
        /// neutral stock values the player more; an overstocked club values that position less.
        /// </summary>
        public static int ClubNeedMultiplierPermille(int samePositionCount)
        {
            if (samePositionCount < 0 || samePositionCount > PlayerDatabaseConstants.CLUB_SQUAD_SIZE)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(samePositionCount),
                    samePositionCount,
                    "Position stock must be inside the club squad bounds.");
            }

            int neutralCount = PlayerDatabaseConstants.CLUB_SQUAD_SIZE / PlayerDatabaseConstants.POSITION_COUNT;
            int delta = neutralCount - samePositionCount;
            int multiplier = TransfersConstants.PERMILLE_DENOM
                + delta * TransfersConstants.ClubNeedPerPlayerPermille;
            if (multiplier <= 0)
            {
                throw new InvalidOperationException("Configured club-need multiplier must remain positive.");
            }

            return multiplier;
        }

        /// <summary>Computes the attributes+age valuation identity before valuing-club context is applied.</summary>
        public static long ValuePlayerPermille(in PlayerAttributes attributes, int age)
        {
            int meanRating = MeanAttributeRating(in attributes);
            int ageMultiplier = AgeCurvePermille(age);

            checked
            {
                long baseValue = (long)meanRating * TransfersConstants.ValuePerRatingPoint;
                return baseValue * ageMultiplier / TransfersConstants.PERMILLE_DENOM;
            }
        }

        /// <summary>Computes the T0 counterparty value including always-on positional scarcity.</summary>
        public static long CounterpartyValuePermille(
            in PlayerAttributes attributes,
            int age,
            int samePositionCount)
        {
            long baseValue = ValuePlayerPermille(in attributes, age);
            int needMultiplier = ClubNeedMultiplierPermille(samePositionCount);

            checked
            {
                return baseValue * needMultiplier / TransfersConstants.PERMILLE_DENOM;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 deterministic integer valuation. |
// | 1.1     | 2026-09-14 | —      | Discharge proxy-review blind-baseline finding with deterministic positional need. |
#endregion
