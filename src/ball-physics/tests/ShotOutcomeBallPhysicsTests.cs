// File:     src/ball-physics/tests/ShotOutcomeBallPhysicsTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Ball Physics #1 §3.1.10.1/§3.1.10.3 (ERR-001-004, ERR-003-007);
//           shot-outcome design KD-5/KD-6; Code Standards #20
// Purpose:  Unit locks for the shot-outcome pass's Ball Physics half: the agent-ball
//           deflection response and the Law 9/10 airborne boundary adjudication.

using NUnit.Framework;

using UnityEngine;

namespace TacticalDirector.BallPhysics.Tests
{
    [TestFixture]
    public class ShotOutcomeBallPhysicsTests
    {
        // ── ApplyAgentDeflection (KD-6, ERR-003-007) ────────────────────────────────────────────

        [Test]
        public void Deflection_ApproachingBall_ReflectsAndRetainsSpeedFraction()
        {
            // Ball driving straight at an agent 0.3 m away along +X: the planar normal is −X,
            // vn = −20, reflection flips the X component, Torso retention scales the whole vector.
            var ball = new BallState
            {
                Position        = new Vector3(10.0f, 34f, 0.5f),
                Velocity        = new Vector3(20f, 0f, 0f),
                AngularVelocity = new Vector3(0f, 0f, 10f),
                State           = BallStateType.Airborne
            };
            Vector3 agentPos = new Vector3(10.3f, 34f, 0f);

            bool applied = BallCollision.ApplyAgentDeflection(ref ball, agentPos, BodyPart.Torso);

            Assert.IsTrue(applied);
            float torsoSpeed = BallPhysicsConstants.BodyPartRetention.TorsoSpeed;
            float torsoSpin  = BallPhysicsConstants.BodyPartRetention.TorsoSpin;
            Assert.AreEqual(-20f * torsoSpeed, ball.Velocity.x, 1e-4f,
                "Normal component must reflect and scale by the body part's speed retention");
            Assert.AreEqual(0f, ball.Velocity.y, 1e-4f);
            Assert.AreEqual(10f * torsoSpin, ball.AngularVelocity.z, 1e-4f,
                "Spin must scale by the body part's spin retention");
        }

        [Test]
        public void Deflection_VerticalVelocity_PassesThroughReflection()
        {
            // The body is a vertical cylinder ⇒ planar normal ⇒ the reflection leaves Vz alone
            // (it is still scaled by speed retention, as the whole vector is).
            var ball = new BallState
            {
                Position = new Vector3(10.0f, 34f, 1.0f),
                Velocity = new Vector3(15f, 0f, -6f),
                State    = BallStateType.Airborne
            };
            Vector3 agentPos = new Vector3(10.3f, 34f, 0f);

            Assert.IsTrue(BallCollision.ApplyAgentDeflection(ref ball, agentPos, BodyPart.Torso));
            Assert.AreEqual(-6f * BallPhysicsConstants.BodyPartRetention.TorsoSpeed,
                ball.Velocity.z, 1e-4f,
                "A planar normal must not reflect the vertical component — only retention scales it");
        }

        [Test]
        public void Deflection_SeparatingBall_IsNoOp()
        {
            // The kick-release geometry: ball inside the kicker's hitbox but moving AWAY —
            // the stateless self-block guard (KD-6 gate b). No cooldown state exists to check.
            var ball = new BallState
            {
                Position = new Vector3(10.5f, 34f, 0.2f),
                Velocity = new Vector3(20f, 0f, 3f),
                State    = BallStateType.Airborne
            };
            Vector3 agentPos = new Vector3(10.2f, 34f, 0f);   // ball ahead of agent, moving further ahead
            Vector3 before = ball.Velocity;

            Assert.IsFalse(BallCollision.ApplyAgentDeflection(ref ball, agentPos, BodyPart.Torso));
            Assert.AreEqual(before, ball.Velocity, "A separating contact must not deflect");
        }

        [Test]
        public void Deflection_DegenerateSeparation_IsNoOp()
        {
            var ball = new BallState
            {
                Position = new Vector3(10f, 34f, 0.5f),
                Velocity = new Vector3(20f, 0f, 0f)
            };
            Vector3 agentPos = new Vector3(10f, 34f, 0f);     // ball exactly at the agent's centre

            Assert.IsFalse(BallCollision.ApplyAgentDeflection(ref ball, agentPos, BodyPart.Torso));
            Assert.AreEqual(20f, ball.Velocity.x, "A degenerate normal must be a no-op, not a NaN");
        }

