// File:     src/season-save/LeagueBootstrapConstants.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     League Bootstrap design supplement (docs/tracking/league-bootstrap-design.md) KD-2/KD-4/
//           KD-5/KD-6/KD-9; path-to-playable roadmap A3; Code Standards #20
// Purpose:  Constant catalogue for the league bootstrap — club-count bounds, the three world-seed
//           domain separators, the RNG stream identity, the league strength spread, the squad
//           position template, and the default season calendar cadence.
//
// This catalogue is DISTINCT from SeasonLoopConstants (#30 Appendix A) and SeasonSaveConstants (the
// save-frame version): it is governed by a design supplement, not by #30's spec text, and nothing
// in it is serialized. The season-save folder already carries two catalogues for the same reason.

using System.Collections.ObjectModel;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PlayerDatabase;

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Constants for <see cref="LeagueBootstrap"/> (league-bootstrap-design.md).
    /// <para>
    /// The <c>[GT]</c> magnitudes are illustrative pending a balance pass (the #21 §9.2 precedent):
    /// the contract is the shape — a linear strength ramp over a seeded rank, a position template that
    /// can field every shipped formation — not the specific numbers.
    /// </para>
    /// </summary>
    public static class LeagueBootstrapConstants
    {
        #region Fixed

        /// <summary>
        /// [FIXED] Domain separator folded into the world seed to derive the ROSTER generation seed
        /// (design KD-4). Distinct from the strength and season separators so a change to one use
        /// cannot shift the others.
        /// </summary>
        public const ulong ROSTER_SEED_DOMAIN = 0xA1B2C3D4E5F60717UL;

        /// <summary>[FIXED] Domain separator for the club-STRENGTH rank permutation seed (design KD-4).</summary>
        public const ulong STRENGTH_SEED_DOMAIN = 0x5D3F1A9C7E20B846UL;

        /// <summary>[FIXED] Domain separator for the SEASON (fixture-schedule) seed (design KD-4).</summary>
        public const ulong SEASON_SEED_DOMAIN = 0x2C8E60B1F4A7D593UL;

        /// <summary>
        /// [FIXED] Draw-site id for the per-club roster-generation stream. Registered under
        /// <see cref="SubsystemOrdinals.PlayerDatabase"/> with <c>entityId = clubId</c> — exactly the
        /// registration <see cref="RosterGenerator"/> documents. No new #16 domain tag or subsystem
        /// ordinal is allocated by the bootstrap (design KD-4: <c>DOMAIN_TAG_SEASON_LOOP</c> / ordinal
        /// 84 stay pinned to #30 T2's first draw site per ERR-030-001).
        /// </summary>
        public const string ROSTER_STREAM_SITE_ID = "player-database.roster-generation";

        /// <summary>[FIXED] Stream version for <see cref="ROSTER_STREAM_SITE_ID"/>; bump on a draw-order change.</summary>
        public const ushort ROSTER_STREAM_VERSION = 1;

        #endregion

        #region GT

        // Backing store for SquadPositionCounts. Declared BEFORE the read-only wrapper below: within a
        // class, static field initializers run in textual order, so wrapping an as-yet-uninitialised
        // array would capture null (the PerceptionConstants.BASE_FOV_HALF_ANGLE static-init defect).
        private static readonly int[] s_squadPositionCounts = { 3, 8, 8, 6 };

        /// <summary>[GT] Default number of clubs in a bootstrapped league — one 38-round double
        /// round-robin (380 fixtures). Config key <c>[league-bootstrap] DefaultClubCount</c>.</summary>
        public static readonly int DefaultClubCount = Config.GetInt("league-bootstrap", "DefaultClubCount", 20);

        /// <summary>
        /// [GT] Upper bound on club count (design KD-2). The floor is <see cref="SeasonState"/>'s own
        /// (≥ 2 clubs). This ceiling is a sanity bound, not a capability limit: 32 clubs is already 62
        /// rounds / 992 fixtures, far past anything playable, and it leaves comfortable headroom under
        /// <see cref="DeterministicSimConstants.MaxRngStreams"/> (the bootstrap registers one stream per
        /// club). Raising it above that budget fails loud at <see cref="LeagueBootstrap.Generate"/> and
        /// is locked by test. Config key <c>[league-bootstrap] MaxClubCount</c>.
        /// </summary>
        public static readonly int MaxClubCount = Config.GetInt("league-bootstrap", "MaxClubCount", 32);

        /// <summary>
        /// [GT] Half-width of the league strength ramp, in <c>[1,20]</c> attribute points (design KD-5).
        /// The weakest club's players are shifted by <c>-LeagueStrengthSpread</c>, the strongest by
        /// <c>+LeagueStrengthSpread</c>, linearly over the seeded rank.
        /// Config key <c>[league-bootstrap] LeagueStrengthSpread</c>.
        /// </summary>
        public static readonly int LeagueStrengthSpread = BoundedSpread("LeagueStrengthSpread", 3);

        /// <summary>[GT] World-day the first fixture round falls on. Config key
        /// <c>[league-bootstrap] FirstRoundDay</c>.</summary>
        public static readonly uint FirstRoundDay = NonNegativeDayValue("FirstRoundDay", 7);

        /// <summary>[GT] World-days between consecutive fixture rounds. Config key
        /// <c>[league-bootstrap] DaysBetweenRounds</c>.</summary>
        public static readonly uint DaysBetweenRounds = NonNegativeDayValue("DaysBetweenRounds", 7);

        /// <summary>
        /// Reads the strength-spread <c>[GT]</c> value, bounding it to the attribute scale it shifts.
        /// A negative spread would silently INVERT the ramp (the bottom of the table would get the best
        /// players); a spread past the scale is meaningless (every attribute clamps) and, unbounded,
        /// would overflow <see cref="LeagueBootstrap.StrengthDelta"/>'s integer ramp arithmetic.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The configured value is outside
        /// <c>[0, ATTRIBUTE_MAX - ATTRIBUTE_MIN]</c>. Called from a static field initializer, so a
        /// consumer observes it wrapped in <c>TypeInitializationException</c>, and this type is
        /// unusable for the rest of the process — the intended fail-loud-at-boot posture
        /// <c>GameplayConfig</c>'s own `FormatException` on a malformed value already takes.</exception>
        private static int BoundedSpread(string key, int fallback)
        {
            int max = PlayerDatabaseConstants.ATTRIBUTE_MAX - PlayerDatabaseConstants.ATTRIBUTE_MIN;
            int value = Config.GetInt("league-bootstrap", key, fallback);
            if (value < 0 || value > max)
            {
                throw new System.InvalidOperationException(
                    $"[league-bootstrap] {key} = {value}; the strength spread must be in [0, {max}] " +
                    "— the range of the [1,20] attribute scale it shifts.");
            }

            return value;
        }

        /// <summary>
        /// Reads a world-day <c>[GT]</c> value, refusing a negative one. World days are <c>uint</c>
        /// (<c>WorldClock</c>), and a bare cast would turn a config typo like <c>-1</c> into
        /// 4,294,967,295 — which surfaces much later as a confusing calendar-overflow throw rather
        /// than as "your config value is negative".
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The configured value is negative. As with
        /// <see cref="BoundedSpread"/>, this runs in a static field initializer, so a consumer observes
        /// it wrapped in <c>TypeInitializationException</c>.</exception>
        private static uint NonNegativeDayValue(string key, int fallback)
        {
            int value = Config.GetInt("league-bootstrap", key, fallback);
            if (value < 0)
            {
                throw new System.InvalidOperationException(
                    $"[league-bootstrap] {key} = {value}; a world-day value cannot be negative.");
            }

            return (uint)value;
        }

        /// <summary>
        /// [GT] Per-position player counts in a bootstrapped squad, indexed by
        /// <see cref="PlayerPosition"/> ordinal (GK, DF, MF, FW). Must sum to
        /// <see cref="PlayerDatabaseConstants.CLUB_SQUAD_SIZE"/>.
        /// <para>
        /// Sized against the maximum any shipped formation family needs for a starting XI — F442
        /// 1/4/4/2, F433 1/4/3/3, F4231 1/4/5/1, so GK 1 / DF 4 / MF 5 / FW 3 — with margin for the
        /// seven position-agnostic bench slots. Design KD-6: this template is what stops
        /// <see cref="RosterGenerator"/>'s uniform position draw from producing a squad that cannot
        /// field a back four (which <c>LineupSelector</c> refuses, fail-loud, a few percent of the time
        /// per line per club).
        /// </para>
        /// <para>
        /// Array-valued <c>[GT]</c>: the <see cref="ProjectConstants.GameplayConfig"/> surface has no
        /// array getter, and this table carries a cross-cell invariant (the sum) that only holds against
        /// compile-time values — the same carve-out <c>TacticalInstructionsConstants</c>' tables take.
        /// </para>
        /// <para>
        /// Exposed READ-ONLY over a private backing array. A public <c>int[]</c> would be writable by
        /// anything in the process, and mutating it changes every league generated afterwards while
        /// still satisfying the sum check (e.g. <c>{25, 0, 0, 0}</c> sums to 25 and yields squads that
        /// cannot field a back four) — silent cross-test contamination and a silent break of the KD-6
        /// guarantee. This is the exposed-mutable-array class the project has caught repeatedly
        /// (match-viewer AR-3 M-1, living-world slice-2 AR-1 M-1, #26 T0 AR-1, #30 T0 H-1); the
        /// array-table carve-out above is about the CONFIG surface, not about write access.
        /// </para>
        /// </summary>
        public static readonly ReadOnlyCollection<int> SquadPositionCounts =
            new ReadOnlyCollection<int>(s_squadPositionCounts);

        #endregion

        /// <summary>
        /// Expands <see cref="SquadPositionCounts"/> into a fresh per-player position array of length
        /// <see cref="PlayerDatabaseConstants.CLUB_SQUAD_SIZE"/>, in <see cref="PlayerPosition"/> ordinal
        /// order (all goalkeepers, then defenders, then midfielders, then forwards).
        /// <para>
        /// A method rather than a <c>static readonly</c> field for two reasons: it never hands out a
        /// shared mutable array (the recurring live-array aliasing defect class), and it dodges the
        /// static-initialisation-order trap a <c>[DERIVED]</c> field reading a <c>[GT]</c> field would
        /// hit (the <c>PerceptionConstants.BASE_FOV_HALF_ANGLE</c> defect). Boot-time only.
        /// </para>
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        /// F3 — the catalogue is incoherent: a negative count, a length that does not cover the
        /// <see cref="PlayerPosition"/> roster, or a sum that is not
        /// <see cref="PlayerDatabaseConstants.CLUB_SQUAD_SIZE"/>.
        /// </exception>
        public static PlayerPosition[] BuildSquadPositionTemplate()
        {
            int[] counts = s_squadPositionCounts;
            if (counts.Length != PositionCount)
            {
                throw new System.InvalidOperationException(
                    $"SquadPositionCounts has {counts.Length} entries but PlayerPosition has " +
                    $"{PositionCount} members.");
            }

            int total = 0;
            for (int p = 0; p < counts.Length; p++)
            {
                if (counts[p] < 0)
                {
                    throw new System.InvalidOperationException(
                        $"SquadPositionCounts[{p}] is negative ({counts[p]}).");
                }

                total += counts[p];
            }

            if (total != PlayerDatabaseConstants.CLUB_SQUAD_SIZE)
            {
                throw new System.InvalidOperationException(
                    $"SquadPositionCounts sums to {total} but a squad holds " +
                    $"{PlayerDatabaseConstants.CLUB_SQUAD_SIZE} players.");
            }

            var template = new PlayerPosition[total];
            int w = 0;
            for (int p = 0; p < counts.Length; p++)
            {
                for (int k = 0; k < counts[p]; k++)
                {
                    template[w++] = (PlayerPosition)p;
                }
            }

            return template;
        }

        /// <summary>
        /// The number of <see cref="PlayerPosition"/> members, from its owning catalogue. Not
        /// re-declared here: <see cref="PlayerDatabaseConstants.POSITION_COUNT"/> is the single source,
        /// so this cannot drift from the enum the template indexes.
        /// </summary>
        internal const int PositionCount = PlayerDatabaseConstants.POSITION_COUNT;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (roadmap A3): seed domains, stream         |
// |         |            |        | identity, club-count bounds, strength spread, position template,   |
// |         |            |        | calendar cadence.                                                  |
// | 1.1     | 2026-07-25 | —      | AR-5 M-3/L-2: SquadPositionCounts was a PUBLIC mutable int[] —     |
// |         |            |        | writable by anything in the process, and a mutation that still     |
// |         |            |        | sums to CLUB_SQUAD_SIZE silently voids the KD-6 fieldable-squad    |
// |         |            |        | guarantee. Now a ReadOnlyCollection over a private backing array   |
// |         |            |        | declared before it (static-init order). Config-reader <exception>  |
// |         |            |        | docs now state what a consumer actually observes                   |
// |         |            |        | (TypeInitializationException from the static initializer).         |
#endregion
