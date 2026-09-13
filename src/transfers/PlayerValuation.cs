// ============================================================================
// File:     src/transfers/PlayerValuation.cs
// Created:  2026-09-12
// Modified: 2026-09-12
// Author:   —
// Specs:    Spec #20 §3.6.2 (style & docs governance)
//           Spec #31 §3.1, FR-TX-001/002 (pure deterministic minimal valuation)
// Purpose:  Computes draw-free integer player value from #27's canonical 31 attributes plus age.
// ============================================================================

using System;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.Transfers
{
    /// <summary>Pure minimal-tier valuation functions; no club need, personality, CA/PA, or RNG.</summary>
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

        /// <summary>Computes Stage-2 player value using integer mean rating × configured value × age multiplier.</summary>
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
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 deterministic integer valuation. |
#endregion
