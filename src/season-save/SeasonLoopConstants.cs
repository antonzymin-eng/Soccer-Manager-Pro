// File:     src/season-save/SeasonLoopConstants.cs
// Created:  2026-07-25
// Modified: 2026-07-25
// Author:   —
// Spec:     Season & Competition Loop #30 Appendix A (constant catalogue); §3.2 (points);
//           §3.1.1 (permutation seed); Code Standards #20
// Purpose:  Constant catalogue for the season/competition loop (#30 Appendix A). Points values, the
//           season-state sub-blob format version, and the identity-permutation seed sentinel. The
//           [CROSS] determinism rows (DOMAIN_TAG_SEASON_LOOP / SUBSYSTEM_ORDINAL_SEASON_LOOP) are
//           DELIBERATELY ABSENT at T0 — ERR-030-001 is spec-text-first and pins the code const to
//           #30 T2's first draw site (registering a stream with zero draw sites is the phantom-surface
//           class FR-LW-031 forbids; the living-world `world.arcs` precedent).

using TacticalDirector.DeterministicSim;

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// Constants for the Season &amp; Competition Loop (#30 Appendix A).
    /// <para>
    /// Magnitudes are illustrative pending the Stage-2 balance pass (#30 Appendix A / the #21 §9.2
    /// precedent): the spec's contract is the shapes and directions, the <c>[GT]</c> numbers are tunable.
    /// </para>
    /// </summary>
    public static class SeasonLoopConstants
    {
        #region Fixed
        /// <summary>
        /// [FIXED] The season-state sub-blob's own format version (#30 KD-1 / Appendix A). Distinct from
        /// the outer <see cref="SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION"/> that frames it and from
        /// every version nested in the sibling world / match blobs. Gates the season block only; a
        /// mismatch fails loud on load (F3). Bump only on a season-block layout change.
        /// <para>
        /// Declared at T0 alongside the value types it will serialize; the codec that reads it
        /// (<c>SeasonStateCodec</c>) lands at #30 T1.
        /// </para>
        /// </summary>
        public const uint SEASON_STATE_FORMAT_VERSION = 1;

        /// <summary>
        /// [FIXED] The seed value that selects the <b>identity</b> club-label permutation in
        /// <see cref="FixtureScheduler.Generate(int[], ulong)"/> (#30 §3.1.1 — "if the permutation is
        /// the identity (a documented Stage-0 option), the generator makes zero draws").
        /// <para>
        /// With this seed the generator is a pure fixed integer schedule with no permutation step, so
        /// FR-SN-027's "fixture generation needs no draw for the single-league case" holds exactly.
        /// </para>
        /// </summary>
        public const ulong IDENTITY_PERMUTATION_SEED = 0UL;

        /// <summary>
        /// [FIXED] Domain-separation constant folded into the round-resolution key for the HOME side's
        /// goal draw (league-bootstrap KD-7). Paired with
        /// <see cref="ROUND_RESOLUTION_AWAY_SUBSTREAM"/>: the two sides derive from the same fixture key
        /// through different constants, so one uniform per side comes out of one keyed derivation
        /// without either side's draw depending on the other's.
        /// </summary>
        public const ulong ROUND_RESOLUTION_HOME_SUBSTREAM = 0x9B1D8F4A6C27E053UL;

        /// <summary>[FIXED] Domain-separation constant for the AWAY side's goal draw. See
        /// <see cref="ROUND_RESOLUTION_HOME_SUBSTREAM"/>.</summary>
        public const ulong ROUND_RESOLUTION_AWAY_SUBSTREAM = 0x4E75C3A912B6D08FUL;

        /// <summary>
        /// [FIXED] Domain-separation constant for the per-fixture <c>matchSeed</c> a managed fixture's
        /// <c>MatchEngine</c> boots from (#30 §3.4 <c>PlayThroughEngine</c>). Distinct from both
        /// round-resolution sub-streams so the engine's seed and the quick-sim's draws cannot correlate
        /// for the same fixture.
        /// </summary>
        public const ulong MATCH_SEED_DOMAIN = 0x7A20D5E8B34C169FUL;

        /// <summary>
        /// [FIXED] Upper bound on the goals one side can be awarded by the round-resolution model — the
        /// termination cap on the inverse-CDF accumulation (league-bootstrap KD-7). Not a tuning dial:
        /// it exists so a corrupted or extreme lambda cannot spin the accumulation loop, and it sits far
        /// above any scoreline the fitted parameters can reach (with <c>QuickSimLambdaMax</c> at 6.0 the
        /// probability of even reaching 20 is ~1e-6).
        /// </summary>
        public const int MAX_GOALS_PER_SIDE = 20;
        #endregion

        #region Cross
        /// <summary>
        /// [CROSS] The Season &amp; Competition Loop's RNG domain tag (FR-SN-027).
        /// Authoritative source: <c>DeterministicSimConstants.DOMAIN_TAG_SEASON_LOOP</c>.
        /// Deterministic Simulation #16 §3.4. Value: 0x22. Allocated at #30 T2's first draw site per
        /// ERR-030-001 — that draw site is <see cref="RoundResolutionModel"/>'s key derivation, which
        /// folds this tag in so the season's draws are domain-separated from every other subsystem's.
        /// <para>
        /// <b>No <c>SubsystemOrdinals.SeasonLoop</c> mirror (ERR-030-012).</b> A subsystem ordinal exists
        /// only to key a REGISTERED <c>DeterministicRngService</c> stream, and the round-resolution model
        /// deliberately registers none: §3.4.1 requires its draws to be keyed on the fixture rather than
        /// cursor-positioned, so that resolving a round's fixtures in any order yields the same table
        /// (T-SN-CAL-003c). Allocating an ordinal with no stream behind it is the zero-consumer phantom
        /// FR-LW-031 forbids and ERR-030-001 exists to prevent; ordinal 84 stays reserved in spec text
        /// for the first genuinely cursor-positioned season event.
        /// </para>
        /// </summary>
        public static readonly byte DomainTagSeasonLoop =
            DeterministicSimConstants.DOMAIN_TAG_SEASON_LOOP;
        #endregion

        #region GT
        /// <summary>[GT] Points awarded for a win (#30 Appendix A; the association-football 3/1/0
        /// convention, §8.2). A rules variant (e.g. 2/1/0) is a config change, not a code change —
        /// config key <c>[season-save] WinPoints</c>.</summary>
        public static readonly int WinPoints = Config.GetInt("season-save", "WinPoints", 3);

        /// <summary>[GT] Points awarded to each club for a draw (#30 Appendix A).
        /// Config key <c>[season-save] DrawPoints</c>.</summary>
        public static readonly int DrawPoints = Config.GetInt("season-save", "DrawPoints", 1);

        /// <summary>[GT] Points awarded for a loss (#30 Appendix A). Zero under the standard convention;
        /// named rather than implied so a rules variant has one place to change.
        /// Config key <c>[season-save] LossPoints</c>.</summary>
        public static readonly int LossPoints = Config.GetInt("season-save", "LossPoints", 0);

        /// <summary>
        /// [GT] The full scale for the board's job-security reading (#30 §2.2 / FR-SN-014). Job security
        /// is carried as an integer per-mille in <c>[0, JobSecurityScale]</c> rather than a float —
        /// see <see cref="BoardState"/> for why (the #41/#40/#33 integer-arithmetic convention).
        /// Config key <c>[season-save] JobSecurityScale</c>.
        /// </summary>
        public static readonly int JobSecurityScale = Config.GetInt("season-save", "JobSecurityScale", 1000);

        // ── Round-resolution model (§3.4.1 / FR-SN-013a; league-bootstrap KD-7) ──────────────────
        //
        // CALIBRATION STATUS: **UNCALIBRATED — these are provisional, not fitted.** Roadmap item A4a is
        // supposed to fit the three shape parameters against an engine-simulated corpus (KD-8), and
        // A4a's Step 0 pilot RAN on 2026-07-26 and REFUSED to proceed: all 20 engine matches finished
        // 0–0 at a measured rating differential of ±6, because a Stage-0 match never puts the ball in
        // motion at all (ERR-030-014 — see docs/tracking/round-resolution-corpus.md for the evidence and
        // the root cause). Fitting against that corpus would have fitted three parameters to a table of
        // zeros, which is precisely the outcome Step 0 exists to prevent.
        //
        // So the values below are chosen to be FOOTBALL-PLAUSIBLE rather than engine-matched: ~1.35 goals
        // per side at parity is close to the real-world top-division rate, the slope gives a ±6 rating gap
        // roughly a 2:1 expected-goals edge, and home advantage is worth about a third of a rating point.
        // They make the league table behave sensibly for a human reading it; they do NOT yet claim to
        // agree with what the engine would produce. Re-run A4a and re-pin these once the engine can play
        // a match — do not hand-tune them into looking calibrated.

        /// <summary>[GT] Expected goals per side at a zero <c>edge</c> — the round-resolution model's
        /// scale parameter (league-bootstrap KD-7). Config key <c>[season-save] QuickSimBaseGoals</c>.</summary>
        public static readonly float QuickSimBaseGoals =
            Config.GetFloat("season-save", "QuickSimBaseGoals", 1.35f);

        /// <summary>
        /// [GT] How steeply one point of rating advantage bends the expected goals — the exponent scale in
        /// <c>BaseGoals · exp(±slope · edge)</c> (league-bootstrap KD-7). Larger values make the league
        /// table separate more sharply by squad strength.
        /// Config key <c>[season-save] QuickSimGoalRatingSlope</c>.
        /// </summary>
        public static readonly float QuickSimGoalRatingSlope =
            Config.GetFloat("season-save", "QuickSimGoalRatingSlope", 0.35f);

        /// <summary>
        /// [GT] Home advantage expressed in the SAME units as a rating difference, added to
        /// <c>dSquad</c> to form <c>edge</c> (league-bootstrap KD-7). This is a parameter of the model,
        /// NOT a property of either squad — it is fitted from the home/away asymmetry within a
        /// calibration bucket and must never appear on the corpus axis (KD-8 / AR-7 H-1).
        /// Config key <c>[season-save] QuickSimHomeAdvantageRating</c>.
        /// </summary>
        public static readonly float QuickSimHomeAdvantageRating =
            Config.GetFloat("season-save", "QuickSimHomeAdvantageRating", 0.30f);

        /// <summary>[GT] Floor on a side's expected goals — a safety clamp, deliberately NOT fitted
        /// (league-bootstrap KD-7). Config key <c>[season-save] QuickSimLambdaMin</c>.</summary>
        public static readonly float QuickSimLambdaMin =
            Config.GetFloat("season-save", "QuickSimLambdaMin", 0.15f);

        /// <summary>[GT] Ceiling on a side's expected goals — a safety clamp, deliberately NOT fitted
        /// (league-bootstrap KD-7). Config key <c>[season-save] QuickSimLambdaMax</c>.</summary>
        public static readonly float QuickSimLambdaMax =
            Config.GetFloat("season-save", "QuickSimLambdaMax", 6.0f);
        #endregion
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-25 | —      | Initial implementation (#30 T0): Appendix A points + scale rows,   |
// |         |            |        | SEASON_STATE_FORMAT_VERSION, IDENTITY_PERMUTATION_SEED. The        |
// |         |            |        | [CROSS] determinism rows stay absent until #30 T2 (ERR-030-001).   |
// | 1.1     | 2026-07-25 | —      | AR pass 5: the four [GT] rows were `public const` ALL_CAPS under a |
// |         |            |        | `GameplayTuned` region — the pattern the June-30 FR-CS-019 pass    |
// |         |            |        | removed tree-wide (ALL_CAPS is [FIXED]-only; [GT] is static        |
// |         |            |        | readonly off GameplayConfig; the region is named `GT`). A const    |
// |         |            |        | cannot take Config.GetInt, so these four were structurally locked  |
// |         |            |        | out of a migration 17 other catalogues already completed.          |
// | 1.2     | 2026-07-26 | —      | #30 T2: the [CROSS] DomainTagSeasonLoop mirror lands at its first  |
// |         |            |        | draw site (ERR-030-001) — the round-resolution key derivation; the |
// |         |            |        | SubsystemOrdinals.SeasonLoop mirror stays absent because the model |
// |         |            |        | registers no cursor stream (ERR-030-012). Plus the [FIXED] sub-    |
// |         |            |        | stream / match-seed domains + MAX_GOALS_PER_SIDE cap, and the five |
// |         |            |        | [GT] round-resolution rows A4a calibrates.                         |
#endregion
