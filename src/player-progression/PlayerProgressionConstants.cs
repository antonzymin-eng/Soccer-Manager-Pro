// File:     src/player-progression/PlayerProgressionConstants.cs
// Created:  2026-07-24
// Modified: 2026-08-08
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

        /// <summary>
        /// [FIXED] The widest age, in years, the model will derive from a <see cref="PlayerLifecycle"/>'s
        /// birth anchor. This is a REPRESENTABILITY bound, not a football one — it exists so the
        /// <c>long</c> day difference can never narrow into <c>int</c> as garbage.
        /// <para>
        /// <c>BirthWorldDay</c> was the one lifecycle field with no range gate, and it is the
        /// authoritative age anchor. An anchor of <c>-(long)int.MaxValue * 365 - 365</c> was accepted at
        /// every boundary, and the daily step then narrowed the derived age to
        /// <c>int.MinValue</c> — which <see cref="AbilityModel.ClassifyAgeBand"/> classifies as
        /// <c>Growth</c>, so the player grows forever and <see cref="RETIREMENT_AGE"/> can never fire
        /// (ERR-028-006's failure mode through a different door), and which the save path then refuses
        /// as a negative age, making a career that loaded and advanced fine permanently unsavable.
        /// </para>
        /// <para>
        /// <b>This is NOT a football-plausibility bound, and the distinction is load-bearing.</b> It was
        /// first set to 1000 — a sanity bound, not a representability one — which contradicted this very
        /// paragraph and broke
        /// <c>SaveRestore_ANegativeBirthWorldDayBeyondInt32Range_SurvivesTheCodec</c>, the lock proving
        /// the <c>i64</c> field width ERR-028-006 bought. That lock needs an anchor that does NOT fit in
        /// 32 bits (birthWorldDay &lt; int.MinValue, i.e. &gt; ~5.88M years), so any bound below that
        /// makes the field width unprovable. A "reasonable age" gate and a 64-bit-width proof cannot
        /// both hold; the width proof wins, because a silently truncating codec is the worse failure.
        /// </para>
        /// <para>
        /// 100,000,000 sits far above that floor and far below the ceiling: the widest derived age is
        /// <c>MAX_DERIVABLE_AGE_YEARS + uint.MaxValue/365</c> ≈ 1.117e8, comfortably inside
        /// <c>int.MaxValue</c>. It still refuses the anchor that produced the defect
        /// (<c>-(long)int.MaxValue * 365 - 365</c> ≈ -7.84e11, which derives ~2.15e9 and overflows).
        /// If a football-plausibility bound on age is ever wanted, it belongs at the roster generator
        /// as a separate <c>[GT]</c>, not here.
        /// </para>
        /// </summary>
        public const int MAX_DERIVABLE_AGE_YEARS = 100_000_000;

        /// <summary>
        /// [FIXED] The #28 sub-blob's self-identifying leading tag — <c>'P''R''O''G'</c>, written BEFORE
        /// the version (ERR-028-004). Not decoration and deliberately NOT the
        /// <c>DOMAIN_TAG_PLAYER_PROGRESSION</c> RNG tag §3.5 named in its place: every sub-blob format in
        /// the save stack sits at version 1, so without a magic each codec decodes a sibling's bytes
        /// cleanly and silently. A version gate separates generations of ONE format, never one format
        /// from another (ERR-029-005 / ERR-041-009).
        /// </summary>
        public const uint PROGRESSION_SAVE_MAGIC = 0x50524F47;   // 'P''R''O''G'

        /// <summary>[FIXED] The lifecycle sub-blob version (independent of every other format version; §3.5). Gates the generation of the format identified by <see cref="PROGRESSION_SAVE_MAGIC"/> — the magic says WHICH format, this says WHICH VERSION of it.</summary>
        public const uint PROGRESSION_SAVE_FORMAT_VERSION = 1;

        /// <summary>
        /// [FIXED] <see cref="PlayerLifecycle.LastAdvancedWorldDay"/>'s never-advanced sentinel.
        /// <c>uint.MaxValue</c> rather than 0 because day 0 is a legitimate world day — a zero default
        /// would read as "already advanced on day 0" and silently skip the first real step (the day-0
        /// trap; the #29 <c>TRAINING_NOT_ADVANCED_SENTINEL</c> precedent, same value for the same reason).
        /// </summary>
        public const uint PROGRESSION_NOT_ADVANCED_SENTINEL = uint.MaxValue;

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

        /// <summary>
        /// [GT] The PotentialAbility headroom a NEW-GAME (bootstrapped) player is seeded with, above his
        /// generated CurrentAbility: <c>PA = clamp(CA + NEW_GAME_PA_HEADROOM, PA_MIN, ABILITY_MAX)</c>.
        /// <para>
        /// <b>This is a placeholder for authored data, and the seam matters more than the value</b>
        /// (ERR-028-003). §3.2 sources new-game PA from the authored player database — which is
        /// <b>#47</b>'s (New-Game Setup &amp; Database Editor), a spec with no <c>src/</c> assembly — so
        /// until #47 lands, a procedurally bootstrapped league has no authored PA to read and #28 must
        /// seed one. Deliberately <b>deterministic, not drawn</b>: a draw here would be #28's first draw
        /// site and would force the <c>player-progression.regen</c> stream to register (FR-PG-020) for a
        /// value #47 is going to overwrite. #47 fills real values through the same seam.
        /// </para>
        /// <para>
        /// <b>Recorded, not fixed:</b> at the §4.3 band step a whole youth career raises CA by only
        /// ~421 of <see cref="ABILITY_MAX"/> (8 growth years x ~52.6, one attribute per year), so ANY
        /// headroom above ~420 makes the PA ceiling unreachable and F1 unexercised in production —
        /// including this one, and including any realistic authored gap. That is a property of the
        /// growth RATE, not of where PA comes from, and it is the Stage-3 <c>curveEnabled</c> tier's to
        /// close. KD-W1 forbids retuning it in the pass that wires the subsystem.
        /// </para>
        /// TODO: replace with the #47 authored value (Stage 1).
        /// </summary>
        public static readonly int NEW_GAME_PA_HEADROOM = 1500;

        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.1     | 2026-08-08 | —      | #28 T1: + PROGRESSION_SAVE_MAGIC ("PROG"), written before the  |
// |         |            |        | version — ERR-028-004, since §3.5 specified the RNG domain tag |
// |         |            |        | as the block's identifier and a version is not one             |
// |         |            |        | (ERR-029-005/ERR-041-009). + NEW_GAME_PA_HEADROOM, the         |
// |         |            |        | deterministic placeholder for #47's authored PA (ERR-028-003). |
// | 1.0     | 2026-07-24 | —      | Initial #28 T0 constant catalogue: Fixed (DAYS_PER_YEAR,       |
// |         |            |        | PROGRESSION_SAVE_FORMAT_VERSION), Derived (PROGRESSION_REGEN_  |
// |         |            |        | FIELDS = 5+31+1), Cross (ATTRIBUTE_MIN/MAX mirror of #27), GT  |
// |         |            |        | (ABILITY_MAX, POINT_COST, GROWTH/DECLINE/RETIREMENT_AGE,       |
// |         |            |        | GROWTH/DECLINE_DAILY_POINTS + the regen balance values). The   |
// |         |            |        | 0x20/82 RNG-identity mirrors land at T2 with the stream (KD-B).|
#endregion
