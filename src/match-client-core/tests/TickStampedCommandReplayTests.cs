// File:     src/match-client-core/tests/TickStampedCommandReplayTests.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P6/§6.1/§9),
//           Code Standards #20
// Purpose:  Head-less locks for the log-driven replay cursor: a replayed command is re-stamped at the
//           tick it was recorded at, entries at the target tick stay pending for the next call, an
//           out-of-order log and a passed application point are refused fail-loud, and FromTick slices
//           the resume tail. No Unity host and no pacing thread — TickOnce runs on the test thread.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    internal sealed class TickStampedCommandReplayTests
    {
        private const ulong Seed = 0x0C1E27A5D6B94E11UL;

        private static TickStampedCommand Stamp(ulong tick, ManagerCommand command) =>
            new TickStampedCommand(tick, in command);

        private static ManagerCommand HomeTactic() =>
            ManagerCommand.SetTeamTactic(0, TeamTactic.Balanced);

        private static ManagerCommand AwayTactic() =>
            ManagerCommand.SetTeamTactic(1, TeamTactic.Balanced);

        [Test]
        public void AdvanceTo_ReStampsEachCommandAtItsRecordedTick()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            var replay = new TickStampedCommandReplay(new[]
            {
                Stamp(0, HomeTactic()),
                Stamp(3, AwayTactic()),
                Stamp(3, HomeTactic()),
                Stamp(7, AwayTactic()),
            });

            replay.AdvanceTo(session, 10);

            IReadOnlyList<TickStampedCommand> log = session.Driver.Log;
            Assert.AreEqual(4, log.Count);
            Assert.AreEqual(0UL, log[0].AppliedTick);
            Assert.AreEqual(3UL, log[1].AppliedTick);
            Assert.AreEqual(3UL, log[2].AppliedTick, "a same-tick batch is applied in FIFO order at one tick");
            Assert.AreEqual(7UL, log[3].AppliedTick);
            Assert.AreEqual(10UL, session.CurrentTick);
            Assert.IsTrue(replay.IsExhausted);
            Assert.AreEqual(0, session.Driver.FailedCommands.Count);
        }

        [Test]
        public void AdvanceTo_LeavesAnEntryStampedAtTheTargetTickPending()
        {
            // The drain for tick T runs inside the tick that advances T -> T+1, so an entry stamped at
            // the target tick belongs to a tick this call does not run. Applying it early would shift
            // its stamp and silently desynchronise a resumed replay.
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            var replay = new TickStampedCommandReplay(new[] { Stamp(5, HomeTactic()) });

            replay.AdvanceTo(session, 5);

            Assert.AreEqual(5UL, session.CurrentTick);
            Assert.AreEqual(0, session.Driver.Log.Count, "not applied yet");
            Assert.AreEqual(1, replay.PendingCount);

            replay.AdvanceTo(session, 6);

            Assert.AreEqual(1, session.Driver.Log.Count);
            Assert.AreEqual(5UL, session.Driver.Log[0].AppliedTick);
            Assert.IsTrue(replay.IsExhausted);
        }

        [Test]
        public void AdvanceTo_IsIncremental_AcrossRepeatedCalls()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            var replay = new TickStampedCommandReplay(new[]
            {
                Stamp(1, HomeTactic()),
                Stamp(4, AwayTactic()),
            });

            for (ulong t = 0; t < 6; t++)
            {
                replay.AdvanceTo(session, t + 1);
            }

            Assert.AreEqual(6UL, session.CurrentTick);
            Assert.AreEqual(2, session.Driver.Log.Count);
            Assert.AreEqual(1UL, session.Driver.Log[0].AppliedTick);
            Assert.AreEqual(4UL, session.Driver.Log[1].AppliedTick);
        }

        [Test]
        public void Constructor_OutOfOrderLog_Throws()
        {
            Assert.Throws<ArgumentException>(() => new TickStampedCommandReplay(new[]
            {
                Stamp(5, HomeTactic()),
                Stamp(2, AwayTactic()),
            }));
        }

        [Test]
        public void Constructor_NullLog_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new TickStampedCommandReplay(null));
        }

        [Test]
        public void AdvanceTo_TargetBehindCurrentTick_Throws()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            var replay = new TickStampedCommandReplay(Array.Empty<TickStampedCommand>());
            replay.AdvanceTo(session, 4);

            Assert.Throws<ArgumentOutOfRangeException>(() => replay.AdvanceTo(session, 2));
        }

        [Test]
        public void AdvanceTo_EntryWhoseApplicationPointHasPassed_Throws()
        {
            // Silently skipping it would produce a run that is NOT the log's run while still reporting
            // success — the divergence the replay exists to rule out.
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            var replay = new TickStampedCommandReplay(new[] { Stamp(2, HomeTactic()) });

            var empty = new TickStampedCommandReplay(Array.Empty<TickStampedCommand>());
            empty.AdvanceTo(session, 6);

            Assert.Throws<InvalidOperationException>(() => replay.AdvanceTo(session, 10));
        }

        [Test]
        public void FromTick_KeepsOnlyTheResumeTail()
        {
            var log = new[]
            {
                Stamp(1, HomeTactic()),
                Stamp(5, AwayTactic()),
                Stamp(9, HomeTactic()),
            };

            TickStampedCommandReplay tail = TickStampedCommandReplay.FromTick(log, 5);

            Assert.AreEqual(2, tail.PendingCount);

            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            new TickStampedCommandReplay(Array.Empty<TickStampedCommand>()).AdvanceTo(session, 5);
            tail.AdvanceTo(session, 12);

            Assert.AreEqual(2, session.Driver.Log.Count);
            Assert.AreEqual(5UL, session.Driver.Log[0].AppliedTick);
            Assert.AreEqual(9UL, session.Driver.Log[1].AppliedTick);
        }

        [Test]
        public void FromTick_ValidatesTheWholeLog_NotJustTheSlice()
        {
            // An out-of-order source can filter into a well-ordered tail; validating only the tail would
            // let a log that is not actually replayable through.
            var log = new[]
            {
                Stamp(9, HomeTactic()),
                Stamp(1, AwayTactic()),
                Stamp(11, HomeTactic()),
            };

            Assert.Throws<ArgumentException>(() => TickStampedCommandReplay.FromTick(log, 9));
        }

        [Test]
        public void AdvanceTo_NullSession_Throws()
        {
            var replay = new TickStampedCommandReplay(Array.Empty<TickStampedCommand>());
            Assert.Throws<ArgumentNullException>(() => replay.AdvanceTo(null, 1));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-08-03 | —      | Initial implementation. |
#endregion
