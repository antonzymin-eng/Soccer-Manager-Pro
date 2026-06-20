// File:     src/match-engine/tests/MatchEngineResolveTests.cs
// Created:  2026-06-19
// Modified: 2026-06-19
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5 Phase C (C1/C1a/C2/C3), Code Standards #20
// Purpose:  Phase C Resolve-phase wiring tests — collision (#3) runs in Resolve and separates an
//           overlapping pair deterministically, and a scripted pass/shot initiates through the host
//           executor adapters and advances under the executor lifecycle. Stays below the CONTACT
//           publish boundary (registry boot + possession-flip completion land at C4).

using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.PassMechanics;
using TacticalDirector.ShotMechanics;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Phase C step C1/C1a/C2/C3 Resolve-wiring tests for <see cref="MatchEngine"/>.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineResolveTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

        // Two outfielders on the home team (index 0 is the goalkeeper), placed mid-pitch so the only
        // collision in play is between them — away from the kickoff lines (x = 26.25 / 78.75).
        private const int AgentA = 1;
        private const int AgentB = 2;

        private static void OverlapTwoAgents(MatchEngine engine)
        {
            var posA = new Vector2(40.0f, 34.0f);
            var posB = new Vector2(40.1f, 34.0f); // 0.1 m apart — well inside the combined hitbox radius

            engine.TestOnly_SetAgent(AgentA, AgentState.CreateAtPosition(posA, new Vector2(1f, 0f)));
            engine.TestOnly_SetAgent(AgentB, AgentState.CreateAtPosition(posB, new Vector2(1f, 0f)));
            // Hold each at its NEW position so the Physics phase does not walk them back to their boot slot.
            engine.TestOnly_SetCommand(AgentA, MovementCommand.Stop(posA));
            engine.TestOnly_SetCommand(AgentB, MovementCommand.Stop(posB));
        }

        [Test]
        public void Collision_SeparatesOverlappingAgents_InResolvePhase()
        {
            var engine = new MatchEngine(MatchSeed);
            OverlapTwoAgents(engine);

            float distBefore = Vector2.Distance(
                engine.TestOnly_AgentSnapshot(AgentA).Position,
                engine.TestOnly_AgentSnapshot(AgentB).Position);

            engine.RunTick();

            float distAfter = Vector2.Distance(
                engine.TestOnly_AgentSnapshot(AgentA).Position,
                engine.TestOnly_AgentSnapshot(AgentB).Position);

            Assert.Greater(distAfter, distBefore,
                "CollisionSystem.UpdateCollisions must run in the Resolve phase and separate the " +
                "overlapping pair (penetration position correction).");
        }

        [Test]
        public void TwoSameSeedRuns_WithCollision_ProduceIdenticalDigestChains()
        {
            const int tickCount = 120;

            List<byte[]> chainA = RunOverlapScenario(tickCount);
            List<byte[]> chainB = RunOverlapScenario(tickCount);

            for (int i = 0; i < tickCount; i++)
            {
                CollectionAssert.AreEqual(
                    chainA[i], chainB[i],
                    $"Digest chain diverged at tick {i + 1} with a live collision in the Resolve phase.");
            }
        }

        private static List<byte[]> RunOverlapScenario(int tickCount)
        {
            var engine = new MatchEngine(MatchSeed);
            OverlapTwoAgents(engine);

            var chain = new List<byte[]>(tickCount);
            for (int i = 0; i < tickCount; i++)
            {
                engine.RunTick();
                chain.Add(engine.CurrentSnapshotDigest);
            }
            return chain;
        }

        [Test]
        public void ScriptedPass_InitiatesThroughExecutorAdapters_AndAdvances()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetPossession(AgentA);

            var request = new PassRequest
            {
                AgentId          = AgentA,
                PassType         = PassType.Ground,
                CrossSubType     = CrossSubType.Flat,
                TargetAgentId    = AgentB,
                TargetPosition   = Vector3.zero,
                IntendedDistance = 10f,
                Urgency          = 0.5f,
                IsWeakFoot       = false,
                TeamId           = 0,
                FrameNumber      = 1
            };

            // Execute resolves through the PassWorldAdapter (possession gate + attribute/state build +
            // kick-speed computation). Initiated == the adapters all returned usable data.
            PassResult result = engine.TestOnly_InitiatePass(AgentA, in request);
            Assert.AreEqual(PassOutcome.Initiated, result.Outcome, "Scripted pass must initiate via the adapter.");
            Assert.IsFalse(engine.TestOnly_PassExecutorIdle(AgentA), "Executor must be mid-windup after Execute.");
            Assert.AreEqual(AgentA, engine.TestOnly_PossessingAgentId, "Possession is retained until CONTACT (not reached here).");

            // One Resolve tick advances the windup. A single tick can at most reach Contact (non-idle);
            // ExecuteContact (the publish path) cannot run until the NEXT Update, so no EventBus boot is
            // needed and possession is still retained.
            engine.RunTick();
            Assert.IsFalse(engine.TestOnly_PassExecutorIdle(AgentA), "Executor lifecycle must keep advancing the pass.");
            Assert.AreEqual(AgentA, engine.TestOnly_PossessingAgentId, "No kick yet — possession unchanged.");
        }

        [Test]
        public void ScriptedShot_InitiatesThroughExecutorAdapters_AndAdvances()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetPossession(AgentA);

            var request = new ShotRequest
            {
                AgentId         = AgentA,
                PowerIntent     = 0.7f,
                ContactZone     = ContactZone.Centre,
                SpinIntent      = 0.2f,
                PlacementTarget = new Vector2(0.5f, 0.5f),
                IsWeakFoot      = false,
                DistanceToGoal  = 20f,
                TeamId          = 0,
                FrameNumber     = 1
            };

            ShotResult result = engine.TestOnly_InitiateShot(AgentA, in request);
            Assert.AreEqual(ShotOutcome.Initiated, result.Outcome, "Scripted shot must initiate via the adapter.");
            Assert.IsFalse(engine.TestOnly_ShotExecutorIdle(AgentA), "Executor must be mid-windup after Execute.");

            engine.RunTick();
            Assert.IsFalse(engine.TestOnly_ShotExecutorIdle(AgentA), "Executor lifecycle must keep advancing the shot.");
            Assert.AreEqual(AgentA, engine.TestOnly_PossessingAgentId, "No kick yet — possession unchanged.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-19 | —      | Initial Phase C C1/C1a/C2/C3 Resolve-wiring tests: collision   |
// |         |            |        | separation in Resolve, same-seed determinism with a live       |
// |         |            |        | collision, and scripted pass/shot initiation + advance through |
// |         |            |        | the host executor adapters (kept below the CONTACT boundary).  |
#endregion
