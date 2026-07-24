// File:     src/player-progression/tests/AbilityModelTests.cs
// Created:  2026-07-24
// Modified: 2026-07-24
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

        [Test]
        public void ClassifyAgeBand_MatchesTheAppendixABoundaries()
        {
            Assert.AreEqual(AbilityModel.AgeBand.Growth, AbilityModel.ClassifyAgeBand(PlayerProgressionConstants.GROWTH_AGE - 1));
            Assert.AreEqual(AbilityModel.AgeBand.Stable, AbilityModel.ClassifyAgeBand(PlayerProgressionConstants.GROWTH_AGE));
            // Appendix A: DECLINE_AGE is "the age ABOVE which" decline begins — the boundary age stays Stable.
            Assert.AreEqual(AbilityModel.AgeBand.Stable, AbilityModel.ClassifyAgeBand(PlayerProgressionConstants.DECLINE_AGE));
            Assert.AreEqual(AbilityModel.AgeBand.Decline, AbilityModel.ClassifyAgeBand(PlayerProgressionConstants.DECLINE_AGE + 1));
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
#endregion
