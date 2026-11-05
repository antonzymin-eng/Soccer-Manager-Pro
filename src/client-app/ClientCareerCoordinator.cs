// File:     src/client-app/ClientCareerCoordinator.cs
// Created:  2026-11-03
// Modified: 2026-11-05 (H1: offer terms validated before the worker)
// Author:   —
// Spec:     docs/tracking/interactive-unity-client-design.md §5-P7 (P7b), §5-P8,
//           UI / Client Framework #38 FR-UI-002/006/009/012, Code Standards #20 §12 rule 1
// Purpose:  Serialize career operations off the UI thread; publish immutable cached screen views and
//           apply navigation only when the UI consumes a completed operation.

using System;
using System.Threading;

using Unity.Profiling;

using TacticalDirector.SeasonSave;
using TacticalDirector.Transfers;
using TacticalDirector.UiFramework;

namespace TacticalDirector.ClientApp
{
    /// <summary>
    /// UI-thread coordinator for the independent P7b career route. The worker exclusively owns the
    /// sources, holder and slot store during an operation; registered sources expose only cached
    /// immutable views. Call <see cref="Refresh"/> on the UI thread to accept completion. No Unity API
    /// is called by the worker, and no new career-to-match navigation is introduced (D-P7-6).
    /// </summary>
    public sealed class ClientCareerCoordinator : IViewModelSource<CareerHomeViewModel>,
        IViewModelSource<NewCareerViewModel>, IViewModelSource<LoadCareerViewModel>,
        IViewModelSource<TransferMarketViewModel>, IViewModelSource<ClubFinancesViewModel>, ICommandDispatcher, IDisposable
    {
        private static readonly ProfilerMarker RefreshMarker = new ProfilerMarker("ClientCareerCoordinator.Refresh");
        private readonly string _directory;
        private readonly RoundResolutionMode _mode;
        private readonly Action<Action> _schedule;
        private readonly object _publicationGate = new object();
        private readonly ManualResetEventSlim _idle = new ManualResetEventSlim(true);
        private readonly CareerSessionHolder _holder = new CareerSessionHolder();
        private ClientScreenFlow _flow;
        private CareerSaveSlots _slots;
        private CareerHomeSource _homeSource;
        private NewCareerSetup _setup;
        private LoadCareerSource _loadSource;
        private TransferMarketSource _transferSource;
        private ClubFinancesSource _financesSource;
        private SeasonCommandDispatcher _dispatcher;
        private TransferSubmissionOutcome? _workerTransferOutcome;   // written and read on the worker only
        private Completion _completion;
        private Action _afterSuccess;
        private ScreenId _operationScreen;
        private bool _initialized;
        private bool _disposed;

        private sealed class Completion
        {
            internal bool HasSnapshot;
            internal bool Initialized;
            internal CareerHomeViewModel Home;
            internal NewCareerViewModel NewCareer;
            internal LoadCareerViewModel Load;
            internal TransferMarketViewModel TransferMarket;
            internal ClubFinancesViewModel Finances;
            internal TransferSubmissionOutcome? TransferOutcome;
            internal string Error;
        }

        /// <summary>Creates a coordinator without touching disk; call Initialize after flow composition.</summary>
        public ClientCareerCoordinator(string directory,
            RoundResolutionMode mode = RoundResolutionMode.ManagedThroughEngine)
            : this(directory, mode, Schedule)
        {
        }

        // The production consumer uses the ThreadPool; tests explicitly park/complete the same work
        // to prove that cached UI reads never enter the live sources during an operation.
        internal ClientCareerCoordinator(string directory, RoundResolutionMode mode, Action<Action> schedule)
        {
            if (string.IsNullOrWhiteSpace(directory)) throw new ArgumentException("A career directory is required.", nameof(directory));
            if (!Enum.IsDefined(typeof(RoundResolutionMode), mode)) throw new ArgumentOutOfRangeException(nameof(mode));
            _directory = directory;
            _mode = mode;
            _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        }

        /// <summary>Cached Career Home snapshot; progress is blocked immediately when work is accepted.</summary>
        public CareerHomeViewModel Home { get; private set; }
        /// <summary>Cached club-choice snapshot.</summary>
        public NewCareerViewModel NewCareer { get; private set; }
        /// <summary>Cached save-browser snapshot; no filesystem read occurs in this getter.</summary>
        public LoadCareerViewModel Load { get; private set; }
        /// <summary>Cached Transfers snapshot (§5-P8), refreshed after every career operation.</summary>
        public TransferMarketViewModel TransferMarket { get; private set; }
        /// <summary>Cached Club Finances snapshot (§5-P8), refreshed after every career operation.</summary>
        public ClubFinancesViewModel Finances { get; private set; }
        /// <summary>
        /// The typed #31 outcome of the last completed <see cref="SubmitTransfer"/>; null after any other
        /// operation or move, and when the submission itself failed (then <see cref="LastError"/> says why).
        /// </summary>
        public TransferSubmissionOutcome? LastTransferOutcome { get; private set; }
        /// <summary>True from operation acceptance through UI consumption, including a queued-but-not-started worker.</summary>
        public bool IsBusy { get; private set; }
        /// <summary>True after slot recovery and source creation complete successfully.</summary>
        public bool IsInitialized => _initialized;
        /// <summary>Persistent operation/load error, cleared only after a successful operation.</summary>
        public string LastError { get; private set; }
        /// <summary>UI change counter; idle polling does not allocate or increment it.</summary>
        public int Revision { get; private set; }
        /// <summary>Whether Main Menu can start Continue now.</summary>
        public bool CanContinue => !_disposed && _initialized && !IsBusy &&
            _flow.Current == ClientScreens.MainMenu && Load.CanContinue;
        /// <summary>Whether the selected club can be started now.</summary>
        public bool CanStart => !_disposed && _initialized && !IsBusy &&
            _flow.Current == ClientScreens.NewCareer && NewCareer.HasSelection;

        CareerHomeViewModel IViewModelSource<CareerHomeViewModel>.Project() => Home;
        NewCareerViewModel IViewModelSource<NewCareerViewModel>.Project() => NewCareer;
        LoadCareerViewModel IViewModelSource<LoadCareerViewModel>.Project() => Load;
        TransferMarketViewModel IViewModelSource<TransferMarketViewModel>.Project() => TransferMarket;
        ClubFinancesViewModel IViewModelSource<ClubFinancesViewModel>.Project() => Finances;

        /// <summary>Lifetime-stable Career Home registration; dispatch is queued rather than run on the UI thread.</summary>
        public ScreenRegistration HomeRegistration => new ScreenRegistration(ClientScreens.CareerHome, this, this);
        /// <summary>Lifetime-stable New Career registration over the cached club-choice view.</summary>
        public ScreenRegistration NewRegistration => new ScreenRegistration(ClientScreens.NewCareer, this, null);
        /// <summary>Lifetime-stable Load Career registration over the cached slot view.</summary>
        public ScreenRegistration LoadRegistration => new ScreenRegistration(ClientScreens.LoadCareer, this, null);
        /// <summary>Lifetime-stable Transfers registration over the cached market view; offers go through <see cref="SubmitTransfer"/>.</summary>
        public ScreenRegistration TransfersRegistration => new ScreenRegistration(ClientScreens.Transfers, this, null);
        /// <summary>Lifetime-stable read-only Club Finances registration.</summary>
        public ScreenRegistration FinancesRegistration => new ScreenRegistration(ClientScreens.ClubFinances, this, null);

        internal void AttachFlow(ClientScreenFlow flow)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ClientCareerCoordinator));
            if (_flow != null) throw new InvalidOperationException("Career coordinator already belongs to a shell.");
            if (flow == null || !flow.HasTransferScreens) throw new ArgumentException("Nine registered screens are required (§5-P8).", nameof(flow));
            _flow = flow;
        }

        /// <summary>Recovers the slot store and scans saves off-thread; may be retried after a failed initialization.</summary>
        public void Initialize()
        {
            RequireScreen(ClientScreens.MainMenu, false);
            if (_initialized) throw new InvalidOperationException("Career storage is already initialized.");
            Begin(() =>
            {
                var slots = new CareerSaveSlots(_directory);
                var home = new CareerHomeSource(_holder);
                var setup = new NewCareerSetup(_holder, slots, _mode);
                var load = new LoadCareerSource(_holder, slots, _mode);
                var transfers = new TransferMarketSource(_holder);
                var finances = new ClubFinancesSource(_holder);
                _transferSource = transfers;
                _financesSource = finances;
                _slots = slots;
                _homeSource = home;
                _setup = setup;
                _loadSource = load;
                _dispatcher = new SeasonCommandDispatcher(_holder);
                return true;
            });
        }

        /// <summary>Opens the club-choice form and generates its preview off-thread using host-supplied entropy.</summary>
        public void OpenNewCareer(ulong seed)
        {
            RequireScreen(ClientScreens.MainMenu);
            _flow.OpenNewCareer();
            NewCareer = default;
            Begin(() => { _setup.Prepare(seed); return true; });
        }

        /// <summary>Selects a club in the completed preview; no live season state is read.</summary>
        public void SelectClub(int clubId)
        {
            RequireScreen(ClientScreens.NewCareer);
            _setup.Select(clubId);
            NewCareer = _setup.Project();
            Revision++;
        }

        /// <summary>Creates/autosaves the chosen career off-thread; navigates only after successful UI publication.</summary>
        public void StartCareer()
        {
            RequireScreen(ClientScreens.NewCareer);
            if (!NewCareer.HasSelection) throw new InvalidOperationException("Select a club before starting.");
            Begin(() => { _setup.Start(); _loadSource.Refresh(); return true; }, _flow.StartCareer);
        }

        /// <summary>Discards the form's navigation; the next entry prepares a fresh preview.</summary>
        public void CancelNewCareer()
        {
            RequireScreen(ClientScreens.NewCareer);
            _flow.CancelNewCareer();
            Revision++;
        }

        /// <summary>Opens the picker and refreshes its slot states off-thread.</summary>
        public void OpenLoadCareer()
        {
            RequireScreen(ClientScreens.MainMenu);
            _flow.OpenLoadCareer();
            Begin(() => { _loadSource.Refresh(); return true; });
        }

        /// <summary>Returns from the idle picker to Main Menu.</summary>
        public void CancelLoadCareer()
        {
            RequireScreen(ClientScreens.LoadCareer);
            _flow.CancelLoadCareer();
            Revision++;
        }

        /// <summary>Loads one slot; failure keeps the current career and picker with a persistent reason.</summary>
        public void LoadCareer(CareerSaveSlot slot)
        {
            RequireScreen(ClientScreens.LoadCareer);
            if (!Enum.IsDefined(typeof(CareerSaveSlot), slot)) throw new ArgumentOutOfRangeException(nameof(slot));
            Begin(() => _loadSource.TryLoad(slot), _flow.EnterLoadedCareer);
        }

        /// <summary>Loads the latest loadable slot directly; does not route through the picker.</summary>
        public void ContinueCareer()
        {
            RequireScreen(ClientScreens.MainMenu);
            Begin(() => _loadSource.TryContinue(), _flow.ContinueCareer);
        }

        /// <summary>Queues the one legal progress action shown by the cached Home projection.</summary>
        public void Progress()
        {
            RequireScreen(ClientScreens.CareerHome);
            ManagerIntent intent = CareerProgress.IntentFor(Home.NextAction);
            Dispatch(in intent);
        }

        /// <inheritdoc />
        public void Dispatch(in ManagerIntent intent)
        {
            RequireScreen(ClientScreens.CareerHome);
            if (!IntentRouting.IsSeasonIntent(intent.Kind) || CareerProgress.ActionFor(intent.Kind) != Home.NextAction)
                throw new InvalidOperationException("Only the currently offered season action may be dispatched.");
            ManagerIntent accepted = intent;
            Begin(() => { _dispatcher.Dispatch(in accepted); _loadSource.Refresh(); return true; });
        }

        /// <summary>Writes one manual slot off-thread; an autosave write is never exposed as a manual request.</summary>
        public void SaveTo(CareerSaveSlot slot)
        {
            RequireScreen(ClientScreens.CareerHome);
            if (!Enum.IsDefined(typeof(CareerSaveSlot), slot) || slot == CareerSaveSlot.Autosave)
                throw new ArgumentOutOfRangeException(nameof(slot), "Choose a manual slot.");
            Begin(() => { _holder.Require().SaveTo(slot); _loadSource.Refresh(); return true; });
        }

        /// <summary>Career Home → Transfers; the market snapshot is already current (refreshed after every operation).</summary>
        public void OpenTransfers()
        {
            RequireScreen(ClientScreens.CareerHome);
            _flow.OpenTransfers();
            LastTransferOutcome = null;
            Revision++;
        }

        /// <summary>Transfers → Career Home; nothing is pending, so leaving abandons nothing.</summary>
        public void CloseTransfers()
        {
            RequireScreen(ClientScreens.Transfers);
            _flow.CloseTransfers();
            LastTransferOutcome = null;
            Revision++;
        }

        /// <summary>Career Home → the read-only Club Finances screen.</summary>
        public void OpenClubFinances()
        {
            RequireScreen(ClientScreens.CareerHome);
            _flow.OpenClubFinances();
            Revision++;
        }

        /// <summary>Club Finances → Career Home.</summary>
        public void CloseClubFinances()
        {
            RequireScreen(ClientScreens.ClubFinances);
            _flow.CloseClubFinances();
            Revision++;
        }

        /// <summary>
        /// Submits one buy or sell offer off-thread through <see cref="CareerSession.SubmitTransfer"/> (§5-P8).
        /// The typed outcome arrives in <see cref="LastTransferOutcome"/> on the next <see cref="Refresh"/>;
        /// it is not autosaved (D-P8-2). Terms are checked on the UI thread before any work is queued: fee and
        /// wage non-negative, wage at most <see cref="ClientAppConstants.MAX_OFFER_WAGE_PER_PERIOD"/>, length at
        /// least one season. #31 bounds no wage, and an unbounded one committed by #31 would reach every finance
        /// projection (H1), so out-of-range terms never reach #31.
        /// </summary>
        /// <exception cref="InvalidOperationException">Not on Transfers, busy, the cached window is closed, or the
        /// offer's terms are out of range (the UI must prevent all of these; nothing is queued).</exception>
        public void SubmitTransfer(in Offer offer)
        {
            RequireScreen(ClientScreens.Transfers);
            if (!TransferMarket.IsWindowOpen) throw new InvalidOperationException("The transfer window is closed.");
            RequireOfferTerms(in offer);
            Offer accepted = offer;
            LastTransferOutcome = null;
            Begin(() => { _workerTransferOutcome = _holder.Require().SubmitTransfer(in accepted); return true; });
        }

        private static void RequireOfferTerms(in Offer offer)
        {
            if (offer.Fee < 0) throw new InvalidOperationException("The offer fee cannot be negative.");
            if (offer.WagePerPeriod < 0) throw new InvalidOperationException("The offered wage cannot be negative.");
            if (offer.WagePerPeriod > ClientAppConstants.MAX_OFFER_WAGE_PER_PERIOD)
                throw new InvalidOperationException("The offered wage cannot exceed "
                    + ClientAppConstants.MAX_OFFER_WAGE_PER_PERIOD + " per period.");
            if (offer.LengthSeasons < 1) throw new InvalidOperationException("The contract must last at least one season.");
        }

        /// <summary>Quits the idle career context; saving is an explicit action, never implied by this move.</summary>
        public void QuitToMainMenu()
        {
            RequireScreen(ClientScreens.CareerHome);
            _flow.QuitToMainMenu();
            _holder.Clear();
            Home = default;
            TransferMarket = default;
            Finances = default;
            LastTransferOutcome = null;
            Revision++;
        }

        private void RequireScreen(ScreenId screen, bool requireInitialized = true)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ClientCareerCoordinator));
            if (_flow == null) throw new InvalidOperationException("Compose the career coordinator into a shell first.");
            if (IsBusy) throw new InvalidOperationException("A career operation is already running.");
            if (requireInitialized && !_initialized) throw new InvalidOperationException("Career storage is not ready.");
            if (_flow.Current != screen) throw new InvalidOperationException("This career action is not available on the current screen.");
        }

        private void Begin(Func<bool> work, Action afterSuccess = null)
        {
            IsBusy = true;
            if (Home.HasCareer) Home = Home.WithProgressBlocked(true);
            _operationScreen = _flow.Current;
            _afterSuccess = afterSuccess;
            Revision++;
            lock (_publicationGate) _idle.Reset();
            try
            {
                _schedule(() => Complete(work));
            }
            catch (Exception exception)
            {
                Publish(new Completion { Error = exception.Message });
            }
        }

        private void Complete(Func<bool> work)
        {
            var result = new Completion();
            _workerTransferOutcome = null;
            try
            {
                if (!work()) result.Error = _loadSource.Project().LastError ?? "The career operation did not complete.";
            }
            catch (Exception exception)
            {
                result.Error = exception.Message;
            }
            try
            {
                // Sources are read only by the worker after all mutation has stopped.
                if (_slots != null)
                {
                    result.Home = _homeSource.Project();
                    result.NewCareer = _setup.Project();
                    result.Load = _loadSource.Project();
                    result.TransferMarket = _transferSource.Project();
                    result.Finances = _financesSource.Project();
                    result.TransferOutcome = _workerTransferOutcome;
                    result.Initialized = true;
                    result.HasSnapshot = true;
                }
            }
            catch (Exception exception)
            {
                result.Error = exception.Message;
            }
            finally
            {
                Publish(result);
            }
        }

        private void Publish(Completion completion)
        {
            // Reset/Dispose cannot overlap Set: observing IsSet alone does not prove Set returned.
            // This gate covers only publication, never disk, simulation, or UI work.
            lock (_publicationGate)
            {
                Volatile.Write(ref _completion, completion);
                _idle.Set();
            }
        }

        /// <summary>Accepts a finished immutable snapshot on the UI thread, then performs its named navigation.</summary>
        public void Refresh()
        {
            using (RefreshMarker.Auto())
            {
                if (_disposed || !IsBusy || !_idle.IsSet) return;
                Completion result = Interlocked.Exchange(ref _completion, null);
                if (result == null) return;
                if (result.HasSnapshot)
                {
                    Home = result.Home;
                    NewCareer = result.NewCareer;
                    Load = result.Load;
                    TransferMarket = result.TransferMarket;
                    Finances = result.Finances;
                    LastTransferOutcome = result.TransferOutcome;
                    _initialized = result.Initialized;
                }
                else if (Home.HasCareer) Home = Home.WithProgressBlocked(false);
                LastError = result.Error;
                IsBusy = false;
                Action navigation = _afterSuccess;
                _afterSuccess = null;
                Revision++;
                if (_flow.Current != _operationScreen)
                {
                    const string staleScreen = "Career operation completed after its screen changed; navigation was refused.";
                    LastError = LastError == null ? staleScreen : LastError + " " + staleScreen;
                    return;
                }
                if (LastError == null) navigation?.Invoke();
            }
        }

        private static void Schedule(Action work)
        {
            if (!ThreadPool.QueueUserWorkItem(_ => work()))
                throw new InvalidOperationException("The career worker could not be scheduled.");
        }

        /// <summary>
        /// Waits for accepted work before releasing the owner. Shutdown may wait for synchronous #30
        /// resolution; it never aborts a writer or permits a later UI callback after disposal.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            _idle.Wait();
            lock (_publicationGate)
            {
                _completion = null;
                _afterSuccess = null;
                _holder.Clear();
                _idle.Dispose();
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-11-03 | —      | P7b worker ownership, immutable publication and guarded career navigation. |
// | 1.1     | 2026-11-04 | —      | Preserve the operation error when stale-screen navigation is refused. |
// | 1.2     | 2026-11-05 | —      | §5-P8: Transfers / Club Finances registrations, worker snapshots, four moves and SubmitTransfer (typed outcome, no autosave); flow must carry nine screens. |
// | 1.3     | 2026-11-05 | —      | H1: SubmitTransfer refuses out-of-range terms (fee/wage < 0, wage > [CROSS] MAX_OFFER_WAGE_PER_PERIOD, length < 1) before Begin; nothing reaches the worker. |
#endregion
