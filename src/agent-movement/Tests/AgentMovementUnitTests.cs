// File:     src/agent-movement/Tests/AgentMovementUnitTests.cs
// Created:  2026-06-04
// Modified: 2026-06-04
// Author:   —
// Spec:     Agent Movement #2 test-plan.md (T-AM-007..009 / T-AM-019..023 / T-AM-034..039 /
//           T-AM-044..047 / T-AM-050..052 / T-AM-070..107), Code Standards #20
// Purpose:  Pure-function unit coverage for the Agent Movement assembly. Sibling to
//           AgentMovementTests.cs (which holds the regression-anchored integration roster).
//           Each [TestFixture] targets a single source file; test IDs are catalogued in
//           docs/specs/agent-movement/test-plan.md.

using NUnit.Framework;
using UnityEngine;

namespace TacticalDirector.AgentMovement.Tests
{
    // ── T-AM-007..009 + T-AM-019..023: AgentStateMachine pure functions ──

    /// <summary>
    /// Pure-function tests for the stumble path on AgentStateMachine.
    /// Companion to AgentStateMachineDwellTests (T-AM-001..006) in AgentMovementTests.cs.
    /// </summary>
    [TestFixture]
    internal sealed class AgentStateMachineStumbleTests
    {
        private const float Tolerance = 0.001f;

        // T-AM-007: default balance produces the documented mid-range dwell
        [Test]
        public void CalculateStumbleDwell_DefaultBalance_InClampRange()
        {
            float dwell = AgentStateMachine.CalculateStumbleDwell(balance: 10);

            // balanceFactor = max(10/20, 0.05) = 0.5; dwell = 0.6 / 0.5 = 1.2; clamp [0.3, 1.5] → 1.2
            Assert.AreEqual(1.2f, dwell, Tolerance);
        }

        // T-AM-008: low balance saturates at the upper clamp
        [Test]
        public void CalculateStumbleDwell_MinBalance_ClampedAtMax()
        {
            float dwell = AgentStateMachine.CalculateStumbleDwell(balance: 1);

            // balanceFactor = max(1/20, 0.05) = 0.05; dwell = 0.6/0.05 = 12.0; clamp → 1.5
            Assert.AreEqual(MovementThresholds.StumbleDwellClampMax, dwell, Tolerance);
        }

        // T-AM-009: max balance produces a fast recovery, still above the lower clamp
        [Test]
        public void CalculateStumbleDwell_MaxBalance_BetweenClamps()
        {
            float dwellMax = AgentStateMachine.CalculateStumbleDwell(balance: 20);
            float dwellDefault = AgentStateMachine.CalculateStumbleDwell(balance: 10);

            // balanceFactor = 20/20 = 1.0; dwell = 0.6/1.0 = 0.6; > 0.3 floor; < default 1.2.
            Assert.That(dwellMax, Is.GreaterThanOrEqualTo(MovementThresholds.StumbleDwellClampMin),
                "Elite balance must still respect the lower clamp.");
            Assert.That(dwellMax, Is.LessThan(dwellDefault),
                "Higher balance must produce faster recovery than default.");
        }

        // T-AM-019: low-stress conditions stay below resistance — no stumble
        [Test]
        public void ShouldStumble_LowStress_ReturnsFalse()
        {
            // At StumbleSpeedThreshold + small angle, even default attrs resist.
            bool stumbled = AgentStateMachine.ShouldStumble(
                speed: MovementThresholds.StumbleSpeedThreshold,
                turnAngle: 30.0f,
                balance: 10,
                agility: 10);

            Assert.IsFalse(stumbled);
        }

        // T-AM-020: worst case — max stress + min attributes → stumble
        [Test]
        public void ShouldStumble_MaxStressMinAttrs_ReturnsTrue()
        {
            bool stumbled = AgentStateMachine.ShouldStumble(
                speed: MovementThresholds.MAX_SPEED,
                turnAngle: TurnConstants.HALF_ROTATION_DEG,
                balance: 1,
                agility: 1);

            Assert.IsTrue(stumbled,
                "Max-speed full-reversal turn at minimum attributes must trigger a stumble.");
        }

        // T-AM-021: moderate stress where elite attrs resist but min attrs stumble.
        // Note: peak stress (speed=MAX_SPEED, turn=180) drives risk = 1.0 × 1.0 × 1.5 = 1.5,
        // which exceeds even max resistance (40/40 = 1.0). Per the §3.1.5 formula, elites
        // are NOT immune at peak stress — the spec deliberately allows that. This test
        // anchors the case where the attribute axis actually discriminates.
        [Test]
        public void ShouldStumble_ModerateStress_EliteAttrsResistButMinAttrsStumble()
        {
            // speed=8, turn=120 → risk = (8/12) × (120/180) × 1.5 ≈ 0.667
            // Min attrs resistance = 2/40 = 0.05 → stumble.
            // Max attrs resistance = 40/40 = 1.0 → resist.
            bool minStumbled = AgentStateMachine.ShouldStumble(
                speed: 8.0f, turnAngle: 120.0f, balance: 1, agility: 1);
            bool maxStumbled = AgentStateMachine.ShouldStumble(
                speed: 8.0f, turnAngle: 120.0f, balance: 20, agility: 20);

            Assert.IsTrue(minStumbled, "Min attrs must stumble at moderate stress.");
            Assert.IsFalse(maxStumbled, "Elite attrs must resist at moderate stress.");
        }

