// File:     src/match-viewer/LiveMatchStreamer.cs
// Created:  2026-07-15
// Modified: 2026-07-15
// Author:   —
// Spec:     Interactive match view (docs/tracking/interactive-match-view-design.md), Code Standards #20
// Purpose:  Paces a real MatchEngine at wall-clock speed on a background thread, observing it
//           between ticks exactly like MatchReplayRecorder (never mutates state beyond calling
//           RunTick()), and exposes the latest captured frame through a lock-protected surface for
//           LiveMatchServer to serve. Presentation tooling — not the 60 Hz simulation hot path, so
//           the zero-allocation / no-Thread / no-try-catch game-loop rules do not apply here (same
//           carve-out MatchReplayRecorder / HtmlReplayExporter already use).

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
        private readonly int[]  _teamIds;
        private readonly bool[] _isGoalkeeper;

        private LifecycleState _state = LifecycleState.NotStarted;
        private Thread _thread;
        private volatile bool _stopRequested;

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

            // Roster metadata never changes across a match — captured once here so LiveMatchServer
            // can render team/GK cues without ever holding a MatchEngine reference itself (§9.1 of
            // the design note: the server and the engine must stay disjoint by construction).
            _teamIds      = new int[MatchEngineConstants.SQUAD_SIZE];
            _isGoalkeeper = new bool[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                _teamIds[i]      = engine.AgentTeamId(i);
                _isGoalkeeper[i] = engine.AgentIsGoalkeeper(i);
            }
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

        /// <summary>True when roster <paramref name="index"/> is a goalkeeper.</summary>
        public bool IsGoalkeeper(int index)
        {
            GuardRosterIndex(index);
            return _isGoalkeeper[index];
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
            _engine.RunTick();
            LiveMatchFrame frame = CaptureFrame();
            ApplyCapturedFrame(frame);
            return frame;
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
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                positions[i] = _engine.AgentView(i).Position;
            }

            return new LiveMatchFrame(
                _engine.CurrentTick,
                _engine.BallView.Position,
                _engine.PossessingAgentId,
                positions,
                _engine.HomeScore,
                _engine.AwayScore,
                _engine.MatchEnded);
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
#endregion
