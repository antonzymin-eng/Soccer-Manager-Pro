// File:     src/season-save/RoundResolutionModel.cs
// Created:  2026-07-26
// Modified: 2026-07-26
// Author:   —
// Spec:     Season & Competition Loop #30 §3.4.1 (FR-SN-013a), FR-SN-027; League Bootstrap design
//           supplement KD-7 (model shape) + KD-8 (calibration); path-to-playable roadmap C1/C1a;
//           Code Standards #20
// Purpose:  The deterministic quick-sim that resolves a non-managed fixture in microseconds instead of
//           the ~2 minutes a real MatchEngine match costs. This is the piece roadmap C1 puts on the
//           critical path: without it a 20-club season needs >= 16 hours of compute, so "simulate every
//           fixture with the real engine" is not a slow option, it is not an option.
//
// Season-cadence only (a handful of arithmetic ops per fixture, called from the world-tick loop), so
// the 60 Hz zero-alloc rules do not apply.

using UnityEngine;

namespace TacticalDirector.SeasonSave
{
    /// <summary>
    /// The round-resolution model (#30 §3.4.1 / league-bootstrap KD-7): a scoreline drawn from the
    /// season RNG sub-stream, <b>keyed</b> on
    /// <c>(seasonSeed, seasonNumber, roundIndex, homeClubId, awayClubId)</c>.
    /// <para>
    /// <b>Keyed, not cursor-positioned — and that is load-bearing.</b> Every draw derives from a pure
    /// function of the fixture's identity, so resolving a round's fixtures in ANY order yields the
    /// byte-identical table (§3.4.1 / T-SN-CAL-003c), and a fixture re-resolved after a save/restore
    /// reproduces its own scoreline with no cursor to restore. That is why this model registers no
    /// <c>DeterministicRngService</c> stream: a cursor-positioned stream would make the result depend on
    /// how many fixtures had been drawn before it. It still draws from the season sub-stream in the sense
    /// FR-SN-027 requires — <see cref="SeasonLoopConstants.DomainTagSeasonLoop"/> is folded into the key,
    /// which is this domain tag's first consumer (ERR-030-001). See that constant's doc for why no
    /// subsystem ordinal is allocated (ERR-030-012).
    /// </para>
    /// <para>
    /// <b>Float posture, stated rather than assumed (KD-7).</b> <c>Mathf.Exp</c> is a libm call and the
    /// lambdas are floats. That is the project's Stage-0 doctrine — single-machine determinism on the
    /// pinned certification host, cross-platform bit-exactness deferred to Stage 5 with Fixed64 — and it
    /// is the posture the match engine already ships. It is called out because this model's output is
    /// <b>persisted</b> (the scoreline reaches <see cref="LeagueTable"/>, which
    /// <see cref="SeasonStateCodec"/> serializes), unlike most float work, and because the adjacent
    /// management specs deliberately use integer/per-mille arithmetic. If a later decision wants
    /// persisted season state off floats entirely, this is the line item to revisit.
    /// </para>
    /// <para>
    /// <b>Calibration (roadmap A4a / KD-8) — NOT DONE, and blocked upstream.</b> The three shape
    /// parameters in <see cref="SeasonLoopConstants"/> are provisional football-plausible values, not
    /// values fitted against the engine. A4a's Step 0 pilot ran and refused to proceed: a Stage-0 match
    /// never sets the ball in motion, so every engine match is 0–0 regardless of squad strength
    /// (ERR-030-014; evidence in <c>docs/tracking/round-resolution-corpus.md</c>). A model that is
    /// internally consistent and empirically wrong passes every unit test and makes the league table feel
    /// wrong — so treat this model's *shape* as settled and its *numbers* as open.
    /// </para>
    /// </summary>
    public static class RoundResolutionModel
    {
        /// <summary>
        /// Resolves one fixture to a scoreline.
        /// </summary>
        /// <param name="fixture">The fixture being resolved; supplies both club ids and the round index.</param>
        /// <param name="seasonSeed">The season's seed — <c>SeasonState.Seed</c>.</param>
        /// <param name="seasonNumber">The season counter, so the same fixture in a later season differs.</param>
        /// <param name="homeRating">The home club's starting-XI mean rating (<c>SquadRating</c>).</param>
        /// <param name="awayRating">The away club's starting-XI mean rating.</param>
        /// <param name="worldDay">The world-day the fixture is played on, recorded on the result.</param>
        /// <exception cref="System.ArgumentOutOfRangeException">
        /// Either rating is non-finite. Fail-loud rather than propagate a NaN into a persisted scoreline:
        /// a NaN <c>edge</c> makes both lambdas NaN, every <c>u &gt; cdf</c> comparison false, and the
        /// model silently returns 0–0 for every fixture — a wrong league table with no error anywhere.
        /// </exception>
        public static MatchResult Resolve(
            in Fixture fixture,
            ulong seasonSeed,
            int seasonNumber,
            float homeRating,
            float awayRating,
            uint worldDay)
        {
            // Negated-compare NaN gate (the project pattern): !(x > y) catches NaN where x <= y does not.
            if (!IsFinite(homeRating))
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(homeRating), homeRating, "Squad ratings must be finite.");
            }

