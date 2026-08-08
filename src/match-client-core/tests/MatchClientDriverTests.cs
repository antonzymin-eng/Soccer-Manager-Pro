// File:     src/match-client-core/tests/MatchClientDriverTests.cs
// Created:  2026-07-24
// Modified: 2026-08-08
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P2/§6), Code Standards #20
// Purpose:  Head-less locks for the deterministic command drain (§5-P2's "core new work"): FIFO apply
//           order, per-batch tick-stamping, the sim-side post-MatchEnded drop, and log-driven
//           determinism — all proven against a fake mutation surface, no Unity host / real engine.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class MatchClientDriverTests
    {
        private static MatchClientDriver NewDriver() => new MatchClientDriver(new ManagerCommandQueue());

        [Test]
        public void DefaultCommand_Apply_ThrowsFailLoud()
        {
            // Defense in depth: even bypassing the queue, applying a default command fails loud
            // instead of staging a malformed team tactic (AR pass-1 Medium).
            var m = new RecordingMutations();
            Assert.Throws<InvalidOperationException>(() => default(ManagerCommand).Apply(m));
            Assert.AreEqual(0, m.Calls.Count);
        }

        [Test]
        public void Service_EmptyQueue_IsNoOp()
        {
            MatchClientDriver driver = NewDriver();
            var m = new RecordingMutations { CurrentTickValue = 5UL };

            driver.Service(m);

            Assert.AreEqual(0, m.Calls.Count);
            Assert.AreEqual(0, driver.Log.Count);
        }

        [Test]
        public void Service_AppliesFifo_AndStampsBatchTick()
        {
            MatchClientDriver driver = NewDriver();
            driver.Commands.Enqueue(ManagerCommand.SetTeamTactic(0, TeamTactic.Balanced));
            driver.Commands.Enqueue(ManagerCommand.SetPlayerTactic(9, PlayerTactic.Default(PlayerRole.Default)));
            driver.Commands.Enqueue(ManagerCommand.Substitute(1, 4, 13, SubstitutionReason.Injury));

            var m = new RecordingMutations { CurrentTickValue = 42UL };
            driver.Service(m);

            // FIFO apply order through the mutation surface.
            CollectionAssert.AreEqual(new[] { "team:0", "player:9", "sub:1:4:13:Injury" }, m.Calls);

            // Every command in one drain is stamped at the same tick (read once at the batch top).
            Assert.AreEqual(3, driver.Log.Count);
            Assert.AreEqual(42UL, driver.Log[0].AppliedTick);
            Assert.AreEqual(42UL, driver.Log[1].AppliedTick);
            Assert.AreEqual(42UL, driver.Log[2].AppliedTick);
            Assert.AreEqual(ManagerCommandKind.SetTeamTactic, driver.Log[0].Command.Kind);
            Assert.AreEqual(ManagerCommandKind.SetPlayerTactic, driver.Log[1].Command.Kind);
            Assert.AreEqual(ManagerCommandKind.Substitute, driver.Log[2].Command.Kind);
        }

        [Test]
        public void Service_CommandRefusedByEngine_IsIsolated_BatchContinues_NoEscape()
        {
            // A mutator that fails loud mid-batch must not escape Service (which would kill the pacing
            // thread) and must not stop the rest of the batch (AR pass-2 Medium).
            MatchClientDriver driver = NewDriver();
            driver.Commands.Enqueue(ManagerCommand.SetTeamTactic(0, TeamTactic.Balanced));
            driver.Commands.Enqueue(ManagerCommand.SetPlayerTactic(5, PlayerTactic.Default(PlayerRole.Default)));  // refused
            driver.Commands.Enqueue(ManagerCommand.Substitute(1, 2, 12, SubstitutionReason.Tactical));

            var m = new RecordingMutations { CurrentTickValue = 8UL, ThrowOnSetPlayerAgentId = 5 };
            Assert.DoesNotThrow(() => driver.Service(m));

            // The good commands before and AFTER the refused one both applied.
            CollectionAssert.AreEqual(new[] { "team:0", "sub:1:2:12:Tactical" }, m.Calls);
            Assert.AreEqual(2, driver.Log.Count, "only the applied commands are logged");
            Assert.AreEqual(1, driver.FailedCommands.Count, "the refused command is recorded, not vanished");
            Assert.AreEqual(ManagerCommandKind.SetPlayerTactic, driver.FailedCommands[0].Command.Kind);
            Assert.AreEqual(8UL, driver.FailedCommands[0].AppliedTick);
        }

        [Test]
        public void Service_SeparateBatches_StampedAtTheirOwnTicks()
        {
            MatchClientDriver driver = NewDriver();
            var m = new RecordingMutations();

            driver.Commands.Enqueue(ManagerCommand.SetTeamTactic(0, TeamTactic.Balanced));
            m.CurrentTickValue = 10UL;
            driver.Service(m);

            driver.Commands.Enqueue(ManagerCommand.SetTeamTactic(1, TeamTactic.Balanced));
            m.CurrentTickValue = 11UL;
            driver.Service(m);

            Assert.AreEqual(2, driver.Log.Count);
            Assert.AreEqual(10UL, driver.Log[0].AppliedTick);
            Assert.AreEqual(11UL, driver.Log[1].AppliedTick);
        }

        [Test]
        public void Service_AfterMatchEnded_DropsAll_NotAppliedNotLogged()
        {
            MatchClientDriver driver = NewDriver();
            driver.Commands.Enqueue(ManagerCommand.SetTeamTactic(0, TeamTactic.Balanced));
            driver.Commands.Enqueue(ManagerCommand.Substitute(1, 2, 12, SubstitutionReason.Tactical));

            var m = new RecordingMutations { CurrentTickValue = 100UL, MatchEndedValue = true };
            driver.Service(m);

            Assert.AreEqual(0, m.Calls.Count, "a finished match is never mutated (§6.2)");
            Assert.AreEqual(0, driver.Log.Count, "dropped commands never enter the log");
            Assert.AreEqual(0, driver.Commands.Count, "and are drained off the queue, not stranded");
        }

        [Test]
        public void TwoDrivers_SameSequenceSameTicks_ProduceIdenticalLogs()
        {
            // Reproducibility is defined against the (appliedTick, command) sequence (§6.1): the same
            // enqueue order serviced at the same ticks yields byte-for-byte the same log.
            List<TickStampedCommand> logA = RunScriptedSession();
            List<TickStampedCommand> logB = RunScriptedSession();

            Assert.AreEqual(logA.Count, logB.Count);
            for (int i = 0; i < logA.Count; i++)
            {
                Assert.AreEqual(logA[i].AppliedTick, logB[i].AppliedTick);
                Assert.AreEqual(logA[i].Command.Kind, logB[i].Command.Kind);
                Assert.AreEqual(logA[i].Command.TeamId, logB[i].Command.TeamId);
                Assert.AreEqual(logA[i].Command.AgentId, logB[i].Command.AgentId);
                Assert.AreEqual(logA[i].Command.OutSlotIndex, logB[i].Command.OutSlotIndex);
                Assert.AreEqual(logA[i].Command.BenchIndex, logB[i].Command.BenchIndex);
                Assert.AreEqual(logA[i].Command.Reason, logB[i].Command.Reason);
            }
        }

        private static List<TickStampedCommand> RunScriptedSession()
        {
            MatchClientDriver driver = NewDriver();
            var m = new RecordingMutations();

            driver.Commands.Enqueue(ManagerCommand.SetTeamTactic(0, TeamTactic.Balanced));
            driver.Commands.Enqueue(ManagerCommand.SetPlayerTactic(3, PlayerTactic.Default(PlayerRole.Default)));
            m.CurrentTickValue = 7UL;
            driver.Service(m);

            driver.Commands.Enqueue(ManagerCommand.Substitute(1, 5, 14, SubstitutionReason.Tactical));
            m.CurrentTickValue = 20UL;
            driver.Service(m);

            return new List<TickStampedCommand>(driver.Log);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-07-24 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
