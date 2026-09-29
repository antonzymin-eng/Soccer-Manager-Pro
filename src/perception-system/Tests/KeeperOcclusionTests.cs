// File:     src/perception-system/Tests/KeeperOcclusionTests.cs
// Created:  2026-09-11
// Modified: 2026-09-11 (W4 review: header compliance and exclusion-mask fail-loud coverage)
// Author:   —
// Spec:     Perception System #7 §3.2; Match-engine wiring backlog W4
// Purpose:  Regression coverage for goalkeeper-specific all-body line of sight.

using System;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;

namespace TacticalDirector.PerceptionSystem.Tests
{
    [TestFixture]
    internal sealed class KeeperOcclusionTests
    {
        private static AgentState[] MakeSeparatedAgents()
        {
            AgentState[] states = new AgentState[22];
            for (int i = 0; i < states.Length; i++)
            {
                // Keep unused bodies farther from the observer than the 10 m ball target.
                states[i].Position = new Vector2(100.0f + i, 100.0f);
            }

            states[0].Position = Vector2.zero;
            return states;
        }

        private static PerceptionAgentAttributes[] MakeTeamZeroAttributes()
        {
            PerceptionAgentAttributes[] attrs = new PerceptionAgentAttributes[22];
            for (int i = 0; i < attrs.Length; i++)
            {
                attrs[i] = PerceptionAgentAttributes.CreateDefault();
                attrs[i].TeamId = 0;
            }

            return attrs;
        }

        [Test]
        public void W4_FriendlyBodyBetweenKeeperAndBall_BlocksKeeperLineOfSight()
        {
            AgentState[] states = MakeSeparatedAgents();
            states[1].Position = new Vector2(5.0f, 0.0f); // own defender directly in front of keeper

            bool occluded = OcclusionFilter.IsOccludedByAnyAgent(
                observerPos: states[0].Position,
                targetPos: new Vector2(10.0f, 0.0f),
                observerId: 0,
                agentStates: states);

            Assert.IsTrue(occluded,
                "W4: a same-team body between keeper and ball must unsight the keeper");
        }

        [Test]
        public void W4_FriendlyBody_DoesNotChangeOrdinaryStageZeroOcclusion()
        {
            AgentState[] states = MakeSeparatedAgents();
            PerceptionAgentAttributes[] attrs = MakeTeamZeroAttributes();
            states[1].Position = new Vector2(5.0f, 0.0f);
            int[] candidates = { 1 };

            bool occluded = OcclusionFilter.IsOccluded(
                observerPos: states[0].Position,
                targetPos: new Vector2(10.0f, 0.0f),
                targetId: -1,
                agentStates: states,
                candidateIds: candidates,
                candidateCount: candidates.Length,
                observerTeamId: 0,
                agentAttrs: attrs);

            Assert.IsFalse(occluded,
                "W4 must not globally change OQ-1: ordinary Stage-0 perception still ignores teammates");
        }

        [Test]
        public void W4_KeeperBody_DoesNotSelfOcclude()
        {
            AgentState[] states = MakeSeparatedAgents();

            bool occluded = OcclusionFilter.IsOccludedByAnyAgent(
                observerPos: states[0].Position,
                targetPos: new Vector2(10.0f, 0.0f),
                observerId: 0,
                agentStates: states);

            Assert.IsFalse(occluded,
                "W4: the observer must be excluded from its own all-body shadow test");
        }

        [Test]
        public void W4_ShortExclusionMask_FailsLoud()
        {
            AgentState[] states = MakeSeparatedAgents();
            bool[] excluded = new bool[states.Length - 1];

            Assert.Throws<ArgumentException>(() =>
                OcclusionFilter.IsOccludedByAnyAgent(
                    observerPos: states[0].Position,
                    targetPos: new Vector2(10.0f, 0.0f),
                    observerId: 0,
                    agentStates: states,
                    excludedAgents: excluded));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                      |
// | 1.0     | 2026-09-11 | —      | W4: keeper all-body LOS, Stage-0 isolation and mask guard. |
#endregion
