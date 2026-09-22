// File:     src/collision-system/tests/CollisionDeflectionFeedbackTests.cs
// Created:  2026-09-11
// Modified: 2026-09-22
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
        private sealed class ContactProbe : ICollisionEventConsumer
        {
            private readonly CollisionEvent[] _events = new CollisionEvent[SpatialHashConstants.AgentCapacity];
            public int Count { get; private set; }
            public CollisionEvent EventAt(int index) => _events[index];

            public void OnCollisionEvent(in CollisionEvent evt)
            {
                _events[Count++] = evt;
            }
        }

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
        public void ReadOnlyAgentBallFeed_PreservesWorld_AndPublishesEntityOrder()
        {
            var system = new CollisionSystem();
            var agents = new[]
            {
                AgentState.CreateAtPosition(new Vector2(10.30f, 34f), Vector2.left),
                AgentState.CreateAtPosition(new Vector2(10.35f, 34f), Vector2.left)
            };
            var attrs = new[] { PlayerAttributes.CreateDefault(), PlayerAttributes.CreateDefault() };
            var ball = new BallState
            {
                Position = new Vector3(10f, 34f, 0.5f),
                Velocity = new Vector3(20f, 1f, 0.5f),
                State = BallStateType.Airborne
            };

            Vector2 a0 = agents[0].Position;
            Vector2 a1 = agents[1].Position;
            Vector3 ballPos = ball.Position;
            Vector3 ballVel = ball.Velocity;
            var probe = new ContactProbe();

            system.PublishAgentBallContacts(agents, attrs, in ball, 1.25f, probe);

            Assert.AreEqual(2, probe.Count, "W3 read-only feed must publish both coarse overlaps.");
            Assert.AreEqual(SpatialHashConstants.BALL_ENTITY_ID, probe.EventAt(0).Entity1ID);
            Assert.AreEqual(0, probe.EventAt(0).Entity2ID);
            Assert.AreEqual(1, probe.EventAt(1).Entity2ID,
                "W3 feed must preserve canonical agent/entity iteration order.");
            Assert.AreEqual(a0, agents[0].Position);
            Assert.AreEqual(a1, agents[1].Position);
            Assert.AreEqual(ballPos, ball.Position);
            Assert.AreEqual(ballVel, ball.Velocity,
                "Read-only W3 publication must not apply the torso deflection response.");
        }

        [Test]
        public void ReadOnlyAgentBallFeed_AboveStage0Reach_PublishesNothing()
        {
            var system = new CollisionSystem();
            var agents = new[] { AgentState.CreateAtPosition(new Vector2(10f, 34f), Vector2.left) };
            var attrs = new[] { PlayerAttributes.CreateDefault() };
            var ball = new BallState
            {
                Position = new Vector3(10f, 34f, CollisionPhysicsConstants.AgentReachHeight + 0.25f),
                State = BallStateType.Airborne
            };
            var probe = new ContactProbe();

            system.PublishAgentBallContacts(agents, attrs, in ball, 2f, probe);

            Assert.AreEqual(0, probe.Count,
                "Collision #3's W3 feed is deliberately coarse and must not become Heading's high-aerial eligibility authority.");
        }

        [Test]
        public void SeparatingOverlap_DoesNotReportDeflection_AndLeavesFlightUnchanged()
        {
            float speed = BallPhysicsConstants.AgentDeflection.MinBallSpeedMps + 5f;
            Vector3 inputVelocity = new Vector3(-speed, 0.75f, 0.25f);

            bool deflected = Run(inputVelocity, out BallState ball);

            Assert.IsFalse(deflected,
                "W4: a separating geometric overlap must not be reported as a changed threat.");
            Assert.AreEqual(inputVelocity.x, ball.Velocity.x, 1e-6f,
                "A no-response overlap must preserve the X flight component exactly.");
            Assert.AreEqual(inputVelocity.y, ball.Velocity.y, 1e-6f,
                "A no-response overlap must preserve the Y flight component exactly.");
            Assert.AreEqual(inputVelocity.z, ball.Velocity.z, 1e-6f,
                "A no-response overlap must preserve the Z flight component exactly.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                        |
// | 1.0     | 2026-09-11 | —      | W4: applied-vs-overlap collision feedback regression locks. |
// | 1.1     | 2026-09-12 | —      | Review: unchanged-flight case now locks all velocity terms. |
#endregion
