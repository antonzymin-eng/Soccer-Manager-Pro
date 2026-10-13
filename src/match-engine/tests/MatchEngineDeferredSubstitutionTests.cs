// File:     src/match-engine/tests/MatchEngineDeferredSubstitutionTests.cs
// Created:  2026-10-13
// Spec:     Match Flow Completion §6, Code Standards #20
// Purpose:  Acceptance locks for deferred substitution requests and deterministic restart execution.

using System;
using NUnit.Framework;
using UnityEngine;
using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineDeferredSubstitutionTests
    {
        private const ulong Seed = 0xC00FEE12345678ABUL;
        private static readonly Vector2 RestartSpot = new Vector2(52.5f, 34f);

        private static MatchEngine Started()
        {
            var e = new MatchEngine(Seed);
            e.RunTick();
            return e;
        }

        private static SubstitutionRequestResult Request(MatchEngine e, int slot, int bench) =>
            e.RequestSubstitution(0, slot, bench, SubstitutionReason.Tactical);

        [Test]
        public void OpenPlayRequestChangesNoRosterStateUntilRestart()
        {
            MatchEngine e = Started();
            Assert.AreEqual(-1, e.TestOnly_ActiveBenchSlot(1));
            SubstitutionRequestResult result = Request(e, 1, 0);
            Assert.IsTrue(result.IsAccepted);
            Assert.AreEqual(1, e.PendingSubstitutionCount);
            Assert.AreEqual(-1, e.TestOnly_ActiveBenchSlot(1));
            Assert.AreEqual(0, e.TestOnly_SubstitutionsUsed(0));

            e.RunTick();
            Assert.AreEqual(-1, e.TestOnly_ActiveBenchSlot(1),
                "An unrelated ordinary tick is not a substitution stoppage.");
            e.TestOnly_ApplyRestart(RestartSpot, 0);
            Assert.AreEqual(0, e.TestOnly_ActiveBenchSlot(1));
            Assert.AreEqual(0, e.PendingSubstitutionCount);
            Assert.AreEqual(1, e.TestOnly_SubstitutionsUsed(0));
            Assert.AreEqual(1, e.SubstitutionWindowsUsed(0));
        }

        [Test]
        public void TwoRequestsExecuteInSequenceUsingOneTeamWindow()
        {
            MatchEngine e = Started();
            SubstitutionRequestResult first = Request(e, 1, 0);
            SubstitutionRequestResult second = Request(e, 2, 1);
            Assert.IsTrue(first.IsAccepted);
            Assert.IsTrue(second.IsAccepted);
            Assert.Greater(second.Sequence, first.Sequence);
            Assert.AreEqual(first.Sequence, e.GetPendingSubstitution(0).Sequence);
            Assert.AreEqual(second.Sequence, e.GetPendingSubstitution(1).Sequence);
            e.TestOnly_ApplyRestart(RestartSpot, 0);
            Assert.AreEqual(0, e.TestOnly_ActiveBenchSlot(1));
            Assert.AreEqual(1, e.TestOnly_ActiveBenchSlot(2));
            Assert.AreEqual(2, e.TestOnly_SubstitutionsUsed(0));
            Assert.AreEqual(1, e.SubstitutionWindowsUsed(0));
        }

        [Test]
        public void DuplicateSlotAndBenchRefusalsLeaveQueueUnchanged()
        {
            MatchEngine e = Started();
            Assert.IsTrue(Request(e, 1, 0).IsAccepted);
            Assert.AreEqual(SubstitutionRequestStatus.OutgoingAlreadyPending, Request(e, 1, 1).Status);
            Assert.AreEqual(SubstitutionRequestStatus.BenchPlayerUnavailable, Request(e, 2, 0).Status);
            Assert.AreEqual(1, e.PendingSubstitutionCount);
            Assert.AreEqual(0, e.TestOnly_SubstitutionsUsed(0));
        }

        [Test]
        public void ThreeUsedWindowsHoldNextFirstHalfRequest()
        {
            MatchEngine e = Started();
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(Request(e, 1 + i, i).IsAccepted);
                e.TestOnly_ApplyRestart(RestartSpot, 0);
            }
            Assert.AreEqual(3, e.SubstitutionWindowsUsed(0));
            Assert.IsTrue(Request(e, 4, 3).IsAccepted,
                "Before half-time, a request waits for the free window.");
            e.TestOnly_ApplyRestart(RestartSpot, 0);
            Assert.AreEqual(1, e.PendingSubstitutionCount);
            Assert.AreEqual(-1, e.TestOnly_ActiveBenchSlot(4));
            Assert.AreEqual(3, e.SubstitutionWindowsUsed(0));
        }

        [Test]
        public void DismissedOutgoingCancelsBeforeTheWindowGate()
        {
            MatchEngine e = Started();
            Assert.IsTrue(Request(e, 1, 0).IsAccepted);
            e.TestOnly_SetIsSentOff(1, true);
            e.TestOnly_ApplyRestart(RestartSpot, 0);
            Assert.AreEqual(0, e.PendingSubstitutionCount);
            Assert.AreEqual(-1, e.TestOnly_ActiveBenchSlot(1));
            Assert.AreEqual(0, e.TestOnly_SubstitutionsUsed(0));
            Assert.AreEqual(0, e.SubstitutionWindowsUsed(0));
            Assert.IsTrue(Request(e, 2, 0).IsAccepted,
                "Cancelled requests must immediately release their reserved bench player.");
        }

        [Test]
        public void KeeperMismatchRefusedAndSentOffKeeperEnablesBenchKeeperFallback()
        {
            MatchEngine e = Started();
            e.TestOnly_SetBenchSlot(0, 0, isGoalkeeper: true);
            Assert.AreEqual(SubstitutionRequestStatus.GoalkeeperRoleMismatch,
                Request(e, 1, 0).Status);
            e.TestOnly_SetIsSentOff(0, true);
            Assert.IsTrue(Request(e, 1, 0).IsAccepted);
            e.TestOnly_ApplyRestart(RestartSpot, 0);
            Assert.IsTrue(e.TestOnly_IsGoalkeeper(1));
            Assert.AreEqual(0, e.TestOnly_ActiveBenchSlot(1));
        }

        [Test]
        public void AcceptedOffTickRequestCannotBeSilentlyOmittedFromSave()
        {
            MatchEngine e = Started();
            Assert.IsTrue(Request(e, 1, 0).IsAccepted);
            Assert.Throws<InvalidOperationException>(() => e.CaptureDurableHeader());
            Assert.Throws<InvalidOperationException>(() => e.CaptureDurablePayload());
            e.RunTick();
            Assert.DoesNotThrow(() => e.CaptureDurableHeader());
            Assert.Greater(e.CaptureDurablePayload().BytesWritten, 0);
        }

        [Test]
        public void PendingQueueAndWindowsRoundTripViaCanonicalSnapshot()
        {
            MatchEngine original = Started();
            Assert.IsTrue(Request(original, 1, 0).IsAccepted);
            original.RunTick(); // capture after the accepted request entered the canonical frame
            SnapshotHeader header = original.CaptureDurableHeader();
            SnapshotPayload payload = original.CaptureDurablePayload();
            MatchEngine restored = MatchEngine.RestoreFromSnapshot(header, payload, Seed);
            Assert.AreEqual(1, restored.PendingSubstitutionCount);
            Assert.AreEqual(original.GetPendingSubstitution(0).Sequence,
                restored.GetPendingSubstitution(0).Sequence);
            Assert.AreEqual(0, restored.SubstitutionWindowsUsed(0));

            original.TestOnly_ApplyRestart(RestartSpot, 0);
            restored.TestOnly_ApplyRestart(RestartSpot, 0);
            Assert.AreEqual(original.TestOnly_ActiveBenchSlot(1), restored.TestOnly_ActiveBenchSlot(1));
            Assert.AreEqual(original.SubstitutionWindowsUsed(0), restored.SubstitutionWindowsUsed(0));
        }

        [Test]
        public void MidplayRequestChangesNextTickDigest()
        {
            MatchEngine a = Started();
            MatchEngine b = Started();
            Assert.IsTrue(Request(b, 1, 0).IsAccepted);
            a.RunTick();
            b.RunTick();
            CollectionAssert.AreNotEqual(a.CurrentSnapshotDigest, b.CurrentSnapshotDigest,
                "The pending FIFO must be part of the v25 canonical state.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                  |
// | 1.0     | 2026-10-13 | —      | Nine deferred substitution test locks. |
#endregion
