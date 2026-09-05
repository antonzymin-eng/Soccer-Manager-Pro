// ============================================================================
// File:     src/club-finances/tests/ClubFinancesT0Tests.cs
// Created:  2026-09-04
// Modified: 2026-09-04
// Author:   Codex / Anton
// Specs:    Spec #20 §3.6.2, §3.9.4 (style/docs; general-test allocation carve-out)
//           Spec #40 §5 (T0-coverable acceptance contract)
// Purpose:  Locks the deterministic projection, ledger separation, failure gates, and integer-only shape.
// ============================================================================
// §3.9.4 general-unit-test — allocation rules relaxed in test body

using System;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

namespace TacticalDirector.ClubFinances.Tests
{
    /// <summary>Acceptance tests for the code surfaces delivered by #40 T0.</summary>
    [TestFixture]
    public sealed class ClubFinancesT0Tests
    {
        /// <summary>Locks the pre-first-season behavior-neutral identity.</summary>
        [Test]
        public void CreateInitial_IsBehaviourNeutralIdentity()
        {
            ClubFinances finances = ClubFinances.CreateInitial(ClubFinancesConstants.StartingClubBalance);

            Assert.That(finances.Balance, Is.EqualTo(500_000L));
            Assert.That(finances.TransferBudget, Is.Zero);
            Assert.That(finances.WageBudget, Is.Zero);
            Assert.That(finances.WageBillAggregate, Is.Zero);
            Assert.That(finances.SeasonRevenueAccrued, Is.Zero);
            Assert.That(finances.FfpBalanceWindow, Is.Zero);
        }

        /// <summary>Locks both interpolation endpoints and the approved position-4 worked example.</summary>
        [Test]
        public void PrizeMoneyForPosition_InterpolatesEndpointsAndWorkedExample()
        {
            Assert.That(FinanceStep.PrizeMoneyForPosition(1, 20), Is.EqualTo(2_000_000L));
            Assert.That(FinanceStep.PrizeMoneyForPosition(20, 20), Is.EqualTo(200_000L));
            Assert.That(FinanceStep.PrizeMoneyForPosition(4, 20), Is.EqualTo(1_715_790L));
        }

        /// <summary>Locks the complete approved settlement worked example.</summary>
        [Test]
        public void SettleFinances_MatchesApprovedWorkedExample()
        {
            ClubFinances prior = new ClubFinances
            {
                Balance = 1_250_000L,
                TransferBudget = 400_000L,
                WageBudget = 180_000L,
                WageBillAggregate = 95_000L,
                SeasonRevenueAccrued = 0,
                FfpBalanceWindow = 0
            };

            ClubFinances result = FinanceStep.SettleFinances(in prior, 4, 20, BoardModifier.Identity);

            Assert.That(result.Balance, Is.EqualTo(2_965_790L));
            Assert.That(result.TransferBudget, Is.EqualTo(786_316L));
            Assert.That(result.WageBudget, Is.EqualTo(307_368L));
            Assert.That(result.WageBillAggregate, Is.EqualTo(95_000L));
            Assert.That(result.SeasonRevenueAccrued, Is.Zero);
            Assert.That(result.FfpBalanceWindow, Is.Zero);
        }

        /// <summary>Proves repeat calls with identical values produce identical results.</summary>
        [Test]
        public void SettleFinances_IsPureForIdenticalInputs()
        {
            ClubFinances prior = ClubFinances.CreateInitial(100L);

            ClubFinances first = FinanceStep.SettleFinances(in prior, 7, 20, BoardModifier.Identity);
            ClubFinances second = FinanceStep.SettleFinances(in prior, 7, 20, BoardModifier.Identity);

            Assert.That(first.Balance, Is.EqualTo(second.Balance));
            Assert.That(first.TransferBudget, Is.EqualTo(second.TransferBudget));
            Assert.That(first.WageBudget, Is.EqualTo(second.WageBudget));
            Assert.That(first.WageBillAggregate, Is.EqualTo(second.WageBillAggregate));
            Assert.That(first.SeasonRevenueAccrued, Is.EqualTo(second.SeasonRevenueAccrued));
            Assert.That(first.FfpBalanceWindow, Is.EqualTo(second.FfpBalanceWindow));
        }

        /// <summary>Locks the default-value board-modifier failure gate.</summary>
        [Test]
        public void DefaultBoardModifier_FailsLoud()
        {
            ClubFinances prior = ClubFinances.CreateInitial(0L);
            BoardModifier invalid = default;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => FinanceStep.SettleFinances(in prior, 1, 20, in invalid));
        }

