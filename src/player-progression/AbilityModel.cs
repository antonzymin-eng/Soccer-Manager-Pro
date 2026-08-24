// File:     src/player-progression/AbilityModel.cs
// Created:  2026-07-24
// Modified: 2026-08-24 (round-2 M/L adversarial findings: AgeBand enum docs, TestOnly_ renames, the
//           retirement-day feedback-loop invariant, ClassifyAgeBand -> internal + LifecycleViewModel.
//           AgeBand as the #38 read surface — v1.5)
//           (construction-day-credit-implemented-twice — the credit's one owner — v1.4)
//           (football-judgment proxy review, batch-1 adversarial findings — v1.3)
//           (ERR-028-022 — the floored-mean anti-symmetry break in GameReadingOffsetDays — v1.2)
//           (ERR-028-020 + ERR-028-021 — football-judgment proxy review batch 1 — v1.1)
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
        /// <summary>
        /// The three growth bands. No separate AgeBand.cs — §4.2 keeps it here.
        /// <para>
        /// <b>Corrected (agebandenum-docs-assert-retired-predicates, round-2 adversarial finding):</b>
        /// the three member docs below used to define the bands by the RETIRED hard age predicates —
        /// ⚠️ <c>Age &lt; GROWTH_AGE</c> / <c>GROWTH_AGE ≤ age ≤ DECLINE_AGE</c> /
        /// <c>Age &gt; DECLINE_AGE</c> (§4.3) — which ERR-028-020 made wrong at both edges and wrong in
        /// exactly the direction that ERR-028-020 is FOR: at the shipped
        /// <see cref="PlayerProgressionConstants.AgeBandRampHalfWidthYears"/> = 2,
        /// <c>ClassifyAgeBand(GROWTH_AGE)</c> now reads <c>Growth</c> (the retired predicate said
        /// <c>Stable</c>) and <c>ClassifyAgeBand(DECLINE_AGE)</c> now reads <c>Decline</c> (the retired
        /// predicate said <c>Stable</c>) — the ramp already carries both edges into their band a full
        /// half-width early. See <see cref="ClassifyAgeBand"/>'s own doc, §3.1.3, for the current rule.
        /// </para>
        /// </summary>
        public enum AgeBand
        {
            /// <summary>The calendar year's net <see cref="AccruedBandPoints(long)"/> is positive — see <see cref="ClassifyAgeBand"/> (§3.1.3).</summary>
            Growth,

            /// <summary>The calendar year's net <see cref="AccruedBandPoints(long)"/> is exactly zero — see <see cref="ClassifyAgeBand"/> (§3.1.3).</summary>
            Stable,

            /// <summary>The calendar year's net <see cref="AccruedBandPoints(long)"/> is negative — see <see cref="ClassifyAgeBand"/> (§3.1.3).</summary>
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
        /// <para>
        /// <b>Year-granular, not a per-day rate — read the answer accordingly.</b> The result is the
        /// SIGN of the whole calendar year's net accrual, quantised per day inside a ramp — reading
        /// <c>ClassifyAgeBand(25) == Growth</c> as "this player grows today" is wrong on most days of
        /// year 25, since a day inside a ramp accrues <c>{0, ±1}</c> depending on exactly which day it
        /// is. A caller that needs the per-day answer wants <see cref="DailyBandPoints(long)"/>, not
        /// this method classified at a coarser grain.
        /// </para>
        /// <para>
        /// <b>INTERNAL (round-2 adversarial finding
        /// classifyageband-public-with-no-callers-and-a-silently-changed-meaning), and here is the
        /// choice made and why.</b> This method used to be <c>public</c> with zero callers anywhere in
        /// `src/` — cross-assembly or otherwise — while its own doc above carries a three-paragraph
        /// warning that the natural reading (<c>ClassifyAgeBand(25) == Growth</c> ⇒ "he grows today")
        /// is wrong most days of the year: a public member nobody calls, with a footgun documented only
        /// in prose, is the worst of the states available. Two fixes were on the table: demote both this
        /// method and <see cref="AgeBand"/> to internal (a test/diagnostic surface only), or give #38 the
        /// read surface its own future consumer would actually want. <b>Chosen: the latter.</b>
        /// <see cref="AgeBand"/> stays <c>public</c> — it is exactly the enum a #38 player-profile screen
        /// wants to render — but the read surface for it is <c>LifecycleViewModel.AgeBand</c>, alongside
        /// the <c>RetirementFlag</c>/<c>RetirementDay</c> fields that already exist there for #31/#38
        /// (FR-PG-023). This method becomes what it always was underneath: the one place that curve-read
        /// is computed, called once per <c>LifecycleView</c> — a real, in-assembly, production caller —
        /// rather than a second surface a UI author could call directly and misread as a per-day answer.
        /// </para>
        /// </summary>
        /// <param name="ageYears">The player's derived age in whole years.</param>
        internal static AgeBand ClassifyAgeBand(int ageYears)
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
            return TestOnly_DailyBandPoints(ageDays, PlayerProgressionConstants.AgeBandRampHalfWidthYears);
        }

        /// <summary>
        /// <see cref="DailyBandPoints(long)"/> against an explicit ramp half-width, so the
        /// <c>half-width = 0</c> §4.3 identity (KD-8 / FR-PG-007) can be EXERCISED rather than
        /// asserted in prose. The catalogue value is a <c>[GT]</c> read once at static
        /// initialisation, so a test cannot vary it any other way — and an identity claim nothing
        /// executes is the class of claim this project has had falsified three times
        /// (`ERR-008-021`/`-022`).
        /// <para>
        /// <b>Named <c>TestOnly_</c> (round-2 adversarial finding
        /// test-affordance-overloads-ignore-the-TestOnly-naming-convention):</b> before this a call
        /// site inside the assembly distinguished this dial-taking form from the catalogue-reading
        /// <see cref="DailyBandPoints(long)"/> above by argument count alone — nothing marked
        /// <c>DailyBandPoints(days, 0)</c> as a test affordance rather than a legitimate production
        /// call pinning the dial off, and bypassing the catalogue is exactly what this form is for.
        /// The house convention already covers this shape (~40 <c>TestOnly_*</c> members in
        /// `MatchEngine.cs`; `agent-movement`'s <c>ToolingOverrideOnly_NaNInjection</c>) — internal +
        /// <c>InternalsVisibleTo</c> + a name that makes misuse visible.
        /// </para>
        /// </summary>
        /// <param name="ageDays">The player's age in whole days on the day being advanced.</param>
        /// <param name="rampHalfWidthYears">The ramp half-width to evaluate against, in years.</param>
        internal static long TestOnly_DailyBandPoints(long ageDays, int rampHalfWidthYears)
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
            return TestOnly_AccruedBandPoints(ageDays + 1, rampHalfWidthYears)
                   - TestOnly_AccruedBandPoints(ageDays, rampHalfWidthYears);
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
            return TestOnly_AccruedBandPoints(daysLived, PlayerProgressionConstants.AgeBandRampHalfWidthYears);
        }

        /// <summary>
        /// <see cref="AccruedBandPoints(long)"/> against an explicit ramp half-width — see
        /// <see cref="TestOnly_DailyBandPoints(long, int)"/> for why the parameterised, <c>TestOnly_</c>
        /// form exists (test-affordance-overloads-ignore-the-TestOnly-naming-convention).
        /// </summary>
        /// <param name="daysLived">Days lived; values at or below zero accrue nothing.</param>
        /// <param name="rampHalfWidthYears">The ramp half-width to evaluate against, in years.</param>
        internal static long TestOnly_AccruedBandPoints(long daysLived, int rampHalfWidthYears)
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
        /// The construction day's own band-step credit (§3.1 / §3.3, ERR-028-018) — the ONE
        /// implementation of the rule <c>ProgressionEngine.SeedLifecycle</c> and
        /// <c>RegenGenerator.GenerateRegen</c> both owe.
        /// <para>
        /// Both sites construct a <see cref="PlayerLifecycle"/> from scratch and anchor its
        /// <see cref="PlayerLifecycle.LastAdvancedWorldDay"/> at their own construction day. That anchor
        /// declares the day already lived, so it will never be replayed by the daily step — and a
        /// <c>GrowthCursor</c> of 0 therefore accounts for that day as NOTHING, shifting the accrual
        /// window one day right of every band edge and costing one whole <c>[1,20]</c> attribute point
        /// per band traversal (<c>POINT_COST == DAYS_PER_YEAR</c>, KD-8). The credit is exactly the
        /// <see cref="DailyBandPoints(long)"/> step the daily loop would have taken on that day.
        /// </para>
        /// <para>
        /// <b>It lives here, at the curve's owner, because #28 has paid twice for it living anywhere
        /// else.</b> ERR-028-018 credited the seed site and left the regen site at 0 (found a day later,
        /// AR pass 7); ERR-028-020 then had to visit both again to move each off the retired three-way
        /// band step. Both landings were one-site fixes to a two-site rule, which is the
        /// parallel-surface class (`LineupSelector.CanSelect`, the two hand-copied cursor-vs-clock
        /// walks). One owner, both sites delegating — the `PlayerCareerStates` AR-pass-9 shape.
        /// </para>
        /// </summary>
        /// <param name="ageYears">The player's age in whole years on his construction day.</param>
        public static long ConstructionDayCredit(int ageYears)
        {
            return DailyBandPoints((long)ageYears * PlayerProgressionConstants.DAYS_PER_YEAR);
        }

        /// <summary>
        /// The per-player retirement age, in days (§3.4, ERR-028-021): the league baseline plus the
        /// goalkeeper allowance plus the game-reading offset. Continuous to the day — one attribute
        /// point moves it by roughly <c>span · DAYS_PER_YEAR / (2 · (ATTRIBUTE_MAX − ATTRIBUTE_MIN))</c>
        /// days, never by a whole year (doctrine P1).
        /// <para>
        /// <b>This is a re-evaluated function of <paramref name="rec"/>, not a stored property — and
        /// <paramref name="rec"/>'s reading attributes are exactly what the daily step mutates
        /// (round-2 adversarial finding retirement-day-derived-from-attributes-the-same-step-mutates).</b>
        /// <see cref="ProgressionEngine"/> calls this once per <c>AdvanceDay</c>, against the SAME
        /// record <see cref="TrySpendOnePoint"/> / <see cref="DrainOnePoint"/> just mutated earlier in
        /// that same call — so a player's retirement day moves under him as Anticipation / Positioning /
        /// Composure rise (Growth) or fall (Decline), and re-evaluating tomorrow can return a different
        /// day than today did. Bounded today only because §3.1.2's spend/drain order keeps each
        /// attribute monotone within a band and <c>RetirementFlag</c> is sticky (no oscillation once
        /// flagged) — that bound is an accident of today's one-directional bands, not a stated invariant,
        /// and it stops holding the day a curve or authored data can move these three attributes
        /// independently of age (the T3 <c>curveEnabled</c> tier; #47's authored database). See
        /// <c>RetirementAgeDays_IsMonotonicWithinABand_AsTheAttributesItReadsAreMonotone</c> for the
        /// locked half of that bound.
        /// </para>
        /// </summary>
        /// <param name="rec">The career-state record — position and the reading attributes.</param>
        /// <exception cref="System.InvalidOperationException">
        /// The <c>[GT]</c> career-length dials are incoherent — a negative span or goalkeeper bonus, or
        /// a combination that puts the retirement day at or before birth. Checked at the one site that
        /// computes the day. <b>Not a config-unbound rationale (corrected — the premise was false for
        /// this catalogue):</b> `PlayerProgressionConstants.cs` has no `Config.GetX` call at all yet —
        /// every `[GT]` here, including the two this guard covers, is still a compile-time literal — so
        /// this is a forward-looking placement for the Stage-1 config loader, not a workaround for a
        /// catalogue lock a config-unbound gate defeats today. `PlayerProgressionConstantsTests` carries
        /// the catalogue-side lock on these same literals; this guard is what stays load-bearing once
        /// the loader lands and the catalogue lock stops seeing anything but the fallback.
        /// </exception>
        public static long RetirementAgeDays(in PlayerRecord rec)
        {
            return TestOnly_RetirementAgeDays(
                in rec,
                PlayerProgressionConstants.RetirementGoalkeeperBonusYears,
                PlayerProgressionConstants.RetirementGameReadingSpanYears);
        }

        /// <summary>
        /// <see cref="RetirementAgeDays(in PlayerRecord)"/> against explicit dial values, so the
        /// zero/zero OFF identity (P5) and both catalogue/config integrity guards can be EXERCISED
        /// rather than asserted in prose — the dials are read once at static initialisation, so no test
        /// can otherwise vary them (the `ERR-008-021`/`-022` posture, per
        /// <see cref="TestOnly_DailyBandPoints(long, int)"/>). Named <c>TestOnly_</c> for the same
        /// reason as that method (test-affordance-overloads-ignore-the-TestOnly-naming-convention).
        /// </summary>
        /// <param name="rec">The career-state record — position and the reading attributes.</param>
        /// <param name="goalkeeperBonusYears">The goalkeeper allowance to evaluate against, in years.</param>
        /// <param name="readingSpanYears">The game-reading offset's full-range span to evaluate against, in years.</param>
        internal static long TestOnly_RetirementAgeDays(in PlayerRecord rec, int goalkeeperBonusYears, int readingSpanYears)
        {
            if (readingSpanYears < 0 || goalkeeperBonusYears < 0)
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
                days += (long)goalkeeperBonusYears * PlayerProgressionConstants.DAYS_PER_YEAR;
            }

            days += TestOnly_GameReadingOffsetDays(in rec.Attributes, readingSpanYears);

            if (days <= 0)
            {
                throw new System.InvalidOperationException(
                    "The computed retirement age is at or before birth — RetirementGameReadingSpanYears "
                    + "outweighs RETIREMENT_AGE; catalogue/config integrity failure (§3.4, Appendix A).");
            }

            return days;
        }

        /// <summary>
        /// The full-range, anti-symmetric game-reading offset in days (§3.4). Reads the SUM of
        /// Anticipation / Positioning / Composure. <b>The offset changes SIGN at the attribute-range
        /// MIDPOINT, not AT it (corrected — no player can occupy the midpoint exactly):</b> the range
        /// midpoint is 10.5 at today's [1,20] bounds, which no integer attribute can equal, so no
        /// player's offset is ever exactly 0. The two means either side of it are the nearest reachable
        /// values — mean 10 gives −38 days, mean 11 gives +38, at today's span (P5 is exact over the
        /// population, not over any one player).
        /// </summary>
        /// <remarks>
        /// ERR-028-022: the sum is carried UNDIVIDED into the numerator rather than being floored to a
        /// mean first. The retired form computed <c>mean = (Ant + Pos + Comp) / 3</c> and mapped that,
        /// and <c>floor(sum / 3)</c> is NOT symmetric about the attribute midpoint — truncation always
        /// bites downward, so the map lost the anti-symmetry the P5 argument rests on everywhere off the
        /// <c>Ant == Pos == Comp</c> diagonal. Measured through the built assembly: −204,621 days over
        /// the uniform [1,20]³ product, i.e. −25.58 d/player, so the league's retirement RATE moved
        /// where the claim was that only who-retires-when moved. Carrying the sum makes the map exactly
        /// anti-symmetric (the [1,20]³ product sums to 0), and — because the sum form's numerator and
        /// denominator are both exactly 3× the mean form's whenever <c>sum == 3·mean</c>, and integer
        /// division truncates toward zero — it reproduces every diagonal value bit-for-bit.
        /// </remarks>
        /// <param name="attrs">The player's canonical [1,20] attributes.</param>
        internal static long GameReadingOffsetDays(in PlayerAttributes attrs)
        {
            return TestOnly_GameReadingOffsetDays(in attrs, PlayerProgressionConstants.RetirementGameReadingSpanYears);
        }

        /// <summary>
        /// <see cref="GameReadingOffsetDays(in PlayerAttributes)"/> against an explicit span, so
        /// <see cref="TestOnly_RetirementAgeDays(in PlayerRecord, int, int)"/> can drive it through the
        /// same dial and the zero-span identity can be exercised directly. Named <c>TestOnly_</c> for
        /// the same reason as <see cref="TestOnly_DailyBandPoints(long, int)"/>
        /// (test-affordance-overloads-ignore-the-TestOnly-naming-convention).
        /// </summary>
        /// <param name="attrs">The player's canonical [1,20] attributes.</param>
        /// <param name="spanYears">The full-range span to evaluate against, in years.</param>
        internal static long TestOnly_GameReadingOffsetDays(in PlayerAttributes attrs, int spanYears)
        {
            int sum = attrs.Anticipation + attrs.Positioning + attrs.Composure;

            long span = (long)spanYears * PlayerProgressionConstants.DAYS_PER_YEAR;
            long numer = (2L * sum
                          - 3L * (PlayerProgressionConstants.ATTRIBUTE_MIN
                                  + PlayerProgressionConstants.ATTRIBUTE_MAX)) * span;
            long denom = 6L * (PlayerProgressionConstants.ATTRIBUTE_MAX
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
        // catalogue test alone: this is a forward-looking placement for the Stage-1 config loader — the
        // [GT] is still a compile-time literal today (PlayerProgressionConstants.cs has no Config.GetX
        // call yet), but once the loader lands, the catalogue lock in PlayerProgressionConstantsTests
        // (which that test file also carries) runs config-unbound and sees only the fallback, and this
        // computing-site guard is what stays load-bearing. Overlapping ramps are not merely untidy — a
        // day inside both accrues growth and decline at once, which the arithmetic represents and no
        // football reading does.
        private static long RampHalfWidthDays(int halfWidthYears)
        {
            if (halfWidthYears < 0)
            {
                throw new System.InvalidOperationException(
                    "AgeBandRampHalfWidthYears must be non-negative — a negative half-width inverts "
                    + "the ramp; catalogue/config integrity failure (§3.1, Appendix A).");
            }

            // ERR-028 review finding "ramp-guard-int-overflow": the comparison MUST run in `long`. As
            // `int` arithmetic, `2 * halfWidthYears` wraps negative for halfWidthYears >= 2^30, which
            // defeats this guard silently (measured: DailyBandPoints(1000, 1_073_741_824) then returns 0
            // where +1 is correct, and AccruedBandPoints(1000/1001, 1_200_000_000) collide on the same
            // garbage value because GrowthPhaseDays' own `u * u` term then overflows `long` too, at
            // u ~ 4.4e11). Casting both sides to `long` here is what keeps the guard load-bearing over
            // its own full `int` parameter domain, not merely over the plausible-bad-config range.
            long edgeSpanYears = (long)PlayerProgressionConstants.DECLINE_AGE + 1
                                  - PlayerProgressionConstants.GROWTH_AGE;
            if (2L * halfWidthYears > edgeSpanYears)
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
// | 1.2     | 2026-08-22 | —      | ERR-028-022. GameReadingOffsetDays carries the UNDIVIDED
// |         |            |        | Anticipation+Positioning+Composure sum into the numerator instead of
// |         |            |        | flooring it to a mean first. floor(sum/3) is not symmetric about the
// |         |            |        | attribute midpoint, so v1.1's map was anti-symmetric only on the
// |         |            |        | Ant==Pos==Comp diagonal — the one line the lock swept — and the
// |         |            |        | ERR-028-021 "offsets sum to exactly 0" P5 claim was false off it:
// |         |            |        | -204,621 days over the uniform [1,20]^3 product (-25.58 d/player),
// |         |            |        | i.e. the whole league retiring early rather than only who-when
// |         |            |        | moving. The new form sums to exactly 0 over that product and
// |         |            |        | reproduces every diagonal value bit-for-bit (numerator and
// |         |            |        | denominator are both exactly 3x v1.1's when sum == 3*mean).
// | 1.3     | 2026-08-23 | —      | Football-judgment proxy review, batch-1 adversarial findings, spec +
// |         |            |        | code same commit. RampHalfWidthDays' disjointness guard now compares
// |         |            |        | in `long` — as `int` arithmetic, `2 * halfWidthYears` wrapped negative
// |         |            |        | at halfWidthYears >= 2^30 and defeated the guard silently, after which
// |         |            |        | GrowthPhaseDays' own u*u term overflowed `long` too and returned
// |         |            |        | garbage on a public API (ramp-guard-int-overflow). + new INTERNAL
// |         |            |        | RetirementAgeDays(in rec, goalkeeperBonusYears, readingSpanYears) and
// |         |            |        | GameReadingOffsetDays(in attrs, spanYears) overloads, so the two
// |         |            |        | retirement dials are exercised through a parameterised overload like
// |         |            |        | DailyBandPoints/AccruedBandPoints already were, instead of reading the
// |         |            |        | catalogue statics directly where no test could vary them
// |         |            |        | (retirement-dials-no-overload). DailyBandPoints(long,int),
// |         |            |        | AccruedBandPoints(long,int) and GameReadingOffsetDays(in attrs) demoted
// |         |            |        | PUBLIC -> INTERNAL (FR-CS-015 — no cross-assembly caller; see the new
// |         |            |        | AssemblyInfo.cs InternalsVisibleTo) (classifyageband-no-production-
// |         |            |        | caller). CORRECTED 2026-08-24 (round-2 finding classifyageband-public-
// |         |            |        | with-no-callers-and-a-silently-changed-meaning): the tag above names
// |         |            |        | ClassifyAgeBand's OWN finding, but the sentence it tags demoted three
// |         |            |        | OTHER methods — ClassifyAgeBand itself stayed PUBLIC at this version,
// |         |            |        | with zero callers anywhere in src/ and a three-paragraph misuse
// |         |            |        | warning in its own doc. Not addressed until v1.5, below.
// |         |            |        | ClassifyAgeBand's doc now states its answer is year-granular,
// |         |            |        | not a per-day rate. GameReadingOffsetDays' doc corrected — the offset
// |         |            |        | changes SIGN at the attribute midpoint 10.5, which no integer attribute
// |         |            |        | can occupy, so no player's offset is ever exactly 0 (midpoint-offset-
// |         |            |        | zero-unattainable). RetirementAgeDays' <exception> doc and
// |         |            |        | RampHalfWidthDays' comment corrected — PlayerProgressionConstants.cs has
// |         |            |        | zero Config.GetX calls today, so "the catalogue lock runs config-unbound"
// |         |            |        | was a false premise for #28 (copied from ERR-041-003 without checking);
// |         |            |        | restated as a forward-looking placement for the Stage-1 loader
// |         |            |        | (config-unbound-premise-false-28). No format version, no draw, no
// |         |            |        | numeric value changed.
// | 1.4     | 2026-08-24 | —      | Round-2 adversarial finding construction-day-credit-implemented-
// |         |            |        | twice (High). + ConstructionDayCredit(int ageYears): the ONE
// |         |            |        | implementation of the ERR-028-018 rule both PlayerLifecycle
// |         |            |        | construction sites owe. RegenGenerator.BandStepFor and the
// |         |            |        | expression inlined in ProgressionEngine.SeedLifecycle were
// |         |            |        | character-for-character the same rule written twice, and
// |         |            |        | BandStepFor's own doc named itself the shared owner of a rule it
// |         |            |        | did not own. #28 had already paid for that duplication twice
// |         |            |        | (ERR-028-018 fixed the seed site and missed the regen site;
// |         |            |        | ERR-028-020 then had to visit both again). BandStepFor is DELETED
// |         |            |        | and both sites now call this method. Behaviour-identical by
// |         |            |        | construction and verified by probe before the collapse (the two
// |         |            |        | implementations agreed on every age 0..200 and on the int
// |         |            |        | domain's edges, int.MinValue/MaxValue included). No numeric
// |         |            |        | value, no draw, no format version, no access-modifier widening.
// | 1.5     | 2026-08-24 | —      | Round-2 Medium/Low adversarial findings (spec + code together).
// |         |            |        | M1 (agebandenum-docs-assert-retired-predicates): AgeBand's three
// |         |            |        | member docs rewritten off the retired §4.3 age predicates (wrong
// |         |            |        | at both edges since ERR-028-020) onto the current rule — the sign
// |         |            |        | of the year's net AccruedBandPoints, per ClassifyAgeBand (§3.1.3).
// |         |            |        | M3 (test-affordance-overloads-ignore-the-TestOnly-naming-
// |         |            |        | convention): the four dial-taking internal overloads renamed to
// |         |            |        | the house TestOnly_ convention — TestOnly_DailyBandPoints,
// |         |            |        | TestOnly_AccruedBandPoints, TestOnly_RetirementAgeDays,
// |         |            |        | TestOnly_GameReadingOffsetDays — so a call site can no longer
// |         |            |        | mistake a catalogue-bypassing test affordance for a production
// |         |            |        | call distinguished by argument count alone; also removes the
// |         |            |        | overload-resolution hazard where a public form gaining a
// |         |            |        | parameter would silently rebind existing calls. Production
// |         |            |        | (catalogue-reading) forms untouched. M5 (retirement-day-derived-
// |         |            |        | from-attributes-the-same-step-mutates): RetirementAgeDays' doc
// |         |            |        | states the feedback loop explicitly — it re-reads rec.Attributes,
// |         |            |        | which TrySpendOnePoint/DrainOnePoint mutate earlier in the same
// |         |            |        | AdvancePlayerTo call, bounded today only by the one-directional
// |         |            |        | spend/drain order and RetirementFlag's stickiness, not by any
// |         |            |        | stated invariant; a monotonicity lock added
// |         |            |        | (RetirementAgeDays_IsMonotonicWithinABand_AsTheAttributesItReads
// |         |            |        | AreMonotone, AbilityModelTests.cs). M7
// |         |            |        | (classifyageband-public-with-no-callers-and-a-silently-changed-
// |         |            |        | meaning): DECIDED — ClassifyAgeBand demoted PUBLIC -> INTERNAL
// |         |            |        | (it had zero callers anywhere in src/, cross-assembly or not, and
// |         |            |        | a footgun documented only in prose); AgeBand stays PUBLIC, but its
// |         |            |        | read surface moves to the new LifecycleViewModel.AgeBand field
// |         |            |        | (LifecycleViewModel.cs v1.1) alongside RetirementFlag/RetirementDay
// |         |            |        | — the #31/#38 surface this project already uses for exactly this
// |         |            |        | shape — computed once per ProgressionEngine.LifecycleView call,
// |         |            |        | which is ClassifyAgeBand's first production caller. The v1.3 row's
// |         |            |        | (classifyageband-no-production-caller) tag is annotated above as
// |         |            |        | misleading — it named a different demotion. No numeric value, no
// |         |            |        | draw, no format version.
#endregion
