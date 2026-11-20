// File:     src/client-app/tests/CareerScreensTests.cs
// Created:  2026-10-18
// Modified: 2026-11-20
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7, UI / Client Framework #38 §3.1
//           (FR-UI-006) and Appendix C (season dispatcher, F3), Code Standards #20
// Purpose:  Locks the career screens' sources and the season dispatcher: Career Home answers its
//           questions from #30's own state and caches between operations; the dispatcher routes the
//           three season kinds and refuses everything else; New Career previews the league it starts;
//           Load Career keeps a failure visible, leaves the active career untouched, and stops a
//           corrupt newest slot from blocking Continue.

using System;
using System.IO;
using System.Linq;

using NUnit.Framework;

using TacticalDirector.LivingWorld;
using TacticalDirector.MatchEngine;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.SeasonSave;
using TacticalDirector.TacticalInstructions;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class CareerScreensTests
    {
        [Test]
        public void CareerHome_WithNoCareer_ProjectsTheDefault()
        {
            var source = new CareerHomeSource(new CareerSessionHolder());
            CareerHomeViewModel vm = source.Project();

            Assert.IsFalse(vm.HasCareer);
            Assert.AreEqual(CareerProgressAction.None, vm.NextAction);
        }

        /// <summary>Verifies the club's fixtures against the stored schedule and its round dates.</summary>
        [Test]
        public void CareerHome_ProjectsOnlyManagedFixturesWithTheirCalendarDays()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            var holder = new CareerSessionHolder();
            holder.Set(session);
            CareerHomeViewModel vm = new CareerHomeSource(holder).Project();

            SeasonState state = session.Loop.State;
            Assert.AreEqual(session.Loop.CurrentWorldDay, vm.WorldDay);
            Assert.IsTrue(vm.Calendar.FieldsEqual(state.Calendar));

            int found = 0;
            foreach (Fixture fixture in state.Fixtures.OrderBy(fixture => fixture.RoundIndex))
            {
                if (!fixture.Involves(session.ManagedClubId))
                {
                    continue;
                }

                CareerFixtureView row = vm.Fixtures[found];
                bool isHome = fixture.HomeClubId == session.ManagedClubId;
                int opponent = isHome ? fixture.AwayClubId : fixture.HomeClubId;
                Assert.AreEqual(fixture.RoundIndex, row.RoundIndex);
                Assert.AreEqual(state.Calendar.DayOfRound(fixture.RoundIndex), row.WorldDay);
                Assert.AreEqual(opponent, row.OpponentId);
                Assert.AreEqual(session.ClubName(opponent), row.OpponentName);
                Assert.AreEqual(isHome, row.IsHome);
                Assert.IsFalse(row.IsPlayed);
                if (found > 0)
                {
                    Assert.Greater(row.WorldDay, vm.Fixtures[found - 1].WorldDay);
                }

                found++;
            }

            Assert.Greater(found, 0);
            Assert.AreEqual(found, vm.Fixtures.Count);
        }

        /// <summary>Verifies that the calendar can reach the last fixture and stops at the season bounds.</summary>
        [Test]
        public void CareerCalendar_PagesThroughTheCurrentSeasonOnWorldDays()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            holder.Set(CareerTestSupport.StartQuick(temp.Slots));
            CareerHomeViewModel home = new CareerHomeSource(holder).Project();
            CareerCalendarWeek first = CareerCalendarWeek.At(in home, home.WorldDay);
            Assert.AreEqual(first.FirstDay, first.PreviousWeek);
            Assert.AreEqual(0U, first.FirstDay % CareerCalendarWeek.DAYS_PER_WEEK);
            Assert.LessOrEqual(first.FirstDay, home.WorldDay);
            Assert.GreaterOrEqual(first.LastDay, home.WorldDay);

            CareerCalendarWeek second = CareerCalendarWeek.At(in home, first.NextWeek);
            Assert.AreEqual(first.LastDay + 1, second.FirstDay);
            Assert.AreEqual(first.FirstDay, second.PreviousWeek);

            uint lastFixture = home.Fixtures[home.Fixtures.Count - 1].WorldDay;
            CareerCalendarWeek last = CareerCalendarWeek.At(in home, uint.MaxValue);
            Assert.LessOrEqual(last.FirstDay, lastFixture);
            Assert.GreaterOrEqual(last.LastDay, lastFixture);
            Assert.AreEqual(last.FirstDay, last.NextWeek);
        }

        /// <summary>Verifies that a calendar week cannot be requested before a career is published.</summary>
        [Test]
        public void CareerCalendar_RequiresAnActiveCareer()
        {
            Assert.Throws<ArgumentException>(() => CareerCalendarWeek.At(default, 0));
        }

        /// <summary>Fixture pages cover every fixture exactly once, in order, and stop at both ends.</summary>
        [Test]
        public void CareerFixturePages_CoverTheSeasonInOrderAndStopAtBothEnds()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            holder.Set(CareerTestSupport.StartQuick(temp.Slots));
            CareerHomeViewModel home = new CareerHomeSource(holder).Project();
            int total = home.Fixtures.Count;
            Assert.Greater(total, CareerFixturePage.FIXTURES_PER_PAGE, "The season must span several pages.");

            CareerFixturePage page = CareerFixturePage.Current(in home);
            Assert.AreEqual(0, page.First, "A new career's next fixture is on the first page.");
            Assert.IsFalse(page.HasPrevious);
            Assert.AreEqual(page.First, page.PreviousFirst);
            int covered = 0;
            while (true)
            {
                Assert.AreEqual(covered, page.First);
                Assert.AreEqual(total, page.Total);
                Assert.AreEqual(Math.Min(CareerFixturePage.FIXTURES_PER_PAGE, total - covered), page.Count);
                covered += page.Count;
                if (!page.HasNext)
                {
                    break;
                }

                CareerFixturePage next = CareerFixturePage.At(in home, page.NextFirst);
                Assert.AreEqual(page.First, next.PreviousFirst);
                Assert.IsTrue(next.HasPrevious);
                page = next;
            }

            Assert.AreEqual(total, covered, "The pages must reach the last fixture.");
            Assert.AreEqual(page.First, page.NextFirst);
            Assert.AreEqual(page.First, CareerFixturePage.At(in home, int.MaxValue).First);
            Assert.AreEqual(0, CareerFixturePage.At(in home, -5).First);
            Assert.AreEqual(CareerFixturePage.FIXTURES_PER_PAGE,
                CareerFixturePage.At(in home, CareerFixturePage.FIXTURES_PER_PAGE + 3).First);
        }

        /// <summary>The current page follows the next unplayed fixture as rounds are played.</summary>
        [Test]
        public void CareerFixturePage_CurrentFollowsTheNextUnplayedFixture()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            holder.Set(session);
            var source = new CareerHomeSource(holder);
            for (int round = 0; round < CareerFixturePage.FIXTURES_PER_PAGE; round++)
            {
                CareerTestSupport.ProgressUntil(session, CareerProgressAction.PlayMatchday);
                session.Progress();
            }

            CareerHomeViewModel home = source.Project();
            Assert.IsTrue(home.Fixtures[CareerFixturePage.FIXTURES_PER_PAGE - 1].IsPlayed);
            Assert.IsFalse(home.Fixtures[CareerFixturePage.FIXTURES_PER_PAGE].IsPlayed);
            CareerFixturePage current = CareerFixturePage.Current(in home);
            Assert.AreEqual(CareerFixturePage.FIXTURES_PER_PAGE, current.First);
            Assert.AreEqual(0, current.PreviousFirst);
        }

        /// <summary>Verifies that a fixture page cannot be requested before a career is published.</summary>
        [Test]
        public void CareerFixturePage_RequiresAnActiveCareer()
        {
            Assert.Throws<ArgumentException>(() => CareerFixturePage.Current(default));
            Assert.Throws<ArgumentException>(() => CareerFixturePage.At(default, 0));
        }

        /// <summary>Standings pages cover every table row exactly once, in order, and stop at both ends.</summary>
        [Test]
        public void CareerStandingsPages_CoverTheTableInOrderAndStopAtBothEnds()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            holder.Set(CareerTestSupport.StartQuick(temp.Slots));
            CareerHomeViewModel home = new CareerHomeSource(holder).Project();
            int total = home.Table.Count;
            Assert.Greater(total, CareerStandingsPage.ROWS_PER_PAGE, "The table must span several pages.");

            CareerStandingsPage page = CareerStandingsPage.At(in home, 0);
            Assert.IsFalse(page.HasPrevious);
            Assert.AreEqual(page.First, page.PreviousFirst);
            int covered = 0;
            while (true)
            {
                Assert.AreEqual(covered, page.First);
                Assert.AreEqual(total, page.Total);
                Assert.AreEqual(Math.Min(CareerStandingsPage.ROWS_PER_PAGE, total - covered), page.Count);
                covered += page.Count;
                if (!page.HasNext)
                {
                    break;
                }

                CareerStandingsPage next = CareerStandingsPage.At(in home, page.NextFirst);
                Assert.AreEqual(page.First, next.PreviousFirst);
                Assert.IsTrue(next.HasPrevious);
                page = next;
            }

            Assert.AreEqual(total, covered, "The pages must reach the last table row.");
            Assert.AreEqual(page.First, page.NextFirst);
            Assert.AreEqual(page.First, CareerStandingsPage.At(in home, int.MaxValue).First);
            Assert.AreEqual(0, CareerStandingsPage.At(in home, -5).First);
            Assert.AreEqual(CareerStandingsPage.ROWS_PER_PAGE,
                CareerStandingsPage.At(in home, CareerStandingsPage.ROWS_PER_PAGE + 3).First);
        }

        /// <summary>The default standings page holds the managed club's row, before and after a round.</summary>
        [Test]
        public void CareerStandingsPage_CurrentHoldsTheManagedClub()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            holder.Set(session);
            var source = new CareerHomeSource(holder);
            AssertCurrentPageHoldsManagedClub(source.Project());

            CareerTestSupport.ProgressUntil(session, CareerProgressAction.PlayMatchday);
            session.Progress();
            AssertCurrentPageHoldsManagedClub(source.Project());
        }

        private static void AssertCurrentPageHoldsManagedClub(CareerHomeViewModel home)
        {
            int managed = -1;
            for (int i = 0; i < home.Table.Count; i++)
            {
                if (home.Table[i].ClubId == home.ManagedClubId)
                {
                    managed = i;
                }
            }

            Assert.GreaterOrEqual(managed, 0, "The managed club must be in the table.");
            CareerStandingsPage current = CareerStandingsPage.Current(in home);
            Assert.That(managed, Is.InRange(current.First, current.First + current.Count - 1));
            Assert.AreEqual(managed - managed % CareerStandingsPage.ROWS_PER_PAGE, current.First);
        }

        /// <summary>Verifies that a standings page cannot be requested before a career is published.</summary>
        [Test]
        public void CareerStandingsPage_RequiresAnActiveCareer()
        {
            Assert.Throws<ArgumentException>(() => CareerStandingsPage.Current(default));
            Assert.Throws<ArgumentException>(() => CareerStandingsPage.At(default, 0));
        }

        [Test]
        public void CareerHome_ProjectsTheSeasonFromSeasonLoopsOwnState()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            holder.Set(CareerTestSupport.StartQuick(temp.Slots));
            CareerSession session = holder.Current;
            var source = new CareerHomeSource(holder);

            CareerHomeViewModel vm = source.Project();
            SeasonState state = session.Loop.State;

            Assert.IsTrue(vm.HasCareer);
            Assert.AreEqual(session.ManagedClubName, vm.ManagedClubName);
            Assert.AreEqual(0, vm.SeasonNumber, "#30's first season is 0.");
            Assert.AreEqual(state.ClubCount, vm.Table.Count);
            Assert.AreEqual(vm.Table.Count, vm.TableClubNames.Count);
            Assert.AreEqual(session.ClubName(vm.Table[0].ClubId), vm.TableClubNames[0]);
            Assert.AreEqual(state.PositionOf(session.ManagedClubId), vm.LeaguePosition);
            Assert.AreEqual(state.Board.Objective.TargetPositionOrBetter, vm.ObjectiveTargetPosition);
            Assert.AreEqual(CareerProgressAction.AdvanceDay, vm.NextAction);
            Assert.AreEqual(CareerChangeKind.Started, vm.LastChange.Kind);

            // The next fixture is the managed club's round-0 fixture in #30's own list.
            Assert.IsTrue(vm.HasNextFixture);
            Fixture expected = default;
            foreach (Fixture f in state.Fixtures)
            {
                if (f.RoundIndex == 0 && f.Involves(session.ManagedClubId))
                {
                    expected = f;
                    break;
                }
            }

            Assert.AreEqual(expected.HomeClubId == session.ManagedClubId, vm.NextFixtureIsHome);
            Assert.AreEqual(vm.NextFixtureIsHome ? expected.AwayClubId : expected.HomeClubId, vm.NextOpponentId);
            Assert.AreEqual(session.ClubName(vm.NextOpponentId), vm.NextOpponentName);
            Assert.AreEqual(state.Calendar.DayOfRound(0), vm.NextFixtureDay);
            Assert.AreEqual((int)(vm.NextFixtureDay - vm.WorldDay), vm.DaysUntilNextMatchday);
        }

        [Test]
        public void CareerHome_CachesBetweenOperations_AndRefreshesAfterOne()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            holder.Set(CareerTestSupport.StartQuick(temp.Slots));
            var source = new CareerHomeSource(holder);

            CareerHomeViewModel first = source.Project();
            CareerHomeViewModel again = source.Project();
            Assert.AreSame(first.Table, again.Table, "no rebuild without an operation.");
            Assert.AreSame(first.Fixtures, again.Fixtures);
            Assert.IsEmpty(first.LatestRoundResults);

            CareerTestSupport.ProgressUntil(holder.Current, CareerProgressAction.PlayMatchday);
            holder.Current.Progress();
            CareerHomeViewModel after = source.Project();

            Assert.AreNotSame(first.Table, after.Table);
            Assert.AreEqual(1, after.RoundsPlayed);
            Assert.AreEqual(CareerChangeKind.RoundPlayed, after.LastChange.Kind);
            Assert.IsTrue(after.LastChange.HasManagedResult);
            Assert.AreEqual(first.NextOpponentName, after.LastResultOpponentName,
                "the result reported is against the opponent the previous projection announced.");
            Assert.AreEqual(1, after.ManagedRow.Played);
            Assert.IsFalse(first.Fixtures[0].IsPlayed, "previously returned fixtures remain snapshots.");
            Assert.IsTrue(after.Fixtures[0].IsPlayed);
            Assert.IsFalse(after.Fixtures[1].IsPlayed);
            Assert.AreEqual(after.Fixtures[1].OpponentId, after.NextOpponentId);
            Assert.AreEqual(after.Fixtures[1].WorldDay, after.NextFixtureDay);
            AssertRoundResults(after, holder.Current);
            Assert.AreSame(after.LatestRoundResults, source.Project().LatestRoundResults);
            Assert.IsEmpty(first.LatestRoundResults);
        }

        /// <summary>Verifies that switching to a resumed career refreshes a previously cached schedule.</summary>
        [Test]
        public void CareerHome_ProjectsSavedCalendarAndPlayedFixturesAfterResume()
        {
            using var temp = new TempSlots();
            CareerSession original = CareerTestSupport.StartQuick(temp.Slots);
            var holder = new CareerSessionHolder();
            holder.Set(original);
            var source = new CareerHomeSource(holder);
            CareerTestSupport.ProgressUntil(original, CareerProgressAction.PlayMatchday);
            original.Progress();
            CareerHomeViewModel saved = source.Project();
            original.Progress();
            CareerHomeViewModel later = source.Project();
            Assert.Greater(later.WorldDay, saved.WorldDay);

            holder.Set(CareerSession.Resume(temp.Slots, CareerSaveSlot.Autosave, RoundResolutionMode.QuickSimAll));
            CareerHomeViewModel resumed = source.Project();

            Assert.AreEqual(saved.WorldDay, resumed.WorldDay);
            Assert.IsTrue(resumed.Calendar.FieldsEqual(saved.Calendar));
            CollectionAssert.AreEqual(saved.Fixtures, resumed.Fixtures);
            Assert.IsTrue(resumed.Fixtures[0].IsPlayed);
            CollectionAssert.AreEqual(saved.Table, resumed.Table);
            Assert.IsEmpty(resumed.LatestRoundResults);
            CollectionAssert.AreEqual(saved.LatestRoundResults.Select(row => row.Result),
                later.LatestRoundResults.Select(row => row.Result), "Advancing a day leaves the latest scores visible.");

            CareerTestSupport.ProgressUntil(holder.Current, CareerProgressAction.PlayMatchday);
            holder.Current.Progress();
            AssertRoundResults(source.Project(), holder.Current);
        }

        [Test]
        public void CareerHome_UsesTheNewSeasonAfterRoundIndicesRestart()
        {
            using var temp = new TempSlots();
            CareerSession session = CareerTestSupport.StartQuick(temp.Slots);
            var holder = new CareerSessionHolder();
            holder.Set(session);
            var source = new CareerHomeSource(holder);

            CareerTestSupport.ProgressUntil(session, CareerProgressAction.StartNextSeason);
            int previousSeasonOutcomes = session.Loop.MatchOutcomes.Count;
            Assert.Greater(previousSeasonOutcomes, 0);
            MatchResult oldFirstRound = session.Loop.MatchOutcomes[0];
            Assert.AreEqual(0, oldFirstRound.RoundIndex);
            session.Progress();
            CareerHomeViewModel newSeason = source.Project();
            Assert.AreEqual(1, newSeason.SeasonNumber);
            Assert.AreEqual(previousSeasonOutcomes, session.Loop.MatchOutcomes.Count);
            Assert.IsEmpty(newSeason.LatestRoundResults);

            CareerTestSupport.ProgressUntil(session, CareerProgressAction.PlayMatchday);
            session.Progress();
            CareerHomeViewModel newFirstRound = source.Project();
            AssertRoundResults(newFirstRound, session);
            Assert.Greater(newFirstRound.LatestRoundResults[0].Result.WorldDay, oldFirstRound.WorldDay);
            Assert.AreEqual(previousSeasonOutcomes + newFirstRound.LatestRoundResults.Count, session.Loop.MatchOutcomes.Count);
        }

        [Test]
        public void CareerHome_SkipsAnInterruptedRoundUntilItCompletes()
        {
            const ulong seed = CareerTestSupport.WORLD_SEED;
            League league = LeagueBootstrap.Generate(seed, 4);
            var loop = new SeasonLoop(new WorldStore(1, seed), league.CreateSeason(0), RoundResolutionMode.QuickSimAll);
            loop.AdvanceToNextFixtureDay();
            MatchResult[] first = loop.AdvanceAndPlayNextRound(league);
            loop.AdvanceToNextFixtureDay();

            Fixture blocked = loop.State.Fixtures.Where(fixture => fixture.RoundIndex == 1).ElementAt(1);
            Assert.Throws<ArgumentException>(() => loop.AdvanceAndPlayNextRound(new MissingRoster(league, blocked.HomeClubId)));
            Assert.AreEqual(1, loop.NextRoundIndex);
            Assert.AreEqual(first.Length + 1, loop.MatchOutcomes.Count, "One fixture in the unfinished round emitted a result.");
            MatchResult partial = loop.MatchOutcomes[first.Length];
            Assert.AreEqual(1, partial.RoundIndex);
            CollectionAssert.AreEqual(first, CareerHomeSource.ProjectRoundResults(league, loop).Select(row => row.Result));

            MatchResult[] remaining = loop.AdvanceAndPlayNextRound(league);
            Assert.AreEqual(2, loop.NextRoundIndex);
            CollectionAssert.AreEqual(new[] { partial, remaining.Single() },
                CareerHomeSource.ProjectRoundResults(league, loop).Select(row => row.Result));
        }

        private static void AssertRoundResults(in CareerHomeViewModel view, CareerSession session)
        {
            int index = view.RoundsPlayed - 1;
            uint day = view.Calendar.DayOfRound(index);
            MatchResult[] expected = session.Loop.MatchOutcomes.Where(result => result.WorldDay == day).ToArray();
            Assert.AreEqual(session.Loop.State.ClubCount / 2, expected.Length);
            CollectionAssert.AreEqual(expected, view.LatestRoundResults.Select(row => row.Result));
            for (int i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(index, view.LatestRoundResults[i].Result.RoundIndex);
                Assert.AreEqual(session.ClubName(expected[i].HomeClubId), view.LatestRoundResults[i].HomeClubName);
                Assert.AreEqual(session.ClubName(expected[i].AwayClubId), view.LatestRoundResults[i].AwayClubName);
            }
        }

        private sealed class MissingRoster : ISquadProvider
        {
            private readonly League _league;
            private readonly int _club;

            internal MissingRoster(League league, int club)
            {
                _league = league;
                _club = club;
            }

            public Squad ResolveByClubId(int clubId) => clubId == _club ? null : _league.ResolveByClubId(clubId);
        }

        [Test]
        public void SeasonDispatcher_RoutesSeasonIntentsThroughTheSession()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            holder.Set(CareerTestSupport.StartQuick(temp.Slots));
            var dispatcher = new SeasonCommandDispatcher(holder);
            uint day = holder.Current.Loop.CurrentWorldDay;

            dispatcher.Dispatch(ManagerIntent.AdvanceDay());
            Assert.AreEqual(day + 1, holder.Current.Loop.CurrentWorldDay);

            Assert.Throws<InvalidOperationException>(() => dispatcher.Dispatch(ManagerIntent.AdvanceRound()),
                "an intent for an action that is not legal now is refused, not forced.");

            CareerTestSupport.ProgressUntil(holder.Current, CareerProgressAction.PlayMatchday);
            dispatcher.Dispatch(ManagerIntent.AdvanceRound());
            Assert.AreEqual(1, holder.Current.Loop.NextRoundIndex);
        }

        [Test]
        public void SeasonDispatcher_RefusesNonSeasonIntents_AndAMissingCareer()
        {
            var holder = new CareerSessionHolder();
            var dispatcher = new SeasonCommandDispatcher(holder);

            Assert.Throws<InvalidOperationException>(() => dispatcher.Dispatch(default(ManagerIntent)));
            Assert.Throws<InvalidOperationException>(() =>
                dispatcher.Dispatch(ManagerIntent.SetTeamTactic(0, TeamTactic.Balanced)));
            Assert.Throws<InvalidOperationException>(() =>
                dispatcher.Dispatch(ManagerIntent.Substitute(0, 0, 0, SubstitutionReason.Tactical)));
            Assert.Throws<InvalidOperationException>(() => dispatcher.Dispatch(ManagerIntent.AdvanceDay()),
                "no career is active.");
        }

        [Test]
        public void NewCareer_PreviewsTheLeagueItStarts()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            var setup = new NewCareerSetup(holder, temp.Slots, RoundResolutionMode.QuickSimAll);

            Assert.Throws<InvalidOperationException>(() => setup.Select(0));
            setup.Prepare(CareerTestSupport.WORLD_SEED);
            NewCareerViewModel vm = setup.Project();
            Assert.IsFalse(vm.HasSelection);
            Assert.Throws<InvalidOperationException>(() => setup.Start(), "no club selected.");
            Assert.Throws<ArgumentException>(() => setup.Select(999));

            Club chosen = vm.Clubs[CareerTestSupport.MANAGED_CLUB];
            setup.Select(chosen.ClubId);
            CareerSession session = setup.Start();

            Assert.AreSame(session, holder.Current);
            Assert.AreEqual(chosen.ClubId, session.ManagedClubId);
            Assert.AreEqual(chosen.Name, session.ManagedClubName, "the previewed club is the started club.");
            Assert.AreEqual(vm.DefaultObjectivePosition, session.Loop.State.Board.Objective.TargetPositionOrBetter);
            Assert.AreEqual(CareerSlotState.Ready, temp.Slots.Describe(CareerSaveSlot.Autosave).State);
        }

        [Test]
        public void LoadCareer_ContinueAndLoad_InstallTheCareer_AndAFailureStaysVisible()
        {
            using var temp = new TempSlots();
            var holder = new CareerSessionHolder();
            var load = new LoadCareerSource(holder, temp.Slots, RoundResolutionMode.QuickSimAll);

            Assert.IsFalse(load.Project().CanContinue);
            Assert.IsFalse(load.TryContinue());
            Assert.IsNotNull(load.Project().LastError, "the reason Continue failed is kept.");
            Assert.IsFalse(holder.HasCareer);

            CareerSession original = CareerTestSupport.StartQuick(temp.Slots);
            original.Progress();
            original.SaveTo(CareerSaveSlot.Manual1);
            load.Refresh();
            Assert.IsTrue(load.Project().CanContinue);
            Assert.AreEqual(CareerSaveSlot.Manual1, load.Project().LatestSlot);
            Assert.IsNull(load.Project().LastError);

            Assert.IsTrue(load.TryContinue());
            CareerSession continued = holder.Current;
            Assert.AreEqual(original.Loop.CurrentWorldDay, continued.Loop.CurrentWorldDay);

            // A damaged slot fails visibly and does not replace the active career (§7 rule 4).
            File.WriteAllBytes(temp.Slots.SavePath(CareerSaveSlot.Manual1), new byte[] { 1, 2, 3 });
            Assert.IsFalse(load.TryLoad(CareerSaveSlot.Manual1));
            Assert.IsNotNull(load.Project().LastError);
            Assert.AreSame(continued, holder.Current);

            Assert.IsTrue(load.TryLoad(CareerSaveSlot.Autosave));
            Assert.AreNotSame(continued, holder.Current);
            Assert.IsNull(load.Project().LastError);
        }

        [Test]
        public void LoadCareer_ACorruptNewestSlot_IsInvalidated_SoContinueFallsBackToTheOlderSave()
        {
            using var temp = new TempSlots();
            CareerSession original = CareerTestSupport.StartQuick(temp.Slots);
            original.SaveTo(CareerSaveSlot.Manual1);
            string savePath = temp.Slots.SavePath(CareerSaveSlot.Manual1);
            File.WriteAllBytes(savePath, new byte[] { 1, 2, 3 });

            var holder = new CareerSessionHolder();
            var load = new LoadCareerSource(holder, temp.Slots, RoundResolutionMode.QuickSimAll);
            Assert.AreEqual(CareerSaveSlot.Manual1, load.Project().LatestSlot, "precondition: the corrupt slot is newest.");

            Assert.IsFalse(load.TryContinue());
            Assert.IsNotNull(load.Project().LastError);
            Assert.IsFalse(holder.HasCareer);
            Assert.AreEqual(CareerSlotState.Damaged, temp.Slots.Describe(CareerSaveSlot.Manual1).State);
            Assert.AreEqual(3, File.ReadAllBytes(savePath).Length, "invalidation keeps the save bytes.");
            Assert.IsFalse(temp.Slots.RestoreDamaged(CareerSaveSlot.Manual1),
                "a set-aside sidecar never comes back over a save it does not describe.");
            Assert.AreEqual(CareerSaveSlot.Autosave, load.Project().LatestSlot);

            Assert.IsTrue(load.TryContinue(), "Continue moves on to the older, loadable autosave.");
            Assert.IsTrue(holder.HasCareer);
        }

        [Test]
        public void LoadCareer_AnUnloadableSlot_IsNotInvalidated()
        {
            using var temp = new TempSlots();
            CareerSession original = CareerTestSupport.StartQuick(temp.Slots);
            original.SaveTo(CareerSaveSlot.Manual2);
            string sidecar = Path.Combine(temp.Directory, "manual-2.slot");
            string text = File.ReadAllText(sidecar);
            string current = "seasonSaveFormat=" + SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION;
            File.WriteAllText(sidecar, text.Replace(current, "seasonSaveFormat=" + (SeasonSaveConstants.SEASON_SAVE_FORMAT_VERSION - 1)));

            var load = new LoadCareerSource(new CareerSessionHolder(), temp.Slots, RoundResolutionMode.QuickSimAll);
            Assert.IsFalse(load.TryLoad(CareerSaveSlot.Manual2));
            Assert.AreEqual(CareerSlotState.Incompatible, temp.Slots.Describe(CareerSaveSlot.Manual2).State,
                "a refused load of an Incompatible slot leaves its D-10 classification alone.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): 7 locks over Career Home, the season |
// |         |            |        | dispatcher, New Career and Load Career.                        |
// | 1.1     | 2026-10-27 | —      | PR 519 review: +2 locks — a corrupt newest slot is invalidated |
// |         |            |        | so Continue falls back; a refused Incompatible slot is kept.   |
// | 1.2     | 2026-10-27 | —      | PR 519 review 4: the corrupt slot's set-aside sidecar cannot   |
// |         |            |        | be restored over the save it no longer describes.              |
// | 1.3     | 2026-11-19 | —      | Verify schedule projection, refresh and resume.                |
// | 1.4     | 2026-11-19 | —      | Verify calendar paging across the season.                     |
// | 1.5     | 2026-11-19 | —      | Verify fixture paging coverage, bounds and the current page.  |
// | 1.6     | 2026-11-19 | —      | Lock round scores through resume, rollover and fixture retry. |
// | 1.7     | 2026-11-20 | —      | Verify standings paging coverage, bounds and the managed-club page. |
#endregion
