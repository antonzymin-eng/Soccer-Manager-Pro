// File:     src/match-engine/tests/MatchEngineDeferredSubstitutionTests.cs
// Created:  2026-10-13
// Spec:     Match Flow Completion §6, Code Standards #20
// Purpose:  Acceptance locks for deferred substitution requests and deterministic restart execution.

using System;
using NUnit.Framework;
using UnityEngine;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;

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
        public void ExhaustedWindowsExecuteAtHalfTimeButRefuseSecondHalfRequests()
        {
            MatchEngine e = Started();
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(Request(e, 1 + i, i).IsAccepted);
                e.TestOnly_ApplyRestart(RestartSpot, 0);
            }
            Assert.IsTrue(Request(e, 4, 3).IsAccepted);
            e.TestOnly_SetPhysicsFrame((int)MatchEngineConstants.HALF_TIME_BOUNDARY_TICK - 1);
            e.RunTick();
            Assert.IsTrue(e.TestOnly_SecondHalfStarted);
            Assert.AreEqual(0, e.PendingSubstitutionCount);
            Assert.AreEqual(3, e.TestOnly_ActiveBenchSlot(4));
            Assert.AreEqual(3, e.SubstitutionWindowsUsed(0),
                "The half-time kickoff is a free substitution window.");
            Assert.AreEqual(SubstitutionRequestStatus.NoWindowsLeft,
                Request(e, 5, 4).Status);
        }

        [Test]
        public void FullTimeRejectsRequestsWithTypedResult()
        {
            MatchEngine e = Started();
            e.TestOnly_SetPhysicsFrame((int)MatchEngineConstants.MATCH_TICKS_TOTAL - 1);
            e.RunTick();
            Assert.AreEqual(SubstitutionRequestStatus.MatchEnded, Request(e, 1, 0).Status);
            Assert.AreEqual(0, e.PendingSubstitutionCount);
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

        [TestCase(0)]
        [TestCase(1)]
        public void FallbackKeeperImmediatelyBecomesGoalkeeperForElevenAndPositioning(int team)
        {
            MatchEngine e = Started();
            int keeper = team * MatchEngineConstants.PLAYERS_PER_TEAM;
            int outgoing = keeper + 1;
            e.TestOnly_SetBenchSlot(team, 0, isGoalkeeper: true);
            e.TestOnly_SetIsSentOff(keeper, true);
            Assert.IsTrue(e.RequestSubstitution(team, outgoing, 0, SubstitutionReason.Tactical).IsAccepted);
            Assert.AreEqual(1, e.PendingSubstitutionCount);
            e.TestOnly_ApplyRestart(RestartSpot, team);

            Assert.AreEqual(outgoing, e.TestOnly_GoalkeeperAgentId(team),
                "#11 must not retain the sent-off slot until the next tick.");
            Assert.AreEqual(outgoing, e.TestOnly_FirstEligibleGoalkeeper(team),
                "The loose-ball collector restriction must use the replacement keeper.");
            Assert.AreEqual(0, e.TestOnly_ActiveBenchSlot(outgoing));
            for (int i = 0; i < 7; i++)
                e.RunTick();
            Assert.AreEqual(TacticalDirector.PositioningAI.RoleId.GK,
                e.TestOnly_PositioningRole(outgoing),
                "Positioning must use the keeper role, not the old outfield formation slot.");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void KeeperReplacementRulesRejectOutfielderForKeeperAndSecondFallback(int team)
        {
            MatchEngine e = Started();
            int keeper = team * MatchEngineConstants.PLAYERS_PER_TEAM;
            int firstOut = keeper + 1;
            int secondOut = keeper + 2;
            Assert.AreEqual(SubstitutionRequestStatus.GoalkeeperRoleMismatch,
                e.RequestSubstitution(team, keeper, 0, SubstitutionReason.Tactical).Status);
            e.TestOnly_SetBenchSlot(team, 0, isGoalkeeper: true);
            e.TestOnly_SetBenchSlot(team, 1, isGoalkeeper: true);
            Assert.AreEqual(SubstitutionRequestStatus.GoalkeeperRoleMismatch,
                e.RequestSubstitution(team, firstOut, 0, SubstitutionReason.Tactical).Status);
            e.TestOnly_SetIsSentOff(keeper, true);
            Assert.IsTrue(e.RequestSubstitution(team, firstOut, 0, SubstitutionReason.Tactical).IsAccepted);
            Assert.AreEqual(SubstitutionRequestStatus.GoalkeeperRoleMismatch,
                e.RequestSubstitution(team, secondOut, 1, SubstitutionReason.Tactical).Status);
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
        public void PreKickoffRequestAppliesImmediatelyWithNoWindow()
        {
            var e = new MatchEngine(Seed);
            Assert.IsTrue(Request(e, 1, 0).IsAccepted);
            Assert.AreEqual(0, e.PendingSubstitutionCount);
            Assert.AreEqual(0, e.TestOnly_ActiveBenchSlot(1));
            Assert.AreEqual(1, e.TestOnly_SubstitutionsUsed(0));
            Assert.AreEqual(0, e.SubstitutionWindowsUsed(0));
            Assert.DoesNotThrow(() => e.RunTick());
        }

        [Test]
        public void FullTimeRemovesPreviouslyAcceptedUnexecutedRequest()
        {
            MatchEngine e = Started();
            // Reach second half first. Jumping directly to full time from the first
            // half also runs the free half-time restart, which correctly executes
            // the queue and cannot represent a full-time cancellation fixture.
            e.TestOnly_SetPhysicsFrame((int)MatchEngineConstants.HALF_TIME_BOUNDARY_TICK - 1);
            e.RunTick();
            Assert.IsTrue(e.TestOnly_SecondHalfStarted);
            Assert.IsTrue(Request(e, 1, 0).IsAccepted);
            e.TestOnly_SetPossession(0);
            e.TestOnly_SetBall(TacticalDirector.BallPhysics.BallState.CreateAtPosition(
                new Vector3(RestartSpot.x, RestartSpot.y, MatchEngineConstants.BALL_REST_HEIGHT_M)));
            e.TestOnly_SetPhysicsFrame((int)MatchEngineConstants.MATCH_TICKS_TOTAL - 1);
            e.RunTick();
            Assert.IsTrue(e.MatchEnded);
            Assert.AreEqual(0, e.PendingSubstitutionCount);
            Assert.AreEqual(-1, e.TestOnly_ActiveBenchSlot(1));
            Assert.AreEqual(0, e.TestOnly_SubstitutionsUsed(0));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void MirroredTeamsExecuteAndCancelOnlyOwnRequests(int team)
        {
            MatchEngine e = Started();
            int outgoing = team * MatchEngineConstants.PLAYERS_PER_TEAM + 1;
            Assert.IsTrue(e.RequestSubstitution(team, outgoing, 0, SubstitutionReason.Tactical).IsAccepted);
            e.TestOnly_ApplyRestart(RestartSpot, team);
            Assert.AreEqual(0, e.TestOnly_ActiveBenchSlot(outgoing));
            Assert.AreEqual(1, e.SubstitutionWindowsUsed(team));
            Assert.AreEqual(0, e.SubstitutionWindowsUsed(1 - team));

            int secondOutgoing = outgoing + 1;
            Assert.IsTrue(e.RequestSubstitution(team, secondOutgoing, 1, SubstitutionReason.Tactical).IsAccepted);
            e.TestOnly_SetIsSentOff(secondOutgoing, true);
            e.TestOnly_ApplyRestart(RestartSpot, team);
            Assert.AreEqual(-1, e.TestOnly_ActiveBenchSlot(secondOutgoing));
            Assert.AreEqual(0, e.PendingSubstitutionCount);
            Assert.AreEqual(1, e.SubstitutionWindowsUsed(team));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RestoredPendingRequestMatchesEveryContinuationDigestThroughRestart(int team)
        {
            var original = Started();
            int outgoing = team * MatchEngineConstants.PLAYERS_PER_TEAM + 1;
            Assert.IsTrue(original.RequestSubstitution(team, outgoing, 0, SubstitutionReason.Tactical).IsAccepted);
            original.RunTick();
            SnapshotHeader header = original.CaptureDurableHeader();
            SnapshotPayload payload = original.CaptureDurablePayload();

            const int continuation = 8;
            var reference = new byte[continuation][];
            for (int i = 0; i < continuation; i++)
            {
                if (i == 3) original.TestOnly_ApplyRestart(RestartSpot, team);
                original.RunTick();
                reference[i] = (byte[])original.CurrentSnapshotDigest.Clone();
            }

            var restored = MatchEngine.RestoreFromSnapshot(header, payload, Seed);
            Assert.AreEqual(1, restored.PendingSubstitutionCount);
            for (int i = 0; i < continuation; i++)
            {
                if (i == 3) restored.TestOnly_ApplyRestart(RestartSpot, team);
                restored.RunTick();
                CollectionAssert.AreEqual(reference[i], restored.CurrentSnapshotDigest,
                    "Restored substitution diverged at continuation tick " + i);
            }
            Assert.AreEqual(0, restored.PendingSubstitutionCount);
            Assert.AreEqual(0, restored.TestOnly_ActiveBenchSlot(outgoing));
            Assert.AreEqual(1, restored.SubstitutionWindowsUsed(team));
        }

        [Test]
        public void ActualThrowInExecutesOnItsResolveTick()
        {
            var e = new MatchEngine(Seed);
            e.TestOnly_SetPossession(0);
            e.RunTick();
            Assert.IsTrue(Request(e, 1, 0).IsAccepted);
            e.TestOnly_SetBall(TacticalDirector.BallPhysics.BallState.CreateAtPosition(
                new Vector3(52.5f, -0.5f, MatchEngineConstants.BALL_REST_HEIGHT_M)));
            e.RunTick();
            Assert.AreEqual(RestartCue.ThrowIn, e.RestartAppliedThisTick,
                "The fixture must exercise an actual production Resolve restart.");
            Assert.AreEqual(0, e.PendingSubstitutionCount);
            Assert.AreEqual(0, e.TestOnly_ActiveBenchSlot(1));
            Assert.AreEqual(1, e.SubstitutionWindowsUsed(0));
            bool found = false;
            for (int i = 0; i < e.TickLedgerCount; i++)
            {
                if (e.TickLedgerOrdinal(i) != EventRegistry.GetOrdinal<SubstitutionEvent>())
                    continue;
                SubstitutionEvent applied = e.TickLedgerRecord<SubstitutionEvent>(i);
                Assert.AreEqual(0, applied.Team);
                Assert.AreEqual(1, applied.Outgoing);
                found = true;
            }
            Assert.IsTrue(found, "A real Resolve restart must publish an executable substitution event.");
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
// | 1.0     | 2026-10-13 | —      | Deferred substitution, mirror, restart and restore locks. |
#endregion