        // ── CheckBoundaries airborne adjudication (KD-5, ERR-001-004) ───────────────────────────

        private static float GoalCentreY => BallPhysicsConstants.Pitch.WIDTH / 2f;

        [Test]
        public void Boundary_AirborneBallInsideGoalMouth_IsGoal()
        {
            // 1.8 m up, between the posts, under the 2.44 m bar — pre-fix this was neither a goal
            // nor out of play (the z < Diameter gate; §5.Z.17 §7.2).
            var ball = new BallState
            {
                Position = new Vector3(-BallPhysicsConstants.Ball.RADIUS - 0.01f, GoalCentreY, 1.8f),
                Velocity = new Vector3(-15f, 0f, 0f),
                State    = BallStateType.Airborne
            };

            var (isOut, restart) = BallCollision.CheckBoundaries(ball, lastTouchTeamID: 1);

            Assert.IsTrue(isOut);
            Assert.AreEqual(RestartType.KickOff, restart, "An airborne ball under the bar is a goal (Law 10)");
        }

        [Test]
        public void Boundary_BallOverTheCrossbar_IsNotAGoal()
        {
            // Same crossing, 0.5 m above the bar: over — a goal kick for the defenders when the
            // attackers touched last. THE crossbar test: pre-fix the goal had unbounded height.
            var ball = new BallState
            {
                Position = new Vector3(
                    -BallPhysicsConstants.Ball.RADIUS - 0.01f,
                    GoalCentreY,
                    BallPhysicsConstants.Pitch.GOAL_HEIGHT + 0.5f),
                Velocity = new Vector3(-15f, 0f, 2f),
                State    = BallStateType.Airborne
            };

            var (isOut, restart) = BallCollision.CheckBoundaries(ball, lastTouchTeamID: 1);

            Assert.IsTrue(isOut, "A ball crossing the goal line over the bar is out of play");
            Assert.AreEqual(RestartType.GoalKick, restart, "Over the bar is a goal kick, not a goal");
        }

        [Test]
        public void Boundary_AirborneBallWideOfThePosts_IsNotAGoal()
        {
            var ball = new BallState
            {
                Position = new Vector3(
                    -BallPhysicsConstants.Ball.RADIUS - 0.01f,
                    GoalCentreY + BallPhysicsConstants.Pitch.GOAL_WIDTH / 2f + 1.0f,
                    1.0f),
                Velocity = new Vector3(-15f, 3f, 0f),
                State    = BallStateType.Airborne
            };

            var (isOut, restart) = BallCollision.CheckBoundaries(ball, lastTouchTeamID: 1);

            Assert.IsTrue(isOut);
            Assert.AreEqual(RestartType.GoalKick, restart);
        }

        [Test]
        public void Boundary_AirborneBallOverTouchline_IsThrowIn()
        {
            var ball = new BallState
            {
                Position = new Vector3(
                    52f,
                    BallPhysicsConstants.Pitch.WIDTH + BallPhysicsConstants.Ball.RADIUS + 0.01f,
                    4.0f),
                Velocity = new Vector3(2f, 8f, 1f),
                State    = BallStateType.Airborne
            };

            var (isOut, restart) = BallCollision.CheckBoundaries(ball, lastTouchTeamID: 0);

            Assert.IsTrue(isOut, "A ball wholly across the touchline in the air is out (Law 9)");
            Assert.AreEqual(RestartType.ThrowIn, restart);
        }

        [Test]
        public void Boundary_HighBallOverPitchInterior_StaysInPlay()
        {
            var ball = new BallState
            {
                Position = new Vector3(52f, 34f, 20f),
                Velocity = new Vector3(10f, 0f, 5f),
                State    = BallStateType.Airborne
            };

            var (isOut, restart) = BallCollision.CheckBoundaries(ball, lastTouchTeamID: 0);

            Assert.IsFalse(isOut, "Height alone never puts the ball out — only crossing a line does");
            Assert.AreEqual(RestartType.None, restart);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-27 | —      | Initial. Locks the shot-outcome pass's Ball Physics half:          |
// |         |            |        | ApplyAgentDeflection (reflect + retention; separating/degenerate   |
// |         |            |        | no-ops — the stateless self-block guard) and the Law 9/10 airborne |
// |         |            |        | boundary adjudication (goal under the bar, goal kick over it,      |
// |         |            |        | throw-in in the air, interior height never out).                   |
#endregion
