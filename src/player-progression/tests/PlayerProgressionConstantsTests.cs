// File:     src/player-progression/tests/PlayerProgressionConstantsTests.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Player Progression & Lifecycle #28 Appendix A (constant catalogue); Code Standards #20
// Purpose:  Balance-pass invariant locks on the #28 constant catalogue — the [GT] shapes/derivations
//           that are the contract, independent of the illustrative magnitudes.

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;

namespace TacticalDirector.PlayerProgression.Tests
{
    [TestFixture]
    public sealed class PlayerProgressionConstantsTests
    {
        [Test]
        public void PointCost_EqualsDaysPerYear_OneStepPerYear()
        {
            Assert.AreEqual(
                PlayerProgressionConstants.DAYS_PER_YEAR,
                PlayerProgressionConstants.POINT_COST,
                "KD-8: POINT_COST must equal DAYS_PER_YEAR so the Growth band spends exactly one point per year.");
        }

        [Test]
        public void AgeBands_AreStrictlyOrdered()
        {
            Assert.Less(PlayerProgressionConstants.GROWTH_AGE, PlayerProgressionConstants.DECLINE_AGE);
            Assert.Less(PlayerProgressionConstants.DECLINE_AGE, PlayerProgressionConstants.RETIREMENT_AGE);
        }

        [Test]
        public void DailyPoints_HaveTheExpectedSigns()
        {
            Assert.AreEqual(+1, PlayerProgressionConstants.GROWTH_DAILY_POINTS);
            Assert.AreEqual(-1, PlayerProgressionConstants.DECLINE_DAILY_POINTS);
        }

        [Test]
        public void RegenFields_AreTheDerivedBudget()
        {
            // The derivation, not the literal — one PA draw on top of #27's per-player identity + attrs.
            Assert.AreEqual(
                PlayerDatabaseConstants.IDENTITY_DRAWS_PER_PLAYER + PlayerDatabaseConstants.ATTRIBUTE_COUNT + 1,
                PlayerProgressionConstants.PROGRESSION_REGEN_FIELDS);
        }

        [Test]
        public void AttributeBounds_MirrorPlayerDatabase()
        {
            Assert.AreEqual(PlayerDatabaseConstants.ATTRIBUTE_MIN, PlayerProgressionConstants.ATTRIBUTE_MIN);
            Assert.AreEqual(PlayerDatabaseConstants.ATTRIBUTE_MAX, PlayerProgressionConstants.ATTRIBUTE_MAX);
        }

        [Test]
        public void RegenPaBalanceValues_AreCoherent()
        {
            Assert.Greater(PlayerProgressionConstants.PA_MIN, 0);
            Assert.LessOrEqual(PlayerProgressionConstants.PA_MIN, PlayerProgressionConstants.ABILITY_MAX);
            Assert.Greater(PlayerProgressionConstants.REGEN_PA_HEADROOM, 0);
            Assert.LessOrEqual(PlayerProgressionConstants.REGEN_AGE_MIN, PlayerProgressionConstants.REGEN_AGE_MAX);
            // A regen's generated age must be below the Growth-band ceiling (a young player, §3.3).
            Assert.Less(PlayerProgressionConstants.REGEN_AGE_MAX, PlayerProgressionConstants.GROWTH_AGE);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-24 | —      | Initial implementation. |
#endregion
