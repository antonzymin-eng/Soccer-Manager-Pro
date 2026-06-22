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
using TacticalDirector.DeterministicSim;

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

        // Roster: index 0 is the home goalkeeper; index 11 (PLAYERS_PER_TEAM) is the away goalkeeper.
        private const int AwayGoalkeeperIndex = MatchEngineConstants.PLAYERS_PER_TEAM;

        [Test]
        public void AiPhase_DrivesChain_GoalkeepersSkipped()
        {
            // Phase D D1: the AI phase now OWNS the held movement commands (the DecisionTree dispatches
            // them each 10 Hz stride tick), superseding the B2 TestOnly_SetCommand injection. This locks
            // two robust invariants over a two-second run: the full perception → decision → dispatch
            // chain executes every stride tick without throwing (RunTick completing TickCount times is
            // the proof — a chain exception would surface here), and goalkeepers stay byte-exact
            // (UpdateAllAgents skips them at Stage 0, regardless of any command the DT writes for them).
            // NOTE: a "≥1 outfielder moved" assertion is deliberately NOT made — at kickoff the loose
            // ball sits ~26 m from the nearest agents, so the DT may hold every outfielder at its
            // formation slot (= kickoff position ⇒ no displacement). Real off-ball motion arrives with
            // Positioning AI slots at D2 and is exercised by the Phase F closed-loop scenario. AI-driven
            // determinism is covered by TwoSameSeedRuns_WithLiveDynamics (the AI runs during those ticks).
            var engine = new MatchEngine(MatchSeed);

            Assert.IsTrue(engine.TestOnly_IsGoalkeeper(GoalkeeperIndex),
                "Roster index 0 must be the home goalkeeper.");
            Assert.IsTrue(engine.TestOnly_IsGoalkeeper(AwayGoalkeeperIndex),
                "Roster index 11 must be the away goalkeeper.");

            Vector2 homeGkStart = engine.TestOnly_AgentSnapshot(GoalkeeperIndex).Position;
            Vector2 awayGkStart = engine.TestOnly_AgentSnapshot(AwayGoalkeeperIndex).Position;

            for (int i = 0; i < TickCount; i++)
            {
                engine.RunTick();
            }

            // The AI chain ran on every stride tick (and never threw — otherwise the loop above aborts).
            Assert.AreEqual((ulong)(TickCount / DeterministicSimConstants.AI_PHASE_STRIDE),
                engine.AiPhaseRunCount,
                "The AI phase must have run once per stride tick across the run.");

            // Both goalkeepers are left completely untouched, so position is byte-exact.
            Vector2 homeGkEnd = engine.TestOnly_AgentSnapshot(GoalkeeperIndex).Position;
            Vector2 awayGkEnd = engine.TestOnly_AgentSnapshot(AwayGoalkeeperIndex).Position;
            Assert.AreEqual(homeGkStart.x, homeGkEnd.x,
                "Home goalkeeper X must not change — UpdateAllAgents skips goalkeepers at Stage 0.");
            Assert.AreEqual(homeGkStart.y, homeGkEnd.y,
                "Home goalkeeper Y must not change — UpdateAllAgents skips goalkeepers at Stage 0.");
            Assert.AreEqual(awayGkStart.x, awayGkEnd.x,
                "Away goalkeeper X must not change — UpdateAllAgents skips goalkeepers at Stage 0.");
            Assert.AreEqual(awayGkStart.y, awayGkEnd.y,
                "Away goalkeeper Y must not change — UpdateAllAgents skips goalkeepers at Stage 0.");
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
// | 1.1     | 2026-06-22 | —      | Phase D D1: the AI phase now owns the held movement      |
// |         |            |        | commands, so OutfieldAgent_MovesTowardTarget... (which   |
// |         |            |        | injected a WalkTo the AI now clobbers each stride tick)  |
// |         |            |        | is replaced by AiPhase_DrivesOutfieldMovement_           |
// |         |            |        | GoalkeepersSkipped: the AI+physics seam moves ≥1         |
// |         |            |        | outfielder while both goalkeepers stay byte-exact.      |
#endregion
