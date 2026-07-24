// File:     src/match-client-core/MatchClientDriver.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §4/§5-P2/§6),
//           Code Standards #20
// Purpose:  Owns the manager command queue and the tick-stamped command log. Its Service method is the
//           streamer's pre-tick hook body: drain the queue and apply each command on the sim thread at
//           the tick top, recording the applied ones stamped with the engine's current tick. Holds NO
//           engine reference — it receives the mutation surface as a parameter each call (§4).

using System;
using System.Collections.Generic;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// The deterministic command drain. <see cref="Service"/> is installed as the streamer's pre-tick
    /// hook (via <see cref="MatchSession"/>) and is also reachable off-tick through the streamer's
    /// <c>ServiceOnce()</c> (§6.3), so the running and paused/ended paths share this one routine. The
    /// driver keeps no engine reference: every apply goes through the <see cref="ILiveMatchMutations"/>
    /// passed to <see cref="Service"/>, so nothing here can touch the engine off the sim thread.
    /// </summary>
    public sealed class MatchClientDriver
    {
        private readonly ManagerCommandQueue _queue;

        // Reused drain scratch (sim-thread only — Service is never re-entered concurrently because the
        // streamer's _tickGate serializes tick and ServiceOnce). Not the 60 Hz hot path.
        private readonly List<ManagerCommand> _drainBuffer = new List<ManagerCommand>();

        // The log is appended on the sim thread (via the hook) and may be read from any thread (a UI
        // consumer, a save). List<T> is not safe for concurrent read+write, so every append and every
        // read is guarded by _logLock, and Log returns a point-in-time snapshot copy rather than a view
        // aliasing the live list.
        private readonly object _logLock = new object();
        private readonly List<TickStampedCommand> _log = new List<TickStampedCommand>();
        private readonly List<TickStampedCommand> _failedLog = new List<TickStampedCommand>();

        /// <summary>Constructs a driver over <paramref name="queue"/>. Must not be null.</summary>
        public MatchClientDriver(ManagerCommandQueue queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
        }

        /// <summary>The enqueue-only command surface handed to the View.</summary>
        public ManagerCommandQueue Commands => _queue;

        /// <summary>
        /// A thread-safe point-in-time snapshot of the tick-stamped command log — the record a live
        /// match is reproducible from (§6.1). Safe to read from any thread (it copies under the log
        /// lock); in-memory only (§11).
        /// </summary>
        public IReadOnlyList<TickStampedCommand> Log
        {
            get { lock (_logLock) { return _log.ToArray(); } }
        }

        /// <summary>
        /// A thread-safe snapshot of commands the engine REFUSED at apply time (an out-of-range index, a
        /// substitution over the cap or of an already-used bench slot, etc.). Each is dropped — not
        /// applied, not in <see cref="Log"/> — and recorded here (stamped with the tick it was attempted
        /// at) so the failure is observable rather than crashing the sim loop. In-memory only.
        /// </summary>
        public IReadOnlyList<TickStampedCommand> FailedCommands
        {
            get { lock (_logLock) { return _failedLog.ToArray(); } }
        }

        /// <summary>
        /// Drains the queue and applies each command through <paramref name="mutations"/> on the sim
        /// thread, then records the applied ones in the tick-stamped log. Sim-side authority for the
        /// post-full-time rule (§6.2): if the match has ended, every drained command is dropped
        /// (neither applied nor logged) — a finished match is never mutated. Called at the top of every
        /// tick (pre-tick hook) and off-tick by <c>ServiceOnce()</c>.
        /// </summary>
        /// <param name="mutations">The live mutation surface. Must not be null.</param>
        public void Service(ILiveMatchMutations mutations)
        {
            if (mutations == null) { throw new ArgumentNullException(nameof(mutations)); }

            _drainBuffer.Clear();
            _queue.DrainInto(_drainBuffer);
            if (_drainBuffer.Count == 0) { return; }

            // Post-MatchEnded drop is decided sim-side against the LIVE engine state, not a lagging
            // frame the UI saw. A command that slipped past the UI's best-effort enqueue-time check in
            // the end-of-match window is harmless: it is dropped here (§6.2).
            if (mutations.MatchEnded)
            {
                _drainBuffer.Clear();
                return;
            }

            // One drain runs at a fixed point at the top of one tick, so every command in the batch is
            // applied at the same tick — read it once.
            ulong appliedTick = mutations.CurrentTick;
            for (int i = 0; i < _drainBuffer.Count; i++)
            {
                ManagerCommand command = _drainBuffer[i];

                // A live mutator fails loud on a refused command (an out-of-range index, a substitution
                // over the cap / of an already-used or sent-off slot). This runs inside the streamer's
                // pre-tick hook on the background pacing thread, so an escaping exception would kill that
                // thread and silently freeze the match. Isolate it: the failing command is dropped (not
                // applied, not logged as applied) and recorded in FailedCommands; the rest of the batch
                // and the sim loop carry on. The try/catch is permitted here — this is the presentation
                // command-drain path, not the 60 Hz zero-alloc game loop (same carve-out the streamer
                // documents). A refused command never mutated the engine (mutators validate before
                // writing), so dropping it leaves no partial state, and never entering Log keeps the
                // reproducible-from-the-log contract intact.
                bool applied;
                try
                {
                    command.Apply(mutations);
                    applied = true;
                }
                catch (Exception)
                {
                    applied = false;
                }

                // Brief per-command lock (uncontended on the sim thread; readers are rare) so a
                // concurrent Log / FailedCommands snapshot never races this append.
                lock (_logLock)
                {
                    if (applied) { _log.Add(new TickStampedCommand(appliedTick, in command)); }
                    else { _failedLog.Add(new TickStampedCommand(appliedTick, in command)); }
                }
            }
            _drainBuffer.Clear();
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial creation (P2): drain-and-apply Service (the pre-tick   |
// |         |            |        | hook body) + tick-stamped log + sim-side post-end drop.        |
// | 1.1     | 2026-07-24 | —      | AR pass-1 Medium: the log append (sim thread) and the Log read  |
// |         |            |        | (any thread) are now guarded by _logLock; Log returns a        |
// |         |            |        | snapshot copy instead of a view aliasing the live list, so a   |
// |         |            |        | UI-thread read during a running match no longer races.         |
// | 1.2     | 2026-07-24 | —      | AR pass-2 Medium: a command whose Apply is refused by the       |
// |         |            |        | engine (bad index / sub cap / used slot) is now isolated —      |
// |         |            |        | dropped + recorded in FailedCommands — instead of escaping the  |
// |         |            |        | pre-tick hook and killing the background pacing thread.         |
#endregion
