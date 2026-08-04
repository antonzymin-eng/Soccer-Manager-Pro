// File:     src/match-client-core/MatchSession.cs
// Created:  2026-07-24
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §4/§5-P0/§5-P6/§6),
//           Code Standards #20
// Purpose:  The single composition root for a live interactive match: from a MatchSetup it constructs
//           and wires the MatchEngine, the reused LiveMatchStreamer, and the MatchClientDriver —
//           installing the driver's drain as the streamer's pre-tick hook and routing the off-tick
//           save-servicing seam through ServiceOnce(). It owns match lifecycle AND the command-channel
//           wiring, so the Unity host and any head-less test drive the identical composition (§5-P0).
//           P6 adds the head-less half of that: a deterministic TickOnce() advance, the sim-thread
//           durable save capture that rides the ServiceOnce seam (§6.3), and the RestoreFrom factory
//           that rebuilds a whole session over a restored engine.

using System;
using System.Threading;

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
    ///
    /// <para><b>Two ways to advance a match, and they are mutually exclusive.</b> <see cref="Start"/>
    /// hands the tick loop to the streamer's background pacing thread (the Unity host's mode);
    /// <see cref="TickOnce"/> advances exactly one tick on the calling thread (the head-less mode the
    /// §5-P6 closed-loop scenario and any log-driven replay use). Mixing them would race two threads
    /// through one engine, so <see cref="TickOnce"/> refuses once <see cref="Start"/> has been
    /// called.</para>
    /// </summary>
    public sealed class MatchSession
    {
        private readonly MatchEngine.MatchEngine _engine;
        private readonly LiveMatchStreamer _streamer;
        private readonly MatchClientDriver _driver;
        private readonly MatchEngineMutations _mutations;

        // Save-request handshake between an arbitrary caller thread (CaptureSave) and the sim thread
        // (the pre-tick hook, whether fired by a tick or by ServiceOnce). Deliberately Interlocked /
        // Volatile rather than a lock: the sim side already runs under the streamer's tick gate, so a
        // lock held across ServiceOnce() on the caller side would establish the opposite lock order
        // (_saveGate -> _tickGate vs _tickGate -> save lock) and could deadlock. _saveGate serializes
        // concurrent CaptureSave CALLERS only, and is never taken by the sim side.
        private readonly object _saveGate = new object();
        private int _saveRequested;
        private byte[] _capturedSaveBlob;
        private Exception _saveFault;

        // Latched by Start(); never cleared (the streamer is single-use after Stop()). Read by
        // TickOnce's guard.
        private int _pacedPlaybackStarted;

        /// <summary>
        /// Builds the composition from <paramref name="setup"/>: constructs the engine, applies the
        /// boot-only mutators once (squads → managers → GK/heading → initial tactics, all pre-kickoff),
        /// then wires the streamer + driver and installs the driver's drain as the streamer's pre-tick
        /// hook. Nothing has ticked yet — call <see cref="Start"/> to begin paced playback, or
        /// <see cref="TickOnce"/> to advance head-lessly.
        /// </summary>
        /// <param name="setup">Boot configuration. Must not be null.</param>
        public MatchSession(MatchSetup setup)
            : this(BootEngine(setup))
        {
        }

        // The wiring half, shared by the boot path and the restore path. Everything here is
        // engine-agnostic: it must NOT apply a boot-only mutator, because on the restore path those
        // mutators' effects are already inside the snapshot (and ConfigureSquads would throw outright
        // on an engine that has ticked).
        private MatchSession(MatchEngine.MatchEngine engine)
        {
            _engine    = engine;
            _streamer  = new LiveMatchStreamer(_engine);
            _mutations = new MatchEngineMutations(_engine);
            _driver    = new MatchClientDriver(new ManagerCommandQueue());

            // The one servicing routine: drain, then service a pending save, run on the sim thread.
            // Installed as the pre-tick hook (fires each tick) and reachable off-tick via ServiceOnce()
            // (§6.3). A method group rather than a closure, so it allocates once here, not per tick.
            _streamer.SetPreTickHook(ServiceOnSimThread);
        }

        // Constructs and boot-configures an engine from a setup. Static so it can run before the
        // instance constructor's field initialisation, and so the null guard reports the public
        // constructor's own parameter name.
        private static MatchEngine.MatchEngine BootEngine(MatchSetup setup)
        {
            if (setup == null) { throw new ArgumentNullException(nameof(setup)); }

            var engine = new MatchEngine.MatchEngine(setup.Seed);

            // Boot-only / pre-kickoff mutators, applied once, in a fixed order (§3-2). ConfigureSquads
            // requires tick 0, which holds here (nothing has ticked). A neutral demo skips squads and
            // managers entirely, leaving the engine byte-identical to a bare same-seed engine except
            // for the staged Balanced tactics (a proven no-op).
            if (setup.HasDistinctSquads)
            {
                engine.ConfigureSquads(setup.HomeSquad, setup.AwaySquad);
            }
            if (setup.HomeManagerMode != ManagerMode.Human)
            {
                engine.ConfigureManager(0, setup.HomeManagerMode, setup.HomeManagerProfile);
            }
            if (setup.AwayManagerMode != ManagerMode.Human)
            {
                engine.ConfigureManager(1, setup.AwayManagerMode, setup.AwayManagerProfile);
            }
            if (setup.GkHeadingEnabled)
            {
                engine.EnableGkHeading();
            }

            TeamTactic home = setup.HomeTactic;
            TeamTactic away = setup.AwayTactic;
            engine.SetTeamTactic(0, in home);
            engine.SetTeamTactic(1, in away);

            return engine;
        }

        /// <summary>
        /// Rebuilds a whole session over the engine restored from <paramref name="saveBlob"/> (a blob
        /// produced by <see cref="CaptureSave"/> / <c>MatchSaveManager.Encode</c>). The restored session
        /// resumes at the saved tick with a FRESH, empty command queue and an empty tick-stamped log —
        /// the log is in-memory only and is not carried across a save (§11), so a resumed match
        /// reproduces from the restored snapshot plus the POST-restore log (§2 item 3).
        ///
        /// <para>Deliberately takes no <see cref="MatchSetup"/>: every boot-only mutator's effect
        /// (squads, managers, the GK/heading opt-in, the committed team/player tactics) is already
        /// inside the snapshot, and re-applying them would either throw (<c>ConfigureSquads</c> refuses
        /// an engine that has ticked) or overwrite restored state with boot values.</para>
        /// </summary>
        /// <param name="saveBlob">The save blob to restore. Must not be null.</param>
        /// <param name="squads">The ClubId → Squad resolver a DISTINCT-squad save needs (#27 T3 / KD-3);
        /// may be null for a neutral / unconfigured-squad save. Must resolve the same rosters the saved
        /// match loaded — the blob carries roster identity, not attribute values.</param>
        public static MatchSession RestoreFrom(byte[] saveBlob, ISquadProvider squads = null)
        {
            if (saveBlob == null) { throw new ArgumentNullException(nameof(saveBlob)); }
            return new MatchSession(MatchSaveManager.Restore(saveBlob, squads));
        }

        /// <summary>The reused ViewModel — the View reads frames from it; playback pause/speed live here (§6.4).</summary>
        public LiveMatchStreamer Streamer => _streamer;

        /// <summary>The command drain + tick-stamped log (read the log for the match record; §6.1).</summary>
        public MatchClientDriver Driver => _driver;

        /// <summary>The enqueue-only command surface for the View's tactical/substitution input.</summary>
        public ManagerCommandQueue Commands => _driver.Commands;

        /// <summary>
        /// The engine's current (last completed) tick — the value the next drained command will be
        /// stamped with, and the coordinate a log-driven replay schedules against.
        ///
        /// <para>Read directly off the engine, so it is meaningful before the first frame exists (unlike
        /// <see cref="TryGetLatestFrame"/>). Intended for the head-less / paused caller: while the
        /// pacing thread is running this is a racing read of a value the sim is advancing, and the
        /// frame is the thread-safe surface.</para>
        /// </summary>
        public ulong CurrentTick => _engine.CurrentTick;

        /// <summary>
        /// True once full time has fired. Same threading caveat as <see cref="CurrentTick"/> — the
        /// frame's <c>MatchEnded</c> is the surface a running View should read.
        /// </summary>
        public bool MatchEnded => _engine.MatchEnded;

        /// <summary>
        /// A fresh copy of the engine's chained snapshot digest after the most recent completed tick —
        /// the byte-level identity of the run, and what "digest-identical" is asserted against in the
        /// §5-P6 closed-loop scenario. Diagnostic accessor; allocates a copy and is not a per-tick
        /// surface. Same threading caveat as <see cref="CurrentTick"/>.
        /// </summary>
        public byte[] CurrentSnapshotDigest => _engine.CurrentSnapshotDigest;

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

        /// <summary>Begins paced background playback (the streamer's pacing loop). Idempotent while running; single-use after <see cref="Stop"/>. Permanently disables <see cref="TickOnce"/> on this session.</summary>
        public void Start()
        {
            // Latched BEFORE the streamer starts: once the pacing thread exists, a TickOnce() on
            // another thread would race it through one engine, and the guard must already be armed.
            // Never cleared — the streamer is single-use after Stop(), so there is no state to return to.
            Volatile.Write(ref _pacedPlaybackStarted, 1);
            _streamer.Start();
        }

        /// <summary>Stops and joins the pacing loop. Idempotent.</summary>
        public void Stop() => _streamer.Stop();

        /// <summary>
        /// Advances the composition by exactly one tick on the CALLING thread and returns the captured
        /// frame — the head-less deterministic advance the §5-P6 closed-loop scenario and any log-driven
        /// replay drive a match with. It runs the real streamer tick: the pre-tick hook fires (draining
        /// and applying queued commands at this tick, stamping them into the log), the engine ticks, any
        /// post-tick observer runs, and the frame is captured — the identical path paced playback takes,
        /// minus the wall-clock pacing.
        ///
        /// <para>Mutually exclusive with <see cref="Start"/>: two threads ticking one engine is a data
        /// race, so this throws once paced playback has been started on this session.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Start"/> has been called on this session.</exception>
        public LiveMatchFrame TickOnce()
        {
            if (Volatile.Read(ref _pacedPlaybackStarted) != 0)
            {
                throw new InvalidOperationException(
                    "TickOnce() is the head-less advance and must not race the pacing loop: Start() has " +
                    "been called on this session. Drive a paced match with Start()/Pause()/Resume(), or a " +
                    "head-less one with TickOnce() — not both.");
            }
            return _streamer.TickOnce();
        }

        /// <summary>Reads the latest captured frame (the observer-neutral read path). False before the first tick.</summary>
        public bool TryGetLatestFrame(out LiveMatchFrame frame) => _streamer.TryGetLatestFrame(out frame);

        /// <summary>
        /// Runs one off-tick servicing pass (drain any queued commands on the sim thread without
        /// advancing a tick) — the seam a save request uses while paused or at full time (§6.3).
        /// </summary>
        public void ServiceOnce() => _streamer.ServiceOnce();

        /// <summary>
        /// Captures a durable save of the match at its current tick and returns it as a blob
        /// (<c>MatchSaveManager.Encode</c>), restorable through <see cref="RestoreFrom"/>. This is the
        /// §6.3 save path: the capture runs on the SIM thread, inside the streamer's tick gate, so it
        /// can never tear-read an engine mid-tick, and it works identically while running, paused, and
        /// at full time (the request is serviced by the shared hook body, which <c>ServiceOnce()</c>
        /// reaches even when the pacing loop is not ticking).
        ///
        /// <para><b>The drained-empty-before-capture invariant (§6.3) holds by construction, not by
        /// assertion:</b> the one hook body drains and applies the queue and only then captures, in the
        /// same sim-thread pass, so no command can be enqueued-but-undrained at the captured state and
        /// be lost on restore.</para>
        ///
        /// <para>A command enqueued at the save tick is in the blob if it was drained before the capture
        /// and absent if it arrived after — both are stamped with the same tick. A caller resuming a
        /// replay across the save should therefore not schedule a command at the save tick itself; see
        /// <see cref="TickStampedCommandReplay.FromTick"/>.</para>
        /// </summary>
        public byte[] CaptureSave()
        {
            // Serializes concurrent CaptureSave callers so one cannot take another's blob. The sim side
            // never takes this, so there is no lock-order cycle with the streamer's tick gate.
            lock (_saveGate)
            {
                Volatile.Write(ref _capturedSaveBlob, null);
                Volatile.Write(ref _saveFault, null);
                Interlocked.Exchange(ref _saveRequested, 1);

                // Takes the tick gate, so it either services the request itself or blocks behind an
                // in-flight tick whose hook already did — and that tick's hook has published its result
                // before releasing the gate. Either way the outcome is visible once this returns.
                _streamer.ServiceOnce();

                Exception fault = Interlocked.Exchange(ref _saveFault, null);
                byte[] blob     = Interlocked.Exchange(ref _capturedSaveBlob, null);

                if (fault != null)
                {
                    throw new InvalidOperationException(
                        "CaptureSave: the sim-thread snapshot capture failed; see the inner exception. " +
                        "The match is unaffected — nothing was mutated and the tick loop is still running.",
                        fault);
                }
                if (blob == null)
                {
                    // Unreachable while the hook is installed (the constructor always installs it).
                    // Fail loud rather than hand back a null a caller would write to disk as a save.
                    Interlocked.Exchange(ref _saveRequested, 0);
                    throw new InvalidOperationException(
                        "CaptureSave: the sim-thread servicing pass produced no snapshot. The session's " +
                        "pre-tick hook is not installed, so the save path has nothing to run on.");
                }
                return blob;
            }
        }

        // The one sim-thread servicing routine, shared by the per-tick path (pre-tick hook) and the
        // off-tick path (ServiceOnce). Runs under the streamer's tick gate.
        private void ServiceOnSimThread()
        {
            _driver.Service(_mutations);

            // AFTER the drain, deliberately: that ordering IS the §6.3 drained-empty-before-capture
            // invariant. A command enqueued before this pass is already applied and inside the state
            // being captured, so it is never both absent from the snapshot and absent from the queue.
            if (Interlocked.Exchange(ref _saveRequested, 0) != 1)
            {
                return;
            }

            try
            {
                Volatile.Write(ref _capturedSaveBlob, MatchSaveManager.Encode(_engine));
            }
            catch (Exception ex)
            {
                // This can run on the background pacing thread, where an escaping exception kills the
                // thread and silently freezes the match — the same failure class MatchClientDriver
                // isolates for a refused command, and the same FR-CS-069 carve-out (a servicing path,
                // not the 60 Hz inner loop). The request came from CaptureSave, so the fault is handed
                // back to that caller rather than swallowed: a save that failed must not look like a
                // save that succeeded.
                Volatile.Write(ref _saveFault, ex);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial creation (P0): composition root — builds/wires engine  |
// |         |            |        | + streamer + driver, installs the pre-tick hook, exposes the   |
// |         |            |        | read (frames) and write (commands) surfaces + ServiceOnce.     |
// | 1.1     | 2026-07-27 | —      | AttachTickObserver (B6): the client-side seam for the streamer's|
// |         |            |        | post-tick observer. The factory receives the engine once and    |
// |         |            |        | returns a READER built from it, so the session still exposes no |
// |         |            |        | standing engine property and no mutation path.                  |
// | 1.2     | 2026-08-03 | —      | P6 head-less closed loop. Constructor split into a static       |
// |         |            |        | BootEngine (boot-only mutators) + a private engine-agnostic     |
// |         |            |        | wiring ctor, so RestoreFrom can build a session over a restored |
// |         |            |        | engine WITHOUT re-applying boot mutators. Adds TickOnce() (the  |
// |         |            |        | head-less advance, fail-loud against a Start()ed session),      |
// |         |            |        | CurrentTick / MatchEnded / CurrentSnapshotDigest reads, and     |
// |         |            |        | CaptureSave() — the durable capture that rides the ServiceOnce  |
// |         |            |        | seam and holds §6.3's drained-empty-before-capture invariant by |
// |         |            |        | ordering (drain, then capture, in one sim-thread pass).         |
#endregion
