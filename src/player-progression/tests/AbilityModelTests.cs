// File:     src/player-progression/tests/AbilityModelTests.cs
// Created:  2026-07-24
// Modified: 2026-08-24 (round-2 M/L adversarial findings: TestOnly_ renames (M3), the retirement-day
//           monotonicity lock (M5), the four-guards comment correction (L1) — v1.5)
//           (construction-day-credit-implemented-twice — the credit's owner lock — v1.4)
//           (football-judgment proxy review, batch-1 adversarial findings — v1.3)
//           (ERR-028-022 — the P5 population sweep widened to the whole attribute product — v1.2)
//           (ERR-028-020 + ERR-028-021 — football-judgment proxy review batch 1 — v1.1)
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.1.2 / §3.2; Code Standards #20
// Purpose:  T-PG-CA-001/002/003 — the derived CA summary, the F1 PA-ceiling clamp, and the deterministic
//           weighted spend/drain order.

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression.Tests
{
    [TestFixture]
    public sealed class AbilityModelTests
    {
        // A neutral all-10 Midfielder: weighted mean = 10 for any weighting, mapped
        // (10 − 1)/(20 − 1) * 10000 = 4736 (integer floor). Uniform attributes ⇒ CA is position-independent.
        private const int NeutralCa = 4736;

        // The ramp edges in years, from the catalogue rather than from literals, so this file states
        // the CONTRACT and not one fitted half-width (ERR-028-020).
        private static readonly int Ramp = PlayerProgressionConstants.AgeBandRampHalfWidthYears;
        private static readonly int GrowthEnds = PlayerProgressionConstants.GROWTH_AGE + Ramp;
        private static readonly int DeclineFull = PlayerProgressionConstants.DECLINE_AGE + 1 + Ramp;

        [Test]
        public void ClassifyAgeBand_ReadsTheContinuousCurve_NotAFixedEdge()
        {
            // REBASELINED at ERR-028-020, and the rebaseline IS the fix. This test previously asserted
            // ClassifyAgeBand(GROWTH_AGE) == Stable and ClassifyAgeBand(DECLINE_AGE) == Stable — i.e.
            // it asserted the cliff: development stopping outright on a birthday. Both now sit INSIDE
            // their ramp, so both still accrue, and the two assertions below are the two that fail
            // against the pre-fix model.
            Assert.AreEqual(
                AbilityModel.AgeBand.Growth,
                AbilityModel.ClassifyAgeBand(PlayerProgressionConstants.GROWTH_AGE),
                "a player is still developing on the day he turns GROWTH_AGE — the edge is the ramp's "
                + "midpoint, not a wall.");
            Assert.AreEqual(
                AbilityModel.AgeBand.Decline,
                AbilityModel.ClassifyAgeBand(PlayerProgressionConstants.DECLINE_AGE),
                "and decline has already begun by DECLINE_AGE, for the mirror reason.");

            // Outside the ramps the bands are what they always were.
            Assert.AreEqual(
                AbilityModel.AgeBand.Growth,
                AbilityModel.ClassifyAgeBand(PlayerProgressionConstants.GROWTH_AGE - Ramp - 1));
            Assert.AreEqual(AbilityModel.AgeBand.Decline, AbilityModel.ClassifyAgeBand(DeclineFull));

            // …and a genuinely stable stretch survives between them, or the ramps have swallowed the
            // peak years entirely.
            Assert.AreEqual(AbilityModel.AgeBand.Stable, AbilityModel.ClassifyAgeBand(GrowthEnds));
            Assert.AreEqual(
                AbilityModel.AgeBand.Stable,
                AbilityModel.ClassifyAgeBand(PlayerProgressionConstants.DECLINE_AGE - Ramp));
        }

        [Test]
        public void AgeCurve_AtZeroHalfWidth_IsTheLiteralSection43Step_KD8()
        {
            // FR-PG-007 / KD-8's identity, EXECUTED. The dial's off position must reproduce the
            // retired three-way band step for every day of a football lifetime — not approximately,
            // and not only in the limit (#30 KD-7a's posture, and #41 FR-MD-027's).
            for (int ageDays = 0; ageDays <= 45 * PlayerProgressionConstants.DAYS_PER_YEAR; ageDays++)
            {
                int ageYears = ageDays / PlayerProgressionConstants.DAYS_PER_YEAR;
                long expected =
                    ageYears < PlayerProgressionConstants.GROWTH_AGE
                        ? PlayerProgressionConstants.GROWTH_DAILY_POINTS
                        : ageYears > PlayerProgressionConstants.DECLINE_AGE
                            ? PlayerProgressionConstants.DECLINE_DAILY_POINTS
                            : 0;

                Assert.AreEqual(
                    expected,
                    AbilityModel.TestOnly_DailyBandPoints(ageDays, rampHalfWidthYears: 0),
                    $"day {ageDays} (age {ageYears}) must reproduce the literal §4.3 step at half-width 0.");
            }
        }

        [Test]
        public void AgeCurve_IsContinuousAcrossEveryEdge_AndNeverStepsMoreThanOneDay()
        {
            // Doctrine P1 stated as an executable property rather than as a description of the shape:
            // the per-day accrual never jumps, and — the half that matters — the RATE measured over a
            // 30-day window moves by at most a few points from one window to the next, so no birthday
            // carries a discontinuity. Against the pre-fix model the growth edge steps a 30-day window
            // from 30 to 0 in one day.
            const int Window = 30;
            long previous = long.MinValue;
            for (int ageDays = 0; ageDays + Window <= 45 * PlayerProgressionConstants.DAYS_PER_YEAR; ageDays++)
            {
                long rate = AbilityModel.AccruedBandPoints(ageDays + Window)
                            - AbilityModel.AccruedBandPoints(ageDays);

                if (previous != long.MinValue)
                {
                    Assert.LessOrEqual(
                        System.Math.Abs(rate - previous), 1L,
                        $"the 30-day accrual rate stepped by more than one point at age-day {ageDays} — "
                        + "that is a cliff, whatever it is centred on.");
                }
                previous = rate;
            }
        }

        [Test]
        public void AgeCurve_RedistributesAccrual_WithoutCreatingOrDestroyingAny_P5()
        {
            // The P5 pivot, and the reason this fix needs no growth-rate recalibration: for ANY
            // half-width the lifetime integral of growth-days is GROWTH_AGE years exactly, and the
            // decline-days past the decline edge are the same as the step's. The ramp moves accrual
            // across an edge; it does not add or remove any.
            long endOfLife = 45L * PlayerProgressionConstants.DAYS_PER_YEAR;
            long stepModel = AbilityModel.TestOnly_AccruedBandPoints(endOfLife, rampHalfWidthYears: 0);
            long rampModel = AbilityModel.TestOnly_AccruedBandPoints(endOfLife, PlayerProgressionConstants.AgeBandRampHalfWidthYears);

            Assert.AreEqual(stepModel, rampModel,
                "the whole-life integral must be identical under both models, or the fix has silently "
                + "retuned the growth rate as well as its shape.");

            // …and it is genuinely a different curve in between, or the equality above is vacuous.
            long midRamp = (long)PlayerProgressionConstants.GROWTH_AGE * PlayerProgressionConstants.DAYS_PER_YEAR;
            Assert.AreNotEqual(
                AbilityModel.TestOnly_AccruedBandPoints(midRamp, rampHalfWidthYears: 0),
                AbilityModel.TestOnly_AccruedBandPoints(midRamp, PlayerProgressionConstants.AgeBandRampHalfWidthYears),
                "precondition: the two models must differ mid-ramp, or this test proves nothing.");
        }

        [Test]
        public void AgeCurve_BeyondTheRepresentabilityCeiling_StillDrains()
        {
            // Found by re-reading the fix, not by the review. §3.1.1's age narrowing saturates at
            // MAX_DERIVABLE_AGE_YEARS, and under the retired band step that pinned age classified as
            // Decline, so an impossibly-old player kept draining a point a year. Because the new
            // accrual is a DIFFERENCE of a cumulative, saturating the cumulative instead of the age
            // would clamp both terms to the same value and he would silently stop declining — a
            // behaviour change nothing inside the football range could ever surface.
            long ceiling = (long)PlayerProgressionConstants.MAX_DERIVABLE_AGE_YEARS
                           * PlayerProgressionConstants.DAYS_PER_YEAR;

            Assert.AreEqual(
                PlayerProgressionConstants.DECLINE_DAILY_POINTS,
                AbilityModel.DailyBandPoints(ceiling),
                "at the representability ceiling the player must still drain at the full decline rate.");
            Assert.AreEqual(
                PlayerProgressionConstants.DECLINE_DAILY_POINTS,
                AbilityModel.DailyBandPoints(ceiling * 20),
                "…and far beyond it, where the saturation is doing all the work.");
        }

        [Test]
        public void ConstructionDayCredit_InsideARamp_IsTheContinuousStep_NotTheRetiredBandStep()
        {
            // construction-day-credit-implemented-twice. The rule's ONE implementation now lives here,
            // so the ramp discrimination that used to be driven through RegenGenerator's internal
            // BandStepFor is driven at the owner instead — and the owner is public, which is what
            // retires the motive for that internal surface.
            //
            // GROWTH_AGE is inside the growth ramp by construction (the ramp is centred on it). The
            // RETIRED three-way form read ClassifyAgeBand, which reads Growth at GROWTH_AGE (see
            // ClassifyAgeBand_ReadsTheContinuousCurve_NotAFixedEdge above — the YEAR's net accrual is
            // positive), so it would have returned GROWTH_DAILY_POINTS unconditionally, while the DAY on
            // which a player is exactly GROWTH_AGE years old sits at the ramp's own midpoint where the
            // continuous rate is far below full. That difference is what this case asserts; it is what
            // no test through GenerateRegen's public entry point can reach, since a regen's drawn age is
            // REGEN_AGE_MIN..REGEN_AGE_MAX (16-20 today), wholly below the ramp.
            int ageInsideTheRamp = PlayerProgressionConstants.GROWTH_AGE;

            long credit = AbilityModel.ConstructionDayCredit(ageInsideTheRamp);

            Assert.AreEqual(
                AbilityModel.DailyBandPoints(
                    (long)ageInsideTheRamp * PlayerProgressionConstants.DAYS_PER_YEAR),
                credit,
                "the construction-day credit must be exactly the step the daily loop would have taken "
                + "on that day — that equality is the whole content of the ERR-028-018 rule.");

            Assert.AreNotEqual(
                (long)PlayerProgressionConstants.GROWTH_DAILY_POINTS,
                credit,
                "…and it must NOT be the retired three-way band step, which returns the full growth "
                + "rate for every day of this year.");

            Assert.Less(
                System.Math.Abs(ageInsideTheRamp - PlayerProgressionConstants.GROWTH_AGE),
                Ramp + 1,
                "precondition: the probed age must sit inside the growth ramp, or the AreNotEqual "
                + "above could pass for a reason unrelated to the curve.");
            Assert.Greater(
                Ramp, 0,
                "precondition: at a zero half-width the two forms coincide everywhere and this case is "
                + "vacuous (that identity is AgeCurve_AtZeroHalfWidth_IsTheLiteralSection43Step_KD8's).");
        }

        [Test]
        public void RetirementAgeDays_IsPerPlayer_ContinuousAndPopulationNeutral()
        {
            // ERR-028-021. Three properties, and the third is the P5 pivot.
            PlayerRecord average = PlayerRecord.CreateDefault(1);       // Midfielder, all attrs = 10

            // (1) A goalkeeper outlasts an identical outfielder, by the catalogue's own margin.
            PlayerRecord keeper = average;
            keeper.Position = PlayerPosition.Goalkeeper;
            Assert.AreEqual(
                (long)PlayerProgressionConstants.RetirementGoalkeeperBonusYears * PlayerProgressionConstants.DAYS_PER_YEAR,
                AbilityModel.RetirementAgeDays(in keeper) - AbilityModel.RetirementAgeDays(in average),
                "goalkeepers play longer, and the difference is the position allowance exactly.");

            // (2) One attribute point moves the day, and by well under a year — the pattern-(b) cure.
            PlayerRecord sharper = average;
            sharper.Attributes.Anticipation = 13;
            sharper.Attributes.Positioning = 13;
            sharper.Attributes.Composure = 13;
            long delta = AbilityModel.RetirementAgeDays(in sharper) - AbilityModel.RetirementAgeDays(in average);
            Assert.Greater(delta, 0, "a better reader of the game lasts longer.");
            Assert.Less(delta, PlayerProgressionConstants.DAYS_PER_YEAR,
                "…but three attribute points must not be worth a whole extra year — that would be the "
                + "cliff moved rather than removed.");

            // (3) Over a uniform attribute population the offsets sum to exactly zero, so the league's
            // retirement rate is unchanged and only WHO retires when moves.
            //
            // ERR-028-022: this sweep runs the FULL [1,20]³ product, not the Ant == Pos == Comp
            // diagonal it swept when ERR-028-021 landed. The diagonal is precisely where the defect
            // was invisible — the retired implementation floored (Ant+Pos+Comp)/3 to a mean before the
            // anti-symmetric map, and on the diagonal that division is exact, so truncation vanished.
            // Off it, floor(sum/3) is not symmetric about the midpoint and the population sum was
            // −204,621 days (−25.58 d/player): the whole league retiring ~2 months early, which is the
            // rate change the P5 claim said could not happen. Mutation-confirmed at the time: replacing
            // the floor with a different wrong rounding left all 539 tests green.
            long sum = 0;
            long population = 0;
            for (int a = PlayerProgressionConstants.ATTRIBUTE_MIN; a <= PlayerProgressionConstants.ATTRIBUTE_MAX; a++)
            {
                for (int p = PlayerProgressionConstants.ATTRIBUTE_MIN; p <= PlayerProgressionConstants.ATTRIBUTE_MAX; p++)
                {
                    for (int c = PlayerProgressionConstants.ATTRIBUTE_MIN; c <= PlayerProgressionConstants.ATTRIBUTE_MAX; c++)
                    {
                        PlayerAttributes attrs = PlayerAttributes.CreateDefault();
                        attrs.Anticipation = a;
                        attrs.Positioning = p;
                        attrs.Composure = c;
                        sum += AbilityModel.GameReadingOffsetDays(in attrs);
                        population++;
                    }
                }
            }

            int range = PlayerProgressionConstants.ATTRIBUTE_MAX - PlayerProgressionConstants.ATTRIBUTE_MIN + 1;
            Assert.AreEqual((long)range * range * range, population,
                "precondition: the sweep must cover the whole attribute product, not one line through it.");
            Assert.AreEqual(0L, sum,
                "the offset is anti-symmetric about the attribute midpoint over the WHOLE attribute "
                + "product, not merely along Ant == Pos == Comp (ERR-028-022 / P5).");
        }

        // ── Catalogue/config integrity guards (guards-unexercised, ramp-guard-int-overflow,
        //    retirement-dials-no-overload — football-judgment proxy review batch-1 adversarial pass) ──
        //
        // Every case below drives the guard through an explicit parameterised TestOnly_ value (never
        // the live catalogue), so it is reachable under a config-unbound gate — mirroring the
        // AgeCurve_* / RetirementAgeDays_* tests above, which established the same pattern for the
        // [GT] dials themselves. There are FOUR fail-loud `if` guards across the two computing sites:
        // RampHalfWidthDays' negative-half-width check and its disjointness check, and
        // TestOnly_RetirementAgeDays' ONE combined dial non-negativity check
        // (`readingSpanYears < 0 || goalkeeperBonusYears < 0` — a single `if`, not two) and its
        // separate `days <= 0` guard. *(Corrected — round-2 finding
        // four-guards-enumerated-as-five-and-mis-named: this comment previously named "the two
        // negative-dial checks inside the new RetirementAgeDays overload" as two of the four, which
        // both mis-described the combined dial check as two separate guards AND silently dropped the
        // `days <= 0` guard from the enumeration entirely — it is the fourth, named here.)* The
        // combined dial check needs two cases below (one per operand) to prove the OR is checked on
        // both sides, not because it is two `if` statements. Mutation-verified: deleting any one of
        // the four guards leaves the whole suite green without the matching case(s) here; each case
        // below is what turns that revert red.

        [Test]
        public void RampHalfWidthDays_Negative_FailsLoud()
        {
            Assert.Throws<System.InvalidOperationException>(
                () => AbilityModel.TestOnly_AccruedBandPoints(1000, rampHalfWidthYears: -1),
                "a negative ramp half-width inverts the ramp and must be refused where it is read.");
        }

        [Test]
        public void RampHalfWidthDays_TooWide_FailsLoud()
        {
            // edgeSpanYears = (DECLINE_AGE + 1) - GROWTH_AGE = 7 at today's 24/30, so a half-width of 4
            // (2*4 = 8 > 7) overlaps the two ramps by construction, whatever the catalogue's own value is.
            int tooWide = (PlayerProgressionConstants.DECLINE_AGE + 1 - PlayerProgressionConstants.GROWTH_AGE) / 2 + 1;
            Assert.Throws<System.InvalidOperationException>(
                () => AbilityModel.TestOnly_AccruedBandPoints(1000, tooWide),
                "2 x half-width exceeding (DECLINE_AGE + 1) - GROWTH_AGE must be refused, or a day "
                + "accrues growth and decline at once.");
        }

        [Test]
        public void RampHalfWidthDays_TooWide_OverflowCannotDefeatTheGuard()
        {
            // ramp-guard-int-overflow, verified against the real assembly with the exact values the
            // review measured, not by inspection. Pre-fix, `2 * halfWidthYears` wrapped negative in
            // `int` arithmetic at halfWidthYears >= 2^30, so the disjointness guard read a negative
            // "too wide" comparison as satisfied and let a wildly-too-wide half-width straight through
            // to GrowthPhaseDays/DeclinePhaseDays, whose own u*u term then overflowed `long` and
            // returned garbage instead of throwing.
            const int HalfWidthAtThePowerOf2Boundary = 1 << 30; // 1,073,741,824
            Assert.Throws<System.InvalidOperationException>(
                () => AbilityModel.TestOnly_DailyBandPoints(1000, HalfWidthAtThePowerOf2Boundary),
                "a half-width at the int-overflow boundary of the pre-fix guard must still be refused, "
                + "not silently accepted as 'not too wide'.");
            Assert.Throws<System.InvalidOperationException>(
                () => AbilityModel.TestOnly_AccruedBandPoints(1000, 1_200_000_000),
                "…and the cumulative form must refuse the same class of value rather than returning the "
                + "garbage the review measured (2,451,094, identical at daysLived 1000 and 1001).");
        }

        [Test]
        public void RetirementAgeDays_NegativeGoalkeeperBonus_FailsLoud()
        {
            PlayerRecord rec = PlayerRecord.CreateDefault(1);
            Assert.Throws<System.InvalidOperationException>(
                () => AbilityModel.TestOnly_RetirementAgeDays(in rec, goalkeeperBonusYears: -1, readingSpanYears: 0),
                "a negative goalkeeper bonus shortens a goalkeeper's career and must be refused where "
                + "it is read, not merely at the catalogue.");
        }

        [Test]
        public void RetirementAgeDays_NegativeReadingSpan_FailsLoud()
        {
            PlayerRecord rec = PlayerRecord.CreateDefault(1);
            Assert.Throws<System.InvalidOperationException>(
                () => AbilityModel.TestOnly_RetirementAgeDays(in rec, goalkeeperBonusYears: 0, readingSpanYears: -1),
                "a negative reading span retires the best readers of the game first and must be "
                + "refused where it is read, not merely at the catalogue.");
        }

        [Test]
        public void RetirementAgeDays_ComputedDayAtOrBeforeBirth_FailsLoud()
        {
            // A minimum-reading player under a wildly oversized (but individually non-negative) span
            // drives the computed day to or past zero — the DAYS-AT-OR-BEFORE-BIRTH guard, distinct
            // from the combined dial-sign check the two cases above exercise.
            PlayerRecord rec = PlayerRecord.CreateDefault(1);
            rec.Attributes.Anticipation = PlayerProgressionConstants.ATTRIBUTE_MIN;
            rec.Attributes.Positioning = PlayerProgressionConstants.ATTRIBUTE_MIN;
            rec.Attributes.Composure = PlayerProgressionConstants.ATTRIBUTE_MIN;

            Assert.Throws<System.InvalidOperationException>(
                () => AbilityModel.TestOnly_RetirementAgeDays(in rec, goalkeeperBonusYears: 0, readingSpanYears: 1_000_000),
                "a computed retirement day at or before birth is a catalogue/config integrity failure, "
                + "not a value to silently clamp.");
        }

        [Test]
        public void RetirementAgeDays_AtZeroZeroDials_ReproducesTheBaselineExactly_ForGoalkeeperAndOutfielder()
        {
            // retirement-dials-no-overload / spec-error-log.md's "zero dials reproduce the retired
            // comparison identically ... Locked" claim, EXECUTED — the public RetirementAgeDays(in rec)
            // reads the live catalogue and cannot be driven to bonus 0 / span 0 unless the catalogue
            // itself happens to be there; this overload can be, so the identity is checked directly
            // rather than only by hand.
            long baseline = (long)PlayerProgressionConstants.RETIREMENT_AGE * PlayerProgressionConstants.DAYS_PER_YEAR;

            for (int a = PlayerProgressionConstants.ATTRIBUTE_MIN; a <= PlayerProgressionConstants.ATTRIBUTE_MAX; a += 3)
            {
                PlayerRecord outfielder = PlayerRecord.CreateDefault(1);
                outfielder.Position = PlayerPosition.Defender;
                outfielder.Attributes.Anticipation = a;
                outfielder.Attributes.Positioning = a;
                outfielder.Attributes.Composure = a;

                PlayerRecord keeper = outfielder;
                keeper.Position = PlayerPosition.Goalkeeper;

                Assert.AreEqual(
                    baseline,
                    AbilityModel.TestOnly_RetirementAgeDays(in outfielder, goalkeeperBonusYears: 0, readingSpanYears: 0),
                    $"outfielder at reading-trio value {a}: bonus 0 / span 0 must reproduce RETIREMENT_AGE "
                    + "exactly, whatever the attributes are.");
                Assert.AreEqual(
                    baseline,
                    AbilityModel.TestOnly_RetirementAgeDays(in keeper, goalkeeperBonusYears: 0, readingSpanYears: 0),
                    $"goalkeeper at reading-trio value {a}: the position term must also vanish at bonus 0.");
            }
        }

        [Test]
        public void RetirementAgeDays_IsMonotonicWithinABand_AsTheAttributesItReadsAreMonotone()
        {
            // round-2 finding retirement-day-derived-from-attributes-the-same-step-mutates. §3.4's
            // RetirementAgeDays is re-evaluated daily against `rec`, the SAME record TrySpendOnePoint /
            // DrainOnePoint mutate earlier in the same AdvancePlayerTo call — the reading trio it reads
            // (Anticipation/Positioning/Composure) is exactly what those two methods move. Nothing
            // stops the retirement day itself moving day over day; what this locks is the ONE property
            // that keeps it from oscillating today: within a single band, each spend/drain call moves
            // every reading attribute in the SAME direction (up in Growth, down in Decline, never
            // both), so the computed retirement day is monotone across a run of same-direction
            // mutations. Reverting to a version where the trio could move in mixed directions within a
            // band (a T3 curve; #47 authored data touching them independently) is exactly what would
            // break this.
            PlayerRecord rec = PlayerRecord.CreateDefault(1); // Midfielder, all attrs = 10
            var growthLife = new PlayerLifecycle { PotentialAbility = PlayerProgressionConstants.ABILITY_MAX };

            long previousGrowth = AbilityModel.RetirementAgeDays(in rec);
            for (int i = 0; i < 5; i++)
            {
                // A spend only ever raises an attribute (never lowers one), so each successful spend
                // that touches Anticipation/Positioning/Composure must move the retirement day the same
                // way (non-decreasing) as the one before it, and never the other way.
                bool spent = AbilityModel.TrySpendOnePoint(ref rec, ref growthLife);
                Assert.IsTrue(spent, "precondition: the PA ceiling must not bind inside this sweep.");
                long day = AbilityModel.RetirementAgeDays(in rec);
                Assert.GreaterOrEqual(
                    day, previousGrowth,
                    "a successful Growth-side spend must never LOWER the retirement day — it only ever "
                    + "raises an attribute, and the offset is monotone increasing in the reading trio.");
                previousGrowth = day;
            }

            PlayerRecord declineRec = PlayerRecord.CreateDefault(1);
            var declineLife = new PlayerLifecycle();
            long previousDecline = AbilityModel.RetirementAgeDays(in declineRec);
            for (int i = 0; i < 5; i++)
            {
                bool drained = AbilityModel.DrainOnePoint(ref declineRec, ref declineLife);
                Assert.IsTrue(drained, "precondition: no attribute floor must bind inside this sweep.");
                long day = AbilityModel.RetirementAgeDays(in declineRec);
                Assert.LessOrEqual(
                    day, previousDecline,
                    "a successful Decline-side drain must never RAISE the retirement day — it only ever "
                    + "lowers an attribute, and the offset is monotone increasing in the reading trio.");
                previousDecline = day;
            }
        }

        [Test]
        public void ComputeCA_Neutral_IsDeterministicAndRecomputeEqualsStored()
        {
            PlayerRecord rec = PlayerRecord.CreateDefault(1); // Midfielder, all attrs = 10
            int first = AbilityModel.ComputeCA(in rec.Attributes, rec.Position);
            int second = AbilityModel.ComputeCA(in rec.Attributes, rec.Position);
            Assert.AreEqual(NeutralCa, first);
            Assert.AreEqual(first, second, "ComputeCA must be a pure function of the attributes (recompute == stored).");
        }

        [Test]
        public void ComputeCA_PositionWeighting_EmphasisesSignatureAttributes()
        {
            // A high-Passing player rates higher as a Midfielder (Passing is a signature attr) than as a
            // Goalkeeper (Passing carries no positional emphasis).
            PlayerAttributes attrs = PlayerAttributes.CreateDefault();
            attrs.Passing = PlayerProgressionConstants.ATTRIBUTE_MAX;

            int midfielderCa = AbilityModel.ComputeCA(in attrs, PlayerPosition.Midfielder);
            int goalkeeperCa = AbilityModel.ComputeCA(in attrs, PlayerPosition.Goalkeeper);

            Assert.Greater(midfielderCa, goalkeeperCa);
        }

        [Test]
        public void TrySpendOnePoint_AtPaCeiling_RefusesAndLeavesAttributesUnchanged()
        {
            PlayerRecord rec = PlayerRecord.CreateDefault(1);
            var life = new PlayerLifecycle { PotentialAbility = NeutralCa }; // PA == current CA ⇒ any raise overshoots (F1)
            int[] before = rec.Attributes.ToArray();

            bool spent = AbilityModel.TrySpendOnePoint(ref rec, ref life);

            Assert.IsFalse(spent, "a spend that would exceed the PA ceiling must be a no-op (F1).");
            CollectionAssert.AreEqual(before, rec.Attributes.ToArray(), "attributes must be unchanged after a refused spend.");
        }

        [Test]
        public void TrySpendOnePoint_RaisesHighestBiasAttribute_TieBrokenByAscendingIndex()
        {
            PlayerRecord rec = PlayerRecord.CreateDefault(1); // Midfielder
            var life = new PlayerLifecycle { PotentialAbility = PlayerProgressionConstants.ABILITY_MAX };

            bool spent = AbilityModel.TrySpendOnePoint(ref rec, ref life);

            Assert.IsTrue(spent);
            // Midfielder signature attrs are Stamina(5) / Passing(6) / Vision(14), all bias 3; the tie
            // breaks to the lowest AttrIdx, Stamina.
            Assert.AreEqual(11, rec.Attributes.Stamina, "the lowest-index highest-bias attribute must rise first.");
            Assert.AreEqual(311, SumAttributes(rec.Attributes), "exactly one attribute point may be spent.");
        }

        [Test]
        public void DrainOnePoint_LowersLowestBiasAttribute_TieBrokenByAscendingIndex()
        {
            PlayerRecord rec = PlayerRecord.CreateDefault(1); // Midfielder
            var life = new PlayerLifecycle();

            AbilityModel.DrainOnePoint(ref rec, ref life);

            // The mirror order: lowest-bias first, ascending index ⇒ Pace(0) sheds first.
            Assert.AreEqual(9, rec.Attributes.Pace, "the lowest-index lowest-bias attribute must fall first.");
            Assert.AreEqual(309, SumAttributes(rec.Attributes));
        }

        private static int SumAttributes(PlayerAttributes attrs)
        {
            int[] a = attrs.ToArray();
            int sum = 0;
            for (int i = 0; i < a.Length; i++)
            {
                sum += a[i];
            }
            return sum;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-24 | —      | Initial implementation. |
// | 1.1     | 2026-08-22 | —      | ERR-028-020 / ERR-028-021. ClassifyAgeBand's boundary lock REBASELINED —
// |         |            |        | it asserted Stable at GROWTH_AGE and at DECLINE_AGE, i.e. it asserted the
// |         |            |        | cliff; both now read inside their ramp. + the half-width-0 §4.3 identity
// |         |            |        | (per-day over 45 years), + per-day continuity, + the exact P5 integral
// |         |            |        | equality with its own vacuity precondition, + the per-player retirement
// |         |            |        | day's three properties. + the representability-ceiling lock: a
// |         |            |        | player past MAX_DERIVABLE_AGE_YEARS must still drain at the full
// |         |            |        | decline rate, which saturating the cumulative rather than the age
// |         |            |        | would silently have stopped.
// | 1.2     | 2026-08-22 | —      | ERR-028-022. RetirementAgeDays_IsPerPlayer_ContinuousAndPopulation-
// |         |            |        | Neutral's property (3) now sweeps the FULL [1,20]^3 attribute
// |         |            |        | product instead of the Ant==Pos==Comp diagonal. The diagonal is
// |         |            |        | exactly where the retired floored-mean's asymmetry vanished, so the
// |         |            |        | v1.1 lock passed against an implementation whose population sum was
// |         |            |        | -204,621 days — it could not see the defect it claimed to prove
// |         |            |        | absent. + a cardinality precondition so the sweep cannot silently
// |         |            |        | narrow back to a line.
// | 1.3     | 2026-08-23 | —      | Football-judgment proxy review, batch-1 adversarial findings
// |         |            |        | (guards-unexercised, ramp-guard-int-overflow, retirement-dials-no-
// |         |            |        | overload). + RampHalfWidthDays_Negative_FailsLoud,
// |         |            |        | RampHalfWidthDays_TooWide_FailsLoud,
// |         |            |        | RampHalfWidthDays_TooWide_OverflowCannotDefeatTheGuard (the
// |         |            |        | int-overflow fix, verified against the exact garbage values the
// |         |            |        | review measured), RetirementAgeDays_NegativeGoalkeeperBonus_
// |         |            |        | FailsLoud, RetirementAgeDays_NegativeReadingSpan_FailsLoud,
// |         |            |        | RetirementAgeDays_ComputedDayAtOrBeforeBirth_FailsLoud (all four
// |         |            |        | now reachable through the new internal parameterised
// |         |            |        | RetirementAgeDays/GameReadingOffsetDays overloads, not the live
// |         |            |        | catalogue), and RetirementAgeDays_AtZeroZeroDials_ReproducesThe-
// |         |            |        | BaselineExactly_ForGoalkeeperAndOutfielder (the zero-dial OFF
// |         |            |        | identity, executed rather than hand-verified). Mutation-verified:
// |         |            |        | each guard deleted independently turns exactly its matching new
// |         |            |        | case red with the rest of the suite green.
// | 1.4     | 2026-08-24 | —      | Round-2 adversarial finding construction-day-credit-implemented-
// |         |            |        | twice (High). + ConstructionDayCredit_InsideARamp_IsThe-
// |         |            |        | ContinuousStep_NotTheRetiredBandStep — the ramp discrimination
// |         |            |        | that RegenGeneratorTests used to drive through RegenGenerator's
// |         |            |        | internal BandStepFor, now driven at the rule's owner, whose
// |         |            |        | public API is what retires that internal surface. Both
// |         |            |        | vacuity preconditions (inside-the-ramp, non-zero half-width)
// |         |            |        | asserted, per the case it replaces. Mutation-verified:
// |         |            |        | reimplementing ConstructionDayCredit as the retired
// |         |            |        | ClassifyAgeBand three-way step fails exactly this case.
// | 1.5     | 2026-08-24 | —      | Round-2 Medium/Low adversarial findings. M3 (test-affordance-
// |         |            |        | overloads-ignore-the-TestOnly-naming-convention): every call to
// |         |            |        | the four dial-taking internal overloads renamed to
// |         |            |        | TestOnly_DailyBandPoints / TestOnly_AccruedBandPoints /
// |         |            |        | TestOnly_RetirementAgeDays (AbilityModel.cs v1.5); calls to the
// |         |            |        | single-argument catalogue-reading forms (DailyBandPoints(long),
// |         |            |        | AccruedBandPoints(long), RetirementAgeDays(in rec),
// |         |            |        | GameReadingOffsetDays(in attrs)) are unchanged. L1 (four-guards-
// |         |            |        | enumerated-as-five-and-mis-named): the guard-block comment
// |         |            |        | corrected — it named "two negative-dial checks" where there is
// |         |            |        | ONE combined `if`, and omitted the `days <= 0` guard from the
// |         |            |        | enumeration entirely; RetirementAgeDays_ComputedDayAtOrBeforeBirth_
// |         |            |        | FailsLoud's own comment corrected to name that guard instead of a
// |         |            |        | second dial check. M5 (retirement-day-derived-from-attributes-the-
// |         |            |        | same-step-mutates): + RetirementAgeDays_IsMonotonicWithinABand_
// |         |            |        | AsTheAttributesItReadsAreMonotone — locks the one property that
// |         |            |        | keeps the daily re-evaluation from oscillating today (each
// |         |            |        | band's spend/drain order is one-directional), so a future change
// |         |            |        | that lets the reading trio move in mixed directions within a band
// |         |            |        | (a T3 curve; #47 authored data) trips a red suite here rather
// |         |            |        | than surfacing as an undiagnosed field report.
#endregion
