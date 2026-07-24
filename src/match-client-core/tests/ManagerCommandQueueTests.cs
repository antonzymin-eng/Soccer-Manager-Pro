// File:     src/match-client-core/tests/ManagerCommandQueueTests.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P2/§6.4), Code Standards #20
// Purpose:  Head-less locks for the manager command queue: FIFO drain, thread-safe enqueue, and the
//           §6.4 invariant that the command set is exactly the three game commands (no playback kind).

using System;
using System.Collections.Generic;
using System.Threading;

using NUnit.Framework;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class ManagerCommandQueueTests
    {
        [Test]
        public void Kinds_AreExactlyThreeGameCommandsPlusNoneSentinel_NoPlaybackKind()
        {
            // §6.4: pause/speed are presentation pacing, never commands. The closed set is three game
            // kinds plus the None=0 zero-value sentinel (AR pass-1 Medium).
            ManagerCommandKind[] kinds = (ManagerCommandKind[])Enum.GetValues(typeof(ManagerCommandKind));
            CollectionAssert.AreEquivalent(
                new[]
                {
                    ManagerCommandKind.None,
                    ManagerCommandKind.SetTeamTactic,
                    ManagerCommandKind.SetPlayerTactic,
                    ManagerCommandKind.Substitute,
                },
                kinds);
            Assert.AreEqual(0, (int)ManagerCommandKind.None, "None is the zero value");
            Assert.AreNotEqual(0, (int)ManagerCommandKind.SetTeamTactic, "no game kind is the zero value");
            Assert.AreNotEqual(0, (int)ManagerCommandKind.SetPlayerTactic);
            Assert.AreNotEqual(0, (int)ManagerCommandKind.Substitute);
        }

        [Test]
        public void DefaultCommand_KindIsNone()
        {
            // A default(ManagerCommand) must read as the sentinel, not as a real mutator.
            Assert.AreEqual(ManagerCommandKind.None, default(ManagerCommand).Kind);
        }

        [Test]
        public void Enqueue_DefaultCommand_ThrowsFailLoud()
        {
            var queue = new ManagerCommandQueue();
            Assert.Throws<ArgumentException>(() => queue.Enqueue(default));
            Assert.AreEqual(0, queue.Count, "a refused command never enters the queue");
        }

        [Test]
        public void DrainInto_IsFifo_AndEmptiesQueue()
        {
            var queue = new ManagerCommandQueue();
            queue.Enqueue(ManagerCommand.Substitute(0, 3, 12, SubstitutionReason.Tactical));
            queue.Enqueue(ManagerCommand.SetPlayerTactic(7, PlayerTactic.Default(PlayerRole.Default)));
            queue.Enqueue(ManagerCommand.SetTeamTactic(1, TeamTactic.Balanced));
            Assert.AreEqual(3, queue.Count);

            var drained = new List<ManagerCommand>();
            int moved = queue.DrainInto(drained);

            Assert.AreEqual(3, moved);
            Assert.AreEqual(0, queue.Count, "queue is empty after drain");
            Assert.AreEqual(ManagerCommandKind.Substitute, drained[0].Kind);
            Assert.AreEqual(ManagerCommandKind.SetPlayerTactic, drained[1].Kind);
            Assert.AreEqual(ManagerCommandKind.SetTeamTactic, drained[2].Kind);
        }

        [Test]
        public void Enqueue_IsThreadSafe_AllCommandsCounted()
        {
            var queue = new ManagerCommandQueue();
            const int threads = 8;
            const int perThread = 500;
            var workers = new Thread[threads];
            for (int t = 0; t < threads; t++)
            {
                int teamId = t & 1;
                workers[t] = new Thread(() =>
                {
                    for (int i = 0; i < perThread; i++)
                    {
                        queue.Enqueue(ManagerCommand.SetTeamTactic(teamId, TeamTactic.Balanced));
                    }
                });
            }
            foreach (Thread w in workers) { w.Start(); }
            foreach (Thread w in workers) { w.Join(); }

            Assert.AreEqual(threads * perThread, queue.Count, "no enqueue lost under concurrency");
        }
    }
}
