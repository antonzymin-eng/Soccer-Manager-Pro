// File:     src/Core/Physics/Ball/Tests/BallPhysicsCoreTests.cs
// Created:  2026-05-24
// Modified: 2026-05-24
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Unit tests for BallPhysicsCore force calculations and validation.
//           Derived test values from Spec §3.1.14 and §5 test plan.

using NUnit.Framework;
using UnityEngine;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.BallPhysics.Tests
{
    /// <summary>
    /// Unit tests for BallPhysicsCore force calculations and validation.
    /// Spec §3.1.14 derived test values and §5 test plan.
    /// </summary>
    public class BallPhysicsCoreTests
    {
        // ── Magnus ───────────────────────────────────────────────────────────────

        [Test]
        public void MagnusForce_ZeroSpin_ReturnsZero()
        {
            Vector3 force = BallPhysicsCore.CalculateMagnusForce(
                velocity:        new Vector3(20f, 0f, 0f),
                angularVelocity: Vector3.zero);

            Assert.AreEqual(Vector3.zero, force);
        }

        [Test]
        public void MagnusForce_ZeroVelocity_ReturnsZero()
        {
            Vector3 force = BallPhysicsCore.CalculateMagnusForce(
                velocity:        Vector3.zero,
                angularVelocity: new Vector3(0f, 0f, 12f));

            Assert.AreEqual(Vector3.zero, force);
        }

        [Test]
        public void MagnusForce_ParallelSpinAndVelocity_ReturnsZero()
        {
            // ω parallel to v → cross product = 0
            Vector3 force = BallPhysicsCore.CalculateMagnusForce(
                velocity:        new Vector3(20f, 0f, 0f),
                angularVelocity: new Vector3(12f, 0f, 0f));

            Assert.That(force.magnitude, Is.LessThan(0.001f));
        }

        [Test]
        public void MagnusForce_Sidespin_CurvesLaterally()
        {
            // ω_z < 0 (CW from above), v in +X → force in -Y
            Vector3 force = BallPhysicsCore.CalculateMagnusForce(
                velocity:        new Vector3(22f, 0f, 0f),
                angularVelocity: new Vector3(0f, 0f, -12f));

            Assert.That(force.y, Is.LessThan(0f), "Clockwise sidespin should produce -Y force");
            Assert.That(Mathf.Abs(force.x), Is.LessThan(0.01f), "No X component");
        }

        // ── Drag ─────────────────────────────────────────────────────────────────

        [Test]
        public void DragForce_ZeroVelocity_ReturnsZero()
        {
            Vector3 force = BallPhysicsCore.CalculateDragForce(Vector3.zero);
            Assert.AreEqual(Vector3.zero, force);
        }

        [Test]
        public void DragForce_OpposesMotion()
        {
            Vector3 velocity = new Vector3(15f, 0f, 0f);
            Vector3 force    = BallPhysicsCore.CalculateDragForce(velocity);

            Assert.That(force.x, Is.LessThan(0f), "Drag must oppose velocity direction");
        }

        [Test]
        public void DragForce_IncreasesWithSpeed()
        {
            Vector3 slow = BallPhysicsCore.CalculateDragForce(new Vector3(5f, 0f, 0f));
            Vector3 fast = BallPhysicsCore.CalculateDragForce(new Vector3(15f, 0f, 0f));

            Assert.That(fast.magnitude, Is.GreaterThan(slow.magnitude));
        }

        // ── Gravity ──────────────────────────────────────────────────────────────

        [Test]
        public void GravityForce_IsDownward()
        {
            Vector3 g = BallPhysicsCore.GetGravityForce();

            Assert.That(g.z, Is.LessThan(0f));
            Assert.AreEqual(0f, g.x);
            Assert.AreEqual(0f, g.y);
        }

        [Test]
        public void GravityForce_MatchesMassTimesG()
        {
            Vector3 g        = BallPhysicsCore.GetGravityForce();
            float   expected = BallPhysicsConstants.Ball.MASS * BallPhysicsConstants.Environment.GRAVITY;

            Assert.That(Mathf.Abs(g.z), Is.EqualTo(expected).Within(0.001f));
        }

        // ── Spin decay ───────────────────────────────────────────────────────────

        [Test]
        public void SpinDecay_ZeroSpin_ReturnsZero()
        {
            Vector3 result = BallPhysicsCore.UpdateSpinDecay(
                angularVelocity: Vector3.zero,
                velocity:        new Vector3(20f, 0f, 0f),
                dt:              1f / 60f);

            Assert.AreEqual(Vector3.zero, result);
        }

        [Test]
        public void SpinDecay_SpinDecreases()
        {
            Vector3 initial = new Vector3(0f, 0f, 12f);
            Vector3 after   = BallPhysicsCore.UpdateSpinDecay(initial, new Vector3(20f, 0f, 0f), 1f / 60f);

            Assert.That(after.magnitude, Is.LessThan(initial.magnitude));
        }

        [Test]
        public void RollingSpinDecay_DecreasesPerSecond()
        {
            Vector3 initial   = new Vector3(0f, 0f, 10f);
            Vector3 afterOneS = BallPhysicsCore.UpdateRollingSpinDecay(initial, 1f);
            float   expected  = 10f - BallPhysicsConstants.Spin.RollingSpinDecayPerSecond;

            Assert.That(afterOneS.magnitude, Is.EqualTo(expected).Within(0.001f));
        }

        // ── Validation ───────────────────────────────────────────────────────────

        [Test]
        public void Validation_DetectsNaN_AndRecovers()
        {
            var ball = new BallState
            {
                Position          = new Vector3(float.NaN, 34f, 0f),
                Velocity          = new Vector3(10f, 0f, 0f),
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(5f, 0f, 0f),
                State             = BallStateType.ROLLING
            };

            BallPhysicsCore.ValidatePhysicsState(ref ball);

            Assert.AreEqual(50f, ball.Position.x, "Should recover to last valid X");
            Assert.AreEqual(BallStateType.STATIONARY, ball.State);
        }

        [Test]
        public void Validation_ClampsExcessiveVelocity()
        {
            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(100f, 0f, 0f),
                State             = BallStateType.ROLLING,
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = Vector3.zero
            };

            BallPhysicsCore.ValidatePhysicsState(ref ball);

            Assert.That(ball.Velocity.magnitude, Is.LessThanOrEqualTo(BallPhysicsConstants.Limits.MaxVelocity));
        }

        [Test]
        public void Validation_PreventsGroundPenetration()
        {
            float belowGround = BallPhysicsConstants.Ball.RADIUS - 0.05f;
            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, belowGround),
                Velocity          = new Vector3(5f, 0f, -1f),
                State             = BallStateType.ROLLING,
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(5f, 0f, 0f)
            };

            BallPhysicsCore.ValidatePhysicsState(ref ball);

            Assert.That(ball.Position.z, Is.GreaterThanOrEqualTo(BallPhysicsConstants.Ball.RADIUS));
        }

        // ── Bounce height (§3.1.14) ──────────────────────────────────────────────

        [Test]
        public void Bounce_DryGrass_ReturnsExpectedHeight()
        {
            // 2m drop: impact velocity v = sqrt(2gh) = 6.26 m/s → rebound 0.80–0.90m
            float groundLevel = BallPhysicsConstants.Ball.RADIUS;

            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, groundLevel),
                Velocity          = new Vector3(0f, 0f, -6.26f),
                AngularVelocity   = Vector3.zero,
                State             = BallStateType.BOUNCING,
                LastValidPosition = new Vector3(50f, 34f, groundLevel),
                LastValidVelocity = new Vector3(0f, 0f, -6.26f)
            };

            BallGroundInteraction.ApplyBounce(ref ball, SurfaceType.GRASS_DRY, null, 0f);

            // h = vz² / (2g)
            float reboundHeight = ball.Velocity.z * ball.Velocity.z
                                / (2f * BallPhysicsConstants.Environment.GRAVITY);

            Assert.That(reboundHeight, Is.GreaterThan(0.80f), "Rebound must reach at least 0.80m");
            Assert.That(reboundHeight, Is.LessThan(0.90f),    "Rebound must not exceed 0.90m");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Fix pass: namespace → TacticalDirector.BallPhysics.Tests; ALL_CAPS |
// |         |            |        | constant refs → PascalCase; file header per FR-CS-056/057.         |
#endregion