            if (!IsFinite(awayRating))
            {
                throw new System.ArgumentOutOfRangeException(
                    nameof(awayRating), awayRating, "Squad ratings must be finite.");
            }

            ulong key = FixtureKey(
                seasonSeed, seasonNumber, fixture.RoundIndex, fixture.HomeClubId, fixture.AwayClubId);

            // dSquad is a property of the two squads and is observable from an engine match — it is the
            // axis the A4a corpus buckets on. edge adds HomeAdvantageRating, which is a parameter of THIS
            // MODEL and not a property of anything the engine knows about (KD-7 / KD-8 AR-7 H-1). Keeping
            // them textually separate is what stops a future calibration pass conflating the two.
            float dSquad = homeRating - awayRating;
            float edge = dSquad + SeasonLoopConstants.QuickSimHomeAdvantageRating;

            float lambdaHome = Lambda(+edge);
            float lambdaAway = Lambda(-edge);

            int homeGoals = PoissonInverseCdf(
                lambdaHome, UnitFloat(key ^ SeasonLoopConstants.ROUND_RESOLUTION_HOME_SUBSTREAM));
            int awayGoals = PoissonInverseCdf(
                lambdaAway, UnitFloat(key ^ SeasonLoopConstants.ROUND_RESOLUTION_AWAY_SUBSTREAM));

