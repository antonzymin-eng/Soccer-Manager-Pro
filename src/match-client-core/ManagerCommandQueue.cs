// File:     src/match-client-core/ManagerCommandQueue.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P2/§6.2),
//           Code Standards #20
// Purpose:  The thread-safe boundary between the UI thread (which enqueues typed game commands) and
//           the sim thread (which drains them at the top of a tick). Enqueue is the only surface the
//           View touches; the drain is internal, driven by MatchClientDriver on the sim thread.

using System;
using System.Collections.Generic;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// A lock-guarded FIFO of pending <see cref="ManagerCommand"/>s. The View calls <see cref="Enqueue"/>
    /// on the UI thread; the driver drains on the sim thread at the top of a tick. Only game commands
    /// enter here — playback pause/speed never do (§6.4).
    /// </summary>
    public sealed class ManagerCommandQueue
    {
        private readonly object _lock = new object();
        private readonly Queue<ManagerCommand> _pending = new Queue<ManagerCommand>();

        /// <summary>
        /// Enqueues a command (thread-safe; UI thread). Applied on the sim thread at the top of the next
        /// tick. An uninitialized (<see cref="ManagerCommandKind.None"/> / <c>default</c>) command is
        /// refused fail-loud here, at the misuse site, so it can never reach a mutator.
        /// </summary>
        /// <exception cref="ArgumentException"><paramref name="command"/> is the uninitialized default value.</exception>
        public void Enqueue(in ManagerCommand command)
        {
            if (command.Kind == ManagerCommandKind.None)
            {
                throw new ArgumentException(
                    "Cannot enqueue an uninitialized (default) ManagerCommand; construct it via a factory.",
                    nameof(command));
            }
            lock (_lock)
            {
                _pending.Enqueue(command);
            }
        }

        /// <summary>Number of commands not yet drained.</summary>
        public int Count
        {
            get { lock (_lock) { return _pending.Count; } }
        }

        /// <summary>
        /// Moves every pending command into <paramref name="dst"/> in FIFO order and empties the queue,
        /// returning the number moved. Sim-thread only, called by <see cref="MatchClientDriver"/>.
        /// </summary>
        internal int DrainInto(List<ManagerCommand> dst)
        {
            lock (_lock)
            {
                int moved = _pending.Count;
                while (_pending.Count > 0)
                {
                    dst.Add(_pending.Dequeue());
                }
                return moved;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial creation (P2): lock-guarded FIFO; UI-thread Enqueue +  |
// |         |            |        | sim-thread internal DrainInto.                                 |
// | 1.1     | 2026-07-24 | —      | AR pass-1 Medium: Enqueue refuses the None (default) command   |
// |         |            |        | fail-loud at the misuse site.                                  |
#endregion
