// File:     src/client-app/tests/ClientCareerCoordinatorTests.cs
// Created:  2026-11-03
// Modified: 2026-11-05 (§5-P8 transfer operation)
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (P7b), Code Standards #20
// Purpose:  Lock worker/UI ownership, cached publication, guarded navigation, persistence and failure recovery.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

using NUnit.Framework;

using TacticalDirector.SeasonSave;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp.Tests
{
    [TestFixture]
    public sealed class ClientCareerCoordinatorTests
    {
        private sealed class Harness : IDisposable
        {
            internal readonly TempSlots Temp = new TempSlots();
            internal readonly Queue<Action> Work = new Queue<Action>();
            internal readonly ClientCareerCoordinator Career;
            internal readonly ClientScreenFlow Flow;

            internal Harness()
            {
                Career = new ClientCareerCoordinator(Temp.Directory, RoundResolutionMode.QuickSimAll, Work.Enqueue);
                Flow = Compose(Career);
                Career.Initialize();
                Finish();
            }

            internal void RunWorker()
            {
                Action work = Work.Dequeue();
                var worker = new Thread(() => work());
                worker.Start();
                Assert.IsTrue(worker.Join(TimeSpan.FromSeconds(30)), "Career operation did not finish.");
            }

            internal void Finish()
            {
                RunWorker();
                Career.Refresh();
            }

            internal void Start()
            {
                Career.OpenNewCareer(CareerTestSupport.WORLD_SEED);
                Finish();
                Career.SelectClub(CareerTestSupport.MANAGED_CLUB);
                Career.StartCareer();
                Finish();
            }

            public void Dispose()
            {
                while (Work.Count > 0) Finish();
                Career.Dispose();
                Temp.Dispose();
            }
        }

        private static ClientScreenFlow Compose(ClientCareerCoordinator career)
        {
            var flow = new ClientScreenFlow(
                new ScreenRegistration(ClientScreens.MainMenu, null, null),
                new ScreenRegistration(ClientScreens.TacticsSetup, null, null),
                new ScreenRegistration(ClientScreens.MatchView, null, null),
                new ScreenRegistration(ClientScreens.PostMatchReport, null, null),
                career.HomeRegistration, career.NewRegistration, career.LoadRegistration,
                career.TransfersRegistration, career.FinancesRegistration);
            career.AttachFlow(flow);
            return flow;
        }

        [Test]
        public void InitializeDoesNotTouchDiskUntilWorker_AndPublishesOnlyOnUiRefresh()
        {
            using var temp = new TempSlots();
            string path = Path.Combine(temp.Directory, "lazy-store");
            Action pending = null;
            using var career = new ClientCareerCoordinator(path, RoundResolutionMode.QuickSimAll, work => pending = work);
            Compose(career);
            Assert.IsFalse(Directory.Exists(path));
            career.Initialize();
            Assert.IsTrue(career.IsBusy);
            Assert.IsFalse(Directory.Exists(path));
            Assert.IsNull(career.Load.Slots);
            pending();
            Assert.IsTrue(Directory.Exists(path));
            Assert.IsTrue(career.IsBusy, "Worker completion alone cannot publish or navigate.");
            Assert.IsNull(career.Load.Slots);
            career.Refresh();
            Assert.IsTrue(career.IsInitialized);
            Assert.IsFalse(career.IsBusy);
            Assert.AreEqual(CareerSaveSlots.SlotCount, career.Load.Slots.Count);
        }

        [Test]
        public void StartPublishesTheChosenCareer_AndReplacesTheFormOnlyAfterCompletion()
        {
            using var h = new Harness();
            h.Career.OpenNewCareer(CareerTestSupport.WORLD_SEED);
            Assert.AreEqual(ClientScreens.NewCareer, h.Flow.Current);
            Assert.IsNull(h.Career.NewCareer.Clubs);
            Assert.Throws<InvalidOperationException>(() => h.Career.SelectClub(CareerTestSupport.MANAGED_CLUB));
            h.Finish();
            Assert.IsFalse(h.Career.CanStart);
            h.Career.SelectClub(CareerTestSupport.MANAGED_CLUB);
            Assert.IsTrue(h.Career.CanStart);
            h.Career.StartCareer();
            Assert.IsFalse(h.Career.CanStart);
            h.RunWorker();
            Assert.AreEqual(ClientScreens.NewCareer, h.Flow.Current);
            Assert.IsFalse(h.Career.Home.HasCareer);
            h.Career.Refresh();
            Assert.AreEqual(ClientScreens.CareerHome, h.Flow.Current);
            Assert.AreEqual(CareerTestSupport.MANAGED_CLUB, h.Career.Home.ManagedClubId);
            Assert.IsTrue(h.Career.Load.CanContinue, "Start autosaves before success is published.");
            h.Career.QuitToMainMenu();
            Assert.AreEqual(ClientScreens.MainMenu, h.Flow.Current);
            Assert.IsFalse(h.Career.Home.HasCareer);
        }

        [Test]
        public void TransferOffer_RunsOnTheWorker_AndPublishesItsTypedOutcomeAndFreshSnapshots()
        {
            using var h = new Harness();
            h.Start();
            Assert.IsTrue(h.Career.TransferMarket.IsWindowOpen, "Start publishes the market snapshot.");
            Assert.IsTrue(h.Career.Finances.HasCareer, "Start publishes the finance snapshot.");
            Assert.Throws<InvalidOperationException>(() => h.Career.SubmitTransfer(
                new TacticalDirector.Transfers.Offer(0, 0, 1L, 1L, 1, true)), "offers only on Transfers");

            h.Career.OpenTransfers();
            Assert.AreEqual(ClientScreens.Transfers, h.Flow.Current);
            TransferMarketViewModel before = h.Career.TransferMarket;
            TransferPlayerRow own = default;
            foreach (TransferPlayerRow row in before.Players)
            {
                if (row.IsManaged) { own = row; break; }
            }

            int buyer = -1;
            foreach (TransferClubRow club in before.Clubs)
            {
                if (!club.IsManaged) { buyer = club.ClubId; break; }
            }

            // A sale at fee 1 is at or below every club's value, so it is Accepted.
            h.Career.SubmitTransfer(new TacticalDirector.Transfers.Offer(own.PlayerId, buyer, 1L, 0L, 1, false));
            Assert.IsTrue(h.Career.IsBusy);
            Assert.IsNull(h.Career.LastTransferOutcome, "nothing is published before Refresh");
            Assert.AreSame(before.Players, h.Career.TransferMarket.Players, "the cached snapshot is untouched while work runs");
            h.Finish();

            Assert.AreEqual(TacticalDirector.Transfers.TransferSubmissionOutcome.Accepted, h.Career.LastTransferOutcome);
            Assert.IsNull(h.Career.LastError);
            Assert.AreEqual(ClientScreens.Transfers, h.Flow.Current, "an offer never navigates");
            Assert.AreEqual(before.ManagedSquadCount - 1, h.Career.TransferMarket.ManagedSquadCount);
            Assert.AreEqual(before.TransferCeiling, h.Career.Finances.TransferBudget);

            h.Career.CloseTransfers();
            Assert.IsNull(h.Career.LastTransferOutcome, "leaving clears the result line");
            h.Career.OpenClubFinances();
            Assert.AreEqual(ClientScreens.ClubFinances, h.Flow.Current);
            h.Career.CloseClubFinances();
            Assert.AreEqual(ClientScreens.CareerHome, h.Flow.Current);
        }

        [Test]
        public void BusyBlocksDuplicateMutationAndNavigation_WhileCachedHomeRemainsStable()
        {
            using var h = new Harness();
            h.Start();
            CareerHomeViewModel before = h.Career.Home;
            h.Career.Progress();
            Assert.IsTrue(h.Career.Home.IsProgressBlocked);
            Assert.Throws<InvalidOperationException>(h.Career.Progress);
            Assert.Throws<InvalidOperationException>(h.Career.QuitToMainMenu);
            Assert.Throws<InvalidOperationException>(() => h.Career.SaveTo(CareerSaveSlot.Manual1));
            Assert.AreEqual(1, h.Work.Count);
            var source = (IViewModelSource<CareerHomeViewModel>)h.Flow.CurrentRegistration.ViewModelSource;
            Assert.AreEqual(before.WorldDay, source.Project().WorldDay);
            Assert.AreSame(before.Table, source.Project().Table);
            h.RunWorker();
            Assert.AreEqual(before.WorldDay, source.Project().WorldDay, "No live source polling after worker mutation.");
            h.Career.Refresh();
            Assert.IsFalse(source.Project().IsProgressBlocked);
            Assert.Greater(source.Project().WorldDay, before.WorldDay);
        }

        [Test]
        public void RegisteredDispatcherQueuesOnlyTheOfferedAction_AndMatchdayStaysOnHome()
        {
            using var h = new Harness();
            h.Start();
            ManagerIntent wrong = ManagerIntent.AdvanceRound();
            Assert.Throws<InvalidOperationException>(() => h.Flow.CurrentRegistration.Dispatcher.Dispatch(in wrong));
            while (h.Career.Home.NextAction != CareerProgressAction.PlayMatchday)
            {
                ManagerIntent day = ManagerIntent.AdvanceDay();
                h.Flow.CurrentRegistration.Dispatcher.Dispatch(in day);
                h.Finish();
            }
            h.Career.Progress();
            h.Finish();
            Assert.AreEqual(1, h.Career.Home.RoundsPlayed);
            Assert.IsTrue(h.Career.Home.LastChange.HasManagedResult);
            Assert.AreEqual(ClientScreens.CareerHome, h.Flow.Current, "D-P7-6 does not exist yet.");
        }

        [Test]
        public void ManualSaveAndContinueRestoreTheSavedDay_DirectlyFromMenu()
        {
            using var h = new Harness();
            h.Start();
            h.Career.Progress();
            h.Finish();
            uint day = h.Career.Home.WorldDay;
            h.Career.SaveTo(CareerSaveSlot.Manual2);
            h.Finish();
            h.Career.QuitToMainMenu();
            h.Career.ContinueCareer();
            Assert.AreEqual(ClientScreens.MainMenu, h.Flow.Current);
            h.Finish();
            Assert.AreEqual(ClientScreens.CareerHome, h.Flow.Current);
            Assert.AreEqual(day, h.Career.Home.WorldDay);
            Assert.AreEqual(CareerSaveSlot.Manual2, h.Career.Load.LatestSlot);
            Assert.Throws<ArgumentOutOfRangeException>(() => h.Career.SaveTo(CareerSaveSlot.Autosave));
            Assert.IsFalse(h.Career.IsBusy);
        }

        [Test]
        public void FailedLoadKeepsPickerAndReason_ThenSuccessfulLoadClearsTheError()
        {
            using var h = new Harness();
            h.Start();
            h.Career.QuitToMainMenu();
            h.Career.OpenLoadCareer();
            h.Finish();
            h.Career.LoadCareer(CareerSaveSlot.Manual3);
            h.Finish();
            Assert.AreEqual(ClientScreens.LoadCareer, h.Flow.Current);
            Assert.IsNotEmpty(h.Career.LastError);
            string error = h.Career.LastError;
            h.Career.Refresh();
            Assert.AreEqual(error, h.Career.LastError);
            Assert.IsFalse(h.Career.Home.HasCareer);
            h.Career.LoadCareer(CareerSaveSlot.Autosave);
            h.Finish();
            Assert.IsNull(h.Career.LastError);
            Assert.AreEqual(ClientScreens.CareerHome, h.Flow.Current);
        }

        [Test]
        public void CancelReturnsToMenu_AndReentryRequiresFreshClubSelection()
        {
            using var h = new Harness();
            h.Career.OpenNewCareer(CareerTestSupport.WORLD_SEED);
            h.Finish();
            h.Career.SelectClub(CareerTestSupport.MANAGED_CLUB);
            h.Career.CancelNewCareer();
            h.Career.OpenNewCareer(CareerTestSupport.WORLD_SEED);
            h.Finish();
            Assert.IsFalse(h.Career.NewCareer.HasSelection);
            h.Career.CancelNewCareer();
            h.Career.OpenLoadCareer();
            h.Finish();
            h.Career.CancelLoadCareer();
            Assert.AreEqual(ClientScreens.MainMenu, h.Flow.Current);
        }

        [Test]
        public void SaveFailureLeavesProgressIntact_AndSuccessfulRetryClearsTheError()
        {
            using var h = new Harness();
            h.Start();
            h.Career.Progress();
            h.Finish();
            uint day = h.Career.Home.WorldDay;
            string blocked = h.Temp.Slots.SavePath(CareerSaveSlot.Manual1);
            Directory.CreateDirectory(blocked);
            h.Career.SaveTo(CareerSaveSlot.Manual1);
            h.Finish();
            Assert.IsNotEmpty(h.Career.LastError);
            Assert.AreEqual(day, h.Career.Home.WorldDay);
            Assert.AreEqual(ClientScreens.CareerHome, h.Flow.Current);
            Assert.IsFalse(h.Career.IsBusy);
            Directory.Delete(blocked);
            h.Career.SaveTo(CareerSaveSlot.Manual1);
            h.Finish();
            Assert.IsNull(h.Career.LastError);
            Assert.AreEqual(day, h.Career.Home.WorldDay);
            Assert.IsTrue(h.Career.Load.Slots[(int)CareerSaveSlot.Manual1].IsLoadable);
        }

        [Test]
        public void DiskInitializationFailureDoesNotReplaceViews_AndCanBeRetried()
        {
            using var temp = new TempSlots();
            string blocked = Path.Combine(temp.Directory, "blocked-store");
            File.WriteAllText(blocked, "file prevents directory creation");
            Action pending = null;
            using var career = new ClientCareerCoordinator(blocked, RoundResolutionMode.QuickSimAll, work => pending = work);
            Compose(career);
            career.Initialize();
            pending();
            career.Refresh();
            Assert.IsFalse(career.IsInitialized);
            Assert.IsNotEmpty(career.LastError);
            Assert.IsNull(career.Load.Slots);
            File.Delete(blocked);
            career.Initialize();
            pending();
            career.Refresh();
            Assert.IsTrue(career.IsInitialized);
            Assert.IsNull(career.LastError);
        }

        [Test]
        public void SchedulingFailureCanBeRetried_AndDoesNotLeaveBusyStuck()
        {
            using var temp = new TempSlots();
            bool fail = true;
            using var career = new ClientCareerCoordinator(temp.Directory, RoundResolutionMode.QuickSimAll,
                work => { if (fail) throw new InvalidOperationException("worker unavailable"); work(); });
            Compose(career);
            career.Initialize();
            career.Refresh();
            Assert.IsFalse(career.IsBusy);
            Assert.IsFalse(career.IsInitialized);
            Assert.AreEqual("worker unavailable", career.LastError);
            fail = false;
            career.Initialize();
            career.Refresh();
            Assert.IsTrue(career.IsInitialized);
            Assert.IsNull(career.LastError);
        }

        [Test]
        public void StaleCompletionCannotNavigateAfterTheScreenWasChangedOutsideTheCoordinator()
        {
            using var h = new Harness();
            h.Start();
            h.Career.QuitToMainMenu();
            h.Career.ContinueCareer();
            h.Flow.OpenTacticsSetup();
            h.Finish();
            Assert.AreEqual(ClientScreens.TacticsSetup, h.Flow.Current);
            StringAssert.Contains("screen changed", h.Career.LastError);
        }

        [Test]
        public void StaleFailedCompletionPreservesTheOriginalLoadError()
        {
            using var h = new Harness();
            h.Career.ContinueCareer();
            h.Flow.OpenTacticsSetup();
            h.Finish();
            Assert.AreEqual(ClientScreens.TacticsSetup, h.Flow.Current);
            Assert.IsNotEmpty(h.Career.Load.LastError);
            StringAssert.StartsWith(h.Career.Load.LastError, h.Career.LastError);
            StringAssert.Contains("screen changed", h.Career.LastError);
        }

        [Test]
        public void ShutdownWaitsForAcceptedWork_AndDoesNotPublishItsLateCompletion()
        {
            using var temp = new TempSlots();
            Action pending = null;
            var career = new ClientCareerCoordinator(temp.Directory, RoundResolutionMode.QuickSimAll, work => pending = work);
            Compose(career);
            career.Initialize();
            using var entered = new ManualResetEventSlim();
            using var finished = new ManualResetEventSlim();
            var shutdown = new Thread(() => { entered.Set(); career.Dispose(); finished.Set(); });
            shutdown.Start();
            Assert.IsTrue(entered.Wait(TimeSpan.FromSeconds(5)));
            try
            {
                Assert.IsFalse(finished.Wait(TimeSpan.FromMilliseconds(20)), "Shutdown cannot release an accepted writer.");
            }
            finally
            {
                pending();
                Assert.IsTrue(shutdown.Join(TimeSpan.FromSeconds(30)));
            }
            career.Refresh();
            Assert.IsFalse(career.IsInitialized, "Disposed UI never consumes a worker completion.");
            Assert.Throws<ObjectDisposedException>(career.Initialize);
        }

        private static void WaitForWorker(ClientCareerCoordinator career)
        {
            var timeout = Stopwatch.StartNew();
            while (career.IsBusy && timeout.Elapsed < TimeSpan.FromSeconds(30))
            {
                career.Refresh();
                Thread.Yield();
            }
            Assert.IsFalse(career.IsBusy);
        }

        [Test]
        public void ProductionWorkerCompletesOffThread_AndIdlePollingKeepsTheRevisionStable()
        {
            using var temp = new TempSlots();
            using var career = new ClientCareerCoordinator(temp.Directory, RoundResolutionMode.QuickSimAll);
            Compose(career);
            career.Initialize();
            WaitForWorker(career);
            Assert.IsTrue(career.IsInitialized);
            int revision = career.Revision;
            var slots = career.Load.Slots;
            career.Refresh();
            Assert.AreEqual(revision, career.Revision);
            Assert.AreSame(slots, career.Load.Slots);
            career.OpenNewCareer(CareerTestSupport.WORLD_SEED);
            WaitForWorker(career);
            career.SelectClub(CareerTestSupport.MANAGED_CLUB);
            career.StartCareer();
            WaitForWorker(career);
            for (int i = 0; i < 10; i++)
            {
                int previous = career.Revision;
                career.Progress();
                WaitForWorker(career);
                Assert.Greater(career.Revision, previous);
                Assert.IsNull(career.LastError);
            }
            career.Dispose();
            career.Dispose();
            Assert.Throws<ObjectDisposedException>(career.Initialize);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-03 | —      | P7b deterministic worker/UI publication and navigation regression cases. |
// | 1.1     | 2026-11-04 | —      | Retain the underlying load failure when navigation becomes stale. |
// | 1.2     | 2026-11-05 | —      | §5-P8: nine-screen composition; worker-run SubmitTransfer publishes its typed outcome and fresh market/finance snapshots. |
#endregion
