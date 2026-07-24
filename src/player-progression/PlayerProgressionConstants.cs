// File:     src/player-progression/PlayerProgressionConstants.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3/§4 + Appendix A (constant catalogue); Code Standards #20
// Purpose:  All numeric constants for #28 aging/growth/regen — the age-derivation divisor, the CA/PA
//           scale, the age-band step, and the regen [GT] balance values. No magic literals in
//           GrowthProjection / AbilityModel / RegenGenerator.

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression
{
    /// <summary>
    /// Constant catalogue for Player Progression &amp; Lifecycle #28. Region order (Code Standards #20):
    /// Fixed → Derived → Cross → GT. The <c>[GT]</c> magnitudes are illustrative pending the §5.6 balance
    /// pass — the shapes/tags are the contract (the #21/#26 precedent). The two RNG-identity mirrors
    /// (<c>DOMAIN_TAG_PLAYER_PROGRESSION</c> / <c>SUBSYSTEM_ORDINAL_PLAYER_PROGRESSION</c>, Appendix A)
    /// land at T2 with the production <c>player-progression.regen</c> stream registration (KD-B), never
    /// earlier — registering a zero-draw stream is the phantom-surface class FR-LW-031 forbids.
    /// </summary>
    public static class PlayerProgressionConstants
    {
        #region Fixed

        /// <summary>[FIXED] World-days per age-year — the age-derivation divisor (§3.1.1).</summary>
        public const int DAYS_PER_YEAR = 365;

        /// <summary>[FIXED] The lifecycle sub-blob version (independent of every other format version; §3.5). Declared now, consumed at T1.</summary>
        public const uint PROGRESSION_SAVE_FORMAT_VERSION = 1;

        #endregion

        #region Derived

        /// <summary>
        /// [DERIVED] Fixed per-regen RNG reservation size (§3.3): the #27 identity draws + the 31
        /// attribute draws + one PotentialAbility draw.
        /// Formula: IDENTITY_DRAWS_PER_PLAYER + ATTRIBUTE_COUNT + 1.
        /// Source constants: PlayerDatabaseConstants.IDENTITY_DRAWS_PER_PLAYER, PlayerDatabaseConstants.ATTRIBUTE_COUNT.
        /// </summary>
        public const int PROGRESSION_REGEN_FIELDS =
            PlayerDatabaseConstants.IDENTITY_DRAWS_PER_PLAYER + PlayerDatabaseConstants.ATTRIBUTE_COUNT + 1;

        #endregion

        #region Cross

        /// <summary>
        /// [CROSS] The [1,20] attribute lower bound a spend/drain respects.
        /// Authoritative source: PlayerDatabaseConstants.ATTRIBUTE_MIN. Squad/Player Data Layer #27.
        /// </summary>
        public const int ATTRIBUTE_MIN = PlayerDatabaseConstants.ATTRIBUTE_MIN;

        /// <summary>
        /// [CROSS] The [1,20] attribute upper bound a spend respects (the F1 ceiling).
        /// Authoritative source: PlayerDatabaseConstants.ATTRIBUTE_MAX. Squad/Player Data Layer #27.
        /// </summary>
        public const int ATTRIBUTE_MAX = PlayerDatabaseConstants.ATTRIBUTE_MAX;

        #endregion

        #region GT

        /// <summary>[GT] The wide-integer CA/PA scale ceiling. TODO: replace with config loader (Stage 1).</summary>
        public static readonly int ABILITY_MAX = 10000;

        /// <summary>
        /// [GT] Cursor points per whole attribute-point. With the §4.3 band step this makes exactly one
        /// [1,20] step per year (KD-8: POINT_COST = DAYS_PER_YEAR ⇒ +1/yr in the Growth band).
        /// TODO: replace with config loader (Stage 1).
        /// </summary>
        public static readonly long POINT_COST = DAYS_PER_YEAR;

        /// <summary>[GT] Age below which a player is in the Growth band (§4.3 &lt;24 → +1/yr). TODO: replace with config loader (Stage 1).</summary>
        public static readonly int GROWTH_AGE = 24;

        /// <summary>[GT] Age above which a player is in the Decline band (§4.3 &gt;30 → −1/yr; age 30 stays Stable). TODO: replace with config loader (Stage 1).</summary>
        public static readonly int DECLINE_AGE = 30;

        /// <summary>[GT] Hard retirement age — deterministic, no draw (§3.4). TODO: replace with config loader (Stage 1).</summary>
        public static readonly int RETIREMENT_AGE = 36;

        /// <summary>[GT] Per-day cursor accrual in the Growth band (Stable = 0). TODO: replace with config loader (Stage 1).</summary>
        public static readonly int GROWTH_DAILY_POINTS = +1;

        /// <summary>[GT] Per-day cursor accrual in the Decline band (Stable = 0). TODO: replace with config loader (Stage 1).</summary>
        public static readonly int DECLINE_DAILY_POINTS = -1;

        // -- Regen [GT] balance values (§3.3; pinned at the §5.6 balance pass) --

        /// <summary>[GT] Regen PotentialAbility floor (a regen is drawn in [max(PA_MIN, CA + REGEN_PA_HEADROOM), ABILITY_MAX]). TODO: replace with config loader (Stage 1).</summary>
        public static readonly int PA_MIN = 4000;

        /// <summary>
        /// [GT] Minimum ability-point gap between a regen's generated CurrentAbility and its drawn
        /// PotentialAbility — the "room to grow" a young regen must have (§3.3). TODO: replace with config loader (Stage 1).
        /// </summary>
        public static readonly int REGEN_PA_HEADROOM = 1000;

        /// <summary>[GT] Regen minimum generated age (young band, §3.3). TODO: replace with config loader (Stage 1).</summary>
        public static readonly int REGEN_AGE_MIN = 16;

        /// <summary>[GT] Regen maximum generated age (young band, §3.3). TODO: replace with config loader (Stage 1).</summary>
        public static readonly int REGEN_AGE_MAX = 20;

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial #28 T0 constant catalogue: Fixed (DAYS_PER_YEAR,       |
// |         |            |        | PROGRESSION_SAVE_FORMAT_VERSION), Derived (PROGRESSION_REGEN_  |
// |         |            |        | FIELDS = 5+31+1), Cross (ATTRIBUTE_MIN/MAX mirror of #27), GT  |
// |         |            |        | (ABILITY_MAX, POINT_COST, GROWTH/DECLINE/RETIREMENT_AGE,       |
// |         |            |        | GROWTH/DECLINE_DAILY_POINTS + the regen balance values). The   |
// |         |            |        | 0x20/82 RNG-identity mirrors land at T2 with the stream (KD-B).|
#endregion
