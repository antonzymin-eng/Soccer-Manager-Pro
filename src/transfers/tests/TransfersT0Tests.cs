// ============================================================================
// File:     src/transfers/tests/TransfersT0Tests.cs
// Created:  2026-09-12
// Modified: 2026-09-21
// Author:   —
// Specs:    Spec #31 §5.1-§5.6 (T0 valuation, offer, bid, re-key, window, fail-loud tests)
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
        public void SubmitBid_AcceptedBuy_PostsFeeOnly_CommitsSpend_AndInsertsRekeyedContract()
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
            Assert.IsTrue(state.TryGetContract(MANAGED_PLAYER_ID, out Contract contract));
            Assert.AreEqual(MANAGED_PLAYER_ID, contract.PlayerId);
            Assert.AreEqual(12_345L, contract.WagePerPeriod);
            Assert.AreEqual(3, contract.LengthSeasons);
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
            long secondValuation = CounterpartyValue(roster, COUNTERPARTY_PLAYER_ID + 1);
            Assert.AreEqual(firstValuation, secondValuation);
            ClubFinancesState finances = CreateFinances(firstValuation * 2L - 1L, 5_000_000L, 0L);
            Offer first = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, firstValuation, 1_000L, 2, true);
            Offer second = new Offer(COUNTERPARTY_PLAYER_ID + 1, COUNTERPARTY_CLUB_ID, secondValuation, 1_000L, 2, true);

            Assert.AreEqual(TransferSubmissionOutcome.Accepted, commands.SubmitBid(in first, 110U, ref finances, state));
            long balanceAfterFirst = finances.Balance;

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
        public void SubmitBid_RosterCommitPreviewMismatch_FailsBeforeLocalStateMutation()
        {
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            roster.CommittedPlayerIdOffset = 1;
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
        public void SubmitBid_SellRosterCommitPreviewMismatch_RetainsLocalContractAndFinance()
        {
            FakeRosterPort roster = CreateRosterWithManagedPlayer();
            roster.CommittedPlayerIdOffset = 1;
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
        public void InsertContract_DefaultContract_FailsLoud()
        {
            TransfersState state = CreateOpenState();
            Contract invalid = default(Contract);

            Assert.Throws<ArgumentOutOfRangeException>(() => state.InsertContract(in invalid));
        }

        [Test]
        public void OnPlayerRekeyed_ManagedExternalIsNoOp_IntraManagedMovesContract()
        {
            TransfersState state = CreateOpenState();
            Contract seeded = new Contract(MANAGED_PLAYER_ID, 1_000L, 2);
            state.InsertContract(in seeded);

            state.OnPlayerRekeyed(MANAGED_PLAYER_ID, COUNTERPARTY_PLAYER_ID);
            Assert.IsTrue(state.TryGetContract(MANAGED_PLAYER_ID, out Contract unchanged));
            Assert.AreEqual(MANAGED_PLAYER_ID, unchanged.PlayerId);

            int intraManagedNewId = MANAGED_PLAYER_ID + 1;
            state.OnPlayerRekeyed(MANAGED_PLAYER_ID, intraManagedNewId);
            Assert.IsFalse(state.TryGetContract(MANAGED_PLAYER_ID, out Contract oldContract));
            Assert.IsTrue(state.TryGetContract(intraManagedNewId, out Contract moved));
            Assert.AreEqual(intraManagedNewId, moved.PlayerId);
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
            roster.Add(PlayerRecord.CreateDefault(COUNTERPARTY_PLAYER_ID));
            return roster;
        }

        private static FakeRosterPort CreateRosterWithTwoCounterpartyPlayers()
        {
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            PlayerRecord second = PlayerRecord.CreateDefault(COUNTERPARTY_PLAYER_ID + 1);
            roster.Add(second);
            return roster;
        }

        private static FakeRosterPort CreateRosterWithManagedPlayer()
        {
            FakeRosterPort roster = new FakeRosterPort();
            roster.Add(PlayerRecord.CreateDefault(MANAGED_PLAYER_ID));
            return roster;
        }

        private static long CounterpartyValue(FakeRosterPort roster, int playerId)
        {
            Assert.IsTrue(roster.TryGetPlayer(playerId, out PlayerRecord player));
            int stock = roster.CountPlayersAtPosition(COUNTERPARTY_CLUB_ID, player.Position);
            int ownerClubId = playerId / PlayerDatabaseConstants.CLUB_SQUAD_SIZE;
            if (ownerClubId == COUNTERPARTY_CLUB_ID)
            {
                stock--;
            }

            return PlayerValuation.CounterpartyValue(in player.Attributes, player.Age, stock);
        }

        private sealed class FakeRosterPort : ITransferRosterPort
        {
            private readonly Dictionary<int, PlayerRecord> _players = new Dictionary<int, PlayerRecord>();
            private readonly Dictionary<int, int> _nextLocalIndexByClub = new Dictionary<int, int>();
            private int _previewPlayerId;

            public bool HasDestinationCapacity { get; set; } = true;

            public int CommittedPlayerIdOffset { get; set; }

            public int CommitCount { get; private set; }

            public Action BeforeCommit { get; set; }

            public void Add(PlayerRecord player)
            {
                _players.Add(player.PlayerId, player);
            }

            public bool TryGetPlayer(int playerId, out PlayerRecord player)
            {
                return _players.TryGetValue(playerId, out player);
            }

            public int CountPlayersAtPosition(int clubId, PlayerPosition position)
            {
                int count = 0;
                foreach (KeyValuePair<int, PlayerRecord> entry in _players)
                {
                    if (entry.Key / PlayerDatabaseConstants.CLUB_SQUAD_SIZE == clubId
                        && entry.Value.Position == position)
                    {
                        count++;
                    }
                }

                return count;
            }

            public bool TryPreviewRosterCommit(int fromClubId, int toClubId, int playerId, out int newPlayerId)
            {
                if (!HasDestinationCapacity
                    || !_players.TryGetValue(playerId, out PlayerRecord player)
                    || player.PlayerId / PlayerDatabaseConstants.CLUB_SQUAD_SIZE != fromClubId)
                {
                    newPlayerId = 0;
                    return false;
                }

                if (!_nextLocalIndexByClub.TryGetValue(toClubId, out int localIndex))
                {
                    localIndex = 0;
                }

                if (localIndex >= PlayerDatabaseConstants.CLUB_SQUAD_SIZE)
                {
                    newPlayerId = 0;
                    return false;
                }

                _previewPlayerId = toClubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE + localIndex;
                newPlayerId = _previewPlayerId;
                return true;
            }

            public int RequestRosterCommit(int fromClubId, int toClubId, int playerId)
            {
                BeforeCommit?.Invoke();
                CommitCount++;
                int localIndex = _previewPlayerId - toClubId * PlayerDatabaseConstants.CLUB_SQUAD_SIZE;
                _nextLocalIndexByClub[toClubId] = localIndex + 1;
                return _previewPlayerId + CommittedPlayerIdOffset;
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
#endregion