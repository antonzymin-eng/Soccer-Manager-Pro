// File:     src/match-client-core/TickStampedCommandReplay.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P6/§6.1/§6.2),
//           Code Standards #20
// Purpose:  Replays a tick-stamped command log against a head-less MatchSession — enqueuing each entry
//           immediately before the tick whose pre-tick CurrentTick equals its AppliedTick, so the drain
//           re-stamps it at exactly the tick it was originally applied at. This is the mechanism §6.1's
//           reproducibility invariant is defined against: the same MatchSetup + the same log re-ticks
//           byte-identically. In-memory only (§11) — persisting a log is separate Stage-1+ work.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// A cursor over a tick-stamped command log that drives a <see cref="MatchSession"/> head-lessly.
    ///
    /// <para><b>Why the log and not "the clicks".</b> The tick a live click lands on is a function of
    /// wall-clock scheduling, pause state and speed multiplier, so human input is not itself
    /// reproducible (§6.1). The log is: it records the tick each command was actually applied at, and
    /// replaying it re-applies each command at that same tick. Entries are enqueued just before
    /// <see cref="MatchSession.TickOnce"/> is called at their <see cref="TickStampedCommand.AppliedTick"/>,
    /// which is exactly where the original drain read <c>CurrentTick</c> — so a replayed run produces a
    /// log equal to the one it was driven from.</para>
    ///
    /// <para>Single-threaded, single-use forward cursor. Not thread-safe: it drives the session's tick
    /// loop, which is the head-less caller's own thread.</para>
    /// </summary>
    public sealed class TickStampedCommandReplay
    {
        private readonly TickStampedCommand[] _entries;
        private int _cursor;

        /// <summary>
        /// Builds a replay over <paramref name="log"/> (defensively copied). The log must be in
        /// non-decreasing applied-tick order — the order <see cref="MatchClientDriver.Log"/> always
        /// produces, since it appends on the sim thread as ticks advance. An out-of-order log is refused
        /// fail-loud rather than silently replayed with entries dropped at ticks already passed.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="log"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="log"/> is not in non-decreasing applied-tick order.</exception>
        public TickStampedCommandReplay(IReadOnlyList<TickStampedCommand> log)
        {
            ValidateNonDecreasing(log);

            _entries = new TickStampedCommand[log.Count];
            for (int i = 0; i < log.Count; i++)
            {
                _entries[i] = log[i];
            }
        }

        /// <summary>
        /// Builds a replay over the tail of <paramref name="log"/> stamped at or after
        /// <paramref name="fromTickInclusive"/> — the resume half of a save/restore round trip, where
        /// the state at the save tick already contains every command applied up to it.
        ///
        /// <para>The slice boundary is the caller's to choose, and it matters: a command enqueued at the
        /// save tick is inside the snapshot if it was drained before the capture and outside it if it
        /// arrived after, yet carries the same stamp either way (see
        /// <see cref="MatchSession.CaptureSave"/>). Save at a tick with no scheduled command and resume
        /// from <c>saveTick + 1</c>, and the boundary is unambiguous.</para>
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="log"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="log"/> is not in non-decreasing applied-tick order.</exception>
        public static TickStampedCommandReplay FromTick(
            IReadOnlyList<TickStampedCommand> log, ulong fromTickInclusive)
        {
            // Validated on the WHOLE log before slicing: an out-of-order source can filter into a
            // well-ordered tail, which would let the constructor's guard pass on a log that is not
            // actually replayable.
            ValidateNonDecreasing(log);

            var tail = new List<TickStampedCommand>(log.Count);
            for (int i = 0; i < log.Count; i++)
            {
                if (log[i].AppliedTick >= fromTickInclusive)
                {
                    tail.Add(log[i]);
                }
            }
            return new TickStampedCommandReplay(tail);
        }

        /// <summary>Entries not yet enqueued.</summary>
        public int PendingCount => _entries.Length - _cursor;

        /// <summary>True once every entry has been enqueued at its applied tick.</summary>
        public bool IsExhausted => _cursor >= _entries.Length;

        /// <summary>
        /// Ticks <paramref name="session"/> forward to <paramref name="targetTick"/>, enqueuing each
        /// pending entry immediately before the tick that starts at its applied tick. On return the
        /// session is at <paramref name="targetTick"/> and every entry stamped in
        /// <c>[startTick, targetTick)</c> has been applied; entries stamped at
        /// <paramref name="targetTick"/> or later stay pending, because their drain belongs to a tick
        /// this call does not run.
        /// </summary>
        /// <exception cref="ArgumentNullException"><paramref name="session"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="targetTick"/> is behind the session's current tick.</exception>
        /// <exception cref="InvalidOperationException">The next pending entry's applied tick has already passed.</exception>
        public void AdvanceTo(MatchSession session, ulong targetTick)
        {
            if (session == null) { throw new ArgumentNullException(nameof(session)); }

            ulong startTick = session.CurrentTick;
            if (targetTick < startTick)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(targetTick), targetTick,
                    "targetTick is behind the session's current tick ("
                        + startTick.ToString(CultureInfo.InvariantCulture)
                        + "); a replay advances a match, it cannot rewind one.");
            }

            // A pending entry stamped before the session's current tick can never be applied at its
            // recorded tick again. Silently skipping it would produce a run that is NOT the log's run
            // while still reporting success — precisely the divergence this class exists to rule out.
            if (_cursor < _entries.Length && _entries[_cursor].AppliedTick < startTick)
            {
                throw new InvalidOperationException(
                    "Replay entry " + _cursor.ToString(CultureInfo.InvariantCulture)
                        + " is stamped at tick "
                        + _entries[_cursor].AppliedTick.ToString(CultureInfo.InvariantCulture)
                        + " but the session is already at tick "
                        + startTick.ToString(CultureInfo.InvariantCulture)
                        + " — its application point has passed. Replay from the session's boot tick, or "
                        + "slice the log with FromTick to match where the session resumes.");
            }

            while (session.CurrentTick < targetTick)
            {
                ulong tick = session.CurrentTick;
                while (_cursor < _entries.Length && _entries[_cursor].AppliedTick == tick)
                {
                    // The drain reads CurrentTick at the top of the tick TickOnce is about to run, so a
                    // command enqueued here is stamped with exactly `tick` — the original stamp.
                    ManagerCommand command = _entries[_cursor].Command;
                    session.Commands.Enqueue(in command);
                    _cursor++;
                }
                session.TickOnce();
            }
        }

        private static void ValidateNonDecreasing(IReadOnlyList<TickStampedCommand> log)
        {
            if (log == null) { throw new ArgumentNullException(nameof(log)); }

            for (int i = 1; i < log.Count; i++)
            {
                if (log[i].AppliedTick < log[i - 1].AppliedTick)
                {
                    throw new ArgumentException(
                        "Tick-stamped log is not in non-decreasing applied-tick order: entry "
                            + i.ToString(CultureInfo.InvariantCulture) + " is stamped at tick "
                            + log[i].AppliedTick.ToString(CultureInfo.InvariantCulture)
                            + " after entry " + (i - 1).ToString(CultureInfo.InvariantCulture)
                            + " at tick "
                            + log[i - 1].AppliedTick.ToString(CultureInfo.InvariantCulture) + ".",
                        nameof(log));
                }
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P6): forward cursor that replays a           |
// |         |            |        | tick-stamped log against a head-less MatchSession, plus the    |
// |         |            |        | FromTick slice for resuming across a save/restore.             |
#endregion
