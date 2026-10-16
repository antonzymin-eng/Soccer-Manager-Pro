// File:     src/match-engine/tests/KeeperRehandlingLawTests.cs
// Created:  2026-10-16
// Modified: 2026-10-16
// Author:   —
// Spec:     Owner-approved separate re-handling fix; IFAB Laws 12.2 / 13.
// Purpose:  Mirror actual distribution, illegal hand contact, restarts and canonical continuation.

using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.DeterministicSim;
using TacticalDirector.GoalkeeperMechanics;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class KeeperRehandlingLawTests
    {
        [TestCase(0)]
        [TestCase(1)]
        public void OwnRelease_AndOwnFeetTouch_StillForbidHands(int team)
        {
            MatchEngine engine = Released(team);
            int keeper = team * 11;
            engine.TestOnly_SetPossession(keeper);
            Assert.AreEqual(keeper, engine.PossessingAgentId, "Playing with the feet is legal.");
            Assert.AreEqual(keeper, engine.TestOnly_ReleasedHandKeeper);
            Assert.IsFalse(engine.TestOnly_TryKeeperHandContact(keeper, World(team, 12f, 34f)));
            engine.RunTick();
            Assert.AreEqual(RestartCue.FreeKick, engine.RestartAppliedThisTick);
            Assert.AreEqual(1 - team, engine.RestartAwardedTeam);
            Assert.AreEqual(engine.PossessingAgentId, engine.TestOnly_IndirectKickTaker);
            Assert.AreEqual(-1, engine.TestOnly_ReleasedHandKeeper);
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void AnotherPlayerTouch_AllowsNewHandContact(int team, bool opponent)
        {
            MatchEngine engine = Released(team);
            int toucher = (opponent ? 1 - team : team) * 11 + 1;
            engine.TestOnly_SetPossession(toucher);
            Assert.AreEqual(-1, engine.TestOnly_ReleasedHandKeeper);
            Assert.IsTrue(engine.TestOnly_TryKeeperHandContact(team * 11, World(team, 12f, 34f)));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ActualBodyDeflection_WithoutControl_DischargesRestriction(int team)
        {
            MatchEngine engine = Released(team);
            int toucher = team * 11 + 1;
            AgentState agent = engine.TestOnly_AgentSnapshot(toucher);
            agent.Position = new Vector2(50f, 34f);
            agent.Velocity = Vector2.zero;
            engine.TestOnly_SetAgent(toucher, in agent);
            engine.TestOnly_ForceBallLoose(new Vector3(50.2f, 34f, 1.2f), new Vector3(-20f, 0f, 0f));
            engine.TestOnly_RunResolvePhase();
            Assert.AreEqual(-1, engine.PossessingAgentId, "An airborne body deflection is not control.");
            Assert.AreEqual(-1, engine.TestOnly_ReleasedHandKeeper,
                "An applied body response is a touch even when no player acquires possession.");
            Assert.IsTrue(engine.TestOnly_TryKeeperHandContact(team * 11, World(team, 12f, 34f)));
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void RealSmotherContact_RejectsClaim_ThenAwardsRestart(int team, bool restore)
        {
            MatchEngine engine = Released(team);
            if (restore) engine = Clone(engine);
            int keeper = team * 11;
            AgentState agent = engine.TestOnly_AgentSnapshot(keeper);
            agent.Position = new Vector2(World(team, 12f, 34f).x, 34f);
            agent.Velocity = Vector2.zero;
            engine.TestOnly_SetAgent(keeper, in agent);
            engine.TestOnly_ForceBallLoose(World(team, 12f, 34f), Vector3.zero);
            GoalkeeperTickState state = engine.TestOnly_GoalkeeperState;
            state.States[team] = GoalkeeperState.Smothered;
            engine.TestOnly_RestoreGoalkeeperState(in state);
            engine.TestOnly_RunPhysicsPhase();
            Assert.AreEqual(-1, engine.PossessingAgentId, "No illegal claim may enter possession.");
            Assert.AreEqual(GoalkeeperState.Recovering, engine.TestOnly_GkState(team));
            engine.TestOnly_RunResolvePhase();
            Assert.AreEqual(RestartCue.FreeKick, engine.RestartAppliedThisTick);
            Assert.AreEqual(1 - team, engine.RestartAwardedTeam);
            Assert.GreaterOrEqual(engine.TestOnly_IndirectKickTaker, 0);
        }

        [TestCase(0, 3f, 34f, true, 5.5f)]
        [TestCase(1, 3f, 34f, true, 5.5f)]
        [TestCase(0, 16.5f, 34f, true, 16.5f)]
        [TestCase(1, 16.5f, 34f, true, 16.5f)]
        [TestCase(0, 16.51f, 34f, false, 16.51f)]
        [TestCase(1, 16.51f, 34f, false, 16.51f)]
        [TestCase(0, 12f, 54.17f, false, 12f)]
        [TestCase(1, 12f, 54.17f, false, 12f)]
        public void PendingOffence_RestoresAreaAndLaw13Placement(int team, float depth, float y,
            bool indirect, float restartDepth)
        {
            MatchEngine engine = Released(team);
            Assert.IsFalse(engine.TestOnly_TryKeeperHandContact(team * 11, World(team, depth, y)));
            engine = Clone(engine);
            engine.RunTick();
            Assert.AreEqual(RestartCue.FreeKick, engine.RestartAppliedThisTick);
            Assert.AreEqual(1 - team, engine.RestartAwardedTeam);
            Assert.AreEqual(indirect, engine.TestOnly_IndirectKickTaker >= 0);
            Assert.AreEqual(World(team, restartDepth, y).x, engine.TestOnly_BallSnapshot.Position.x, 0.0001f);
            Assert.AreEqual(y, engine.TestOnly_BallSnapshot.Position.y, 0.0001f);
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(0, true)]
        [TestCase(1, true)]
        public void IndirectKick_CannotScoreDirectly_AcrossRestore(int team, bool ownGoal)
        {
            MatchEngine engine = Released(team);
            Assert.IsFalse(engine.TestOnly_TryKeeperHandContact(team * 11, World(team, 12f, 34f)));
            engine.RunTick();
            engine = Clone(engine);
            int awarded = 1 - team;
            // The awarded side attacks the offending keeper's goal; own-goal is the other end.
            bool rightGoal = ownGoal ? awarded == 1 : awarded == 0;
            engine.TestOnly_ForceBallLoose(new Vector3(rightGoal ? 106f : -1f, 34f, 1f), Vector3.zero);
            engine.RunTick();
            Assert.AreEqual(0, engine.HomeScore + engine.AwayScore);
            Assert.AreEqual(ownGoal ? RestartCue.Corner : RestartCue.GoalKick, engine.RestartAppliedThisTick);
            Assert.AreEqual(1 - awarded, engine.RestartAwardedTeam);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Restriction_RestoresWithDeterministicContinuation(int team)
        {
            MatchEngine engine = Released(team);
            SnapshotHeader header = engine.CaptureDurableHeader();
            SnapshotPayload payload = engine.CaptureDurablePayload();
            var digests = new List<byte[]>();
            for (int i = 0; i < 90; i++) { engine.RunTick(); digests.Add(engine.CurrentSnapshotDigest); }
            MatchEngine restored = MatchEngine.RestoreFromSnapshot(header, payload, engine.MatchSeed);
            Assert.AreEqual(team * 11, restored.TestOnly_ReleasedHandKeeper);
            for (int i = 0; i < digests.Count; i++)
            {
                restored.RunTick();
                CollectionAssert.AreEqual(digests[i], restored.CurrentSnapshotDigest, "resumed tick " + i);
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        public void Restart_ClearsOwnReleaseRestriction(int team)
        {
            MatchEngine engine = Released(team);
            engine.TestOnly_ApplyRestart(new Vector2(30f, 34f), team);
            Assert.AreEqual(-1, engine.TestOnly_ReleasedHandKeeper);
            Assert.IsTrue(engine.TestOnly_TryKeeperHandContact(team * 11, World(team, 12f, 34f)));
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ContinuousHandControl_DoesNotArmRehandling(int team)
        {
            MatchEngine engine = W8BDistributionTests.Fixture(GkDistributionPolicy.Quick, team, false);
            Assert.IsTrue(engine.TestOnly_TryKeeperHandContact(team * 11, World(team, 10f, 34f)));
            Assert.IsTrue(engine.TestOnly_TryKeeperHandContact(team * 11, World(team, 10f, 34f)));
            Assert.AreEqual(-1, engine.TestOnly_ReleasedHandKeeper);
            engine.RunTick();
            Assert.AreEqual(RestartCue.None, engine.RestartAppliedThisTick);
        }

        [TestCase(0)]
        [TestCase(1)]
        public void BallAlreadyOutOfPlay_GoalTakesPrecedenceOverRehandling(int team)
        {
            MatchEngine engine = Released(team);
            Vector3 goal = new Vector3(team == 0 ? -1f : 106f, 34f, 1f);
            engine.TestOnly_ForceBallLoose(goal, Vector3.zero);
            Assert.IsFalse(engine.TestOnly_TryKeeperHandContact(team * 11, goal));
            engine.RunTick();
            Assert.AreEqual(1, engine.HomeScore + engine.AwayScore);
            Assert.AreEqual(RestartCue.KickOff, engine.RestartAppliedThisTick);
        }

        private static MatchEngine Released(int team)
        {
            MatchEngine engine = W8BDistributionTests.Fixture(GkDistributionPolicy.Quick, team, false);
            for (int i = 0; i < 100 && engine.TestOnly_ReleasedHandKeeper < 0; i++) engine.RunTick();
            Assert.AreEqual(team * 11, engine.TestOnly_ReleasedHandKeeper, "Fixture must execute a real #5 release.");
            return engine;
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
// | 1.0 | 2026-10-16 | — | Own-release handling, actual body touch, smother rejection, IFK and restore. |
#endregion
