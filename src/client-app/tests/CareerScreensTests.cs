// File:     src/client-app/tests/CareerScreensTests.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7, UI / Client Framework #38 §3.1
//           (FR-UI-006) and Appendix C (season dispatcher, F3), Code Standards #20
// Purpose:  Locks the career screens' sources and the season dispatcher: Career Home answers its
//           questions from #30's own state and caches between operations; the dispatcher routes the
//           three season kinds and refuses everything else; New Career previews the league it starts;
//           Load Career keeps a failure visible and leaves the active career untouched.

using System;
using System.IO;

using NUnit.Framework;

using TacticalDirector.MatchEngine;
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
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-10-18 | —      | Initial creation (§5-P7): 7 locks over Career Home, the season |
// |         |            |        | dispatcher, New Career and Load Career.                        |
#endregion
