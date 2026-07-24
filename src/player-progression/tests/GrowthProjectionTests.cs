// File:     src/player-progression/tests/GrowthProjectionTests.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Player Progression & Lifecycle #28 §3.1 + Appendix B; Code Standards #20
// Purpose:  T-PG-DET-001/002, T-PG-ID-001/002 — byte-exact growth across a value-copy "save", age
//           gap-independence, the curve-off §4.3 identity step, and TrainingInput.Neutral neutrality.

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression.Tests
{
    [TestFixture]
    public sealed class GrowthProjectionTests
    {
        // A large base day so BirthWorldDay stays non-negative for every age used here.
        private const uint BaseDay = 100000;
        private const int NeutralAttributeSum = 31 * 10; // PlayerRecord.CreateDefault ⇒ all attrs = 10

        [Test]
        public void GrowthBand_SpendsExactlyOnePointPerYear_CurveOff()
        {
            (PlayerRecord rec, PlayerLifecycle life) = NewPlayer(ageAtBase: 18); // < GROWTH_AGE ⇒ Growth

            // One year of daily steps (365 calls: BaseDay .. BaseDay+364).
            StepInclusive(ref rec, ref life, BaseDay, BaseDay + 364);

            Assert.AreEqual(NeutralAttributeSum + 1, SumAttributes(rec.Attributes),
                "the literal §4.3 step (KD-8): exactly one attribute point per year in the Growth band.");
            Assert.AreEqual(0L, life.GrowthCursor, "the cursor returns to 0 after the point is spent.");
        }

        [Test]
        public void DeclineBand_DrainsExactlyOnePointPerYear_CurveOff()
        {
            (PlayerRecord rec, PlayerLifecycle life) = NewPlayer(ageAtBase: 32); // > DECLINE_AGE ⇒ Decline

            StepInclusive(ref rec, ref life, BaseDay, BaseDay + 364);

            Assert.AreEqual(NeutralAttributeSum - 1, SumAttributes(rec.Attributes),
                "the symmetric decline step: exactly one attribute point drained per year.");
            Assert.AreEqual(0L, life.GrowthCursor);
        }

        [Test]
        public void Age_IsDerivedFromBirthDay_IndependentOfCallHistory()
        {
            (PlayerRecord rec, PlayerLifecycle life) = NewPlayer(ageAtBase: 18);

            // Age is a pure function of (worldDay − BirthWorldDay); no discrete rollover accumulates.
            StepInclusive(ref rec, ref life, BaseDay, BaseDay + 364);
            Assert.AreEqual(18, rec.Age, "still 18 within the first derived year.");

            GrowthProjection.AdvanceDayForPlayer(ref rec, ref life, BaseDay + 365, TrainingInput.Neutral, curveEnabled: false);
            Assert.AreEqual(19, rec.Age, "the derived age ticks to 19 once a full year of world-days has elapsed.");
        }

        [Test]
        public void SaveAtAnyDay_IsByteIdenticalToTheUninterruptedRun()
        {
            (PlayerRecord live, PlayerLifecycle liveLife) = NewPlayer(ageAtBase: 18);

            // Advance to a mid-year "save" point.
            StepInclusive(ref live, ref liveLife, BaseDay, BaseDay + 200);

            // A value-copy of the two structs stands in for the T1 codec round-trip.
            PlayerRecord saved = live;
            PlayerLifecycle savedLife = liveLife;

            // Continue both the live run and the restored copy identically.
            StepInclusive(ref live, ref liveLife, BaseDay + 201, BaseDay + 400);
            StepInclusive(ref saved, ref savedLife, BaseDay + 201, BaseDay + 400);

            AssertSameState(live, liveLife, saved, savedLife);
        }

        [Test]
        public void TwoIdenticalPlayers_SteppedIdentically_AreByteIdentical()
        {
            (PlayerRecord a, PlayerLifecycle aLife) = NewPlayer(ageAtBase: 20);
            (PlayerRecord b, PlayerLifecycle bLife) = NewPlayer(ageAtBase: 20);

            StepInclusive(ref a, ref aLife, BaseDay, BaseDay + 800);
            StepInclusive(ref b, ref bLife, BaseDay, BaseDay + 800);

            AssertSameState(a, aLife, b, bLife);
        }

        [Test]
        public void TrainingInputNeutral_IsByteIdenticalToDefault()
        {
            (PlayerRecord withNeutral, PlayerLifecycle neutralLife) = NewPlayer(ageAtBase: 18);
            (PlayerRecord withDefault, PlayerLifecycle defaultLife) = NewPlayer(ageAtBase: 18);

            for (uint d = BaseDay; d <= BaseDay + 400; d++)
            {
                GrowthProjection.AdvanceDayForPlayer(ref withNeutral, ref neutralLife, d, TrainingInput.Neutral, curveEnabled: false);
                GrowthProjection.AdvanceDayForPlayer(ref withDefault, ref defaultLife, d, default, curveEnabled: false);
            }

            AssertSameState(withNeutral, neutralLife, withDefault, defaultLife);
        }

        private static (PlayerRecord, PlayerLifecycle) NewPlayer(int ageAtBase)
        {
            PlayerRecord rec = PlayerRecord.CreateDefault(1); // Midfielder, all attrs = 10
            var life = new PlayerLifecycle
            {
                PotentialAbility = PlayerProgressionConstants.ABILITY_MAX, // no ceiling in these tests
                CurrentAbility = 0,
                GrowthCursor = 0,
                BirthWorldDay = BaseDay - (uint)(ageAtBase * PlayerProgressionConstants.DAYS_PER_YEAR),
                RetirementFlag = false,
                RetirementDay = 0
            };
            return (rec, life);
        }

        private static void StepInclusive(ref PlayerRecord rec, ref PlayerLifecycle life, uint fromDay, uint toDay)
        {
            for (uint d = fromDay; d <= toDay; d++)
            {
                GrowthProjection.AdvanceDayForPlayer(ref rec, ref life, d, TrainingInput.Neutral, curveEnabled: false);
            }
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

        private static void AssertSameState(in PlayerRecord ra, in PlayerLifecycle la, in PlayerRecord rb, in PlayerLifecycle lb)
        {
            Assert.AreEqual(ra.PlayerId, rb.PlayerId, "PlayerId");
            Assert.AreEqual(ra.Age, rb.Age, "Age");
            Assert.AreEqual(ra.Position, rb.Position, "Position");
            CollectionAssert.AreEqual(ra.Attributes.ToArray(), rb.Attributes.ToArray(), "attributes");
            Assert.AreEqual(ra.Attributes.WeakFootRating, rb.Attributes.WeakFootRating, "WeakFootRating");
            Assert.AreEqual(la.GrowthCursor, lb.GrowthCursor, "GrowthCursor");
            Assert.AreEqual(la.CurrentAbility, lb.CurrentAbility, "CurrentAbility");
            Assert.AreEqual(la.PotentialAbility, lb.PotentialAbility, "PotentialAbility");
            Assert.AreEqual(la.BirthWorldDay, lb.BirthWorldDay, "BirthWorldDay");
            Assert.AreEqual(la.RetirementFlag, lb.RetirementFlag, "RetirementFlag");
            Assert.AreEqual(la.RetirementDay, lb.RetirementDay, "RetirementDay");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-24 | —      | Initial implementation. |
#endregion
