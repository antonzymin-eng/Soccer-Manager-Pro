// ============================================================================
// File:     src/season-save/tests/SeasonLoopFinanceTests.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Spec:     Club Finances & Economy #40 §3.2/§3.4/§4.1-§4.3/§7.1 T2b,
//           T-FN-LIFE-001, T-FN-ORD-001/003, T-FN-DET-002; Season Loop #30 §3.5;
//           Code Standards #20 §3.9.4
// Purpose:  Locks #40's production bootstrap, Restore-only legacy migration, runtime ledger surface,
//           boundary settlement ordering, across-roll identity, refused-roll atomicity, and preservation
//           of already-composed runtime subsystems at the #30 composition root.
// ============================================================================

using NUnit.Framework;

using TacticalDirector.ClubFinances;
using TacticalDirector.Discipline;
using TacticalDirector.LivingWorld;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.PlayerProgression;

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
        public void GenericLegacyComposition_EmptyFinanceStateIsNotSilentlyInitialized()
        {
            // The generic constructor can still represent the explicit pre-T2/unwired state used by
            // existing low-level tests and callers. It must not manufacture starting cash: the first
            // finance operation fails loud. Only Restore is allowed to migrate a persisted empty T1b block.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);
            var loop = new SeasonLoop(
                world,
                league.CreateSeason(managedClubId: 0),
                RoundResolutionMode.QuickSimAll);

            Assert.That(loop.FinanceEntriesForSave(), Is.Empty);
            Assert.Throws<System.InvalidOperationException>(() => loop.FinanceView(0));
        }

        [Test]
        public void Restore_EmptyLegacyFinanceBlock_UpgradesExactlyOnceToPersistedSeasonClubUniverse()
        {
            // This is the compatibility path the P2 correction exists for: a well-formed v7/T1b save
            // can contain an empty FNCE block because the producer did not yet exist. Restore, and only
            // Restore, converts that persisted representation into live T2b state.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);
            SeasonState season = league.CreateSeason(managedClubId: 0);

            SeasonLoop restored = SeasonLoop.Restore(
                world,
                SeasonStateCodec.Encode(season),
                RoundResolutionMode.QuickSimAll,
                financesOrNull: System.Array.Empty<ClubFinanceEntry>());

            ClubFinanceEntry[] entries = restored.FinanceEntriesForSave();
            Assert.That(entries, Has.Length.EqualTo(ClubCount));
            for (int i = 0; i < entries.Length; i++)
            {
                Assert.That(entries[i].ClubId, Is.EqualTo(i));
                Assert.That(entries[i].Finances.Balance, Is.EqualTo(ClubFinancesConstants.StartingClubBalance));
            }
        }

        [Test]
        public void CreateLoop_PreservesCareerAndDisciplineWhenProgressionIsAbsent()
        {
            // P1 regression, false branch of progressionIsRoster: career must still bind to this league
            // and discipline must survive finance bootstrap instead of being discarded by a bare loop.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);
            PlayerCareerStates career = PlayerCareerStates.ForLeague(
                league, league.ClubIds(), injuryOccurrenceEnabled: false);
            var discipline = new DisciplineState();

            SeasonLoop loop = league.CreateLoop(
                world,
                managedClubId: 0,
                RoundResolutionMode.QuickSimAll,
                careerOrNull: career,
                disciplineOrNull: discipline);

            Assert.That(loop.Career, Is.SameAs(career));
            Assert.That(loop.Progression, Is.Null);
            Assert.That(loop.Discipline, Is.SameAs(discipline));
            Assert.That(loop.FinanceEntriesForSave(), Has.Length.EqualTo(ClubCount));
        }

        [Test]
        public void CreateLoop_PreservesCareerAndProgression_WhenProgressionOwnsTheRosterAuthority()
        {
            // P1 regression, true branch of progressionIsRoster: CreateLoop must pass NO second squad
            // provider, allowing SeasonLoop to project its provider from this exact progression store.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);
            ProgressionEngine progression = SeedProgression(league);
            PlayerCareerStates career = PlayerCareerStates.ForLeague(
                new ProgressionSquads(progression),
                league.ClubIds(),
                injuryOccurrenceEnabled: false);

            SeasonLoop loop = league.CreateLoop(
                world,
                managedClubId: 0,
                RoundResolutionMode.QuickSimAll,
                careerOrNull: career,
                progressionOrNull: progression);

            Assert.That(loop.Career, Is.SameAs(career));
            Assert.That(loop.Progression, Is.SameAs(progression));
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
        public void RollToNextSeason_SettlesFromFinalTableWithConcretePositionEconomics_AndPreservesClubSet()
        {
            // §3.9.4 general-unit-test — allocation rules relaxed in test body.
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);
            SeasonLoop loop = league.CreateLoop(world, managedClubId: 0, RoundResolutionMode.QuickSimAll);

            CompleteSeason(loop, league);

            int championClubId = ClubAtPosition(loop.State, 1);
            int bottomClubId = ClubAtPosition(loop.State, ClubCount);
            loop.RollToNextSeason();

            ClubFinanceEntry[] after = loop.FinanceEntriesForSave();
            ClubFinanceEntry champion = EntryFor(after, championClubId);
            ClubFinanceEntry bottom = EntryFor(after, bottomClubId);

            // Independent endpoint locks: these do NOT call SettleFinances, so reversing/ignoring the
            // table position in SeasonFinanceRuntime cannot make production and expected fail together.
            Assert.That(
                champion.Finances.Balance,
                Is.EqualTo(ClubFinancesConstants.StartingClubBalance + ClubFinancesConstants.PrizeMoneyWinner));
            Assert.That(
                bottom.Finances.Balance,
                Is.EqualTo(ClubFinancesConstants.StartingClubBalance + ClubFinancesConstants.PrizeMoneyLastPlace));

            long championTransfer =
                ClubFinancesConstants.BaseTransferBudget
                + ClubFinancesConstants.PrizeMoneyWinner
                * ClubFinancesConstants.TransferBudgetPrizeSharePermille
                / ClubFinancesConstants.PERMILLE_DENOM;
            long bottomTransfer =
                ClubFinancesConstants.BaseTransferBudget
                + ClubFinancesConstants.PrizeMoneyLastPlace
                * ClubFinancesConstants.TransferBudgetPrizeSharePermille
                / ClubFinancesConstants.PERMILLE_DENOM;
            Assert.That(champion.Finances.TransferBudget, Is.EqualTo(championTransfer));
            Assert.That(bottom.Finances.TransferBudget, Is.EqualTo(bottomTransfer));
            Assert.That(champion.Finances.TransferBudget, Is.GreaterThan(bottom.Finances.TransferBudget));

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

        private static ProgressionEngine SeedProgression(League league)
        {
            var squads = new Squad[league.ClubCount];
            for (int clubId = 0; clubId < squads.Length; clubId++)
            {
                squads[clubId] = league.ResolveByClubId(clubId);
            }

            return ProgressionEngine.SeedFrom(squads, newGameWorldDay: 0u);
        }

        private static int ClubAtPosition(SeasonState state, int position)
        {
            for (int clubId = 0; clubId < ClubCount; clubId++)
            {
                if (state.PositionOf(clubId) == position)
                {
                    return clubId;
                }
            }

            Assert.Fail($"No club finished in position {position}.");
            return -1;
        }

        private static ClubFinanceEntry EntryFor(ClubFinanceEntry[] entries, int clubId)
        {
            for (int i = 0; i < entries.Length; i++)
            {
                if (entries[i].ClubId == clubId)
                {
                    return entries[i];
                }
            }

            Assert.Fail($"No finance entry exists for club {clubId}.");
            return default;
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
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-09-11 | —      | #40 T2b production lifecycle and boundary regression set.   |
// | 1.1     | 2026-09-11 | —      | Alias finance state type to avoid namespace/type ambiguity. |
// | 1.2     | 2026-09-11 | —      | First review locks for empty-state and subsystem handling.   |
// | 1.3     | 2026-09-11 | —      | Claude review: Restore-only migration, career/progression    |
// |         |            |        | preservation coverage, and independent position economics.  |
#endregion
