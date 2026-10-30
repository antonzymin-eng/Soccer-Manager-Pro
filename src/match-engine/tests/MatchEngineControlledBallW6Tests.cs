// File:     src/match-engine/tests/MatchEngineControlledBallW6Tests.cs
// Created:  2026-09-14
// Modified: 2026-09-22 (W6 ordering correction surfaced by W3: keeper + outfielder Resolve-collision attachment, plus mirrored keeper goal-plane correction)
// Modified: 2026-09-15 (W6 review closure — direct lock for the Controlled keeper own-goal-plane invariant)
// Modified: 2026-09-14
// Author:   —
// Spec:     Match-engine wiring backlog W6; Ball Physics #1 §3.1.11; Code Standards #20
// Purpose:  Composed W6 locks: genuine possession enters BallState.Controlled and follows the holder
//           across Physics and Resolve collision correction, restart-taker designation remains a placed
//           Stationary ball, keeper control cannot carry the attached ball through either defended goal
//           plane, and non-kick release exits physical control without introducing new cross-tick state.

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineControlledBallW6Tests
    {
        private const ulong MatchSeed = 0x5736434F4E54524FUL;
        private const int Outfielder = 1;

        [Test]
        public void LoosePickup_ProducesControlledBall_AndAnchorsToHolder()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetPossession(MatchEngineConstants.NO_POSSESSION);

            var spot = new Vector2(52.5f, 20f);
            engine.TestOnly_SetAgent(
                Outfielder, AgentState.CreateAtPosition(spot, Vector2.right));
            engine.TestOnly_SetCommand(Outfielder, MovementCommand.Stop(spot));
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                spot.x + 0.4f, spot.y, MatchEngineConstants.BALL_REST_HEIGHT_M)));

            engine.RunTick();

            Assert.AreEqual(Outfielder, engine.TestOnly_PossessingAgentId);
            Assert.AreEqual(BallStateType.Controlled, engine.BallView.State,
                "W6: a real loose-ball pickup must enter Ball Physics' Controlled state.");
            AssertBallXYAtHolder(engine, Outfielder);
            Assert.AreEqual(MatchEngineConstants.BALL_REST_HEIGHT_M, engine.BallView.Position.z, 1e-6f);
            Assert.AreEqual(Vector3.zero, engine.BallView.Velocity);
        }

        [Test]
        public void ControlledBall_FollowsOutfielderAcrossPhysics()
        {
            var engine = new MatchEngine(MatchSeed ^ 0x10UL);
            var initial = new Vector2(40f, 25f);
            engine.TestOnly_SetAgent(
                Outfielder, AgentState.CreateAtPosition(initial, Vector2.right));
            engine.TestOnly_SetCommand(Outfielder, MovementCommand.Stop(initial));
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                initial.x + 0.25f, initial.y, MatchEngineConstants.BALL_REST_HEIGHT_M)));
            engine.TestOnly_SetPossession(Outfielder);

            var moved = new Vector2(44f, 29f);
            engine.TestOnly_SetAgent(
                Outfielder, AgentState.CreateAtPosition(moved, Vector2.right));
            engine.TestOnly_SetCommand(Outfielder, MovementCommand.Stop(moved));

            engine.RunTick();

            Assert.AreEqual(BallStateType.Controlled, engine.BallView.State);
            AssertBallXYAtHolder(engine, Outfielder);
            Assert.AreEqual(MatchEngineConstants.BALL_REST_HEIGHT_M, engine.BallView.Position.z, 1e-6f,
                "Outfield control is the Ball Physics §3.1.11 foot-position contract.");
        }

        [Test]
        public void ControlledKeeper_ReattachesAfterResolveCollisionCorrection()
        {
            var engine = new MatchEngine(MatchSeed ^ 0x18UL);
            int keeper = FindKeeper(engine, team: 0);
            Assert.GreaterOrEqual(keeper, 0);

            // Static overlap is deliberate: Collision #3 resolves penetration even without impact
            // velocity, so this isolates its Resolve-phase position correction from locomotion.
            var keeperPos = new Vector2(40f, 25f);
            var teammatePos = new Vector2(40.20f, 25f);
            engine.TestOnly_SetAgent(
                keeper, AgentState.CreateAtPosition(keeperPos, Vector2.right));
            engine.TestOnly_SetAgent(
                Outfielder, AgentState.CreateAtPosition(teammatePos, Vector2.right));

            const float claimHeight = 1.6f;
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                keeperPos.x, keeperPos.y, claimHeight)));
            engine.TestOnly_SetPossession(keeper);

            Vector2 before = engine.AgentView(keeper).Position;
            engine.TestOnly_RunResolvePhase();
            Vector2 after = engine.AgentView(keeper).Position;

            Assert.Greater((after - before).sqrMagnitude, 1e-8f,
                "Precondition: Resolve collision response must actually position-correct the holder.");
            Assert.AreEqual(keeper, engine.TestOnly_PossessingAgentId,
                "Agent-agent separation must not itself release keeper possession.");
            Assert.AreEqual(BallStateType.Controlled, engine.BallView.State);
            AssertBallXYAtHolder(engine, keeper);
            Assert.AreEqual(claimHeight, engine.BallView.Position.z, 1e-6f,
                "Post-collision attachment reconciliation must preserve keeper claim height.");
        }

        [Test]
        public void ControlledOutfielder_ReattachesAfterResolveCollisionCorrection()
        {
            var engine = new MatchEngine(MatchSeed ^ 0x19UL);
            int partner = FindOtherOutfielder(engine, engine.AgentTeamId(Outfielder), Outfielder);
            Assert.GreaterOrEqual(partner, 0);

            var holderPos = new Vector2(40f, 25f);
            var partnerPos = new Vector2(40.20f, 25f);
            engine.TestOnly_SetAgent(
                Outfielder, AgentState.CreateAtPosition(holderPos, Vector2.right));
            engine.TestOnly_SetAgent(
                partner, AgentState.CreateAtPosition(partnerPos, Vector2.right));
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                holderPos.x, holderPos.y, MatchEngineConstants.BALL_REST_HEIGHT_M)));
            engine.TestOnly_SetPossession(Outfielder);

            Vector2 before = engine.AgentView(Outfielder).Position;
            engine.TestOnly_RunResolvePhase();
            Vector2 after = engine.AgentView(Outfielder).Position;

            Assert.Greater((after - before).sqrMagnitude, 1e-8f,
                "Precondition: Resolve collision response must actually position-correct the outfield holder.");
            Assert.AreEqual(Outfielder, engine.TestOnly_PossessingAgentId);
            Assert.AreEqual(BallStateType.Controlled, engine.BallView.State);
            AssertBallXYAtHolder(engine, Outfielder);
            Assert.AreEqual(MatchEngineConstants.BALL_REST_HEIGHT_M, engine.BallView.Position.z, 1e-6f,
                "Resolve reattachment must retain the outfield foot-height contract.");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void ControlledKeeper_ResolveCollisionCannotPushAttachedBallBehindOwnGoalPlane(int team)
        {
            var engine = new MatchEngine(MatchSeed ^ 0x1AUL ^ (ulong)team);
            int keeper = FindKeeper(engine, team);
            int partner = FindOtherOutfielder(engine, team, keeper);
            Assert.GreaterOrEqual(keeper, 0);
            Assert.GreaterOrEqual(partner, 0);

            float ownGoalX = team == 0 ? 0.0f : MatchEngineConstants.PITCH_LENGTH_M;
            float inward = team == 0 ? 1.0f : -1.0f;
            var keeperPos = new Vector2(
                ownGoalX + inward * 0.01f, MatchEngineConstants.KickoffBallYM);
            var partnerPos = new Vector2(
                ownGoalX + inward * 0.20f, MatchEngineConstants.KickoffBallYM);

            engine.TestOnly_SetAgent(
                keeper, AgentState.CreateAtPosition(
                    keeperPos, team == 0 ? Vector2.right : Vector2.left));
            engine.TestOnly_SetAgent(
                partner, AgentState.CreateAtPosition(
                    partnerPos, team == 0 ? Vector2.right : Vector2.left));

            const float claimHeight = 1.6f;
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                keeperPos.x, keeperPos.y, claimHeight)));
            engine.TestOnly_SetPossession(keeper);

            Vector2 partnerBefore = engine.AgentView(partner).Position;
            engine.TestOnly_RunResolvePhase();

            AgentState corrected = engine.AgentView(keeper);
            Vector2 partnerAfter = engine.AgentView(partner).Position;
            Assert.Greater((partnerAfter - partnerBefore).sqrMagnitude, 1e-8f,
                "Precondition: the goalmouth overlap must actually execute agent-agent separation.");
            Assert.AreEqual(ownGoalX, corrected.Position.x, 1e-6f,
                "Resolve collision correction must not leave a controlling keeper behind the goal plane.");
            Assert.AreEqual(corrected.Position, corrected.LastValidPosition,
                "Post-collision goal-plane correction must refresh the keeper recovery checkpoint.");
            Assert.AreEqual(keeper, engine.TestOnly_PossessingAgentId);
            Assert.AreEqual(BallStateType.Controlled, engine.BallView.State);
            AssertBallXYAtHolder(engine, keeper);
            Assert.AreEqual(claimHeight, engine.BallView.Position.z, 1e-6f,
                "Goal-plane reconciliation must preserve the actual claim/contact height.");
        }

        [Test]
        public void RestartTakerAward_DoesNotConvertPlacedBallToControlled()
        {
            var engine = new MatchEngine(MatchSeed ^ 0x20UL);

            Assert.AreNotEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId,
                "Precondition: boot awards the kickoff taker.");
            Assert.AreEqual(BallStateType.Stationary, engine.BallView.State,
                "W6: restart taker designation is not physical open-play control.");
            Assert.AreEqual(MatchEngineConstants.KickoffBallXM, engine.BallView.Position.x, 1e-6f);
            Assert.AreEqual(MatchEngineConstants.KickoffBallYM, engine.BallView.Position.y, 1e-6f);
            Assert.AreEqual(MatchEngineConstants.BALL_REST_HEIGHT_M, engine.BallView.Position.z, 1e-6f);
        }

        [Test]
        public void GoalkeeperControl_PreservesClaimHeight_AndFollowsKeeper()
        {
            var engine = new MatchEngine(MatchSeed ^ 0x30UL);
            int keeper = FindKeeper(engine, team: 0);
            Assert.GreaterOrEqual(keeper, 0);

            Vector2 keeperPos = engine.AgentView(keeper).Position;
            const float claimHeight = 1.6f;
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                keeperPos.x, keeperPos.y, claimHeight)));
            engine.TestOnly_SetPossession(keeper);

            Assert.AreEqual(BallStateType.Controlled, engine.BallView.State);
            Assert.AreEqual(claimHeight, engine.BallView.Position.z, 1e-6f,
                "W6 must not collapse a keeper claim to ground height.");

            var moved = new Vector2(keeperPos.x + 1.25f, keeperPos.y + 0.75f);
            engine.TestOnly_SetAgent(
                keeper, AgentState.CreateAtPosition(moved, Vector2.right));
            engine.TestOnly_SetCommand(keeper, MovementCommand.Stop(moved));
            engine.RunTick();

            AssertBallXYAtHolder(engine, keeper);
            Assert.AreEqual(claimHeight, engine.BallView.Position.z, 1e-6f,
                "Keeper carry preserves the actual claim/contact height while x/y follow the keeper.");
        }

        [TestCase(0)]
        [TestCase(1)]
        public void GoalkeeperControl_ClampsAtDefendedGoalPlane_AndKeepsRecoveryCoherent(int team)
        {
            var engine = new MatchEngine(MatchSeed ^ 0x38UL ^ (ulong)team);
            int keeper = FindKeeper(engine, team);
            Assert.GreaterOrEqual(keeper, 0);

            float ownGoalX = team == 0 ? 0.0f : MatchEngineConstants.PITCH_LENGTH_M;
            float exteriorX = team == 0 ? -1.25f : MatchEngineConstants.PITCH_LENGTH_M + 1.25f;
            float outwardVelocityX = team == 0 ? -2.0f : 2.0f;
            const float lateralVelocity = 0.75f;
            const float claimHeight = 1.6f;

            var carrier = AgentState.CreateAtPosition(
                new Vector2(exteriorX, MatchEngineConstants.KickoffBallYM),
                team == 0 ? Vector2.left : Vector2.right);
            carrier.Velocity = new Vector2(outwardVelocityX, lateralVelocity);
            carrier.Speed = carrier.Velocity.magnitude;
            carrier.LastValidPosition = carrier.Position;
            carrier.LastValidVelocity = carrier.Velocity;
            engine.TestOnly_SetAgent(keeper, carrier);
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                carrier.Position.x, carrier.Position.y, claimHeight)));

            engine.TestOnly_SetPossession(keeper);

            AgentState clamped = engine.AgentView(keeper);
            Assert.AreEqual(ownGoalX, clamped.Position.x, 1e-6f,
                "A physically controlling goalkeeper must not remain behind the goal plane he defends.");
            Assert.AreEqual(0.0f, clamped.Velocity.x, 1e-6f,
                "Outward velocity must be removed when the host clamps a controlled keeper at his own goal plane.");
            Assert.AreEqual(lateralVelocity, clamped.Velocity.y, 1e-6f,
                "The W6 host correction must not erase legal lateral keeper motion.");
            Assert.AreEqual(clamped.Velocity.magnitude, clamped.Speed, 1e-6f,
                "AgentState.Speed must remain coherent with the host-corrected velocity.");
            Assert.AreEqual(clamped.Position, clamped.LastValidPosition,
                "Safety recovery must not retain the illegal pre-clamp position.");
            Assert.AreEqual(clamped.Velocity, clamped.LastValidVelocity,
                "Safety recovery must not retain the illegal outward pre-clamp velocity.");
            Assert.AreEqual(BallStateType.Controlled, engine.BallView.State);
            AssertBallXYAtHolder(engine, keeper);
            Assert.AreEqual(claimHeight, engine.BallView.Position.z, 1e-6f,
                "Goal-plane correction must preserve the keeper's actual claim/contact height.");
        }

        [Test]
        public void ForceBallLoose_ExitsControlledState()
        {
            var engine = new MatchEngine(MatchSeed ^ 0x40UL);
            var holderPos = new Vector2(45f, 30f);
            engine.TestOnly_SetAgent(
                Outfielder, AgentState.CreateAtPosition(holderPos, Vector2.right));
            engine.TestOnly_SetPossession(Outfielder);
            Assert.AreEqual(BallStateType.Controlled, engine.BallView.State);

            var loosePos = new Vector3(47f, 30f, MatchEngineConstants.BALL_REST_HEIGHT_M);
            var looseVelocity = new Vector3(3f, 0f, 0f);
            engine.TestOnly_ForceBallLoose(loosePos, looseVelocity);

            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId);
            Assert.AreEqual(BallStateType.Rolling, engine.BallView.State,
                "W6 test staging must not leave a loose ball trapped in Controlled.");
            Assert.AreEqual(loosePos, engine.BallView.Position);
            Assert.AreEqual(looseVelocity, engine.BallView.Velocity);
        }

        [Test]
        public void LooseBall_DoesNotFreezeElapsedTackleCooldown()
        {
            var engine = new MatchEngine(MatchSeed ^ 0x48UL);
            const int defender = 4;
            engine.TestOnly_SetTackleCooldown(defender, remainingStrides: 2);

            engine.TestOnly_ForceBallLoose(
                new Vector3(52.5f, 34f, MatchEngineConstants.BALL_REST_HEIGHT_M),
                new Vector3(1f, 0f, 0f));

            engine.TestOnly_RunTackleResolver();

            Assert.AreEqual(1, engine.TestOnly_TackleCooldown(defender),
                "Tackle cooldown is elapsed AI-stride time; a loose/restart interval must not freeze it.");
        }

        [Test]
        public void ForcedKeeperRelease_ExitsControlled_AndDropsBallAtFeet()
        {
            var engine = new MatchEngine(MatchSeed ^ 0x50UL);
            int keeper = FindKeeper(engine, team: 0);
            Assert.GreaterOrEqual(keeper, 0);

            Vector2 keeperPos = engine.AgentView(keeper).Position;
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                keeperPos.x, keeperPos.y, 1.7f)));
            engine.TestOnly_SetPossession(keeper);
            Assert.AreEqual(BallStateType.Controlled, engine.BallView.State);

            for (int i = 0; i < MatchEngineConstants.GkMaxHoldTicks; i++)
            {
                engine.TestOnly_RunGoalkeeperReleaseRule();
            }

            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId);
            Assert.AreEqual(BallStateType.Stationary, engine.BallView.State,
                "A non-kick Law-12 release must explicitly leave Controlled.");
            Assert.AreEqual(MatchEngineConstants.BALL_REST_HEIGHT_M, engine.BallView.Position.z, 1e-6f,
                "The forced release places the ball at the keeper's feet, not floating at claim height.");
            Assert.AreEqual(Vector3.zero, engine.BallView.Velocity);
            Assert.Greater(engine.TestOnly_GkReleaseCooldownRemaining, 0);
        }

        private static int FindKeeper(MatchEngine engine, int team)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (engine.AgentTeamId(i) == team && engine.AgentIsGoalkeeper(i))
                {
                    return i;
                }
            }
            return -1;
        }

        private static int FindOtherOutfielder(MatchEngine engine, int team, int exclude)
        {
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (i != exclude
                    && engine.AgentTeamId(i) == team
                    && !engine.AgentIsGoalkeeper(i))
                {
                    return i;
                }
            }
            return -1;
        }

        private static void AssertBallXYAtHolder(MatchEngine engine, int holder)
        {
            Vector2 holderPos = engine.AgentView(holder).Position;
            Assert.AreEqual(holderPos.x, engine.BallView.Position.x, 1e-6f);
            Assert.AreEqual(holderPos.y, engine.BallView.Position.y, 1e-6f);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                        |
// | 1.4     | 2026-09-22 | —      | W6 scope closure: add outfield Resolve-correction attachment   |
// |         |            |        | coverage and mirrored home/away goal-plane collision cases.    |
// |         |            |        | The original keeper regression remains the pre-fix discriminator. |
// | 1.3     | 2026-09-22 | —      | W6 ordering defect surfaced by W3: direct Resolve collision-   |
// |         |            |        | correction lock; a still-Controlled keeper remains XY-attached |
// |         |            |        | after Collision #3 moves the holder during penetration response.|
// | 1.2     | 2026-09-15 | —      | Review closure: direct two-goal-plane keeper-control lock,    |
// |         |            |        | including AgentState recovery-checkpoint coherence.           |
// | 1.1     | 2026-09-14 | —      | P2 lock: loose ball still advances elapsed tackle cooldown.   |
// | 1.0     | 2026-09-14 | —      | W6 composed physical-control and release regression locks.    |
#endregion