        // T-AM-022: deterministic — repeat invocations on the same inputs match
        [Test]
        public void ShouldStumble_SameInputs_ProducesSameOutput()
        {
            bool a = AgentStateMachine.ShouldStumble(8.0f, 90.0f, 10, 10);
            bool b = AgentStateMachine.ShouldStumble(8.0f, 90.0f, 10, 10);

            Assert.AreEqual(a, b,
                "ShouldStumble must be deterministic — Stage 0 uses no RNG path here.");
        }

        // T-AM-023: zero turn angle reduces risk to the floor; resistance alone decides
        [Test]
        public void ShouldStumble_ZeroTurnAngle_FloorBoundedBehaviour()
        {
            // Risk = max(0, MinStumbleRisk) = MinStumbleRisk = 0.03.
            // Resistance at agility+balance=2 (min) = 2/40 = 0.05 → risk < resistance → no stumble.
            bool minAttrs = AgentStateMachine.ShouldStumble(
                speed: MovementThresholds.MAX_SPEED, turnAngle: 0.0f, balance: 1, agility: 1);
            Assert.IsFalse(minAttrs,
                "At min attrs, MinStumbleRisk (0.03) is still below resistance (0.05).");

            bool defaultAttrs = AgentStateMachine.ShouldStumble(
                speed: MovementThresholds.MAX_SPEED, turnAngle: 0.0f, balance: 10, agility: 10);
            Assert.IsFalse(defaultAttrs,
                "Zero turn means only the risk floor applies; default resistance comfortably exceeds it.");
        }
    }

    // ── T-AM-034..039: AgentSafetySystem unit tests ──

    [TestFixture]
    internal sealed class AgentSafetySystemUnitTests
    {
        // T-AM-034: each invalid input class trips HasInvalidValues
        [Test]
        public void HasInvalidValues_InvalidInputs_ReturnsTrue()
        {
            // NaN in position
            Assert.IsTrue(AgentSafetySystem.HasInvalidValues(
                position: new Vector2(float.NaN, 0.0f),
                velocity: Vector2.zero,
                facing: Vector2.up));

            // Inf in velocity
            Assert.IsTrue(AgentSafetySystem.HasInvalidValues(
                position: Vector2.zero,
                velocity: new Vector2(0.0f, float.PositiveInfinity),
                facing: Vector2.up));

            // Zero facing (sqrMagnitude below epsilon)
            Assert.IsTrue(AgentSafetySystem.HasInvalidValues(
                position: Vector2.zero,
                velocity: Vector2.zero,
                facing: Vector2.zero),
                "Zero facing must be flagged — downstream Normalize would return NaN.");
        }

        // T-AM-035: fully valid inputs pass
        [Test]
        public void HasInvalidValues_ValidInputs_ReturnsFalse()
        {
            bool invalid = AgentSafetySystem.HasInvalidValues(
                position: new Vector2(50.0f, 30.0f),
                velocity: new Vector2(3.0f, 1.0f),
                facing: Vector2.right);

            Assert.IsFalse(invalid);
        }

        // T-AM-036: below-cap velocity passes through unchanged
        [Test]
        public void ClampVelocity_BelowCap_PreservesVelocity()
        {
            var v = new Vector2(5.0f, 3.0f); // magnitude ≈ 5.83 < MAX_SPEED_CLAMP (12)
            Vector2 clamped = AgentSafetySystem.ClampVelocity(v);

            Assert.AreEqual(v, clamped);
        }

        // T-AM-037: above-cap velocity rescales to MAX_SPEED_CLAMP with direction preserved
        [Test]
        public void ClampVelocity_AboveCap_RescalesToMaxPreservingDirection()
        {
            var v = new Vector2(20.0f, 0.0f); // way above the 12 m/s cap
            Vector2 clamped = AgentSafetySystem.ClampVelocity(v);

            Assert.AreEqual(MovementThresholds.MAX_SPEED_CLAMP, clamped.magnitude, 0.0001f);
            Assert.AreEqual(1.0f, clamped.normalized.x, 0.0001f, "Direction must be preserved.");
            Assert.AreEqual(0.0f, clamped.normalized.y, 0.0001f);
        }

