// File:     src/client-app/tests/CareerScreenPresenterTests.cs
// Created:  2026-11-06
// Modified: 2026-11-06
// Author:   —
// Spec:     Interactive Unity client §5-P7, UI / Client Framework #38, Code Standards #20
// Purpose:  Lock worker handoff, guarded navigation, saved-slot recovery and overwrite behavior.

using System;
using System.IO;
using System.Threading;

using NUnit.Framework;

using TacticalDirector.SeasonSave;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class CareerScreenPresenterTests
    {
        private TempSlots _temp;
        private CareerScreenPresenter _view;
        private ClientScreenFlow _flow;
        private Action _work;

        [SetUp]
        public void SetUp()
        {
            _temp = new TempSlots();
            _view = new CareerScreenPresenter(_temp.Directory, RoundResolutionMode.QuickSimAll, work => _work = work);
            _flow = Attach(_view);
            Run(_view.Initialize);
        }

        [TearDown]
        public void TearDown() { _view.Dispose(); _temp.Dispose(); }

        private static ClientScreenFlow Attach(CareerScreenPresenter view)
        {
            var menu = new ScreenRegistration(ClientScreens.MainMenu, null, null);
            var setup = new ScreenRegistration(ClientScreens.TacticsSetup, null, null);
            var match = new ScreenRegistration(ClientScreens.MatchView, null, null);
            var report = new ScreenRegistration(ClientScreens.PostMatchReport, null, null);
            var home = new ScreenRegistration(ClientScreens.CareerHome, view, view);
            var create = new ScreenRegistration(ClientScreens.NewCareer, view, null);
            var load = new ScreenRegistration(ClientScreens.LoadCareer, view, null);
            var flow = new ClientScreenFlow(in menu, in setup, in match, in report, in home, in create, in load);
            view.AttachFlow(flow);
            return flow;
        }

        private void Run(Action action)
        {
            action();
            Complete();
        }

        private void Complete()
        {
            Assert.IsTrue(_view.IsBusy);
            Action work = _work;
            _work = null;
            work();
            Assert.IsTrue(_view.IsBusy, "Even finished work must wait for the UI handoff.");
            _view.Refresh();
            Assert.IsFalse(_view.IsBusy);
        }

        private void Start()
        {
            Run(() => _view.OpenNew(CareerTestSupport.WORLD_SEED));
            _view.SelectClub(CareerTestSupport.MANAGED_CLUB);
            Run(_view.StartCareer);
        }

        [Test]
        public void NewCareer_SelectionAutosaveAndQuitHaveExplicitDestinations()
        {
            Assert.IsFalse(_view.Load.CanContinue);
            Run(() => _view.OpenNew(CareerTestSupport.WORLD_SEED));
            Assert.AreEqual(ClientScreens.NewCareer, _flow.Current);
            Run(_view.StartCareer);
            Assert.IsNotEmpty(_view.Error);
            Assert.AreEqual(ClientScreens.NewCareer, _flow.Current);
            _view.SelectClub(CareerTestSupport.MANAGED_CLUB);
            Run(_view.StartCareer);
            Assert.IsNull(_view.Error);
            Assert.AreEqual(ClientScreens.CareerHome, _flow.Current);
            Assert.IsTrue(_view.Home.HasCareer);
            Assert.AreEqual(CareerSlotState.Ready, _view.Load.Slots[0].State);
            _view.Back();
            Assert.AreEqual(ClientScreens.MainMenu, _flow.Current);
            Assert.IsFalse(_view.Home.HasCareer);
            Run(_view.ContinueCareer);
            Assert.AreEqual(ClientScreens.CareerHome, _flow.Current);
            Assert.AreEqual(CareerChangeKind.Resumed, _view.Home.LastChange.Kind);
        }

        [Test]
        public void BusyActions_KeepSnapshotAndRefuseAllReentryUntilUiCompletion()
        {
            Start();
            uint day = _view.Home.WorldDay;
            var table = _view.Home.Table;
            ManagerIntent invalid = default;
            Assert.Throws<InvalidOperationException>(() => _view.Dispatch(in invalid));
            Assert.IsFalse(_view.IsBusy);
            _view.Progress();
            Assert.IsTrue(_view.Home.IsProgressBlocked);
            Assert.AreEqual(day, _view.Home.WorldDay);
            Assert.AreSame(table, _view.Home.Table);
            Assert.Throws<InvalidOperationException>(_view.Progress);
            Assert.Throws<InvalidOperationException>(_view.Back);
            Assert.Throws<InvalidOperationException>(() => _view.RequestSave(CareerSaveSlot.Manual1));
            Assert.Throws<InvalidOperationException>(_view.RefreshSlots);
            _view.Refresh();
            Assert.IsTrue(_view.IsBusy, "An unfinished worker never blocks Refresh.");
            Complete();
            Assert.IsFalse(_view.Home.IsProgressBlocked);
            Assert.Greater(_view.Home.WorldDay, day);
        }

        [Test]
        public void ManualSave_RequiresConfirmationAndContinueUsesLatestSlot()
        {
            Start();
            Run(_view.Progress);
            Run(() => _view.RequestSave(CareerSaveSlot.Manual2));
            uint savedDay = _view.Home.WorldDay;
            Assert.AreEqual(CareerSaveSlot.Manual2, _view.SavedSlot);
            long sequence = _view.Load.Slots[2].Sequence;
            Run(_view.Progress);
            _view.RequestSave(CareerSaveSlot.Manual2);
            Assert.AreEqual(CareerSaveSlot.Manual2, _view.PendingOverwrite);
            Assert.IsFalse(_view.IsBusy);
            _view.CancelOverwrite();
            Assert.AreEqual(sequence, _temp.Slots.Describe(CareerSaveSlot.Manual2).Sequence);
            _view.Back();
            Run(_view.ContinueCareer);
            Assert.AreEqual(savedDay, _view.Home.WorldDay);
            _view.RequestSave(CareerSaveSlot.Manual2);
            Run(_view.ConfirmOverwrite);
            Assert.Greater(_view.Load.Slots[2].Sequence, sequence);
            Assert.IsNull(_view.PendingOverwrite);
            Assert.Throws<ArgumentOutOfRangeException>(() => _view.RequestSave(CareerSaveSlot.Autosave));
        }

        [Test]
        public void CorruptLatestSave_FailureStaysVisibleAndContinueCanFallBack()
        {
            Start();
            Run(() => _view.RequestSave(CareerSaveSlot.Manual1));
            File.WriteAllBytes(_temp.Slots.SavePath(CareerSaveSlot.Manual1), new byte[] { 1, 2, 3 });
            _view.Back();
            Run(_view.ContinueCareer);
            Assert.AreEqual(ClientScreens.MainMenu, _flow.Current);
            Assert.IsNotEmpty(_view.Error);
            Assert.AreEqual(CareerSlotState.Damaged, _view.Load.Slots[1].State);
            string error = _view.Error;
            _view.Refresh();
            Assert.AreEqual(error, _view.Error);
            Run(_view.ContinueCareer);
            Assert.AreEqual(ClientScreens.CareerHome, _flow.Current);
            Assert.IsNull(_view.Error);
        }

        [Test]
        public void PickerFailure_PreservesCareerAndBackReturnsToMenu()
        {
            Start();
            _view.Back();
            Run(_view.OpenLoad);
            Run(() => _view.LoadSlot(CareerSaveSlot.Manual3));
            Assert.AreEqual(ClientScreens.LoadCareer, _flow.Current);
            Assert.IsNotEmpty(_view.Error);
            Run(() => _view.LoadSlot(CareerSaveSlot.Autosave));
            Assert.AreEqual(ClientScreens.CareerHome, _flow.Current);
            Assert.IsTrue(_view.Home.HasCareer);
        }

        [Test]
        public void FailedManualSave_KeepsCareerAndCanBeRetriedWithoutFalseSuccess()
        {
            Start();
            string path = _temp.Slots.SavePath(CareerSaveSlot.Manual1);
            Directory.CreateDirectory(path);
            Run(() => _view.RequestSave(CareerSaveSlot.Manual1));
            Assert.IsNotEmpty(_view.Error);
            Assert.IsNull(_view.SavedSlot);
            Assert.IsTrue(_view.Home.HasCareer);
            Assert.AreEqual(ClientScreens.CareerHome, _flow.Current);
            Directory.Delete(path);
            Run(() => _view.RequestSave(CareerSaveSlot.Manual1));
            Assert.IsNull(_view.Error);
            Assert.AreEqual(CareerSaveSlot.Manual1, _view.SavedSlot);
        }

        [Test]
        public void Matchday_WorkerCommitsRoundAndAutosaveWithoutEnteringDemoGraph()
        {
            Start();
            while (_view.Home.NextAction != CareerProgressAction.PlayMatchday) Run(_view.Progress);
            int rounds = _view.Home.RoundsPlayed;
            long sequence = _view.Load.Slots[0].Sequence;
            Run(_view.Progress);
            Assert.AreEqual(rounds + 1, _view.Home.RoundsPlayed);
            Assert.AreEqual(CareerChangeKind.RoundPlayed, _view.Home.LastChange.Kind);
            Assert.Greater(_view.Load.Slots[0].Sequence, sequence);
            Assert.AreEqual(ClientScreens.CareerHome, _flow.Current);
        }

        [Test]
        public void DisposeBeforeWorkerStarts_CancelsQueuedActionWithoutNavigation()
        {
            Run(() => _view.OpenNew(CareerTestSupport.WORLD_SEED));
            _view.SelectClub(CareerTestSupport.MANAGED_CLUB);
            _view.StartCareer();
            _view.Dispose();
            _work();
            _view.Refresh();
            Assert.AreEqual(ClientScreens.NewCareer, _flow.Current);
            Assert.IsFalse(_view.Home.HasCareer);
            Assert.AreEqual(CareerSlotState.Empty, _temp.Slots.Describe(CareerSaveSlot.Autosave).State);
            Assert.Throws<InvalidOperationException>(_view.StartCareer);
        }

        [Test]
        public void FailedStorageInitialization_CanBeRetried()
        {
            string path = Path.Combine(_temp.Directory, "blocked");
            File.WriteAllText(path, "file prevents directory creation");
            using var view = new CareerScreenPresenter(path, RoundResolutionMode.QuickSimAll, work => work());
            Attach(view);
            view.Initialize(); view.Refresh();
            Assert.IsFalse(view.IsReady);
            Assert.IsNotEmpty(view.Error);
            File.Delete(path);
            view.Initialize(); view.Refresh();
            Assert.IsTrue(view.IsReady);
            Assert.IsNull(view.Error);
        }

        [Test]
        public void ProductionScheduler_CompletesWithoutCallingUnityOrNavigatingOnWorker()
        {
            using var view = new CareerScreenPresenter(_temp.Directory);
            ClientScreenFlow flow = Attach(view);
            view.Initialize();
            Assert.IsTrue(SpinWait.SpinUntil(() => { view.Refresh(); return !view.IsBusy; }, 10000));
            Assert.IsTrue(view.IsReady, view.Error);
            view.OpenNew(CareerTestSupport.WORLD_SEED);
            Assert.AreEqual(ClientScreens.NewCareer, flow.Current);
            Assert.IsTrue(SpinWait.SpinUntil(() => { view.Refresh(); return !view.IsBusy; }, 10000));
            Assert.IsNotNull(view.New.Clubs);
            Assert.IsFalse(view.New.HasSelection);
        }
    }
}
#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-06 | —      | P7b worker, navigation, recovery, confirmation and lifetime regressions. |
#endregion
