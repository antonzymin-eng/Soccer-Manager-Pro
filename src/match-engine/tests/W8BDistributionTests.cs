// File:     src/match-engine/tests/W8BDistributionTests.cs
// Created:  2026-10-15
// Modified: 2026-10-15
// Author:   —
// Spec:     #21 §3.4.1, #11 §3.8, #5 §3.8.13; W8 preregistered B locks
// Purpose:  Composed policy, CONTACT, cancellation, deadline and canonical replay locks.

using System;
using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.PassMechanics;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>Composed W8 B policy, hand-control lifecycle and canonical replay locks.</summary>
    [TestFixture]
    public sealed class W8BDistributionTests
    {
        private const ulong Seed = 0xD1A6D05EUL;
        private const int ClaimFrame = 1001;
        private static readonly byte DistributionOrdinal = EventRegistry.GetOrdinal<DistributionExecutedEvent>();

        private static IEnumerable<TestCaseData> Policies()
        {
            foreach (GkDistributionPolicy policy in Enum.GetValues(typeof(GkDistributionPolicy)))
                for (int team = 0; team < 2; team++)
                    foreach (bool hasReceiver in new[] { true, false })
                        yield return new TestCaseData(policy, team, hasReceiver);
        }

        [TestCaseSource(nameof(Policies))]
        public void AllPolicies_CommitAtDelay_HoldThroughWindup_ExecuteOneContact(
            GkDistributionPolicy policy, int team, bool hasReceiver)
        {
            MatchEngine engine = Fixture(policy, team, hasReceiver);
            int keeper = team * 11;
            int due = (ClaimFrame / 6 + Delay(policy)) * 6;
            engine.TestOnly_SetPhysicsFrame(due - 7);
            engine.RunTick();
            Assert.AreEqual(0, engine.TestOnly_PassExecutorState(keeper).ExecutionMode, "no early policy request");
            // Re-establish exact geometry after the preceding heartbeat moved the outfielders.
            if (hasReceiver)
            {
                Place(engine, keeper + 1, World(team, 18f, 30f));
                Place(engine, keeper + 2, World(team, 18f, 38f));
                Place(engine, keeper + 3, World(team, 20f, 14f));
            }
            engine.TestOnly_SetPhysicsFrame(due - 1);
            engine.RunTick();
            PassExecutorState executor = engine.TestOnly_PassExecutorState(keeper);
            Assert.AreEqual(1, executor.ExecutionMode, "approved policy must reach real #5 executor");
            GoalkeeperDistributionRequest request = executor.GoalkeeperRequest;
            Assert.AreEqual(due, request.FrameNumber);
            Assert.AreEqual(Delivery(policy), request.Delivery);
            int receiver = hasReceiver && policy != GkDistributionPolicy.LongKick
                ? team * 11 + (policy == GkDistributionPolicy.ThrowOut ? 3 : 1) : -1;
            Assert.AreEqual(receiver, request.TargetAgentId, "nearest/width/roster tie-break or zone");
            if (receiver < 0)
                Assert.AreEqual(new Vector3(team == 0 ? 35f : 70f, 34f, 0f), request.TargetPosition);
            Assert.AreEqual(Vector3.zero, request.SpinIntent);
            DeliveryKind delivery = policy == GkDistributionPolicy.SlowDown || policy == GkDistributionPolicy.RollOut
                ? DeliveryKind.Roll : policy == GkDistributionPolicy.Quick || policy == GkDistributionPolicy.ThrowOut
                    ? DeliveryKind.Throw : DeliveryKind.Kick;
            float coefficient = GoalkeeperDistribution.ComputeAccuracyCoeff(delivery, engine.TestOnly_GoalkeeperState.Attrs[team]);
            Assert.AreEqual(Mathf.Clamp01(Power(policy) * coefficient), request.EmittedPower01);
            Assert.AreEqual(Mathf.CeilToInt(GoalkeeperDistribution.ComputeWindupMs(delivery) * 60f / 1000f), request.WindupFrames);
            Assert.AreEqual(GoalkeeperDistribution.ComputeReleasePoint(Vector3.zero, delivery).z, request.ReleaseHeightM);
            Assert.AreEqual(GoalkeeperState.Distributing, engine.TestOnly_GkState(team));
            Assert.AreEqual(keeper, engine.PossessingAgentId);
            Assert.AreNotEqual(due / 6, engine.TestOnly_DtLastAction(keeper).HeartbeatTick, "no keeper on-ball DT dispatch");

            int terminals = 0, events = 0;
            engine.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) =>
            {
                Assert.AreEqual(keeper, id);
                Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Completed, feedback.Kind);
                Assert.IsFalse(missing);
                Assert.AreEqual(request.FrameNumber + request.WindupFrames, feedback.ContactFrame);
                Assert.AreEqual(receiver, engine.TestOnly_PassInFlightReceiverId, "W5 is armed at CONTACT only for a receiver");
                terminals++;
            };
            for (int i = 0; i < request.WindupFrames - 1; i++)
            {
                engine.RunTick();
                Assert.AreEqual(keeper, engine.PossessingAgentId, "hand control throughout WINDUP");
                Assert.AreEqual(0, CountEvents(engine));
            }
            engine.RunTick();
            events += CountEvents(engine);
            Assert.AreEqual(1, terminals);
            Assert.IsTrue(engine.TestOnly_TryGetPressPassEvent(1 - team, out PassAttemptEvent pass), "real W5 opponent pass ring");
            Assert.AreEqual(keeper, pass.AgentId);
            Assert.AreEqual(receiver, pass.TargetAgentId);
            Assert.AreEqual(request.FrameNumber + request.WindupFrames, pass.Frame);
            Assert.AreEqual(1, events);
            Assert.AreEqual(GoalkeeperState.Recovering, engine.TestOnly_GkState(team));
            Assert.IsFalse(engine.TestOnly_GoalkeeperState.DistributeIntentActive[team]);
            Assert.AreEqual(0, engine.TestOnly_PassExecutorState(keeper).ExecutionMode);
            for (int i = 0; i < 3; i++) { engine.RunTick(); events += CountEvents(engine); }
            Assert.AreEqual(1, terminals);
            Assert.AreEqual(1, events);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void LiveReceiverAndKeeper_AreSampledAtContact(int team)
        {
            MatchEngine engine = Committed(GkDistributionPolicy.Quick, team, true);
            int keeper = team * 11, receiver = keeper + 1;
            Place(engine, receiver, World(team, 24f, 28f));
            Place(engine, keeper, World(team, 12f, 34f));
            GoalkeeperDistributionFeedback terminal = default;
            engine.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) => terminal = feedback;
            while (engine.TestOnly_PassExecutorState(keeper).ExecutionMode != 0) engine.RunTick();
            Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Completed, terminal.Kind);
            Vector2 target = engine.AgentView(receiver).Position;
            Assert.AreEqual(target.x, terminal.EffectiveTargetPosition.x, 1e-4f);
            Assert.AreEqual(target.y, terminal.EffectiveTargetPosition.y, 1e-4f);
            Assert.AreEqual(engine.AgentView(keeper).Position.x, terminal.ReleasePoint.x, 1e-4f);
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void LostReceiver_FallsBackOnce_AcrossCanonicalRestore(int team, bool ownGoalLine)
        {
            MatchEngine engine = Committed(GkDistributionPolicy.Quick, team, true);
            int keeper = team * 11, receiver = keeper + 1;
            if (ownGoalLine)
            {
                // Force a valid committed fallback through the real #5 serialized request.
                var request = engine.TestOnly_PassExecutorState(keeper).GoalkeeperRequest;
                engine.TestOnly_CancelGoalkeeperDistribution(keeper);
                engine.RunTick();
                request.TargetPosition = World(team, -5f, 90f);
                request.FrameNumber = (int)engine.CurrentTick;
                Assert.IsTrue(engine.TestOnly_StartGoalkeeperDistribution(in request, out _));
                GoalkeeperTickState pending = Clone(engine.TestOnly_GoalkeeperState);
                pending.States[team] = GoalkeeperState.Distributing;
                pending.DistributionPending[team] = true;
                engine.TestOnly_RestoreGoalkeeperState(in pending);
            }
            engine.RunTick();
            SnapshotHeader header = engine.CaptureDurableHeader();
            SnapshotPayload payload = engine.CaptureDurablePayload();
            var digests = new List<byte[]>();
            GoalkeeperDistributionFeedback terminal = default;
            int terminals = 0, events = 0;
            Action<MatchEngine> invalidate = e => e.TestOnly_SetIsSentOff(receiver, true);
            Action<MatchEngine> observe = e => e.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) =>
            {
                Assert.IsTrue(missing); terminal = feedback; terminals++;
            };
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;
            try
            {
                invalidate(engine); observe(engine);
                for (int i = 0; i < 60; i++) { engine.RunTick(); digests.Add(engine.CurrentSnapshotDigest); events += CountEvents(engine); }
                Assert.AreEqual(1, terminals); Assert.AreEqual(1, events);
                Assert.AreEqual(-1, terminal.EffectiveTargetAgentId);
                if (ownGoalLine) Assert.AreEqual(World(team, 35f, 34f), terminal.EffectiveTargetPosition);
                MatchEngine restored = MatchEngine.RestoreFromSnapshot(header, payload, Seed);
                terminals = 0; events = 0; invalidate(restored); observe(restored);
                for (int i = 0; i < 60; i++)
                {
                    restored.RunTick();
                    CollectionAssert.AreEqual(digests[i], restored.CurrentSnapshotDigest, "resumed tick " + i);
                    events += CountEvents(restored);
                }
                Assert.AreEqual(1, terminals); Assert.AreEqual(1, events);
                Assert.AreEqual(-1, restored.TestOnly_PassInFlightReceiverId);
            }
            finally { UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false; }
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void LossOrRestart_CancelsWithoutLaunch(int team, bool restart)
        {
            MatchEngine engine = Committed(GkDistributionPolicy.ShortKick, team, true);
            int keeper = team * 11, count = 0;
            engine.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) =>
            {
                Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Cancelled, feedback.Kind); count++;
            };
            if (restart) engine.TestOnly_ApplyRestart(engine.AgentView(keeper).Position, team);
            else engine.TestOnly_SetPossession(keeper + 1);
            engine.RunTick();
            Assert.AreEqual(1, count);
            Assert.AreEqual(0, CountEvents(engine));
            Assert.IsFalse(engine.TestOnly_GoalkeeperState.DistributeIntentActive[team]);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RetainedCancellation_RetriesOnlyWhileBothDeadlinesPermit(int team)
        {
            MatchEngine engine = Committed(GkDistributionPolicy.ShortKick, team, false);
            int keeper = team * 11;
            int claimTick = engine.TestOnly_GoalkeeperState.ClaimTick[team];
            engine.TestOnly_CancelGoalkeeperDistribution(keeper);
            engine.RunTick();
            Assert.AreEqual(GoalkeeperState.HandsOnBall, engine.TestOnly_GkState(team));
            Assert.AreEqual(claimTick, engine.TestOnly_GoalkeeperState.ClaimTick[team]);
            int next = ((int)engine.CurrentTick / 6 + 1) * 6;
            engine.TestOnly_SetPhysicsFrame(next - 1); engine.RunTick();
            Assert.AreEqual(1, engine.TestOnly_PassExecutorState(keeper).ExecutionMode, "early retry fits");
            engine.TestOnly_CancelGoalkeeperDistribution(keeper); engine.RunTick();
            int tooLate = (claimTick + GoalkeeperConstants.GK_HOLD_MAX_TICKS) * 6 - 6;
            engine.TestOnly_SetPhysicsFrame(tooLate - 1); engine.RunTick();
            Assert.AreEqual(0, engine.TestOnly_PassExecutorState(keeper).ExecutionMode, "late retry refused");
            Assert.AreEqual(GoalkeeperState.HandsOnBall, engine.TestOnly_GkState(team));
            Assert.AreEqual(claimTick, engine.TestOnly_GoalkeeperState.ClaimTick[team]);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void DefensiveLateAcceptedContact_IsCancelledAtEarlierHandBoundary(int team)
        {
            MatchEngine engine = Fixture(GkDistributionPolicy.ShortKick, team, false);
            int keeper = team * 11;
            GoalkeeperTickState state = Clone(engine.TestOnly_GoalkeeperState);
            int boundary = (state.ClaimTick[team] + GoalkeeperConstants.GK_HOLD_MAX_TICKS) * 6;
            engine.TestOnly_SetPhysicsFrame(boundary - 2);
            var request = new GoalkeeperDistributionRequest
            {
                AgentId = keeper,
                TeamId = team,
                Delivery = GoalkeeperDeliveryVariant.Kick,
                TargetAgentId = -1,
                TargetPosition = World(team, 35f, 34f),
                EmittedPower01 = 0.5f,
                SpinIntent = Vector3.zero,
                ReleaseHeightM = 0.2f,
                WindupFrames = 1,
                FrameNumber = boundary - 2
            };
            Assert.IsTrue(engine.TestOnly_StartGoalkeeperDistribution(in request, out _));
            state.States[team] = GoalkeeperState.Distributing;
            state.DistributionPending[team] = true;
            engine.TestOnly_RestoreGoalkeeperState(in state);
            int cancelled = 0;
            engine.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) =>
            {
                Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Cancelled, feedback.Kind); cancelled++;
            };
            engine.RunTick(); // WINDUP expires; CONTACT waits for boundary tick.
            Assert.AreEqual(2, engine.TestOnly_PassExecutorState(keeper).State);
            engine.RunTick(); // Earlier inherited hand boundary cancels before CONTACT.
            Assert.AreEqual(1, cancelled); Assert.AreEqual(0, CountEvents(engine));
            Assert.AreEqual(keeper, engine.PossessingAgentId);
            Assert.AreEqual(GoalkeeperState.HandsOnBall, engine.TestOnly_GkState(team));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void RepeatedRejections_PreserveClocks_AndStopWhenBudgetCloses(int team)
        {
            MatchEngine engine = Fixture(GkDistributionPolicy.ShortKick, team, false);
            int keeper = team * 11;
            int claimTick = engine.TestOnly_GoalkeeperState.ClaimTick[team];
            int due = (claimTick + Delay(GkDistributionPolicy.ShortKick)) * 6;
            engine.TestOnly_SetPhysicsFrame(due);
            var ordinary = new PassRequest
            {
                AgentId = keeper,
                TeamId = team,
                PassType = PassType.Ground,
                CrossSubType = CrossSubType.Flat,
                TargetAgentId = keeper + 1,
                TargetPosition = World(team, 35f, 34f),
                IntendedDistance = 25f,
                Urgency = 0.5f,
                FrameNumber = due
            };
            Assert.AreEqual(PassOutcome.Initiated, engine.TestOnly_InitiatePass(keeper, in ordinary).Outcome);
            int rejected = 0;
            engine.TestOnly_W8BTerminalFeedbackObserver = (id, feedback, missing) =>
            {
                Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Rejected, feedback.Kind); rejected++;
            };
            int stoppedAt = -1;
            for (int frame = due; frame < (claimTick + GoalkeeperConstants.GK_HOLD_MAX_TICKS) * 6; frame += 6)
            {
                engine.TestOnly_SetPhysicsFrame(frame);
                int before = rejected;
                engine.TestOnly_RunGoalkeeperDistributionProducer();
                if (before == rejected && stoppedAt < 0) stoppedAt = frame;
                if (stoppedAt >= 0) Assert.AreEqual(before, rejected, "closed budget stays closed");
                Assert.AreEqual(claimTick, engine.TestOnly_GoalkeeperState.ClaimTick[team]);
                Assert.IsFalse(engine.TestOnly_GoalkeeperState.DistributeIntentActive[team]);
                Assert.AreEqual(GoalkeeperState.HandsOnBall, engine.TestOnly_GkState(team));
            }
            Assert.Greater(rejected, 1); Assert.Greater(stoppedAt, due);
        }

        [TestCaseSource(nameof(Policies))]
        public void Producer_ConsumesNoRng_AndCannotDuplicateAcceptedRequest(
            GkDistributionPolicy policy, int team, bool receiver)
        {
            MatchEngine engine = Fixture(policy, team, receiver);
            engine.TestOnly_SetPhysicsFrame((ClaimFrame / 6 + Delay(policy)) * 6);
            int count = engine.Rng.StreamCount;
            var streams = new RngStreamState[count];
            for (int i = 0; i < count; i++) streams[i] = engine.Rng.GetStreamState(i);
            engine.TestOnly_RunGoalkeeperDistributionProducer();
            GoalkeeperDistributionRequest request = engine.TestOnly_PassExecutorState(team * 11).GoalkeeperRequest;
            engine.TestOnly_RunGoalkeeperDistributionProducer();
            Assert.AreEqual(request.FrameNumber, engine.TestOnly_PassExecutorState(team * 11).GoalkeeperRequest.FrameNumber);
            Assert.AreEqual(1, engine.TestOnly_PassExecutorState(team * 11).ExecutionMode);
            Assert.AreEqual(count, engine.Rng.StreamCount);
            for (int i = 0; i < count; i++)
            {
                Assert.AreEqual(streams[i].RngCursor, engine.Rng.GetStreamState(i).RngCursor);
                Assert.AreEqual(streams[i].ActionOrdinal, engine.Rng.GetStreamState(i).ActionOrdinal);
            }
        }

        [TestCaseSource(nameof(Policies))]
        public void RemovingLastCandidatesBeforeCommit_UsesPolicyPreservingZone(
            GkDistributionPolicy policy, int team, bool unused)
        {
            MatchEngine engine = Fixture(policy, team, true);
            for (int i = 1; i <= 3; i++) engine.TestOnly_SetIsSentOff(team * 11 + i, true);
            engine.TestOnly_SetPhysicsFrame((ClaimFrame / 6 + Delay(policy)) * 6 - 1);
            engine.RunTick();
            GoalkeeperDistributionRequest request = engine.TestOnly_PassExecutorState(team * 11).GoalkeeperRequest;
            Assert.AreEqual(1, engine.TestOnly_PassExecutorState(team * 11).ExecutionMode);
            Assert.AreEqual(-1, request.TargetAgentId);
            Assert.AreEqual(Delivery(policy), request.Delivery);
            Assert.AreEqual(World(team, 35f, 34f), request.TargetPosition);
        }

        [Test]
        public void ConfiguredFirstAttemptBudget_IsStrictlyBeforeBothGuards()
        {
            foreach (GkDistributionPolicy policy in Enum.GetValues(typeof(GkDistributionPolicy)))
            {
                DeliveryKind kind = Delivery(policy) == GoalkeeperDeliveryVariant.Roll ? DeliveryKind.Roll
                    : Delivery(policy) == GoalkeeperDeliveryVariant.Throw ? DeliveryKind.Throw : DeliveryKind.Kick;
                int windup = Mathf.CeilToInt(GoalkeeperDistribution.ComputeWindupMs(kind) * 60f / 1000f);
                int contact = Delay(policy) * 6 + windup + 1;
                Assert.Less(contact, GoalkeeperConstants.GK_HOLD_MAX_TICKS * 6 - 5);
                Assert.Less(contact, MatchEngineConstants.GkMaxHoldTicks);
            }
            Assert.Less(TacticalInstructionsConstants.GK_DIST_MAX_POLICY_DELAY_TICKS * 6 + 84 + 1,
                GoalkeeperConstants.GK_HOLD_MAX_TICKS * 6 - 5, "maximum permitted 1400 ms windup");
        }

        internal static MatchEngine Fixture(GkDistributionPolicy policy, int team, bool hasReceiver)
        {
            var engine = new MatchEngine(Seed);
            engine.EnableGkHeading();
            var tactic = new TeamTactic(Mentality.Balanced, TacticFormation.F442, Tempo.Standard, TacticWidth.Standard,
                TacticPassing.Mixed, TacticPressing.Medium, LineOfEngagement.Standard, 0.5f, TacticDefWidth.Standard,
                TransitionPlan.HoldShape, TransitionPlan.Regroup, false, TacticTriggerMask.None, FocusPlay.Mixed, policy, 0);
            engine.SetTeamTactic(team, in tactic);
            for (int i = 0; i < 6; i++) engine.RunTick();
            for (int i = 0; i < 22; i++) Place(engine, i, World(team, 70f, i * 2f + 10f));
            int keeper = team * 11;
            Place(engine, keeper, World(team, 10f, 34f));
            if (hasReceiver)
            {
                Place(engine, keeper + 1, World(team, 18f, 30f));
                Place(engine, keeper + 2, World(team, 18f, 38f));
                Place(engine, keeper + 3, World(team, 20f, 14f));
            }
            engine.TestOnly_SetPhysicsFrame(ClaimFrame);
            engine.TestOnly_SetPossession(keeper);
            GoalkeeperTickState state = Clone(engine.TestOnly_GoalkeeperState);
            state.States[team] = GoalkeeperState.HandsOnBall;
            state.ClaimTick[team] = ClaimFrame / 6;
            state.ReleaseTickEarliest[team] = ClaimFrame / 6 + 1;
            engine.TestOnly_RestoreGoalkeeperState(in state);
            return engine;
        }

        private static MatchEngine Committed(GkDistributionPolicy policy, int team, bool receiver)
        {
            MatchEngine engine = Fixture(policy, team, receiver);
            engine.TestOnly_SetPhysicsFrame((ClaimFrame / 6 + Delay(policy)) * 6 - 1);
            engine.RunTick();
            Assert.AreEqual(1, engine.TestOnly_PassExecutorState(team * 11).ExecutionMode);
            return engine;
        }

        private static Vector3 World(int team, float x, float y) => new Vector3(team == 0 ? x : 105f - x, y, 0f);
        private static void Place(MatchEngine engine, int id, Vector3 position)
        {
            AgentState state = engine.TestOnly_AgentSnapshot(id);
            state.Position = new Vector2(position.x, position.y); state.Velocity = Vector2.zero;
            state.LastValidPosition = state.Position; state.LastValidVelocity = Vector2.zero;
            engine.TestOnly_SetAgent(id, in state);
        }
        private static int CountEvents(MatchEngine engine)
        {
            int count = 0;
            for (int i = 0; i < engine.TickLedgerCount; i++)
                if (engine.TickLedgerOrdinal(i) == DistributionOrdinal) count++;
            return count;
        }
        private static int Delay(GkDistributionPolicy p) => p == GkDistributionPolicy.SlowDown ? 35 : p == GkDistributionPolicy.Quick ? 5 : 10;
        private static GoalkeeperDeliveryVariant Delivery(GkDistributionPolicy p) =>
            p == GkDistributionPolicy.SlowDown || p == GkDistributionPolicy.RollOut ? GoalkeeperDeliveryVariant.Roll
                : p == GkDistributionPolicy.Quick || p == GkDistributionPolicy.ThrowOut ? GoalkeeperDeliveryVariant.Throw : GoalkeeperDeliveryVariant.Kick;
        private static float Power(GkDistributionPolicy p)
        {
            switch (p)
            {
                case GkDistributionPolicy.SlowDown: return 0.50f;
                case GkDistributionPolicy.Quick: return 0.75f;
                case GkDistributionPolicy.ShortKick: return 0.55f;
                case GkDistributionPolicy.LongKick: return 0.90f;
                case GkDistributionPolicy.RollOut: return 0.55f;
                default: return 0.70f;
            }
        }
        private static GoalkeeperTickState Clone(GoalkeeperTickState s) =>
            new GoalkeeperTickState(
                (GoalkeeperState[])s.States.Clone(),
                (GoalkeeperAgentAttributes[])s.Attrs.Clone(),
                (GkContactState[])s.ContactStates.Clone(),
                (SaveIntent[])s.SaveIntents.Clone(),
                (bool[])s.SaveIntentActive.Clone(),
                (ClaimIntent[])s.ClaimIntents.Clone(),
                (bool[])s.ClaimIntentActive.Clone(),
                (RushIntent[])s.RushIntents.Clone(),
                (bool[])s.RushIntentActive.Clone(),
                (DistributeIntent[])s.DistributeIntents.Clone(),
                (bool[])s.DistributeIntentActive.Clone(),
                (GoalkeeperPositioningContract[])s.PositioningContracts.Clone(),
                (int[])s.DiveLaunchFrames.Clone(),
                (int[])s.DiveDurationFrames.Clone(),
                (float[])s.DivePeakHandZ.Clone(),
                (float[])s.DiveDirectionLateral.Clone(),
                (float[])s.RushLaunchMps.Clone(),
                (int[])s.RushInitialAttackerId.Clone(),
                (float[])s.ShotDetectedTickMs.Clone(),
                (float[])s.RequiredReactionMs.Clone(),
                (bool[])s.ShotEventPending.Clone(),
                (int[])s.ClaimTick.Clone(),
                (int[])s.ReleaseTickEarliest.Clone(),
                (int[])s.RecoveryCooldownEndTick.Clone(),
                (bool[])s.DistributionPending.Clone());
    }
}

#region VersionHistory
// | Version | Date | Author | Notes |
// | 1.0 | 2026-10-15 | — | Composed W8 B policy, CONTACT, cancellation, deadline and replay locks. |
#endregion
