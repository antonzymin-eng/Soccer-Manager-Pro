// File:     src/match-engine/tests/KeeperOutsideAreaHandlingTests.cs
// Created:  2026-10-18
// Modified: 2026-10-18
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.1.4; match-flow §5.2; D-07 option A.
// Purpose:  Verify outside-area hand contacts and carries, restart precedence and canonical restore.

using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.PassMechanics;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>Mirrored Law 12 outside-area handling and continuous-control regression locks.</summary>
    [TestFixture]
    public sealed class KeeperOutsideAreaHandlingTests
    {
        [TestCase(0, 16.5f, 34f, true)]
        [TestCase(1, 16.5f, 34f, true)]
        [TestCase(0, 16.51f, 34f, false)]
        [TestCase(1, 16.51f, 34f, false)]
        [TestCase(0, 10f, 13.84f, true)]
        [TestCase(1, 10f, 13.84f, true)]
        [TestCase(0, 10f, 13.83f, false)]
        [TestCase(1, 10f, 13.83f, false)]
        [TestCase(0, 10f, 54.16f, true)]
        [TestCase(1, 10f, 54.16f, true)]
        [TestCase(0, 10f, 54.17f, false)]
        [TestCase(1, 10f, 54.17f, false)]
        [TestCase(0, 95f, 34f, false)]
        [TestCase(1, 95f, 34f, false)]
        public void HandContact_UsesBallLocationAndOwnArea(int team, float depth, float y, bool legal)
        {
            MatchEngine engine = Fixture(team);
            Vector3 contact = World(team, depth, y);
            // The keeper stays inside: the ball's contact position determines the offence.
            engine.TestOnly_ForceBallLoose(contact, Vector3.zero);
            Assert.AreEqual(-1, engine.TestOnly_ReleasedHandKeeper, "No own-release restriction is armed.");
            Assert.AreEqual(legal, engine.TestOnly_TryKeeperHandContact(team * 11, contact));
            if (legal) return;
            engine.RunTick();
            AssertDirectFreeKick(engine, team, contact);
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void ActualSmotherOutsideArea_IsDeniedBeforeAcquisition_AcrossRestore(int team, bool restore)
        {
            MatchEngine engine = Fixture(team);
            Vector3 contact = World(team, 20f, 34f);
            Place(engine, team * 11, contact);
            engine.TestOnly_ForceBallLoose(contact, Vector3.zero);
            GoalkeeperTickState state = engine.TestOnly_GoalkeeperState;
            state.States[team] = GoalkeeperState.Smothered;
            engine.TestOnly_RestoreGoalkeeperState(in state);
            engine.TestOnly_RunPhysicsPhase();
            Assert.AreEqual(-1, engine.PossessingAgentId, "Illegal handling must not become possession.");
            Assert.AreEqual(GoalkeeperState.Recovering, engine.TestOnly_GkState(team));
            Assert.AreEqual(-1, engine.TestOnly_ReleasedHandKeeper);
            if (restore) engine = Clone(engine);
            engine.RunTick();
            AssertDirectFreeKick(engine, team, contact);
            engine.RunTick();
            Assert.AreEqual(0, HandlingRestartCount(engine), "One contact cannot produce another whistle.");
        }

        [TestCase(0, 16.51f, 34f, false)]
        [TestCase(1, 16.51f, 34f, false)]
        [TestCase(0, 10f, 13.83f, false)]
        [TestCase(1, 10f, 54.17f, false)]
        [TestCase(0, 16.51f, 34f, true)]
        [TestCase(1, 16.51f, 34f, true)]
        public void HandCarryOutsideArea_WhistlesImmediately_AcrossRestore(int team, float depth, float y, bool restore)
        {
            MatchEngine engine = Fixture(team);
            Vector3 contact = World(team, depth, y);
            Place(engine, team * 11, contact);
            if (restore) engine = Clone(engine);
            engine.RunTick();
            AssertDirectFreeKick(engine, team, contact);
            Assert.AreEqual(0, engine.TestOnly_GkHoldTicks);
            Assert.AreNotEqual(GoalkeeperState.HandsOnBall, engine.TestOnly_GkState(team));
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void OutsideAreaFeet_AndOnBoundaryHands_RemainLegal(int team, bool feet)
        {
            MatchEngine engine = Fixture(team);
            if (feet)
            {
                GoalkeeperTickState state = engine.TestOnly_GoalkeeperState;
                state.States[team] = GoalkeeperState.Set;
                engine.TestOnly_RestoreGoalkeeperState(in state);
            }
            Place(engine, team * 11, World(team, feet ? 20f : 16.5f, 34f));
            engine.RunTick();
            Assert.AreEqual(RestartCue.None, engine.RestartAppliedThisTick);
            Assert.AreEqual(team * 11, engine.PossessingAgentId);
            Assert.AreEqual(0, HandlingRestartCount(engine));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void KeeperBodyOutside_BallContactInside_RemainsLegal(int team)
        {
            MatchEngine engine = Fixture(team);
            GoalkeeperTickState state = engine.TestOnly_GoalkeeperState;
            state.States[team] = GoalkeeperState.Set;
            engine.TestOnly_RestoreGoalkeeperState(in state);
            Place(engine, team * 11, World(team, 17f, 34f));
            Vector3 contact = World(team, 16.5f, 34f);
            engine.TestOnly_ForceBallLoose(contact, Vector3.zero);
            Assert.IsTrue(engine.TestOnly_TryKeeperHandContact(team * 11, contact));
            engine.RunTick();
            Assert.AreEqual(0, HandlingRestartCount(engine));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void HandCarryCrossesBoundaryDuringMovement_WhistlesAtSettledBall(int team)
        {
            MatchEngine engine = Fixture(team);
            int keeper = team * 11;
            Place(engine, keeper, World(team, 16.49f, 34f));
            AgentState agent = engine.TestOnly_AgentSnapshot(keeper);
            agent.Velocity = new Vector2(team == 0 ? 4f : -4f, 0f);
            agent.Speed = 4f;
            agent.FacingDirection = agent.Velocity.normalized;
            engine.TestOnly_SetAgent(keeper, in agent);
            Vector3 target = World(team, 20f, 34f);
            MovementCommand move = MovementCommand.MoveTo(new Vector2(target.x, target.y));
            engine.TestOnly_SetCommand(keeper, in move);
            engine.RunTick();
            Vector2 settled = engine.AgentView(keeper).Position;
            Assert.Greater(Mathf.Abs(settled.x - (team == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M)), 16.5f);
            AssertDirectFreeKick(engine, team, new Vector3(settled.x, settled.y, MatchEngineConstants.BALL_REST_HEIGHT_M));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void OutsideHandling_DiscardsCapturedFoul_WithoutDelayedWhistle(int team)
        {
            MatchEngine engine = Fixture(team);
            engine.TestOnly_InjectFoulCandidate(team * 11 + 1, (1 - team) * 11 + 1);
            Vector3 contact = World(team, 20f, 34f);
            engine.TestOnly_ForceBallLoose(contact, Vector3.zero);
            Assert.IsFalse(engine.TestOnly_TryKeeperHandContact(team * 11, contact));
            engine.RunTick();
            AssertDirectFreeKick(engine, team, contact);
            Assert.AreEqual(0f, engine.TestOnly_FoulCandidateForceN);
            engine.RunTick();
            Assert.AreEqual(0, HandlingRestartCount(engine));
            Assert.AreEqual(RestartCue.None, engine.RestartAppliedThisTick);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void CollisionPushingHandCarrierOutside_WhistlesBeforeExecutors(int team)
        {
            MatchEngine engine = Fixture(team);
            Place(engine, team * 11, World(team, 16.49f, 34f));
            Place(engine, team * 11 + 1, World(team, 16f, 34f));
            engine.TestOnly_RunResolvePhase();
            Vector2 keeperPosition = engine.AgentView(team * 11).Position;
            Vector3 contact = new Vector3(keeperPosition.x, keeperPosition.y, MatchEngineConstants.BALL_REST_HEIGHT_M);
            Assert.Greater(Mathf.Abs(contact.x - (team == 0 ? 0f : MatchEngineConstants.PITCH_LENGTH_M)), 16.5f,
                "Fixture must push the holder across the penalty-area line.");
            Assert.AreEqual(RestartCue.FreeKick, engine.RestartAppliedThisTick);
            Assert.AreEqual(1 - team, engine.RestartAwardedTeam);
            Assert.AreEqual(contact.x, engine.TestOnly_BallSnapshot.Position.x, 0.0001f);
            Assert.AreEqual(-1, engine.TestOnly_IndirectKickTaker);
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void CarryOutsideAtDistributionContact_CancelsLaunch_AcrossRestore(int team, bool restore)
        {
            MatchEngine engine = Fixture(team);
            int keeper = team * 11;
            var request = new GoalkeeperDistributionRequest
            {
                AgentId = keeper,
                TeamId = team,
                Delivery = GoalkeeperDeliveryVariant.Throw,
                TargetAgentId = -1,
                TargetPosition = World(team, 35f, 34f),
                EmittedPower01 = 0.5f,
                SpinIntent = Vector3.zero,
                ReleaseHeightM = 1f,
                WindupFrames = 1,
                FrameNumber = (int)engine.CurrentTick
            };
            Assert.IsTrue(engine.TestOnly_StartGoalkeeperDistribution(in request, out _));
            GoalkeeperTickState state = engine.TestOnly_GoalkeeperState;
            state.States[team] = GoalkeeperState.Distributing;
            state.DistributionPending[team] = true;
            engine.TestOnly_RestoreGoalkeeperState(in state);
            engine.RunTick();
            Assert.AreEqual(2, engine.TestOnly_PassExecutorState(keeper).State, "Next update would release at CONTACT.");
            Vector3 contact = World(team, 17f, 34f);
            Place(engine, keeper, contact);
            if (restore) engine = Clone(engine);
            engine.RunTick();
            AssertDirectFreeKick(engine, team, contact);
            Assert.AreEqual(GoalkeeperDistributionFeedbackKind.Cancelled,
                engine.TestOnly_PassExecutorState(keeper).GoalkeeperFeedback.Kind);
            Assert.AreEqual(0, DistributionCount(engine));
            Assert.AreEqual(BallStateType.Stationary, engine.TestOnly_BallSnapshot.State);
            engine.RunTick();
            Assert.AreEqual(0, DistributionCount(engine), "Cancellation cannot launch on the next frame either.");
            Assert.AreEqual(0, HandlingRestartCount(engine));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void QueuedOutsideOffence_RestoresWithIdenticalContinuation(int team)
        {
            MatchEngine engine = Fixture(team);
            Vector3 contact = World(team, 20f, 34f);
            Place(engine, team * 11, contact);
            engine.TestOnly_RunPhysicsPhase(); // Continuous-control offence is now pending, no release history.
            Assert.AreEqual(-1, engine.TestOnly_ReleasedHandKeeper);
            SnapshotHeader header = engine.CaptureDurableHeader();
            SnapshotPayload payload = engine.CaptureDurablePayload();
            SnapshotPayload overlay = engine.CapturePostSnapshotWorldState();
            var digests = new List<byte[]>();
            for (int i = 0; i < 12; i++)
            {
                engine.RunTick();
                if (i == 0) AssertDirectFreeKick(engine, team, contact);
                digests.Add(engine.CurrentSnapshotDigest);
            }
            MatchEngine restored = MatchEngine.RestoreFromSnapshot(header, payload, engine.MatchSeed);
            restored.ApplyPostSnapshotWorldState(overlay, null);
            for (int i = 0; i < digests.Count; i++)
            {
                restored.RunTick();
                if (i == 0) AssertDirectFreeKick(restored, team, contact);
                CollectionAssert.AreEqual(digests[i], restored.CurrentSnapshotDigest, "resumed tick " + i);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        public void OutsideAreaDirectFreeKick_CanScoreDirectly(int team)
        {
            MatchEngine engine = Fixture(team);
            Vector3 contact = World(team, 20f, 34f);
            engine.TestOnly_ForceBallLoose(contact, Vector3.zero);
            Assert.IsFalse(engine.TestOnly_TryKeeperHandContact(team * 11, contact));
            engine.RunTick();
            AssertDirectFreeKick(engine, team, contact);
            engine.TestOnly_ForceBallLoose(new Vector3(team == 0 ? -1f : 106f, 34f, 1f), Vector3.zero);
            engine.RunTick();
            Assert.AreEqual(1, engine.HomeScore + engine.AwayScore);
            Assert.AreEqual(RestartCue.KickOff, engine.RestartAppliedThisTick);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void BallAlreadyOutOfPlay_TakesBoundaryRestart(int team)
        {
            MatchEngine engine = Fixture(team);
            Vector3 outside = World(team, 20f, 69f);
            engine.TestOnly_ForceBallLoose(outside, Vector3.zero);
            Assert.IsFalse(engine.TestOnly_TryKeeperHandContact(team * 11, outside));
            engine.RunTick();
            Assert.AreEqual(RestartCue.ThrowIn, engine.RestartAppliedThisTick);
            Assert.AreEqual(0, HandlingRestartCount(engine));
        }

        private static MatchEngine Fixture(int team)
        {
            MatchEngine engine = W8BDistributionTests.Fixture(GkDistributionPolicy.SlowDown, team, false);
            // Commit the fixture's forced clock/state to a canonical snapshot before overlay tests.
            engine.RunTick();
            // Avoid a tactical heartbeat changing this forced contact geometry.
            Place(engine, team * 11, World(team, 10f, 34f));
            return engine;
        }

        private static void Place(MatchEngine engine, int agentId, Vector3 position)
        {
            AgentState agent = engine.TestOnly_AgentSnapshot(agentId);
            agent.Position = new Vector2(position.x, position.y);
            agent.Velocity = Vector2.zero;
            agent.Speed = 0f;
            agent.LastValidPosition = agent.Position;
            agent.LastValidVelocity = Vector2.zero;
            engine.TestOnly_SetAgent(agentId, in agent);
            MovementCommand stop = MovementCommand.Stop(agent.Position);
            engine.TestOnly_SetCommand(agentId, in stop);
        }

        private static void AssertDirectFreeKick(MatchEngine engine, int offenderTeam, Vector3 contact)
        {
            Assert.AreEqual(RestartCue.FreeKick, engine.RestartAppliedThisTick);
            Assert.AreEqual(1 - offenderTeam, engine.RestartAwardedTeam);
            Assert.AreEqual(-1, engine.TestOnly_IndirectKickTaker, "Outside handling is a direct free kick.");
            Assert.AreEqual(contact.x, engine.TestOnly_BallSnapshot.Position.x, 0.0001f);
            Assert.AreEqual(contact.y, engine.TestOnly_BallSnapshot.Position.y, 0.0001f);
            Assert.AreEqual(1, HandlingRestartCount(engine));
            byte ordinal = EventRegistry.GetOrdinal<RestartAwardedEvent>();
            for (int i = 0; i < engine.TickLedgerCount; i++)
            {
                if (engine.TickLedgerOrdinal(i) != ordinal) continue;
                RestartAwardedEvent evt = engine.TickLedgerRecord<RestartAwardedEvent>(i);
                Assert.AreEqual(RestartAwardedEvent.DIRECT_FREE_KICK, evt.RestartKind);
                Assert.AreEqual(1 - offenderTeam, evt.AwardedTeam);
                Assert.AreEqual(contact, evt.Location);
            }
        }

        private static int HandlingRestartCount(MatchEngine engine)
        {
            byte ordinal = EventRegistry.GetOrdinal<RestartAwardedEvent>();
            int count = 0;
            for (int i = 0; i < engine.TickLedgerCount; i++)
                if (engine.TickLedgerOrdinal(i) == ordinal &&
                    engine.TickLedgerRecord<RestartAwardedEvent>(i).RestartKind >= RestartAwardedEvent.INDIRECT_FREE_KICK)
                    count++;
            return count;
        }

        private static int DistributionCount(MatchEngine engine)
        {
            byte ordinal = EventRegistry.GetOrdinal<DistributionExecutedEvent>();
            int count = 0;
            for (int i = 0; i < engine.TickLedgerCount; i++)
                if (engine.TickLedgerOrdinal(i) == ordinal) count++;
            return count;
        }

        private static MatchEngine Clone(MatchEngine engine)
        {
            SnapshotHeader header = engine.CaptureDurableHeader();
            SnapshotPayload payload = engine.CaptureDurablePayload();
            SnapshotPayload overlay = engine.CapturePostSnapshotWorldState();
            MatchEngine restored = MatchEngine.RestoreFromSnapshot(header, payload, engine.MatchSeed);
            restored.ApplyPostSnapshotWorldState(overlay, null);
            return restored;
        }

        private static Vector3 World(int team, float depth, float y) =>
            new Vector3(team == 0 ? depth : MatchEngineConstants.PITCH_LENGTH_M - depth,
                y, MatchEngineConstants.BALL_REST_HEIGHT_M);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0 | 2026-10-18 | — | D-07 outside-area handling, carries, inclusive boundaries and restore. |
#endregion
