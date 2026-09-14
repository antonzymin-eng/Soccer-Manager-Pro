// ============================================================================
// File:     src/transfers/TransfersConstants.cs
// Created:  2026-09-12
// Modified: 2026-09-14
// Author:   —
// Specs:    Spec #20 §3.2.3, §3.6.2 (constant catalogue, GT loading, style/docs)
//           Spec #31 Appendix A, §3.1-§3.2, §3.5 (transfer constants)
// Purpose:  Declares the fixed integer identities and tunable Stage-2 transfer magnitudes.
// ============================================================================

using System;

using TacticalDirector.PlayerDatabase;

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.Transfers
{
    /// <summary>Constant catalogue for Transfers, Contracts &amp; Negotiation #31.</summary>
    public static class TransfersConstants
    {
        #region Fixed

        /// <summary>[FIXED] Shared per-mille denominator and identity multiplier. Spec #31 Appendix A.</summary>
        public const int PERMILLE_DENOM = 1000;

        /// <summary>[FIXED] Identity multiplier for deferred personality/staff modulation. Spec #31 Appendix A.</summary>
        public const int IDENTITY_MULTIPLIER_PERMILLE = 1000;

        #endregion

        #region Derived bounds

        private static readonly int MaxClubNeedPerPlayerPermille = DeriveMaxClubNeedPerPlayerPermille();

        #endregion

        #region GT

        /// <summary>[GT] Currency value of one mean-rating point. Config key [transfers] ValuePerRatingPoint. Spec #31 Appendix A.</summary>
        public static readonly int ValuePerRatingPoint = Positive("ValuePerRatingPoint", 10_000);

        /// <summary>[GT] First age in the neutral peak band. Config key [transfers] PeakAgeMin. Spec #31 Appendix A.</summary>
        public static readonly int PeakAgeMin = Positive("PeakAgeMin", 23);

        /// <summary>[GT] Last age in the neutral peak band. Config key [transfers] PeakAgeMax. Spec #31 Appendix A.</summary>
        public static readonly int PeakAgeMax = AtLeast("PeakAgeMax", 29, PeakAgeMin);

        /// <summary>[GT] Very-young valuation multiplier; must remain below the neutral peak. Spec #31 Appendix A.</summary>
        public static readonly int YoungDiscountPermille = SubPermille("YoungDiscountPermille", 800);

        /// <summary>[GT] Per-year decline after the peak band. Config key [transfers] DeclinePerYearPermille. Spec #31 Appendix A.</summary>
        public static readonly int DeclinePerYearPermille = Positive("DeclinePerYearPermille", 50);

        /// <summary>[GT] Floor for the post-peak age multiplier; must remain below the neutral peak. Spec #31 Appendix A.</summary>
        public static readonly int MinimumAgeMultiplierPermille = SubPermille("MinimumAgeMultiplierPermille", 400);

        /// <summary>
        /// [GT] Width of the synchronous counter-offer band around counterparty value, in per-mille.
        /// It is strictly inside (0,1000) so a positive valuation retains both counter and reject regions.
        /// </summary>
        public static readonly int NegotiationCounterBandPermille = PositiveSubPermille("NegotiationCounterBandPermille", 50);

        /// <summary>
        /// [GT] Value multiplier step per player above/below the neutral positional-stock count.
        /// The upper bound is derived from #27 squad/position cardinalities so every legal stock keeps the
        /// resulting multiplier strictly positive.
        /// </summary>
        public static readonly int ClubNeedPerPlayerPermille = BoundedNonNegative(
            "ClubNeedPerPlayerPermille",
            20,
            MaxClubNeedPerPlayerPermille);

        /// <summary>[GT] Minimal summer-window length in world days. Config key [transfers] SummerWindowLengthDays. Spec #31 Appendix A.</summary>
        public static readonly int SummerWindowLengthDays = Positive("SummerWindowLengthDays", 45);

        /// <summary>[GT] Genesis contract length used by the later T2 seeding step. Config key [transfers] DefaultContractSeasons. Spec #31 Appendix A.</summary>
        public static readonly int DefaultContractSeasons = Positive("DefaultContractSeasons", 3);

        /// <summary>[GT] Genesis wage used by the later T2 seeding step. Config key [transfers] DefaultWagePerPeriod. Spec #31 Appendix A.</summary>
        public static readonly int DefaultWagePerPeriod = NonNegative("DefaultWagePerPeriod", 1_000);

        #endregion

        private static int DeriveMaxClubNeedPerPlayerPermille()
        {
            int neutralCount = PlayerDatabaseConstants.CLUB_SQUAD_SIZE / PlayerDatabaseConstants.POSITION_COUNT;
            int maximumOverstock = PlayerDatabaseConstants.CLUB_SQUAD_SIZE - neutralCount;
            if (maximumOverstock <= 0)
            {
                throw new InvalidOperationException("#27 squad/position cardinalities cannot derive a club-need safety bound.");
            }

            return (PERMILLE_DENOM - 1) / maximumOverstock;
        }

        private static int Positive(string key, int fallback)
        {
            int value = Config.GetInt("transfers", key, fallback);
            if (value <= 0)
            {
                throw new InvalidOperationException("[transfers] " + key + " must be positive.");
            }

            return value;
        }

        private static int NonNegative(string key, int fallback)
        {
            int value = Config.GetInt("transfers", key, fallback);
            if (value < 0)
            {
                throw new InvalidOperationException("[transfers] " + key + " must be non-negative.");
            }

            return value;
        }

        private static int AtLeast(string key, int fallback, int minimum)
        {
            int value = Config.GetInt("transfers", key, fallback);
            if (value < minimum)
            {
                throw new InvalidOperationException("[transfers] " + key + " must be at least " + minimum + ".");
            }

            return value;
        }

        private static int Permille(string key, int fallback)
        {
            int value = Config.GetInt("transfers", key, fallback);
            if (value < 0 || value > PERMILLE_DENOM)
            {
                throw new InvalidOperationException("[transfers] " + key + " must be in [0,1000].");
            }

            return value;
        }

        private static int SubPermille(string key, int fallback)
        {
            int value = Permille(key, fallback);
            if (value >= PERMILLE_DENOM)
            {
                throw new InvalidOperationException("[transfers] " + key + " must be below 1000.");
            }

            return value;
        }

        private static int PositiveSubPermille(string key, int fallback)
        {
            int value = SubPermille(key, fallback);
            if (value == 0)
            {
                throw new InvalidOperationException("[transfers] " + key + " must be positive.");
            }

            return value;
        }

        private static int BoundedNonNegative(string key, int fallback, int maximum)
        {
            int value = NonNegative(key, fallback);
            if (value > maximum)
            {
                throw new InvalidOperationException("[transfers] " + key + " must not exceed " + maximum + ".");
            }

            return value;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 fixed/GT constants catalogue with GameplayConfig loading. |
// | 1.1     | 2026-09-14 | —      | Add deterministic counter-offer band and always-on positional-need tuning. |
// | 1.2     | 2026-09-14 | —      | Derive club-need safety cap from #27 cardinalities; require real age discounts and sub-1000 negotiation band. |
// | 1.3     | 2026-09-14 | —      | Require positive PeakAgeMin so the young-discount region is non-empty and its boundary test is valid. |
#endregion
