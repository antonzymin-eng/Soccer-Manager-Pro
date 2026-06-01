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
            // ω parallel to v (both in +X) → cross product = 0
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

        // ── UT-MAG-004 ────────────────────────────────────────────────────────────

        [Test]
        public void MagnusForce_Topspin_ProducesDownwardForce()
        {
            // ω_y < 0 (topspin), v in +X.
            // ω̂ × v̂ = (0,-1,0) × (1,0,0) = (0*0-0*0, 0*1-(-1)*0, (-1)*0-0*1) = (0,0,-1) → downward.
            Vector3 force = BallPhysicsCore.CalculateMagnusForce(
                velocity:        new Vector3(20f, 0f, 0f),
                angularVelocity: new Vector3(0f, -10f, 0f));

            Assert.That(force.z, Is.LessThan(-0.5f),           "Topspin must produce downward Z force");
            Assert.That(Mathf.Abs(force.x), Is.LessThan(0.1f), "No significant X component");
            Assert.That(Mathf.Abs(force.y), Is.LessThan(0.1f), "No significant Y component");
        }

        // ── UT-MAG-005 ────────────────────────────────────────────────────────────

        [Test]
        public void MagnusForce_HighSpinParameter_ClampsLiftCoefficient()
        {
            // S = r*omega/v = 0.11*50/5 = 1.1 > MaxSpinParameter(1.0) → clamp must not produce NaN/Inf.
            Vector3 force = BallPhysicsCore.CalculateMagnusForce(
                velocity:        new Vector3(5f, 0f, 0f),
                angularVelocity: new Vector3(0f, 0f, 50f));

            Assert.IsFalse(float.IsNaN(force.x) || float.IsNaN(force.y) || float.IsNaN(force.z),
                "No NaN components when spin parameter is clamped");
            Assert.IsFalse(float.IsInfinity(force.magnitude),
                "No infinite magnitude when spin parameter is clamped");
            Assert.That(force.magnitude, Is.GreaterThan(0f),
                "Non-zero finite force confirms clamping succeeded");
        }

        // ── UT-DRG-002 ────────────────────────────────────────────────────────────

        [Test]
        public void DragForce_ScalesWithVelocitySquared()
        {
            // At v=10 m/s Cd=0.20 (laminar). At v=20 m/s crisis t=0 so Cd=0.20 also.
            // Ratio should be (20/10)^2 = 4 since same Cd at the crisis boundary.
            Vector3 slow = BallPhysicsCore.CalculateDragForce(new Vector3(10f, 0f, 0f));
            Vector3 fast = BallPhysicsCore.CalculateDragForce(new Vector3(20f, 0f, 0f));

            float ratio = fast.magnitude / slow.magnitude;

            Assert.That(ratio, Is.InRange(3.8f, 4.2f),
                $"Expected ~4× drag ratio (v² scaling at same Cd). Actual: {ratio:F3}");
        }

        // ── UT-DRG-003 ────────────────────────────────────────────────────────────

        [Test]
        public void DragForce_LowerAtTurbulentSpeed_ThanExpectedFromSquareAlone()
        {
            // At 15 m/s: laminar, Cd=0.20. At 30 m/s: turbulent, Cd=0.10.
            // Pure v² at same Cd → ratio 4. With Cd halved → ratio ≈ 0.5 × 4 = 2.0.
            Vector3 f15 = BallPhysicsCore.CalculateDragForce(new Vector3(15f, 0f, 0f));
            Vector3 f30 = BallPhysicsCore.CalculateDragForce(new Vector3(30f, 0f, 0f));

            float ratio = f30.magnitude / f15.magnitude;

            Assert.That(ratio, Is.InRange(1.8f, 2.2f),
                $"Expected ~2× ratio (Cd halved offsets v² quadrupling). Actual: {ratio:F3}");
        }

        // ── UT-BNC-002 ────────────────────────────────────────────────────────────

        [Test]
        public void Bounce_Topspin_KicksForward()
        {
            // ω_y < 0 = topspin; friction impulse transfers spin to linear forward velocity.
            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(5f, 0f, -3f),
                AngularVelocity   = new Vector3(0f, -10f, 0f),
                State             = BallStateType.BOUNCING,
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(5f, 0f, -3f)
            };

            BallGroundInteraction.ApplyBounce(ref ball, SurfaceType.GRASS_DRY, null, 0f);

            Assert.That(ball.Velocity.x, Is.GreaterThan(5.0f),
                "Topspin must increase forward velocity after bounce");
        }

        // ── UT-BNC-003 ────────────────────────────────────────────────────────────

        [Test]
        public void Bounce_Backspin_Bites()
        {
            // ω_y > 0 = backspin; friction impulse opposes forward motion.
            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(8f, 0f, -3f),
                AngularVelocity   = new Vector3(0f, 10f, 0f),
                State             = BallStateType.BOUNCING,
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(8f, 0f, -3f)
            };

            BallGroundInteraction.ApplyBounce(ref ball, SurfaceType.GRASS_DRY, null, 0f);

            Assert.That(ball.Velocity.x, Is.LessThan(8.0f),
                "Backspin must reduce forward velocity after bounce");
        }

        // ── UT-BNC-004 ────────────────────────────────────────────────────────────

        [Test]
        public void Bounce_PositionClampedToGroundLevel()
        {
            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(5f, 0f, -4f),
                AngularVelocity   = Vector3.zero,
                State             = BallStateType.BOUNCING,
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(5f, 0f, -4f)
            };

            BallGroundInteraction.ApplyBounce(ref ball, SurfaceType.GRASS_DRY, null, 0f);

            Assert.AreEqual(BallPhysicsConstants.Ball.RADIUS, ball.Position.z,
                "ApplyBounce must clamp position.z to Ball.RADIUS exactly");
        }

        // ── UT-BNC-005 ────────────────────────────────────────────────────────────

        [Test]
        public void Bounce_SpinRetentionApplied()
        {
            // SpinRetention for GRASS_DRY = 0.80. Friction also modifies omega slightly,
            // so tolerance is ±0.5 rad/s around the raw-retention value of 8.0.
            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(0f, 0f, -4f),
                AngularVelocity   = new Vector3(0f, 10f, 0f),
                State             = BallStateType.BOUNCING,
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(0f, 0f, -4f)
            };

            BallGroundInteraction.ApplyBounce(ref ball, SurfaceType.GRASS_DRY, null, 0f);

            float expectedRetained = 10f * BallPhysicsConstants.SurfaceSpinRetention.GrassDry;
            Assert.That(ball.AngularVelocity.magnitude,
                Is.InRange(expectedRetained - 0.5f, expectedRetained + 0.5f),
                $"Spin magnitude after bounce should be ~{expectedRetained:F1} rad/s (retention={BallPhysicsConstants.SurfaceSpinRetention.GrassDry})");
        }

        // ── UT-BNC-006 ────────────────────────────────────────────────────────────

        [Test]
        public void Bounce_WetGrass_HigherRebound_ThanDryGrass()
        {
            // Wet CoR=0.70 > dry CoR=0.65 → higher post-bounce vz.
            float groundLevel = BallPhysicsConstants.Ball.RADIUS;
            var ballDry = new BallState
            {
                Position          = new Vector3(50f, 34f, groundLevel),
                Velocity          = new Vector3(0f, 0f, -6.26f),
                AngularVelocity   = Vector3.zero,
                State             = BallStateType.BOUNCING,
                LastValidPosition = new Vector3(50f, 34f, groundLevel),
                LastValidVelocity = new Vector3(0f, 0f, -6.26f)
            };

            BallGroundInteraction.ApplyBounce(ref ballDry, SurfaceType.GRASS_DRY, null, 0f);
            float reboundDry = ballDry.Velocity.z;

            var ballWet = new BallState
            {
                Position          = new Vector3(50f, 34f, groundLevel),
                Velocity          = new Vector3(0f, 0f, -6.26f),
                AngularVelocity   = Vector3.zero,
                State             = BallStateType.BOUNCING,
                LastValidPosition = new Vector3(50f, 34f, groundLevel),
                LastValidVelocity = new Vector3(0f, 0f, -6.26f)
            };

            BallGroundInteraction.ApplyBounce(ref ballWet, SurfaceType.GRASS_WET, null, 0f);
            float reboundWet = ballWet.Velocity.z;

            Assert.That(reboundWet, Is.GreaterThan(reboundDry),
                "Wet grass CoR=0.70 should produce higher rebound than dry grass CoR=0.65");
        }

        // ── UT-VAL-002 ────────────────────────────────────────────────────────────

        [Test]
        public void Validation_DetectsInfinity_AndRecovers()
        {
            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(float.PositiveInfinity, 0f, 0f),
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = Vector3.zero,
                State             = BallStateType.ROLLING
            };

            BallPhysicsCore.ValidatePhysicsState(ref ball);

            Assert.IsFalse(float.IsInfinity(ball.Velocity.x),
                "ValidatePhysicsState must eliminate infinite velocity");
            Assert.AreEqual(BallStateType.STATIONARY, ball.State);
        }

        // ── UT-VAL-004 ────────────────────────────────────────────────────────────

        [Test]
        public void Validation_ClampsExcessiveSpin()
        {
            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = Vector3.zero,
                AngularVelocity   = new Vector3(0f, 100f, 0f),
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = Vector3.zero,
                State             = BallStateType.ROLLING
            };

            BallPhysicsCore.ValidatePhysicsState(ref ball);

            Assert.That(ball.AngularVelocity.magnitude,
                Is.LessThanOrEqualTo(BallPhysicsConstants.Limits.MaxSpin),
                $"AngularVelocity must be clamped to MaxSpin={BallPhysicsConstants.Limits.MaxSpin}");
        }

        // ── UT-SPN-002 ────────────────────────────────────────────────────────────

        [Test]
        public void SpinDecay_FasterAtHigherVelocity()
        {
            // totalDecay = DecayVelocityFactor*v + DecaySpinFactor*spinRate
            // Higher ball velocity → larger totalDecay → more spin lost per frame.
            Vector3 initial = new Vector3(0f, 0f, 20f);
            float   dt      = 1f / 60f;

            Vector3 afterLow  = BallPhysicsCore.UpdateSpinDecay(initial, new Vector3(5f, 0f, 0f), dt);
            Vector3 afterHigh = BallPhysicsCore.UpdateSpinDecay(initial, new Vector3(15f, 0f, 0f), dt);

            float decayAtLow  = initial.magnitude - afterLow.magnitude;
            float decayAtHigh = initial.magnitude - afterHigh.magnitude;

            Assert.That(decayAtHigh, Is.GreaterThan(decayAtLow),
                "Higher ball velocity must produce greater spin decay per frame");
        }

        // ── UT-SPN-003 ────────────────────────────────────────────────────────────

        [Test]
        public void SpinDecay_ZeroBelowThreshold()
        {
            // Spin magnitude 0.05 < MinSpin(0.1) → must return zero immediately.
            Vector3 result = BallPhysicsCore.UpdateSpinDecay(
                angularVelocity: new Vector3(0f, 0.05f, 0f),
                velocity:        new Vector3(5f, 0f, 0f),
                dt:              1f / 60f);

            Assert.AreEqual(Vector3.zero, result,
                "Spin below MinSpin threshold must be zeroed");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Fix pass: namespace → TacticalDirector.BallPhysics.Tests; ALL_CAPS |
// |         |            |        | constant refs → PascalCase; file header per FR-CS-056/057.         |
// | 1.2     | 2026-06-01 | —      | Add 13 missing spec §5 unit tests: UT-MAG-004/005, UT-DRG-002/003, |
// |         |            |        | UT-BNC-002/003/004/005/006, UT-VAL-002/004, UT-SPN-002/003.        |
// |         |            |        | Bug fix: UT-MAG-003 comment corrected (ω parallel to v in +X).     |
#endregion
