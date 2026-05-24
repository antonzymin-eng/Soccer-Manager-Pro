// File:     src/Core/Physics/Ball/Tests/BallIntegrationTests.cs
// Created:  2026-05-24
// Modified: 2026-05-24
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  End-to-end integration tests against §3.1.14 derived validation test cases.
//           Each test simulates a realistic scenario and verifies output within the
//           expected range derived in the spec.

using NUnit.Framework;
using UnityEngine;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.BallPhysics.Tests
{
    /// <summary>
    /// End-to-end integration tests against §3.1.14 derived validation test cases.
    /// Each test simulates a realistic scenario and verifies the output is within
    /// the specified expected range derived in the spec.
    /// </summary>
    public class BallIntegrationTests
    {
        private const float DT = 1f / 60f;

        // ── §3.1.14: Free kick curve (1.5–3.0 m lateral deviation over 25 m) ────

        [Test]
        public void FreekickTrajectory_CurvesWithinExpectedRange()
        {
            // Right-foot free kick from (25,34): v=(22,0,6), ω=(0,0,-12)
            // Expected: 1.5–3.0 m lateral curve over 25 m forward.
            var ball = new BallState
            {
                State             = BallStateType.AIRBORNE,
                Position          = new Vector3(25f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(22f, 0f, 6f),
                AngularVelocity   = new Vector3(0f, 0f, -12f),
                LastValidPosition = new Vector3(25f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(22f, 0f, 6f)
            };

            float startY  = ball.Position.y;
            int   maxSteps = 300;

            while (ball.Position.x < 50f
                && ball.Position.z >= BallPhysicsConstants.Ball.RADIUS
                && maxSteps-- > 0)
            {
                BallPhysicsCore.UpdateBallPhysics(
                    ref ball, DT, SurfaceType.GRASS_DRY, Vector3.zero, null, 0f);
            }

            float lateralDeviation = Mathf.Abs(ball.Position.y - startY);

            Assert.That(lateralDeviation, Is.GreaterThan(1.5f),
                $"Ball curved only {lateralDeviation:F2} m — expected >1.5 m");
            Assert.That(lateralDeviation, Is.LessThan(3.0f),
                $"Ball curved {lateralDeviation:F2} m — expected <3.0 m");
        }

        // ── §3.1.14: Rolling distance (26–31 m on dry grass at 10 m/s) ──────────

        [Test]
        public void RollingDistance_DryGrass_StopsWithinExpectedRange()
        {
            var ball = new BallState
            {
                State             = BallStateType.ROLLING,
                Position          = new Vector3(0f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(10f, 0f, 0f),
                AngularVelocity   = Vector3.zero,
                LastValidPosition = new Vector3(0f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(10f, 0f, 0f)
            };

            int maxSteps = 60 * 15; // 15 seconds max

            while (ball.State == BallStateType.ROLLING && maxSteps-- > 0)
            {
                BallPhysicsCore.UpdateBallPhysics(
                    ref ball, DT, SurfaceType.GRASS_DRY, Vector3.zero, null, 0f);
            }

            float stoppingDistance = ball.Position.x;

            Assert.That(stoppingDistance, Is.GreaterThan(26f),
                $"Ball stopped at {stoppingDistance:F1} m — expected >26 m");
            Assert.That(stoppingDistance, Is.LessThan(31f),
                $"Ball stopped at {stoppingDistance:F1} m — expected <31 m");
        }

        // ── Possession: ApplyKick + SetBallControlled round-trip ─────────────────

        [Test]
        public void Possession_SetControlled_ThenKick_TransitionsCorrectly()
        {
            var ball = BallState.CreateAtPosition(new Vector3(52.5f, 34f, BallPhysicsConstants.Ball.RADIUS));

            BallCollision.SetBallControlled(ref ball);
            Assert.AreEqual(BallStateType.CONTROLLED, ball.State);
            Assert.AreEqual(Vector3.zero, ball.Velocity);

            BallCollision.ApplyKick(
                ref ball,
                velocity:  new Vector3(20f, 0f, 5f),
                spin:      new Vector3(0f, 0f, -10f),
                agentId:   0,
                matchTime: 0f,
                logger:    null);

            Assert.AreEqual(BallStateType.AIRBORNE, ball.State,
                "Kick with positive z velocity must produce AIRBORNE state");
            Assert.That(ball.Velocity.magnitude, Is.GreaterThan(0f));
        }

        [Test]
        public void Possession_GroundKick_TransitionsToRolling()
        {
            var ball = BallState.CreateAtPosition(new Vector3(52.5f, 34f, BallPhysicsConstants.Ball.RADIUS));
            BallCollision.SetBallControlled(ref ball);

            BallCollision.ApplyKick(
                ref ball,
                velocity:  new Vector3(10f, 0f, 0f),
                spin:      Vector3.zero,
                agentId:   1,
                matchTime: 0f);

            Assert.AreEqual(BallStateType.ROLLING, ball.State);
        }

        // ── Boundary detection ───────────────────────────────────────────────────

        [Test]
        public void Boundary_BallCrossesTouchline_ReturnsThrowIn()
        {
            var ball = new BallState
            {
                State    = BallStateType.ROLLING,
                Position = new Vector3(52f, -BallPhysicsConstants.Ball.RADIUS - 0.01f, 0f),
                Velocity = new Vector3(5f, -2f, 0f)
            };

            var (isOut, restart) = BallCollision.CheckBoundaries(ball, lastTouchTeamID: 0);

            Assert.IsTrue(isOut);
            Assert.AreEqual(RestartType.THROW_IN, restart);
        }

        [Test]
        public void Boundary_BallEntersGoal_ReturnsKickoff()
        {
            // Ball at ground level inside home goal: x < -r, y at goal centre, z < crossbar.
            // z must be < Ball.Diameter (0.22 m) to satisfy the Stage 0 lowEnough gate.
            float goalCenterY = BallPhysicsConstants.Pitch.WIDTH / 2f;
            var ball = new BallState
            {
                State    = BallStateType.ROLLING,
                Position = new Vector3(
                    -BallPhysicsConstants.Ball.RADIUS - 0.01f,
                    goalCenterY,
                    BallPhysicsConstants.Ball.RADIUS),   // ground level — within lowEnough gate
                Velocity = new Vector3(-5f, 0f, 0f)
            };

            var (isOut, restart) = BallCollision.CheckBoundaries(ball, lastTouchTeamID: 1);

            Assert.IsTrue(isOut);
            Assert.AreEqual(RestartType.KICKOFF, restart);
        }

        // ── Goal post collision ───────────────────────────────────────────────────

        [Test]
        public void GoalPostCollision_ReflectsVelocity()
        {
            var ball = new BallState
            {
                Position        = new Vector3(0f, 34f, 1f),
                Velocity        = new Vector3(-15f, 0f, 0f),
                AngularVelocity = Vector3.zero,
                State           = BallStateType.AIRBORNE
            };

            Vector3 postCenter   = new Vector3(0f, 34f, 1f);
            Vector3 contactPoint = new Vector3(0.06f, 34f, 1f); // On post surface.

            BallCollision.ApplyGoalPostCollision(ref ball, contactPoint, postCenter, null, 0f);

            Assert.That(ball.Velocity.x, Is.GreaterThan(0f),
                "Ball must rebound away from post");
        }

        // ── NaN recovery under full update ───────────────────────────────────────

        [Test]
        public void FullUpdate_NaNInput_RecoversWithoutException()
        {
            var ball = new BallState
            {
                State             = BallStateType.AIRBORNE,
                Position          = new Vector3(float.NaN, 34f, 5f),
                Velocity          = new Vector3(20f, 0f, 2f),
                AngularVelocity   = Vector3.zero,
                LastValidPosition = new Vector3(50f, 34f, 5f),
                LastValidVelocity = new Vector3(20f, 0f, 2f)
            };

            Assert.DoesNotThrow(() =>
                BallPhysicsCore.UpdateBallPhysics(ref ball, DT, SurfaceType.GRASS_DRY, Vector3.zero, null, 0f));

            Assert.IsFalse(float.IsNaN(ball.Position.x), "Position must not be NaN after recovery");
        }

        // ── Event logging ─────────────────────────────────────────────────────────

        [Test]
        public void EventLogger_LogsBounce_EventIsRecorded()
        {
            var logger = new BallEventLogger();
            var ball   = new BallState
            {
                State             = BallStateType.BOUNCING,
                Position          = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(5f, 0f, -4f),
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(5f, 0f, -4f)
            };

            BallGroundInteraction.ApplyBounce(ref ball, SurfaceType.GRASS_DRY, logger, 10f);

            var events = logger.ExportEvents();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(BallEventType.BOUNCE, events[0].Type);
            Assert.AreEqual(10f, events[0].Timestamp, 0.001f);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Bug fix: Boundary_BallEntersGoal_ReturnsKickoff had z=0.5f which   |
// |         |            |        | exceeded Ball.Diameter (0.22 m) making lowEnough=false — goal was  |
// |         |            |        | never detected; corrected to Ball.RADIUS (ground level).           |
// |         |            |        | Standards fix: namespace → TacticalDirector.BallPhysics.Tests;     |
// |         |            |        | ALL_CAPS constant refs → PascalCase; file header per FR-CS-056/057. |
#endregion
