// File:     src/match-engine/tests/MatchEngineControlledBallW6Tests.cs
// Created:  2026-09-14
// Modified: 2026-09-14
// Author:   —
// Spec:     Match-engine wiring backlog W6; Ball Physics #1 §3.1.11; Code Standards #20
// Purpose:  Composed W6 locks: genuine possession enters BallState.Controlled and follows the holder,
//           restart-taker designation remains a placed Stationary ball, and non-kick release exits
//           physical control without introducing new cross-tick state.

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
// | 1.0     | 2026-09-14 | —      | W6 composed physical-control and release regression locks.    |
#endregion
