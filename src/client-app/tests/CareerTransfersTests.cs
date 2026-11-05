// File:     src/client-app/tests/CareerTransfersTests.cs
// Created:  2026-11-05
// Modified: 2026-11-05
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P8 (D-P8-1..3),
//           docs/design/ux-s2-transfers-finances.md §2–§4, Transfers #31 FR-TX-025, Club Finances #40
//           FR-FN-026, Code Standards #20
// Purpose:  Locks the host-free transfer/finance half: CareerSession.SubmitTransfer passes #31's typed
//           outcome through, refuses a closed window before #31, and never autosaves; the two sources read
//           the owners' state; the copy formats every outcome; the flow's moves are guarded.

using System;
using System.IO;

using NUnit.Framework;

using TacticalDirector.PlayerDatabase;
using TacticalDirector.SeasonSave;
using TacticalDirector.Transfers;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class CareerTransfersTests
    {
        // ── CareerSession.SubmitTransfer ─────────────────────────────────────────────────────────────

        [Test]
        public void GenesisBuy_AtTheClubsPrice_IsAccepted_AndMovesThePlayerBudgetAndRows()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            var holder = new CareerSessionHolder();
            holder.Set(session);
            var market = new TransferMarketSource(holder);
            TransferMarketViewModel before = market.Project();
            Assert.That(before.IsTransfersWired && before.IsWindowOpen, Is.True, "a new career opens in its window");
            Assert.That(before.RemainingBudget, Is.EqualTo(100_000L), "ERR-040-004 genesis ceiling");

            TransferPlayerRow target = CheapestOtherClubPlayer(before);
            long price = ClubPrice(session, target.PlayerId, buy: true);
            Assert.That(price, Is.LessThanOrEqualTo(before.RemainingBudget), "non-vacuity: an affordable target exists");
            int revision = session.Revision;

            TransferSubmissionOutcome outcome = session.SubmitTransfer(
                new Offer(target.PlayerId, target.ClubId, price, before.DefaultWagePerPeriod, before.DefaultContractSeasons, true));

            Assert.That(outcome, Is.EqualTo(TransferSubmissionOutcome.Accepted));
            Assert.That(session.Revision, Is.EqualTo(revision + 1));
            Assert.That(session.IsBusy, Is.False);
            TransferMarketViewModel after = market.Project();
            Assert.That(after.CommittedSpend, Is.EqualTo(price));
            Assert.That(after.RemainingBudget, Is.EqualTo(before.RemainingBudget - price));
            Assert.That(after.ManagedSquadCount, Is.EqualTo(before.ManagedSquadCount + 1));
            TransferPlayerRow moved = RowOf(after, target.PlayerId);
            Assert.That(moved.ClubId, Is.EqualTo(session.ManagedClubId));
            Assert.That(moved.IsManaged && moved.HasContract, Is.True, "the buy inserted his managed contract");
            Assert.That(moved.ContractWagePerPeriod, Is.EqualTo(before.DefaultWagePerPeriod));
        }

        [Test]
        public void TypedOutcomes_PassThroughUnchanged_AndOnlyAcceptedMutates()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            var holder = new CareerSessionHolder();
            holder.Set(session);
            var market = new TransferMarketSource(holder);
            TransferMarketViewModel before = market.Project();
            TransferPlayerRow target = CheapestOtherClubPlayer(before);
            long price = ClubPrice(session, target.PlayerId, buy: true);

            Assert.That(session.SubmitTransfer(new Offer(target.PlayerId, target.ClubId, 1L, 1_000L, 1, true)),
                Is.EqualTo(TransferSubmissionOutcome.Rejected));
            Assert.That(session.SubmitTransfer(new Offer(target.PlayerId, target.ClubId, price - 1L, 1_000L, 1, true)),
                Is.EqualTo(TransferSubmissionOutcome.CounterOffered), "one below the price is inside the counter band");

            TransferPlayerRow expensive = MostExpensiveOtherClubPlayer(before);
            long dearPrice = ClubPrice(session, expensive.PlayerId, buy: true);
            Assert.That(dearPrice, Is.GreaterThan(before.RemainingBudget), "non-vacuity: the league's dearest player costs more than the base ceiling");
            Assert.That(session.SubmitTransfer(new Offer(expensive.PlayerId, expensive.ClubId, dearPrice, 1_000L, 1, true)),
                Is.EqualTo(TransferSubmissionOutcome.InsufficientBudget));

            TransferMarketViewModel after = market.Project();
            Assert.That(after.CommittedSpend, Is.Zero, "no refused outcome commits spend");
            Assert.That(after.ManagedSquadCount, Is.EqualTo(before.ManagedSquadCount));
            Assert.That(RowOf(after, target.PlayerId).ClubId, Is.EqualTo(target.ClubId));
        }

        [Test]
        public void Sale_IsAccepted_AndCreditsTheBalanceWithoutTouchingTheCeiling()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            var holder = new CareerSessionHolder();
            holder.Set(session);
            var market = new TransferMarketSource(holder);
            var finances = new ClubFinancesSource(holder);
            TransferMarketViewModel before = market.Project();
            ClubFinancesViewModel cashBefore = finances.Project();
            TransferPlayerRow own = FirstManagedPlayer(before);
            int buyer = FirstOtherClub(before);
            long price = ClubPrice(session, own.PlayerId, buy: false, buyer: buyer);

            Assert.That(session.SubmitTransfer(new Offer(own.PlayerId, buyer, price, 0L, 1, false)),
                Is.EqualTo(TransferSubmissionOutcome.Accepted));

            ClubFinancesViewModel cashAfter = finances.Project();
            Assert.That(cashAfter.Balance, Is.EqualTo(cashBefore.Balance + price));
            Assert.That(cashAfter.TransferBudget, Is.EqualTo(cashBefore.TransferBudget), "only settlement sets the ceiling (FR-FN-003)");
            Assert.That(cashAfter.ContractCount, Is.EqualTo(cashBefore.ContractCount - (own.HasContract ? 1 : 0)));
            Assert.That(market.Project().ManagedSquadCount, Is.EqualTo(before.ManagedSquadCount - 1));
        }

        [Test]
        public void AcceptedTransfer_IsNotAutosaved_D_P8_2()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            var holder = new CareerSessionHolder();
            holder.Set(session);
            TransferMarketViewModel market = new TransferMarketSource(holder).Project();
            string autosave = temp.Slots.SavePath(CareerSaveSlot.Autosave);
            byte[] savedBefore = File.ReadAllBytes(autosave);
            TransferPlayerRow target = CheapestOtherClubPlayer(market);

            Assert.That(session.SubmitTransfer(new Offer(target.PlayerId, target.ClubId,
                ClubPrice(session, target.PlayerId, buy: true), 1_000L, 1, true)), Is.EqualTo(TransferSubmissionOutcome.Accepted));

            CollectionAssert.AreEqual(savedBefore, File.ReadAllBytes(autosave), "a transfer never writes the autosave slot");
        }

        [Test]
        public void ClosedWindow_IsRefusedBeforeTransfers_WithNothingChanged()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            var holder = new CareerSessionHolder();
            holder.Set(session);
            var market = new TransferMarketSource(holder);
            TransferPlayerRow target = CheapestOtherClubPlayer(market.Project());
            for (int guard = 0; market.Project().IsWindowOpen; guard++)
            {
                Assert.That(guard, Is.LessThan(500), "the summer window must close");
                session.Progress();
            }

            TransferMarketViewModel closed = market.Project();
            TransferPlayerRow stillThere = RowOf(closed, target.PlayerId);
            var refused = Assert.Throws<InvalidOperationException>(() => session.SubmitTransfer(
                new Offer(target.PlayerId, stillThere.ClubId, 1L, 1_000L, 1, true)));
            StringAssert.StartsWith("The transfer window is closed", refused.Message,
                "the session refuses before #31's own F4 throw becomes the control flow");
            Assert.That(session.IsBusy, Is.False);
            Assert.That(market.Project().CommittedSpend, Is.EqualTo(closed.CommittedSpend));
            Assert.That(RowOf(market.Project(), target.PlayerId).ClubId, Is.EqualTo(stillThere.ClubId));
        }

        // ── sources ──────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void MarketRows_CoverTheLeague_WithOwnersValuesAndCachedBetweenOperations()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            var holder = new CareerSessionHolder();
            Assert.That(new TransferMarketSource(holder).Project().HasCareer, Is.False, "no career → default");
            holder.Set(session);
            var source = new TransferMarketSource(holder);
            TransferMarketViewModel market = source.Project();

            int expected = 0;
            foreach (int club in session.League.ClubIds())
            {
                expected += session.Loop.Progression.SquadFor(club).Count;
            }

            Assert.That(market.Players.Count, Is.EqualTo(expected), "one row per league player");
            Assert.That(market.Clubs.Count, Is.EqualTo(session.League.ClubIds().Length));
            Assert.That(market.SquadCapacity, Is.EqualTo(PlayerDatabaseConstants.CLUB_SQUAD_SIZE));
            foreach (TransferPlayerRow row in market.Players)
            {
                Assert.That(session.Loop.Progression.TryFindPlayer(row.PlayerId, out PlayerRecord record, out int owner), Is.True);
                Assert.That(row.ClubId, Is.EqualTo(owner));
                Assert.That(row.EstimatedValue, Is.EqualTo(PlayerValuation.ValuePlayer(in record.Attributes, record.Age)), "D-P8-1");
                Assert.That(row.IsManaged, Is.EqualTo(owner == session.ManagedClubId));
                if (!row.IsManaged)
                {
                    Assert.That(row.HasContract, Is.False, "only managed contracts exist at minimal #31");
                }
            }

            Assert.That(source.Project().Players, Is.SameAs(market.Players), "unchanged revision → cached");
        }

        [Test]
        public void Finances_ReadTheManagedClubsLedgerAndContracts()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            var holder = new CareerSessionHolder();
            holder.Set(session);
            ClubFinancesViewModel view = new ClubFinancesSource(holder).Project();
            var ledger = session.Loop.FinanceView(session.ManagedClubId);
            Contract[] contracts = session.Loop.TransfersSnapshot().ContractsSnapshot();
            long wages = 0;
            foreach (Contract c in contracts)
            {
                wages += c.WagePerPeriod;
            }

            Assert.That(view.HasCareer, Is.True);
            Assert.That(view.Balance, Is.EqualTo(ledger.Balance));
            Assert.That(view.TransferBudget, Is.EqualTo(ledger.TransferBudget));
            Assert.That(view.WageBudget, Is.EqualTo(ledger.WageBudget));
            Assert.That(view.WageBillAggregate, Is.EqualTo(ledger.WageBillAggregate));
            Assert.That(view.ContractCount, Is.EqualTo(contracts.Length));
            Assert.That(view.ContractCount, Is.GreaterThan(0), "non-vacuity: genesis seeds managed contracts (FR-TX-028)");
            Assert.That(view.ContractedWages, Is.EqualTo(wages));
            Assert.That(view.RemainingTransferBudget, Is.EqualTo(ledger.TransferBudget));
        }

        // ── copy ─────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void EveryOutcome_HasItsOwnMessage()
        {
            var text = new CareerScreenText();
            var market = new TransferMarketViewModel(true, 0, 3, true, 0, 44, 100_000, 40_000, 25, 25, 1_000, 3,
                new System.Collections.ObjectModel.ReadOnlyCollection<TransferPlayerRow>(new TransferPlayerRow[0]),
                new System.Collections.ObjectModel.ReadOnlyCollection<TransferClubRow>(new TransferClubRow[0]));
            var seen = new System.Collections.Generic.HashSet<string>();
            foreach (TransferSubmissionOutcome outcome in Enum.GetValues(typeof(TransferSubmissionOutcome)))
            {
                Assert.That(seen.Add(text.Outcome(outcome, true, in market)), Is.True, outcome + " repeats another message");
            }

            StringAssert.Contains("60000", text.Outcome(TransferSubmissionOutcome.InsufficientBudget, true, in market));
            StringAssert.Contains("25", text.Outcome(TransferSubmissionOutcome.SquadFull, true, in market));
            Assert.That(text.Outcome(TransferSubmissionOutcome.SquadDepleted, false, in market),
                Is.Not.EqualTo(text.Outcome(TransferSubmissionOutcome.SquadDepleted, true, in market)));
            StringAssert.Contains("44", text.Window(in market));
            Assert.That(market.HasSquadRoom, Is.False);
        }

        [Test]
        public void Finances_ShowDebtAsOverdrawn_AndTheWageBillAsNotTracked()
        {
            var text = new CareerScreenText();
            string debt = text.Finances(new ClubFinancesViewModel("Test FC", -2_500, 100_000, 0, 50_000, 0, 3_000, 3));
            StringAssert.Contains("overdrawn by 2500", debt);
            StringAssert.Contains("not tracked yet", debt);
            StringAssert.Contains("3000", debt);
            Assert.That(text.Finances(default), Is.Empty);
        }

        // ── flow ─────────────────────────────────────────────────────────────────────────────────────

        [Test]
        public void TransferMoves_AreRefusedWithoutTheScreens_AndOffCareerHome()
        {
            var seven = new ClientScreenFlow(
                Reg(ClientScreens.MainMenu), Reg(ClientScreens.TacticsSetup), Reg(ClientScreens.MatchView),
                Reg(ClientScreens.PostMatchReport), Reg(ClientScreens.CareerHome), Reg(ClientScreens.NewCareer),
                Reg(ClientScreens.LoadCareer));
            Assert.That(seven.HasTransferScreens, Is.False);
            seven.ContinueCareer();
            Assert.Throws<InvalidOperationException>(seven.OpenTransfers);
            Assert.Throws<InvalidOperationException>(seven.OpenClubFinances);

            var nine = new ClientScreenFlow(
                Reg(ClientScreens.MainMenu), Reg(ClientScreens.TacticsSetup), Reg(ClientScreens.MatchView),
                Reg(ClientScreens.PostMatchReport), Reg(ClientScreens.CareerHome), Reg(ClientScreens.NewCareer),
                Reg(ClientScreens.LoadCareer), Reg(ClientScreens.Transfers), Reg(ClientScreens.ClubFinances));
            Assert.That(nine.HasTransferScreens && nine.HasCareerScreens, Is.True);
            Assert.Throws<InvalidOperationException>(nine.OpenTransfers, "Main Menu is not Career Home");
            nine.ContinueCareer();
            nine.OpenTransfers();
            Assert.Throws<InvalidOperationException>(nine.QuitToMainMenu, "quit only from Career Home");
            Assert.Throws<InvalidOperationException>(nine.OpenClubFinances, "no Transfers → Finances edge");
            nine.CloseTransfers();
            Assert.That(nine.Current, Is.EqualTo(ClientScreens.CareerHome));

            Assert.Throws<ArgumentException>(() => new ClientScreenFlow(
                Reg(ClientScreens.MainMenu), Reg(ClientScreens.TacticsSetup), Reg(ClientScreens.MatchView),
                Reg(ClientScreens.PostMatchReport), Reg(ClientScreens.CareerHome), Reg(ClientScreens.NewCareer),
                Reg(ClientScreens.LoadCareer), Reg(ClientScreens.ClubFinances), Reg(ClientScreens.Transfers)),
                "transposed registrations are refused");
        }

        // ── helpers ──────────────────────────────────────────────────────────────────────────────────

        private static ScreenRegistration Reg(ScreenId id) => new ScreenRegistration(id, null, null);

        // The club's acceptance price, computed exactly as TransferCommands.SubmitBid computes it. The UI never
        // shows it (D-P8-1); the test needs it to build deterministic Accepted / CounterOffered offers.
        private static long ClubPrice(CareerSession session, int playerId, bool buy, int buyer = -1)
        {
            Assert.That(session.Loop.Progression.TryFindPlayer(playerId, out PlayerRecord record, out int owner), Is.True);
            int counterparty = buy ? owner : buyer;
            Squad squad = session.Loop.Progression.SquadFor(counterparty);
            int stock = 0;
            for (int i = 0; i < squad.Count; i++)
            {
                if (squad.GetPlayer(i).Position == record.Position)
                {
                    stock++;
                }
            }

            if (buy)
            {
                stock--;
            }

            return PlayerValuation.CounterpartyValue(in record.Attributes, record.Age, stock);
        }

        private static TransferPlayerRow CheapestOtherClubPlayer(in TransferMarketViewModel market)
        {
            TransferPlayerRow best = default;
            long value = long.MaxValue;
            foreach (TransferPlayerRow row in market.Players)
            {
                if (!row.IsManaged && row.EstimatedValue < value)
                {
                    best = row;
                    value = row.EstimatedValue;
                }
            }

            Assert.That(value, Is.LessThan(long.MaxValue));
            return best;
        }

        private static TransferPlayerRow MostExpensiveOtherClubPlayer(in TransferMarketViewModel market)
        {
            TransferPlayerRow best = default;
            long value = -1;
            foreach (TransferPlayerRow row in market.Players)
            {
                if (!row.IsManaged && row.EstimatedValue > value)
                {
                    best = row;
                    value = row.EstimatedValue;
                }
            }

            return best;
        }

        private static TransferPlayerRow FirstManagedPlayer(in TransferMarketViewModel market)
        {
            foreach (TransferPlayerRow row in market.Players)
            {
                if (row.IsManaged)
                {
                    return row;
                }
            }

            throw new AssertionException("no managed player");
        }

        private static int FirstOtherClub(in TransferMarketViewModel market)
        {
            foreach (TransferClubRow club in market.Clubs)
            {
                if (!club.IsManaged)
                {
                    return club.ClubId;
                }
            }

            throw new AssertionException("no other club");
        }

        private static TransferPlayerRow RowOf(in TransferMarketViewModel market, int playerId)
        {
            foreach (TransferPlayerRow row in market.Players)
            {
                if (row.PlayerId == playerId)
                {
                    return row;
                }
            }

            throw new AssertionException("player " + playerId + " is not listed");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-05 | —      | Initial creation (§5-P8): SubmitTransfer outcomes/refusals/no-autosave, sources, copy and flow guards. |
#endregion
