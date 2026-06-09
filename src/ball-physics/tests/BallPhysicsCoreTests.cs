// File:     src/ball-physics/tests/BallPhysicsCoreTests.cs
// Created:  2026-05-24
// Modified: 2026-06-09 (AR-7 fix pass)
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
            Vector3 force = BallPhysicsCore.CalculateMagnusForce(
                velocity:        new Vector3(20f, 0f, 0f),
                angularVelocity: new Vector3(12f, 0f, 0f));

            Assert.That(force.magnitude, Is.LessThan(0.001f));
        }

        [Test]
        public void MagnusForce_Sidespin_CurvesLaterally()
        {
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
            UnityEngine.TestTools.LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(@"\[BallPhysics\] NaN/Infinity detected"));

            var ball = new BallState
            {
                Position          = new Vector3(float.NaN, 34f, 0f),
                Velocity          = new Vector3(10f, 0f, 0f),
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(5f, 0f, 0f),
                State             = BallStateType.Rolling
            };

            BallPhysicsCore.ValidatePhysicsState(ref ball);

            Assert.AreEqual(50f, ball.Position.x, "Should recover to last valid X");
            Assert.AreEqual(BallStateType.Stationary, ball.State);
        }

        [Test]
        public void Validation_ClampsExcessiveVelocity()
        {
            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(100f, 0f, 0f),
                State             = BallStateType.Rolling,
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
                State             = BallStateType.Rolling,
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
            float groundLevel = BallPhysicsConstants.Ball.RADIUS;

            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, groundLevel),
                Velocity          = new Vector3(0f, 0f, -6.26f),
                AngularVelocity   = Vector3.zero,
                State             = BallStateType.Bouncing,
                LastValidPosition = new Vector3(50f, 34f, groundLevel),
                LastValidVelocity = new Vector3(0f, 0f, -6.26f)
            };

            BallGroundInteraction.ApplyBounce(ref ball, SurfaceType.GrassDry, null, 0f);

            float reboundHeight = ball.Velocity.z * ball.Velocity.z
                                / (2f * BallPhysicsConstants.Environment.GRAVITY);

            Assert.That(reboundHeight, Is.GreaterThan(0.80f), "Rebound must reach at least 0.80m");
            Assert.That(reboundHeight, Is.LessThan(0.90f),    "Rebound must not exceed 0.90m");
        }

        // ── UT-MAG-004 ────────────────────────────────────────────────────────────

        [Test]
        public void MagnusForce_Topspin_ProducesDownwardForce()
        {
            // Topspin for +X motion is +Y angular velocity (forward-rolling convention:
            // contact-point velocity ω × (-r·ẑ) opposes v). ω̂ × v̂ = ŷ × x̂ = -ẑ → downward.
            // AR-7 M-2: the original (0, -10, 0) was backspin and produced UPWARD force.
            Vector3 force = BallPhysicsCore.CalculateMagnusForce(
                velocity:        new Vector3(20f, 0f, 0f),
                angularVelocity: new Vector3(0f, 10f, 0f));

            Assert.That(force.z, Is.LessThan(-0.5f),           "Topspin must produce downward Z force");
            Assert.That(Mathf.Abs(force.x), Is.LessThan(0.1f), "No significant X component");
            Assert.That(Mathf.Abs(force.y), Is.LessThan(0.1f), "No significant Y component");
        }

        // ── UT-MAG-005 ────────────────────────────────────────────────────────────

        [Test]
        public void MagnusForce_HighSpinParameter_ClampsLiftCoefficient()
        {
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
            // Forward kick requires the contact point to move BACKWARD: ω·r > v_x, i.e.
            // ω_y > 5/0.11 ≈ 45.5 rad/s. AR-7 M-2: the original (0, -10, 0) was backspin
            // (and sub-rolling magnitude) — friction decelerated the ball instead.
            // Expected with ω_y = 60: v_x ≈ 5.64 m/s (stick impulse, coupling divisor 2.5).
            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(5f, 0f, -3f),
                AngularVelocity   = new Vector3(0f, 60f, 0f),
                State             = BallStateType.Bouncing,
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(5f, 0f, -3f)
            };

            BallGroundInteraction.ApplyBounce(ref ball, SurfaceType.GrassDry, null, 0f);

            Assert.That(ball.Velocity.x, Is.GreaterThan(5.0f),
                "Topspin must increase forward velocity after bounce");
        }

        // ── UT-BNC-003 ────────────────────────────────────────────────────────────

        [Test]
        public void Bounce_Backspin_Bites()
        {
            // Backspin for +X motion is -Y angular velocity (AR-7 M-2: original (0, 10, 0)
            // was topspin). Contact slip 8 + 30·0.11 = 11.3 m/s forward → friction is
            // μ-capped; expected v_x ≈ 5.03 m/s.
            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(8f, 0f, -3f),
                AngularVelocity   = new Vector3(0f, -30f, 0f),
                State             = BallStateType.Bouncing,
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(8f, 0f, -3f)
            };

            BallGroundInteraction.ApplyBounce(ref ball, SurfaceType.GrassDry, null, 0f);

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
                State             = BallStateType.Bouncing,
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(5f, 0f, -4f)
            };

            BallGroundInteraction.ApplyBounce(ref ball, SurfaceType.GrassDry, null, 0f);

            Assert.AreEqual(BallPhysicsConstants.Ball.RADIUS, ball.Position.z,
                "ApplyBounce must clamp position.z to Ball.RADIUS exactly");
        }

        // ── UT-BNC-005 ────────────────────────────────────────────────────────────

        [Test]
        public void Bounce_SpinRetentionApplied()
        {
            // Spin about the ground normal (+Z) produces zero contact-point slip, so
            // friction torque does not touch it and the surface retention factor is the
            // only effect — isolating the quantity under test (AR-7 M-2: the original
            // (0, 10, 0) Y-axis spin also took an ~6 rad/s friction-impulse reduction,
            // making the 10 × 0.8 expectation unreachable).
            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(0f, 0f, -4f),
                AngularVelocity   = new Vector3(0f, 0f, 10f),
                State             = BallStateType.Bouncing,
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(0f, 0f, -4f)
            };

            BallGroundInteraction.ApplyBounce(ref ball, SurfaceType.GrassDry, null, 0f);

            float expectedRetained = 10f * BallPhysicsConstants.SurfaceSpinRetention.GrassDry;
            Assert.That(ball.AngularVelocity.magnitude,
                Is.InRange(expectedRetained - 0.5f, expectedRetained + 0.5f),
                $"Spin magnitude after bounce should be ~{expectedRetained:F1} rad/s (retention={BallPhysicsConstants.SurfaceSpinRetention.GrassDry})");
        }

        // ── UT-BNC-006 ────────────────────────────────────────────────────────────

        [Test]
        public void Bounce_WetGrass_HigherRebound_ThanDryGrass()
        {
            float groundLevel = BallPhysicsConstants.Ball.RADIUS;
            var ballDry = new BallState
            {
                Position          = new Vector3(50f, 34f, groundLevel),
                Velocity          = new Vector3(0f, 0f, -6.26f),
                AngularVelocity   = Vector3.zero,
                State             = BallStateType.Bouncing,
                LastValidPosition = new Vector3(50f, 34f, groundLevel),
                LastValidVelocity = new Vector3(0f, 0f, -6.26f)
            };

            BallGroundInteraction.ApplyBounce(ref ballDry, SurfaceType.GrassDry, null, 0f);
            float reboundDry = ballDry.Velocity.z;

            var ballWet = new BallState
            {
                Position          = new Vector3(50f, 34f, groundLevel),
                Velocity          = new Vector3(0f, 0f, -6.26f),
                AngularVelocity   = Vector3.zero,
                State             = BallStateType.Bouncing,
                LastValidPosition = new Vector3(50f, 34f, groundLevel),
                LastValidVelocity = new Vector3(0f, 0f, -6.26f)
            };

            BallGroundInteraction.ApplyBounce(ref ballWet, SurfaceType.GrassWet, null, 0f);
            float reboundWet = ballWet.Velocity.z;

            Assert.That(reboundWet, Is.GreaterThan(reboundDry),
                "Wet grass CoR=0.70 should produce higher rebound than dry grass CoR=0.65");
        }

        // ── UT-VAL-002 ────────────────────────────────────────────────────────────

        [Test]
        public void Validation_DetectsInfinity_AndRecovers()
        {
            UnityEngine.TestTools.LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(@"\[BallPhysics\] NaN/Infinity detected"));

            var ball = new BallState
            {
                Position          = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(float.PositiveInfinity, 0f, 0f),
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = Vector3.zero,
                State             = BallStateType.Rolling
            };

            BallPhysicsCore.ValidatePhysicsState(ref ball);

            Assert.IsFalse(float.IsInfinity(ball.Velocity.x),
                "ValidatePhysicsState must eliminate infinite velocity");
            Assert.AreEqual(BallStateType.Stationary, ball.State);
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
                State             = BallStateType.Rolling
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
// | 1.3     | 2026-06-02 | —      | AR-1 fixes. H-2: file header path corrected to src/ball-physics/.  |
// |         |            |        | M-4: BallStateType / SurfaceType enum members renamed PascalCase   |
// |         |            |        | across all test inputs and assertions.                             |
// | 1.4     | 2026-06-03 | —      | AR-2 M-2: LogAssert.Expect added to                                |
// |         |            |        | Validation_DetectsNaN_AndRecovers and                              |
// |         |            |        | Validation_DetectsInfinity_AndRecovers so the recovery-path        |
// |         |            |        | Debug.LogError emitted by ValidatePhysicsState does not fail the   |
// |         |            |        | Unity NUnit runner (parallels the AR-1 H-1 follow-on already       |
// |         |            |        | applied on the two integration-test counterparts).                 |
// | 1.5     | 2026-06-09 | —      | AR-7 M-2: spin-sign convention corrected in four tests. Topspin    |
// |         |            |        | for +X motion is +Y angular velocity; the suite had it inverted.   |
// |         |            |        | MagnusForce_Topspin: (0,-10,0)→(0,10,0). Bounce_Topspin: ω_y=60    |
// |         |            |        | (must exceed v/r≈45.5 for forward kick). Bounce_Backspin: ω_y=-30. |
// |         |            |        | Bounce_SpinRetentionApplied: spin moved to the +Z normal axis so   |
// |         |            |        | friction torque cannot contaminate the retention measurement.      |
// |         |            |        | Expectations verified by numerical mirror of the fixed physics.    |
#endregion
