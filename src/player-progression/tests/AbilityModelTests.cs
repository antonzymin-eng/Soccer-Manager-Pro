// File:     src/player-progression/tests/AbilityModelTests.cs
// Created:  2026-07-24
// Modified: 2026-08-22 (ERR-028-020 + ERR-028-021 — football-judgment proxy review batch 1 — v1.1)
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
                    AbilityModel.DailyBandPoints(ageDays, rampHalfWidthYears: 0),
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
            long stepModel = AbilityModel.AccruedBandPoints(endOfLife, rampHalfWidthYears: 0);
            long rampModel = AbilityModel.AccruedBandPoints(endOfLife, PlayerProgressionConstants.AgeBandRampHalfWidthYears);

            Assert.AreEqual(stepModel, rampModel,
                "the whole-life integral must be identical under both models, or the fix has silently "
                + "retuned the growth rate as well as its shape.");

            // …and it is genuinely a different curve in between, or the equality above is vacuous.
            long midRamp = (long)PlayerProgressionConstants.GROWTH_AGE * PlayerProgressionConstants.DAYS_PER_YEAR;
            Assert.AreNotEqual(
                AbilityModel.AccruedBandPoints(midRamp, rampHalfWidthYears: 0),
                AbilityModel.AccruedBandPoints(midRamp, PlayerProgressionConstants.AgeBandRampHalfWidthYears),
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
            long sum = 0;
            for (int a = PlayerProgressionConstants.ATTRIBUTE_MIN; a <= PlayerProgressionConstants.ATTRIBUTE_MAX; a++)
            {
                PlayerAttributes attrs = PlayerAttributes.CreateDefault();
                attrs.Anticipation = a;
                attrs.Positioning = a;
                attrs.Composure = a;
                sum += AbilityModel.GameReadingOffsetDays(in attrs);
            }
            Assert.AreEqual(0L, sum, "the offset is anti-symmetric about the attribute midpoint (P5).");
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
#endregion
