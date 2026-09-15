// File:     src/ball-physics/tests/BallControlStateTests.cs
// Created:  2026-09-14
// Modified: 2026-09-14
// Author:   —
// Spec:     Ball Physics #1 §3.1.11; Match-engine wiring backlog W6; Code Standards #20
// Purpose:  Lock Ball Physics' physical-control entry/release state transitions used by W6.

using NUnit.Framework;
using UnityEngine;

namespace TacticalDirector.BallPhysics.Tests
{
    [TestFixture]
    public sealed class BallControlStateTests
    {
        [Test]
        public void SetBallControlled_ZeroesMotion_AndRefreshesRecoveryCheckpoint()
        {
            var ball = BallState.CreateAtPosition(new Vector3(12f, 8f, BallPhysicsConstants.Ball.RADIUS));
            ball.Velocity = new Vector3(2f, 1f, 0f);
            ball.AngularVelocity = new Vector3(0f, 0f, 5f);
            ball.LastValidPosition = Vector3.zero;
            ball.LastValidVelocity = Vector3.one;

            BallCollision.SetBallControlled(ref ball);

            Assert.AreEqual(BallStateType.Controlled, ball.State);
            Assert.AreEqual(Vector3.zero, ball.Velocity);
            Assert.AreEqual(Vector3.zero, ball.AngularVelocity);
            Assert.AreEqual(ball.Position, ball.LastValidPosition);
            Assert.AreEqual(Vector3.zero, ball.LastValidVelocity);
        }

        [Test]
        public void ReleaseBallControl_FromControlled_ReturnsStationaryWithoutMovingBall()
        {
            var position = new Vector3(12f, 8f, 1.4f);
            var ball = BallState.CreateAtPosition(position);
            BallCollision.SetBallControlled(ref ball);

            BallCollision.ReleaseBallControl(ref ball);

            Assert.AreEqual(BallStateType.Stationary, ball.State);
            Assert.AreEqual(position, ball.Position);
            Assert.AreEqual(Vector3.zero, ball.Velocity);
            Assert.AreEqual(Vector3.zero, ball.AngularVelocity);
            Assert.AreEqual(position, ball.LastValidPosition);
            Assert.AreEqual(Vector3.zero, ball.LastValidVelocity);
        }

        [Test]
        public void ReleaseBallControl_FromPlacedStationaryBall_IsNoOp()
        {
            var position = new Vector3(52.5f, 34f, BallPhysicsConstants.Ball.RADIUS);
            var ball = BallState.CreateAtPosition(position);

            BallCollision.ReleaseBallControl(ref ball);

            Assert.AreEqual(BallStateType.Stationary, ball.State);
            Assert.AreEqual(position, ball.Position);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                |
// | 1.0     | 2026-09-14 | —      | W6 Controlled entry/non-kick release regression set. |
#endregion