        /// <summary>Locks final-position and club-count bounds.</summary>
        [TestCase(0, 20)]
        [TestCase(21, 20)]
        [TestCase(1, 0)]
        [TestCase(1, 1)]
        public void InvalidTableCoordinates_FailLoud(int position, int clubCount)
        {
            ClubFinances prior = ClubFinances.CreateInitial(0L);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => FinanceStep.SettleFinances(in prior, position, clubCount, BoardModifier.Identity));
        }

        /// <summary>Proves transfer cash debits do not touch wage liability or ceilings and debt remains representable.</summary>
        [Test]
        public void TransferDebit_MovesCashOnly_AndMayCreateDebt()
        {
            ClubFinances finances = new ClubFinances
            {
                Balance = 10L,
                TransferBudget = 123L,
                WageBudget = 456L,
                WageBillAggregate = 7L
            };

            FinanceTransaction transaction = new FinanceTransaction(
                FinanceTransactionKind.Debit,
                FinanceLineItem.TransferFee,
                25L);

            FinanceLedger.ApplyTransaction(ref finances, in transaction);

            Assert.That(finances.Balance, Is.EqualTo(-15L));
            Assert.That(finances.WageBillAggregate, Is.EqualTo(7L));
            Assert.That(finances.TransferBudget, Is.EqualTo(123L));
            Assert.That(finances.WageBudget, Is.EqualTo(456L));
        }

        /// <summary>Proves wage transactions move liability only and preserve cash and ceilings.</summary>
        [Test]
        public void WageDebitThenCredit_MovesLiabilityOnly()
        {
            ClubFinances finances = new ClubFinances
            {
                Balance = 500L,
                TransferBudget = 100L,
                WageBudget = 100L,
                WageBillAggregate = 20L
            };

            FinanceTransaction debit = new FinanceTransaction(
                FinanceTransactionKind.Debit,
                FinanceLineItem.PlayerWage,
                12L);
            FinanceLedger.ApplyTransaction(ref finances, in debit);

            Assert.That(finances.Balance, Is.EqualTo(500L));
            Assert.That(finances.WageBillAggregate, Is.EqualTo(32L));

            FinanceTransaction credit = new FinanceTransaction(
                FinanceTransactionKind.Credit,
                FinanceLineItem.StaffWage,
                12L);
            FinanceLedger.ApplyTransaction(ref finances, in credit);

            Assert.That(finances.Balance, Is.EqualTo(500L));
            Assert.That(finances.WageBillAggregate, Is.EqualTo(20L));
            Assert.That(finances.TransferBudget, Is.EqualTo(100L));
            Assert.That(finances.WageBudget, Is.EqualTo(100L));
        }

        /// <summary>Locks the wage-liability underflow failure gate.</summary>
        [Test]
        public void WageCreditBeyondAggregate_FailsLoud()
        {
            ClubFinances finances = ClubFinances.CreateInitial(0L);
            FinanceTransaction transaction = new FinanceTransaction(
                FinanceTransactionKind.Credit,
                FinanceLineItem.PlayerWage,
                1L);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => FinanceLedger.ApplyTransaction(ref finances, in transaction));
        }

        /// <summary>Locks negative magnitudes and undefined enum ordinals as caller errors.</summary>
        [TestCase(-1L, FinanceTransactionKind.Debit, FinanceLineItem.General)]
        [TestCase(1L, (FinanceTransactionKind)255, FinanceLineItem.General)]
        [TestCase(1L, FinanceTransactionKind.Debit, (FinanceLineItem)255)]
        public void MalformedTransaction_FailsLoud(
            long amount,
            FinanceTransactionKind kind,
            FinanceLineItem lineItem)
        {
            ClubFinances finances = ClubFinances.CreateInitial(0L);
            FinanceTransaction transaction = new FinanceTransaction(kind, lineItem, amount);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => FinanceLedger.ApplyTransaction(ref finances, in transaction));
        }

        /// <summary>Proves the transfer-budget query is a pure read.</summary>
        [Test]
        public void AvailableTransferBudget_IsReadOnly()
        {
            ClubFinances finances = new ClubFinances
            {
                Balance = 10L,
                TransferBudget = 123L,
                WageBudget = 456L,
                WageBillAggregate = 7L,
                SeasonRevenueAccrued = 8L,
                FfpBalanceWindow = 9L
            };
            ClubFinances before = finances;

            long available = FinanceLedger.AvailableTransferBudget(in finances);

            Assert.That(available, Is.EqualTo(123L));
            Assert.That(finances.Balance, Is.EqualTo(before.Balance));
            Assert.That(finances.TransferBudget, Is.EqualTo(before.TransferBudget));
            Assert.That(finances.WageBudget, Is.EqualTo(before.WageBudget));
            Assert.That(finances.WageBillAggregate, Is.EqualTo(before.WageBillAggregate));
            Assert.That(finances.SeasonRevenueAccrued, Is.EqualTo(before.SeasonRevenueAccrued));
            Assert.That(finances.FfpBalanceWindow, Is.EqualTo(before.FfpBalanceWindow));
        }

        /// <summary>Locks the observer to detached value copies of the four Stage-2 fields.</summary>
        [Test]
        public void FinancesViewModel_CopiesOnlyObserverFields()
        {
            ClubFinances finances = new ClubFinances
            {
                Balance = -50L,
                TransferBudget = 123L,
                WageBudget = 456L,
                WageBillAggregate = 7L,
                SeasonRevenueAccrued = 8L,
                FfpBalanceWindow = 9L
            };

            FinancesViewModel view = FinancesViewModel.From(in finances);

            Assert.That(view.Balance, Is.EqualTo(-50L));
            Assert.That(view.TransferBudget, Is.EqualTo(123L));
            Assert.That(view.WageBudget, Is.EqualTo(456L));
            Assert.That(view.WageBillAggregate, Is.EqualTo(7L));
        }

        /// <summary>Proves every T0 consuming seam rejects negative ceiling/liability state.</summary>
        [Test]
        public void NegativeCoherenceFields_FailAtConsumingSeams()
        {
            ClubFinances negativeTransfer = new ClubFinances { TransferBudget = -1L, WageBudget = 0L, WageBillAggregate = 0L };
            ClubFinances negativeWage = new ClubFinances { TransferBudget = 0L, WageBudget = -1L, WageBillAggregate = 0L };
            ClubFinances negativeAggregate = new ClubFinances { TransferBudget = 0L, WageBudget = 0L, WageBillAggregate = -1L };

            Assert.Throws<ArgumentOutOfRangeException>(() => FinanceLedger.AvailableTransferBudget(in negativeTransfer));
            Assert.Throws<ArgumentOutOfRangeException>(() => FinancesViewModel.From(in negativeWage));

            FinanceTransaction transaction = new FinanceTransaction(FinanceTransactionKind.Credit, FinanceLineItem.General, 0L);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => FinanceLedger.ApplyTransaction(ref negativeAggregate, in transaction));
        }

        /// <summary>Statically locks currency-bearing/value-copy fields away from floating-point and decimal types.</summary>
        [Test]
        public void AccountingSurface_UsesIntegerFieldsOnly()
        {
            Type[] types =
            {
                typeof(ClubFinances),
                typeof(FinanceTransaction),
                typeof(BoardModifier),
                typeof(FinancesViewModel)
            };

            FieldInfo[] fields = types
                .SelectMany(type => type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                .ToArray();

            Assert.That(fields, Is.Not.Empty);
            Assert.That(
                fields.Any(field => field.FieldType == typeof(float) || field.FieldType == typeof(double) || field.FieldType == typeof(decimal)),
                Is.False);
        }

        /// <summary>Locks the fixed catalogue invariants and T0 fallback magnitudes.</summary>
        [Test]
        public void ConstantCatalogue_SatisfiesRequiredInvariants()
        {
            Assert.That(ClubFinancesConstants.PERMILLE_DENOM, Is.EqualTo(1000));
            Assert.That(ClubFinancesConstants.BOARD_MODIFIER_IDENTITY_PERMILLE, Is.EqualTo(1000));
            Assert.That(ClubFinancesConstants.PrizeMoneyLastPlace, Is.LessThanOrEqualTo(ClubFinancesConstants.PrizeMoneyWinner));
            Assert.That(ClubFinancesConstants.ClubFinancesBudgetCeilingMax, Is.GreaterThanOrEqualTo(0L));
        }
    }
}

#region VersionHistory
// Version | Date       | Author        | Change
// --------|------------|---------------|----------------------------------------------
// 1.0     | 2026-09-04 | Codex / Anton | Initial #40 T0 acceptance coverage.
#endregion
