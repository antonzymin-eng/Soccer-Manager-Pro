// File:     src/client-app/CareerScreenPresenter.cs
// Created:  2026-11-06
// Modified: 2026-11-06
// Author:   —
// Spec:     Interactive Unity client §5-P7, UI / Client Framework #38, Code Standards #20
// Purpose:  Serialize career UI work on a worker and publish immutable snapshots on the client thread.
//           Navigation and feedback never inspect the non-thread-safe sources while work is pending.

using System;
using System.Threading;

using Unity.Profiling;

using TacticalDirector.SeasonSave;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>Client-thread career actions and cached screen sources; one worker owns domain access at a time.</summary>
    public sealed class CareerScreenPresenter : IDisposable,
        IViewModelSource<CareerHomeViewModel>, IViewModelSource<NewCareerViewModel>,
        IViewModelSource<LoadCareerViewModel>, ICommandDispatcher
    {
        private static readonly ProfilerMarker RefreshMarker = new ProfilerMarker("CareerScreenPresenter.Refresh");
        private readonly CareerSessionHolder _holder = new CareerSessionHolder();
        private readonly string _directory;
        private readonly RoundResolutionMode _mode;
        private readonly Action<Action> _schedule;
        private readonly Action _beforeSnapshot;
        private readonly object _handoff = new object();
        private CareerSaveSlots _slots;
        private CareerHomeSource _home;
        private NewCareerSetup _new;
        private LoadCareerSource _load;
        private SeasonCommandDispatcher _dispatcher;
        private ClientScreenFlow _flow;
        private Completion _completed;
        private Action _after;
        private bool _disposed;
        private bool _workerRunning;
        private bool _hasUnsavedProgress;

        private sealed class Completion
        {
            internal CareerHomeViewModel Home;
            internal NewCareerViewModel New;
            internal LoadCareerViewModel Load;
            internal string Error;
            internal bool Succeeded;
            internal bool HasSnapshots;
            internal bool HasSnapshotWarning;
            internal bool HasUnsavedProgress;
        }

        /// <summary>Constructs a binding with a background worker; initialization is explicitly requested after flow attachment.</summary>
        public CareerScreenPresenter(string directory) : this(directory, RoundResolutionMode.ManagedThroughEngine,
            action => ThreadPool.QueueUserWorkItem(_ => action())) { }

        internal CareerScreenPresenter(string directory, RoundResolutionMode mode, Action<Action> schedule, Action beforeSnapshot = null)
        {
            _directory = directory ?? throw new ArgumentNullException(nameof(directory));
            _mode = mode;
            _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
            _beforeSnapshot = beforeSnapshot;
        }

        /// <summary>The last complete Career Home snapshot, safe to render while a worker runs.</summary>
        public CareerHomeViewModel Home { get; private set; }
        /// <summary>The last complete New Career snapshot.</summary>
        public NewCareerViewModel New { get; private set; }
        /// <summary>The last complete slot snapshot, also used by Main Menu.</summary>
        public LoadCareerViewModel Load { get; private set; }
        /// <summary>True from accepted action until client-thread completion consumption.</summary>
        public bool IsBusy { get; private set; }
        /// <summary>True once the slot store and sources have been opened successfully.</summary>
        public bool IsReady { get; private set; }
        /// <summary>Developer diagnostic only; player feedback uses localized generic roles. Cleared by a new operation.</summary>
        public string Error { get; private set; }
        /// <summary>True when screen-data refresh failed; refresh before another progress or save action.</summary>
        public bool HasSnapshotWarning { get; private set; }
        /// <summary>Whether the last worker action committed successfully, independent of projection refresh.</summary>
        public bool LastOperationSucceeded { get; private set; }
        /// <summary>True when the active career has progress not secured by a successful save.</summary>
        public bool HasUnsavedProgress { get; private set; }
        /// <summary>True while the player is deciding whether to discard unsaved progress.</summary>
        public bool PendingQuit { get; private set; }
        /// <summary>The confirmed manual save, retained until another operation.</summary>
        public CareerSaveSlot? SavedSlot { get; private set; }
        /// <summary>An occupied manual slot awaiting explicit overwrite confirmation.</summary>
        public CareerSaveSlot? PendingOverwrite { get; private set; }
        /// <summary>Presentation revision; unchanged refreshes do not rebuild text or layout.</summary>
        public int Revision { get; private set; }

        CareerHomeViewModel IViewModelSource<CareerHomeViewModel>.Project() => Home;
        NewCareerViewModel IViewModelSource<NewCareerViewModel>.Project() => New;
        LoadCareerViewModel IViewModelSource<LoadCareerViewModel>.Project() => Load;

        /// <summary>Attaches the one seven-screen graph before initialization.</summary>
        public void AttachFlow(ClientScreenFlow flow)
        {
            if (_flow != null || flow == null || !flow.HasCareerScreens)
                throw new InvalidOperationException("A career presenter requires one seven-screen flow.");
            _flow = flow;
        }

        /// <summary>Opens or retries the store off the client thread.</summary>
        public void Initialize()
        {
            RequireScreen(ClientScreens.MainMenu, false);
            Begin(() =>
            {
                _slots = new CareerSaveSlots(_directory);
                _home = new CareerHomeSource(_holder);
                _new = new NewCareerSetup(_holder, _slots, _mode);
                _load = new LoadCareerSource(_holder, _slots, _mode);
                _dispatcher = new SeasonCommandDispatcher(_holder);
                return true;
            }, () => IsReady = true);
        }

        /// <summary>Opens New Career and prepares a generated, non-editable seed on the worker.</summary>
        public void OpenNew(ulong worldSeed)
        {
            RequireScreen(ClientScreens.MainMenu);
            _flow.OpenNewCareer();
            New = default;
            Begin(() => { _new.Prepare(worldSeed); return true; });
        }

        /// <summary>Selects a club from the prepared snapshot.</summary>
        public void SelectClub(int clubId)
        {
            RequireScreen(ClientScreens.NewCareer);
            _new.Select(clubId);
            New = _new.Project();
            Revision++;
        }

        /// <summary>Starts and autosaves a selected career before replacing the form with Career Home.</summary>
        public void StartCareer()
        {
            RequireScreen(ClientScreens.NewCareer);
            Begin(() => { _new.Start(); _hasUnsavedProgress = _holder.Require().LastAutosaveError != null; return true; }, _flow.StartCareer);
        }

        /// <summary>Opens the picker immediately, then refreshes its slot states off-thread.</summary>
        public void OpenLoad()
        {
            RequireScreen(ClientScreens.MainMenu);
            _flow.OpenLoadCareer();
            RefreshSlots();
        }

        /// <summary>Refreshes metadata without discarding the current career.</summary>
        public void RefreshSlots()
        {
            RequireIdle();
            Begin(() => { _load.Refresh(); return true; });
        }

        /// <summary>Resumes the latest ready save directly from Main Menu.</summary>
        public void ContinueCareer()
        {
            RequireScreen(ClientScreens.MainMenu);
            Begin(() => { bool loaded = _load.TryContinue(); if (loaded) _hasUnsavedProgress = false; return loaded; }, _flow.ContinueCareer);
        }

        /// <summary>Loads a selected ready slot; failure keeps the picker and current career intact.</summary>
        public void LoadSlot(CareerSaveSlot slot)
        {
            RequireScreen(ClientScreens.LoadCareer);
            Begin(() => { bool loaded = _load.TryLoad(slot); if (loaded) _hasUnsavedProgress = false; return loaded; }, _flow.EnterLoadedCareer);
        }

        /// <summary>Dispatches the one legal progress action through the existing season dispatcher.</summary>
        public void Progress()
        {
            RequireScreen(ClientScreens.CareerHome);
            RequireCurrentSnapshot();
            ManagerIntent intent;
            switch (Home.NextAction)
            {
                case CareerProgressAction.AdvanceDay: intent = ManagerIntent.AdvanceDay(); break;
                case CareerProgressAction.PlayMatchday: intent = ManagerIntent.AdvanceRound(); break;
                case CareerProgressAction.StartNextSeason: intent = ManagerIntent.StartNextSeason(); break;
                default: throw new InvalidOperationException("No career progress action is available.");
            }
            Dispatch(in intent);
        }

        /// <inheritdoc />
        public void Dispatch(in ManagerIntent intent)
        {
            RequireScreen(ClientScreens.CareerHome);
            if (!IntentRouting.IsSeasonIntent(intent.Kind))
                throw new InvalidOperationException("The career dispatcher accepts only season intents.");
            RequireCurrentSnapshot();
            ManagerIntent command = intent;
            Begin(() =>
            {
                _hasUnsavedProgress = true;
                _dispatcher.Dispatch(in command);
                if (command.Kind != IntentKind.AdvanceDay)
                    _hasUnsavedProgress = _holder.Require().LastAutosaveError != null;
                return true;
            });
        }

        /// <summary>Requests a manual save, asking before replacing any occupied or unreadable slot.</summary>
        public void RequestSave(CareerSaveSlot slot)
        {
            RequireScreen(ClientScreens.CareerHome);
            RequireCurrentSnapshot();
            if (slot < CareerSaveSlot.Manual1 || slot > CareerSaveSlot.Manual3)
                throw new ArgumentOutOfRangeException(nameof(slot));
            if (Load.Slots[(int)slot].State != CareerSlotState.Empty)
            {
                PendingQuit = false;
                PendingOverwrite = slot;
                Revision++;
                return;
            }
            Save(slot);
        }

        /// <summary>Confirms the displayed slot replacement.</summary>
        public void ConfirmOverwrite()
        {
            RequireScreen(ClientScreens.CareerHome);
            if (!PendingOverwrite.HasValue) throw new InvalidOperationException("No overwrite is pending.");
            Save(PendingOverwrite.Value);
        }

        /// <summary>Closes overwrite confirmation without writing.</summary>
        public void CancelOverwrite() { RequireIdle(); PendingOverwrite = null; Revision++; }

        private void Save(CareerSaveSlot slot) => Begin(() =>
        {
            _holder.Require().SaveTo(slot);
            _hasUnsavedProgress = false;
            return true;
        }, () => SavedSlot = slot);

        /// <summary>Cancels a form, or quits the career with a named, guarded destination.</summary>
        public void Back()
        {
            RequireIdle();
            if (_flow.Current == ClientScreens.NewCareer) _flow.CancelNewCareer();
            else if (_flow.Current == ClientScreens.LoadCareer) _flow.CancelLoadCareer();
            else if (_flow.Current == ClientScreens.CareerHome)
            {
                if (HasUnsavedProgress) PendingQuit = true;
                else QuitCareer();
            }
            else throw new InvalidOperationException("No career Back action here.");
            PendingOverwrite = null;
            Revision++;
        }

        /// <summary>Confirms discarding the explicitly disclosed unsaved progress.</summary>
        public void ConfirmQuit()
        {
            RequireScreen(ClientScreens.CareerHome);
            if (!PendingQuit) throw new InvalidOperationException("No discard confirmation is pending.");
            QuitCareer();
            Revision++;
        }

        /// <summary>Dismisses discard confirmation and retains the active career.</summary>
        public void CancelQuit() { RequireIdle(); PendingQuit = false; Revision++; }

        private void QuitCareer()
        {
            _flow.QuitToMainMenu();
            _holder.Clear();
            Home = default;
            HasUnsavedProgress = _hasUnsavedProgress = false;
            PendingQuit = false;
        }

        /// <summary>Records a synchronous action failure for persistent, localized player feedback.</summary>
        public void ReportActionFailure(Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));
            if (_disposed) return;
            Error = exception.Message;
            Revision++;
        }

        private void Begin(Func<bool> work, Action after = null)
        {
            RequireIdle(false);
            IsBusy = true;
            Error = null;
            PendingQuit = false;
            SavedSlot = null;
            PendingOverwrite = null;
            Home = Home.WithProgressBlocked(true);
            _after = after;
            Revision++;
            try
            {
                // Presentation scheduling only: no Unity objects, navigation, or UI-thread source reads.
                _schedule(() =>
                {
                    lock (_handoff)
                    {
                        if (_disposed) return;
                        _workerRunning = true;
                    }
                    var result = new Completion();
                    try
                    {
                        try { result.Succeeded = work(); }
                        catch (Exception e) { result.Error = e.Message; }
                        try
                        {
                            _beforeSnapshot?.Invoke();
                            _load?.RefreshAfterOperation(result.Succeeded);
                            result.Home = _home?.Project() ?? default;
                            result.New = _new?.Project() ?? default;
                            result.Load = _load?.Project() ?? default;
                            result.HasSnapshots = true;
                            if (!result.Succeeded && result.Error == null) result.Error = result.Load.LastError;
                        }
                        catch (Exception e)
                        {
                            result.Error = result.Error ?? e.Message;
                            result.HasSnapshotWarning = true;
                        }
                        result.HasUnsavedProgress = _hasUnsavedProgress;
                    }
                    finally
                    {
                        lock (_handoff)
                        {
                            _completed = result;
                            _workerRunning = false;
                            Monitor.PulseAll(_handoff);
                        }
                    }
                });
            }
            catch (Exception e)
            {
                IsBusy = false;
                Home = Home.WithProgressBlocked(false);
                Error = e.Message;
                _after = null;
                Revision++;
            }
        }

        /// <summary>Consumes a finished handoff; no blocking wait, source read, or navigation on the worker.</summary>
        public void Refresh()
        {
            using (RefreshMarker.Auto())
            {
                if (_disposed || !IsBusy) return;
                Completion result;
                lock (_handoff) { result = _completed; _completed = null; }
                if (result == null) return;
                if (result.HasSnapshots)
                {
                    Home = result.Home;
                    New = result.New;
                    Load = result.Load;
                }
                else Home = Home.WithProgressBlocked(false);
                Error = result.Error;
                HasSnapshotWarning = result.HasSnapshotWarning;
                LastOperationSucceeded = result.Succeeded;
                HasUnsavedProgress = result.HasUnsavedProgress;
                IsBusy = false;
                Action after = _after;
                _after = null;
                if (result.Succeeded) after?.Invoke();
                Revision++;
            }
        }

        private void RequireCurrentSnapshot()
        {
            if (HasSnapshotWarning) throw new InvalidOperationException("Refresh the career screen before another progress or save action.");
        }

        private void RequireIdle(bool requireReady = true)
        {
            if (_disposed || IsBusy || _flow == null || (requireReady && !IsReady))
                throw new InvalidOperationException("The career UI is busy, unavailable, or disposed.");
        }

        private void RequireScreen(ScreenId screen, bool requireReady = true)
        {
            RequireIdle(requireReady);
            if (_flow.Current != screen) throw new InvalidOperationException("Career action is not available on this screen.");
        }

        /// <summary>Cancels queued work and quiesces running work before a replacement shell can own the store or engine.</summary>
        /// <remarks>Only teardown waits; normal Refresh never waits for an operation to finish.</remarks>
        public void Dispose()
        {
            lock (_handoff)
            {
                _disposed = true;
                _after = null;
                while (_workerRunning) Monitor.Wait(_handoff);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-06 | —      | P7b single-worker actions, cached projections, errors and overwrite confirmation. |
// | 1.1     | 2026-11-06 | —      | Separate committed work from projection failure; protect stale views and confirm dirty quit. |
#endregion
