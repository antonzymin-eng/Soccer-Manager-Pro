// File:     src/match-viewer/LiveMatchStreamer.cs
// Created:  2026-07-15
// Modified: 2026-07-27 (interactive Unity client §5-P1: captures the per-agent cues / substitution
//           counts / derived period, and latches the engine's within-tick restart cue — KD-P1-3,
//           held in a single RestartBanner after AR-1 M-6)
// Modified: 2026-08-03 (P4a: CaptureFrame samples the live goalkeeper flag into LiveAgentCue)
// Modified: 2026-08-04 (P4a AR pass: shirt numbers come from the shared RosterShirtNumbers rule)
// Modified: 2026-08-15 (P4b AR pass M-5: + EffectiveTicksPerSecond, the pacing loop's own
//           ticksPerSecond × SpeedMultiplier product, so a consumer's interpolation alpha reads the
//           streamer's actual pacing rate instead of reimplementing it against the DEFAULT tick rate)
// Author:   —
// Spec:     Interactive match view (docs/tracking/interactive-match-view-design.md) +
//           interactive Unity client (docs/tracking/interactive-unity-client-design.md §4/§5-P0/§6),
//           Code Standards #20
// Purpose:  Paces a real MatchEngine at wall-clock speed on a background thread, observing it
//           between ticks exactly like MatchReplayRecorder (never mutates state beyond calling
//           RunTick()), and exposes the latest captured frame through a lock-protected surface for
//           LiveMatchServer to serve. Presentation tooling — not the 60 Hz simulation hot path, so
//           the zero-allocation / no-Thread / no-try-catch game-loop rules do not apply here (same
//           carve-out MatchReplayRecorder / HtmlReplayExporter already use). Optionally carries a
//           single sim-thread pre-tick hook (the interactive Unity client's command-drain / save
//           servicing seam) — the browser viewer installs none, keeping its playback-only invariant.

using System;
using System.Diagnostics;
using System.Threading;

using UnityEngine;

