// File:     src/collision-system/tests/CollisionDeflectionFeedbackTests.cs
// Created:  2026-09-11
// Modified: 2026-09-11
// Author:   —
// Spec:     Collision System #3 §3.4.3; Match-engine wiring backlog W4; Code Standards #20
// Purpose:  Lock W4 per-call feedback: real response reports a deflection; separating overlap does not.

using NUnit.Framework;
using UnityEngine;
using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.CollisionSystem.Tests
{
    [TestFixture]
    public sealed class CollisionDeflectionFeedbackTests
    {
        private static bool Run(Vector3 ballVelocity, out BallState ball)
        {
            var system = new CollisionSystem();
            var agents = new[] { AgentState.CreateAtPosition(new Vector2(10.3f, 34f), Vector2.left) };
            var attrs = new[] { PlayerAttributes.CreateDefault() };
            var teams = new[] { 0 };
            var keepers = new[] { false };
            var knockdown = new bool[1];
            var force = new float[1];
            var stumble = new bool[1];
            ball = new BallState {
                Position = new Vector3(10f, 34f, 0.5f),
                Velocity = ballVelocity,
                State = BallStateType.Airborne
            };

            system.UpdateCollisions(
                agents, attrs, teams, keepers, knockdown, force, stumble,
                ref ball, 123UL, 1, 0.016f, null, out bool deflected);
            return deflected;
        }

        [Test]
        public void AppliedAgentBallResponse_ReportsDeflection()
        {
            float speed = BallPhysicsConstants.AgentDeflection.MinBallSpeedMps + 5f;
            bool deflected = Run(new Vector3(speed, 0f, 0f), out BallState ball);
            Assert.IsTrue(deflected);
            Assert.Less(ball.Velocity.x, 0f);
        }

        [Test]
        public void SeparatingOverlap_DoesNotReportDeflection()
        {
            float speed = BallPhysicsConstants.AgentDeflection.MinBallSpeedMps + 5f;
            bool deflected = Run(new Vector3(-speed, 0f, 0f), out BallState ball);
            Assert.IsFalse(deflected);
            Assert.Less(ball.Velocity.x, 0f);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                        |
// | 1.0     | 2026-09-11 | —      | W4: applied-vs-overlap collision feedback regression locks. |
#endregion
