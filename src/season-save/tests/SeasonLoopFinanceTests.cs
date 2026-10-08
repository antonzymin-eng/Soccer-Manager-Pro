// ============================================================================
// File:     src/season-save/tests/SeasonLoopFinanceTests.cs
// Created:  2026-09-11
// Modified: 2026-10-08
// Author:   —
// Spec:     Club Finances & Economy #40 §3.2/§3.4/§4.1-§4.3/§7.1 T2b,
//           T-FN-LIFE-001, T-FN-ORD-001/003, T-FN-DET-002; Season Loop #30 §3.5;
//           Code Standards #20 §3.9.4
// Purpose:  Locks #40's production bootstrap, Restore-only legacy migration, runtime ledger surface,
//           daily publication/refusal, boundary settlement ordering, across-roll identity, and preservation
//           of already-composed runtime subsystems at the #30 composition root.
//           General unit tests (Code Standards #20 §3.9.4): reflection/allocation rules relaxed.
// ============================================================================

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

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

        /// <summary>T-FN-DAY-001: both gate paths preserve every populated field with zero inputs.</summary>
        [TestCase(false)]
        [TestCase(true)]
        public void DailyAccounting_ZeroInputsPreserveAllClubFields(bool enabled)
        {
            ClubFinanceEntry[] entries = PopulatedFinances();
            ClubFinanceEntry[] published = entries;
            ClubFinanceEntry[] before = (ClubFinanceEntry[])entries.Clone();

            entries = SeasonFinanceRuntime.PrepareDailyRevenue(entries, enabled);

            Assert.That(entries, Is.Not.SameAs(published), "Success must publish a detached result.");
            AssertEntriesEqual(before, published);
            AssertEntriesEqual(before, entries);
            Assert.That(ClubFinancesConstants.DEEP_REVENUE_ENABLED, Is.False);
        }

        /// <summary>T-FN-DAY-007: a late-club refusal publishes nothing; repairing it permits retry.</summary>
        [TestCase(false)]
        [TestCase(true)]
        public void DailyAccounting_LateClubFailurePreservesPublishedArrayAndAllowsRetry(bool enabled)
        {
            ClubFinanceEntry[] expected = PopulatedFinances();
            ClubFinanceEntry[] entries = (ClubFinanceEntry[])expected.Clone();
            int last = entries.Length - 1;
            var corrupt = entries[last].Finances;
            corrupt.SeasonRevenueAccrued = -1L;
            entries[last] = new ClubFinanceEntry(entries[last].ClubId, in corrupt);
            ClubFinanceEntry[] published = entries;
            ClubFinanceEntry[] before = (ClubFinanceEntry[])entries.Clone();

            var error = Assert.Throws<ArgumentOutOfRangeException>(() =>
                entries = SeasonFinanceRuntime.PrepareDailyRevenue(entries, enabled));

            Assert.That(error.ParamName, Is.EqualTo("finances"));
            Assert.That(error.Message, Does.Contain("SeasonRevenueAccrued must be non-negative current-season revenue (F1)."));
            Assert.That(entries, Is.SameAs(published), "Refusal must retain the published array.");
            AssertEntriesEqual(before, published);

            // Repair the injected corruption and retry from the same published state.
            entries[last] = expected[last];
            entries = SeasonFinanceRuntime.PrepareDailyRevenue(entries, enabled);

            Assert.That(entries, Is.Not.SameAs(published));
            AssertEntriesEqual(expected, published);
            AssertEntriesEqual(expected, entries);
            // Zero inputs cannot expose partial monetary writes; T3b2 must add non-zero retry tests.
        }

        /// <summary>T-FN-DAY-002: day advance reaches the primitive for every club, even gate-off.</summary>
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void DayAdvance_ValidatesEveryClubBeforeIncrementingClock(int corruptIndex)
        {
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);
            SeasonLoop loop = league.CreateLoop(world, 0, RoundResolutionMode.QuickSimAll);
            ClubFinanceEntry[] entries = LiveEntries(loop);
            var corrupt = entries[corruptIndex].Finances;
            corrupt.SeasonRevenueAccrued = -1L;
            entries[corruptIndex] = new ClubFinanceEntry(entries[corruptIndex].ClubId, in corrupt);
            ClubFinanceEntry[] before = (ClubFinanceEntry[])entries.Clone();

            var error = Assert.Throws<ArgumentOutOfRangeException>(() => loop.AdvanceDays(1));

            Assert.That(error.ParamName, Is.EqualTo("finances"));
            Assert.That(error.Message, Does.Contain("SeasonRevenueAccrued must be non-negative current-season revenue (F1)."));
            Assert.That(loop.CurrentWorldDay, Is.Zero, "Finance failure must precede the clock increment.");
            Assert.That(LiveEntries(loop), Is.SameAs(entries), "A refused day must not publish a finance array.");
            AssertEntriesEqual(before, entries);
        }

        /// <summary>T-FN-DAY-003: fixture preparation and resolution do not complete the finance day.</summary>
        [Test]
        public void FixtureDay_AccountsOnlyOnFollowingDayAdvance()
        {
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);
            SeasonLoop loop = league.CreateLoop(world, 0, RoundResolutionMode.QuickSimAll);
            loop.AdvanceToNextFixtureDay();
            uint fixtureDay = loop.CurrentWorldDay;
            ClubFinanceEntry[] entries = LiveEntries(loop);
            var corrupt = entries[ClubCount - 1].Finances;
            corrupt.TransferBudget = -1L;
            entries[ClubCount - 1] = new ClubFinanceEntry(ClubCount - 1, in corrupt);

            loop.AdvanceDays(0);
            Assert.That(loop.AdvanceToNextFixtureDay(), Is.Zero);
            loop.AdvanceAndPlayNextRound(league);
            Assert.That(loop.CurrentWorldDay, Is.EqualTo(fixtureDay));
            Assert.Throws<ArgumentOutOfRangeException>(() => loop.AdvanceDays(1));
            Assert.That(loop.CurrentWorldDay, Is.EqualTo(fixtureDay));
        }

        /// <summary>T-FN-DAY-001: real day commands preserve finances and the living-world identity.</summary>
        [Test]
        public void DayAdvance_PreservesPopulatedFinancesAndWorldIdentity()
        {
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);
            var bareWorld = new WorldStore(0, WorldSeed);
            ClubFinanceEntry[] expected = PopulatedFinances();
            var loop = new SeasonLoop(world, league.CreateSeason(0),
                RoundResolutionMode.QuickSimAll, financesOrNull: expected);
            ClubFinanceEntry[] published = LiveEntries(loop);

            loop.AdvanceDays(3);
            for (int i = 0; i < 3; i++)
            {
                bareWorld.AdvanceDay();
            }

            Assert.That(LiveEntries(loop), Is.Not.SameAs(published), "Day advancement must install the staged result.");
            AssertEntriesEqual(expected, published);
            AssertEntriesEqual(expected, loop.FinanceEntriesForSave());
            Assert.That(world.Snapshot(), Is.EqualTo(bareWorld.Snapshot()));
        }

        /// <summary>T-FN-DAY-004: cardinality/order of an identity invocation is a compiled-call-graph lock.</summary>
        [Test]
        public void DailyAccounting_SeasonLoopHasOneClockAdvanceCallerOutsideFixturePreparation()
        {
            // §3.9.4 general-unit-test — reflection/allocation rules relaxed in test body.
            // Identity output alone cannot distinguish zero, one or two calls. Inspect decoded IL,
            // not source substrings, so this lock runs under both the Linux and Unity test runners.
            // Scope: methods/constructors declared directly on SeasonLoop, excluding generated types.
            // T3b2 must supplement this lock with behavioural counts using non-zero daily amounts.
            MethodInfo daily = typeof(SeasonFinanceRuntime).GetMethod("PrepareDailyRevenue",
                BindingFlags.Static | BindingFlags.NonPublic);
            MethodInfo tick = typeof(SeasonLoop).GetMethod("RunWorldTickInFixedOrder",
                BindingFlags.Instance | BindingFlags.NonPublic);
            var callers = new List<MethodBase>();
            var methods = new List<MethodBase>(typeof(SeasonLoop).GetMethods(
                BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic
                | BindingFlags.DeclaredOnly));
            methods.AddRange(typeof(SeasonLoop).GetConstructors(
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
            foreach (MethodBase method in methods)
            {
                foreach (MethodBase called in CalledMethods(method))
                {
                    if (called == daily)
                    {
                        callers.Add(method);
                    }
                }
            }

            Assert.That(callers, Is.EqualTo(new[] { tick }),
                "SeasonLoop's declared methods/constructors must contain exactly one direct finance call site.");
            List<MethodBase> calls = CalledMethods(tick);
            int careerIndex = calls.FindIndex(m => m.Name == "RunCareerDaySteps");
            int financeIndex = calls.IndexOf(daily);
            int clockIndex = calls.FindIndex(m => m.DeclaringType == typeof(WorldStore) && m.Name == "AdvanceDay");
            Assert.That(careerIndex, Is.GreaterThanOrEqualTo(0));
            Assert.That(financeIndex, Is.GreaterThan(careerIndex));
            Assert.That(clockIndex, Is.GreaterThan(financeIndex));
        }

        /// <summary>General-test-only corruption injection; production keeps finance state private.</summary>
        private static ClubFinanceEntry[] LiveEntries(SeasonLoop loop) =>
            (ClubFinanceEntry[])typeof(SeasonLoop).GetField("_finances",
                BindingFlags.Instance | BindingFlags.NonPublic).GetValue(loop);

        private static ClubFinanceEntry[] PopulatedFinances()
        {
            var entries = new ClubFinanceEntry[ClubCount];
            for (int i = 0; i < entries.Length; i++)
            {
                var finances = new TacticalDirector.ClubFinances.ClubFinances
                {
                    Balance = -100L - i,
                    TransferBudget = 200L + i,
                    WageBudget = 300L + i,
                    WageBillAggregate = 400L + i,
                    SeasonRevenueAccrued = 500L + i,
                    FfpBalanceWindow = -600L - i
                };
                entries[i] = new ClubFinanceEntry(i, in finances);
            }
            return entries;
        }

        private static List<MethodBase> CalledMethods(MethodBase method)
        {
            var codes = new Dictionary<short, OpCode>();
            foreach (FieldInfo field in typeof(OpCodes).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (field.FieldType == typeof(OpCode))
                {
                    var code = (OpCode)field.GetValue(null);
                    codes[code.Value] = code;
                }
            }
            var calls = new List<MethodBase>();
            byte[] il = method.GetMethodBody()?.GetILAsByteArray();
            if (il == null)
            {
                return calls;
            }
            for (int offset = 0; offset < il.Length;)
            {
                short value = il[offset++];
                if (value == 0xFE)
                {
                    value = unchecked((short)(0xFE00 | il[offset++]));
                }
                OpCode code = codes[value];
                if (code.OperandType == OperandType.InlineMethod)
                {
                    calls.Add(method.Module.ResolveMethod(BitConverter.ToInt32(il, offset)));
                }
                switch (code.OperandType)
                {
                    case OperandType.InlineNone: break;
                    case OperandType.ShortInlineBrTarget:
                    case OperandType.ShortInlineI:
                    case OperandType.ShortInlineVar: offset += 1; break;
                    case OperandType.InlineVar: offset += 2; break;
                    case OperandType.InlineI8:
                    case OperandType.InlineR: offset += 8; break;
                    case OperandType.InlineSwitch:
                        offset += 4 + 4 * BitConverter.ToInt32(il, offset);
                        break;
                    default: offset += 4; break;
                }
            }
            return calls;
        }

        /// <summary>T-FN-DAY-006: the daily slot continues through the break and a season roll.</summary>
        [Test]
        public void SeasonBreakAndNextSeason_DailyPassPreservesCurrentFinanceValues()
        {
            League league = LeagueBootstrap.Generate(WorldSeed, ClubCount);
            var world = new WorldStore(0, WorldSeed);
            SeasonLoop loop = league.CreateLoop(world, 0, RoundResolutionMode.QuickSimAll);
            CompleteSeason(loop, league);
            ClubFinanceEntry[] beforeBreak = loop.FinanceEntriesForSave();

            loop.AdvanceDays(1);
            AssertEntriesEqual(beforeBreak, loop.FinanceEntriesForSave());
            loop.RollToNextSeason();
            ClubFinanceEntry[] settled = loop.FinanceEntriesForSave();
            loop.AdvanceDays(1);
            AssertEntriesEqual(settled, loop.FinanceEntriesForSave());
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
// | 1.4     | 2026-10-08 | —      | T3b1: identity, per-club live invocation, fixture timing and IL ownership/order locks. |
// | 1.5     | 2026-10-08 | —      | PR #491 review: detached publication, late-club refusal/retry, and explicit IL scan scope. |
#endregion
