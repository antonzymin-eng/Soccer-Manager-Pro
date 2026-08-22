// File:     src/player-progression/AbilityModel.cs
// Created:  2026-07-24
// Modified: 2026-08-22 (ERR-028-020 + ERR-028-021 — football-judgment proxy review batch 1 — v1.1)
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.1.2 / §3.2 (CA/PA model + weighted spend); Code Standards #20
// Purpose:  Pure, draw-free ability arithmetic: the derived CurrentAbility summary, the age-band
//           classifier, and the deterministic weighted attribute spend/drain. Runs on the world tick
//           (day cadence), NOT the 60 Hz hot path — plain arrays are fine here (KD-6 class).

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression
{
    /// <summary>
    /// Pure ability arithmetic for #28 (§3.1.2 / §3.2). <see cref="ComputeCA"/> derives the CurrentAbility
    /// cache from the [1,20] attributes (never a second accumulator, FR-PG-003); <see cref="ClassifyAgeBand"/>
    /// maps a derived age to its growth band; <see cref="TrySpendOnePoint"/> / <see cref="DrainOnePoint"/>
    /// are the deterministic attribute mutations the daily step drives.
    /// </summary>
    public static class AbilityModel
    {
        /// <summary>The three growth bands. No separate AgeBand.cs — §4.2 keeps it here.</summary>
        public enum AgeBand
        {
            /// <summary>Age &lt; GROWTH_AGE: gains points (§4.3).</summary>
            Growth,

            /// <summary>GROWTH_AGE ≤ age ≤ DECLINE_AGE: no change (§4.3).</summary>
            Stable,

            /// <summary>Age &gt; DECLINE_AGE: loses points (§4.3).</summary>
            Decline
        }

        /// <summary>
        /// Describes which band a derived age falls in, by READING the continuous accrual curve rather
        /// than by classifying the age independently of it (ERR-028-020).
        /// <para>
        /// <b>This is deliberately no longer the accrual authority</b>, and the indirection is the
        /// point. Before ERR-028-020 this method decided the daily rate, so the rate stepped
        /// discontinuously at an exact integer age; the rate now comes from
        /// <see cref="DailyBandPoints"/>, and re-deriving a band here from `GROWTH_AGE`/`DECLINE_AGE`
        /// would be a second surface answering the same question — the parallel-surface trap this
        /// project has filed three times (`SquadRating`/`LineupSelector.CanSelect` being the nearest).
        /// The band is therefore the SIGN of the year's own net accrual: positive ⇒ Growth, negative ⇒
        /// Decline, zero ⇒ Stable. A whole year rather than a single day, because inside a ramp the
        /// per-day accrual is quantised to <c>{0, ±1}</c> and adjacent days differ.
        /// </para>
        /// <para>
        /// At <see cref="PlayerProgressionConstants.AgeBandRampHalfWidthYears"/> = 0 this reproduces
        /// the retired predicate exactly: every year below `GROWTH_AGE` nets a full year of growth,
        /// every year above `DECLINE_AGE` a full year of decline, and the years between net zero.
        /// </para>
        /// </summary>
        /// <param name="ageYears">The player's derived age in whole years.</param>
        public static AgeBand ClassifyAgeBand(int ageYears)
        {
            long from = (long)ageYears * PlayerProgressionConstants.DAYS_PER_YEAR;
            long net = AccruedBandPoints(from + PlayerProgressionConstants.DAYS_PER_YEAR)
                       - AccruedBandPoints(from);

            if (net > 0)
            {
                return AgeBand.Growth;
            }
            if (net < 0)
            {
                return AgeBand.Decline;
            }
            return AgeBand.Stable;
        }

        /// <summary>
        /// The age-continuous daily cursor accrual (§3.1 step 2, ERR-028-020) — the single authority on
        /// how much a player's <c>GrowthCursor</c> moves on the day he is <paramref name="ageDays"/>
        /// old.
        /// <para>
        /// <b>Computed as a difference of a cumulative integral, not as a rate.</b> The football
        /// judgment "how fast is this player still developing" is continuous in age, but the cursor is
        /// integer fixed-point at a scale where one day of full growth is one unit (FR-PG-002,
        /// <c>POINT_COST = DAYS_PER_YEAR</c>), so a rate expressed per-day could not represent
        /// anything between 0 and 1. Taking the difference of an exact integer cumulative —
        /// <see cref="AccruedBandPoints"/> — instead gives a per-day step in <c>{0, ±1}</c> whose
        /// DENSITY follows the continuous curve exactly, with no rounding drift over any span and no
        /// rescaling of the persisted cursor (so no save-format change; the ERR-028-004 block is
        /// untouched).
        /// </para>
        /// </summary>
        /// <param name="ageDays">The player's age in whole days on the day being advanced.</param>
        public static long DailyBandPoints(long ageDays)
        {
            return DailyBandPoints(ageDays, PlayerProgressionConstants.AgeBandRampHalfWidthYears);
        }

        /// <summary>
        /// <see cref="DailyBandPoints(long)"/> against an explicit ramp half-width, so the
        /// <c>half-width = 0</c> §4.3 identity (KD-8 / FR-PG-007) can be EXERCISED rather than
        /// asserted in prose. The catalogue value is a <c>[GT]</c> read once at static
        /// initialisation, so a test cannot vary it any other way — and an identity claim nothing
        /// executes is the class of claim this project has had falsified three times
        /// (`ERR-008-021`/`-022`).
        /// </summary>
        /// <param name="ageDays">The player's age in whole days on the day being advanced.</param>
        /// <param name="rampHalfWidthYears">The ramp half-width to evaluate against, in years.</param>
        public static long DailyBandPoints(long ageDays, int rampHalfWidthYears)
        {
            // The saturation belongs HERE, on the age, and not inside AccruedBandPoints on the
            // cumulative — this is a real difference and it took a re-read to notice. §3.1.1's age
            // narrowing saturates at MAX_DERIVABLE_AGE_YEARS, so an anchor beyond that ceiling reports
            // a pinned age; under the RETIRED band step that pinned age classified as Decline and the
            // player kept draining a point a year. If the ceiling were applied to the cumulative
            // instead, both terms of the difference below would clamp to the same value and such a
            // player would silently stop declining altogether — a behaviour change nothing in the
            // football range could ever surface. Clamping the AGE to one day inside the ceiling makes
            // his daily step the step AT the ceiling, which is the full decline rate, exactly as before.
            long ceiling = (long)PlayerProgressionConstants.MAX_DERIVABLE_AGE_YEARS
                           * PlayerProgressionConstants.DAYS_PER_YEAR;
            if (ageDays >= ceiling)
            {
                ageDays = ceiling - 1;
            }

            // AccruedBandPoints counts days LIVED, so the day on which the player is `ageDays` old is
            // the (ageDays + 1)-th: its own contribution is the cumulative through it minus the
            // cumulative through the day before.
            return AccruedBandPoints(ageDays + 1, rampHalfWidthYears)
                   - AccruedBandPoints(ageDays, rampHalfWidthYears);
        }

        /// <summary>
        /// The exact cumulative cursor accrual over the first <paramref name="daysLived"/> days of a
        /// player's life (§3.1, ERR-028-020) — the integral <see cref="DailyBandPoints"/> differences.
        /// <para>
        /// <b>The P5 pivot lives here.</b> Both phase integrals are centred on their old step edge, so
        /// the TOTAL growth-days over a whole life is <c>GROWTH_AGE · DAYS_PER_YEAR</c> for every
        /// half-width including 0, and the total decline-days past the decline edge likewise. The ramp
        /// therefore redistributes accrual across an edge without creating or destroying any — a
        /// completed traversal still gains exactly one attribute-point per year of the band and still
        /// leaves no residue (ERR-028-018's invariant, preserved by construction rather than re-fitted).
        /// </para>
        /// </summary>
        /// <param name="daysLived">Days lived; values at or below zero accrue nothing.</param>
        public static long AccruedBandPoints(long daysLived)
        {
            return AccruedBandPoints(daysLived, PlayerProgressionConstants.AgeBandRampHalfWidthYears);
        }

        /// <summary>
        /// <see cref="AccruedBandPoints(long)"/> against an explicit ramp half-width — see
        /// <see cref="DailyBandPoints(long, int)"/> for why the parameterised form exists.
        /// </summary>
        /// <param name="daysLived">Days lived; values at or below zero accrue nothing.</param>
        /// <param name="rampHalfWidthYears">The ramp half-width to evaluate against, in years.</param>
        public static long AccruedBandPoints(long daysLived, int rampHalfWidthYears)
        {
            if (daysLived <= 0)
            {
                return 0;
            }

            // No representability ceiling here, deliberately: the cumulative is a `long` and both
            // branches are written so it cannot overflow (the growth phase is bounded by `g`, the
            // decline phase by `n − e`, and the squared terms by `(2h)²`). Saturating the CUMULATIVE
            // would make two adjacent days beyond the ceiling clamp to the same value and their
            // difference vanish, which is why the age ceiling lives in DailyBandPoints instead — see
            // the note there.
            long h = RampHalfWidthDays(rampHalfWidthYears);
            return GrowthPhaseDays(daysLived, h) * PlayerProgressionConstants.GROWTH_DAILY_POINTS
                   + DeclinePhaseDays(daysLived, h) * PlayerProgressionConstants.DECLINE_DAILY_POINTS;
        }

        /// <summary>
        /// The per-player retirement age, in days (§3.4, ERR-028-021): the league baseline plus the
        /// goalkeeper allowance plus the game-reading offset. Continuous to the day — one attribute
        /// point moves it by roughly <c>span · DAYS_PER_YEAR / (2 · (ATTRIBUTE_MAX − ATTRIBUTE_MIN))</c>
        /// days, never by a whole year (doctrine P1).
        /// </summary>
        /// <param name="rec">The career-state record — position and the reading attributes.</param>
        /// <exception cref="System.InvalidOperationException">
        /// The <c>[GT]</c> career-length dials are incoherent — a negative span or goalkeeper bonus, or
        /// a combination that puts the retirement day at or before birth. A catalogue/config integrity
        /// failure rather than a caller error, checked at the one site that computes the day (the
        /// <c>MedicalStep.DrawOccurrence</c> guard posture): the catalogue locks run config-unbound and
        /// see only the fallbacks, so a shipped config could otherwise retire an entire league on the
        /// day it is generated.
        /// </exception>
        public static long RetirementAgeDays(in PlayerRecord rec)
        {
            if (PlayerProgressionConstants.RetirementGameReadingSpanYears < 0
                || PlayerProgressionConstants.RetirementGoalkeeperBonusYears < 0)
            {
                throw new System.InvalidOperationException(
                    "RetirementGameReadingSpanYears and RetirementGoalkeeperBonusYears must be "
                    + "non-negative — a negative span retires the best readers of the game first and a "
                    + "negative bonus shortens a goalkeeper's career; catalogue/config integrity "
                    + "failure (§3.4, Appendix A).");
            }

            long days = (long)PlayerProgressionConstants.RETIREMENT_AGE
                        * PlayerProgressionConstants.DAYS_PER_YEAR;

            if (rec.Position == PlayerPosition.Goalkeeper)
            {
                days += (long)PlayerProgressionConstants.RetirementGoalkeeperBonusYears
                        * PlayerProgressionConstants.DAYS_PER_YEAR;
            }

            days += GameReadingOffsetDays(in rec.Attributes);

            if (days <= 0)
            {
                throw new System.InvalidOperationException(
                    "The computed retirement age is at or before birth — RetirementGameReadingSpanYears "
                    + "outweighs RETIREMENT_AGE; catalogue/config integrity failure (§3.4, Appendix A).");
            }

            return days;
        }

        /// <summary>
        /// The full-range, anti-symmetric game-reading offset in days (§3.4). Mean of Anticipation /
        /// Positioning / Composure; at the attribute midpoint the offset is 0, so an average outfielder
        /// retires on exactly today's day (P5).
        /// </summary>
        /// <param name="attrs">The player's canonical [1,20] attributes.</param>
        public static long GameReadingOffsetDays(in PlayerAttributes attrs)
        {
            int mean = (attrs.Anticipation + attrs.Positioning + attrs.Composure) / 3;

            long span = (long)PlayerProgressionConstants.RetirementGameReadingSpanYears
                        * PlayerProgressionConstants.DAYS_PER_YEAR;
            long numer = (2L * mean
                          - (PlayerProgressionConstants.ATTRIBUTE_MIN
                             + PlayerProgressionConstants.ATTRIBUTE_MAX)) * span;
            long denom = 2L * (PlayerProgressionConstants.ATTRIBUTE_MAX
                               - PlayerProgressionConstants.ATTRIBUTE_MIN);

            return numer / denom;
        }

        // Growth-days accrued over the first `n` days of life — the integral of a rate that is 1.0 up
        // to `g − h`, falls linearly to 0 at `g + h`, and is 0 thereafter, where `g` is the old
        // GROWTH_AGE edge in days and `h` the ramp half-width. Written in the shifted variable
        // `u = n − (g − h)` rather than in `n` so the squared term is bounded by `(2h)²` — in `n` it
        // would overflow `long` for an anchor near MAX_DERIVABLE_AGE_YEARS.
        private static long GrowthPhaseDays(long n, long h)
        {
            long g = (long)PlayerProgressionConstants.GROWTH_AGE * PlayerProgressionConstants.DAYS_PER_YEAR;

            if (h <= 0)
            {
                return n < g ? n : g;   // the exact §4.3 step: day k accrues iff k / DAYS_PER_YEAR < GROWTH_AGE
            }
            if (n <= g - h)
            {
                return n;
            }
            if (n >= g + h)
            {
                return g;               // the centred ramp's total equals the step's total — the P5 pivot
            }

            long u = n - (g - h);
            return (g - h) + u - (u * u) / (4 * h);
        }

        // Decline-days accrued over the first `n` days of life — the mirror integral, rising from 0 at
        // `e − h` to 1.0 at `e + h` and 1.0 thereafter, where `e = (DECLINE_AGE + 1) · DAYS_PER_YEAR`
        // is the old edge in days (the retired predicate was `ageYears > DECLINE_AGE`, so the first
        // declining day is the first day of age DECLINE_AGE + 1).
        private static long DeclinePhaseDays(long n, long h)
        {
            long e = ((long)PlayerProgressionConstants.DECLINE_AGE + 1)
                     * PlayerProgressionConstants.DAYS_PER_YEAR;

            if (h <= 0)
            {
                return n > e ? n - e : 0;
            }
            if (n <= e - h)
            {
                return 0;
            }
            if (n >= e + h)
            {
                return n - e;
            }

            long v = n - (e - h);
            return (v * v) / (4 * h);
        }

        // The ramp half-width in days, with the disjointness invariant enforced HERE rather than in a
        // catalogue test: the [GT] is a config key and the catalogue lock runs config-unbound, so it
        // sees the fallback forever while a shipped config overlaps the two ramps (ERR-041-003's class,
        // and the DrawOccurrence guard posture's sixth instance). Overlapping ramps are not merely
        // untidy — a day inside both accrues growth and decline at once, which the arithmetic
        // represents and no football reading does.
        private static long RampHalfWidthDays(int halfWidthYears)
        {
            if (halfWidthYears < 0)
            {
                throw new System.InvalidOperationException(
                    "AgeBandRampHalfWidthYears must be non-negative — a negative half-width inverts "
                    + "the ramp; catalogue/config integrity failure (§3.1, Appendix A).");
            }

            int edgeSpanYears = PlayerProgressionConstants.DECLINE_AGE + 1
                                - PlayerProgressionConstants.GROWTH_AGE;
            if (2 * halfWidthYears > edgeSpanYears)
            {
                throw new System.InvalidOperationException(
                    "AgeBandRampHalfWidthYears is too wide — 2 x half-width must not exceed "
                    + "(DECLINE_AGE + 1) - GROWTH_AGE, or the growth and decline ramps overlap and a "
                    + "day accrues both; catalogue/config integrity failure (§3.1, Appendix A).");
            }

            return (long)halfWidthYears * PlayerProgressionConstants.DAYS_PER_YEAR;
        }

        /// <summary>
        /// The derived CurrentAbility summary (§3.2): the position-weighted mean of the 31 [1,20]
        /// attributes, mapped linearly [ATTRIBUTE_MIN, ATTRIBUTE_MAX] → [0, ABILITY_MAX] with integer
        /// floor division. Weights are <c>1 + PositionAttributeBias</c> so a position's signature
        /// attributes count more. Integer-only, so a restore recomputes it bit-exact (FR-PG-003).
        /// </summary>
        /// <param name="attrs">The player's canonical [1,20] attributes.</param>
        /// <param name="pos">The player's coarse position (indexes the bias/weight table).</param>
        public static int ComputeCA(in PlayerAttributes attrs, PlayerPosition pos)
        {
            return ComputeCAFromArray(attrs.ToArray(), pos);
        }

        // The exact weighting is a §3.2 [GT] balance detail; the shape (weight = 1 + bias, linear scale)
        // is the contract. Operates on an AttrIdx-ordered array so it can be reused during spend/drain
        // candidate evaluation without a PlayerAttributes round-trip per candidate.
        private static int ComputeCAFromArray(int[] a, PlayerPosition pos)
        {
            int[] bias = PlayerDatabaseConstants.PositionAttributeBias[(int)pos];
            long numer = 0;   // Σ weight_i * attr_i
            long sumW = 0;    // Σ weight_i
            for (int i = 0; i < AttrIdx.Count; i++)
            {
                long w = 1 + bias[i];
                numer += w * a[i];
                sumW += w;
            }
            long span = PlayerProgressionConstants.ATTRIBUTE_MAX - PlayerProgressionConstants.ATTRIBUTE_MIN;
            long scaled = (numer - sumW * PlayerProgressionConstants.ATTRIBUTE_MIN)
                          * PlayerProgressionConstants.ABILITY_MAX
                          / (sumW * span);
            return (int)scaled;
        }

        /// <summary>
        /// Raises the next attribute by the deterministic weighted order (§3.1.2): highest
        /// <c>PositionAttributeBias</c> weight first, ties by ascending <see cref="AttrIdx"/>. An
        /// attribute at ATTRIBUTE_MAX, or whose +1 raise would push the derived CA past
        /// <c>lifecycle.PotentialAbility</c>, is skipped (F1). Signature mirrors the §3.1 pseudocode's
        /// <c>(ref record, ref lifecycle)</c>.
        /// </summary>
        /// <returns><c>true</c> if a point was spent; <c>false</c> if none is raisable (caller leaves the cursor — no thrash).</returns>
        public static bool TrySpendOnePoint(ref PlayerRecord rec, ref PlayerLifecycle life)
        {
            int[] bias = PlayerDatabaseConstants.PositionAttributeBias[(int)rec.Position];
            int[] a = rec.Attributes.ToArray();
            int maxBias = MaxBias(bias);

            // Highest bias level first, ties ascending index: pick the first attribute below MAX whose
            // raise keeps CA ≤ PA.
            for (int level = maxBias; level >= 0; level--)
            {
                for (int i = 0; i < AttrIdx.Count; i++)
                {
                    if (bias[i] != level || a[i] >= PlayerProgressionConstants.ATTRIBUTE_MAX)
                    {
                        continue;
                    }
                    a[i] += 1;
                    if (ComputeCAFromArray(a, rec.Position) <= life.PotentialAbility)
                    {
                        CommitAttributes(ref rec, a);
                        return true;
                    }
                    a[i] -= 1; // overshoots PA — revert and try the next candidate
                }
            }
            return false;
        }

        /// <summary>
        /// Symmetric decline (§3.1): lowers the next attribute by the mirror order — lowest
        /// <c>PositionAttributeBias</c> weight first, ties by ascending <see cref="AttrIdx"/> — so a
        /// declining player sheds their least-emphasised attributes first. An attribute at ATTRIBUTE_MIN
        /// is skipped; a fully-drained player is a no-op (the caller's cursor still advances toward 0).
        /// </summary>
        /// <returns>
        /// <c>true</c> when a point was drained; <c>false</c> when every attribute already sits at
        /// <c>ATTRIBUTE_MIN</c> and there is nothing left to take.
        /// <para>
        /// It returns a result at all because of AR pass 6's High: as a <c>void</c> no-op this method
        /// gave the caller's drain loop NO failure exit, so a large negative cursor spun it once per
        /// <c>POINT_COST</c> with no diagnostic — 1.26e13 iterations (~70 days of CPU) for a cursor of
        /// <c>long.MinValue/2</c>. The spend side has had a refusal exit since AR pass 5's M2 clamp;
        /// this is the mirror it never got.
        /// </para>
        /// </returns>
        public static bool DrainOnePoint(ref PlayerRecord rec, ref PlayerLifecycle life)
        {
            int[] bias = PlayerDatabaseConstants.PositionAttributeBias[(int)rec.Position];
            int[] a = rec.Attributes.ToArray();
            int maxBias = MaxBias(bias);

            for (int level = 0; level <= maxBias; level++)
            {
                for (int i = 0; i < AttrIdx.Count; i++)
                {
                    if (bias[i] != level || a[i] <= PlayerProgressionConstants.ATTRIBUTE_MIN)
                    {
                        continue;
                    }
                    a[i] -= 1;
                    CommitAttributes(ref rec, a);
                    return true;
                }
            }

            return false;
        }

        private static int MaxBias(int[] bias)
        {
            int max = 0;
            for (int i = 0; i < AttrIdx.Count; i++)
            {
                if (bias[i] > max)
                {
                    max = bias[i];
                }
            }
            return max;
        }

        private static void CommitAttributes(ref PlayerRecord rec, int[] a)
        {
            PlayerAttributes attrs = rec.Attributes;
            attrs.FromArray(a);
            rec.Attributes = attrs;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-24 | —      | Initial implementation. |
// | 1.1     | 2026-08-22 | —      | ERR-028-020 / ERR-028-021. + DailyBandPoints / AccruedBandPoints (the
// |         |            |        | age-continuous accrual of §3.1.3, as the first difference of an exact
// |         |            |        | integer cumulative — so the per-day step stays in {0, +-1} and the
// |         |            |        | persisted cursor's scale, hence the save format, is untouched) and their
// |         |            |        | two phase integrals; + RetirementAgeDays / GameReadingOffsetDays (§3.4).
// |         |            |        | ClassifyAgeBand rewritten as a READ of the curve (the sign of the year's
// |         |            |        | net accrual) rather than a second authority over the same question.
// |         |            |        | The MAX_DERIVABLE_AGE_YEARS ceiling is applied to the AGE in
// |         |            |        | DailyBandPoints, NOT to the cumulative: saturating the cumulative
// |         |            |        | would clamp both terms of the difference and an impossibly-old
// |         |            |        | player would silently stop declining, where the retired band step
// |         |            |        | kept him at the full decline rate. Locked.
#endregion
