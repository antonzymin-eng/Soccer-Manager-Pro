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
using System.Collections.ObjectModel;

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
        private readonly List<TickStampedCommand> _log = new List<TickStampedCommand>();
        private readonly ReadOnlyCollection<TickStampedCommand> _logView;

        /// <summary>Constructs a driver over <paramref name="queue"/>. Must not be null.</summary>
        public MatchClientDriver(ManagerCommandQueue queue)
        {
            _queue = queue ?? throw new ArgumentNullException(nameof(queue));
            // Wraps the live list (reflects appends) but is not castable back to a mutable List, so a
            // caller cannot mutate the log through the Log surface (the project's read-only-list rule).
            _logView = new ReadOnlyCollection<TickStampedCommand>(_log);
        }

        /// <summary>The enqueue-only command surface handed to the View.</summary>
        public ManagerCommandQueue Commands => _queue;

        /// <summary>The tick-stamped command log — the record a live match is reproducible from (§6.1). Read-only view; in-memory only (§11).</summary>
        public IReadOnlyList<TickStampedCommand> Log => _logView;

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
                command.Apply(mutations);
                _log.Add(new TickStampedCommand(appliedTick, in command));
            }
            _drainBuffer.Clear();
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial creation (P2): drain-and-apply Service (the pre-tick   |
// |         |            |        | hook body) + tick-stamped log + sim-side post-end drop.        |
#endregion