        // T-AM-038: pitch clamp respects in-bounds and rescales out-of-bounds
        [Test]
        public void ClampToPitch_RespectsBufferedBounds()
        {
            // In-bounds: unchanged.
            var inside = new Vector2(50.0f, 30.0f);
            Assert.AreEqual(inside, AgentSafetySystem.ClampToPitch(inside));

            // Out-of-bounds (negative): clamped to -buffer.
            var negative = new Vector2(-100.0f, -100.0f);
            Vector2 clamped = AgentSafetySystem.ClampToPitch(negative);
            Assert.AreEqual(-SafetyConstants.PitchBoundaryBuffer, clamped.x, 0.0001f);
            Assert.AreEqual(-SafetyConstants.PitchBoundaryBuffer, clamped.y, 0.0001f);

            // Out-of-bounds (over): clamped to dimension + buffer.
            var over = new Vector2(500.0f, 500.0f);
            clamped = AgentSafetySystem.ClampToPitch(over);
            Assert.AreEqual(SafetyConstants.PitchLengthX + SafetyConstants.PitchBoundaryBuffer,
                clamped.x, 0.0001f);
            Assert.AreEqual(SafetyConstants.PitchWidthY + SafetyConstants.PitchBoundaryBuffer,
                clamped.y, 0.0001f);
        }

        // T-AM-039: Validate happy-path — inputs survive, wasRecovered=false
        [Test]
        public void Validate_ValidInputs_LeavesInPlaceAndReportsNoRecovery()
        {
            Vector2 pos = new Vector2(40.0f, 20.0f);
            Vector2 vel = new Vector2(2.0f, 1.0f);
            Vector2 facing = Vector2.right;

            AgentSafetySystem.Validate(
                ref pos, ref vel, ref facing,
                lastValidPosition: Vector2.zero,
                lastValidVelocity: Vector2.zero,
                lastValidFacing: Vector2.up,
                out bool wasRecovered);

            Assert.IsFalse(wasRecovered, "Happy-path Validate must report no recovery.");
            Assert.AreEqual(new Vector2(40.0f, 20.0f), pos,
                "In-bounds position is unchanged (boundary clamp is a no-op).");
            Assert.AreEqual(new Vector2(2.0f, 1.0f), vel,
                "Below-cap velocity is unchanged.");
            Assert.AreEqual(Vector2.right, facing,
                "Valid facing is unchanged.");
        }
    }

    // ── T-AM-044..047: OscillationGuard edge cases ──

    [TestFixture]
    internal sealed class OscillationGuardEdgeTests
    {
        // T-AM-044: 6 transitions in window + 1 transition > WindowSeconds later → not blocked
        [Test]
        public void RecordAndCheck_StaleTimestampsExpireFromWindow()
        {
            var guard = new OscillationGuard();
            guard.Initialize();

            // 6 fast transitions — within MaxTransitionsPerSecond, no lock.
            for (int i = 0; i < 6; i++)
            {
                guard.RecordAndCheck(i * 0.1f);
            }

            // Jump forward more than WindowSeconds (1.0) past the LAST timestamp.
            // The previous 6 timestamps (last at 0.5) are all > 1.0 s in the past.
            bool blocked = guard.RecordAndCheck(2.0f);

            Assert.IsFalse(blocked,
                "After WindowSeconds elapses, stale timestamps must drop out of recentCount.");
        }

        // T-AM-045: ring buffer wraps cleanly past BufferSize entries with well-spaced timing
        [Test]
        public void RecordAndCheck_BufferWrapsWithoutFalseLock()
        {
            var guard = new OscillationGuard();
            guard.Initialize();

            // Space transitions 0.5 s apart — never more than 2 in any 1 s window.
            // Push 12 transitions, wrapping the BufferSize=8 ring twice.
            for (int i = 0; i < 12; i++)
            {
                bool blocked = guard.RecordAndCheck(i * 0.5f);
                Assert.IsFalse(blocked, $"Call {i} at t={i * 0.5f:F1}s should not block.");
            }
        }

        // T-AM-046: bursty then quiet — burst inside the window must not lock if attempts
        // outside the window are not counted
        [Test]
        public void RecordAndCheck_TimestampsOutsideWindowAreNotCounted()
        {
            var guard = new OscillationGuard();
            guard.Initialize();

            // First burst: 6 transitions in 0.5 s (no lock).
            for (int i = 0; i < 6; i++)
            {
                guard.RecordAndCheck(i * 0.1f);
            }

            // Jump past WindowSeconds — all prior timestamps are now stale.
            // Second burst: 6 more transitions, also no lock.
            for (int i = 0; i < 6; i++)
            {
                bool blocked = guard.RecordAndCheck(5.0f + i * 0.1f);
                Assert.IsFalse(blocked,
                    $"Burst-2 call {i} should not lock — first-burst timestamps are stale.");
            }
        }

        // T-AM-047: Initialize() after lock wipes the guard fully (supports the AR-7 M-1
        // collision-bypass reset path in AgentMovementSystem.Update)
        [Test]
        public void Initialize_AfterLock_FullyResetsGuard()
        {
            var guard = new OscillationGuard();
            guard.Initialize();
            for (int i = 0; i < 7; i++)
            {
                guard.RecordAndCheck(i * 0.1f);
            }
            // Sanity precondition: guard is locked.
            Assert.IsTrue(guard.RecordAndCheck(0.7f));

            // Re-initialise — wipes lock and ring buffer to NegativeInfinity sentinels.
            guard.Initialize();

            // Next transition must succeed.
            bool blocked = guard.RecordAndCheck(0.8f);
            Assert.IsFalse(blocked,
                "Initialize() must fully reset lock state and timestamp history.");
        }
    }

