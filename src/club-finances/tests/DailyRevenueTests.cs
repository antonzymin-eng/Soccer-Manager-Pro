// File:     src/club-finances/tests/DailyRevenueTests.cs
// Created:  2026-10-13
// Modified: 2026-10-13
// Author:   —
// Spec:     Club Finances & Economy #40 §3.7 / §5.10, Code Standards #20
// Purpose:  Locks deterministic revenue amounts, integer floors and enabled-model failure rows.

using System;

using NUnit.Framework;

namespace TacticalDirector.ClubFinances.Tests
{
    [TestFixture]
    public sealed class DailyRevenueTests
    {
        /// <summary>T-FN-REV-001: default-catalogue worked example, endpoints and multiple home fixtures.</summary>
        [TestCase(1L, 4, 20, 1_000L, 35_800L)]
        [TestCase(1L, 1, 20, 1_000L, 38_000L)]
        [TestCase(1L, 20, 20, 1_000L, 24_000L)]
        [TestCase(0L, 4, 20, 1_000L, 0L)]
        [TestCase(2L, 4, 20, 1_000L, 71_600L)]
        [TestCase(1L, 2, 4, 1_000L, 33_360L)]
        [TestCase(1L, 3, 4, 1_000L, 28_680L)]
        public void DailyRevenue_UsesExactIntegerAmounts(long homeCount, int position, int clubs,
            long sponsorship, long matchday)
        {
            var amounts = FinanceStep.DailyRevenue(homeCount, position, clubs, true);
            Assert.That(amounts.Sponsorship, Is.EqualTo(sponsorship));
            Assert.That(amounts.Matchday, Is.EqualTo(matchday));
        }

        /// <summary>T-FN-REV-009: break/pre-season days earn nothing.</summary>
        [Test]
        public void DailyRevenue_OutsideSeason_ReturnsZero()
        {
            var amounts = FinanceStep.DailyRevenue(0, 4, 20, false);
            Assert.That(amounts.Sponsorship, Is.Zero);
            Assert.That(amounts.Matchday, Is.Zero);
        }

        /// <summary>T-FN-REV-002/009: every model input refusal fails at its named boundary.</summary>
        [TestCase(-1L, 1, 20, true, "homeFixturesCompleted")]
        [TestCase(0L, 1, 1, true, "clubCount")]
        [TestCase(0L, 1, 0, true, "clubCount")]
        [TestCase(0L, 0, 20, true, "leaguePosition")]
        [TestCase(0L, 21, 20, true, "leaguePosition")]
        [TestCase(1L, 1, 20, false, "homeFixturesCompleted")]
        public void DailyRevenue_InvalidInput_FailsLoud(long homeCount, int position, int clubs,
            bool inSeason, string parameter)
        {
            var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
                FinanceStep.DailyRevenue(homeCount, position, clubs, inSeason));
            Assert.That(error.ParamName, Is.EqualTo(parameter));
        }

        /// <summary>T-FN-REV-002: a large completed-fixture count cannot wrap into plausible currency.</summary>
        [Test]
        public void DailyRevenue_MatchdayOverflow_FailsLoud()
        {
            Assert.Throws<OverflowException>(() => FinanceStep.DailyRevenue(long.MaxValue, 1, 20, true));
        }

        /// <summary>T-FN-REV-003: the only production gate remains disabled in the default catalogue.</summary>
        [Test]
        public void DefaultCatalogue_RevenueIsOff()
        {
            Assert.That(ClubFinancesConstants.DeepRevenueEnabled, Is.False);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-10-13 | —      | T3b2 formula, boundary, overflow and default-gate locks. |
#endregion
