// File:     src/match-engine/tests/KeeperPerceptionGateTests.cs
// Created:  2026-09-11
// Modified: 2026-09-11 (W4 review: sent-off/non-participating bodies cannot screen the keeper)
// Author:   —
// Spec:     Match-engine wiring backlog W4
// Purpose:  Pure regression locks for the unified keeper save-perception predicate.

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class KeeperPerceptionGateTests
    {
        private static AgentState[] AgentsWithKeeperAt(Vector2 keeperPosition)
        {
            AgentState[] agents = new AgentState[22];
            for (int i = 0; i < agents.Length; i++)
            {
                agents[i].Position = new Vector2(100.0f + i, 100.0f);
            }

            agents[0].Position = keeperPosition;
            return agents;
        }

        [Test]
        public void W4_ClearGoalBoundShot_IsAvailable()
        {
            Vector2 keeper = new Vector2(0.0f, 34.0f);
            AgentState[] agents = AgentsWithKeeperAt(keeper);
            Vector3 ball = new Vector3(5.0f, 34.0f, 0.11f);
            Vector3 velocity = new Vector3(-10.0f, 0.0f, 0.0f);

            bool available = KeeperPerceptionGate.SaveAvailable(
                keeperTeam: 0,
                keeperAgentId: 0,
                keeperPosition: in keeper,
                ballPosition: in ball,
                ballVelocity: in velocity,
                ballLoose: true,
                agents: agents);

            Assert.IsTrue(available,
                "W4: an unobstructed shot that satisfies SaveArmed must remain save-available");
        }

        [Test]
        public void W4_FriendlyScreen_DisarmsSaveAvailability()
        {
            Vector2 keeper = new Vector2(0.0f, 34.0f);
            AgentState[] agents = AgentsWithKeeperAt(keeper);
            agents[1].Position = new Vector2(2.5f, 34.0f); // same-team identity is intentionally irrelevant
            Vector3 ball = new Vector3(5.0f, 34.0f, 0.11f);
            Vector3 velocity = new Vector3(-10.0f, 0.0f, 0.0f);

            bool available = KeeperPerceptionGate.SaveAvailable(
                keeperTeam: 0,
                keeperAgentId: 0,
                keeperPosition: in keeper,
                ballPosition: in ball,
                ballVelocity: in velocity,
                ballLoose: true,
                agents: agents);

            Assert.IsFalse(available,
                "W4: any participating body between keeper and ball must make the keeper unsighted");
        }

        [Test]
        public void W4_ExcludedBody_DoesNotDisarmSaveAvailability()
        {
            Vector2 keeper = new Vector2(0.0f, 34.0f);
            AgentState[] agents = AgentsWithKeeperAt(keeper);
            agents[1].Position = new Vector2(2.5f, 34.0f);
            bool[] excludedAgents = new bool[agents.Length];
            excludedAgents[1] = true;
            Vector3 ball = new Vector3(5.0f, 34.0f, 0.11f);
            Vector3 velocity = new Vector3(-10.0f, 0.0f, 0.0f);

            bool available = KeeperPerceptionGate.SaveAvailable(
                keeperTeam: 0,
                keeperAgentId: 0,
                keeperPosition: in keeper,
                ballPosition: in ball,
                ballVelocity: in velocity,
                ballLoose: true,
                agents: agents,
                excludedAgents: excludedAgents);

            Assert.IsTrue(available,
                "W4: a sent-off/non-participating player's retained position must not screen the keeper");
        }

        [Test]
        public void W4_OffLineBody_DoesNotDisarmSaveAvailability()
        {
            Vector2 keeper = new Vector2(0.0f, 34.0f);
            AgentState[] agents = AgentsWithKeeperAt(keeper);
            agents[1].Position = new Vector2(2.5f, 40.0f);
            Vector3 ball = new Vector3(5.0f, 34.0f, 0.11f);
            Vector3 velocity = new Vector3(-10.0f, 0.0f, 0.0f);

            bool available = KeeperPerceptionGate.SaveAvailable(
                keeperTeam: 0,
                keeperAgentId: 0,
                keeperPosition: in keeper,
                ballPosition: in ball,
                ballVelocity: in velocity,
                ballLoose: true,
                agents: agents);

            Assert.IsTrue(available,
                "W4: a body outside the shadow cone must not blind the keeper");
        }

        [Test]
        public void W4_OcclusionCannotArmOtherwiseInvalidFlight()
        {
            Vector2 keeper = new Vector2(0.0f, 34.0f);
            AgentState[] agents = AgentsWithKeeperAt(keeper);
            Vector3 ball = new Vector3(5.0f, 34.0f, 0.11f);
            Vector3 velocity = new Vector3(+10.0f, 0.0f, 0.0f); // away from team-0 goal

            bool available = KeeperPerceptionGate.SaveAvailable(
                keeperTeam: 0,
                keeperAgentId: 0,
                keeperPosition: in keeper,
                ballPosition: in ball,
                ballVelocity: in velocity,
                ballLoose: true,
                agents: agents);

            Assert.IsFalse(available,
                "W4: perception is an additional gate; it must not replace SaveArmed flight geometry");
        }
    }
}