    // ── T-AM-050..052: PerformanceContext unit tests ──

    [TestFixture]
    internal sealed class PerformanceContextUnitTests
    {
        // T-AM-050: neutral context is the identity modifier
        [Test]
        public void EvaluateAttribute_NeutralContext_ReturnsRawValue()
        {
            PerformanceContext perf = PerformanceContext.CreateNeutral();

            Assert.AreEqual(15.0f, perf.EvaluateAttribute(15), 0.0001f);
            Assert.AreEqual(1.0f, perf.EvaluateAttribute(1), 0.0001f);
            Assert.AreEqual(20.0f, perf.EvaluateAttribute(20), 0.0001f);
        }

        // T-AM-051: combined-modifier product applies multiplicatively
        [Test]
        public void EvaluateAttribute_PartialModifiers_AppliesProduct()
        {
            // 0.9 × 0.8 × 0.7 = 0.504
            PerformanceContext perf = PerformanceContext.Create(
                form: 0.9f, context: 0.8f, career: 0.7f);

            float expected = 10 * 0.9f * 0.8f * 0.7f;
            Assert.AreEqual(expected, perf.EvaluateAttribute(10), 0.0001f);
        }

        // T-AM-052: Create() clamps modifiers to [0, 1]
        [Test]
        public void Create_OutOfRangeModifiers_ClampedTo01()
        {
            PerformanceContext perf = PerformanceContext.Create(
                form: 1.5f, context: -0.2f, career: 0.5f);

            Assert.AreEqual(1.0f, perf.FormModifier, 0.0001f, "form > 1 clamps to 1.");
            Assert.AreEqual(0.0f, perf.ContextModifier, 0.0001f, "context < 0 clamps to 0.");
            Assert.AreEqual(0.5f, perf.CareerModifier, 0.0001f, "in-range modifier unchanged.");
        }
    }

    // ── T-AM-070..083: AgentLocomotion formula tests ──

    [TestFixture]
    internal sealed class AgentLocomotionUnitTests
    {
        private const float Tolerance = 0.001f;
        private const float Dt = 1.0f / 60.0f;

        // T-AM-070: top-speed lower anchor at pace=1
        [Test]
        public void CalculateBaseTopSpeed_PaceMin_ReturnsTopSpeedMin()
        {
            float v = AgentLocomotion.CalculateBaseTopSpeed(1.0f);
            Assert.AreEqual(LocomotionConstants.TOP_SPEED_MIN, v, Tolerance);
        }

        // T-AM-071: top-speed upper anchor at pace=20
        [Test]
        public void CalculateBaseTopSpeed_PaceMax_ReturnsTopSpeedMax()
        {
            float v = AgentLocomotion.CalculateBaseTopSpeed(20.0f);
            Assert.AreEqual(LocomotionConstants.TOP_SPEED_MAX, v, Tolerance);
        }

        // T-AM-072: out-of-range pace clamps at endpoints (no extrapolation)
        [Test]
        public void CalculateBaseTopSpeed_OutOfRange_ClampsAtEndpoints()
        {
            Assert.AreEqual(LocomotionConstants.TOP_SPEED_MIN,
                AgentLocomotion.CalculateBaseTopSpeed(-5.0f), Tolerance,
                "Below pace=1 must clamp at TOP_SPEED_MIN, not extrapolate.");
            Assert.AreEqual(LocomotionConstants.TOP_SPEED_MAX,
                AgentLocomotion.CalculateBaseTopSpeed(50.0f), Tolerance,
                "Above pace=20 must clamp at TOP_SPEED_MAX.");
        }

        // T-AM-073: accel-k endpoints
        [Test]
        public void CalculateBaseAccelK_EndpointsMatchConstants()
        {
            Assert.AreEqual(LocomotionConstants.AccelKMin,
                AgentLocomotion.CalculateBaseAccelK(1.0f), Tolerance);
            Assert.AreEqual(LocomotionConstants.AccelKMax,
                AgentLocomotion.CalculateBaseAccelK(20.0f), Tolerance);
        }

        // T-AM-074: at top speed, ApplyAcceleration is a fixed point
        [Test]
        public void ApplyAcceleration_AtTopSpeed_StaysAtTopSpeed()
        {
            float top = 8.0f;
            float result = AgentLocomotion.ApplyAcceleration(
                currentSpeed: top, topSpeed: top, k: 0.8f, dt: Dt);

            Assert.AreEqual(top, result, Tolerance,
                "Exponential interp at the asymptote is a fixed point.");
        }

        // T-AM-075: from rest, ApplyAcceleration approaches top speed monotonically
        [Test]
        public void ApplyAcceleration_FromRest_StrictlyBetweenZeroAndTopSpeed()
        {
            float top = 8.0f;
            float result = AgentLocomotion.ApplyAcceleration(0.0f, top, 0.8f, Dt);

            Assert.That(result, Is.GreaterThan(0.0f), "Some velocity gained in one frame.");
            Assert.That(result, Is.LessThan(top), "Cannot reach top speed in one frame.");
        }

