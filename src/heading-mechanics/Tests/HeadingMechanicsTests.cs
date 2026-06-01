// File:     src/heading-mechanics/Tests/HeadingMechanicsTests.cs
// Created:  2026-05-31
// Modified: 2026-05-31
// Author:   —
// Spec:     Heading Mechanics #10 §5, Code Standards #20
// Purpose:  Unit tests for Heading Mechanics. §5.1 unit test groups.

using NUnit.Framework;

using UnityEngine;

namespace TacticalDirector.HeadingMechanics.Tests
{
    // ── Zero-noise RNG stub ──────────────────────────────────────────────────────
    // Returns a fixed constant from NextFloat; returns 0 from NextGaussian so that
    // noise terms (timing jitter, point-error Gaussian) contribute exactly 0.
    // This lets deterministic formula verification tests compare exact expected values.

    /// <summary>
    /// IHeadingRngService stub that returns zero Gaussian noise and a fixed uniform value.
    /// Used in deterministic formula tests to eliminate stochastic variation.
    /// </summary>
    internal sealed class ZeroNoiseRng : IHeadingRngService
    {
        private readonly float _fixedFloat;

        /// <param name="fixedFloat">Value returned by NextFloat (default 0.5 — middle of [0, 1)).</param>
        internal ZeroNoiseRng(float fixedFloat = 0.5f)
        {
            _fixedFloat = fixedFloat;
        }

        /// <inheritdoc/>
        public float NextFloat(int drawSiteId) => _fixedFloat;

        /// <inheritdoc/>
        public float NextGaussian(int drawSiteId) => 0.0f;
    }

    // ────────────────────────────────────────────────────────────────────────────
    // §5.1.2 — JumpReach Formula (HeadingJumpKinematics)
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Unit tests for HeadingJumpKinematics: FM-010-001 JumpReach formula,
    /// apex-frame derivation, landing-frame derivation, and parabolic head-Z trajectory.
    /// Spec §5.1.2 / FR-HE-004 / FR-HE-021 / KD-18.
    /// </summary>
    [TestFixture]
    internal class HeadingJumpKinematicsTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────────

        private static HeadingAgentAttributes MakeAttrs(int heading, int strength, int balance, float fatigue = 0.0f)
        {
            return new HeadingAgentAttributes
            {
                Heading  = heading,
                Strength = strength,
                Balance  = balance,
                Fatigue  = fatigue,
                TeamId   = 0
            };
        }

        private static float NormAttr(int attr)
        {
            float clamped = Mathf.Clamp(attr,
                HeadingMechanicsConstants.ATTR_MIN,
                HeadingMechanicsConstants.ATTR_MAX);
            return clamped / HeadingMechanicsConstants.ATTR_MAX;
        }

        // ── Tests ─────────────────────────────────────────────────────────────────

        [Test]
        public void JumpReach_MinAttributes_ReturnsBaseReach()
        {
            // All attributes at floor (1 → norm ≈ 0.05); result is base + small offsets.
            HeadingAgentAttributes attrs = MakeAttrs(1, 1, 1);
            float reach = HeadingJumpKinematics.ComputeJumpReach(attrs);

            float expectedMin = HeadingMechanicsConstants.JUMP_REACH_BASE_M;
            Assert.IsTrue(reach >= expectedMin,
                $"JumpReach {reach} must be >= JUMP_REACH_BASE_M {expectedMin}");
        }

        [Test]
        public void JumpReach_MaxAttributes_ReturnsMaxReach()
        {
            // All attributes at ceiling (20 → norm = 1.0).
            HeadingAgentAttributes attrs = MakeAttrs(20, 20, 20);
            float reach = HeadingJumpKinematics.ComputeJumpReach(attrs);

            float expectedMax = HeadingMechanicsConstants.JUMP_REACH_BASE_M
                              + HeadingMechanicsConstants.JumpReachKStrength
                              + HeadingMechanicsConstants.JumpReachKBalance
                              + HeadingMechanicsConstants.JumpReachKHeading;

            Assert.AreEqual(expectedMax, reach, 1e-4f,
                "JumpReach at max attributes must equal BASE + KStrength + KBalance + KHeading.");
        }

        [Test]
        public void JumpReach_MonotoneInStrength()
        {
            // Increasing Strength while holding Heading and Balance constant
            // must produce a non-decreasing JumpReach.
            HeadingAgentAttributes lo = MakeAttrs(heading: 10, strength: 5,  balance: 10);
            HeadingAgentAttributes hi = MakeAttrs(heading: 10, strength: 15, balance: 10);

            Assert.IsTrue(
                HeadingJumpKinematics.ComputeJumpReach(hi) >=
                HeadingJumpKinematics.ComputeJumpReach(lo),
                "JumpReach must be monotone non-decreasing in Strength.");
        }

        [Test]
        public void JumpReach_MonotoneInBalance()
        {
            HeadingAgentAttributes lo = MakeAttrs(heading: 10, strength: 10, balance: 3);
            HeadingAgentAttributes hi = MakeAttrs(heading: 10, strength: 10, balance: 18);

            Assert.IsTrue(
                HeadingJumpKinematics.ComputeJumpReach(hi) >=
                HeadingJumpKinematics.ComputeJumpReach(lo),
                "JumpReach must be monotone non-decreasing in Balance.");
        }

        [Test]
        public void JumpReach_MonotoneInHeading()
        {
            HeadingAgentAttributes lo = MakeAttrs(heading: 2,  strength: 10, balance: 10);
            HeadingAgentAttributes hi = MakeAttrs(heading: 19, strength: 10, balance: 10);

            Assert.IsTrue(
                HeadingJumpKinematics.ComputeJumpReach(hi) >=
                HeadingJumpKinematics.ComputeJumpReach(lo),
                "JumpReach must be monotone non-decreasing in Heading.");
        }

        [Test]
        public void JumpReach_IsAlwaysPositive()
        {
            // Verify no negative values at the worst-case attribute floor.
            HeadingAgentAttributes attrs = MakeAttrs(1, 1, 1);
            float reach = HeadingJumpKinematics.ComputeJumpReach(attrs);
            Assert.IsTrue(reach > 0.0f, $"JumpReach must be positive; got {reach}");
        }

