// ============================================================================
// File:     src/season-save/tests/SeasonLoopFinanceTests.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Spec:     Club Finances & Economy #40 §3.2/§3.4/§4.1-§4.3/§7.1 T2b,
//           T-FN-LIFE-001, T-FN-ORD-001/003, T-FN-DET-002; Season Loop #30 §3.5;
//           Code Standards #20 §3.9.4
// Purpose:  Locks #40's production bootstrap, compatibility activation, runtime ledger surface,
//           boundary settlement ordering, across-roll identity, refused-roll atomicity, and preservation
//           of already-composed runtime subsystems at the #30 composition root.
// ============================================================================

using NUnit.Framework;

using TacticalDirector.ClubFinances;
using TacticalDirector.Discipline;
using TacticalDirector.LivingWorld;

using ClubFinanceState = TacticalDirector.ClubFinances.ClubFinances;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    public sealed class SeasonLoopFinanceTests
    {
        private const ulong WorldSeed = 0x40F1AACEUL;
        private const int ClubCount = 4;

        [Test]
        public void CreateLoop_BootstrapsOneCanonicalInitialFinanceEntryPerLeagueClub()
        {
            // §3.9.4 general-unit-test — allocation rules relaxed in test body.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);

            SeasonLoop loop = league.CreateLoop(world, managedClubId: 0, RoundResolutionMode.QuickSimAll);
            ClubFinanceEntry[] entries = loop.FinanceEntriesForSave();

            Assert.That(entries, Has.Length.EqualTo(ClubCount));
            for (int i = 0; i < entries.Length; i++)
            {
                Assert.That(entries[i].ClubId, Is.EqualTo(i));
                Assert.That(entries[i].Finances.Balance, Is.EqualTo(ClubFinancesConstants.StartingClubBalance));
                Assert.That(entries[i].Finances.TransferBudget, Is.Zero);
                Assert.That(entries[i].Finances.WageBudget, Is.Zero);
                Assert.That(entries[i].Finances.WageBillAggregate, Is.Zero);
                Assert.That(entries[i].Finances.SeasonRevenueAccrued, Is.Zero);
                Assert.That(entries[i].Finances.FfpBalanceWindow, Is.Zero);
            }
        }

        [Test]
        public void GenericPreT2Composition_UpgradesEmptyFinanceInputInsteadOfSilentlySkippingT2b()
        {
            // T1b allowed null/empty while the producer did not exist. T2b must make that a
            // compatibility INPUT only: once the loop exists, its live finance set is complete.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);
            var loop = new SeasonLoop(
                world,
                league.CreateSeason(managedClubId: 0),
                RoundResolutionMode.QuickSimAll);

            ClubFinanceEntry[] entries = loop.FinanceEntriesForSave();
            Assert.That(entries, Has.Length.EqualTo(ClubCount));
            for (int i = 0; i < entries.Length; i++)
            {
                Assert.That(entries[i].ClubId, Is.EqualTo(i));
                Assert.That(entries[i].Finances.Balance, Is.EqualTo(ClubFinancesConstants.StartingClubBalance));
            }
        }

        [Test]
        public void CreateLoop_PreservesAlreadyComposedDisciplineStateWhileAddingFinances()
        {
            // Regression for Codex P1: finance bootstrap is additive composition, not a fresh bare loop
            // that quietly drops already-live subsystems.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);
            var discipline = new DisciplineState();

            SeasonLoop loop = league.CreateLoop(
                world,
                managedClubId: 0,
                RoundResolutionMode.QuickSimAll,
                disciplineOrNull: discipline);

            Assert.That(loop.Discipline, Is.SameAs(discipline));
            Assert.That(loop.FinanceEntriesForSave(), Has.Length.EqualTo(ClubCount));
        }

        [Test]
        public void RuntimeLedgerSurface_RoutesTransactionAndBudgetQueryThroughOwnedClubEntry()
        {
            // §3.9.4 general-unit-test — allocation rules relaxed in test body.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);
            SeasonLoop loop = league.CreateLoop(world, managedClubId: 0, RoundResolutionMode.QuickSimAll);
            var debit = new FinanceTransaction(
                FinanceTransactionKind.Debit,
                FinanceLineItem.TransferFee,
                125_000L);

            loop.ApplyTransaction(0, in debit);
            FinancesViewModel view = loop.FinanceView(0);

            Assert.That(
                view.Balance,
                Is.EqualTo(ClubFinancesConstants.StartingClubBalance - 125_000L));
            Assert.That(loop.AvailableTransferBudget(0), Is.Zero,
                "between-boundary ledger activity must not rewrite the settlement-owned budget ceiling");
            Assert.That(loop.FinanceView(1).Balance, Is.EqualTo(ClubFinancesConstants.StartingClubBalance),
                "a keyed transaction must not mutate another club's finance state");
        }

        [Test]
        public void RollToNextSeason_SettlesEveryClubFromFinalTable_AndPreservesClubSetAcrossRolls()
        {
            // §3.9.4 general-unit-test — allocation rules relaxed in test body.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);
            SeasonLoop loop = league.CreateLoop(world, managedClubId: 0, RoundResolutionMode.QuickSimAll);

            CompleteSeason(loop, league);

            ClubFinanceEntry[] before = loop.FinanceEntriesForSave();
            var expected = new ClubFinanceEntry[before.Length];
            BoardModifier board = BoardModifier.Identity;
            for (int i = 0; i < before.Length; i++)
            {
                ClubFinanceState prior = before[i].Finances;
                ClubFinanceState next = FinanceStep.SettleFinances(
                    in prior,
                    loop.State.PositionOf(before[i].ClubId),
                    ClubCount,
                    in board);
                expected[i] = new ClubFinanceEntry(before[i].ClubId, in next);
            }

            loop.RollToNextSeason();
            AssertEntriesEqual(expected, loop.FinanceEntriesForSave());

            // T-FN-LIFE-001 across-roll half: the same stable ClubIds survive another full boundary.
            CompleteSeason(loop, league);
            loop.RollToNextSeason();
            ClubFinanceEntry[] afterSecondRoll = loop.FinanceEntriesForSave();
            Assert.That(afterSecondRoll, Has.Length.EqualTo(ClubCount));
            for (int i = 0; i < afterSecondRoll.Length; i++)
            {
                Assert.That(afterSecondRoll[i].ClubId, Is.EqualTo(i));
            }
        }

        [Test]
        public void RefusedRoll_LeavesFinanceEntriesUnchanged()
        {
            // §3.9.4 general-unit-test — allocation rules relaxed in test body.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);
            SeasonLoop loop = league.CreateLoop(world, managedClubId: 0, RoundResolutionMode.QuickSimAll);
            CompleteSeason(loop, league);
            ClubFinanceEntry[] before = loop.FinanceEntriesForSave();

            uint nextOpening = loop.State.Calendar
                .ShiftedToNextSeason(SeasonLoopConstants.SeasonBreakDays)
                .NextFixtureDay();
            while (world.CurrentWorldTick <= nextOpening)
            {
                world.AdvanceDay();
            }

            Assert.Throws<System.InvalidOperationException>(() => loop.RollToNextSeason());
            AssertEntriesEqual(before, loop.FinanceEntriesForSave());
        }

        private static void CompleteSeason(SeasonLoop loop, League league)
        {
            while (!loop.IsSeasonComplete)
            {
                loop.AdvanceToNextFixtureDay();
                loop.AdvanceAndPlayNextRound(league);
            }
        }

        private static void AssertEntriesEqual(ClubFinanceEntry[] expected, ClubFinanceEntry[] actual)
        {
            Assert.That(actual, Has.Length.EqualTo(expected.Length));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.That(actual[i].ClubId, Is.EqualTo(expected[i].ClubId));
                Assert.That(actual[i].Finances.Balance, Is.EqualTo(expected[i].Finances.Balance));
                Assert.That(actual[i].Finances.TransferBudget, Is.EqualTo(expected[i].Finances.TransferBudget));
                Assert.That(actual[i].Finances.WageBudget, Is.EqualTo(expected[i].Finances.WageBudget));
                Assert.That(actual[i].Finances.WageBillAggregate, Is.EqualTo(expected[i].Finances.WageBillAggregate));
                Assert.That(actual[i].Finances.SeasonRevenueAccrued, Is.EqualTo(expected[i].Finances.SeasonRevenueAccrued));
                Assert.That(actual[i].Finances.FfpBalanceWindow, Is.EqualTo(expected[i].Finances.FfpBalanceWindow));
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                   |
// | 1.0     | 2026-09-11 | —      | #40 T2b production lifecycle and boundary regression set. |
// | 1.1     | 2026-09-11 | —      | Alias finance state type to avoid namespace/type ambiguity. |
// | 1.2     | 2026-09-11 | —      | Review locks: legacy empty input upgrades at composition;   |
// |         |            |        | CreateLoop preserves already-live subsystem state.          |
#endregion