        // T-AM-076: ApplyAcceleration safety clamp at MAX_SPEED_CLAMP
        [Test]
        public void ApplyAcceleration_ResultClampedAtMaxSpeedClamp()
        {
            // Drive topSpeed above the cap; result must clamp to the cap.
            float result = AgentLocomotion.ApplyAcceleration(
                currentSpeed: 11.0f,
                topSpeed: 100.0f,
                k: 100.0f, // huge k → decay ≈ 0 → newSpeed ≈ topSpeed
                dt: Dt);

            Assert.LessOrEqual(result, MovementThresholds.MAX_SPEED_CLAMP + Tolerance);
        }

        // T-AM-077: below MIN_VELOCITY_MAGNITUDE, ApplyDeceleration short-circuits to 0
        [Test]
        public void ApplyDeceleration_BelowMinVelocity_ReturnsZero()
        {
            float result = AgentLocomotion.ApplyDeceleration(
                currentSpeed: 0.0005f, // < MIN_VELOCITY_MAGNITUDE (0.001)
                stoppingDistanceM: 5.0f, dt: Dt);

            Assert.AreEqual(0.0f, result, Tolerance);
        }

        // T-AM-078: decel demand capped at MAX_ACCELERATION
        [Test]
        public void ApplyDeceleration_HighDemand_CappedAtMaxAcceleration()
        {
            // At v=10 with d=0.1 (clamped at MinStoppingDistanceM), raw decel = 100/0.2 = 500 m/s².
            // Capped at MAX_ACCELERATION=8 → speed decrement per frame ≤ 8 × dt.
            float v0 = 10.0f;
            float result = AgentLocomotion.ApplyDeceleration(
                currentSpeed: v0, stoppingDistanceM: 0.05f, dt: Dt);

            float minSpeedAfterCap = v0 - MovementThresholds.MAX_ACCELERATION * Dt;
            Assert.AreEqual(minSpeedAfterCap, result, Tolerance,
                "Decel demand must be capped at MAX_ACCELERATION even with tiny stoppingDistance.");
        }

        // T-AM-079: normal-demand decel reduces speed by v²/(2d) × dt
        [Test]
        public void ApplyDeceleration_NormalDemand_UsesKinematicFormula()
        {
            // At v=4 with d=4 (above MinStoppingDistance): raw decel = 16/8 = 2 m/s² (< 8 cap).
            float v0 = 4.0f;
            float d = 4.0f;
            float expectedDecel = (v0 * v0) / (LocomotionConstants.KINEMATIC_HALF * d); // 2.0
            float expectedSpeed = v0 - expectedDecel * Dt;

            float result = AgentLocomotion.ApplyDeceleration(v0, d, Dt);

            Assert.AreEqual(expectedSpeed, result, Tolerance);
        }

        // T-AM-080: controlled-mode stopping-distance endpoints
        [Test]
        public void CalculateStoppingDistance_ControlledMode_HitsEndpoints()
        {
            Assert.AreEqual(LocomotionConstants.ControlledDecelDistMin,
                AgentLocomotion.CalculateStoppingDistance(DecelerationMode.CONTROLLED, 1.0f),
                Tolerance);
            Assert.AreEqual(LocomotionConstants.ControlledDecelDistMax,
                AgentLocomotion.CalculateStoppingDistance(DecelerationMode.CONTROLLED, 20.0f),
                Tolerance);
        }

        // T-AM-081: emergency-mode stopping-distance endpoints
        [Test]
        public void CalculateStoppingDistance_EmergencyMode_HitsEndpoints()
        {
            Assert.AreEqual(LocomotionConstants.EmergencyDecelDistMin,
                AgentLocomotion.CalculateStoppingDistance(DecelerationMode.EMERGENCY, 1.0f),
                Tolerance);
            Assert.AreEqual(LocomotionConstants.EmergencyDecelDistMax,
                AgentLocomotion.CalculateStoppingDistance(DecelerationMode.EMERGENCY, 20.0f),
                Tolerance);
        }

        // T-AM-082: aerobic modifier returns 1.0 at or above the threshold
        [Test]
        public void CalculateAerobicModifier_AboveThreshold_ReturnsOne()
        {
            Assert.AreEqual(1.0f,
                AgentLocomotion.CalculateAerobicModifier(MovementThresholds.AerobicModifierThreshold),
                Tolerance);
            Assert.AreEqual(1.0f,
                AgentLocomotion.CalculateAerobicModifier(1.0f), Tolerance);
        }

        // T-AM-083: below the threshold, modifier scales linearly from floor to 1.0
        [Test]
        public void CalculateAerobicModifier_BelowThreshold_LinearInterp()
        {
            // At pool = 0 → AerobicModifierFloor.
            Assert.AreEqual(MovementThresholds.AerobicModifierFloor,
                AgentLocomotion.CalculateAerobicModifier(0.0f), Tolerance);

            // At pool = threshold/2 → midpoint between floor and 1.0.
            float mid = AgentLocomotion.CalculateAerobicModifier(
                MovementThresholds.AerobicModifierThreshold * 0.5f);
            float expectedMid = Mathf.Lerp(MovementThresholds.AerobicModifierFloor, 1.0f, 0.5f);
            Assert.AreEqual(expectedMid, mid, Tolerance);
        }
    }