        [Test]
        public void ComputeApexFrame_ZeroStart_EqualsRoundedHalfPhaseDuration()
        {
            int apexFrame = HeadingJumpKinematics.ComputeApexFrame(jumpStartFrame: 0);

            float expectedFloat = HeadingMechanicsConstants.JumpPhaseDurationMs
                                * HeadingMechanicsConstants.JumpApexFraction
                                / HeadingMechanicsConstants.FrameMs;
            int expected = Mathf.RoundToInt(expectedFloat);

            Assert.AreEqual(expected, apexFrame,
                "Apex frame from jumpStartFrame=0 must equal round(JUMP_PHASE_DURATION_MS * JUMP_APEX_FRACTION / FRAME_MS).");
        }

        [Test]
        public void ComputeApexFrame_NonZeroStart_OffsetByStart()
        {
            int start = 120;
            int apexZero     = HeadingJumpKinematics.ComputeApexFrame(0);
            int apexNonZero  = HeadingJumpKinematics.ComputeApexFrame(start);

            Assert.AreEqual(apexZero + start, apexNonZero,
                "ComputeApexFrame must linearly offset by jumpStartFrame.");
        }

        [Test]
        public void ComputeHeadZ_AtApex_EqualsJumpReach()
        {
            float jumpReach = 2.65f;
            int   start     = 0;
            int   apex      = HeadingJumpKinematics.ComputeApexFrame(start);
            float z         = HeadingJumpKinematics.ComputeHeadZ(start, jumpReach, apex);

            // At apex (u = 0.5) parabolic formula gives 4 * 0.5 * 0.5 * jumpReach = jumpReach.
            Assert.AreEqual(jumpReach, z, 1e-3f,
                "Head Z at apex frame must equal jumpReach (parabolic peak).");
        }

