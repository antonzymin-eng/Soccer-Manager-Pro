// File:     src/positioning-ai/Tests/RestDefenseEvaluatorTests.cs
// Created:  2026-07-07
// Modified: 2026-07-07
// Author:   —
// Spec:     Positioning AI #12 §3.5, new §7.13 (cheap-item addition), Code Standards #20
// Purpose:  Locks RestDefenseEvaluator.Evaluate: phase gate (only IN_POSSESSION applies), the
//           REST_DEFENSE_MIN_COUNT threshold, GK exclusion, and inactive-agent exclusion.

using NUnit.Framework;
using UnityEngine;

namespace TacticalDirector.PositioningAI.Tests
{
    [TestFixture]
    internal class RestDefenseEvaluatorTests
    {
        private static PositioningPerceptionSnapshot BuildSnapshot(float[] outfieldX, bool[] active = null)
        {
            var snap = new PositioningPerceptionSnapshot(11);
            snap.Agents[0] = new AgentPositioningData(
                entityId: 0, slotIndex: 0, position: new Vector2(5f, 34f),
                isActive: true, role: RoleId.GK, isGoalkeeper: true);

            for (int i = 0; i < outfieldX.Length; i++)
            {
                bool isActive = active == null || active[i];
                snap.Agents[i + 1] = new AgentPositioningData(
                    entityId: i + 1, slotIndex: i + 1, position: new Vector2(outfieldX[i], 34f),
                    isActive: isActive, role: RoleId.CB, isGoalkeeper: false);
            }
            return snap;
        }

        [Test]
        public void Evaluate_NotInPossession_AlwaysSufficient()
        {
            // All 10 outfield agents far upfield — would be insufficient if IN_POSSESSION.
            var snap = BuildSnapshot(new float[] { 90, 91, 92, 93, 94, 95, 96, 97, 98, 99 });

            Assert.IsTrue(RestDefenseEvaluator.Evaluate(snap, Phase.OutOfPoss));
            Assert.IsTrue(RestDefenseEvaluator.Evaluate(snap, Phase.TransToAtk));
            Assert.IsTrue(RestDefenseEvaluator.Evaluate(snap, Phase.TransToDef));
        }

        [Test]
        public void Evaluate_InPossession_ThreeAtOrBehindLine_IsSufficient()
        {
            // Exactly REST_DEFENSE_MIN_COUNT (3) agents at/behind the 40%-of-105m = 42m line.
            var snap = BuildSnapshot(new float[] { 40, 41, 42, 50, 60, 70, 80, 85, 90, 95 });

            Assert.IsTrue(RestDefenseEvaluator.Evaluate(snap, Phase.InPoss));
        }

        [Test]
        public void Evaluate_InPossession_OnlyTwoBehindLine_IsInsufficient()
        {
            var snap = BuildSnapshot(new float[] { 40, 41, 50, 60, 70, 80, 85, 90, 95, 100 });

            Assert.IsFalse(RestDefenseEvaluator.Evaluate(snap, Phase.InPoss));
        }

        [Test]
        public void Evaluate_GoalkeeperExcludedFromCount()
        {
            // GK sits at x=5 (deep) but only 2 outfield agents are behind the line — GK must not
            // count toward REST_DEFENSE_MIN_COUNT.
            var snap = BuildSnapshot(new float[] { 40, 41, 50, 60, 70, 80, 85, 90, 95, 100 });

            Assert.IsFalse(RestDefenseEvaluator.Evaluate(snap, Phase.InPoss));
        }

        [Test]
        public void Evaluate_InactiveAgentsExcludedFromCount()
        {
            // 3 agents behind the line, but one is inactive (substituted) — only 2 count.
            var snap = BuildSnapshot(
                new float[] { 40, 41, 42, 50, 60, 70, 80, 85, 90, 95 },
                new bool[]  { true, true, false, true, true, true, true, true, true, true });

            Assert.IsFalse(RestDefenseEvaluator.Evaluate(snap, Phase.InPoss));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-07-07 | —      | Initial implementation (cheap-item addition). |
#endregion