    // ── T-AM-084..099: AgentDirectionalMovement formula tests ──

    [TestFixture]
    internal sealed class AgentDirectionalMovementUnitTests
    {
        private const float Tolerance = 0.001f;

        // T-AM-084: lateral-multiplier endpoints
        [Test]
        public void LateralMultiplier_EndpointsMatchConstants()
        {
            Assert.AreEqual(DirectionalConstants.LateralMultMin,
                AgentDirectionalMovement.LateralMultiplier(1.0f), Tolerance);
            Assert.AreEqual(DirectionalConstants.LateralMultMax,
                AgentDirectionalMovement.LateralMultiplier(20.0f), Tolerance);
        }

        // T-AM-085: backward-multiplier endpoints
        [Test]
        public void BackwardMultiplier_EndpointsMatchConstants()
        {
            Assert.AreEqual(DirectionalConstants.BackwardMultMin,
                AgentDirectionalMovement.BackwardMultiplier(1.0f), Tolerance);
            Assert.AreEqual(DirectionalConstants.BackwardMultMax,
                AgentDirectionalMovement.BackwardMultiplier(20.0f), Tolerance);
        }

        // T-AM-086: angles in the forward zone (with hysteresis) return 1.0
        [Test]
        public void CalculateDirectionalMultiplier_ForwardZone_ReturnsOne()
        {
            float agility = 10.0f;
            Assert.AreEqual(1.0f,
                AgentDirectionalMovement.CalculateDirectionalMultiplier(0.0f, agility),
                Tolerance);
            Assert.AreEqual(1.0f,
                AgentDirectionalMovement.CalculateDirectionalMultiplier(
                    DirectionalConstants.FORWARD_ZONE_MAX, agility),
                Tolerance);
            Assert.AreEqual(1.0f,
                AgentDirectionalMovement.CalculateDirectionalMultiplier(
                    DirectionalConstants.FORWARD_ZONE_MAX + DirectionalConstants.ZONE_HYSTERESIS,
                    agility),
                Tolerance,
                "Hysteresis extends the pure-forward zone by ZONE_HYSTERESIS.");
        }

        // T-AM-087: lateral zone returns the lateral multiplier
        [Test]
        public void CalculateDirectionalMultiplier_LateralZone_ReturnsLateralMult()
        {
            float agility = 10.0f;
            float lat = AgentDirectionalMovement.LateralMultiplier(agility);

            Assert.AreEqual(lat,
                AgentDirectionalMovement.CalculateDirectionalMultiplier(
                    DirectionalConstants.LATERAL_ZONE_START, agility),
                Tolerance);
            Assert.AreEqual(lat,
                AgentDirectionalMovement.CalculateDirectionalMultiplier(60.0f, agility),
                Tolerance);
            Assert.AreEqual(lat,
                AgentDirectionalMovement.CalculateDirectionalMultiplier(
                    DirectionalConstants.LATERAL_ZONE_END, agility),
                Tolerance);
        }

        // T-AM-088: backward zone returns the backward multiplier
        [Test]
        public void CalculateDirectionalMultiplier_BackwardZone_ReturnsBackwardMult()
        {
            float agility = 10.0f;
            float bwd = AgentDirectionalMovement.BackwardMultiplier(agility);

            Assert.AreEqual(bwd,
                AgentDirectionalMovement.CalculateDirectionalMultiplier(
                    DirectionalConstants.BACKWARD_ZONE_START, agility),
                Tolerance);
            Assert.AreEqual(bwd,
                AgentDirectionalMovement.CalculateDirectionalMultiplier(
                    TurnConstants.HALF_ROTATION_DEG, agility),
                Tolerance);
        }

        // T-AM-089: forward-lateral blend region interpolates between 1.0 and lateral
        [Test]
        public void CalculateDirectionalMultiplier_ForwardBlend_InterpolatesBetweenOneAndLateral()
        {
            float agility = 10.0f;
            float lat = AgentDirectionalMovement.LateralMultiplier(agility);

            // Blend region [33, 40]; midpoint = 36.5.
            float mid = AgentDirectionalMovement.CalculateDirectionalMultiplier(36.5f, agility);

            Assert.That(mid, Is.LessThan(1.0f).And.GreaterThan(lat),
                "Forward-blend value must sit strictly between 1.0 and the lateral multiplier.");
        }

        // T-AM-090: lateral-backward blend region interpolates between lateral and backward
        [Test]
        public void CalculateDirectionalMultiplier_BackwardBlend_InterpolatesBetweenLateralAndBackward()
        {
            float agility = 10.0f;
            float lat = AgentDirectionalMovement.LateralMultiplier(agility);
            float bwd = AgentDirectionalMovement.BackwardMultiplier(agility);

            // Blend region [80, 87]; midpoint = 83.5.
            float mid = AgentDirectionalMovement.CalculateDirectionalMultiplier(83.5f, agility);

            Assert.That(mid, Is.LessThan(lat).And.GreaterThan(bwd),
                "Backward-blend value must sit strictly between lateral and backward multipliers.");
        }

