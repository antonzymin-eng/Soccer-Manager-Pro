// File:     src/match-engine/tests/MatchEngineDeferredSubstitutionTests.cs
// Created:  2026-10-13
// Modified: 2026-10-15 (§6.5 lock completion: locks 1, 2, 3, 4, 7, 8, 11 and 13 with home/away mirrors)
// Spec:     Match Flow Completion §6, Code Standards #20
// Purpose:  Acceptance locks for deferred substitution requests and deterministic restart execution.

using System;
using NUnit.Framework;
using UnityEngine;
using TacticalDirector.AgentMovement;
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

        [TestCase(0)]
        [TestCase(1)]
        public void LegacySwapCannotInvalidateQueuedSlotBenchOrCapacity(int team)
        {
            MatchEngine e = Started();
            int first = team * MatchEngineConstants.PLAYERS_PER_TEAM + 1;
            Assert.IsTrue(e.RequestSubstitution(team, first, 0, SubstitutionReason.Tactical).IsAccepted);
            Assert.Throws<InvalidOperationException>(() =>
                e.SubstitutePlayer(team, first, 1, SubstitutionReason.Tactical));
            Assert.Throws<InvalidOperationException>(() =>
                e.SubstitutePlayer(team, first + 1, 0, SubstitutionReason.Tactical));
            for (int i = 1; i < MatchEngineConstants.MAX_SUBSTITUTIONS_PER_TEAM; i++)
                Assert.IsTrue(e.RequestSubstitution(team, first + i, i, SubstitutionReason.Tactical).IsAccepted);
            Assert.Throws<InvalidOperationException>(() =>
                e.SubstitutePlayer(team, first + 5, 5, SubstitutionReason.Tactical));
            Assert.AreEqual(0, e.TestOnly_SubstitutionsUsed(team));
            Assert.DoesNotThrow(() => e.TestOnly_ApplyRestart(RestartSpot, team));
            Assert.AreEqual(0, e.PendingSubstitutionCount);
            Assert.AreEqual(MatchEngineConstants.MAX_SUBSTITUTIONS_PER_TEAM, e.TestOnly_SubstitutionsUsed(team));
        }

        [TestCase(2)]
        [TestCase(256)]
        [TestCase(-1)]
        public void UndefinedReasonDoesNotChangeQueueSequenceOrSaveState(int reason)
        {
            MatchEngine e = Started();
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                e.RequestSubstitution(0, 1, 0, (SubstitutionReason)reason));
            Assert.AreEqual(0, e.PendingSubstitutionCount);
            Assert.DoesNotThrow(() => e.CaptureDurableHeader());
            Assert.DoesNotThrow(() => e.CaptureDurablePayload());
            var control = Started();
            Assert.AreEqual(Request(control, 1, 0).Sequence, Request(e, 1, 0).Sequence);
            e.RunTick();
            Assert.DoesNotThrow(() => MatchEngine.RestoreFromSnapshot(
                e.CaptureDurableHeader(), e.CaptureDurablePayload(), Seed));
        }

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

        // ── §6.5 lock completion (October 15) ────────────────────────────────────────────────────

        private static int Slot(int team, int index) => team * MatchEngineConstants.PLAYERS_PER_TEAM + index;

        private static bool Accept(MatchEngine e, int team, int index, int bench) =>
            e.RequestSubstitution(team, Slot(team, index), bench, SubstitutionReason.Tactical).IsAccepted;

        /// <summary>Uses all three Law 3 windows for <paramref name="team"/>: slots 1–3 for benches 0–2.</summary>
        private static void UseThreeWindows(MatchEngine e, int team)
        {
            for (int i = 0; i < MatchEngineConstants.MAX_SUBSTITUTION_WINDOWS_PER_TEAM; i++)
            {
                Assert.IsTrue(Accept(e, team, 1 + i, i));
                e.TestOnly_ApplyRestart(RestartSpot, team);
            }
            Assert.AreEqual(MatchEngineConstants.MAX_SUBSTITUTION_WINDOWS_PER_TEAM, e.SubstitutionWindowsUsed(team));
        }

        private static void ReachSecondHalf(MatchEngine e)
        {
            e.TestOnly_SetPhysicsFrame((int)MatchEngineConstants.HALF_TIME_BOUNDARY_TICK - 1);
            e.RunTick();
            Assert.IsTrue(e.TestOnly_SecondHalfStarted, "precondition: the second-half kickoff ran");
        }

        private static byte[][] ContinuationDigests(MatchEngine e, int ticks, Action<MatchEngine, int> beforeTick)
        {
            var digests = new byte[ticks][];
            for (int i = 0; i < ticks; i++)
            {
                beforeTick?.Invoke(e, i);
                e.RunTick();
                digests[i] = (byte[])e.CurrentSnapshotDigest.Clone();
            }
            return digests;
        }

        private static void AssertSameDigests(byte[][] expected, byte[][] actual, string what)
        {
            for (int i = 0; i < expected.Length; i++)
                CollectionAssert.AreEqual(expected[i], actual[i], what + " diverged at continuation tick " + i);
        }

        /// <summary>Lock 8 observation for one execution tick: the request ran, its event was published in
        /// that tick's ledger, and the transient publication buffer is empty at the tick boundary.</summary>
        private static void AssertExecutedAndPublishedThisTick(
            MatchEngine e, int team, int outgoing, RestartCue cue, string kind)
        {
            Assert.AreEqual(cue, e.RestartAppliedThisTick, kind + ": the fixture must reach a real restart");
            Assert.AreEqual(0, e.PendingSubstitutionCount, kind + ": the request executed");
            Assert.AreEqual(0, e.TestOnly_ActiveBenchSlot(outgoing), kind + ": bench 0 is on");
            Assert.AreEqual(0, e.TestOnly_PendingSubstitutionEventCount,
                kind + ": the transient event queue must be empty at the tick boundary (§6.2.5)");
            bool found = false;
            for (int i = 0; i < e.TickLedgerCount; i++)
            {
                if (e.TickLedgerOrdinal(i) != EventRegistry.GetOrdinal<SubstitutionEvent>())
                    continue;
                SubstitutionEvent applied = e.TickLedgerRecord<SubstitutionEvent>(i);
                found |= applied.Team == team && applied.Outgoing == outgoing;
            }
            Assert.IsTrue(found, kind + ": SubstitutionEvent must be published in the tick of execution");
        }

        /// <summary>Lock 1: a pending request changes no on-pitch state, tick after tick, until the
        /// next stoppage; there it executes before taker selection, so the incoming player can take
        /// the restart that the outgoing player was nearest to.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void PendingRequestChangesNoOnPitchStateThenIncomingPlayerCanTakeTheRestart(int team)
        {
            MatchEngine withRequest = Started();
            MatchEngine control = Started();
            int outgoing = Slot(team, 5);
            bool keeperBefore = withRequest.TestOnly_IsGoalkeeper(outgoing);
            Assert.IsTrue(Accept(withRequest, team, 5, 0));

            for (int t = 0; t < 5; t++)
            {
                withRequest.RunTick();
                control.RunTick();
                Assert.AreEqual(RestartCue.None, withRequest.RestartAppliedThisTick, "precondition: open play");
                Assert.AreEqual(-1, withRequest.TestOnly_ActiveBenchSlot(outgoing));
                Assert.AreEqual(keeperBefore, withRequest.TestOnly_IsGoalkeeper(outgoing));
                Assert.AreEqual(0, withRequest.TestOnly_SubstitutionsUsed(team));
                for (int a = 0; a < MatchEngineConstants.SQUAD_SIZE; a++)
                {
                    AgentState got = withRequest.TestOnly_AgentSnapshot(a);
                    AgentState want = control.TestOnly_AgentSnapshot(a);
                    Assert.AreEqual(want.Position, got.Position, "agent " + a + " position, tick " + t);
                    Assert.AreEqual(want.Velocity, got.Velocity, "agent " + a + " velocity, tick " + t);
                }
                Assert.AreEqual(control.TestOnly_BallSnapshot.Position, withRequest.TestOnly_BallSnapshot.Position);
                Assert.AreEqual(control.TestOnly_PossessingAgentId, withRequest.TestOnly_PossessingAgentId);
            }

            var spot = new Vector2(team == 0 ? 20f : 85f, 10f);
            withRequest.TestOnly_SetAgent(outgoing, AgentState.CreateAtPosition(spot, Vector2.right));
            withRequest.TestOnly_ApplyRestart(spot, team);
            Assert.AreEqual(0, withRequest.TestOnly_ActiveBenchSlot(outgoing), "executed at the restart");
            Assert.AreEqual(outgoing, withRequest.TestOnly_PossessingAgentId,
                "execution precedes taker selection, so the incoming player in that slot takes the restart");
        }

        /// <summary>Lock 2: requests executed at one real stoppage publish in sequence order.</summary>
        [Test]
        public void TwoRequestsAtOneRealRestartPublishInSequenceOrder()
        {
            var e = new MatchEngine(Seed);
            e.TestOnly_SetPossession(0);
            e.RunTick();
            Assert.IsTrue(Accept(e, 0, 2, 1), "first in sequence: slot 2");
            Assert.IsTrue(Accept(e, 0, 1, 0), "second in sequence: slot 1");
            e.TestOnly_SetBall(TacticalDirector.BallPhysics.BallState.CreateAtPosition(
                new Vector3(52.5f, -0.5f, MatchEngineConstants.BALL_REST_HEIGHT_M)));
            e.RunTick();
            Assert.AreEqual(RestartCue.ThrowIn, e.RestartAppliedThisTick, "precondition");

            int published = 0;
            var order = new int[2];
            for (int i = 0; i < e.TickLedgerCount; i++)
            {
                if (e.TickLedgerOrdinal(i) != EventRegistry.GetOrdinal<SubstitutionEvent>())
                    continue;
                order[published++] = e.TickLedgerRecord<SubstitutionEvent>(i).Outgoing;
            }
            Assert.AreEqual(2, published);
            CollectionAssert.AreEqual(new[] { 2, 1 }, order, "FIFO: sequence order, not slot order");
            Assert.AreEqual(1, e.SubstitutionWindowsUsed(0), "both executions share one window");
        }

        /// <summary>Lock 3: the cap counts used plus pending, and only execution changes the count.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void LimitCountsPendingRequestsAndOnlyExecutionChangesTheCount(int team)
        {
            MatchEngine e = Started();
            Assert.IsTrue(Accept(e, team, 1, 0));
            Assert.IsTrue(Accept(e, team, 2, 1));
            e.TestOnly_ApplyRestart(RestartSpot, team);
            Assert.AreEqual(2, e.TestOnly_SubstitutionsUsed(team));

            for (int i = 0; i < 3; i++)
                Assert.IsTrue(Accept(e, team, 3 + i, 2 + i));
            Assert.AreEqual(2, e.TestOnly_SubstitutionsUsed(team), "acceptance does not count");
            Assert.AreEqual(SubstitutionRequestStatus.LimitReached,
                e.RequestSubstitution(team, Slot(team, 6), 5, SubstitutionReason.Tactical).Status);
            Assert.AreEqual(3, e.PendingSubstitutionCount, "a refusal leaves the queue unchanged");

            e.TestOnly_ApplyRestart(RestartSpot, team);
            Assert.AreEqual(MatchEngineConstants.MAX_SUBSTITUTIONS_PER_TEAM, e.TestOnly_SubstitutionsUsed(team));
            Assert.AreEqual(SubstitutionRequestStatus.LimitReached,
                e.RequestSubstitution(team, Slot(team, 6), 5, SubstitutionReason.Tactical).Status);
            Assert.IsTrue(Accept(e, 1 - team, 1, 0), "the other team's cap is separate");
        }

        /// <summary>Lock 4: a request held by the window gate is still cancelled at the dismissal's free
        /// kick, not kept until half-time, and its bench player is free straight away.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void DismissalCancelsARequestHeldByTheWindowGate(int team)
        {
            MatchEngine e = Started();
            UseThreeWindows(e, team);
            int held = Slot(team, 4);
            Assert.IsTrue(Accept(e, team, 4, 3), "first half: accepted and held for half-time");
            e.TestOnly_ApplyRestart(RestartSpot, team);
            Assert.AreEqual(1, e.PendingSubstitutionCount, "precondition: held by the gate");

            e.TestOnly_SetIsSentOff(held, true);
            e.TestOnly_ApplyRestart(RestartSpot, 1 - team);
            Assert.AreEqual(0, e.PendingSubstitutionCount);
            Assert.AreEqual(-1, e.TestOnly_ActiveBenchSlot(held));
            Assert.AreEqual(3, e.TestOnly_SubstitutionsUsed(team), "a cancellation uses no substitution");
            Assert.AreEqual(3, e.SubstitutionWindowsUsed(team));
            Assert.IsTrue(Accept(e, team, 5, 3), "bench 3 is released at the cancellation");
        }

        /// <summary>Lock 8 / lock 1: an execution at each production restart kind publishes in its own
        /// tick and leaves the transient event queue empty at the tick boundary.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void ExecutionAtEveryRestartKindPublishesInItsOwnTick(int team)
        {
            int outgoing = Slot(team, 5);

            var throwIn = new MatchEngine(Seed);
            throwIn.TestOnly_SetPossession(0);
            throwIn.RunTick();
            Assert.IsTrue(Accept(throwIn, team, 5, 0));
            throwIn.TestOnly_SetBall(TacticalDirector.BallPhysics.BallState.CreateAtPosition(
                new Vector3(52.5f, -0.5f, MatchEngineConstants.BALL_REST_HEIGHT_M)));
            throwIn.RunTick();
            AssertExecutedAndPublishedThisTick(throwIn, team, outgoing, RestartCue.ThrowIn, "throw-in");

            MatchEngine goal = Started();
            Assert.IsTrue(Accept(goal, team, 5, 0));
            goal.TestOnly_SetBall(TacticalDirector.BallPhysics.BallState.CreateAtPosition(new Vector3(
                MatchEngineConstants.PITCH_LENGTH_M + 0.5f, MatchEngineConstants.PITCH_WIDTH_M / 2f,
                MatchEngineConstants.BALL_REST_HEIGHT_M)));
            goal.RunTick();
            Assert.AreEqual(1, goal.TestOnly_Goals(0), "precondition: a goal was scored");
            AssertExecutedAndPublishedThisTick(goal, team, outgoing, RestartCue.KickOff, "goal kickoff");

            MatchEngine foul = Started();
            Assert.IsTrue(Accept(foul, team, 5, 0));
            const int Offender = 0;
            const int Victim = 12;
            var victimPos = new Vector2(40f, 20f);
            foul.TestOnly_SetAgent(Victim, AgentState.CreateAtPosition(victimPos, new Vector2(1f, 0f)));
            foul.TestOnly_SetCommand(Victim, MovementCommand.Stop(victimPos));
            foul.TestOnly_InjectFoulCandidate(Offender, Victim);
            foul.RunTick();
            AssertExecutedAndPublishedThisTick(foul, team, outgoing, RestartCue.FreeKick, "foul free kick");

            MatchEngine halfTime = Started();
            Assert.IsTrue(Accept(halfTime, team, 5, 0));
            ReachSecondHalf(halfTime);
            AssertExecutedAndPublishedThisTick(halfTime, team, outgoing, RestartCue.KickOff, "second-half kickoff");
            Assert.AreEqual(0, halfTime.SubstitutionWindowsUsed(team), "the half-time kickoff is free");

            foreach (MatchEngine e in new[] { throwIn, goal, foul, halfTime })
            {
                e.RunTick();
                Assert.AreEqual(0, e.TestOnly_PendingSubstitutionEventCount, "still empty one tick later");
                Assert.AreEqual(1, e.TestOnly_SubstitutionsUsed(team), "executed exactly once");
            }
        }

        /// <summary>Lock 8, offside: the offside free kick executes the queue and publishes at once, so
        /// nothing is left in the transient buffer. The offside decision is driven through its Resolve
        /// seam, as the offside tests do.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void OffsideFreeKickExecutionLeavesNoBufferedEvent(int team)
        {
            var e = new MatchEngine(Seed);
            e.TestOnly_SetPhysicsFrame(10);   // past kickoff, so the request is deferred
            int outgoing = Slot(team, 5);
            Assert.IsTrue(Accept(e, team, 5, 0));
            Assert.AreEqual(1, e.PendingSubstitutionCount, "precondition: deferred, not pre-kickoff");

            int toucher = Slot(team, 9);
            float offsideX = team == 0 ? MatchEngineConstants.PITCH_LENGTH_M - 2f : 2f;
            e.TestOnly_SetAgent(toucher, AgentState.CreateAtPosition(new Vector2(offsideX, 34f), new Vector2(1f, 0f)));
            EventBus.BeginPhase(PhaseId.Resolve);
            Assert.IsTrue(e.TestOnly_EvaluateAndApplyOffside(toucher), "precondition: offside");

            Assert.AreEqual(RestartCue.FreeKick, e.RestartAppliedThisTick);
            Assert.AreEqual(0, e.PendingSubstitutionCount);
            Assert.AreEqual(0, e.TestOnly_ActiveBenchSlot(outgoing));
            Assert.AreEqual(0, e.TestOnly_PendingSubstitutionEventCount,
                "a Resolve-phase restart publishes directly; nothing may wait for the next tick");
        }

        /// <summary>Lock 7 / lock 9: a save on the stoppage tick that executed the request continues
        /// with identical digests, and the request is applied once, not again after restore.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void SaveOnTheExecutingStoppageTickContinuesIdenticallyAndAppliesOnce(int team)
        {
            var e = new MatchEngine(Seed);
            e.TestOnly_SetPossession(0);
            e.RunTick();
            int outgoing = Slot(team, 5);
            Assert.IsTrue(Accept(e, team, 5, 0));
            e.TestOnly_SetBall(TacticalDirector.BallPhysics.BallState.CreateAtPosition(
                new Vector3(52.5f, -0.5f, MatchEngineConstants.BALL_REST_HEIGHT_M)));
            e.RunTick();
            Assert.AreEqual(RestartCue.ThrowIn, e.RestartAppliedThisTick, "precondition");
            Assert.AreEqual(0, e.TestOnly_ActiveBenchSlot(outgoing), "precondition: executed this tick");

            byte[] blob = MatchSaveManager.Encode(e);
            byte[][] reference = ContinuationDigests(e, 8, null);

            MatchEngine restored = MatchSaveManager.Restore(blob);
            Assert.AreEqual(0, restored.PendingSubstitutionCount);
            Assert.AreEqual(0, restored.TestOnly_ActiveBenchSlot(outgoing));
            AssertSameDigests(reference, ContinuationDigests(restored, 8, null), "Stoppage-tick restore");
            Assert.AreEqual(1, restored.TestOnly_SubstitutionsUsed(team), "applied once, not again");
            Assert.AreEqual(1, restored.SubstitutionWindowsUsed(team));
        }

        /// <summary>Lock 13: save/restore with all windows used and a request held by the gate continues
        /// with identical digests through another stoppage (still held) and the half-time kickoff
        /// (executed, free).</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void SaveWithWindowsUsedAndARequestHeldContinuesIdentically(int team)
        {
            MatchEngine e = Started();
            UseThreeWindows(e, team);
            Assert.IsTrue(Accept(e, team, 4, 3));
            e.TestOnly_ApplyRestart(RestartSpot, team);
            e.RunTick();
            Assert.AreEqual(1, e.PendingSubstitutionCount, "precondition: held by the gate");

            byte[] blob = MatchSaveManager.Encode(e);
            Action<MatchEngine, int> script = (engine, i) =>
            {
                if (i == 2) engine.TestOnly_ApplyRestart(RestartSpot, team);
                if (i == 4) engine.TestOnly_SetPhysicsFrame((int)MatchEngineConstants.HALF_TIME_BOUNDARY_TICK - 1);
            };
            byte[][] reference = ContinuationDigests(e, 8, script);

            MatchEngine restored = MatchSaveManager.Restore(blob);
            Assert.AreEqual(1, restored.PendingSubstitutionCount);
            Assert.AreEqual(3, restored.SubstitutionWindowsUsed(team));
            AssertSameDigests(reference, ContinuationDigests(restored, 8, script), "Held-window restore");
            Assert.AreEqual(0, restored.PendingSubstitutionCount, "executed at the half-time kickoff");
            Assert.AreEqual(3, restored.TestOnly_ActiveBenchSlot(Slot(team, 4)));
            Assert.AreEqual(3, restored.SubstitutionWindowsUsed(team), "half-time used no window");
        }

        /// <summary>Lock 12 / lock 2 (PR #505 review): requests that share a stoppage share its window, so
        /// two requests at a team's THIRD window both execute there. The first execution must not close
        /// the gate on the second.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void TwoRequestsAtTheThirdWindowBothExecuteThere(int team)
        {
            MatchEngine e = Started();
            for (int i = 0; i < 2; i++)
            {
                Assert.IsTrue(Accept(e, team, 1 + i, i));
                e.TestOnly_ApplyRestart(RestartSpot, team);
            }
            Assert.AreEqual(2, e.SubstitutionWindowsUsed(team), "precondition: one window left");

            Assert.IsTrue(Accept(e, team, 3, 2));
            Assert.IsTrue(Accept(e, team, 4, 3));
            e.TestOnly_ApplyRestart(RestartSpot, team);
            Assert.AreEqual(0, e.PendingSubstitutionCount, "both requests execute at the third window");
            Assert.AreEqual(2, e.TestOnly_ActiveBenchSlot(Slot(team, 3)));
            Assert.AreEqual(3, e.TestOnly_ActiveBenchSlot(Slot(team, 4)));
            Assert.AreEqual(4, e.TestOnly_SubstitutionsUsed(team));
            Assert.AreEqual(3, e.SubstitutionWindowsUsed(team), "one window for the shared stoppage");
            Assert.AreEqual(0, e.SubstitutionWindowsUsed(1 - team));
        }

        /// <summary>The same at a real second-half restart: nothing may be left waiting for a window that
        /// never comes, and only a later request is refused.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void SecondHalfThirdWindowExecutesEveryRequestQueuedForIt(int team)
        {
            MatchEngine e = Started();
            ReachSecondHalf(e);
            for (int i = 0; i < 2; i++)
            {
                Assert.IsTrue(Accept(e, team, 1 + i, i));
                e.TestOnly_ApplyRestart(RestartSpot, team);
            }
            Assert.IsTrue(Accept(e, team, 3, 2));
            Assert.IsTrue(Accept(e, team, 4, 3));
            e.TestOnly_SetBall(TacticalDirector.BallPhysics.BallState.CreateAtPosition(
                new Vector3(52.5f, -0.5f, MatchEngineConstants.BALL_REST_HEIGHT_M)));
            e.RunTick();
            Assert.AreEqual(RestartCue.ThrowIn, e.RestartAppliedThisTick, "precondition: a real restart");
            Assert.AreEqual(0, e.PendingSubstitutionCount, "nothing is stranded until full time");
            Assert.AreEqual(4, e.TestOnly_SubstitutionsUsed(team));
            Assert.AreEqual(3, e.SubstitutionWindowsUsed(team));
            Assert.AreEqual(SubstitutionRequestStatus.NoWindowsLeft,
                e.RequestSubstitution(team, Slot(team, 5), 4, SubstitutionReason.Tactical).Status);
        }

        /// <summary>Lock 11: fallback (1) in the first half with no window left is accepted, the team
        /// plays without a keeper, and the keeper comes on free at the half-time kickoff.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void FallbackKeeperHeldByTheGateComesOnFreeAtHalfTime(int team)
        {
            MatchEngine e = Started();
            UseThreeWindows(e, team);
            e.TestOnly_SetBenchSlot(team, 5, isGoalkeeper: true);
            e.TestOnly_SetIsSentOff(Slot(team, 0), true);
            int fallback = Slot(team, 4);
            Assert.IsTrue(Accept(e, team, 4, 5), "fallback accepted in the first half without a window");

            e.TestOnly_ApplyRestart(RestartSpot, team);
            Assert.AreEqual(1, e.PendingSubstitutionCount, "held for half-time");
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, e.TestOnly_FirstEligibleGoalkeeper(team),
                "the team plays without a keeper until half-time");

            ReachSecondHalf(e);
            Assert.AreEqual(0, e.PendingSubstitutionCount);
            Assert.IsTrue(e.TestOnly_IsGoalkeeper(fallback));
            Assert.AreEqual(fallback, e.TestOnly_FirstEligibleGoalkeeper(team));
            Assert.AreEqual(3, e.SubstitutionWindowsUsed(team), "the half-time kickoff is free");
        }

        /// <summary>Lock 11: with no window left in the second half, or no substitution left, fallback
        /// (1) is refused and the team plays on without a keeper.</summary>
        [TestCase(0)]
        [TestCase(1)]
        public void FallbackKeeperIsRefusedWithoutASecondHalfWindowOrASubstitution(int team)
        {
            MatchEngine noWindow = Started();
            UseThreeWindows(noWindow, team);
            ReachSecondHalf(noWindow);
            noWindow.TestOnly_SetBenchSlot(team, 5, isGoalkeeper: true);
            noWindow.TestOnly_SetIsSentOff(Slot(team, 0), true);
            Assert.AreEqual(SubstitutionRequestStatus.NoWindowsLeft,
                noWindow.RequestSubstitution(team, Slot(team, 4), 5, SubstitutionReason.Tactical).Status);
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, noWindow.TestOnly_FirstEligibleGoalkeeper(team));

            MatchEngine noSubstitution = Started();
            for (int i = 0; i < MatchEngineConstants.MAX_SUBSTITUTIONS_PER_TEAM; i++)
                Assert.IsTrue(Accept(noSubstitution, team, 1 + i, i));
            noSubstitution.TestOnly_ApplyRestart(RestartSpot, team);
            Assert.AreEqual(MatchEngineConstants.MAX_SUBSTITUTIONS_PER_TEAM, noSubstitution.TestOnly_SubstitutionsUsed(team));
            noSubstitution.TestOnly_SetBenchSlot(team, 5, isGoalkeeper: true);
            noSubstitution.TestOnly_SetIsSentOff(Slot(team, 0), true);
            Assert.AreEqual(SubstitutionRequestStatus.LimitReached,
                noSubstitution.RequestSubstitution(team, Slot(team, 6), 5, SubstitutionReason.Tactical).Status);
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, noSubstitution.TestOnly_FirstEligibleGoalkeeper(team));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                  |
// | 1.0     | 2026-10-13 | —      | Deferred substitution, mirror, restart and restore locks. |
// | 1.1     | 2026-10-15 | —      | PR #505 Codex regression locks for reservations and malformed reasons. |
// | 1.2     | 2026-10-15 | —      | §6.5 lock completion: open-play no-effect vs a control run and incoming-player taker (1); FIFO publication order at a real throw-in (2); LimitReached counting pending (3); dismissal cancels a gate-held request (4); stoppage-tick save via MatchSaveManager (7); execution and empty event buffer at throw-in, goal kickoff, foul and offside free kicks and the second-half kickoff (8); fallback held to half-time and its refusals (11); held-window save (13). Home/away mirrored. |
// | 1.3     | 2026-10-15 | —      | PR #505 review: two requests at the third window both execute there (first half, via the restart seam) and at a real second-half throw-in, where nothing may be stranded and a later request is refused NoWindowsLeft. Home/away mirrored; both failed before the fix. |
#endregion
