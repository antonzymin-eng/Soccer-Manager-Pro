// ============================================================================
// File:     src/transfers/tests/TransfersT0Tests.cs
// Created:  2026-09-12
// Modified: 2026-10-20 (#31 T2)
// Author:   —
// Specs:    Spec #31 §5.1-§5.6 (T0 valuation, offer, bid, stable-id re-home, window, fail-loud tests)
// Purpose:  Locks the first D5 implementation slice against the approved minimal-tier transfer contract.
// ============================================================================

using System;
using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.ClubFinances;
using TacticalDirector.PlayerDatabase;

using ClubFinancesState = TacticalDirector.ClubFinances.ClubFinances;

namespace TacticalDirector.Transfers.Tests
{
    [TestFixture]
    public sealed class TransfersT0Tests
    {
        private const int MANAGED_CLUB_ID = 10;
        private const int COUNTERPARTY_CLUB_ID = 11;
        private const int MANAGED_PLAYER_ID = MANAGED_CLUB_ID * PlayerDatabaseConstants.CLUB_SQUAD_SIZE;
        private const int COUNTERPARTY_PLAYER_ID = COUNTERPARTY_CLUB_ID * PlayerDatabaseConstants.CLUB_SQUAD_SIZE;

        [Test]
        public void ValuePlayer_IsDeterministic_WeakFootExcluded_AndGoldenVectorPinned()
        {
            PlayerAttributes first = PlayerAttributes.CreateDefault();
            PlayerAttributes weakFootChangedAttributes = first;
            weakFootChangedAttributes.WeakFootRating = PlayerDatabaseConstants.WEAK_FOOT_MAX;

            long firstValue = PlayerValuation.ValuePlayer(in first, 25);
            long repeatedValue = PlayerValuation.ValuePlayer(in first, 25);
            long weakFootChanged = PlayerValuation.ValuePlayer(in weakFootChangedAttributes, 25);

            Assert.AreEqual(100_000L, firstValue);
            Assert.AreEqual(firstValue, repeatedValue);
            Assert.AreEqual(firstValue, weakFootChanged);

            PlayerAttributes onePointHigher = first;
            onePointHigher.Pace++;
            Assert.AreEqual(100_322L, PlayerValuation.ValuePlayer(in onePointHigher, 25));
        }

        [Test]
        public void AgeCurvePermille_HasStrictDiscounts_PeakBoundary_AndFloor()
        {
            int young = PlayerValuation.AgeCurvePermille(TransfersConstants.PeakAgeMin - 1);
            int peakStart = PlayerValuation.AgeCurvePermille(TransfersConstants.PeakAgeMin);
            int peakEnd = PlayerValuation.AgeCurvePermille(TransfersConstants.PeakAgeMax);
            int old = PlayerValuation.AgeCurvePermille(TransfersConstants.PeakAgeMax + 1);
            int farOld = PlayerValuation.AgeCurvePermille(TransfersConstants.PeakAgeMax + 10_000);

            Assert.AreEqual(TransfersConstants.PERMILLE_DENOM, peakStart);
            Assert.AreEqual(TransfersConstants.PERMILLE_DENOM, peakEnd);
            Assert.Less(young, peakStart);
            Assert.Less(old, peakEnd);
            Assert.AreEqual(TransfersConstants.MinimumAgeMultiplierPermille, farOld);
        }

        [Test]
        public void ClubNeedMultiplier_ValuesScarcityAboveNeutralAndOverstockBelowNeutral()
        {
            int neutralCount = PlayerDatabaseConstants.CLUB_SQUAD_SIZE / PlayerDatabaseConstants.POSITION_COUNT;
            int scarce = PlayerValuation.ClubNeedMultiplierPermille(0);
            int neutral = PlayerValuation.ClubNeedMultiplierPermille(neutralCount);
            int overstocked = PlayerValuation.ClubNeedMultiplierPermille(PlayerDatabaseConstants.CLUB_SQUAD_SIZE);

            Assert.Greater(scarce, neutral);
            Assert.AreEqual(TransfersConstants.PERMILLE_DENOM, neutral);
            Assert.Less(overstocked, neutral);
            Assert.Greater(overstocked, 0);
            Assert.Greater(TransfersConstants.NegotiationCounterBandPermille, 0);
            Assert.Less(TransfersConstants.NegotiationCounterBandPermille, TransfersConstants.PERMILLE_DENOM);
        }