        // T-AM-091: directional sqrt scaling is the identity at mult = 1.0
        [Test]
        public void ApplyDirectionalToAccelK_UnitMult_ReturnsKBase()
        {
            float k = 0.7f;
            Assert.AreEqual(k, AgentDirectionalMovement.ApplyDirectionalToAccelK(k, 1.0f), Tolerance);
        }

        // T-AM-092: sqrt scaling applied for fractional multipliers
        [Test]
        public void ApplyDirectionalToAccelK_FractionalMult_AppliesSqrtScaling()
        {
            float k = 0.8f;
            float result = AgentDirectionalMovement.ApplyDirectionalToAccelK(k, 0.5f);
            Assert.AreEqual(k * Mathf.Sqrt(0.5f), result, Tolerance);
        }

        // T-AM-093: negative input clamped — no NaN from sqrt
        [Test]
        public void ApplyDirectionalToAccelK_NegativeMult_ClampsToZeroWithoutNaN()
        {
            float result = AgentDirectionalMovement.ApplyDirectionalToAccelK(0.7f, -0.5f);
            Assert.AreEqual(0.0f, result, Tolerance);
            Assert.That(result, Is.Not.NaN);
        }

        // T-AM-094: same direction → angle 0
        [Test]
        public void MovementAngleDeg_SameDirection_ReturnsZero()
        {
            Assert.AreEqual(0.0f,
                AgentDirectionalMovement.MovementAngleDeg(Vector2.up, Vector2.up),
                Tolerance);
        }

        // T-AM-095: orthogonal vectors → 90 degrees
        [Test]
        public void MovementAngleDeg_Orthogonal_Returns90()
        {
            Assert.AreEqual(90.0f,
                AgentDirectionalMovement.MovementAngleDeg(Vector2.right, Vector2.up),
                Tolerance);
        }

        // T-AM-096: opposite vectors → 180 degrees
        [Test]
        public void MovementAngleDeg_Opposite_Returns180()
        {
            Assert.AreEqual(TurnConstants.HALF_ROTATION_DEG,
                AgentDirectionalMovement.MovementAngleDeg(Vector2.right, -Vector2.right),
                Tolerance);
        }

        // T-AM-097: zero-vector input → 0 (degenerate guard)
        [Test]
        public void MovementAngleDeg_ZeroInput_ReturnsZero()
        {
            Assert.AreEqual(0.0f,
                AgentDirectionalMovement.MovementAngleDeg(Vector2.zero, Vector2.up),
                Tolerance);
            Assert.AreEqual(0.0f,
                AgentDirectionalMovement.MovementAngleDeg(Vector2.right, Vector2.zero),
                Tolerance);
        }

        // T-AM-098: zero target → currentFacing unchanged, signedAngleApplied = 0
        [Test]
        public void RotateFacingToward_ZeroTarget_LeavesCurrentFacingUnchanged()
        {
            Vector2 result = AgentDirectionalMovement.RotateFacingToward(
                currentFacing: Vector2.up,
                targetFacing: Vector2.zero,
                maxTurnDeg: 30.0f,
                out float signedAngleApplied);

            Assert.AreEqual(Vector2.up, result);
            Assert.AreEqual(0.0f, signedAngleApplied, Tolerance);
        }

        // T-AM-099: large requested angle is clamped to ±maxTurnDeg
        [Test]
        public void RotateFacingToward_LargeAngle_ClampedToMaxTurnDeg()
        {
            // currentFacing = up, target = -right (90° CCW rotation).
            float maxTurnDeg = 10.0f;
            AgentDirectionalMovement.RotateFacingToward(
                currentFacing: Vector2.up,
                targetFacing: Vector2.left,
                maxTurnDeg: maxTurnDeg,
                out float signedAngleApplied);

            Assert.AreEqual(maxTurnDeg, Mathf.Abs(signedAngleApplied), Tolerance,
                "|signedAngleApplied| must be clamped to maxTurnDeg.");
        }
    }

    // ── T-AM-100..107: AgentTurning formula tests ──

    [TestFixture]
    internal sealed class AgentTurningUnitTests
    {
        private const float Tolerance = 0.01f; // turn rates are large numbers; absolute tol = 1%/100

        // T-AM-100: at speed=0, turn rate is high (clamped at TURN_RATE_CAP if balanceMod=1)
        [Test]
        public void CalculateMaxTurnRate_ZeroSpeed_NearTurnRateBase()
        {
            float rate = AgentTurning.CalculateMaxTurnRate(
                speed: 0.0f, agility: 10, balance: 10, AgentMovementState.IDLE);

            // At speed=0, denominator = 1 → rate = TURN_RATE_BASE × balanceMod ≤ TURN_RATE_BASE
            Assert.That(rate, Is.LessThanOrEqualTo(TurnConstants.TURN_RATE_CAP + Tolerance));
            Assert.That(rate, Is.GreaterThan(TurnConstants.TURN_RATE_FLOOR),
                "Zero-speed rate must be well above the lower clamp.");
        }