            return new MatchResult(
                fixture.HomeClubId, fixture.AwayClubId, homeGoals, awayGoals, fixture.RoundIndex, worldDay);
        }

        /// <summary>
        /// The per-fixture <c>matchSeed</c> a managed fixture's <c>MatchEngine</c> boots from — derived
        /// from the same fixture identity as <see cref="Resolve"/> but through a distinct domain constant,
        /// so the engine's seed and the quick-sim's draws cannot correlate for the same fixture. Keyed for
        /// the same reason: replaying a round must reboot the engine with the same seed regardless of
        /// resolution order or of a save/restore in between.
        /// </summary>
        public static ulong MatchSeedFor(
            in Fixture fixture, ulong seasonSeed, int seasonNumber)
        {
            ulong key = FixtureKey(
                seasonSeed, seasonNumber, fixture.RoundIndex, fixture.HomeClubId, fixture.AwayClubId);
            return Mix(key ^ SeasonLoopConstants.MATCH_SEED_DOMAIN);
        }

        /// <summary>
        /// The expected goals for a side at <paramref name="signedEdge"/>:
        /// <c>clamp(BaseGoals · exp(slope · edge), LambdaMin, LambdaMax)</c>. The clamps are safety
        /// bounds, not fitted parameters (KD-7).
        /// </summary>
        internal static float Lambda(float signedEdge)
        {
            float raw = SeasonLoopConstants.QuickSimBaseGoals
                * Mathf.Exp(SeasonLoopConstants.QuickSimGoalRatingSlope * signedEdge);

            if (raw < SeasonLoopConstants.QuickSimLambdaMin)
            {
                return SeasonLoopConstants.QuickSimLambdaMin;
            }

            if (raw > SeasonLoopConstants.QuickSimLambdaMax)
            {
                return SeasonLoopConstants.QuickSimLambdaMax;
            }

            return raw;
        }

        /// <summary>
        /// Poisson quantile by <b>inversion</b>: accumulate <c>p = exp(-λ)</c>, <c>cdf = p</c>, and while
        /// <c>u &gt; cdf</c> step <c>k++, p *= λ/k, cdf += p</c>, bounded by
        /// <see cref="SeasonLoopConstants.MAX_GOALS_PER_SIDE"/>.
        /// <para>
        /// <b>Named as inversion on purpose (KD-7 / AR-7 M-1).</b> "A Poisson draw" also admits Knuth's
        /// product-of-uniforms and a normal approximation, which consume different numbers of uniforms
        /// and produce different scorelines <i>from the identical key</i>. That scoreline reaches
        /// <see cref="LeagueTable"/> and is serialized, so the divergence would be a save-format
        /// divergence, not a transient one. Inversion also needs exactly ONE uniform per side.
        /// </para>
        /// </summary>
        /// <param name="lambda">Expected goals; assumed already clamped by <see cref="Lambda"/>.</param>
        /// <param name="u">A uniform in <c>[0, 1)</c>.</param>
        internal static int PoissonInverseCdf(float lambda, float u)
        {
            float p = Mathf.Exp(-lambda);
            float cdf = p;
            int k = 0;

            while (u > cdf && k < SeasonLoopConstants.MAX_GOALS_PER_SIDE)
            {
                k++;
                p *= lambda / k;
                cdf += p;
            }

            return k;
        }

        /// <summary>
        /// The fixture key: a pure function of the season and the fixture's identity, with the #16 season
        /// domain tag folded in first so these draws are domain-separated from every other subsystem's.
        /// Each field passes through a full SplitMix64 finalizer, so a one-club-id difference avalanches
        /// (adjacent fixtures must not produce correlated scorelines).
        /// </summary>
        internal static ulong FixtureKey(
            ulong seasonSeed, int seasonNumber, int roundIndex, int homeClubId, int awayClubId)
        {
            ulong h = Mix(SeasonLoopConstants.DomainTagSeasonLoop ^ seasonSeed);
            h = Mix(h ^ (ulong)(uint)seasonNumber);
            h = Mix(h ^ (ulong)(uint)roundIndex);
            h = Mix(h ^ (ulong)(uint)homeClubId);
            h = Mix(h ^ (ulong)(uint)awayClubId);
            return h;
        }

        /// <summary>
        /// A uniform in <c>[0, 1)</c> from the top 24 bits of <c>Mix(value)</c> — the 24-bit window
        /// matches float mantissa precision, and the divisor is exactly representable, so the mapping
        /// introduces no rounding bias (the <c>PassErrorCalculator</c> precedent).
        /// </summary>
        internal static float UnitFloat(ulong value)
        {
            const uint Mantissa24Mask = 0x00FFFFFFu;
            const float Mantissa24Scale = 16777216f;   // 2^24

            ulong mixed = Mix(value);
            uint top24 = (uint)(mixed >> 40) & Mantissa24Mask;
            return top24 / Mantissa24Scale;
        }

        /// <summary>SplitMix64's finalizing mix — the key-derivation step function.</summary>
        /// <remarks>
        /// <c>internal</c> rather than <c>private</c> so <see cref="SeasonLoop.DeriveNextSeasonSeed"/>
        /// reuses this finalizer instead of carrying a second copy. SplitMix64 duplication ACROSS
        /// assemblies is this project's accepted norm (four pre-existing copies, no shared helper on
        /// `deterministic-sim`), but a second copy inside the same assembly would be plain drift.
        /// </remarks>
        internal static ulong Mix(ulong value)
        {
            unchecked  // Spec #16 §3.4.4: deliberate 64-bit wrap-around; not an overflow bug
            {
                ulong z = value + 0x9E3779B97F4A7C15UL;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }

        /// <summary>
        /// Finiteness predicate. Hand-rolled rather than <c>float.IsFinite</c>, which is .NET Core 3.0+
        /// and absent from the <c>netstandard2.1</c> surface production assemblies compile against.
        /// </summary>
        private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial implementation (#30 T2 / league-bootstrap KD-7): keyed     |
// |         |            |        | fixture derivation folding DOMAIN_TAG_SEASON_LOOP (its first draw  |
// |         |            |        | site, ERR-030-001), exp-shaped lambdas with safety clamps,         |
// |         |            |        | inverse-CDF Poisson quantile with a MAX_GOALS_PER_SIDE cap, the    |
// |         |            |        | per-fixture managed matchSeed, and a fail-loud non-finite gate.    |
#endregion
