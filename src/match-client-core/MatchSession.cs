// File:     src/match-client-core/MatchSession.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §4/§5-P0/§6),
//           Code Standards #20
// Purpose:  The single composition root for a live interactive match: from a MatchSetup it constructs
//           and wires the MatchEngine, the reused LiveMatchStreamer, and the MatchClientDriver —
//           installing the driver's drain as the streamer's pre-tick hook and routing the off-tick
//           save-servicing seam through ServiceOnce(). It owns match lifecycle AND the command-channel
//           wiring, so the Unity host and any head-less test drive the identical composition (§5-P0).

using System;

using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// Owns and wires a whole live-match composition. The View reads frames via
    /// <see cref="TryGetLatestFrame"/> (the observer-neutral read path) and issues game commands via
    /// <see cref="Commands"/> (the deterministic write path) — it never touches the engine directly.
    /// Boot-only mutators from the <see cref="MatchSetup"/> are applied once here, pre-kickoff; live
    /// changes only ever flow through the command queue.
    /// </summary>
    public sealed class MatchSession
    {
        private readonly MatchEngine.MatchEngine _engine;
        private readonly LiveMatchStreamer _streamer;
        private readonly MatchClientDriver _driver;
        private readonly MatchEngineMutations _mutations;

        /// <summary>
        /// Builds the composition from <paramref name="setup"/>: constructs the engine, applies the
        /// boot-only mutators once (squads → managers → GK/heading → initial tactics, all pre-kickoff),
        /// then wires the streamer + driver and installs the driver's drain as the streamer's pre-tick
        /// hook. Nothing has ticked yet — call <see cref="Start"/> to begin paced playback.
        /// </summary>
        /// <param name="setup">Boot configuration. Must not be null.</param>
        public MatchSession(MatchSetup setup)
        {
            if (setup == null) { throw new ArgumentNullException(nameof(setup)); }

            _engine = new MatchEngine.MatchEngine(setup.Seed);

            // Boot-only / pre-kickoff mutators, applied once, in a fixed order (§3-2). ConfigureSquads
            // requires tick 0, which holds here (nothing has ticked). A neutral demo skips squads and
            // managers entirely, leaving the engine byte-identical to a bare same-seed engine except
            // for the staged Balanced tactics (a proven no-op).
            if (setup.HasDistinctSquads)
            {
                _engine.ConfigureSquads(setup.HomeSquad, setup.AwaySquad);
            }
            if (setup.HomeManagerMode != ManagerMode.Human)
            {
                _engine.ConfigureManager(0, setup.HomeManagerMode, setup.HomeManagerProfile);
            }
            if (setup.AwayManagerMode != ManagerMode.Human)
            {
                _engine.ConfigureManager(1, setup.AwayManagerMode, setup.AwayManagerProfile);
            }
            if (setup.GkHeadingEnabled)
            {
                _engine.EnableGkHeading();
            }

            TeamTactic home = setup.HomeTactic;
            TeamTactic away = setup.AwayTactic;
            _engine.SetTeamTactic(0, in home);
            _engine.SetTeamTactic(1, in away);

            _streamer  = new LiveMatchStreamer(_engine);
            _mutations = new MatchEngineMutations(_engine);
            _driver    = new MatchClientDriver(new ManagerCommandQueue());

            // The one servicing routine: the driver's drain, run on the sim thread. Installed as the
            // pre-tick hook (fires each tick) and reachable off-tick via ServiceOnce() (§6.3). The
            // closure captures the session-owned mutation adapter, so the driver itself never holds an
            // engine reference. Allocated once here, not per tick.
            _streamer.SetPreTickHook(() => _driver.Service(_mutations));
        }

        /// <summary>The reused ViewModel — the View reads frames from it; playback pause/speed live here (§6.4).</summary>
        public LiveMatchStreamer Streamer => _streamer;

        /// <summary>The command drain + tick-stamped log (read the log for the match record; §6.1).</summary>
        public MatchClientDriver Driver => _driver;

        /// <summary>The enqueue-only command surface for the View's tactical/substitution input.</summary>
        public ManagerCommandQueue Commands => _driver.Commands;

        /// <summary>
        /// Attaches a per-tick observation over the running match — the seam a live derivation such
        /// as Match Analytics #37 needs, whose contract is to consume every tick exactly once.
        ///
        /// <para><paramref name="observerFactory"/> is invoked ONCE, here, with the session's engine,
        /// and returns the action the streamer runs on the sim thread after each completed tick. The
        /// engine is passed to the factory rather than exposed as a property so there is no standing
        /// public handle on it: <b>build a reader from it, never a mutator.</b> Mutating the engine
        /// directly bypasses the command queue, and it is the queue that produces the tick-stamped
        /// log a live match is replayed from (§6.1) — a change applied around it is simply absent
        /// from the record, and the replay silently diverges.</para>
        ///
        /// <para>Must be called before <see cref="Start"/>, once. The observer runs under the
        /// streamer's tick gate and MUST NOT block or start/stop the session.</para>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="observerFactory"/> is null.</exception>
        /// <exception cref="InvalidOperationException">
        /// The factory returned null, an observer is already attached, or the session has started.
        /// </exception>
        public void AttachTickObserver(Func<MatchEngine.MatchEngine, Action> observerFactory)
        {
            if (observerFactory == null) { throw new ArgumentNullException(nameof(observerFactory)); }

            Action observer = observerFactory(_engine);
            if (observer == null)
            {
                throw new InvalidOperationException(
                    "observerFactory returned null. An attached-but-absent observer would look wired " +
                    "and silently see nothing.");
            }

            _streamer.SetPostTickObserver(observer);
        }

        /// <summary>Begins paced background playback (the streamer's pacing loop). Idempotent while running; single-use after <see cref="Stop"/>.</summary>
        public void Start() => _streamer.Start();

        /// <summary>Stops and joins the pacing loop. Idempotent.</summary>
        public void Stop() => _streamer.Stop();

        /// <summary>Reads the latest captured frame (the observer-neutral read path). False before the first tick.</summary>
        public bool TryGetLatestFrame(out LiveMatchFrame frame) => _streamer.TryGetLatestFrame(out frame);

        /// <summary>
        /// Runs one off-tick servicing pass (drain any queued commands on the sim thread without
        /// advancing a tick) — the seam a save request uses while paused or at full time (§6.3). The
        /// durable-capture wiring that rides this seam lands at P2/P3; at P0 it drains the queue.
        /// </summary>
        public void ServiceOnce() => _streamer.ServiceOnce();
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial creation (P0): composition root — builds/wires engine  |
// |         |            |        | + streamer + driver, installs the pre-tick hook, exposes the   |
// |         |            |        | read (frames) and write (commands) surfaces + ServiceOnce.     |
#endregion