        // T-AM-101: higher speed reduces the turn rate
        [Test]
        public void CalculateMaxTurnRate_HigherSpeed_StrictlyLowerRate()
        {
            float low = AgentTurning.CalculateMaxTurnRate(2.0f, 10, 10, AgentMovementState.JOGGING);
            float high = AgentTurning.CalculateMaxTurnRate(8.0f, 10, 10, AgentMovementState.SPRINTING);

            Assert.That(high, Is.LessThan(low),
                "Turn rate must decrease as speed increases per §3.4.2.");
        }

        // T-AM-102: extreme speed clamps at TURN_RATE_FLOOR
        [Test]
        public void CalculateMaxTurnRate_ExtremeSpeed_ClampedAtFloor()
        {
            // With agility=20 → kTurn=0.35; very large speed → rate→0; clamp lifts to floor.
            float rate = AgentTurning.CalculateMaxTurnRate(
                speed: 1000.0f, agility: 20, balance: 10, AgentMovementState.SPRINTING);

            Assert.AreEqual(TurnConstants.TURN_RATE_FLOOR, rate, Tolerance);
        }

        // T-AM-103: GROUNDED state forces rate to 0, then the floor clamp lifts to TURN_RATE_FLOOR
        [Test]
        public void CalculateMaxTurnRate_Grounded_ClampedAtFloor()
        {
            // StateModifier(GROUNDED) = 0 → raw rate = 0 → clamp [floor, cap] → floor.
            float rate = AgentTurning.CalculateMaxTurnRate(
                speed: 0.0f, agility: 10, balance: 10, AgentMovementState.GROUNDED);

            Assert.AreEqual(TurnConstants.TURN_RATE_FLOOR, rate, Tolerance,
                "GROUNDED zeroes the stateMod, but the lower clamp keeps the public floor.");
        }

        // T-AM-104: near-zero turn rate → infinite radius guard
        [Test]
        public void MinimumTurnRadius_NearZeroRate_ReturnsMaxValue()
        {
            float r = AgentTurning.MinimumTurnRadius(
                speedMs: 5.0f,
                maxTurnRateDeg: TurnConstants.TURN_RATE_EPSILON_DEG * 0.5f);

            Assert.AreEqual(float.MaxValue, r);
        }

        // T-AM-105: normal turn radius matches r = v / ω_rad
        [Test]
        public void MinimumTurnRadius_NormalInputs_MatchesKinematic()
        {
            float v = 5.0f;
            float omegaDeg = 180.0f;
            float expected = v / (omegaDeg * Mathf.Deg2Rad);

            float result = AgentTurning.MinimumTurnRadius(v, omegaDeg);
            Assert.AreEqual(expected, result, Tolerance);
        }

        // T-AM-106: zero signed turn rate → zero lean
        [Test]
        public void CalculateLeanAngle_ZeroTurnRate_ReturnsZero()
        {
            float lean = AgentTurning.CalculateLeanAngle(speedMs: 8.0f, signedTurnRateDeg: 0.0f);
            Assert.AreEqual(0.0f, lean, Tolerance);
        }

        // T-AM-107: extreme centripetal load clamps lean angle, sign preserved
        [Test]
        public void CalculateLeanAngle_ExtremeCentripetal_ClampedWithSignPreserved()
        {
            // Right-turn lean (negative sign per Unity convention).
            float leanRight = AgentTurning.CalculateLeanAngle(
                speedMs: MovementThresholds.MAX_SPEED, signedTurnRateDeg: -720.0f);

            Assert.That(leanRight, Is.LessThan(0.0f),
                "Negative turn rate must produce negative lean (right-side lean).");
            Assert.AreEqual(TurnConstants.MAX_LEAN_ANGLE, Mathf.Abs(leanRight), 0.5f,
                "Magnitude must saturate at MAX_LEAN_ANGLE.");

            // Left-turn lean (positive sign).
            float leanLeft = AgentTurning.CalculateLeanAngle(
                speedMs: MovementThresholds.MAX_SPEED, signedTurnRateDeg: 720.0f);
            Assert.That(leanLeft, Is.GreaterThan(0.0f));
            Assert.AreEqual(TurnConstants.MAX_LEAN_ANGLE, Mathf.Abs(leanLeft), 0.5f);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                              |
// | 1.0     | 2026-06-04 | —      | Initial: 59 pure-function unit tests across 7 fixtures (AgentStateMachineStumble / |
// |         |            |        | AgentSafetySystemUnit / OscillationGuardEdge / PerformanceContextUnit /            |
// |         |            |        | AgentLocomotionUnit / AgentDirectionalMovementUnit / AgentTurningUnit). IDs        |
// |         |            |        | catalogued in docs/specs/agent-movement/test-plan.md v0.2.                         |
#endregion
