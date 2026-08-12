// File:     src/season-save/SeasonLoopConstants.cs
// Created:  2026-07-25
// Modified: 2026-07-27
// Modified: 2026-08-12 (A4a: the three round-resolution [GT]s FITTED against the engine corpus)
// Author:   —
// Spec:     Season & Competition Loop #30 Appendix A (constant catalogue); §3.2 (points);
//           §3.1.1 (permutation seed); §3.5 (boundary roll); Code Standards #20
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

        /// <summary>
        /// [FIXED] Domain-separation constant for the next-season seed the boundary roll derives
        /// (#30 §3.5 <c>DeriveNextSeasonSeed</c>, T3). Distinct from every round-resolution and
        /// match-seed constant above, so a season's own seed cannot correlate with any draw made
        /// <i>inside</i> that season — the successor seed is a different question from any fixture's.
        /// </summary>
        public const ulong SEASON_ROLL_SEED_DOMAIN = 0x3C6EF35F1B4D97A1UL;
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

        // ── Season-boundary roll (§3.5 / FR-SN-029; T3) ──────────────────────────────────────────

        /// <summary>
        /// [GT] Calendar days between a season's LAST round and the next season's FIRST round — the
        /// close-season gap. Positive by requirement: at zero the new season's opening round would fall
        /// on the day the old one ended, and the two calendars would be indistinguishable to the cursor.
        /// Config key <c>[season-save] SeasonBreakDays</c>.
        /// </summary>
        public static readonly uint SeasonBreakDays = PositiveDayValue("SeasonBreakDays", 56);

        /// <summary>
        /// [GT] Job-security gained (per-mille) when the club MEETS the board's objective, applied once
        /// at the boundary roll (§3.5 step (b)). Clamped into <see cref="JobSecurityScale"/> by
        /// <see cref="BoardState"/>, so a run of good seasons saturates at fully secure rather than
        /// accumulating credit without bound.
        /// Config key <c>[season-save] BoardJobSecurityMetDeltaPerMille</c>.
        /// </summary>
        public static readonly int BoardJobSecurityMetDeltaPerMille =
            Config.GetInt("season-save", "BoardJobSecurityMetDeltaPerMille", 150);

        /// <summary>
        /// [GT] Job-security lost (per-mille) <b>per league position short</b> of the objective. Scaling
        /// by the shortfall rather than charging a flat penalty is the whole point: missing a top-half
        /// target by one place is a different conversation from finishing bottom, and a flat rate would
        /// make those identical. Clamped at zero (sacked) by <see cref="BoardState"/>.
        /// Config key <c>[season-save] BoardJobSecurityMissedDeltaPerMille</c>.
        /// </summary>
        public static readonly int BoardJobSecurityMissedDeltaPerMille =
            Config.GetInt("season-save", "BoardJobSecurityMissedDeltaPerMille", 120);

        // ── Round-resolution model (§3.4.1 / FR-SN-013a; league-bootstrap KD-7) ──────────────────
        //
        // CALIBRATION STATUS: **FITTED against the engine, 2026-08-12 (roadmap A4a).** KD-8's Step 0
        // pilot PASSED on the current tree (strong-at-home mean margin +4.000, strong-away −3.500 over
        // 20 keyed matches), and the corpus behind these three numbers is 198 real 90-minute MatchEngine
        // matches over an 11-bucket dSquad grid at 18 per bucket, least-squares fitted by
        // tools/round-resolution-fit.py. Evidence, provenance and raw rows:
        // docs/tracking/round-resolution-corpus.md. The superseded provisional values were 1.35 / 0.35 /
        // 0.30 — football-plausible guesses that made no claim to agree with the engine.
        //
        // **THE FIT DOES NOT MEET KD-8's ACCEPTANCE BARS, and that is recorded rather than papered over.**
        // Two causes, both measured, neither fixable by re-fitting these constants:
        //   1. The ±0.25 per-bucket bar is below the corpus's own noise floor at 18 samples/bucket — 15 of
        //      22 bucket-sides have a standard error on their own mean larger than 0.25 — so no model,
        //      including a perfect one, could be SHOWN to satisfy it at that depth (ERR-030-033).
        //   2. The engine's scorelines are over-dispersed relative to Poisson (mean var/mean 1.395,
        //      pooled z = +5.4), and KD-7's model IS a Poisson draw, whose variance equals its mean by
        //      definition. So the engine's spread — more blowouts, more shut-outs, fewer draws — is
        //      outside this model's FAMILY, not merely off in its coefficients (ERR-030-034).
        // These values are therefore the best available fit of the specified model shape, and are a
        // strict improvement on guesses; they are not a claim that the quick-sim reproduces the engine's
        // score distribution. Do not hand-tune them into looking calibrated — re-run A4a.
        //
        // RE-CAPTURE TRIGGER (KD-8): the corpus measures what the engine does at ONE commit. Anything
        // that moves scoring invalidates the fit rather than merely aging it. The capture commit and
        // SNAPSHOT_SCHEMA_VERSION are recorded in the artifact above.

        /// <summary>[GT] Expected goals per side at a zero <c>edge</c> — the round-resolution model's
        /// scale parameter (league-bootstrap KD-7). Config key <c>[season-save] QuickSimBaseGoals</c>.</summary>
        public static readonly float QuickSimBaseGoals =
            Config.GetFloat("season-save", "QuickSimBaseGoals", 1.2325f);

        /// <summary>
        /// [GT] How steeply one point of rating advantage bends the expected goals — the exponent scale in
        /// <c>BaseGoals · exp(±slope · edge)</c> (league-bootstrap KD-7). Larger values make the league
        /// table separate more sharply by squad strength.
        /// Config key <c>[season-save] QuickSimGoalRatingSlope</c>.
        /// </summary>
        public static readonly float QuickSimGoalRatingSlope =
            Config.GetFloat("season-save", "QuickSimGoalRatingSlope", 0.2162f);

        /// <summary>
        /// [GT] Home advantage expressed in the SAME units as a rating difference, added to
        /// <c>dSquad</c> to form <c>edge</c> (league-bootstrap KD-7). This is a parameter of the model,
        /// NOT a property of either squad — it is fitted from the home/away asymmetry within a
        /// calibration bucket and must never appear on the corpus axis (KD-8 / AR-7 H-1).
        /// Config key <c>[season-save] QuickSimHomeAdvantageRating</c>.
        /// </summary>
        public static readonly float QuickSimHomeAdvantageRating =
            Config.GetFloat("season-save", "QuickSimHomeAdvantageRating", 0.4996f);

        /// <summary>[GT] Floor on a side's expected goals — a safety clamp, deliberately NOT fitted
        /// (league-bootstrap KD-7). Config key <c>[season-save] QuickSimLambdaMin</c>.</summary>
        public static readonly float QuickSimLambdaMin =
            Config.GetFloat("season-save", "QuickSimLambdaMin", 0.15f);

        /// <summary>[GT] Ceiling on a side's expected goals — a safety clamp, deliberately NOT fitted
        /// (league-bootstrap KD-7). Config key <c>[season-save] QuickSimLambdaMax</c>.</summary>
        public static readonly float QuickSimLambdaMax =
            Config.GetFloat("season-save", "QuickSimLambdaMax", 6.0f);
        #endregion

        /// <summary>
        /// Reads a strictly-positive day-count <c>[GT]</c> value, refusing a non-positive one at the
        /// point of READ rather than letting it wrap. <c>GameplayConfig</c> has no unsigned getter, so a
        /// config file carrying <c>0</c> or a negative would otherwise become either a zero-length break
        /// or ~4.29e9 days — the league-bootstrap AR-4 L finding, applied here at the sibling seam.
        /// </summary>
        /// <exception cref="System.InvalidOperationException">The configured value is not positive.</exception>
        private static uint PositiveDayValue(string key, int fallback)
        {
            int value = Config.GetInt("season-save", key, fallback);
            if (value <= 0)
            {
                throw new System.InvalidOperationException(
                    $"[season-save] {key} must be a positive number of days; got {value}.");
            }

            return (uint)value;
        }
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
// | 1.3     | 2026-07-27 | —      | #30 T3 (boundary roll): [FIXED] SEASON_ROLL_SEED_DOMAIN — the      |
// |         |            |        | next-season seed derives through its own domain so a season's seed |
// |         |            |        | cannot correlate with any draw made inside it. Plus three [GT]     |
// |         |            |        | rows — SeasonBreakDays (close-season gap, read through the new     |
// |         |            |        | PositiveDayValue guard so a zero or negative config value fails    |
// |         |            |        | loud instead of wrapping to ~4.29e9) and the two board            |
// |         |            |        | job-security deltas, the missed one charged PER PLACE SHORT so     |
// |         |            |        | missing by one is not the same conversation as finishing bottom.   |
// | 1.4     | 2026-08-12 | —      | **Roadmap A4a — the three round-resolution [GT]s are FITTED, not  |
// |         |            |        | provisional.** QuickSimBaseGoals 1.35 -> 1.2325, GoalRatingSlope  |
// |         |            |        | 0.35 -> 0.2162, QuickSimHomeAdvantageRating 0.30 -> 0.4996, by    |
// |         |            |        | least squares over 198 real MatchEngine matches (11 dSquad        |
// |         |            |        | buckets x 18). KD-8 Step 0 PASSED first (+4.000 / -3.500 margins).|
// |         |            |        | The CALIBRATION STATUS block above is rewritten accordingly and   |
// |         |            |        | records the FAIL verdict against KD-8's two acceptance bars with  |
// |         |            |        | its two measured causes (ERR-030-033 the bar is below the corpus's|
// |         |            |        | own noise floor at 18/bucket; ERR-030-034 the engine is Poisson-  |
// |         |            |        | over-dispersed at z=+5.4, which is a model-FAMILY gap no re-fit   |
// |         |            |        | of these three closes). Locked by RoundResolutionFitLockTests.    |
#endregion
