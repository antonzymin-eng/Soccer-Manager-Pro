// File:     src/match-engine/tests/MatchEnginePhysicsTests.cs
// Created:  2026-06-16
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5 Phase B (B2), Code Standards #20
// Purpose:  Phase B step B2 tests — proves the Physics phase drives the real Ball Physics (#1) and
//           Agent Movement (#2) seams: the ball integrates under gravity, an outfielder walks toward
//           its command target while goalkeepers are skipped, and determinism holds with live dynamics.

using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Phase B step B2 physics-wiring tests for <see cref="MatchEngine"/>.
    /// </summary>
    [TestFixture]
    public sealed class MatchEnginePhysicsTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;
        private const int   TickCount = 120; // two seconds at 60 Hz

        // Roster layout (MatchEngine.InitializeKickoffState): index 0 of each team is the goalkeeper.
        private const int GoalkeeperIndex = 0;
        private const int OutfieldIndex   = 1;

        private static Vector2 CentreSpot()
        {
            return new Vector2(
                MatchEngineConstants.KickoffBallXM,
                MatchEngineConstants.KickoffBallYM);
        }

        [Test]
        public void Ball_DroppedFromHeight_FallsThroughPhysicsSeam()
        {
            var engine = new MatchEngine(MatchSeed);

            // Drop an airborne ball over the centre spot; a Stationary ball would be a no-op.
            const float dropHeight = 5.0f;
            BallState dropped = BallState.CreateAtPosition(new Vector3(
                MatchEngineConstants.KickoffBallXM,
                MatchEngineConstants.KickoffBallYM,
                dropHeight));
            dropped.State = BallStateType.Airborne;
            engine.TestOnly_SetBall(dropped);

            float startZ = engine.TestOnly_BallSnapshot.Position.z;

            for (int i = 0; i < 60; i++) // one second
            {
                engine.RunTick();
            }

            BallState after = engine.TestOnly_BallSnapshot;

            Assert.Less(after.Position.z, startZ,
                "The ball must fall — RunPhysicsPhase did not drive BallPhysicsCore.UpdateBallPhysics.");
            Assert.GreaterOrEqual(after.Position.z, 0.0f,
                "The ball must not fall through the ground.");

            // A spinless vertical drop has no horizontal velocity or Magnus force, so it stays over
            // the drop point — proving the seam integrated only what physics dictates.
            Assert.AreEqual(MatchEngineConstants.KickoffBallXM, after.Position.x, 0.01f,
                "Vertical drop must not drift in X.");
            Assert.AreEqual(MatchEngineConstants.KickoffBallYM, after.Position.y, 0.01f,
                "Vertical drop must not drift in Y.");
        }

        [Test]
        public void OutfieldAgent_MovesTowardTarget_GoalkeeperSkipped()
        {
            var engine = new MatchEngine(MatchSeed);

            Assert.IsTrue(engine.TestOnly_IsGoalkeeper(GoalkeeperIndex),
                "Roster index 0 must be the goalkeeper.");
            Assert.IsFalse(engine.TestOnly_IsGoalkeeper(OutfieldIndex),
                "Roster index 1 must be an outfielder.");

            Vector2 target = CentreSpot();
            Vector2 outfieldStart = engine.TestOnly_AgentSnapshot(OutfieldIndex).Position;
            Vector2 gkStart       = engine.TestOnly_AgentSnapshot(GoalkeeperIndex).Position;

            // Command BOTH toward the centre. The goalkeeper MUST still be skipped by UpdateAllAgents.
            engine.TestOnly_SetCommand(OutfieldIndex, MovementCommand.WalkTo(target));
            engine.TestOnly_SetCommand(GoalkeeperIndex, MovementCommand.WalkTo(target));

            for (int i = 0; i < TickCount; i++)
            {
                engine.RunTick();
            }

            Vector2 outfieldEnd = engine.TestOnly_AgentSnapshot(OutfieldIndex).Position;
            Vector2 gkEnd       = engine.TestOnly_AgentSnapshot(GoalkeeperIndex).Position;

            Assert.Less(
                Vector2.Distance(outfieldEnd, target),
                Vector2.Distance(outfieldStart, target),
                "The outfielder must move toward its WalkTo target (movement seam not driven).");

            // The goalkeeper's state is left completely untouched, so position is byte-exact.
            Assert.AreEqual(gkStart.x, gkEnd.x,
                "Goalkeeper X must not change — UpdateAllAgents skips goalkeepers at Stage 0.");
            Assert.AreEqual(gkStart.y, gkEnd.y,
                "Goalkeeper Y must not change — UpdateAllAgents skips goalkeepers at Stage 0.");
        }

        [Test]
        public void TwoSameSeedRuns_WithLiveDynamics_ProduceIdenticalDigestChains()
        {
            List<byte[]> chainA = RunWithDynamics();
            List<byte[]> chainB = RunWithDynamics();

            for (int i = 0; i < TickCount; i++)
            {
                CollectionAssert.AreEqual(
                    chainA[i], chainB[i],
                    $"Digest chain diverged at tick {i + 1} with live ball + agent dynamics.");
            }
        }

        // Runs a fixed scenario with real dynamics (a dropped ball + a walking outfielder) and
        // captures the per-tick snapshot digest chain. Injections are identical across calls, so two
        // same-seed runs MUST agree tick-for-tick.
        private static List<byte[]> RunWithDynamics()
        {
            var engine = new MatchEngine(MatchSeed);

            BallState dropped = BallState.CreateAtPosition(new Vector3(
                MatchEngineConstants.KickoffBallXM,
                MatchEngineConstants.KickoffBallYM,
                5.0f));
            dropped.State = BallStateType.Airborne;
            engine.TestOnly_SetBall(dropped);
            engine.TestOnly_SetCommand(OutfieldIndex, MovementCommand.WalkTo(CentreSpot()));

            var chain = new List<byte[]>(TickCount);
            for (int i = 0; i < TickCount; i++)
            {
                engine.RunTick();
                chain.Add(engine.CurrentSnapshotDigest);
            }
            return chain;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                   |
// | 1.0     | 2026-06-16 | —      | Initial Phase B step B2 physics-wiring tests: ball drop |
// |         |            |        | integration, outfield walk + goalkeeper-skip, and       |
// |         |            |        | same-seed determinism with live dynamics.               |
#endregion
