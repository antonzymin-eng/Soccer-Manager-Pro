// File:     src/player-progression/tests/PlayerProgressionConstantsTests.cs
// Created:  2026-07-24
// Modified: 2026-08-23 (football-judgment proxy review, batch-1 adversarial findings — v1.1)
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

        // ── config-unbound-premise-false-28 ──────────────────────────────────────────
        //
        // AbilityModel.RampHalfWidthDays/TestOnly_RetirementAgeDays enforce FOUR fail-loud guards at
        // their computing site: the ramp half-width's non-negativity and disjointness (mirrored by the
        // two methods above), and the two retirement dials' non-negativity (mirrored by
        // RetirementDials_AreNonNegative below, in one method since it is ONE combined `if` at the
        // computing site too — see AbilityModelTests.cs). *(Corrected — round-2 finding
        // four-guards-enumerated-as-five-and-mis-named: this comment previously said "these same three
        // invariants", underselling RetirementDials_AreNonNegative by one — it asserts TWO invariants,
        // not one. The count here is THREE TEST METHODS covering FOUR invariants, matching
        // AbilityModelTests.cs' four computing-site guards; TestOnly_RetirementAgeDays' fifth guard,
        // `days <= 0`, has NO catalogue-level equivalent here, since it depends on a player record's
        // attributes and cannot be evaluated from catalogue constants alone.)* The documented rationale
        // ("forward-looking placement for the Stage-1 config loader") is different from — and narrower
        // than — the reason the sibling #29/#41 computing-site guards give ("the catalogue lock runs
        // config-unbound"). That reason is false for THIS catalogue: PlayerProgressionConstants.cs has
        // zero Config.GetX calls today, so a catalogue-level lock is not defeated here and belongs
        // alongside the computing-site guards above.

        [Test]
        public void AgeBandRampHalfWidthYears_IsNonNegative()
        {
            Assert.GreaterOrEqual(PlayerProgressionConstants.AgeBandRampHalfWidthYears, 0,
                "a negative ramp half-width inverts the ramp (§3.1.3, Appendix A).");
        }

        [Test]
        public void AgeBandRampHalfWidthYears_LeavesTheTwoRampsDisjoint()
        {
            int edgeSpanYears = PlayerProgressionConstants.DECLINE_AGE + 1 - PlayerProgressionConstants.GROWTH_AGE;
            Assert.LessOrEqual(2 * PlayerProgressionConstants.AgeBandRampHalfWidthYears, edgeSpanYears,
                "2 x half-width must not exceed (DECLINE_AGE + 1) - GROWTH_AGE, or a day sits inside "
                + "both ramps and accrues growth and decline at once (§3.1.3, Appendix A).");
        }

        [Test]
        public void RetirementDials_AreNonNegative()
        {
            Assert.GreaterOrEqual(PlayerProgressionConstants.RetirementGoalkeeperBonusYears, 0,
                "a negative goalkeeper bonus shortens a goalkeeper's career (§3.4, Appendix A).");
            Assert.GreaterOrEqual(PlayerProgressionConstants.RetirementGameReadingSpanYears, 0,
                "a negative reading span retires the best readers of the game first (§3.4, Appendix A).");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-24 | —      | Initial implementation. |
// | 1.1     | 2026-08-23 | —      | Football-judgment proxy review, batch-1 adversarial findings
// |         |            |        | (config-unbound-premise-false-28): + catalogue-level non-negative
// |         |            |        | / disjointness locks on AgeBandRampHalfWidthYears,
// |         |            |        | RetirementGoalkeeperBonusYears and RetirementGameReadingSpanYears —
// |         |            |        | this catalogue has zero Config.GetX calls, so a catalogue lock is
// |         |            |        | not defeated by a config-unbound gate here (unlike the sibling
// |         |            |        | #29/#41 rationale it had been copied from) and belongs alongside
// |         |            |        | the other locks in this file. No value changed.
#endregion