        [Test]
        public void EvaluateOffer_UsesAcceptedCounterAndRejectedBands_ForBuyAndSell()
        {
            const long valuation = 500L;
            long band = NegotiationEngine.CounterBandAmount(valuation);
            Offer buyAccepted = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation, 0L, 1, true);
            Offer buyCounter = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation - 1L, 0L, 1, true);
            Offer buyRejected = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation - band - 1L, 0L, 1, true);
            Offer sellAccepted = new Offer(MANAGED_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation, 0L, 1, false);
            Offer sellCounter = new Offer(MANAGED_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation + 1L, 0L, 1, false);
            Offer sellRejected = new Offer(MANAGED_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation + band + 1L, 0L, 1, false);

            Assert.AreEqual(NegotiationOutcome.Accepted, NegotiationEngine.EvaluateOffer(in buyAccepted, valuation));
            Assert.AreEqual(NegotiationOutcome.CounterOffered, NegotiationEngine.EvaluateOffer(in buyCounter, valuation));
            Assert.AreEqual(NegotiationOutcome.Rejected, NegotiationEngine.EvaluateOffer(in buyRejected, valuation));
            Assert.AreEqual(NegotiationOutcome.Accepted, NegotiationEngine.EvaluateOffer(in sellAccepted, valuation));
            Assert.AreEqual(NegotiationOutcome.CounterOffered, NegotiationEngine.EvaluateOffer(in sellCounter, valuation));
            Assert.AreEqual(NegotiationOutcome.Rejected, NegotiationEngine.EvaluateOffer(in sellRejected, valuation));
        }

        [Test]
        public void EvaluateOffer_MalformedWageOrLength_FailsLoudAtReusableSeam()
        {
            Offer negativeWage = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, 500L, -1L, 1, true);
            Offer zeroLength = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, 500L, 0L, 0, true);

            Assert.Throws<ArgumentOutOfRangeException>(() => NegotiationEngine.EvaluateOffer(in negativeWage, 500L));
            Assert.Throws<ArgumentOutOfRangeException>(() => NegotiationEngine.EvaluateOffer(in zeroLength, 500L));
        }

        [Test]
        public void TransferWindow_IsInclusiveAtBothEdges()
        {
            TransferWindow window = new TransferWindow(100U, 120U);

            Assert.IsTrue(window.IsWindowOpen(100U));
            Assert.IsTrue(window.IsWindowOpen(120U));
            Assert.IsFalse(window.IsWindowOpen(99U));
            Assert.IsFalse(window.IsWindowOpen(121U));
        }

        [Test]
        public void SubmitBid_AcceptedBuy_PostsFeeOnly_CommitsSpend_AndInsertsContractUnderTheStableId()
        {
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 77_000L);
            long valuation = CounterpartyValue(roster, COUNTERPARTY_PLAYER_ID);
            Offer offer = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation, 12_345L, 3, true);

            TransferSubmissionOutcome outcome = commands.SubmitBid(in offer, 110U, ref finances, state);

            Assert.AreEqual(TransferSubmissionOutcome.Accepted, outcome);
            Assert.AreEqual(5_000_000L - valuation, finances.Balance);
            Assert.AreEqual(1_000_000L, finances.TransferBudget);
            Assert.AreEqual(77_000L, finances.WageBillAggregate);
            Assert.AreEqual(valuation, state.CommittedSpendThisWindow);
            Assert.AreEqual(1, roster.CommitCount);

            // ERR-031-005: the player keeps his id and the contract is keyed by it.
            Assert.IsTrue(state.TryGetContract(COUNTERPARTY_PLAYER_ID, out Contract contract));
            Assert.AreEqual(COUNTERPARTY_PLAYER_ID, contract.PlayerId);
            Assert.AreEqual(12_345L, contract.WagePerPeriod);
            Assert.AreEqual(3, contract.LengthSeasons);
            Assert.IsTrue(roster.TryGetPlayer(COUNTERPARTY_PLAYER_ID, out PlayerRecord _, out int club));
            Assert.AreEqual(MANAGED_CLUB_ID, club, "the re-home moved the record, id unchanged");
        }

        [Test]
        public void SubmitBid_PlayerWhoseIdIsOutsideHisClubsPackedRange_IsOwnedByTheRoster()
        {
            // A regen's id comes from #28's monotonic cursor, far past every club's packed range: id 9000
            // would read as "club 360" under id/CLUB_SQUAD_SIZE. Ownership must come from the roster.
            const int RegenId = 9000;
            FakeRosterPort roster = new FakeRosterPort();
            roster.Add(PlayerRecord.CreateDefault(RegenId), COUNTERPARTY_CLUB_ID);
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            long valuation = CounterpartyValue(roster, RegenId);
            Offer offer = new Offer(RegenId, COUNTERPARTY_CLUB_ID, valuation, 500L, 2, true);

            TransferSubmissionOutcome outcome = commands.SubmitBid(in offer, 110U, ref finances, state);

            Assert.AreEqual(TransferSubmissionOutcome.Accepted, outcome);
            Assert.IsTrue(state.TryGetContract(RegenId, out Contract contract));
            Assert.AreEqual(2, contract.LengthSeasons);
        }

        [Test]
        public void SubmitBid_DirectionThatDisagreesWithRosterOwnership_FailsLoud()
        {
            // The id 260 is club 10's packed range, but the roster says club 11 holds him: a "sell" of him
            // by the managed club 10 is a caller-contract bug, judged by the roster, not the arithmetic.
            FakeRosterPort roster = new FakeRosterPort();
            roster.Add(PlayerRecord.CreateDefault(MANAGED_PLAYER_ID), COUNTERPARTY_CLUB_ID);
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            state.InsertContract(new Contract(MANAGED_PLAYER_ID, 1_000L, 2));
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            Offer sell = new Offer(MANAGED_PLAYER_ID, COUNTERPARTY_CLUB_ID, 1L, 0L, 1, false);

            Assert.Throws<InvalidOperationException>(() => commands.SubmitBid(in sell, 110U, ref finances, state));
            Assert.AreEqual(0, roster.CommitCount);
        }

        [Test]
        public void SubmitBid_CounterOffer_MutatesNothing()
        {
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 77_000L);
            long valuation = CounterpartyValue(roster, COUNTERPARTY_PLAYER_ID);
            Offer offer = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation - 1L, 1_000L, 3, true);

            TransferSubmissionOutcome outcome = commands.SubmitBid(in offer, 110U, ref finances, state);

            Assert.AreEqual(TransferSubmissionOutcome.CounterOffered, outcome);
            Assert.AreEqual(5_000_000L, finances.Balance);
            Assert.AreEqual(0L, state.CommittedSpendThisWindow);
            Assert.AreEqual(0, state.ContractCount);
            Assert.AreEqual(0, roster.CommitCount);
        }

        [Test]
        public void SubmitBid_RejectedBuy_MutatesNothing()
        {
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 77_000L);
            long valuation = CounterpartyValue(roster, COUNTERPARTY_PLAYER_ID);
            long band = NegotiationEngine.CounterBandAmount(valuation);
            Offer offer = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation - band - 1L, 1_000L, 3, true);

            TransferSubmissionOutcome outcome = commands.SubmitBid(in offer, 110U, ref finances, state);

            Assert.AreEqual(TransferSubmissionOutcome.Rejected, outcome);
            Assert.AreEqual(5_000_000L, finances.Balance);
            Assert.AreEqual(0L, state.CommittedSpendThisWindow);
            Assert.AreEqual(0, state.ContractCount);
            Assert.AreEqual(0, roster.CommitCount);
        }

        [Test]
        public void SubmitBid_SecondBuyOverStaticCeiling_ReturnsInsufficientBudgetWithoutSecondMutation()
        {
            FakeRosterPort roster = CreateRosterWithTwoCounterpartyPlayers();
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            long firstValuation = CounterpartyValue(roster, COUNTERPARTY_PLAYER_ID);

            // The first buy really leaves the seller (stable ids: the record re-homes), so the seller's
            // positional stock drops and the second player is valued AFTER it — scarcer, so dearer.
            long secondValuationAfterFirstSale = PlayerValuation.CounterpartyValue(
                PlayerAttributes.CreateDefault(), 25, 0);
            Assert.Greater(secondValuationAfterFirstSale, firstValuation);
            ClubFinancesState finances = CreateFinances(
                firstValuation + secondValuationAfterFirstSale - 1L, 5_000_000L, 0L);
            Offer first = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, firstValuation, 1_000L, 2, true);

            Assert.AreEqual(TransferSubmissionOutcome.Accepted, commands.SubmitBid(in first, 110U, ref finances, state));
            long balanceAfterFirst = finances.Balance;
            long secondValuation = CounterpartyValue(roster, COUNTERPARTY_PLAYER_ID + 1);
            Assert.AreEqual(secondValuationAfterFirstSale, secondValuation);
            Offer second = new Offer(COUNTERPARTY_PLAYER_ID + 1, COUNTERPARTY_CLUB_ID, secondValuation, 1_000L, 2, true);

            TransferSubmissionOutcome secondOutcome = commands.SubmitBid(in second, 110U, ref finances, state);

            Assert.AreEqual(TransferSubmissionOutcome.InsufficientBudget, secondOutcome);
            Assert.AreEqual(balanceAfterFirst, finances.Balance);
            Assert.AreEqual(firstValuation, state.CommittedSpendThisWindow);
            Assert.AreEqual(1, roster.CommitCount);
            Assert.AreEqual(1, state.ContractCount);
        }

        [Test]
        public void SubmitBid_OverBudgetBuyWithExtremeDebt_ReturnsInsufficientBudgetBeforeFinanceStaging()
        {
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            long valuation = CounterpartyValue(roster, COUNTERPARTY_PLAYER_ID);
            ClubFinancesState finances = CreateFinances(
                valuation - 1L,
                long.MinValue + valuation - 1L,
                0L);
            Offer offer = new Offer(
                COUNTERPARTY_PLAYER_ID,
                COUNTERPARTY_CLUB_ID,
                valuation,
                1_000L,
                2,
                true);
            long balanceBefore = finances.Balance;

            TransferSubmissionOutcome outcome = commands.SubmitBid(in offer, 110U, ref finances, state);

            Assert.AreEqual(TransferSubmissionOutcome.InsufficientBudget, outcome);
            Assert.AreEqual(balanceBefore, finances.Balance);
            Assert.AreEqual(0L, state.CommittedSpendThisWindow);
            Assert.AreEqual(0, state.ContractCount);
            Assert.AreEqual(0, roster.CommitCount);
        }
        [Test]
        public void SubmitBid_FullDestination_ReturnsSquadFullWithoutFinanceOrContractMutation()
        {
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            roster.HasDestinationCapacity = false;
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            long valuation = CounterpartyValue(roster, COUNTERPARTY_PLAYER_ID);
            Offer offer = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation, 1_000L, 2, true);

            TransferSubmissionOutcome outcome = commands.SubmitBid(in offer, 110U, ref finances, state);

            Assert.AreEqual(TransferSubmissionOutcome.SquadFull, outcome);
            Assert.AreEqual(5_000_000L, finances.Balance);
            Assert.AreEqual(0L, state.CommittedSpendThisWindow);
            Assert.AreEqual(0, state.ContractCount);
            Assert.AreEqual(0, roster.CommitCount);
        }

        [Test]
        public void SubmitBid_Sell_CommitsRosterBeforeRemovingContract_AndCreditsFee()
        {
            FakeRosterPort roster = CreateRosterWithManagedPlayer();
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            Contract seeded = new Contract(MANAGED_PLAYER_ID, 2_000L, 2);
            state.InsertContract(in seeded);
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 20_000L);
            long valuation = CounterpartyValue(roster, MANAGED_PLAYER_ID);
            Offer offer = new Offer(MANAGED_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation, 1_000L, 2, false);
            bool contractPresentAtCommit = false;
            roster.BeforeCommit = () => contractPresentAtCommit = state.TryGetContract(MANAGED_PLAYER_ID, out Contract ignored);

            TransferSubmissionOutcome outcome = commands.SubmitBid(in offer, 110U, ref finances, state);

            Assert.AreEqual(TransferSubmissionOutcome.Accepted, outcome);
            Assert.IsTrue(contractPresentAtCommit);
            Assert.AreEqual(5_000_000L + valuation, finances.Balance);
            Assert.AreEqual(20_000L, finances.WageBillAggregate);
            Assert.AreEqual(0L, state.CommittedSpendThisWindow);
            Assert.AreEqual(0, state.ContractCount);
            Assert.AreEqual(1, roster.CommitCount);
        }

        [Test]
        public void SubmitBid_PositionalNeedExcludesNegotiatedPlayerSymmetrically()
        {
            PlayerAttributes attributes = PlayerAttributes.CreateDefault();
            long zeroStockValue = PlayerValuation.CounterpartyValue(in attributes, 25, 0);

            FakeRosterPort buyRoster = CreateRosterWithCounterpartyPlayer();
            TransferCommands buyCommands = new TransferCommands(buyRoster);
            TransfersState buyState = CreateOpenState();
            ClubFinancesState buyFinances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            Offer buyNearMiss = new Offer(
                COUNTERPARTY_PLAYER_ID,
                COUNTERPARTY_CLUB_ID,
                zeroStockValue - 1L,
                1_000L,
                2,
                true);

            FakeRosterPort sellRoster = CreateRosterWithManagedPlayer();
            TransferCommands sellCommands = new TransferCommands(sellRoster);
            TransfersState sellState = CreateOpenState();
            Contract seeded = new Contract(MANAGED_PLAYER_ID, 1_000L, 2);
            sellState.InsertContract(in seeded);
            ClubFinancesState sellFinances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            Offer sellNearMiss = new Offer(
                MANAGED_PLAYER_ID,
                COUNTERPARTY_CLUB_ID,
                zeroStockValue + 1L,
                1_000L,
                2,
                false);

            Assert.AreEqual(
                TransferSubmissionOutcome.CounterOffered,
                buyCommands.SubmitBid(in buyNearMiss, 110U, ref buyFinances, buyState));
            Assert.AreEqual(
                TransferSubmissionOutcome.CounterOffered,
                sellCommands.SubmitBid(in sellNearMiss, 110U, ref sellFinances, sellState));
        }

        [Test]
        public void SubmitBid_RosterCommitThatDoesNotLand_FailsBeforeLocalStateMutation()
        {
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            roster.CommitDoesNotLand = true;
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            long valuation = CounterpartyValue(roster, COUNTERPARTY_PLAYER_ID);
            Offer offer = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation, 1_000L, 2, true);

            Assert.Throws<InvalidOperationException>(() => commands.SubmitBid(in offer, 110U, ref finances, state));
            Assert.AreEqual(5_000_000L, finances.Balance);
            Assert.AreEqual(0L, state.CommittedSpendThisWindow);
            Assert.AreEqual(0, state.ContractCount);
            Assert.AreEqual(1, roster.CommitCount);
        }

        [Test]
        public void SubmitBid_SellRosterCommitThatDoesNotLand_RetainsLocalContractAndFinance()
        {
            FakeRosterPort roster = CreateRosterWithManagedPlayer();
            roster.CommitDoesNotLand = true;
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            Contract seeded = new Contract(MANAGED_PLAYER_ID, 2_000L, 2);
            state.InsertContract(in seeded);
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 20_000L);
            long valuation = CounterpartyValue(roster, MANAGED_PLAYER_ID);
            Offer offer = new Offer(MANAGED_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation, 1_000L, 2, false);

            Assert.Throws<InvalidOperationException>(() => commands.SubmitBid(in offer, 110U, ref finances, state));
            Assert.AreEqual(5_000_000L, finances.Balance);
            Assert.AreEqual(20_000L, finances.WageBillAggregate);
            Assert.AreEqual(0L, state.CommittedSpendThisWindow);
            Assert.IsTrue(state.TryGetContract(MANAGED_PLAYER_ID, out Contract retained));
            Assert.AreEqual(MANAGED_PLAYER_ID, retained.PlayerId);
            Assert.AreEqual(1, roster.CommitCount);
        }

        [Test]
        public void SubmitBid_OutsideWindow_FailsBeforeValuationOrMutation()
        {
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            Offer offer = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, 100L, 0L, 1, true);

            Assert.Throws<InvalidOperationException>(() => commands.SubmitBid(in offer, 99U, ref finances, state));
            Assert.AreEqual(5_000_000L, finances.Balance);
            Assert.AreEqual(0, roster.CommitCount);
            Assert.AreEqual(0, state.ContractCount);
        }

        [Test]
        public void SubmitBid_MalformedOffer_FailsLoud()
        {
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            Offer negativeFee = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, -1L, 0L, 1, true);
            Offer negativeWage = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, 1L, -1L, 1, true);
            Offer zeroLength = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, 1L, 0L, 0, true);

            Assert.Throws<ArgumentOutOfRangeException>(() => commands.SubmitBid(in negativeFee, 110U, ref finances, state));
            Assert.Throws<ArgumentOutOfRangeException>(() => commands.SubmitBid(in negativeWage, 110U, ref finances, state));
            Assert.Throws<ArgumentOutOfRangeException>(() => commands.SubmitBid(in zeroLength, 110U, ref finances, state));
            Assert.AreEqual(5_000_000L, finances.Balance);
            Assert.AreEqual(0, roster.CommitCount);
        }

        [Test]
        public void InsertContract_BeyondTheSquadSize_FailsLoud_AndLeavesTheSetUntouched()
        {
            TransfersState state = CreateOpenState();
            for (int i = 0; i < PlayerDatabaseConstants.CLUB_SQUAD_SIZE; i++)
            {
                Contract contract = new Contract(MANAGED_PLAYER_ID + i, 1_000L, 2);
                state.InsertContract(in contract);
            }

            Contract extra = new Contract(MANAGED_PLAYER_ID + PlayerDatabaseConstants.CLUB_SQUAD_SIZE, 1_000L, 2);
            var e = Assert.Throws<InvalidOperationException>(() => state.InsertContract(in extra));

            StringAssert.Contains("at most", e.Message);
            Assert.AreEqual(PlayerDatabaseConstants.CLUB_SQUAD_SIZE, state.ContractCount);
            Assert.IsFalse(state.TryGetContract(extra.PlayerId, out Contract _));
        }

        [Test]
        public void SubmitBid_BuyOfAPlayerWhoseIdAlreadyHoldsAManagedContract_FailsLoudBeforeTheRosterCommit()
        {
            // The contract set and the roster disagree: the external player already has a managed contract.
            // The pre-check must fire before RequestRosterCommit, because the port cannot be unwound.
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            Contract stray = new Contract(COUNTERPARTY_PLAYER_ID, 777L, 1);
            state.InsertContract(in stray);
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            long valuation = CounterpartyValue(roster, COUNTERPARTY_PLAYER_ID);
            Offer offer = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation, 1_000L, 2, true);

            Assert.Throws<InvalidOperationException>(() => commands.SubmitBid(in offer, 110U, ref finances, state));

            Assert.AreEqual(0, roster.CommitCount);
            Assert.AreEqual(5_000_000L, finances.Balance);
            Assert.AreEqual(0L, state.CommittedSpendThisWindow);
            Assert.AreEqual(1, state.ContractCount);
            Assert.IsTrue(state.TryGetContract(COUNTERPARTY_PLAYER_ID, out Contract retained));
            Assert.AreEqual(777L, retained.WagePerPeriod);
        }

        [TestCase(-1)]
        [TestCase(PlayerDatabaseConstants.CLUB_SQUAD_SIZE + 1)]
        public void SubmitBid_InvalidPositionalStockFromThePort_FailsLoudWithoutCommit(int portCount)
        {
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            roster.PositionalStockOverride = portCount;
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            Offer offer = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, 1L, 1_000L, 2, true);

            Assert.Throws<InvalidOperationException>(() => commands.SubmitBid(in offer, 110U, ref finances, state));

            Assert.AreEqual(0, roster.CommitCount);
            Assert.AreEqual(5_000_000L, finances.Balance);
            Assert.AreEqual(0, state.ContractCount);
        }

        [Test]
        public void SubmitBid_BuyWithZeroPositionalStockAtTheSellingClub_FailsLoudWithoutCommit()
        {
            // The player is on the counterparty's roster, so a count of 0 at his position is inconsistent.
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            roster.PositionalStockOverride = 0;
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            Offer offer = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, 1L, 1_000L, 2, true);

            Assert.Throws<InvalidOperationException>(() => commands.SubmitBid(in offer, 110U, ref finances, state));

            Assert.AreEqual(0, roster.CommitCount);
            Assert.AreEqual(5_000_000L, finances.Balance);
            Assert.AreEqual(0, state.ContractCount);
        }

        // ── #31 T2 command locks (ERR-031-007/009/010) ───────────────────────────────

        [Test]
        public void SubmitBid_AReleaseThatDepletesTheSellingSquad_ReturnsSquadDepleted_WithNoMutation()
        {
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            roster.CanReleasePlayers = false;
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            long valuation = CounterpartyValue(roster, COUNTERPARTY_PLAYER_ID);
            Offer offer = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation, 1_000L, 2, true);

            Assert.AreEqual(TransferSubmissionOutcome.SquadDepleted, commands.SubmitBid(in offer, 110U, ref finances, state));
            Assert.AreEqual(5_000_000L, finances.Balance);
            Assert.AreEqual(0L, state.CommittedSpendThisWindow);
            Assert.AreEqual(0, state.ContractCount);
            Assert.AreEqual(0, roster.CommitCount);
        }

        [Test]
        public void SubmitBid_AnUnknownCounterpartyClub_FailsLoud_BeforeAnyValuation()
        {
            FakeRosterPort roster = CreateRosterWithManagedPlayer();
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            state.InsertContract(new Contract(MANAGED_PLAYER_ID, 1_000L, 2));
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            Offer sellToNowhere = new Offer(MANAGED_PLAYER_ID, 999, 1L, 0L, 1, false);

            Assert.Throws<ArgumentOutOfRangeException>(() => commands.SubmitBid(in sellToNowhere, 110U, ref finances, state));
            Assert.AreEqual(0, roster.CommitCount);
            Assert.AreEqual(1, state.ContractCount);
        }

        [Test]
        public void SubmitBid_AnUncontractedManagedPlayer_IsSellable()
        {
            // ERR-031-009: a contract that aged out leaves the player un-contracted but on the roster.
            FakeRosterPort roster = CreateRosterWithManagedPlayer();
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            long valuation = CounterpartyValue(roster, MANAGED_PLAYER_ID);
            Offer offer = new Offer(MANAGED_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation, 1_000L, 2, false);

            Assert.AreEqual(TransferSubmissionOutcome.Accepted, commands.SubmitBid(in offer, 110U, ref finances, state));
            Assert.AreEqual(5_000_000L + valuation, finances.Balance);
            Assert.IsTrue(roster.TryGetPlayer(MANAGED_PLAYER_ID, out PlayerRecord _, out int club));
            Assert.AreEqual(COUNTERPARTY_CLUB_ID, club);
        }

        [Test]
        public void InsertContract_DefaultContract_FailsLoud()
        {
            TransfersState state = CreateOpenState();
            Contract invalid = default(Contract);

            Assert.Throws<ArgumentOutOfRangeException>(() => state.InsertContract(in invalid));
        }

        [Test]
        public void T0StateAndCommands_ContainNoFloatOrDoubleFields()
        {
            Type[] types =
            {
                typeof(Contract),
                typeof(Offer),
                typeof(TransferWindow),
                typeof(ClubTransferState),
                typeof(TransfersState),
                typeof(TransferCommands)
            };

            for (int i = 0; i < types.Length; i++)
            {
                System.Reflection.FieldInfo[] fields = types[i].GetFields(
                    System.Reflection.BindingFlags.Instance
                    | System.Reflection.BindingFlags.Static
                    | System.Reflection.BindingFlags.Public
                    | System.Reflection.BindingFlags.NonPublic);
                for (int j = 0; j < fields.Length; j++)
                {
                    Assert.AreNotEqual(typeof(float), fields[j].FieldType);
                    Assert.AreNotEqual(typeof(double), fields[j].FieldType);
                }
            }
        }

        private static TransfersState CreateOpenState()
        {
            TransferWindow window = new TransferWindow(100U, 120U);
            return new TransfersState(MANAGED_CLUB_ID, in window);
        }

        private static ClubFinancesState CreateFinances(long transferBudget, long balance, long wageBill)
        {
            ClubFinancesState finances = ClubFinancesState.CreateInitial(balance);
            finances.TransferBudget = transferBudget;
            finances.WageBudget = 500_000L;
            finances.WageBillAggregate = wageBill;
            return finances;
        }

        private static FakeRosterPort CreateRosterWithCounterpartyPlayer()
        {
            FakeRosterPort roster = new FakeRosterPort();
            roster.Add(PlayerRecord.CreateDefault(COUNTERPARTY_PLAYER_ID), COUNTERPARTY_CLUB_ID);
            return roster;
        }

        private static FakeRosterPort CreateRosterWithTwoCounterpartyPlayers()
        {
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            PlayerRecord second = PlayerRecord.CreateDefault(COUNTERPARTY_PLAYER_ID + 1);
            roster.Add(second, COUNTERPARTY_CLUB_ID);
            return roster;
        }

        private static FakeRosterPort CreateRosterWithManagedPlayer()
        {
            FakeRosterPort roster = new FakeRosterPort();
            roster.Add(PlayerRecord.CreateDefault(MANAGED_PLAYER_ID), MANAGED_CLUB_ID);
            return roster;
        }

        private static long CounterpartyValue(FakeRosterPort roster, int playerId)
        {
            Assert.IsTrue(roster.TryGetPlayer(playerId, out PlayerRecord player, out int ownerClubId));
            int stock = roster.CountPlayersAtPosition(COUNTERPARTY_CLUB_ID, player.Position);
            if (ownerClubId == COUNTERPARTY_CLUB_ID)
            {
                stock--;
            }

            return PlayerValuation.CounterpartyValue(in player.Attributes, player.Age, stock);
        }

        private sealed class FakeRosterPort : ITransferRosterPort
        {
            private readonly Dictionary<int, PlayerRecord> _players = new Dictionary<int, PlayerRecord>();
            private readonly Dictionary<int, int> _clubOf = new Dictionary<int, int>();

            public bool HasDestinationCapacity { get; set; } = true;

            // F2 breach injection: the commit counts but the player never reaches the destination.
            public bool CommitDoesNotLand { get; set; }

            public int CommitCount { get; private set; }

            public Action BeforeCommit { get; set; }

            // Port-breach injection: when set, every positional-stock query returns this value verbatim.
            public int? PositionalStockOverride { get; set; }

            // ERR-031-007: whether the releasing club keeps a matchday squad.
            public bool CanReleasePlayers { get; set; } = true;

            // ERR-031-010: the league's club universe.
            public HashSet<int> Clubs { get; } = new HashSet<int> { MANAGED_CLUB_ID, COUNTERPARTY_CLUB_ID };

            public bool CarriesClub(int clubId) => Clubs.Contains(clubId);

            public bool CanRelease(int clubId, int playerId) => CanReleasePlayers;

            public void Add(PlayerRecord player, int clubId)
            {
                _players.Add(player.PlayerId, player);
                _clubOf.Add(player.PlayerId, clubId);
            }

            public bool TryGetPlayer(int playerId, out PlayerRecord player, out int clubId)
            {
                clubId = -1;
                return _players.TryGetValue(playerId, out player) && _clubOf.TryGetValue(playerId, out clubId);
            }

            public int CountPlayersAtPosition(int clubId, PlayerPosition position)
            {
                if (PositionalStockOverride.HasValue)
                {
                    return PositionalStockOverride.Value;
                }

                int count = 0;
                foreach (KeyValuePair<int, PlayerRecord> entry in _players)
                {
                    if (_clubOf[entry.Key] == clubId && entry.Value.Position == position)
                    {
                        count++;
                    }
                }

                return count;
            }

            public bool TryPreviewRosterCommit(int fromClubId, int toClubId, int playerId)
            {
                // ERR-031-010: false means ONLY "destination full"; anything else is a defect and throws.
                if (!_clubOf.TryGetValue(playerId, out int owner) || owner != fromClubId
                    || !Clubs.Contains(fromClubId) || !Clubs.Contains(toClubId))
                {
                    throw new InvalidOperationException("Fake port: preview of an impossible move.");
                }

                return HasDestinationCapacity;
            }

            public void RequestRosterCommit(int fromClubId, int toClubId, int playerId)
            {
                BeforeCommit?.Invoke();
                CommitCount++;
                if (!CommitDoesNotLand)
                {
                    _clubOf[playerId] = toClubId;
                }
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 valuation/offer/bid/re-key/window/fail-loud coverage. |
// | 1.1     | 2026-09-14 | —      | Lock finance alias, Codex term validation, counter-offer band, and positional need. |
// | 1.2     | 2026-09-14 | —      | Review corrections: golden/fractional valuation, strict age boundaries, typed budget/full outcomes, symmetric need, pre-commit port-breach lock. |
// | 1.3     | 2026-09-14 | —      | Add sell-direction preview/commit mismatch regression locking retained local contract/finance on a port breach. |
// | 1.4     | 2026-09-21 | —      | ERR-031-002: lock extreme coherent debt + over-budget buy returning InsufficientBudget before checked finance staging, with no mutation/roster commit. |
// | 1.5     | 2026-10-19 | —      | ERR-031-005: stable-id fake port (explicit club membership); off-range-id ownership and roster-judged direction locks; commit breach is 'did not land'; the re-key test is deleted with the handler. |
// | 1.6     | 2026-10-20 | —      | AR round 1 (ERR-031-006 cycle) M1/M5/L1: 26th-contract refusal, buy duplicate-contract pre-check, invalid positional-count and buy zero-stock throws (FakeRosterPort.PositionalStockOverride); header reworded to stable-id semantics. |
// | 1.7     | 2026-10-20 | —      | #31 T2: fake port gains CarriesClub/CanRelease and throws on an impossible preview (ERR-031-010); locks SquadDepleted (ERR-031-007), unknown-counterparty fail-loud (ERR-031-010) and the un-contracted sale (ERR-031-009). |
#endregion