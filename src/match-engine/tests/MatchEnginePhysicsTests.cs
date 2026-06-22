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

        // AI-driven collision-free window (ticks). The nearest non-GK to a goalkeeper at kickoff is the
        // same-team adjacent agent at ~5.67 m (PITCH_WIDTH/12 lateral gap on the shared line). Even at an
        // unreachable constant 10 m/s straight at the GK, 30 ticks (0.5 s) covers ≤ 5 m < 5.67 m, so no
        // agent can close to collision range — and the AI in fact pulls agents toward the central ball
        // (away from the spread GKs). This makes the byte-exact GK assertion provably sound (collision,
        // which DOES process GKs in Resolve, cannot fire). 30 ticks = 5 stride ticks of the AI chain.
        private const int AiCollisionFreeTicks = 30;

        [Test]
        public void AiPhase_DrivesChain_GoalkeepersSkipped()
        {
            // Phase D D1: the AI phase now OWNS the held movement commands (the DecisionTree dispatches
            // them each 10 Hz stride tick), superseding the B2 TestOnly_SetCommand injection. Locks three
            // robust invariants: (1) the full perception → decision → dispatch chain executes every stride
            // tick without throwing (RunTick completing is the proof); (2) the chain actually produces a
            // decision — at least one outfielder's DecisionTree dispatches (not a silent abort at
            // SnapshotValidator); (3) goalkeepers stay byte-exact over the collision-free window
            // (UpdateAllAgents skips them; collision can't reach them — see AiCollisionFreeTicks).
            // A specific "outfielder MOVED" assertion is deliberately NOT made — at kickoff the loose ball
            // sits ~26 m away so the DT may hold every agent at its formation slot; real off-ball motion
            // arrives with Positioning AI slots at D2. AI determinism is covered by
            // TwoSameSeedRuns_WithLiveDynamics (the AI runs during those 120 ticks).
            var engine = new MatchEngine(MatchSeed);

            Assert.IsTrue(engine.TestOnly_IsGoalkeeper(GoalkeeperIndex),
                "Roster index 0 must be the home goalkeeper.");
            Assert.IsTrue(engine.TestOnly_IsGoalkeeper(AwayGoalkeeperIndex),
                "Roster index 11 must be the away goalkeeper.");

            Vector2 homeGkStart = engine.TestOnly_AgentSnapshot(GoalkeeperIndex).Position;
            Vector2 awayGkStart = engine.TestOnly_AgentSnapshot(AwayGoalkeeperIndex).Position;

            for (int i = 0; i < AiCollisionFreeTicks; i++)
            {
                engine.RunTick();
            }

            // The AI chain ran on every stride tick (and never threw — otherwise the loop above aborts).
            Assert.AreEqual((ulong)(AiCollisionFreeTicks / DeterministicSimConstants.AI_PHASE_STRIDE),
                engine.AiPhaseRunCount,
                "The AI phase must have run once per stride tick across the run.");

            // The chain produced a real decision: at least one outfielder dispatched an action. (On the
            // first heartbeat every valid agent transitions IDLE → EVALUATING → dispatch, so this holds
            // unless the pipeline silently aborts at the validation gate.)
            bool anyDispatched = false;
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (engine.TestOnly_IsGoalkeeper(i)) continue;
                if (engine.TestOnly_DtHasDispatched(i))
                {
                    anyDispatched = true;
                    break;
                }
            }
            Assert.IsTrue(anyDispatched,
                "No DecisionTree dispatched — the AI pipeline aborted before producing a decision.");

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
// | 1.2     | 2026-06-22 | —      | Phase D D1 AR (M-1 + L-1): run capped to AiCollision-    |
// |         |            |        | FreeTicks (30 = 5 stride ticks) so collision (which      |
// |         |            |        | processes GKs in Resolve) provably cannot reach a GK,    |
// |         |            |        | making byte-exact sound; added a "≥1 DecisionTree        |
// |         |            |        | dispatched" assertion (TestOnly_DtHasDispatched) so the  |
// |         |            |        | chain can't silently abort at SnapshotValidator.        |
#endregion
