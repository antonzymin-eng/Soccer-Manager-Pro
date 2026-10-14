// File:     src/match-engine/tests/W8BPrerequisiteTests.cs
// Created:  2026-10-13
// Modified: 2026-10-13 (F-05 replay: state-derived CONTACT and sequential reference/restore)
// Author:   —
// Spec:     W8 B #466 follow-ups, #5 §3.8.13, #11 §3.8.2 and Match Engine Resolve
// Purpose:  Real adapter identity, exactly-once terminal feedback and F-05 across restore.

using NUnit.Framework;
using UnityEngine;
using TacticalDirector.DeterministicSim;
using TacticalDirector.PassMechanics;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class W8BPrerequisiteTests
    {
        private const ulong Seed = 0x00000000D1A6D05EUL;
        private const int HomeKeeper = 0;
        private const int AwayKeeper = 11;
        private const int HomeReceiver = 1;

        private static GoalkeeperDistributionRequest Request(int targetId, int windupFrames = 1)
        {
            return new GoalkeeperDistributionRequest
            {
                AgentId = HomeKeeper,
                TeamId = 0,
                Delivery = GoalkeeperDeliveryVariant.Roll,
                TargetAgentId = targetId,
                TargetPosition = new Vector3(35f, 34f, 0f),
                EmittedPower01 = 0.5f,
                SpinIntent = Vector3.zero,
                ReleaseHeightM = 0.2f,
                WindupFrames = windupFrames,
                FrameNumber = 1
            };
        }

        [Test]
        public void PassWorldAdapter_GoalkeeperIdentity_UsesRealTeamRoleAndSentOffState()
        {
            var engine = new MatchEngine(Seed);
            Assert.IsTrue(engine.TestOnly_IsGoalkeeper(HomeKeeper));
            Assert.IsTrue(engine.TestOnly_IsGoalkeeper(AwayKeeper));
            Assert.IsTrue(engine.TestOnly_PassWorldAdapterIsGoalkeeperOfTeam(HomeKeeper, 0));
            Assert.IsTrue(engine.TestOnly_PassWorldAdapterIsGoalkeeperOfTeam(AwayKeeper, 1));
            Assert.IsFalse(engine.TestOnly_PassWorldAdapterIsGoalkeeperOfTeam(HomeKeeper, 1));
            Assert.IsFalse(engine.TestOnly_PassWorldAdapterIsGoalkeeperOfTeam(AwayKeeper, 0));
            Assert.IsFalse(engine.TestOnly_PassWorldAdapterIsGoalkeeperOfTeam(HomeReceiver, 0));
            Assert.IsFalse(engine.TestOnly_PassWorldAdapterIsGoalkeeperOfTeam(12, 1));
            Assert.IsFalse(engine.TestOnly_PassWorldAdapterIsGoalkeeperOfTeam(-1, 0));
            Assert.IsFalse(engine.TestOnly_PassWorldAdapterIsGoalkeeperOfTeam(
                MatchEngineConstants.SQUAD_SIZE, 1));

            engine.TestOnly_SetIsSentOff(HomeKeeper, true);
            engine.TestOnly_SetIsSentOff(AwayKeeper, true);
            Assert.IsFalse(engine.TestOnly_PassWorldAdapterIsGoalkeeperOfTeam(HomeKeeper, 0));
            Assert.IsFalse(engine.TestOnly_PassWorldAdapterIsGoalkeeperOfTeam(AwayKeeper, 1));
        }

        [Test]
        public void RealPassExecutorIdentityGate_RejectsWrongTeamAndOutfielder_AndAcceptsAwayKeeper()
        {
            var engine = new MatchEngine(Seed);
            engine.TestOnly_SetPossession(HomeKeeper);
            GoalkeeperDistributionRequest wrongTeam = Request(HomeReceiver);
            wrongTeam.TeamId = 1;
            Assert.IsFalse(engine.TestOnly_StartGoalkeeperDistribution(
                HomeKeeper, in wrongTeam, out GoalkeeperDistributionFeedback wrongTeamFeedback));
            Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Rejected, wrongTeamFeedback.Kind);
            Assert.IsFalse(engine.TestOnly_PassExecutorState(HomeKeeper).GoalkeeperFeedbackPending);

            engine.TestOnly_SetPossession(HomeReceiver);
            GoalkeeperDistributionRequest outfielder = Request(2);
            outfielder.AgentId = HomeReceiver;
            Assert.IsFalse(engine.TestOnly_StartGoalkeeperDistribution(
                HomeReceiver, in outfielder, out GoalkeeperDistributionFeedback outfieldFeedback));
            Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Rejected, outfieldFeedback.Kind);
            Assert.AreEqual(0, engine.TestOnly_PassExecutorState(HomeReceiver).State);

            engine.TestOnly_SetPossession(AwayKeeper);
            GoalkeeperDistributionRequest away = Request(12);
            away.AgentId = AwayKeeper;
            away.TeamId = 1;
            away.TargetPosition = new Vector3(70f, 34f, 0f);
            Assert.IsTrue(engine.TestOnly_StartGoalkeeperDistribution(
                AwayKeeper, in away, out GoalkeeperDistributionFeedback accepted));
            Assert.AreEqual(1, engine.TestOnly_PassExecutorState(AwayKeeper).State);
        }

        [Test]
        public void CanonicalRestore_PreservesOriginalReceiver_ForExactlyOnceF05()
        {
            var original = new MatchEngine(Seed);
            original.TestOnly_SetPossession(HomeKeeper);
            GoalkeeperDistributionRequest request = Request(HomeReceiver, windupFrames: 3);
            Assert.IsTrue(original.TestOnly_StartGoalkeeperDistribution(
                HomeKeeper, in request, out GoalkeeperDistributionFeedback ignored));
            original.RunTick(); // Real Snapshot phase with WINDUP remaining 2.
            SnapshotHeader header = original.CaptureDurableHeader();
            SnapshotPayload payload = original.CaptureDurablePayload();
            Assert.AreEqual(HomeReceiver,
                original.TestOnly_PassExecutorState(HomeKeeper).GoalkeeperRequest.TargetAgentId);

            // The receiver disappears AFTER the checkpoint but BEFORE CONTACT. Do the
            // uninterrupted continuation FIRST, then restore and replay the exact mutation.
            // EventBus is process-static and RestoreFromSnapshot resets it at boot; running
            // two engines interleaved would violate the match replay test harness contract.
            original.TestOnly_SetIsSentOff(HomeReceiver, true);
            int originalTerminals = 0;
            original.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) =>
            {
                Assert.AreEqual(HomeKeeper, id);
                Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Completed, feedback.Kind);
                Assert.AreEqual(PassMechanicsConstants.AGENT_ID_NONE, feedback.EffectiveTargetAgentId);
                Assert.IsTrue(missing);
                originalTerminals++;
            };
            const int continuationTicks = 5;
            var referenceDigests = new byte[continuationTicks][];
            int originalContactFrames = 0;
            for (int i = 0; i < continuationTicks; i++)
            {
                // #5 Update executes CONTACT only when the executor ENTERS the frame
                // in Contact state. Do not assume a particular iteration or windup timing.
                if (original.TestOnly_PassExecutorState(HomeKeeper).State == 2)
                {
                    originalContactFrames++;
                    Assert.AreEqual(0, originalTerminals, "CONTACT must emit only once.");
                    UnityEngine.TestTools.LogAssert.Expect(
                        LogType.Warning, "gk.distribution.target_receiver_missing keeper=0 receiver=1");
                }
                original.RunTick();
                referenceDigests[i] = original.CurrentSnapshotDigest;
            }
            Assert.AreEqual(1, originalContactFrames, "Expected exactly one original CONTACT frame.");
            Assert.AreEqual(1, originalTerminals);

            // Restore only AFTER reference execution: its boot resets the shared EventBus.
            var restored = MatchEngine.RestoreFromSnapshot(header, payload, Seed);
            Assert.AreEqual(HomeReceiver,
                restored.TestOnly_PassExecutorState(HomeKeeper).GoalkeeperRequest.TargetAgentId);
            // Restore commits the saved digest into SnapshotCodec's previous-link state.
            // CurrentSnapshotDigest itself is only refreshed by the NEXT RunTick.
            restored.TestOnly_SetIsSentOff(HomeReceiver, true);
            int restoredTerminals = 0;
            restored.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) =>
            {
                Assert.AreEqual(HomeKeeper, id);
                Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Completed, feedback.Kind);
                Assert.AreEqual(PassMechanicsConstants.AGENT_ID_NONE, feedback.EffectiveTargetAgentId);
                Assert.IsTrue(missing);
                restoredTerminals++;
            };
            int restoredContactFrames = 0;
            for (int i = 0; i < continuationTicks; i++)
            {
                if (restored.TestOnly_PassExecutorState(HomeKeeper).State == 2)
                {
                    restoredContactFrames++;
                    Assert.AreEqual(0, restoredTerminals, "Replay CONTACT must emit only once.");
                    UnityEngine.TestTools.LogAssert.Expect(
                        LogType.Warning, "gk.distribution.target_receiver_missing keeper=0 receiver=1");
                }
                restored.RunTick();
                CollectionAssert.AreEqual(referenceDigests[i], restored.CurrentSnapshotDigest,
                    $"F-05 must preserve deterministic continuation at replay tick {i + 1}.");
            }
            Assert.AreEqual(1, restoredContactFrames, "Expected exactly one restored CONTACT frame.");
            Assert.AreEqual(1, restoredTerminals);
            Assert.IsFalse(restored.TestOnly_PassExecutorState(HomeKeeper).GoalkeeperFeedbackPending);
        }

        [Test]
        public void CancelledFeedback_IsConsumedOnceBeforeOrdinaryModeCanResume()
        {
            var engine = new MatchEngine(Seed);
            engine.TestOnly_SetPossession(HomeKeeper);
            int count = 0;
            GoalkeeperDistributionFeedbackKind lastKind = GoalkeeperDistributionFeedbackKind.Rejected;
            engine.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) =>
            {
                Assert.AreEqual(HomeKeeper, id);
                Assert.IsFalse(missing);
                count++;
                lastKind = feedback.Kind;
            };

            GoalkeeperDistributionRequest request = Request(HomeReceiver, windupFrames: 3);
            Assert.IsTrue(engine.TestOnly_StartGoalkeeperDistribution(
                HomeKeeper, in request, out GoalkeeperDistributionFeedback startFeedback));
            Assert.IsTrue(engine.TestOnly_CancelGoalkeeperDistribution(HomeKeeper));
            Assert.IsTrue(engine.TestOnly_PassExecutorState(HomeKeeper).GoalkeeperFeedbackPending);

            engine.RunTick();
            Assert.AreEqual(1, count);
            Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Cancelled, lastKind);
            Assert.IsFalse(engine.TestOnly_PassExecutorState(HomeKeeper).GoalkeeperFeedbackPending);
            Assert.AreEqual(0, engine.TestOnly_PassExecutorState(HomeKeeper).ExecutionMode);

            // The previous goalkeeper request no longer blocks the ordinary #5 executor.
            var ordinary = new PassRequest
            {
                AgentId = HomeKeeper,
                TeamId = 0,
                PassType = PassType.Ground,
                CrossSubType = CrossSubType.Flat,
                TargetAgentId = HomeReceiver,
                TargetPosition = new Vector3(35f, 34f, 0f),
                IntendedDistance = 10f,
                Urgency = 0.5f,
                IsWeakFoot = false,
                FrameNumber = 3
            };
            PassResult pass = engine.TestOnly_InitiatePass(HomeKeeper, in ordinary);
            Assert.AreEqual(PassOutcome.Initiated, pass.Outcome);
            engine.RunTick();
            Assert.AreEqual(1, count, "Terminal feedback must not replay on later Resolve ticks.");
        }

        [Test]
        public void RejectedRequest_IsReportedOnceWithoutPendingExecutorFeedback()
        {
            var engine = new MatchEngine(Seed);
            int count = 0;
            engine.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) =>
            {
                Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Rejected, feedback.Kind);
                Assert.IsFalse(missing);
                count++;
            };

            GoalkeeperDistributionRequest request = Request(HomeReceiver);
            Assert.IsFalse(engine.TestOnly_StartGoalkeeperDistribution(
                HomeKeeper, in request, out GoalkeeperDistributionFeedback feedback));
            Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Rejected, feedback.Kind);
            Assert.IsFalse(engine.TestOnly_PassExecutorState(HomeKeeper).GoalkeeperFeedbackPending);
            engine.RunTick();
            Assert.AreEqual(1, count);
        }

        [Test]
        public void LostReceiver_AfterExecutorRestore_TriggersF05FromOriginalRequest()
        {
            var source = new MatchEngine(Seed);
            source.TestOnly_SetPossession(HomeKeeper);
            GoalkeeperDistributionRequest request = Request(HomeReceiver);
            Assert.IsTrue(source.TestOnly_StartGoalkeeperDistribution(
                HomeKeeper, in request, out GoalkeeperDistributionFeedback ignored));
            source.RunTick(); // WINDUP expires; serialized execution is waiting for CONTACT.
            PassExecutorState checkpoint = source.TestOnly_PassExecutorState(HomeKeeper);
            Assert.AreEqual(2, checkpoint.State);
            Assert.AreEqual(HomeReceiver, checkpoint.GoalkeeperRequest.TargetAgentId);

            var restored = new MatchEngine(Seed);
            restored.TestOnly_SetPossession(HomeKeeper);
            restored.TestOnly_SetPassExecutorState(HomeKeeper, in checkpoint);
            restored.TestOnly_SetIsSentOff(HomeReceiver, true);

            int count = 0;
            bool receiverMissing = false;
            GoalkeeperDistributionFeedback terminal = default;
            restored.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) =>
            {
                count++;
                receiverMissing = missing;
                terminal = feedback;
            };
            UnityEngine.TestTools.LogAssert.Expect(
                LogType.Warning, "gk.distribution.target_receiver_missing keeper=0 receiver=1");
            restored.RunTick();

            Assert.AreEqual(1, count);
            Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Completed, terminal.Kind);
            Assert.AreEqual(PassMechanicsConstants.AGENT_ID_NONE, terminal.EffectiveTargetAgentId);
            Assert.IsTrue(receiverMissing, "F-05 requires an originally named receiver, not only effective -1.");
            Assert.IsFalse(restored.TestOnly_PassExecutorState(HomeKeeper).GoalkeeperFeedbackPending);
            Assert.AreEqual(0, restored.TestOnly_PassExecutorState(HomeKeeper).ExecutionMode);
            restored.RunTick();
            Assert.AreEqual(1, count, "Restored terminal feedback must be consumed exactly once.");
        }

        [Test]
        public void IntentionalReceiverlessContact_DoesNotReportF05()
        {
            var engine = new MatchEngine(Seed);
            engine.TestOnly_SetPossession(HomeKeeper);
            int count = 0;
            engine.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) =>
            {
                Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Completed, feedback.Kind);
                Assert.AreEqual(PassMechanicsConstants.AGENT_ID_NONE, feedback.EffectiveTargetAgentId);
                Assert.IsFalse(missing, "Intentionally receiverless must not emit missing-receiver telemetry.");
                count++;
            };
            GoalkeeperDistributionRequest request = Request(PassMechanicsConstants.AGENT_ID_NONE);
            Assert.IsTrue(engine.TestOnly_StartGoalkeeperDistribution(
                HomeKeeper, in request, out GoalkeeperDistributionFeedback ignored));
            engine.RunTick(); // WINDUP -> CONTACT
            engine.RunTick(); // CONTACT -> terminal -> Resolve consumes
            Assert.AreEqual(1, count);
            Assert.IsFalse(engine.TestOnly_PassExecutorState(HomeKeeper).GoalkeeperFeedbackPending);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.4     | 2026-10-13 | —      | Expect F-05 at #5 executor Contact state rather than an assumed iteration; assert one CONTACT and terminal on reference and restore with full per-tick digest chain. |
// | 1.3     | 2026-10-13 | —      | Sequential reference/replay runs respect process-static EventBus reset; apply identical receiver invalidation after checkpoint; assert one diagnostic and each resumed digest. |
// | 1.2     | 2026-10-13 | —      | Intermediate restore probe moved invalidation into checkpoint; superseded by 1.3. |
// | 1.1     | 2026-10-13 | —      | Assert emitted F-05 warning on ordinary and canonical-restore CONTACT; retain one report per kick. |
// | 1.0     | 2026-10-13 | —      | W8 B #466 pre-wiring host/identity/feedback/F-05 locks. |
#endregion