using TacticalDirector.DeterministicSim;
using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchViewer
{
    /// <summary>
    /// Owns a <see cref="MatchEngine.MatchEngine"/> and is the ONLY thing that ever calls its
    /// <c>RunTick()</c> — no other type in this assembly (in particular, <see cref="LiveMatchServer"/>)
    /// holds a reference to the engine at all, so the "thing that can mutate the match" and "the
    /// thing an HTTP request can reach" are disjoint by construction, not just by convention.
    /// Single-use: once <see cref="Stop"/> has been called, <see cref="Start"/> throws.
    /// </summary>
    public sealed class LiveMatchStreamer
    {
        private enum LifecycleState { NotStarted, Running, Stopped }

        private readonly MatchEngine.MatchEngine _engine;
        private readonly int _ticksPerSecond;
        private readonly object _lock = new object();

        // Serializes a tick (RunTick + capture) against an off-tick ServiceOnce() call so the
        // optional pre-tick hook — which may mutate the engine — never interleaves with a tick.
        // Distinct from _lock (the brief frame-handoff lock) to keep frame reads non-blocking.
        // Lock order is always _tickGate then _lock, never the reverse.
        private readonly object _tickGate = new object();
        private readonly int[]  _teamIds;
        private readonly bool[] _isGoalkeeper;
        private readonly int[]  _shirtNumbers;

        // Optional sim-thread hook run at the top of every tick, ahead of RunTick() (and by
        // ServiceOnce() off-tick). Null for the browser viewer, which supplies none — so its
        // playback-only / disjoint-by-construction invariant is preserved by construction, not
        // convention. Volatile: written once under _lock before Start(), read under _tickGate
        // (which may be a different thread via ServiceOnce()).
        private volatile Action _preTickHook;

        // The post-tick observer (see SetPostTickObserver). Distinct from the pre-tick hook because
        // it must fire once per COMPLETED tick and never on the off-tick ServiceOnce() path.
        private volatile Action _postTickObserver;

        // Latched cause of the observer's first failure; see PostTickObserverFault.
        private volatile Exception _postTickObserverFault;

        private LifecycleState _state = LifecycleState.NotStarted;
        private Thread _thread;
        private volatile bool _stopRequested;

        // P1 KD-P1-3 — the latched last restart. The engine reports a restart only for the tick it was
        // applied on (so it stays out of the snapshot entirely); this layer, which sees every tick, holds
        // the cross-tick memory a HUD needs. Written and read only inside CaptureFrame, itself only ever
        // called under _tickGate, so this needs no separate synchronisation; it reaches other threads
        // solely as an immutable copy inside a published LiveMatchFrame. No initializer is needed and
        // that is the point: default(RestartBanner) IS RestartBanner.None (AR-1 M-3).
        private RestartBanner _lastRestart;

        private LiveMatchFrame? _latestFrame;
        private bool _paused;
        private bool _autoPaused;
        private float _speedMultiplier = 1f;
        private bool _pacingEpochDirty;

        /// <summary>
        /// Wraps <paramref name="engine"/> (already constructed/configured by the caller — pre-
        /// kickoff, same convention as <c>MatchReplayRecorder</c>'s pre-configured-engine overload).
        /// </summary>
        /// <param name="engine">The engine to tick and observe. Must not be null.</param>
        /// <param name="ticksPerSecond">Target real-time tick rate at 1× speed (&gt; 0).</param>
        public LiveMatchStreamer(MatchEngine.MatchEngine engine, int ticksPerSecond = DeterministicSimConstants.PHYSICS_TICK_HZ)
        {
            if (engine == null) { throw new ArgumentNullException(nameof(engine)); }
            if (ticksPerSecond <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(ticksPerSecond), ticksPerSecond, "ticksPerSecond must be > 0.");
            }

            _engine         = engine;
            _ticksPerSecond = ticksPerSecond;

            // Captured once here so LiveMatchServer can render roster cues without ever holding a
            // MatchEngine reference itself (§9.1 of the design note: the server and the engine must
            // stay disjoint by construction). Team ids never change across a match — a bench player
            // belongs to the team whose bench they sit on. The goalkeeper flags do NOT share that
            // property (a substitution rewrites the slot's flag), so this copy is a boot-time
            // snapshot only and the live flag rides LiveAgentCue.IsGoalkeeper on each frame.
            _teamIds      = new int[MatchEngineConstants.SQUAD_SIZE];
            _isGoalkeeper = new bool[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _teamIds[i]      = engine.AgentTeamId(i);
                _isGoalkeeper[i] = engine.AgentIsGoalkeeper(i);
            }

            // Shirt numbers are slot ordinals derived from the team ids, so they are boot-constant
            // for the same reason team ids are — the number belongs to the slot, and a substitution
            // hands it to whoever fills that slot.
            _shirtNumbers = RosterShirtNumbers.Assign(_teamIds);
        }

        /// <summary>Number of agents per frame (mirrors <c>MatchEngineConstants.SQUAD_SIZE</c>).</summary>
        public int AgentCount => _teamIds.Length;

        /// <summary>Pitch length (goal-to-goal, X), metres — exposed so <see cref="LiveMatchServer"/> never needs its own <c>MatchEngine</c> reference.</summary>
        public float PitchLengthM => MatchEngineConstants.PITCH_LENGTH_M;

        /// <summary>Pitch width (touchline-to-touchline, Y), metres — exposed so <see cref="LiveMatchServer"/> never needs its own <c>MatchEngine</c> reference.</summary>
        public float PitchWidthM => MatchEngineConstants.PITCH_WIDTH_M;

        /// <summary>Team id (0 = home, 1 = away) of roster <paramref name="index"/>.</summary>
        public int TeamId(int index)
        {
            GuardRosterIndex(index);
            return _teamIds[index];
        }

        /// <summary>
        /// True when roster <paramref name="index"/> started the match as a goalkeeper.
        ///
        /// <para><b>Boot-time only.</b> Substituting a keeper for an outfield player (or the
        /// reverse) moves which slot is the goalkeeper, and this array is not re-sampled. Use
        /// <c>LiveAgentCue.IsGoalkeeper</c> from the current frame for the live answer; this
        /// accessor exists for callers that need roster metadata before the first frame is
        /// captured.</para>
        /// </summary>
        public bool IsGoalkeeper(int index)
        {
            GuardRosterIndex(index);
            return _isGoalkeeper[index];
        }

        /// <summary>
        /// Shirt number drawn on roster <paramref name="index"/>'s marker — 1-based within its team,
        /// per <see cref="RosterShirtNumbers"/>, which is the one implementation of that rule.
        /// </summary>
        public int ShirtNumber(int index)
        {
            GuardRosterIndex(index);
            return _shirtNumbers[index];
        }

        private static void GuardRosterIndex(int index)
        {
            if (index < 0 || index >= MatchEngineConstants.SQUAD_SIZE)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, "index must be a roster index in [0, AgentCount).");
            }
        }

        /// <summary>
        /// Runs exactly one engine tick and captures the resulting frame into the lock-protected
        /// "latest frame" surface. Pure and synchronous — no threading, no wall-clock timing — so
        /// tests can drive a deterministic tick sequence directly. Internal contract: callers must
        /// not invoke this concurrently with a running pacing loop (started via <see cref="Start"/>)
        /// and must not invoke it after <see cref="Stop"/> — the only sanctioned callers are the
        /// pacing loop itself and test code exercising the streamer before <see cref="Start"/>.
        /// </summary>
        internal LiveMatchFrame TickOnce()
        {
            lock (_tickGate)
            {
                // Pre-tick hook (if any) runs on the sim thread at the top of the tick, ahead of the
                // AI phase, so a command it applies takes effect this tick (§6.2 of the Unity-client
                // note). Firing it inside TickOnce() — not the pacing-loop wrapper — is what lets the
                // head-less command-drain test exercise the same drain the live loop does.
                _preTickHook?.Invoke();
                _engine.RunTick();

                // Post-tick observer (if any) runs on the sim thread while still holding the tick
                // gate, so it sees exactly one invocation per completed tick, in order, with the
                // engine's per-tick observation surfaces still describing THAT tick. Deliberately
                // NOT fired from ServiceOnce(): no tick advances there, and an observer counting
                // ticks would see a repeat.
                InvokePostTickObserver();

                LiveMatchFrame frame = CaptureFrame();
                ApplyCapturedFrame(frame);
                return frame;
            }
        }

        /// <summary>
        /// Installs the optional sim-thread pre-tick hook (the interactive Unity client's
        /// command-drain / save-servicing seam). Must be called once, before <see cref="Start"/>;
        /// the browser viewer never calls it. The hook runs on the sim thread at the top of every
        /// tick (and via <see cref="ServiceOnce"/>) and MUST NOT block or start/stop this streamer.
        /// </summary>
        /// <param name="hook">The pre-tick action. Must not be null.</param>
        public void SetPreTickHook(Action hook)
        {
            if (hook == null) { throw new ArgumentNullException(nameof(hook)); }
            lock (_lock)
            {
                if (_state != LifecycleState.NotStarted)
                {
                    throw new InvalidOperationException("SetPreTickHook must be called before Start().");
                }
                if (_preTickHook != null)
                {
                    throw new InvalidOperationException("A pre-tick hook is already installed (set-once).");
                }
                _preTickHook = hook;
            }
        }

        /// <summary>
        /// Installs the optional sim-thread post-tick observer — the seam a per-tick derivation such
        /// as Match Analytics #37 needs, whose §3.5 contract is that it consumes EVERY tick exactly
        /// once and fails loud on a gap.
        ///
        /// <para><b>Why this cannot reuse the pre-tick hook.</b> That hook is set-once and already
        /// taken by the client's command drain, and it also fires from <see cref="ServiceOnce"/>,
        /// where no tick advances — an every-tick consumer hung off it would see a repeated tick and
        /// refuse. So this is a second, distinct seam rather than a chain on the first.</para>
        ///
        /// <para><b>It does not weaken the playback-only invariant.</b> The observer is handed
        /// nothing and returns nothing: it can only read whatever the caller already had a reference
        /// to. This assembly still exposes no way to mutate a match (the invariant
        /// <c>interactive-unity-client-design.md</c> AR-1 H-2 and ERR-038-001 protect); manager input
        /// remains the client's own <c>ManagerCommandQueue</c>, drained by the pre-tick hook.</para>
        ///
        /// <para>Must be called once, before <see cref="Start"/>. The observer runs on the sim thread
        /// under the tick gate and MUST NOT block or start/stop this streamer.</para>
        /// </summary>
        /// <param name="observer">The post-tick action. Must not be null.</param>
        public void SetPostTickObserver(Action observer)
        {
            if (observer == null) { throw new ArgumentNullException(nameof(observer)); }
            lock (_lock)
            {
                if (_state != LifecycleState.NotStarted)
                {
                    throw new InvalidOperationException("SetPostTickObserver must be called before Start().");
                }
                if (_postTickObserver != null)
                {
                    throw new InvalidOperationException("A post-tick observer is already installed (set-once).");
                }
                _postTickObserver = observer;
            }
        }

        /// <summary>
        /// The exception that disarmed the post-tick observer, or null while it is healthy.
        ///
        /// <para>A View should surface this: after it is set, the observer is no longer running, so
        /// anything derived from it (live statistics, a post-match report) is frozen at the failing
        /// tick and will otherwise look merely stale.</para>
        /// </summary>
        public Exception PostTickObserverFault => _postTickObserverFault;

        /// <summary>
        /// Runs the observer, and on the first exception disarms it and latches the cause.
        ///
        /// <para>The pacing loop does not guard <see cref="TickOnce"/>, so an unhandled exception on
        /// this thread terminates the process — a derived statistic must not be able to kill a match
        /// in progress. Nor may it be swallowed: it is latched on
        /// <see cref="PostTickObserverFault"/> and the observer is not invoked again, so the failure
        /// is visible once instead of sixty times a second (the same posture
        /// <c>MatchClientDriver</c> takes for a command the engine refuses).</para>
        /// </summary>
        private void InvokePostTickObserver()
        {
            Action observer = _postTickObserver;
            if (observer == null) { return; }

            try
            {
                observer();
            }
            catch (Exception ex)
            {
                // Presentation/observation path, not the 60 Hz simulation inner loop FR-CS-069's
                // try/catch ban targets — the same carve-out the connection handling already uses.
                _postTickObserver     = null;
                _postTickObserverFault = ex;
            }
        }

        /// <summary>
        /// Runs one pass of the pre-tick hook WITHOUT advancing a tick — the seam for servicing a
        /// save request (or draining queued commands) while the streamer is paused or at full time,
        /// the exact states where the pre-tick hook never fires because the pacing loop is not
        /// ticking (§6.3 of the Unity-client note). Serialized against any in-flight tick via the
        /// same <c>_tickGate</c> the tick holds, so the engine is never touched by two paths at once;
        /// a no-op when no hook is installed. The running path and this off-tick path share the one
        /// hook, so there is no second servicing routine to keep in sync. Intended for the paused /
        /// full-time path: calling it during live playback is still thread-safe but services the hook
        /// off a tick boundary, which is rarely what a caller wants while the pacing loop is ticking.
        /// </summary>
        public void ServiceOnce()
        {
            lock (_tickGate)
            {
                _preTickHook?.Invoke();
            }
        }

        /// <summary>
        /// Stores <paramref name="frame"/> as the latest frame and applies the full-time
        /// auto-pause rule. Split out from <see cref="TickOnce"/> so tests can exercise the
        /// auto-pause decision directly against a hand-built frame, without needing a real engine
        /// run all the way to <c>MATCH_TICKS_TOTAL</c> (~324,000 ticks) or any engine-internal seam.
        /// </summary>
        internal void ApplyCapturedFrame(LiveMatchFrame frame)
        {
            lock (_lock)
            {
                _latestFrame = frame;
                if (frame.MatchEnded)
                {
                    // Full time: AI/Physics/Resolve are frozen inside the engine already, but the
                    // tick/snapshot loop keeps advancing — without this, the pacing thread would
                    // spin forever ticking a match with nothing left to show. A future Resume()
                    // call is still honoured (harmless — the engine stays frozen regardless).
                    _autoPaused = true;
                }
            }
        }

        private LiveMatchFrame CaptureFrame()
        {
            var positions = new Vector2[MatchEngineConstants.SQUAD_SIZE];
            var cues      = new LiveAgentCue[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                positions[i] = _engine.AgentView(i).Position;
                cues[i]      = new LiveAgentCue(
                    _engine.AgentYellowCards(i),
                    _engine.AgentIsSentOff(i),
                    _engine.AgentBenchSlot(i),
                    _engine.AgentIsGoalkeeper(i));
            }

            var subsUsed = new int[MatchEngineConstants.TEAM_COUNT];
            for (int t = 0; t < MatchEngineConstants.TEAM_COUNT; t++)
            {
                subsUsed[t] = _engine.SubstitutionsUsed(t);
            }

            // P1 KD-P1-3 — the engine reports a restart for the tick it happened on and then forgets it,
            // which keeps it off the snapshot. Latching the cross-tick memory is this layer's job, and
            // this is the only place that sees every tick. Called under _tickGate from TickOnce(), so
            // the latch needs no lock of its own.
            RestartCue restartThisTick = _engine.RestartAppliedThisTick;
            if (restartThisTick != RestartCue.None)
            {
                _lastRestart = new RestartBanner(
                    restartThisTick, _engine.RestartAwardedTeam, _engine.CurrentTick);
            }

            return new LiveMatchFrame(
                _engine.CurrentTick,
                _engine.BallView.Position,
                _engine.PossessingAgentId,
                positions,
                cues,
                subsUsed,
                new Scoreline(_engine.HomeScore, _engine.AwayScore),
                _engine.MatchEnded,
                _engine.CurrentPeriod,
                _lastRestart);
        }

        /// <summary>
        /// Spawns the background pacing thread. A no-op if already running; throws if this
        /// streamer was previously stopped (single-use — a new match needs a new streamer over a
        /// new engine, same as <c>MatchEngine</c> itself is single-match-per-instance).
        /// </summary>
        public void Start()
        {
            lock (_lock)
            {
                if (_state == LifecycleState.Stopped)
                {
                    throw new InvalidOperationException(
                        "LiveMatchStreamer is single-use: Start() cannot be called again after Stop().");
                }
                if (_state == LifecycleState.Running)
                {
                    return;
                }

                // _state and _thread must become visible together, atomically, under this lock —
                // otherwise a Stop() racing in right after this method released the lock but before
                // _thread was assigned would observe _state == Running with a still-null _thread,
                // and then call .Join() on null (see the identical defect class fixed in
                // LiveMatchServer.Start()/Stop() the same day).
                _thread = new Thread(PacingLoop) { IsBackground = true, Name = "LiveMatchStreamer" };
                _state = LifecycleState.Running;
                _thread.Start();
            }
        }

        /// <summary>Stops the pacing thread and joins it. Idempotent — a no-op if never started or already stopped.</summary>
        public void Stop()
        {
            Thread threadToJoin;
            lock (_lock)
            {
                if (_state != LifecycleState.Running)
                {
                    return;
                }
                _state = LifecycleState.Stopped;
                threadToJoin = _thread;
            }

            _stopRequested = true;
            threadToJoin.Join();
        }

        /// <summary>Reads the latest captured frame. False before the first tick has been captured.</summary>
        public bool TryGetLatestFrame(out LiveMatchFrame frame)
        {
            lock (_lock)
            {
                if (_latestFrame.HasValue)
                {
                    frame = _latestFrame.Value;
                    return true;
                }
            }

            frame = default;
            return false;
        }

        /// <summary>True if the user paused playback, or the match reached full time (auto-pause).</summary>
        public bool IsPaused
        {
            get { lock (_lock) { return _paused || _autoPaused; } }
        }

        /// <summary>Pauses the pacing loop (it stops calling <see cref="TickOnce"/> until <see cref="Resume"/>).</summary>
        public void Pause()
        {
            lock (_lock) { _paused = true; }
        }

        /// <summary>
        /// Resumes playback — clears both the user-pause and the full-time auto-pause (a user who
        /// explicitly asks to resume after full time gets a live view of a match that stays frozen
        /// regardless, which is harmless). Resets the pacing reference so the loop does not try to
        /// "catch up" on ticks that were never meant to happen while paused.
        /// </summary>
        public void Resume()
        {
            lock (_lock)
            {
                _paused          = false;
                _autoPaused      = false;
                _pacingEpochDirty = true;
            }
        }

        /// <summary>Current playback-speed multiplier (1.0 = real time).</summary>
        public float SpeedMultiplier
        {
            get { lock (_lock) { return _speedMultiplier; } }
        }

        /// <summary>
        /// Ticks per second the pacing loop is currently targeting — <c>ticksPerSecond ×
        /// SpeedMultiplier</c>, the exact product <see cref="PacingLoop"/> divides into to schedule
        /// each tick's target wall-clock time.
        ///
        /// <para><b>Why a consumer must read this rather than recompute it.</b> <c>ticksPerSecond</c>
        /// is a per-instance constructor argument that only DEFAULTS to
        /// <c>DeterministicSimConstants.PHYSICS_TICK_HZ</c> — a caller may construct this streamer
        /// with a different value, and a consumer that re-derives the product from the global
        /// constant instead of asking the streamer would silently diverge from the rate this loop is
        /// actually pacing at. AR pass M-5 over the interactive Unity client's P4b binding: the
        /// <c>MatchClientBehaviour</c> render loop's interpolation alpha
        /// (<c>FrameInterpolator.ComputeAlpha</c>) now reads this property instead of reimplementing
        /// the product itself.</para>
        /// </summary>
        public float EffectiveTicksPerSecond
        {
            get { lock (_lock) { return _ticksPerSecond * _speedMultiplier; } }
        }

        /// <summary>
        /// Sets the playback-speed multiplier. Throws for a non-finite, non-positive, or
        /// out-of-[<see cref="MatchViewerConstants.MinLiveSpeedMultiplier"/>,
        /// <see cref="MatchViewerConstants.MaxLiveSpeedMultiplier"/>] value — fail loud rather than
        /// silently clamp, so a caller (the HTTP control endpoint) can turn the exception into a
        /// 400 response instead of accepting a nonsensical speed.
        /// </summary>
        public void SetSpeedMultiplier(float multiplier)
        {
            if (!(multiplier > 0f) || float.IsInfinity(multiplier))
            {
                throw new ArgumentOutOfRangeException(nameof(multiplier), multiplier, "multiplier must be finite and > 0.");
            }
            if (multiplier < MatchViewerConstants.MinLiveSpeedMultiplier || multiplier > MatchViewerConstants.MaxLiveSpeedMultiplier)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(multiplier), multiplier,
                    string.Format(
                        System.Globalization.CultureInfo.InvariantCulture,
                        "multiplier must be within [{0}, {1}].",
                        MatchViewerConstants.MinLiveSpeedMultiplier, MatchViewerConstants.MaxLiveSpeedMultiplier));
            }

            lock (_lock)
            {
                _speedMultiplier  = multiplier;
                _pacingEpochDirty = true;
            }
        }

        private void PacingLoop()
        {
            var stopwatch = Stopwatch.StartNew();
            ulong tickIndex = 0;

            while (!_stopRequested)
            {
                bool shouldTick;
                float speed;
                lock (_lock)
                {
                    shouldTick = !_paused && !_autoPaused;
                    speed = _speedMultiplier;
                    if (_pacingEpochDirty)
                    {
                        // A speed change or a resume-from-pause invalidates the tickIndex-based
                        // target below (it assumes constant speed since the epoch started) — start
                        // a fresh epoch rather than let the next tick's target jump discontinuously.
                        stopwatch.Restart();
                        tickIndex = 0;
                        _pacingEpochDirty = false;
                    }
                }

                if (!shouldTick)
                {
                    Thread.Sleep(MatchViewerConstants.LivePausedPollIntervalMs);
                    continue;
                }

                TickOnce();
                tickIndex++;

                double targetMs = tickIndex * (1000.0 / (_ticksPerSecond * speed));
                double remainingMs = targetMs - stopwatch.Elapsed.TotalMilliseconds;
                if (remainingMs > 0.0)
                {
                    Thread.Sleep((int)Math.Min(remainingMs, int.MaxValue));
                }
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-15 | —      | Initial creation: real-time-paced MatchEngine tick loop with a |
// |         |            |        | lock-protected latest-frame surface + pause/resume/speed        |
// |         |            |        | control + full-time auto-pause + cached roster metadata (team/ |
// |         |            |        | GK per index) so LiveMatchServer never needs a MatchEngine      |
// |         |            |        | reference of its own. Drift-corrected pacing via a Stopwatch-  |
// |         |            |        | based target-wall-time scheme (per                              |
// |         |            |        | docs/tracking/interactive-match-view-design.md AR-1). TickOnce  |
// |         |            |        | split into TickOnce + internal ApplyCapturedFrame so tests can  |
// |         |            |        | exercise the full-time auto-pause decision against a hand-built |
// |         |            |        | frame — reaching a real MatchEnded=true needs ~324,000 ticks or |
// |         |            |        | an engine-internal seam this assembly has no InternalsVisibleTo |
// |         |            |        | access to. Self-review caught a Start()/Stop() race: _state was  |
// |         |            |        | flipped to Running inside the lock but _thread was assigned      |
// |         |            |        | afterward, outside it — a Stop() racing into that window would   |
// |         |            |        | capture a still-null _thread and call .Join() on null; _thread   |
// |         |            |        | creation + assignment + .Start() now all happen inside the same  |
// |         |            |        | lock as the _state flip (the same defect class as                |
// |         |            |        | LiveMatchServer.Start()/Stop(), fixed there first).               |
// | 1.1     | 2026-07-24 | —      | Interactive Unity client §4/§5-P0/§6: optional sim-thread       |
// |         |            |        | pre-tick hook (SetPreTickHook, set-once, pre-Start) run at the  |
// |         |            |        | top of every TickOnce ahead of RunTick, plus ServiceOnce() to  |
// |         |            |        | run that hook off-tick (save/drain while paused or at full     |
// |         |            |        | time). New _tickGate serializes a tick against ServiceOnce so  |
// |         |            |        | the hook (which may mutate the engine) never interleaves with  |
// |         |            |        | a tick. Browser viewer installs no hook — playback-only        |
// |         |            |        | invariant preserved by construction.                           |
// | 1.4     | 2026-07-27 | —      | P1 richer observation frame (§5-P1). CaptureFrame now samples  |
// |         |            |        | the per-agent cues (booking / sent-off / bench slot), the      |
// |         |            |        | per-team substitution counts and the derived MatchPeriod, and  |
// |         |            |        | LATCHES the engine's within-tick restart cue (KD-P1-3) — the   |
// |         |            |        | cross-tick memory deliberately lives here, not in the engine,  |
// |         |            |        | so nothing about it reaches the snapshot. The three latch      |
// |         |            |        | fields are written and read only inside CaptureFrame, itself   |
// |         |            |        | only called from TickOnce under _tickGate, so they need no     |
// |         |            |        | synchronisation of their own and escape only as struct copies. |
// | 1.5     | 2026-07-27 | —      | P1 AR-1 M-6: the three latch fields collapse into a single     |
// |         |            |        | RestartBanner. It carries no initializer on purpose — the      |
// |         |            |        | banner derives its team and tick from its own cue, so the      |
// |         |            |        | zeroed default already reads as "no restart" and there is no   |
// |         |            |        | separate no-restart constant to keep in step with it.          |
// | 1.6     | 2026-08-03 | —      | P4a: CaptureFrame samples AgentIsGoalkeeper into the new       |
// |         |            |        | LiveAgentCue.IsGoalkeeper each tick, and the boot-cached       |
// |         |            |        | _isGoalkeeper array is re-documented as boot-time only. The    |
// |         |            |        | comment above it claimed "roster metadata never changes";      |
// |         |            |        | true of team ids, false of goalkeeper flags, which a           |
// |         |            |        | substitution rewrites (MatchEngine.SubstitutePlayer).          |
// | 1.7     | 2026-08-04 | —      | P4a AR pass (M-6): + a boot-cached ShirtNumber(int) computed by |
// |         |            |        | RosterShirtNumbers, so LiveMatchServer serves the number and   |
// |         |            |        | the viewer script stops recomputing the rule in JavaScript.    |
// |         |            |        | Unlike goalkeeper flags these ARE boot-constant: the number    |
// |         |            |        | belongs to the slot, and a substitution hands it to whoever    |
// |         |            |        | fills that slot. Also records the 2026-08-03 Modified line the |
// |         |            |        | v1.6 landing left off the header.                              |
// | 1.8     | 2026-08-15 | —      | P4b AR pass M-5: + EffectiveTicksPerSecond (ticksPerSecond ×   |
// |         |            |        | SpeedMultiplier under the same lock SpeedMultiplier already    |
// |         |            |        | uses) — the interactive Unity client's render loop no longer   |
// |         |            |        | recomputes this product against the DEFAULT tick rate, which   |
// |         |            |        | would have silently diverged from a streamer constructed with  |
// |         |            |        | a non-default ticksPerSecond.                                  |
#endregion
