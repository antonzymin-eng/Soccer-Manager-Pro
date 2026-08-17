// File:     src/match-client-core/tests/MatchSessionTests.cs
// Created:  2026-07-24
// Modified: 2026-08-08
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P0/§5-P6/§6.3),
//           Code Standards #20
// Purpose:  Head-less locks for the composition root: a neutral session builds and wires the engine +
//           streamer + driver, an off-tick ServiceOnce() drains a queued command through the real
//           engine and stamps it in the log, and MatchSetup enforces the both-or-neither squad rule.
//           P6 adds locks for the head-less advance (TickOnce, and its fail-loud refusal to race a
//           Start()ed pacing loop), the §6.3 drained-empty-before-capture save invariant, and the
//           RestoreFrom factory. No Unity host and no background pacing thread — ServiceOnce and
//           TickOnce run on the test thread.

using System;

using NUnit.Framework;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;
using TacticalDirector.PlayerDatabase;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class MatchSessionTests
    {
        private const ulong Seed = 0xA11CE5EEDUL;

        [Test]
        public void NeutralDemo_Builds_AndExposesSurfaces()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));

            Assert.IsNotNull(session.Streamer);
            Assert.IsNotNull(session.Driver);
            Assert.IsNotNull(session.Commands);
            Assert.AreEqual(0, session.Driver.Log.Count, "nothing serviced yet");
            Assert.IsFalse(session.TryGetLatestFrame(out _), "no frame before the first tick");
        }

        [Test]
        public void ServiceOnce_DrainsQueuedCommand_ThroughRealEngine_AndLogsAtTickZero()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));

            session.Commands.Enqueue(ManagerCommand.SetTeamTactic(0, TeamTactic.Balanced));
            session.ServiceOnce();

            Assert.AreEqual(1, session.Driver.Log.Count);
            Assert.AreEqual(0UL, session.Driver.Log[0].AppliedTick, "no tick has advanced");
            Assert.AreEqual(ManagerCommandKind.SetTeamTactic, session.Driver.Log[0].Command.Kind);
            Assert.AreEqual(0, session.Commands.Count, "queue drained");
        }

        [Test]
        public void ServiceOnce_EmptyQueue_IsNoOp()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            session.ServiceOnce();
            Assert.AreEqual(0, session.Driver.Log.Count);
        }

        [Test]
        public void GkHeadingEnabled_Setup_Builds_AndServicesACommand()
        {
            var setup = new MatchSetup(Seed, gkHeadingEnabled: true);
            var session = new MatchSession(setup);

            session.Commands.Enqueue(ManagerCommand.SetPlayerTactic(4, PlayerTactic.Default(PlayerRole.Default)));
            session.ServiceOnce();

            Assert.AreEqual(1, session.Driver.Log.Count);
            Assert.AreEqual(ManagerCommandKind.SetPlayerTactic, session.Driver.Log[0].Command.Kind);
        }

        [Test]
        public void Constructor_NullSetup_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MatchSession(null));
        }

        [Test]
        public void MatchSetup_SingleSquad_Throws_BothOrNeither()
        {
            var oneSquad = new Squad(1, new[] { PlayerRecord.CreateDefault(0) });

            Assert.Throws<ArgumentException>(() => new MatchSetup(Seed, homeSquad: oneSquad, awaySquad: null));
            Assert.Throws<ArgumentException>(() => new MatchSetup(Seed, homeSquad: null, awaySquad: oneSquad));
        }

        // ── P6: the head-less advance ────────────────────────────────────────────────────────────────

        [Test]
        public void TickOnce_AdvancesTheEngine_AndPublishesAFrame()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));

            LiveMatchFrame frame = session.TickOnce();

            Assert.AreEqual(1UL, session.CurrentTick);
            Assert.AreEqual(1UL, frame.Tick);
            Assert.IsTrue(session.TryGetLatestFrame(out _), "a frame exists after the first tick");
        }

        [Test]
        public void TickOnce_RunsTheCommandDrain_StampingAtThePreTickTick()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            session.TickOnce();
            session.TickOnce();

            session.Commands.Enqueue(ManagerCommand.SetTeamTactic(0, TeamTactic.Balanced));
            session.TickOnce();

            Assert.AreEqual(1, session.Driver.Log.Count);
            Assert.AreEqual(2UL, session.Driver.Log[0].AppliedTick,
                "the drain reads CurrentTick at the top of the tick, before RunTick advances it");
            Assert.AreEqual(3UL, session.CurrentTick);
        }

        [Test]
        public void TickOnce_AfterStart_Throws_RatherThanRacingThePacingLoop()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            session.Start();
            try
            {
                Assert.Throws<InvalidOperationException>(() => session.TickOnce());
            }
            finally
            {
                session.Stop();
            }
        }

        [Test]
        public void CurrentSnapshotDigest_AdvancesWithTheChain()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            session.TickOnce();
            byte[] first = session.CurrentSnapshotDigest;
            session.TickOnce();
            byte[] second = session.CurrentSnapshotDigest;

            CollectionAssert.AreNotEqual(first, second, "the chained digest must change every tick");
        }

        // ── P6: durable capture on the ServiceOnce seam (§6.3) ───────────────────────────────────────

        [Test]
        public void CaptureSave_DrainsTheQueueBeforeCapturing()
        {
            // The §6.3 invariant. If the capture ran before the drain, this command would be neither in
            // the snapshot nor in the queue of the restored session — silently lost.
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            for (int i = 0; i < 20; i++) { session.TickOnce(); }

            session.Commands.Enqueue(ManagerCommand.SetTeamTactic(0, TeamTactic.Balanced));
            byte[] blob = session.CaptureSave();

            Assert.IsNotNull(blob);
            Assert.AreEqual(0, session.Commands.Count, "the queue is drained empty by the capture pass");
            Assert.AreEqual(1, session.Driver.Log.Count, "the queued command was applied, not dropped");
            Assert.AreEqual(20UL, session.Driver.Log[0].AppliedTick);
        }

        [Test]
        public void CaptureSave_WhilePaused_StillCaptures()
        {
            // ServiceOnce reaches the shared hook body even when nothing is ticking — the AR-7 H-1 fix.
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            session.TickOnce();
            session.Streamer.Pause();

            Assert.IsNotNull(session.CaptureSave());
        }

        [Test]
        public void RestoreFrom_ResumesAtTheSavedTick_WithAFreshCommandLog()
        {
            var session = new MatchSession(MatchSetup.NeutralDemo(Seed));
            for (int i = 0; i < 15; i++) { session.TickOnce(); }
            session.Commands.Enqueue(ManagerCommand.SetTeamTactic(1, TeamTactic.Balanced));
            byte[] blob = session.CaptureSave();

            MatchSession restored = MatchSession.RestoreFrom(blob);

            Assert.AreEqual(15UL, restored.CurrentTick);
            Assert.AreEqual(0, restored.Driver.Log.Count,
                "the tick-stamped log is in-memory only and is not carried across a save (§11)");
            Assert.AreEqual(0, restored.Commands.Count);

            // And it is a live, drivable composition, not a read-only shell.
            restored.Commands.Enqueue(ManagerCommand.SetTeamTactic(0, TeamTactic.Balanced));
            restored.TickOnce();
            Assert.AreEqual(16UL, restored.CurrentTick);
            Assert.AreEqual(1, restored.Driver.Log.Count);
            Assert.AreEqual(15UL, restored.Driver.Log[0].AppliedTick);
        }

        [Test]
        public void RestoreFrom_NullBlob_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => MatchSession.RestoreFrom(null));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-07-24 | —            | Initial file. |
// | 1.1     | 2026-08-03 | —            | Substantive edit; no version-history row was recorded for it at the time (FR-CS-058 gap, predates this hygiene pass). |
// | 1.2     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