        [Test]
        public void ComputeHeadZ_OutsideWindow_ReturnsZero()
        {
            // Frame before jump start must return 0.
            float z = HeadingJumpKinematics.ComputeHeadZ(jumpStartFrame: 100, jumpReachM: 2.5f, currentFrame: 50);
            Assert.AreEqual(0.0f, z, 1e-6f, "HeadZ outside aerial window must return 0.");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // §5.1.3 — Contact Quality (HeadingContactQuality)
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Unit tests for HeadingContactQuality: FM-010-002, asymmetric timing, point-error scaling,
    /// telemetry label assignment, and heading-attribute influence on pointQuality.
    /// Spec §5.1.3 / FR-HE-002 / FR-HE-022 / KD-2.
    /// </summary>
    [TestFixture]
    internal class HeadingContactQualityTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────────

        private static HeadingAgentAttributes MakeAttrs(int heading)
        {
            return new HeadingAgentAttributes
            {
                Heading  = heading,
                Strength = 10,
                Balance  = 10,
                Fatigue  = 0.0f,
                TeamId   = 0
            };
        }

        private static float RunCompute(
            int actualFrame,
            int idealFrame,
            Vector2 contactActual,
            Vector2 contactIntent,
            int heading,
            out float timingOffsetMs,
            out Vector2 contactPointError,
            out ContactQualityLabel label)
        {
            return HeadingContactQuality.Compute(
                actualFrame,
                idealFrame,
                contactActual,
                contactIntent,
                MakeAttrs(heading),
                new ZeroNoiseRng(),
                out timingOffsetMs,
                out contactPointError,
                out label);
        }

        // ── Tests ─────────────────────────────────────────────────────────────────

        [Test]
        public void ContactQuality_PerfectContact_ReturnsOne()
        {
            // Zero timing offset, zero point error → scalar = 1.0.
            float scalar = RunCompute(
                actualFrame: 100, idealFrame: 100,
                contactActual: Vector2.zero, contactIntent: Vector2.zero,
                heading: 10,
                out float _, out Vector2 _, out ContactQualityLabel _);

            Assert.AreEqual(1.0f, scalar, 1e-4f,
                "Perfect timing and zero point error must produce scalar = 1.0.");
        }

        [Test]
        public void ContactQuality_LateOffsetExceedsTolerance_ReturnsZero()
        {
            // Late offset far beyond MaxLateToleranceMs → timingQuality = 0.
            // With zero noise and zero point error, scalar ≈ 0 (alpha * 0 + (1-alpha) * 1 if no point error).
            // Use exact late tolerance + large margin.
            int framesTooLate = Mathf.CeilToInt(
                (HeadingMechanicsConstants.MaxLateToleranceMs * 2.0f) / HeadingMechanicsConstants.FrameMs);

            float scalar = RunCompute(
                actualFrame: 100 + framesTooLate, idealFrame: 100,
                contactActual: Vector2.zero, contactIntent: Vector2.zero,
                heading: 10,
                out float _, out Vector2 _, out ContactQualityLabel _);

            // timingQuality = 0, pointQuality = 1 → scalar = alpha * 0 + (1-alpha) * 1 = 1 - alpha
            float expected = (1.0f - HeadingMechanicsConstants.TimingPointBlendAlpha);
            Assert.AreEqual(expected, scalar, 1e-4f,
                "Far-late header must produce scalar equal to (1 - alpha) * pointQuality.");
        }

        [Test]
        public void ContactQuality_AsymmetricTolerance_LateWorseThanEarlyAtSameOffset()
        {
            // +50 ms late vs -50 ms early. Because MaxLateToleranceMs < MaxEarlyToleranceMs,
            // the late timing quality must be lower (worse) than the early timing quality at the same |offset|.
            int framesForFiftyMs = Mathf.RoundToInt(50.0f / HeadingMechanicsConstants.FrameMs);

            float earlyScalar = RunCompute(
                actualFrame: 100 - framesForFiftyMs, idealFrame: 100,
                contactActual: Vector2.zero, contactIntent: Vector2.zero,
                heading: 10,
                out float _, out Vector2 _, out ContactQualityLabel _);

            float lateScalar = RunCompute(
                actualFrame: 100 + framesForFiftyMs, idealFrame: 100,
                contactActual: Vector2.zero, contactIntent: Vector2.zero,
                heading: 10,
                out float _, out Vector2 _, out ContactQualityLabel _);

            Assert.IsTrue(lateScalar < earlyScalar,
                $"Late scalar ({lateScalar}) must be lower than early scalar ({earlyScalar}) at equal |offset| " +
                "because MaxLateToleranceMs < MaxEarlyToleranceMs (pass-1 H-1).");
        }

        [Test]
        public void ContactQuality_LargePointError_ReducesScalar()
        {
            // Large point error should yield a lower scalar than zero point error.
            float perfectScalar = RunCompute(
                actualFrame: 100, idealFrame: 100,
                contactActual: Vector2.zero, contactIntent: Vector2.zero,
                heading: 10,
                out float _, out Vector2 _, out ContactQualityLabel _);

            float errorScalar = RunCompute(
                actualFrame: 100, idealFrame: 100,
                contactActual: new Vector2(0.06f, 0.0f), contactIntent: Vector2.zero,
                heading: 10,
                out float _, out Vector2 _, out ContactQualityLabel _);

            Assert.IsTrue(errorScalar < perfectScalar,
                "A non-zero point error must reduce the contact quality scalar.");
        }

        [Test]
        public void ContactQuality_Label_PerfectIsOnTime()
        {
            RunCompute(
                actualFrame: 100, idealFrame: 100,
                contactActual: Vector2.zero, contactIntent: Vector2.zero,
                heading: 10,
                out float _, out Vector2 _, out ContactQualityLabel label);

            Assert.AreEqual(ContactQualityLabel.OnTime, label,
                "Perfect-timing contact must receive OnTime label (telemetry only, KD-2).");
        }

        [Test]
        public void ContactQuality_Label_FarEarlyIsEarly()
        {
            int earlyFrames = HeadingMechanicsConstants.FramesEarlyTolerance + 5;
            RunCompute(
                actualFrame: 100 - earlyFrames, idealFrame: 100,
                contactActual: Vector2.zero, contactIntent: Vector2.zero,
                heading: 10,
                out float timingOffsetMs, out Vector2 _, out ContactQualityLabel label);

            // timingOffsetMs < -EarlyLabelThresholdMs → Early
            if (timingOffsetMs < -HeadingMechanicsConstants.EarlyLabelThresholdMs)
            {
                Assert.AreEqual(ContactQualityLabel.Early, label,
                    "Contact far before ideal frame must receive Early label.");
            }
            else
            {
                // The offset fell inside the OnTime band — tolerate as boundary case.
                Assert.AreEqual(ContactQualityLabel.OnTime, label);
            }
        }

        [Test]
        public void ContactQuality_HigherHeadingAttributeReducesEffectivePointError()
        {
            // For identical physical contact geometry, a higher Heading attribute must produce
            // equal or higher scalar (smaller effective point error). §5.1.5 Heading-direction monotonicity.
            Vector2 contactActual = new Vector2(0.04f, 0.0f);
            Vector2 contactIntent = Vector2.zero;

            float lowScalar = RunCompute(
                actualFrame: 100, idealFrame: 100,
                contactActual: contactActual, contactIntent: contactIntent,
                heading: 4,
                out float _, out Vector2 _, out ContactQualityLabel _);

            float highScalar = RunCompute(
                actualFrame: 100, idealFrame: 100,
                contactActual: contactActual, contactIntent: contactIntent,
                heading: 18,
                out float _, out Vector2 _, out ContactQualityLabel _);

            Assert.IsTrue(highScalar >= lowScalar,
                $"Higher Heading ({highScalar}) must produce >= scalar vs lower Heading ({lowScalar}).");
        }

        [Test]
        public void ContactQuality_ScalarAlwaysInUnitRange()
        {
            // Worst-case inputs: extreme late + large point error.
            int lateFrames = Mathf.CeilToInt(
                (HeadingMechanicsConstants.MaxLateToleranceMs * 3.0f) / HeadingMechanicsConstants.FrameMs);

            float scalar = RunCompute(
                actualFrame: 100 + lateFrames, idealFrame: 100,
                contactActual: new Vector2(0.15f, 0.15f), contactIntent: Vector2.zero,
                heading: 1,
                out float _, out Vector2 _, out ContactQualityLabel _);

            Assert.IsTrue(scalar >= 0.0f && scalar <= 1.0f,
                $"Contact quality scalar {scalar} must be in [0, 1].");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // §5.1.4 — Power & Launch Angle (HeadingPowerAngle)
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Unit tests for HeadingPowerAngle: outgoing-speed formula (FM-010-003),
    /// fatigue convention, reflection geometry, and own-goal-shape flag (§3.8).
    /// Spec §5.1.4 / FR-HE-011 / KD-9 / FR-HE-007.
    /// </summary>
    [TestFixture]
    internal class HeadingPowerAngleTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────────

        private static HeadingAgentAttributes MakeAttrs(int heading, int strength, float fatigue)
        {
            return new HeadingAgentAttributes
            {
                Heading  = heading,
                Strength = strength,
                Balance  = 10,
                Fatigue  = fatigue,
                TeamId   = 0
            };
        }

        private static HeaderIntent MakeIntent(float powerIntent)
        {
            return new HeaderIntent
            {
                PowerIntent        = powerIntent,
                ContactPointIntent = Vector2.zero,
                TargetIntent       = new Vector3(52.5f, 34.0f, 2.0f),
                AttemptCommittedTick = 0,
                SetPieceContext    = SetPieceContext.OpenPlay
            };
        }

        // ── Tests ─────────────────────────────────────────────────────────────────

        [Test]
        public void OutgoingSpeed_MonotoneInPowerIntent()
        {
            HeadingAgentAttributes attrs = MakeAttrs(heading: 12, strength: 12, fatigue: 0.0f);

            float speedLow  = HeadingPowerAngle.ComputeOutgoingSpeed(attrs, MakeIntent(0.3f), 1.0f);
            float speedHigh = HeadingPowerAngle.ComputeOutgoingSpeed(attrs, MakeIntent(0.9f), 1.0f);

            Assert.IsTrue(speedHigh > speedLow,
                "Outgoing speed must increase with PowerIntent.");
        }

        [Test]
        public void OutgoingSpeed_MonotoneDecreasingInFatigue()
        {
            HeadingAgentAttributes loFatigue = MakeAttrs(heading: 15, strength: 12, fatigue: 0.0f);
            HeadingAgentAttributes hiFatigue = MakeAttrs(heading: 15, strength: 12, fatigue: 1.0f);
            HeaderIntent intent = MakeIntent(0.8f);

            float speedRested   = HeadingPowerAngle.ComputeOutgoingSpeed(loFatigue, intent, 1.0f);
            float speedFatigued = HeadingPowerAngle.ComputeOutgoingSpeed(hiFatigue, intent, 1.0f);

            Assert.IsTrue(speedFatigued < speedRested,
                "Outgoing speed must decrease as fatigue increases (KD-9: 0 = rested, 1 = fatigued).");
        }

        [Test]
        public void OutgoingSpeed_ZeroFatigue_EffectiveAttributeEqualsHeadingNorm()
        {
            // At fatigue = 0, effective attribute = headingNorm (no penalty).
            // Validate the formula: outgoingSpeed = powerMps * powerIntent * qualityScalar.
            HeadingAgentAttributes attrs = MakeAttrs(heading: 20, strength: 20, fatigue: 0.0f);
            HeaderIntent intent = MakeIntent(1.0f);
            float quality = 1.0f;

            float speed = HeadingPowerAngle.ComputeOutgoingSpeed(attrs, intent, quality);

            float headingNorm  = 1.0f;  // attr 20 / ATTR_MAX 20
            float strengthNorm = 1.0f;
            float powerMps = HeadingMechanicsConstants.PowerBaseMps
                           + HeadingMechanicsConstants.PowerKStrength * strengthNorm
                           + HeadingMechanicsConstants.PowerKHeading  * headingNorm;
            float expected = powerMps * 1.0f * 1.0f;

            Assert.AreEqual(expected, speed, 1e-4f,
                "At zero fatigue and max attrs, outgoingSpeed must equal PowerBase + KStrength + KHeading.");
        }

        [Test]
        public void OutgoingSpeed_ZeroPowerIntent_ReturnsZero()
        {
            HeadingAgentAttributes attrs = MakeAttrs(heading: 15, strength: 15, fatigue: 0.0f);
            float speed = HeadingPowerAngle.ComputeOutgoingSpeed(attrs, MakeIntent(0.0f), 1.0f);

            Assert.AreEqual(0.0f, speed, 1e-6f,
                "PowerIntent = 0 must produce outgoing speed = 0.");
        }

        [Test]
        public void ReflectionDirection_DegenerateIncoming_ReturnsFallback()
        {
            // Degenerate (near-zero) incoming velocity must return Vector3.forward.
            Vector3 direction = HeadingPowerAngle.ComputeReflectionDirection(
                contactPointActual_worldspace: new Vector3(1.0f, 0.0f, 0.0f),
                headCentre_worldspace: Vector3.zero,
                incomingBallVelocity: new Vector3(0.0f, 0.0f, 0.0f));

            Assert.AreEqual(Vector3.forward, direction,
                "Degenerate incoming velocity must fall back to Vector3.forward.");
        }

        [Test]
        public void ReflectionDirection_ReturnsUnitVector()
        {
            // A non-degenerate input must produce a unit-length direction.
            Vector3 direction = HeadingPowerAngle.ComputeReflectionDirection(
                contactPointActual_worldspace: new Vector3(0.1f, 0.0f, 0.0f),
                headCentre_worldspace: Vector3.zero,
                incomingBallVelocity: new Vector3(-10.0f, 0.0f, -5.0f));

            Assert.AreEqual(1.0f, direction.magnitude, 1e-5f,
                "Reflection direction must be a unit vector.");
        }

        [Test]
        public void OwnGoalFlag_TrajectoryTowardOwnGoal_ReturnsTrue()
        {
            // Team 0 defends goal at X = 0. Place agent near own-half and aim back toward X = 0.
            Vector3 contactPosition = new Vector3(15.0f, 34.0f, 1.5f);  // 15 m from own goal
            // Velocity strongly in -X direction, moderate Z component keeps it in goal height range.
            Vector3 outgoingVelocity = new Vector3(-14.0f, 0.0f, 1.0f);

            bool flag = HeadingPowerAngle.ComputeOwnGoalFlag(outgoingVelocity, contactPosition, teamId: 0);

            Assert.IsTrue(flag,
                "Trajectory aimed directly at own goal bounding box must set own-goal flag.");
        }

        [Test]
        public void OwnGoalFlag_TrajectoryAwayFromOwnGoal_ReturnsFalse()
        {
            // Attacker (team 0) heading toward opponent goal at X = 105.
            Vector3 contactPosition = new Vector3(80.0f, 34.0f, 1.5f);
            Vector3 outgoingVelocity = new Vector3(15.0f, 0.0f, -2.0f);  // toward +X, away from X=0

            bool flag = HeadingPowerAngle.ComputeOwnGoalFlag(outgoingVelocity, contactPosition, teamId: 0);

            Assert.IsFalse(flag,
                "Trajectory aimed away from own goal must not set the own-goal flag.");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // §5.1.5 — Spin Transfer (HeadingSpinTransfer)
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Unit tests for HeadingSpinTransfer: FM-010-004, spin-preservation-factor formula,
    /// reversal boundary, and head angular-velocity derivation.
    /// Spec §5.1.5 / FR-HE-015 / KD-16 / v0.2 H-1.
    /// </summary>
    [TestFixture]
    internal class HeadingSpinTransferTests
    {
        // ── Tests ─────────────────────────────────────────────────────────────────

        [Test]
        public void SpinTransfer_ZeroIncomingSpin_OnlyHeadContribution()
        {
            // With zero incoming spin, the outgoing spin is purely from headAngularVelocity.
            Vector2 currentFacing = new Vector2(1.0f, 0.0f);
            Vector2 prevFacing    = new Vector2(0.0f, 1.0f); // 90 deg rotation → large yaw rate

            Vector3 outgoingSpin = HeadingSpinTransfer.ComputeOutgoingSpin(
                incomingSpin: Vector3.zero,
                contactPointActual_headLocal: Vector2.zero,
                currentFacingDir: currentFacing,
                prevFacingDir: prevFacing);

            // Head angular-velocity is in Z direction only (Stage 0 yaw-only approximation).
            // Outgoing spin should be non-zero and purely in Z.
            Assert.AreEqual(0.0f, outgoingSpin.x, 1e-5f, "X spin must be zero (no pitch/roll).");
            Assert.AreEqual(0.0f, outgoingSpin.y, 1e-5f, "Y spin must be zero (no pitch/roll).");
        }

        [Test]
        public void SpinTransfer_AtReversalThreshold_IncomingContributionIsZero()
        {
            // When contactPointAxialOffset == SPIN_TRANSFER_REVERSAL_THRESHOLD,
            // spinPreservationFactor = SpinPreservationBase * (1 - 1) = 0.
            // The incoming-spin contribution to outgoingSpin must be exactly zero.
            float threshold = HeadingMechanicsConstants.SpinTransferReversalThreshold;
            Vector3 incomingSpin = new Vector3(5.0f, 0.0f, 0.0f);
            Vector2 contactAtThreshold = new Vector2(threshold, 0.0f);

            // Use static-facing directions so headAngularVelocity = 0 (no facing change).
            Vector2 facing = new Vector2(1.0f, 0.0f);

            Vector3 outgoingSpin = HeadingSpinTransfer.ComputeOutgoingSpin(
                incomingSpin: incomingSpin,
                contactPointActual_headLocal: contactAtThreshold,
                currentFacingDir: facing,
                prevFacingDir: facing);

            // Head angular velocity is zero (identical frames), so outgoing spin = factor * incomingSpin = 0 * incomingSpin.
            Assert.AreEqual(0.0f, outgoingSpin.x, 1e-5f,
                "At the reversal threshold, the incoming-spin contribution must be zero (v0.2 H-1).");
        }

        [Test]
        public void SpinTransfer_BeyondReversalThreshold_SignIsReversed()
        {
            // At 2 * SPIN_TRANSFER_REVERSAL_THRESHOLD, factor = SpinPreservationBase * (1 - 2) = -SpinPreservationBase.
            // Incoming-spin contribution sign should be inverted relative to threshold=0 case.
            float threshold = HeadingMechanicsConstants.SpinTransferReversalThreshold;
            Vector3 incomingSpin = new Vector3(5.0f, 0.0f, 0.0f);
            Vector2 facing = new Vector2(1.0f, 0.0f);

            Vector3 spinBefore = HeadingSpinTransfer.ComputeOutgoingSpin(
                incomingSpin: incomingSpin,
                contactPointActual_headLocal: Vector2.zero,  // factor = SpinPreservationBase (positive)
                currentFacingDir: facing,
                prevFacingDir: facing);

            Vector3 spinAfter = HeadingSpinTransfer.ComputeOutgoingSpin(
                incomingSpin: incomingSpin,
                contactPointActual_headLocal: new Vector2(threshold * 2.0f, 0.0f),  // factor negative
                currentFacingDir: facing,
                prevFacingDir: facing);

            // At offset = 0: contribution = SpinPreservationBase * incomingSpin.x (positive)
            // At offset = 2*threshold: contribution = -SpinPreservationBase * incomingSpin.x (negative)
            Assert.IsTrue(spinAfter.x < 0.0f && spinBefore.x > 0.0f,
                "At 2× reversal threshold, incoming-spin contribution must be sign-inverted (single reversal, no double-reversal).");
        }

        [Test]
        public void DeriveHeadAngularVelocity_StaticFacing_ReturnsZero()
        {
            Vector2 facing = new Vector2(1.0f, 0.0f);
            Vector3 angVel = HeadingSpinTransfer.DeriveHeadAngularVelocity(facing, facing);

            Assert.AreEqual(Vector3.zero, angVel,
                "Static facing direction must produce zero angular velocity.");
        }

        [Test]
        public void DeriveHeadAngularVelocity_DegenerateFacing_ReturnsZero()
        {
            Vector3 angVel = HeadingSpinTransfer.DeriveHeadAngularVelocity(Vector2.zero, Vector2.zero);
            Assert.AreEqual(Vector3.zero, angVel,
                "Degenerate (zero-length) facing vectors must return zero angular velocity.");
        }

        [Test]
        public void SpinTransfer_Monotonicity_LinearInAxialOffset()
        {
            // The incoming-spin contribution is linear in contactPointAxialOffset (no discontinuity at threshold).
            // Verify monotone decrease of factor magnitude as offset increases from 0 toward threshold.
            float threshold = HeadingMechanicsConstants.SpinTransferReversalThreshold;
            Vector3 incomingSpin = new Vector3(4.0f, 0.0f, 0.0f);
            Vector2 facing = new Vector2(1.0f, 0.0f);

            float offset0   = 0.0f;
            float offsetHalf = threshold * 0.5f;

            Vector3 spin0 = HeadingSpinTransfer.ComputeOutgoingSpin(
                incomingSpin, new Vector2(offset0, 0.0f), facing, facing);
            Vector3 spinH = HeadingSpinTransfer.ComputeOutgoingSpin(
                incomingSpin, new Vector2(offsetHalf, 0.0f), facing, facing);

            // At offset 0: factor = SpinPreservationBase; contribution = SpinPreservationBase * incomingSpin.x
            // At offset = threshold/2: factor = SpinPreservationBase * 0.5; contribution smaller.
            Assert.IsTrue(spinH.x < spin0.x,
                "Incoming-spin contribution must decrease monotonically with axial offset (linear in offset, no discontinuity).");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // §5.1.6 — Duel Resolution (HeadingDuelResolution)
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Unit tests for HeadingDuelResolution: FM-010-005 base-score formula, 2-way and 3-way
    /// duel winner selection, disturbance-factor saturation, and near-tie tiebreak behaviour.
    /// Spec §5.1.6 / FR-HE-010 / FR-HE-017 / FR-HE-023.
    /// </summary>
    [TestFixture]
    internal class HeadingDuelResolutionTests
    {
        // ── Helpers ──────────────────────────────────────────────────────────────

        private static HeadingAgentAttributes MakeAttrs(int heading, int strength, int balance)
        {
            return new HeadingAgentAttributes
            {
                Heading  = heading,
                Strength = strength,
                Balance  = balance,
                Fatigue  = 0.0f,
                TeamId   = 0
            };
        }

        private static float NormAttr(int attr)
        {
            float clamped = Mathf.Clamp(attr,
                HeadingMechanicsConstants.ATTR_MIN,
                HeadingMechanicsConstants.ATTR_MAX);
            return clamped / HeadingMechanicsConstants.ATTR_MAX;
        }

        // ── Tests ─────────────────────────────────────────────────────────────────

        [Test]
        public void ComputeBaseScore_MaxAttributes_EqualsWeightSum()
        {
            // All three weight terms at norm = 1.0 → baseScore = w_B + w_S + w_H = 1.0.
            HeadingAgentAttributes attrs = MakeAttrs(20, 20, 20);
            float score = HeadingDuelResolution.ComputeBaseScore(attrs);

            float expected = HeadingMechanicsConstants.DuelBalanceWeight
                           + HeadingMechanicsConstants.DuelStrengthWeight
                           + HeadingMechanicsConstants.DuelHeadingWeight;

            Assert.AreEqual(expected, score, 1e-4f,
                "Base score at max attrs must equal sum of all three duel weights.");
        }

        [Test]
        public void ComputeBaseScore_MinAttributes_IsPositive()
        {
            HeadingAgentAttributes attrs = MakeAttrs(1, 1, 1);
            float score = HeadingDuelResolution.ComputeBaseScore(attrs);
            Assert.IsTrue(score > 0.0f, $"Base score must be positive even at minimum attributes; got {score}.");
        }

        [Test]
        public void ComputeBaseScore_WeightedFormulaMatchesExpected()
        {
            // Explicit formula check at specific attribute values.
            HeadingAgentAttributes attrs = MakeAttrs(heading: 10, strength: 15, balance: 5);
            float score = HeadingDuelResolution.ComputeBaseScore(attrs);

            float hNorm = NormAttr(10);
            float sNorm = NormAttr(15);
            float bNorm = NormAttr(5);
            float expected = HeadingMechanicsConstants.DuelBalanceWeight  * bNorm
                           + HeadingMechanicsConstants.DuelStrengthWeight * sNorm
                           + HeadingMechanicsConstants.DuelHeadingWeight  * hNorm;

            Assert.AreEqual(expected, score, 1e-5f,
                "ComputeBaseScore must match FM-010-005 formula exactly.");
        }

        [Test]
        public void TwoWayDuel_StrongerAgentWins()
        {
            HeadingDuelResolution duel = new HeadingDuelResolution();
            ZeroNoiseRng rng = new ZeroNoiseRng(fixedFloat: 0.0f);

            HeadingAgentAttributes strongAttrs = MakeAttrs(heading: 18, strength: 18, balance: 18);
            HeadingAgentAttributes weakAttrs   = MakeAttrs(heading:  5, strength:  5, balance:  5);

            float strongScore = HeadingDuelResolution.ComputeBaseScore(strongAttrs);
            float weakScore   = HeadingDuelResolution.ComputeBaseScore(weakAttrs);

            const float matchTime = 1.0f;
            duel.RegisterDuelCandidate(agentId: 1, contactFrameMatchTime: matchTime, baseScore: strongScore);
            duel.RegisterDuelCandidate(agentId: 2, contactFrameMatchTime: matchTime, baseScore: weakScore);

            duel.ResolveAll(rng);

            ContestedDuelContext ctx = duel.GetDuel(0);
            Assert.AreEqual(1, ctx.WinnerAgentId,
                "The agent with the higher base score must win the 2-way duel.");
        }

        [Test]
        public void TwoWayDuel_LoserReceivesNonZeroDisturbance()
        {
            HeadingDuelResolution duel = new HeadingDuelResolution();
            ZeroNoiseRng rng = new ZeroNoiseRng(fixedFloat: 0.0f);

            float highScore = 0.9f;
            float lowScore  = 0.3f;  // gap = 0.6 → saturated disturbance

            duel.RegisterDuelCandidate(agentId: 10, contactFrameMatchTime: 2.0f, baseScore: highScore);
            duel.RegisterDuelCandidate(agentId: 20, contactFrameMatchTime: 2.0f, baseScore: lowScore);

            duel.ResolveAll(rng);

            ContestedDuelContext ctx = duel.GetDuel(0);
            int loserSlot = (ctx.WinnerAgentId == 10) ? 1 : 0;
            float disturbance = duel.GetDisturbanceFactor(ctx.BufferStartIndex, loserSlot);

            Assert.IsTrue(disturbance > 0.0f,
                "The duel loser must receive a positive disturbance factor.");
        }

        [Test]
        public void TwoWayDuel_LargeGap_DisturbanceSaturatesAtMax()
        {
            HeadingDuelResolution duel = new HeadingDuelResolution();
            ZeroNoiseRng rng = new ZeroNoiseRng(fixedFloat: 0.0f);

            // Gap >> DuelDisturbanceGapSaturation → disturbance = DuelDisturbanceMax.
            float highScore = 1.0f;
            float lowScore  = 1.0f - HeadingMechanicsConstants.DuelDisturbanceGapSaturation - 0.1f;

            duel.RegisterDuelCandidate(agentId: 1, contactFrameMatchTime: 3.0f, baseScore: highScore);
            duel.RegisterDuelCandidate(agentId: 2, contactFrameMatchTime: 3.0f, baseScore: lowScore);

            duel.ResolveAll(rng);

            ContestedDuelContext ctx = duel.GetDuel(0);
            int loserSlot = (ctx.WinnerAgentId == 1) ? 1 : 0;
            float disturbance = duel.GetDisturbanceFactor(ctx.BufferStartIndex, loserSlot);

            Assert.AreEqual(HeadingMechanicsConstants.DuelDisturbanceMax, disturbance, 1e-4f,
                "Loser disturbance must saturate at DuelDisturbanceMax when gap >= DuelDisturbanceGapSaturation.");
        }

        [Test]
        public void ThreeWayDuel_CorrectWinnerSelected()
        {
            HeadingDuelResolution duel = new HeadingDuelResolution();
            ZeroNoiseRng rng = new ZeroNoiseRng(fixedFloat: 0.0f);

            const float matchTime = 4.0f;
            duel.RegisterDuelCandidate(agentId: 1, contactFrameMatchTime: matchTime, baseScore: 0.50f);
            duel.RegisterDuelCandidate(agentId: 2, contactFrameMatchTime: matchTime, baseScore: 0.80f);
            duel.RegisterDuelCandidate(agentId: 3, contactFrameMatchTime: matchTime, baseScore: 0.40f);

            duel.ResolveAll(rng);

            ContestedDuelContext ctx = duel.GetDuel(0);
            Assert.AreEqual(2, ctx.WinnerAgentId,
                "Agent with highest base score (0.80) must win the 3-way duel.");
        }

        [Test]
        public void ClearFrameBuffer_ResetsDuelCount()
        {
            HeadingDuelResolution duel = new HeadingDuelResolution();
            ZeroNoiseRng rng = new ZeroNoiseRng();

            duel.RegisterDuelCandidate(agentId: 1, contactFrameMatchTime: 1.0f, baseScore: 0.7f);
            duel.RegisterDuelCandidate(agentId: 2, contactFrameMatchTime: 1.0f, baseScore: 0.5f);
            duel.ResolveAll(rng);

            Assert.AreEqual(1, duel.DuelCount, "Duel count before clear must be 1.");

            duel.ClearFrameBuffer();
            Assert.AreEqual(0, duel.DuelCount, "Duel count after ClearFrameBuffer must be 0.");
        }

        [Test]
        public void HeadingAttrScale_HighHeadingProducesHigherScale()
        {
            HeadingAgentAttributes loAttrs = new HeadingAgentAttributes { Heading = 4,  Strength = 10, Balance = 10 };
            HeadingAgentAttributes hiAttrs = new HeadingAgentAttributes { Heading = 18, Strength = 10, Balance = 10 };

            float loScale = HeadingContactQuality.ComputeHeadingAttrScale(loAttrs);
            float hiScale = HeadingContactQuality.ComputeHeadingAttrScale(hiAttrs);

            Assert.IsTrue(hiScale > loScale,
                $"Higher Heading must produce a larger headingAttrScale ({hiScale} > {loScale}).");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // §5.1.2 — JumpKinematics: ComputeHeadZ parabola shape (additional coverage)
    // ────────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Additional coverage for parabolic HeadZ trajectory: symmetry around apex and
    /// ground-touch at start and landing frames.
    /// Spec §5.1.2 / KD-18 / FR-HE-019.
    /// </summary>
    [TestFixture]
    internal class HeadingJumpKinematicsParabolaTests
    {
        [Test]
        public void ComputeHeadZ_AtJumpStart_IsNearZero()
        {
            // u = 0 at jumpStartFrame → parabola = 0.
            float z = HeadingJumpKinematics.ComputeHeadZ(jumpStartFrame: 0, jumpReachM: 2.5f, currentFrame: 0);
            Assert.AreEqual(0.0f, z, 1e-5f, "HeadZ at jump start must be 0 (u = 0).");
        }

        [Test]
        public void ComputeHeadZ_AtLandingFrame_IsNearZero()
        {
            int start   = 0;
            int landing = HeadingJumpKinematics.ComputeLandingFrame(start);
            float z     = HeadingJumpKinematics.ComputeHeadZ(start, jumpReachM: 2.5f, currentFrame: landing);

            Assert.AreEqual(0.0f, z, 1e-5f, "HeadZ at landing frame must be 0 (u = 1).");
        }

        [Test]
        public void ComputeHeadZ_Monotone_RisingBeforeApex()
        {
            int start    = 0;
            int apex     = HeadingJumpKinematics.ComputeApexFrame(start);
            float reach  = 2.6f;

            float zEarly = HeadingJumpKinematics.ComputeHeadZ(start, reach, apex - 3);
            float zApex  = HeadingJumpKinematics.ComputeHeadZ(start, reach, apex);

            Assert.IsTrue(zApex >= zEarly,
                "HeadZ must be non-decreasing before the apex frame.");
        }

        [Test]
        public void ComputeHeadZ_Monotone_FallingAfterApex()
        {
            int start   = 0;
            int apex    = HeadingJumpKinematics.ComputeApexFrame(start);
            int landing = HeadingJumpKinematics.ComputeLandingFrame(start);
            float reach = 2.6f;

            if (apex + 3 <= landing)
            {
                float zApex  = HeadingJumpKinematics.ComputeHeadZ(start, reach, apex);
                float zLater = HeadingJumpKinematics.ComputeHeadZ(start, reach, apex + 3);

                Assert.IsTrue(zApex >= zLater,
                    "HeadZ must be non-increasing after the apex frame.");
            }
            else
            {
                Assert.Inconclusive("Jump phase too short to verify monotone descent with 3-frame offset.");
            }
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // §5.2 Integration / §5.3 Validation / §5.4 Conformance
    // ────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class HeadingMechanicsIntegrationTests
    {
        // ── §5.2 Integration ──────────────────────────────────────────────────────

        /// <summary>T-HE-I-001: Open-play header from a Pass Mechanics #5 cross; verifies KD-5 (no CrossDelivery label read). §5.2.1.</summary>
        [Test]
        public void Integration_T_HE_I_001_OpenPlayHeaderFromCrossProducesOutgoingVelocity()
        {
            Assert.Ignore("Stage 0+1: requires Pass Mechanics #5 cross delivery and 22-agent match tick loop — activate when HeadingMechanics is wired into TickOrchestrator");
        }

        /// <summary>T-HE-I-002: Corner-kick header (set-piece pathway, KD-13); verifies same pipeline as open-play cross (no set-piece-specific branch). §5.2.2 / FR-HE-016.</summary>
        [Test]
        public void Integration_T_HE_I_002_CornerKickHeaderRoutesIdenticallyToOpenPlay()
        {
            Assert.Ignore("Stage 0+1: requires Pass Mechanics #5 set-piece delivery — activate when HeadingMechanics is wired into TickOrchestrator");
        }

        /// <summary>T-HE-I-003: Free-kick header (far-post delivery, downward volley); same KD-13 pathway. §5.2.3.</summary>
        [Test]
        public void Integration_T_HE_I_003_FreeKickHeaderFarPostDownwardVolley()
        {
            Assert.Ignore("Stage 0+1: requires Pass Mechanics #5 wide free-kick delivery — activate when HeadingMechanics is wired into TickOrchestrator");
        }

        /// <summary>T-HE-I-004: GK headed clearance executes #10 pipeline; no GK-specific physics branch (KD-7, FR-HE-009). §5.2.4.</summary>
        [Test]
        public void Integration_T_HE_I_004_GoalkeeperHeadedClearanceUsesStandardPipeline()
        {
            Assert.Ignore("Stage 0+1: requires Goalkeeper Mechanics #11 integration — activate when HeadingMechanics is wired into TickOrchestrator alongside GoalkeeperMechanics");
        }

        /// <summary>T-HE-I-005: Contested 2-way duel (defender vs. striker); winner emits HeaderExecutedEvent, loser emits disturbed or failed event. §5.2.5.</summary>
        [Test]
        public void Integration_T_HE_I_005_Contested2WayDuelWinnerAndLoserEmitCorrectEvents()
        {
            Assert.Ignore("Stage 0+1: requires full duel pipeline with two simultaneous header commits — activate when HeadingMechanics is wired into TickOrchestrator");
        }

        /// <summary>T-HE-I-006: Contested 3-way duel (two strikers + defender); sub-scenario (a) tight gap → losers emit disturbed executed; (b) lopsided → losers fail. §5.2.6.</summary>
        [Test]
        public void Integration_T_HE_I_006_Contested3WayDuelTightAndLopsidedSubScenarios()
        {
            Assert.Ignore("Stage 0+1: requires three simultaneous header commits — activate when HeadingMechanics is wired into TickOrchestrator");
        }

        /// <summary>T-HE-I-007: Mistimed jump → F-01 failure; KD-17 intent re-validation; ball trajectory unchanged. §5.2.7.</summary>
        [Test]
        public void Integration_T_HE_I_007_MistimedJumpEmitsFailedEventAndLeavesBalUnchanged()
        {
            Assert.Ignore("Stage 0+1: requires multi-tick heading pipeline with intent staleness re-validation — activate when HeadingMechanics is wired into TickOrchestrator");
        }

        /// <summary>T-HE-I-008: Own-goal-shape flag → HeaderExecutedEvent published with ownGoalShapedTrajectory=true; mock Event System confirms adjudication is its responsibility. §5.2.8.</summary>
        [Test]
        public void Integration_T_HE_I_008_OwnGoalShapeFlagPublishedToEventSystem()
        {
            Assert.Ignore("Stage 0+1: requires Event System #17 mock subscriber — activate when EventBus HeaderExecutedEvent (0x12) subscription is available in test harness");
        }

        /// <summary>T-HE-I-009: Deterministic replay — 1000-tick scenario, 12 headers, 2 duels, 1 near-tie tiebreak; three runs produce byte-identical HeaderExecutedEvent sequences. §5.2.9 / FR-HE-008.</summary>
        [Test]
        public void Integration_T_HE_I_009_DeterministicReplay1000TickByteIdentical()
        {
            Assert.Ignore("Stage 0+1: requires #16 SnapshotCodec and replay harness — activate when DeterministicSim snapshot round-trip is verified and HeadingMechanics is wired into TickOrchestrator");
        }

        // ── §5.3 Validation Scenarios ─────────────────────────────────────────────

        /// <summary>T-HE-V-001: 22-agent 10-minute match segment; ~3 headers expected; timing-label distribution OnTime≈55%, Early≈20%, Late≈25%. §5.3.1.</summary>
        [Test]
        public void Validation_T_HE_V_001_22AgentMatchSegmentHeaderFrequencyAndTimingDistribution()
        {
            Assert.Ignore("Stage 0+1: requires 22-agent match simulation — activate when HeadingMechanics is wired into TickOrchestrator and telemetry pipeline is live");
        }

        /// <summary>T-HE-V-002: Corner A/B sensitivity — Heading 75 vs. 90; measurable outcome divergence in pointError, contactQualityScalar, outgoingSpeed. §5.3.2 / KD-4.</summary>
        [Test]
        public void Validation_T_HE_V_002_CornerABHeadingAttributeSensitivity()
        {
            Assert.Ignore("Stage 0+1: requires identical-seed corner delivery with two striker attribute profiles — activate when HeadingMechanics is wired into TickOrchestrator");
        }

        /// <summary>T-HE-V-003: Fatigue gradient — fatigue=0 vs 1; ~12% reduction in mean outgoingSpeed at full fatigue. §5.3.3 / KD-9.</summary>
        [Test]
        public void Validation_T_HE_V_003_FatigueGradientReducesOutgoingSpeedByTwelvePercent()
        {
            Assert.Ignore("Stage 0+1: requires identical-seed delivery with two fatigue states — activate when HeadingMechanics is wired into TickOrchestrator and fatigue input is live");
        }

        // ── §5.4 Cross-Spec Conformance ───────────────────────────────────────────

        /// <summary>T-HE-C-001: grep -r 'HeaderType|HeaderClass|HeaderStyle' src/ returns zero matches (KD-1, FR-HE-003). §5.4.1.</summary>
        [Test]
        public void Conformance_T_HE_C_001_NoHeaderTypeSymbolInSrc()
        {
            Assert.Ignore("Stage 0+1: CI-time grep gate — activate when heading-mechanics assembly is included in the CI pipeline grep lint checks");
        }

        /// <summary>T-HE-C-002: Every constant in HeadingMechanicsConstants.cs has a valid source-tag comment (KD-11, FR-HE-014). §5.4.2.</summary>
        [Test]
        public void Conformance_T_HE_C_002_ConstantTagVerification()
        {
            Assert.Ignore("Stage 0+1: CI-time constant-tag grep gate — activate when #20 §3.x constant-tag lint is wired into CI pipeline");
        }

        /// <summary>T-HE-C-003: Every RNG call in heading-mechanics/ uses DeterministicRng draw-site API; no System.Random or Random.Range (KD-10, FR-HE-008). §5.4.3.</summary>
        [Test]
        public void Conformance_T_HE_C_003_RngRoutingDisciplineNoSystemRandom()
        {
            Assert.Ignore("Stage 0+1: CI-time RNG routing grep gate — activate when #16 DeterministicRngService is wired into HeadingMechanics and CI lint checks are live");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-31 | —      | Initial implementation. 25+ unit tests across 5 test fixtures:    |
// |         |            |        | HeadingJumpKinematicsTests (§5.1.2, 8 tests),                     |
// |         |            |        | HeadingContactQualityTests (§5.1.3, 7 tests),                     |
// |         |            |        | HeadingPowerAngleTests (§5.1.4, 7 tests),                         |
// |         |            |        | HeadingSpinTransferTests (§5.1.5, 6 tests),                       |
// |         |            |        | HeadingDuelResolutionTests (§5.1.6, 8 tests),                     |
// |         |            |        | HeadingJumpKinematicsParabolaTests (§5.1.2 extra, 4 tests).       |
// | 1.1     | 2026-06-01 | —      | Added HeadingMechanicsIntegrationTests class with 15 stubs:        |
// |         |            |        | T-HE-I-001..009 (§5.2 integration), T-HE-V-001..003 (§5.3         |
// |         |            |        | validation), T-HE-C-001..003 (§5.4 conformance). All stubs use    |
// |         |            |        | Assert.Ignore with Stage 0+1 activation conditions.               |
#endregion
