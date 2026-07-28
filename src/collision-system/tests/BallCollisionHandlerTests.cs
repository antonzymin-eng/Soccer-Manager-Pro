// File:     src/collision-system/tests/BallCollisionHandlerTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Collision System #3 §3.4.3 (ERR-003-007); shot-outcome design KD-6;
//           Code Standards #20
// Purpose:  Locks the detection-side gates on the now-live agent-ball deflection routing:
//           a Controlled ball is possession, a sub-gate-speed ball is first-touch territory,
//           and a fast approaching ball deflects.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.CollisionSystem.Tests
{
    [TestFixture]
    public class BallCollisionHandlerTests
    {
        private static AgentBallCollisionData Data(Vector3 agentPos)
        {
            return new AgentBallCollisionData
            {
                ContactPoint  = agentPos,
                AgentPosition = agentPos,
                AgentVelocity = Vector3.zero,
                BodyPart      = BodyPart.Torso,
                AgentID       = 3,
                TeamID        = 0,
                IsGoalkeeper  = false
            };
        }

        [Test]
        public void ControlledBall_IsNeverDeflected()
        {
            // A dribbling touch is possession, not a deflection (KD-6 gate a).
            var ball = new BallState
            {
                Position = new Vector3(10.0f, 34f, 0.11f),
                Velocity = new Vector3(15f, 0f, 0f),
                State    = BallStateType.Controlled
            };
            Vector3 before = ball.Velocity;

            BallCollisionHandler.OnAgentCollision(ref ball, Data(new Vector3(10.3f, 34f, 0f)));

            Assert.AreEqual(before, ball.Velocity, "A Controlled ball must never deflect off its holder");
        }

        [Test]
        public void SlowBall_IsLeftToTheControlModel()
        {
            // Below AgentDeflection.MinBallSpeedMps the contact is first-touch / pickup territory
            // (KD-6 gate c) — the reception path is untouched by this pass.
            var ball = new BallState
            {
                Position = new Vector3(10.0f, 34f, 0.11f),
                Velocity = new Vector3(
                    BallPhysicsConstants.AgentDeflection.MinBallSpeedMps * 0.5f, 0f, 0f),
                State    = BallStateType.Rolling
            };
            Vector3 before = ball.Velocity;

            BallCollisionHandler.OnAgentCollision(ref ball, Data(new Vector3(10.3f, 34f, 0f)));

            Assert.AreEqual(before, ball.Velocity, "A sub-gate-speed ball must not deflect");
        }

        [Test]
        public void FastApproachingBall_Deflects()
        {
            var ball = new BallState
            {
                Position = new Vector3(10.0f, 34f, 0.5f),
                Velocity = new Vector3(
                    BallPhysicsConstants.AgentDeflection.MinBallSpeedMps + 5f, 0f, 0f),
                State    = BallStateType.Airborne
            };

            BallCollisionHandler.OnAgentCollision(ref ball, Data(new Vector3(10.3f, 34f, 0f)));

            Assert.Less(ball.Velocity.x, 0f,
                "A fast ball approaching the body must deflect (reflected normal component)");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-27 | —      | Initial. Locks the KD-6 detection-side gates on the now-live       |
// |         |            |        | BallCollisionHandler.OnAgentCollision routing (ERR-003-007).       |
#endregion
