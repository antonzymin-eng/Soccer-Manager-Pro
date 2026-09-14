// ============================================================================
// File:     src/transfers/tests/TransfersT0Tests.cs
// Created:  2026-09-12
// Modified: 2026-09-14
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
        public void ValuePlayerPermille_IsDeterministic_AndWeakFootIsExcluded()
        {
            PlayerAttributes first = PlayerAttributes.CreateDefault();
            PlayerAttributes second = first;
            second.WeakFootRating = PlayerDatabaseConstants.WEAK_FOOT_MAX;

            long a = PlayerValuation.ValuePlayerPermille(in first, 25);
            long b = PlayerValuation.ValuePlayerPermille(in first, 25);
            long weakFootChanged = PlayerValuation.ValuePlayerPermille(in second, 25);

            Assert.AreEqual(a, b);
            Assert.AreEqual(a, weakFootChanged);
            Assert.Greater(a, 0L);
        }

        [Test]
        public void AgeCurvePermille_HasNeutralPeak_YoungDiscount_AndOlderDecline()
        {
            int young = PlayerValuation.AgeCurvePermille(TransfersConstants.PeakAgeMin - 1);
            int peak = PlayerValuation.AgeCurvePermille(TransfersConstants.PeakAgeMin);
            int old = PlayerValuation.AgeCurvePermille(TransfersConstants.PeakAgeMax + 1);

            Assert.AreEqual(TransfersConstants.PERMILLE_DENOM, peak);
            Assert.LessOrEqual(young, peak);
            Assert.LessOrEqual(old, peak);
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

            NegotiationOutcome outcome = commands.SubmitBid(in offer, 110U, ref finances, state);

            Assert.AreEqual(NegotiationOutcome.Accepted, outcome);
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

            NegotiationOutcome outcome = commands.SubmitBid(in offer, 110U, ref finances, state);

            Assert.AreEqual(NegotiationOutcome.CounterOffered, outcome);
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

            NegotiationOutcome outcome = commands.SubmitBid(in offer, 110U, ref finances, state);

            Assert.AreEqual(NegotiationOutcome.Rejected, outcome);
            Assert.AreEqual(5_000_000L, finances.Balance);
            Assert.AreEqual(0L, state.CommittedSpendThisWindow);
            Assert.AreEqual(0, state.ContractCount);
            Assert.AreEqual(0, roster.CommitCount);
        }

        [Test]
        public void SubmitBid_SecondBuyOverStaticCeiling_FailsBeforeAnySecondMutation()
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

            commands.SubmitBid(in first, 110U, ref finances, state);
            long balanceAfterFirst = finances.Balance;

            Assert.Throws<InvalidOperationException>(() => commands.SubmitBid(in second, 110U, ref finances, state));
            Assert.AreEqual(balanceAfterFirst, finances.Balance);
            Assert.AreEqual(firstValuation, state.CommittedSpendThisWindow);
            Assert.AreEqual(1, roster.CommitCount);
            Assert.AreEqual(1, state.ContractCount);
        }

        [Test]
        public void SubmitBid_FullDestination_FailsWithoutFinanceOrContractMutation()
        {
            FakeRosterPort roster = CreateRosterWithCounterpartyPlayer();
            roster.HasDestinationCapacity = false;
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 0L);
            long valuation = CounterpartyValue(roster, COUNTERPARTY_PLAYER_ID);
            Offer offer = new Offer(COUNTERPARTY_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation, 1_000L, 2, true);

            Assert.Throws<InvalidOperationException>(() => commands.SubmitBid(in offer, 110U, ref finances, state));
            Assert.AreEqual(5_000_000L, finances.Balance);
            Assert.AreEqual(0L, state.CommittedSpendThisWindow);
            Assert.AreEqual(0, state.ContractCount);
            Assert.AreEqual(0, roster.CommitCount);
        }

        [Test]
        public void SubmitBid_Sell_RemovesContractBeforeRekey_AndCreditsFee()
        {
            FakeRosterPort roster = CreateRosterWithManagedPlayer();
            TransferCommands commands = new TransferCommands(roster);
            TransfersState state = CreateOpenState();
            Contract seeded = new Contract(MANAGED_PLAYER_ID, 2_000L, 2);
            state.InsertContract(in seeded);
            ClubFinancesState finances = CreateFinances(1_000_000L, 5_000_000L, 20_000L);
            long valuation = CounterpartyValue(roster, MANAGED_PLAYER_ID);
            Offer offer = new Offer(MANAGED_PLAYER_ID, COUNTERPARTY_CLUB_ID, valuation, 1_000L, 2, false);
            bool absentBeforeCommit = false;
            roster.BeforeCommit = () => absentBeforeCommit = !state.TryGetContract(MANAGED_PLAYER_ID, out Contract ignored);

            NegotiationOutcome outcome = commands.SubmitBid(in offer, 110U, ref finances, state);

            Assert.AreEqual(NegotiationOutcome.Accepted, outcome);
            Assert.IsTrue(absentBeforeCommit);
            Assert.AreEqual(5_000_000L + valuation, finances.Balance);
            Assert.AreEqual(20_000L, finances.WageBillAggregate);
            Assert.AreEqual(0L, state.CommittedSpendThisWindow);
            Assert.AreEqual(0, state.ContractCount);
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
            return PlayerValuation.CounterpartyValuePermille(in player.Attributes, player.Age, stock);
        }

        private sealed class FakeRosterPort : ITransferRosterPort
        {
            private readonly Dictionary<int, PlayerRecord> _players = new Dictionary<int, PlayerRecord>();
            private readonly Dictionary<int, int> _nextLocalIndexByClub = new Dictionary<int, int>();
            private int _previewPlayerId;

            public bool HasDestinationCapacity { get; set; } = true;

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
                return _previewPlayerId;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Change |
// | --------|------------|--------|---------------------------------------------- |
// | 1.0     | 2026-09-12 | —      | Initial #31 T0 valuation/offer/bid/re-key/window/fail-loud coverage. |
// | 1.1     | 2026-09-14 | —      | Lock finance alias, Codex term validation, counter-offer band, and positional need. |
#endregion
