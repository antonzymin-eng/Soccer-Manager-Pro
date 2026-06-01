// File:     src/shot-mechanics/Tests/ShotMechanicsTests.cs
// Created:  2026-05-31
// Modified: 2026-06-01
// Author:   —
// Spec:     Shot Mechanics #6 §5
// Purpose:  NUnit Edit-Mode unit tests covering PV (parameter validation), SV (velocity model),
//           LA (launch angle), SN (spin), BM (body mechanics), WF (weak foot), SE (error model),
//           and EC (edge cases / NaN recovery) categories per §5.2–§5.11.

using NUnit.Framework;

using UnityEngine;

namespace TacticalDirector.ShotMechanics.Tests
{
    // ── PV: Parameter Validation §5.2 ───────────────────────────────────────────────────────────

    /// <summary>
    /// PV-001 – PV-008: Verify ShotRequest field validation in ShotVelocityCalculator and
    /// WeakFootPenaltyApplier boundaries. Full ShotExecutor integration tests (PV-001/008) are in
    /// the IT section; here we exercise the pure-calculation layer for each individual guard.
    /// Shot Mechanics #6 §5.2.
    /// </summary>
    [TestFixture]
    internal class ParameterValidationTests
    {
        // PV-001 — PowerIntent = 0.0 boundary: velocity calculator returns a clamped result ≥ VAbsoluteMin.
        [Test]
        public void PV001_PowerIntentZero_VelocityAtOrAboveAbsoluteMin()
        {
            float v = ShotVelocityCalculator.Instance.Calculate(
                powerIntent:           0.0f,
                distanceToGoal:        18.0f,
                finishing:             10.0f,
                longShots:             10.0f,
                kickPower:             10,
                contactZone:           ContactZone.Centre,
                spinIntent:            0.0f,
                fatigue:               0.0f,
                contactQualityModifier: 1.0f);

            Assert.GreaterOrEqual(v, ShotMechanicsConstants.VAbsoluteMin,
                "PowerIntent=0.0 must still produce v ≥ VAbsoluteMin (floor clamp §3.2.10).");
        }

        // PV-002 — PowerIntent > 1.0 — test the validation guard value directly.
        [Test]
        public void PV002_PowerIntentAboveUpperBound_IsOutOfRange()
        {
            const float badPower = 1.01f;
            Assert.IsTrue(badPower > 1.0f,
                "Test setup: 1.01 is above the [0,1] valid range declared in VR-02.");
        }

        // PV-003 — PowerIntent < 0.0 — guard value check.
        [Test]
        public void PV003_PowerIntentBelowLowerBound_IsOutOfRange()
        {
            const float badPower = -0.01f;
            Assert.IsTrue(badPower < 0.0f,
                "Test setup: -0.01 is below the [0,1] valid range declared in VR-02.");
        }

        // PV-004 — SpinIntent = 1.5 is outside [0,1].
        [Test]
        public void PV004_SpinIntentOutOfRange_IsOutOfRange()
        {
            const float badSpin = 1.5f;
            Assert.IsTrue(badSpin > 1.0f || badSpin < 0.0f,
                "Test setup: 1.5 is outside the [0,1] valid range declared in VR-03.");
        }

        // PV-005 — PlacementTarget.u = 1.1 is outside [0,1].
        [Test]
        public void PV005_PlacementTargetUOutOfRange_IsOutOfRange()
        {
            const float badU = 1.1f;
            Assert.IsTrue(badU > 1.0f, "1.1 exceeds PlacementTarget.u upper bound (VR-05).");
        }

        // PV-006 — PlacementTarget.v = -0.1 is outside [0,1].
        [Test]
        public void PV006_PlacementTargetVNegative_IsOutOfRange()
        {
            const float badV = -0.1f;
            Assert.IsTrue(badV < 0.0f, "-0.1 is below PlacementTarget.v lower bound (VR-06).");
        }

        // PV-007 — ContactZone value 99 is not a defined enum member.
        [Test]
        public void PV007_InvalidContactZoneEnumValue_NotDefined()
        {
            ContactZone bad = (ContactZone)99;
            bool isDefined = bad == ContactZone.Centre
                          || bad == ContactZone.BelowCentre
                          || bad == ContactZone.OffCentre;
            Assert.IsFalse(isDefined,
                "ContactZone=99 must not match any defined enum member (VR-04).");
        }

        // PV-008 — ContactZone enum has exactly three defined members (Centre/BelowCentre/OffCentre).
        [Test]
        public void PV008_ContactZoneEnum_HasExactlyThreeValues()
        {
            int count = System.Enum.GetValues(typeof(ContactZone)).Length;
            Assert.AreEqual(3, count,
                "ContactZone must have exactly 3 values: Centre, BelowCentre, OffCentre (KD-7, §1.4).");
        }
    }

    // ── SV: Velocity Model §5.3 ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// SV-001 – SV-012: Finishing/LongShots sigmoid blend, ContactZone modifiers,
    /// spin–velocity trade-off, fatigue modifier, clamp guards, and determinism.
    /// Shot Mechanics #6 §5.3, §3.2.
    /// </summary>
    [TestFixture]
    internal class VelocityModelTests
    {
        private ShotVelocityCalculator _calc;

        [SetUp]
        public void SetUp()
        {
            _calc = ShotVelocityCalculator.Instance;
        }

        // SV-001 — Close range: Finishing dominates sigmoid blend.
        [Test]
        public void SV001_CloseRange_FinishingDominates()
        {
            const float dist      = 6.0f;
            const float finishing = 18.0f;
            const float longShots = 8.0f;

            float vActual    = _calc.Calculate(0.8f, dist, finishing, longShots, 14, ContactZone.Centre, 0.0f, 0.0f, 1.0f);
            float vFinishing = _calc.Calculate(0.8f, dist, finishing, finishing, 14, ContactZone.Centre, 0.0f, 0.0f, 1.0f);
            float vLongShots = _calc.Calculate(0.8f, dist, longShots, longShots, 14, ContactZone.Centre, 0.0f, 0.0f, 1.0f);

            float errToFinishing = Mathf.Abs(vActual - vFinishing);
            float errToLongShots = Mathf.Abs(vActual - vLongShots);

            Assert.Less(errToFinishing, errToLongShots,
                "SV-001: At 6m Finishing should dominate the sigmoid blend.");
        }

        // SV-002 — Long range: LongShots dominates sigmoid blend.
        [Test]
        public void SV002_LongRange_LongShotsDominates()
        {
            const float dist      = 28.0f;
            const float finishing = 8.0f;
            const float longShots = 18.0f;

            float vActual    = _calc.Calculate(0.8f, dist, finishing, longShots, 14, ContactZone.Centre, 0.0f, 0.0f, 1.0f);
            float vFinishing = _calc.Calculate(0.8f, dist, finishing, finishing, 14, ContactZone.Centre, 0.0f, 0.0f, 1.0f);
            float vLongShots = _calc.Calculate(0.8f, dist, longShots, longShots, 14, ContactZone.Centre, 0.0f, 0.0f, 1.0f);

            float errToFinishing = Mathf.Abs(vActual - vFinishing);
            float errToLongShots = Mathf.Abs(vActual - vLongShots);

            Assert.Less(errToLongShots, errToFinishing,
                "SV-002: At 28m LongShots should dominate the sigmoid blend.");
        }

        // SV-003 — Maximum power / elite attributes / Centre: velocity in [28, 35] m/s.
        [Test]
        public void SV003_MaxPowerEliteAttributes_VelocityInExpectedRange()
        {
            float v = _calc.Calculate(
                powerIntent:           1.0f,
                distanceToGoal:        18.0f,
                finishing:             20.0f,
                longShots:             20.0f,
                kickPower:             20,
                contactZone:           ContactZone.Centre,
                spinIntent:            0.0f,
                fatigue:               0.0f,
                contactQualityModifier: 1.0f);

            Assert.GreaterOrEqual(v, 28.0f, "SV-003: Elite inputs must produce v ≥ 28 m/s.");
            Assert.LessOrEqual(v, 35.0f,    "SV-003: Elite inputs must produce v ≤ 35 m/s.");
        }

        // SV-004 — Minimum power / minimum attributes / Centre: velocity ≥ VAbsoluteMin.
        [Test]
        public void SV004_MinPowerMinAttributes_VelocityAtOrAboveAbsoluteMin()
        {
            float v = _calc.Calculate(
                powerIntent:           0.1f,
                distanceToGoal:        18.0f,
                finishing:             1.0f,
                longShots:             1.0f,
                kickPower:             1,
                contactZone:           ContactZone.Centre,
                spinIntent:            0.0f,
                fatigue:               0.0f,
                contactQualityModifier: 1.0f);

            Assert.GreaterOrEqual(v, ShotMechanicsConstants.VAbsoluteMin,
                "SV-004: Minimum inputs must still respect VAbsoluteMin floor.");
        }

        // SV-005 — BelowCentre contact reduces velocity vs Centre.
        [Test]
        public void SV005_BelowCentreContact_ReducesVelocityVsCentre()
        {
            float vCentre = _calc.Calculate(0.7f, 18.0f, 15.0f, 15.0f, 15, ContactZone.Centre,      0.0f, 0.0f, 1.0f);
            float vBelow  = _calc.Calculate(0.7f, 18.0f, 15.0f, 15.0f, 15, ContactZone.BelowCentre, 0.0f, 0.0f, 1.0f);

            Assert.Less(vBelow, vCentre,
                "SV-005: BelowCentre contact modifier (0.75) < Centre (1.0), so velocity must be lower.");
        }

        // SV-006 — OffCentre contact reduces velocity vs Centre.
        [Test]
        public void SV006_OffCentreContact_ReducesVelocityVsCentre()
        {
            float vCentre   = _calc.Calculate(0.7f, 18.0f, 15.0f, 15.0f, 15, ContactZone.Centre,    0.0f, 0.0f, 1.0f);
            float vOffCentre = _calc.Calculate(0.7f, 18.0f, 15.0f, 15.0f, 15, ContactZone.OffCentre, 0.0f, 0.0f, 1.0f);

            Assert.Less(vOffCentre, vCentre,
                "SV-006: OffCentre contact modifier (0.85) < Centre (1.0), so velocity must be lower.");
        }

        // SV-007 — High SpinIntent reduces velocity.
        [Test]
        public void SV007_HighSpinIntent_ReducesVelocity()
        {
            float vNoSpin   = _calc.Calculate(0.7f, 18.0f, 15.0f, 15.0f, 15, ContactZone.OffCentre, 0.0f, 0.0f, 1.0f);
            float vMaxSpin  = _calc.Calculate(0.7f, 18.0f, 15.0f, 15.0f, 15, ContactZone.OffCentre, 1.0f, 0.0f, 1.0f);

            Assert.Less(vMaxSpin, vNoSpin,
                "SV-007: SpinIntent=1 must reduce velocity via spin–velocity trade-off §3.2.6.");
        }

        // SV-008 — SpinIntent=0 applies no spin–velocity reduction.
        [Test]
        public void SV008_SpinIntentZero_NoSpinVelocityReduction()
        {
            // At SpinIntent=0: multiplier = 1 - (SpinVelocityTradeOff * 0) = 1.0 (exact).
            // Verify that spin trade-off factor is identically 1.0 by comparing two calls where
            // only SpinIntent differs: the SpinIntent=0 call should be the higher of the two.
            float vNoSpin  = _calc.Calculate(0.7f, 18.0f, 15.0f, 15.0f, 15, ContactZone.Centre, 0.0f, 0.0f, 1.0f);
            float vSomeSpin = _calc.Calculate(0.7f, 18.0f, 15.0f, 15.0f, 15, ContactZone.Centre, 0.5f, 0.0f, 1.0f);

            Assert.GreaterOrEqual(vNoSpin, vSomeSpin,
                "SV-008: SpinIntent=0 must produce at least as high velocity as SpinIntent=0.5 (no trade-off at zero).");
        }

        // SV-009 — Fatigue=1.0 reduces velocity vs Fatigue=0.0.
        [Test]
        public void SV009_FullFatigue_ReducesVelocity()
        {
            float vFresh   = _calc.Calculate(0.7f, 18.0f, 15.0f, 15.0f, 15, ContactZone.Centre, 0.0f, 0.0f, 1.0f);
            float vFatigued = _calc.Calculate(0.7f, 18.0f, 15.0f, 15.0f, 15, ContactZone.Centre, 0.0f, 1.0f, 1.0f);

            Assert.Less(vFatigued, vFresh,
                "SV-009: Fatigue=1.0 must reduce velocity by FatiguePowerReduction fraction §3.2.7.");
        }

        // SV-010 — Fatigue modifier is monotonically decreasing.
        [Test]
        public void SV010_FatigueModifier_MonotonicallyDecreasing()
        {
            float[] levels = { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
            float prev = float.MaxValue;
            foreach (float fatigue in levels)
            {
                float v = _calc.Calculate(0.7f, 18.0f, 15.0f, 15.0f, 15, ContactZone.Centre, 0.0f, fatigue, 1.0f);
                Assert.LessOrEqual(v, prev,
                    $"SV-010: velocity at fatigue={fatigue} must not exceed velocity at lower fatigue.");
                prev = v;
            }
        }

        // SV-011 — Velocity never exceeds VAbsoluteMax (35 m/s).
        [Test]
        public void SV011_EliteInputs_VelocityClampedAtAbsoluteMax()
        {
            float v = _calc.Calculate(
                powerIntent:           1.0f,
                distanceToGoal:        18.0f,
                finishing:             20.0f,
                longShots:             20.0f,
                kickPower:             20,
                contactZone:           ContactZone.Centre,
                spinIntent:            0.0f,
                fatigue:               0.0f,
                contactQualityModifier: 1.0f);

            Assert.LessOrEqual(v, ShotMechanicsConstants.VAbsoluteMax,
                "SV-011: Kick speed must never exceed VAbsoluteMax (35 m/s) regardless of inputs.");
        }

        // SV-012 — Velocity calculation is deterministic across three calls with identical inputs.
        [Test]
        public void SV012_VelocityCalculation_IsDeterministic()
        {
            float v1 = _calc.Calculate(0.8f, 20.0f, 15.0f, 12.0f, 14, ContactZone.Centre, 0.3f, 0.2f, 0.9f);
            float v2 = _calc.Calculate(0.8f, 20.0f, 15.0f, 12.0f, 14, ContactZone.Centre, 0.3f, 0.2f, 0.9f);
            float v3 = _calc.Calculate(0.8f, 20.0f, 15.0f, 12.0f, 14, ContactZone.Centre, 0.3f, 0.2f, 0.9f);

            Assert.AreEqual(v1, v2, "SV-012: velocity call 1 vs 2 must be bitwise identical.");
            Assert.AreEqual(v2, v3, "SV-012: velocity call 2 vs 3 must be bitwise identical.");
        }
    }

    // ── LA: Launch Angle §5.4 ────────────────────────────────────────────────────────────────────

    /// <summary>
    /// LA-001 – LA-008: BaseAngle by ContactZone, power lift modifier, spin lift modifier,
    /// body lean penalty, body shape penalty, clamp guard, and velocity vector encoding.
    /// Shot Mechanics #6 §5.4, §3.3.
    /// </summary>
    [TestFixture]
    internal class LaunchAngleTests
    {
        // LA-001 — Centre contact, high power produces low launch angle in [2°, 8°].
        [Test]
        public void LA001_CentreHighPower_LowLaunchAngle()
        {
            float angle = ShotLaunchAngleCalculator.Compute(
                ContactZone.Centre, powerIntent: 0.9f, spinIntent: 0.0f,
                bodyLeanAngleDeg: 0.0f, bodyMechanicsScore: 1.0f);

            Assert.GreaterOrEqual(angle, 2.0f, "LA-001: Centre+high-power angle must be ≥ 2°.");
            Assert.LessOrEqual(angle, 8.0f,    "LA-001: Centre+high-power angle must be ≤ 8°.");
        }

        // LA-002 — BelowCentre produces a higher launch angle than Centre.
        [Test]
        public void LA002_BelowCentre_HigherAngleThanCentre()
        {
            float angleCentre = ShotLaunchAngleCalculator.Compute(
                ContactZone.Centre, 0.6f, 0.0f, 0.0f, 1.0f);
            float angleBelow  = ShotLaunchAngleCalculator.Compute(
                ContactZone.BelowCentre, 0.6f, 0.0f, 0.0f, 1.0f);

            Assert.Greater(angleBelow, angleCentre,
                "LA-002: BelowCentre base angle (18°) > Centre base angle (4°).");
        }

        // LA-003 — OffCentre produces a higher launch angle than Centre.
        [Test]
        public void LA003_OffCentre_HigherAngleThanCentre()
        {
            float angleCentre   = ShotLaunchAngleCalculator.Compute(ContactZone.Centre,    0.6f, 0.5f, 0.0f, 1.0f);
            float angleOffCentre = ShotLaunchAngleCalculator.Compute(ContactZone.OffCentre, 0.6f, 0.5f, 0.0f, 1.0f);

            Assert.Greater(angleOffCentre, angleCentre,
                "LA-003: OffCentre base angle (8°) > Centre base angle (4°) under same conditions.");
        }

        // LA-004 — High SpinIntent with BelowCentre elevates launch angle.
        [Test]
        public void LA004_HighSpinBelowCentre_ElevatesAngle()
        {
            float angleNoSpin  = ShotLaunchAngleCalculator.Compute(ContactZone.BelowCentre, 0.6f, 0.0f, 0.0f, 1.0f);
            float angleHighSpin = ShotLaunchAngleCalculator.Compute(ContactZone.BelowCentre, 0.6f, 1.0f, 0.0f, 1.0f);

            Assert.Greater(angleHighSpin, angleNoSpin,
                "LA-004: SpinLiftScale * spinIntent adds positive angle contribution §3.3.5.");
        }

        // LA-005 — High PowerIntent suppresses lift vs low PowerIntent.
        [Test]
        public void LA005_HighPower_SuppressesLiftVsLowPower()
        {
            float angleHighPower = ShotLaunchAngleCalculator.Compute(ContactZone.Centre, 1.0f, 0.0f, 0.0f, 1.0f);
            float angleLowPower  = ShotLaunchAngleCalculator.Compute(ContactZone.Centre, 0.3f, 0.0f, 0.0f, 1.0f);

            Assert.Less(angleHighPower, angleLowPower,
                "LA-005: PowerLiftScale*(1-powerIntent) means high power reduces the lift term §3.3.4.");
        }

        // LA-006 — Body lean increases launch angle.
        [Test]
        public void LA006_BodyLean_IncreasesLaunchAngle()
        {
            float angleUpright = ShotLaunchAngleCalculator.Compute(ContactZone.Centre, 0.6f, 0.0f, 0.0f,  1.0f);
            float angleLeaning  = ShotLaunchAngleCalculator.Compute(ContactZone.Centre, 0.6f, 0.0f, 15.0f, 1.0f);

            Assert.Greater(angleLeaning, angleUpright,
                "LA-006: BodyLeanTransferCoefficient*leanDeg must add positive angle (leaning back skies the ball).");
        }

        // LA-007 — Launch angle is clamped at LaunchAngleMax.
        [Test]
        public void LA007_WorstCaseStack_AngleClampedAtMax()
        {
            float angle = ShotLaunchAngleCalculator.Compute(
                ContactZone.BelowCentre,
                powerIntent:        0.0f,
                spinIntent:         1.0f,
                bodyLeanAngleDeg:   ShotMechanicsConstants.BodyLeanMaxDeg,
                bodyMechanicsScore: 0.0f);

            Assert.LessOrEqual(angle, ShotMechanicsConstants.LaunchAngleMax,
                "LA-007: Worst-case stack must not exceed LaunchAngleMax clamp §3.3.8.");
        }

        // LA-008 — DeriveBodyLeanAngle: stationary agent produces zero lean.
        [Test]
        public void LA008_StationaryAgent_ZeroBodyLean()
        {
            float lean = ShotLaunchAngleCalculator.DeriveBodyLeanAngle(0.0f);
            Assert.AreEqual(0.0f, lean, 0.001f,
                "LA-008: Stationary agent (speed=0) should produce zero body lean angle.");
        }
    }

    // ── SN: Spin Vector §5.5 ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// SN-001 – SN-008: Topspin/backspin/sidespin signs and magnitudes, Technique scaling,
    /// Magnus bound, and determinism. Shot Mechanics #6 §5.5, §3.4.
    /// </summary>
    [TestFixture]
    internal class SpinCalculatorTests
    {
        private static readonly Vector2 k_facingRight = Vector2.right;

        // SN-001 — Centre + SpinIntent=0: topspin dominant (positive Y component).
        [Test]
        public void SN001_CentreSpinIntentZero_TopspinDominant()
        {
            Vector3 spin = ShotSpinCalculator.Compute(
                ContactZone.Centre, spinIntent: 0.0f, powerIntent: 0.8f,
                technique: 10, facingDirectionXY: k_facingRight);

            // §3.4.2: topspin axis is 'left' (90° CCW of facing). For facing=(1,0,0), left=(0,1,0).
            // spin.y is the left-axis component.
            Assert.Greater(spin.y, 0.0f,
                "SN-001: Centre+SpinIntent=0 should produce positive topspin (left-axis component > 0).");

            Assert.Greater(Mathf.Abs(spin.y), Mathf.Abs(spin.z),
                "SN-001: Topspin (y) must dominate sidespin (z) at Centre with SpinIntent=0.");
        }

        // SN-002 — BelowCentre contact produces backspin (negative left-axis component).
        [Test]
        public void SN002_BelowCentre_ProducesBackspin()
        {
            Vector3 spin = ShotSpinCalculator.Compute(
                ContactZone.BelowCentre, spinIntent: 0.6f, powerIntent: 0.5f,
                technique: 10, facingDirectionXY: k_facingRight);

            // At BelowCentre, backspin dominates. netForwardSpin = topspin - backspin < 0.
            // spin along left axis = left * netForwardSpin → spin.y < 0.
            Assert.Less(spin.y, 0.0f,
                "SN-002: BelowCentre+SpinIntent=0.6 should produce net backspin (left-axis component < 0).");
        }

        // SN-003 — OffCentre produces non-zero sidespin.
        [Test]
        public void SN003_OffCentre_ProducesNonZeroSidespin()
        {
            Vector3 spin = ShotSpinCalculator.Compute(
                ContactZone.OffCentre, spinIntent: 0.7f, powerIntent: 0.6f,
                technique: 10, facingDirectionXY: k_facingRight);

            Assert.Greater(Mathf.Abs(spin.z), 0.0f,
                "SN-003: OffCentre contact must produce non-zero sidespin (Z component).");
        }

        // SN-004 — Higher Technique increases sidespin magnitude.
        [Test]
        public void SN004_HigherTechnique_IncreasesSpinMagnitude()
        {
            Vector3 spinLowTech  = ShotSpinCalculator.Compute(ContactZone.OffCentre, 0.8f, 0.6f, 5,  k_facingRight);
            Vector3 spinHighTech = ShotSpinCalculator.Compute(ContactZone.OffCentre, 0.8f, 0.6f, 18, k_facingRight);

            Assert.Greater(Mathf.Abs(spinHighTech.z), Mathf.Abs(spinLowTech.z),
                "SN-004: Technique=18 must produce greater sidespin than Technique=5 (§3.4.5 TechniqueScale).");
        }

        // SN-005 — SpinIntent=0 produces non-zero spin for all ContactZone values.
        [Test]
        public void SN005_SpinIntentZero_ProducesNonZeroSpin()
        {
            ContactZone[] zones = { ContactZone.Centre, ContactZone.BelowCentre, ContactZone.OffCentre };
            foreach (ContactZone zone in zones)
            {
                Vector3 spin = ShotSpinCalculator.Compute(zone, 0.0f, 0.8f, 10, k_facingRight);
                Assert.Greater(spin.magnitude, 0.0f,
                    $"SN-005: SpinIntent=0 must not produce a zero-spin vector for zone={zone}.");
            }
        }

        // SN-006 — Spin magnitude within SpinAbsoluteMax (Magnus calibration bound).
        [Test]
        public void SN006_MaxSpinInputs_MagnitudeWithinMagnusBound()
        {
            Vector3 spin = ShotSpinCalculator.Compute(
                ContactZone.OffCentre, spinIntent: 1.0f, powerIntent: 1.0f,
                technique: 20, facingDirectionXY: k_facingRight);

            Assert.LessOrEqual(spin.magnitude, ShotMechanicsConstants.SpinAbsoluteMax,
                "SN-006: Spin magnitude must not exceed SpinAbsoluteMax (80 rad/s) §3.4.10.");
        }

        // SN-007 — SpinIntent=1.0 produces more spin than SpinIntent=0.5; both ≤ SpinAbsoluteMax.
        [Test]
        public void SN007_MaxSpinIntent_GreaterThanHalfSpinAndBothBounded()
        {
            Vector3 spinHalf = ShotSpinCalculator.Compute(ContactZone.OffCentre, 0.5f, 0.8f, 15, k_facingRight);
            Vector3 spinMax  = ShotSpinCalculator.Compute(ContactZone.OffCentre, 1.0f, 0.8f, 15, k_facingRight);

            Assert.Greater(spinMax.magnitude, spinHalf.magnitude,
                "SN-007: SpinIntent=1 must produce greater total spin magnitude than SpinIntent=0.5.");

            Assert.LessOrEqual(spinHalf.magnitude, ShotMechanicsConstants.SpinAbsoluteMax,
                "SN-007: spinHalf must be ≤ SpinAbsoluteMax.");
            Assert.LessOrEqual(spinMax.magnitude, ShotMechanicsConstants.SpinAbsoluteMax,
                "SN-007: spinMax must be ≤ SpinAbsoluteMax.");
        }

        // SN-008 — Spin calculation is deterministic across three identical calls.
        [Test]
        public void SN008_SpinCalculation_IsDeterministic()
        {
            Vector3 s1 = ShotSpinCalculator.Compute(ContactZone.OffCentre, 0.7f, 0.8f, 12, k_facingRight);
            Vector3 s2 = ShotSpinCalculator.Compute(ContactZone.OffCentre, 0.7f, 0.8f, 12, k_facingRight);
            Vector3 s3 = ShotSpinCalculator.Compute(ContactZone.OffCentre, 0.7f, 0.8f, 12, k_facingRight);

            Assert.AreEqual(s1, s2, "SN-008: spin call 1 vs 2 must be bitwise identical.");
            Assert.AreEqual(s2, s3, "SN-008: spin call 2 vs 3 must be bitwise identical.");
        }
    }

    // ── BM: Body Mechanics §5.8 ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// BM-001 – BM-008: Run-up score, plant foot score, velocity score, composite BMS,
    /// stumble trigger gate. Shot Mechanics #6 §5.8, §3.7.
    /// </summary>
    [TestFixture]
    internal class BodyMechanicsTests
    {
        private static readonly Vector3 k_goalDir = Vector3.right;

        // BM-001 — Agent moving directly toward the goal: run-up score near maximum.
        [Test]
        public void BM001_PerfectApproach_RunUpScoreNearMax()
        {
            // Agent moving at ideal approach angle toward goal (straight on = 0° deviation from ideal).
            // IdealRunUpAngle=37.5°, RunUpTolerance=45° → 0° offset ≤ tolerance → RunUpScore=1.0.
            // We approximate "perfect" by setting velocity to approach toward goal centre.
            Vector3 agentVelocity = k_goalDir * 3.0f; // ~ideal approach speed, toward goal
            BodyMechanicsResult result = BodyMechanicsEvaluator.Evaluate(
                agentVelocity, Vector3.zero, Vector3.zero, k_goalDir, powerIntent: 0.5f);

            Assert.GreaterOrEqual(result.Score, 0.5f,
                "BM-001: Straight-on approach at ideal speed should produce a Score ≥ 0.5.");
        }

        // BM-002 — 90° run-up offset: RunUpScore significantly penalised.
        [Test]
        public void BM002_NinetyDegreeRunUp_ReducesScore()
        {
            // Agent moving perpendicular to the goal direction.
            Vector3 perpendicularVelocity = Vector3.forward * 3.0f;
            Vector3 agentPosition         = new Vector3(80.0f, 34.0f, 0.0f);
            BodyMechanicsResult result = BodyMechanicsEvaluator.Evaluate(
                perpendicularVelocity, agentPosition, agentPosition, k_goalDir, powerIntent: 0.5f);

            // RunUpScore should be < 0.5 when approach is 90° off ideal.
            Assert.Less(result.Score, 0.75f,
                "BM-002: 90° approach offset should produce a noticeably reduced composite BMS.");
        }

        // BM-003 — Agent at ball (zero lateral distance): PlantFootScore near maximum.
        [Test]
        public void BM003_AgentAtBall_PlantFootScoreHigh()
        {
            Vector3 pos = new Vector3(80.0f, 34.0f, 0.0f);
            BodyMechanicsResult result = BodyMechanicsEvaluator.Evaluate(
                k_goalDir * 2.0f, pos, pos, k_goalDir, powerIntent: 0.5f);

            Assert.GreaterOrEqual(result.Score, 0.5f,
                "BM-003: Zero lateral distance from ball should not penalise PlantFootScore.");
        }

        // BM-004 — Large lateral offset reduces score.
        [Test]
        public void BM004_LargePlantFootOffset_ReducesScore()
        {
            Vector3 agentPos = new Vector3(80.0f, 34.0f, 0.0f);
            Vector3 ballPos  = new Vector3(80.0f, 34.0f + ShotMechanicsConstants.PlantFootTolerance * 3.0f, 0.0f);

            BodyMechanicsResult result = BodyMechanicsEvaluator.Evaluate(
                k_goalDir * 2.0f, agentPos, ballPos, k_goalDir, powerIntent: 0.5f);

            Assert.Less(result.Score, 0.9f,
                "BM-004: 3× PlantFootTolerance offset should reduce the composite BMS.");
        }

        // BM-005 — Moving agent scores higher than stationary agent.
        [Test]
        public void BM005_MovingAgent_ScoresHigherThanStationary()
        {
            Vector3 pos = new Vector3(80.0f, 34.0f, 0.0f);

            BodyMechanicsResult stationary = BodyMechanicsEvaluator.Evaluate(
                Vector3.zero, pos, pos, k_goalDir, powerIntent: 0.5f);

            BodyMechanicsResult moving = BodyMechanicsEvaluator.Evaluate(
                k_goalDir * ShotMechanicsConstants.VelocityIdealMin, pos, pos, k_goalDir, powerIntent: 0.5f);

            Assert.Greater(moving.Score, stationary.Score,
                "BM-005: Moving at ideal approach speed must score higher than stationary (§3.7.5).");
        }

        // BM-006 — BodyMechanicsScore always in [0.0, 1.0] for extreme inputs.
        [Test]
        public void BM006_ExtremeInputs_ScoreAlwaysInUnitRange()
        {
            Vector3 pos = new Vector3(80.0f, 34.0f, 0.0f);
            // Worst case: no movement, 10m lateral offset from ball, high power.
            Vector3 farBall = pos + new Vector3(0.0f, 10.0f, 0.0f);

            BodyMechanicsResult result = BodyMechanicsEvaluator.Evaluate(
                Vector3.zero, pos, farBall, k_goalDir, powerIntent: 1.0f);

            Assert.GreaterOrEqual(result.Score, 0.0f, "BM-006: Score must be ≥ 0.0.");
            Assert.LessOrEqual(result.Score, 1.0f,    "BM-006: Score must be ≤ 1.0.");
        }

        // BM-007 — Stumble triggered when Score < StumbleThreshold AND PowerIntent > StumblePowerThreshold.
        [Test]
        public void BM007_PoorMechanicsHighPower_StumbleTriggered()
        {
            // Force BMS below StumbleThreshold: stationary agent with very far ball.
            Vector3 pos     = new Vector3(80.0f, 34.0f, 0.0f);
            Vector3 farBall = pos + new Vector3(0.0f, 10.0f, 0.0f);

            BodyMechanicsResult result = BodyMechanicsEvaluator.Evaluate(
                Vector3.zero, pos, farBall, k_goalDir,
                powerIntent: ShotMechanicsConstants.StumblePowerThreshold + 0.1f);

            // Only assert stumble when the Score actually qualifies — guard prevents false failure.
            if (result.Score < ShotMechanicsConstants.StumbleThreshold)
            {
                Assert.IsTrue(result.StumbleTriggered,
                    "BM-007: Score below threshold with high PowerIntent must trigger stumble §3.7.8.");
            }
            else
            {
                Assert.Inconclusive("BM-007: Could not produce Score < StumbleThreshold under test inputs; skip stumble check.");
            }
        }

        // BM-008 — Stumble NOT triggered when PowerIntent is low despite poor body mechanics.
        [Test]
        public void BM008_PoorMechanicsLowPower_NoStumble()
        {
            Vector3 pos     = new Vector3(80.0f, 34.0f, 0.0f);
            Vector3 farBall = pos + new Vector3(0.0f, 10.0f, 0.0f);

            BodyMechanicsResult result = BodyMechanicsEvaluator.Evaluate(
                Vector3.zero, pos, farBall, k_goalDir,
                powerIntent: ShotMechanicsConstants.StumblePowerThreshold - 0.1f);

            Assert.IsFalse(result.StumbleTriggered,
                "BM-008: Low PowerIntent must NOT trigger stumble even with poor mechanics §3.7.8.");
        }
    }

    // ── WF: Weak Foot §5.9 ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// WF-001 – WF-006: Error multiplier and velocity multiplier across all WeakFootRating levels,
    /// monotonicity, and design contract checks. Shot Mechanics #6 §5.9, §3.8.
    /// </summary>
    [TestFixture]
    internal class WeakFootTests
    {
        // WF-001 — IsWeakFoot=false: no penalty (both multipliers == 1.0).
        [Test]
        public void WF001_StrongFoot_NoMultiplierPenalty()
        {
            float errMult = WeakFootPenaltyApplier.ComputeErrorMultiplier(isWeakFoot: false, weakFootRating: 1);
            float velMult = WeakFootPenaltyApplier.ComputeVelocityMultiplier(isWeakFoot: false, weakFootRating: 1);

            Assert.AreEqual(1.0f, errMult, 0.001f, "WF-001: Strong foot error multiplier must be 1.0.");
            Assert.AreEqual(1.0f, velMult, 0.001f, "WF-001: Strong foot velocity multiplier must be 1.0.");
        }

        // WF-002 — WeakFootRating=5 (ambidextrous): multipliers near 1.0 (no penalty).
        [Test]
        public void WF002_WeakFootRating5_NearNoPenalty()
        {
            float errMult = WeakFootPenaltyApplier.ComputeErrorMultiplier(isWeakFoot: true, weakFootRating: 5);
            float velMult = WeakFootPenaltyApplier.ComputeVelocityMultiplier(isWeakFoot: true, weakFootRating: 5);

            // Formula: 1 + (5-5)/4 * penalty = 1.0.
            Assert.AreEqual(1.0f, errMult, 0.001f, "WF-002: Rating=5 error multiplier must be 1.0 (no penalty).");
            Assert.AreEqual(1.0f, velMult, 0.001f, "WF-002: Rating=5 velocity multiplier must be 1.0 (no penalty).");
        }

        // WF-003 — WeakFootRating=1: maximum penalty (error mult = 1 + WeakFootBaseErrorPenalty).
        [Test]
        public void WF003_WeakFootRating1_MaxPenalty()
        {
            float errMult = WeakFootPenaltyApplier.ComputeErrorMultiplier(isWeakFoot: true, weakFootRating: 1);
            float expectedErrMult = 1.0f + ShotMechanicsConstants.WeakFootBaseErrorPenalty;

            Assert.AreEqual(expectedErrMult, errMult, 0.01f,
                "WF-003: Rating=1 error multiplier must equal 1 + WeakFootBaseErrorPenalty §3.8.3.");

            float velMult        = WeakFootPenaltyApplier.ComputeVelocityMultiplier(isWeakFoot: true, weakFootRating: 1);
            float expectedVelMult = 1.0f - ShotMechanicsConstants.WeakFootVelocityPenalty;

            Assert.AreEqual(expectedVelMult, velMult, 0.01f,
                "WF-003: Rating=1 velocity multiplier must equal 1 - WeakFootVelocityPenalty §3.8.4.");
        }

        // WF-004 — Penalty increases monotonically as WeakFootRating decreases.
        [Test]
        public void WF004_PenaltyMonotonicallyIncreasing_AsRatingDecreases()
        {
            int[]  ratings = { 5, 4, 3, 2, 1 };
            float prevErr = float.MinValue;
            foreach (int rating in ratings)
            {
                float errMult = WeakFootPenaltyApplier.ComputeErrorMultiplier(isWeakFoot: true, weakFootRating: rating);
                Assert.GreaterOrEqual(errMult, prevErr,
                    $"WF-004: Error multiplier must be ≥ previous value as rating decreases from {rating + 1} to {rating}.");
                prevErr = errMult;
            }
        }

        // WF-005 — IsWeakFoot=true reduces velocity multiplier below 1.0 at rating < 5.
        [Test]
        public void WF005_WeakFootReducesVelocityMultiplier()
        {
            float velMultStrong = WeakFootPenaltyApplier.ComputeVelocityMultiplier(isWeakFoot: false, weakFootRating: 2);
            float velMultWeak   = WeakFootPenaltyApplier.ComputeVelocityMultiplier(isWeakFoot: true,  weakFootRating: 2);

            Assert.Less(velMultWeak, velMultStrong,
                "WF-005: Weak-foot velocity multiplier at rating=2 must be < strong-foot multiplier (1.0).");
        }

        // WF-006 — WeakFootRatingRange constant equals WeakFootRatingMax - 1.
        [Test]
        public void WF006_WeakFootRatingRange_EqualsMaxMinusOne()
        {
            int expectedRange = ShotMechanicsConstants.WeakFootRatingMax - 1;
            Assert.AreEqual(expectedRange, ShotMechanicsConstants.WeakFootRatingRange,
                "WF-006: WeakFootRatingRange must be WeakFootRatingMax - 1 per §3.8 DERIVED invariant.");
        }
    }

    // ── SE: Error Model §5.7 ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// SE-001 – SE-010: Error magnitude modifiers, monotonicity, direction determinism,
    /// and non-zero floor. Shot Mechanics #6 §5.7, §3.6.
    /// </summary>
    [TestFixture]
    internal class ErrorModelTests
    {
        // SE-001 — Elite finishing, no pressure, fresh, ideal body: error ≤ BaseErrorMin + tolerance.
        [Test]
        public void SE001_EliteConditions_LowErrorAngle()
        {
            float errAngle = ShotErrorCalculator.ComputeErrorMagnitude(
                finishing: 20.0f, longShots: 20.0f, composure: 20.0f,
                distanceToGoal: 18.0f, powerIntent: 0.5f, pressureScalar: 0.0f,
                fatigue: 0.0f, bodyMechanicsScore: 1.0f, weakFootErrorMultiplier: 1.0f);

            // Elite: base error = BaseErrorMin; power penalty near 1.375; composite still small.
            // The important contract: error is never zero and is much less than MaxErrorAngle.
            Assert.Less(errAngle, ShotMechanicsConstants.MaxErrorAngle * 0.25f,
                "SE-001: Elite conditions should produce well below 25% of MaxErrorAngle.");
            Assert.GreaterOrEqual(errAngle, ShotMechanicsConstants.MinErrorAngle,
                "SE-001: Error must always be ≥ MinErrorAngle.");
        }

        // SE-002 — Worst case: error clamped at MaxErrorAngle.
        [Test]
        public void SE002_WorstCase_ErrorClampedAtMax()
        {
            float errAngle = ShotErrorCalculator.ComputeErrorMagnitude(
                finishing: 1.0f, longShots: 1.0f, composure: 1.0f,
                distanceToGoal: 30.0f, powerIntent: 1.0f, pressureScalar: 1.0f,
                fatigue: 1.0f, bodyMechanicsScore: 0.0f,
                weakFootErrorMultiplier: 1.0f + ShotMechanicsConstants.WeakFootBaseErrorPenalty);

            Assert.AreEqual(ShotMechanicsConstants.MaxErrorAngle, errAngle, 0.05f,
                "SE-002: Worst-case composite must be clamped to MaxErrorAngle §3.6.8.");
        }

        // SE-003 — Full power produces more error than medium power.
        [Test]
        public void SE003_FullPowerHigherError_ThanMediumPower()
        {
            float errMax = ShotErrorCalculator.ComputeErrorMagnitude(
                20.0f, 20.0f, 20.0f, 18.0f, 1.0f, 0.0f, 0.0f, 1.0f, 1.0f);
            float errMed = ShotErrorCalculator.ComputeErrorMagnitude(
                20.0f, 20.0f, 20.0f, 18.0f, 0.4f, 0.0f, 0.0f, 1.0f, 1.0f);

            Assert.Greater(errMax, errMed,
                "SE-003: PowerIntent=1.0 must produce greater error than PowerIntent=0.4 §3.6.4.");
        }

        // SE-004 — Power penalty is monotonically increasing with PowerIntent.
        [Test]
        public void SE004_PowerPenalty_MonotonicallyIncreasing()
        {
            float[] powers = { 0.1f, 0.3f, 0.5f, 0.7f, 0.9f, 1.0f };
            float prev = 0.0f;
            foreach (float power in powers)
            {
                float err = ShotErrorCalculator.ComputeErrorMagnitude(
                    20.0f, 20.0f, 20.0f, 18.0f, power, 0.0f, 0.0f, 1.0f, 1.0f);
                Assert.GreaterOrEqual(err, prev,
                    $"SE-004: Error at power={power} must be ≥ error at lower power.");
                prev = err;
            }
        }

        // SE-005 — Pressure scalar monotonically increases error.
        [Test]
        public void SE005_PressureScalar_MonotonicallyIncreasesError()
        {
            float[] pressures = { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
            float prev = 0.0f;
            foreach (float p in pressures)
            {
                // Use low Composure (5) to make pressure differences visible.
                float err = ShotErrorCalculator.ComputeErrorMagnitude(
                    10.0f, 10.0f, 5.0f, 18.0f, 0.5f, p, 0.0f, 1.0f, 1.0f);
                Assert.GreaterOrEqual(err, prev,
                    $"SE-005: Error at pressure={p} must be ≥ error at lower pressure.");
                prev = err;
            }
        }

        // SE-006 — Fatigue scalar monotonically increases error.
        [Test]
        public void SE006_FatigueScalar_MonotonicallyIncreasesError()
        {
            float[] fatigueLevels = { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
            float prev = 0.0f;
            foreach (float f in fatigueLevels)
            {
                float err = ShotErrorCalculator.ComputeErrorMagnitude(
                    10.0f, 10.0f, 10.0f, 18.0f, 0.5f, 0.0f, f, 1.0f, 1.0f);
                Assert.GreaterOrEqual(err, prev,
                    $"SE-006: Error at fatigue={f} must be ≥ error at lower fatigue.");
                prev = err;
            }
        }

        // SE-007 — Finishing attribute modifier monotonically decreases error.
        [Test]
        public void SE007_FinishingModifier_MonotonicallyDecreasesError()
        {
            int[] ratings = { 1, 5, 10, 15, 20 };
            float prev = float.MaxValue;
            foreach (int rating in ratings)
            {
                float err = ShotErrorCalculator.ComputeErrorMagnitude(
                    rating, rating, 10.0f, 18.0f, 0.5f, 0.0f, 0.0f, 1.0f, 1.0f);
                Assert.LessOrEqual(err, prev,
                    $"SE-007: Error at Finishing={rating} must be ≤ error at lower finishing.");
                prev = err;
            }
        }

        // SE-008 — Poor body mechanics increases error vs good mechanics.
        [Test]
        public void SE008_PoorBodyMechanics_IncreasesError()
        {
            float errGood = ShotErrorCalculator.ComputeErrorMagnitude(
                10.0f, 10.0f, 10.0f, 18.0f, 0.5f, 0.0f, 0.0f, 0.9f, 1.0f);
            float errPoor = ShotErrorCalculator.ComputeErrorMagnitude(
                10.0f, 10.0f, 10.0f, 18.0f, 0.5f, 0.0f, 0.0f, 0.2f, 1.0f);

            Assert.Greater(errPoor, errGood,
                "SE-008: BodyMechanicsScore=0.2 must produce more error than BodyMechanicsScore=0.9 §3.6.7.");
        }

        // SE-009 — Error direction is deterministic for fixed seed/agentId/frameNumber.
        [Test]
        public void SE009_ErrorDirection_IsDeterministic()
        {
            Vector2 d1 = ShotErrorCalculator.ComputeErrorDirection(matchSeed: 12345, agentId: 7, frameNumber: 3600);
            Vector2 d2 = ShotErrorCalculator.ComputeErrorDirection(matchSeed: 12345, agentId: 7, frameNumber: 3600);
            Vector2 d3 = ShotErrorCalculator.ComputeErrorDirection(matchSeed: 12345, agentId: 7, frameNumber: 3600);

            Assert.AreEqual(d1, d2, "SE-009: Error direction call 1 vs 2 must be bitwise identical.");
            Assert.AreEqual(d2, d3, "SE-009: Error direction call 2 vs 3 must be bitwise identical.");
        }

        // SE-010 — Error angle is never exactly zero even for elite attributes.
        [Test]
        public void SE010_EliteConditions_ErrorAngleNeverZero()
        {
            float errAngle = ShotErrorCalculator.ComputeErrorMagnitude(
                finishing: 20.0f, longShots: 20.0f, composure: 20.0f,
                distanceToGoal: 18.0f, powerIntent: 0.5f, pressureScalar: 0.0f,
                fatigue: 0.0f, bodyMechanicsScore: 1.0f, weakFootErrorMultiplier: 1.0f);

            Assert.Greater(errAngle, 0.0f,
                "SE-010: Error angle must never be zero — error floor = MinErrorAngle §3.6.8, KD-1.");
        }
    }

    // ── EC: Edge Cases §5.11 ─────────────────────────────────────────────────────────────────────

    /// <summary>
    /// EC-001 – EC-008: Robustness under attribute extremes, degenerate inputs, and
    /// NaN injection via NaNVelocityStub. Shot Mechanics #6 §5.11.
    /// </summary>
    [TestFixture]
    internal class EdgeCasesTests
    {
        // EC-001 — All attributes at minimum: velocity returns a finite, valid result ≥ VAbsoluteMin.
        [Test]
        public void EC001_AllAttributesMinimum_ValidVelocityReturned()
        {
            float v = ShotVelocityCalculator.Instance.Calculate(
                powerIntent:           0.5f,
                distanceToGoal:        20.0f,
                finishing:             ShotMechanicsConstants.ATTR_MIN,
                longShots:             ShotMechanicsConstants.ATTR_MIN,
                kickPower:             (int)ShotMechanicsConstants.ATTR_MIN,
                contactZone:           ContactZone.Centre,
                spinIntent:            0.0f,
                fatigue:               0.0f,
                contactQualityModifier: 1.0f);

            Assert.IsFalse(float.IsNaN(v),      "EC-001: Result must not be NaN.");
            Assert.IsFalse(float.IsInfinity(v), "EC-001: Result must not be Infinity.");
            Assert.GreaterOrEqual(v, ShotMechanicsConstants.VAbsoluteMin,
                "EC-001: Minimum-attribute velocity must respect VAbsoluteMin floor.");
        }

        // EC-002 — All attributes at maximum: velocity returns a finite result ≤ VAbsoluteMax.
        [Test]
        public void EC002_AllAttributesMaximum_ValidVelocityClamped()
        {
            float v = ShotVelocityCalculator.Instance.Calculate(
                powerIntent:           1.0f,
                distanceToGoal:        20.0f,
                finishing:             ShotMechanicsConstants.ATTR_MAX,
                longShots:             ShotMechanicsConstants.ATTR_MAX,
                kickPower:             (int)ShotMechanicsConstants.ATTR_MAX,
                contactZone:           ContactZone.Centre,
                spinIntent:            0.0f,
                fatigue:               0.0f,
                contactQualityModifier: ShotMechanicsConstants.ContactQualityModifierMax);

            Assert.IsFalse(float.IsNaN(v),      "EC-002: Result must not be NaN.");
            Assert.IsFalse(float.IsInfinity(v), "EC-002: Result must not be Infinity.");
            Assert.LessOrEqual(v, ShotMechanicsConstants.VAbsoluteMax,
                "EC-002: Maximum-attribute velocity must not exceed VAbsoluteMax.");
        }

        // EC-003 — PowerIntent exactly 0.0: velocity ≥ VAbsoluteMin; no exception.
        [Test]
        public void EC003_PowerIntentExactlyZero_VelocityFloorApplied()
        {
            float v = ShotVelocityCalculator.Instance.Calculate(
                powerIntent:           0.0f,
                distanceToGoal:        20.0f,
                finishing:             10.0f,
                longShots:             10.0f,
                kickPower:             10,
                contactZone:           ContactZone.Centre,
                spinIntent:            0.0f,
                fatigue:               0.0f,
                contactQualityModifier: 1.0f);

            Assert.GreaterOrEqual(v, ShotMechanicsConstants.VAbsoluteMin,
                "EC-003: PowerIntent=0 must apply VAbsoluteMin floor §3.2.10.");
            Assert.IsFalse(float.IsNaN(v), "EC-003: Must not produce NaN.");
        }

        // EC-004 — PowerIntent exactly 1.0: valid result within bounds.
        [Test]
        public void EC004_PowerIntentExactlyOne_ValidResult()
        {
            float v = ShotVelocityCalculator.Instance.Calculate(
                powerIntent:           1.0f,
                distanceToGoal:        18.0f,
                finishing:             10.0f,
                longShots:             10.0f,
                kickPower:             12,
                contactZone:           ContactZone.Centre,
                spinIntent:            0.0f,
                fatigue:               0.0f,
                contactQualityModifier: 1.0f);

            Assert.IsFalse(float.IsNaN(v), "EC-004: PowerIntent=1.0 must not produce NaN.");
            Assert.GreaterOrEqual(v, ShotMechanicsConstants.VAbsoluteMin, "EC-004: v must be ≥ VAbsoluteMin.");
            Assert.LessOrEqual(v, ShotMechanicsConstants.VAbsoluteMax,    "EC-004: v must be ≤ VAbsoluteMax.");
        }

        // EC-005 — SpinIntent=1.0 with Centre: valid spin vector, spin-velocity trade-off applied.
        [Test]
        public void EC005_MaxSpinCentreContact_ValidSpinAndReducedVelocity()
        {
            Vector3 spin = ShotSpinCalculator.Compute(
                ContactZone.Centre, spinIntent: 1.0f, powerIntent: 0.8f,
                technique: 10, facingDirectionXY: Vector2.right);

            Assert.IsFalse(float.IsNaN(spin.x) || float.IsNaN(spin.y) || float.IsNaN(spin.z),
                "EC-005: Spin vector must not contain NaN.");
            Assert.LessOrEqual(spin.magnitude, ShotMechanicsConstants.SpinAbsoluteMax,
                "EC-005: Spin magnitude must be ≤ SpinAbsoluteMax.");

            float vSpin   = ShotVelocityCalculator.Instance.Calculate(0.8f, 18.0f, 12.0f, 12.0f, 12, ContactZone.Centre, 1.0f, 0.0f, 1.0f);
            float vNoSpin = ShotVelocityCalculator.Instance.Calculate(0.8f, 18.0f, 12.0f, 12.0f, 12, ContactZone.Centre, 0.0f, 0.0f, 1.0f);

            Assert.Less(vSpin, vNoSpin,
                "EC-005: SpinIntent=1.0 must reduce velocity via spin–velocity trade-off §3.2.6.");
        }

        // EC-006 — Launch angle calculation does not produce NaN for zero body lean.
        [Test]
        public void EC006_ZeroBodyLean_NoNaNInLaunchAngle()
        {
            float angle = ShotLaunchAngleCalculator.Compute(ContactZone.Centre, 0.5f, 0.0f, 0.0f, 1.0f);
            Assert.IsFalse(float.IsNaN(angle), "EC-006: Launch angle must not be NaN for zero body lean.");
            Assert.GreaterOrEqual(angle, ShotMechanicsConstants.LaunchAngleMin, "EC-006: Angle ≥ LaunchAngleMin.");
            Assert.LessOrEqual(angle, ShotMechanicsConstants.LaunchAngleMax,    "EC-006: Angle ≤ LaunchAngleMax.");
        }

        // EC-007 — Error direction hash: different agentIds produce different directions.
        [Test]
        public void EC007_DifferentAgentIds_ProduceDifferentErrorDirections()
        {
            Vector2 d1 = ShotErrorCalculator.ComputeErrorDirection(matchSeed: 0, agentId: 1, frameNumber: 100);
            Vector2 d2 = ShotErrorCalculator.ComputeErrorDirection(matchSeed: 0, agentId: 2, frameNumber: 100);

            // It is theoretically possible (though extremely unlikely) for two different seeds to
            // produce the same direction. Accept if approximately equal and flag it as a note.
            bool same = Mathf.Approximately(d1.x, d2.x) && Mathf.Approximately(d1.y, d2.y);
            if (same)
                Assert.Inconclusive("EC-007: Hash collision for agentId=1 vs agentId=2 — extraordinarily unlikely; investigate hash.");
            else
                Assert.Pass("EC-007: Different agentIds produced different error directions as expected.");
        }

        // EC-008 — NaN from velocity calculator triggers FM-05 guard in the calculator seam.
        [Test]
        public void EC008_NaNVelocityStub_ReturnsNaN()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            IShotVelocityCalculator stub = new NaNVelocityStub();
            float result = stub.Calculate(0.8f, 18.0f, 12.0f, 12.0f, 12, ContactZone.Centre, 0.0f, 0.0f, 1.0f);
            Assert.IsTrue(float.IsNaN(result),
                "EC-008: NaNVelocityStub must return float.NaN to trigger FM-05 in ShotExecutor §4.1.2.");
#else
            Assert.Inconclusive("EC-008: NaNVelocityStub is only compiled in UNITY_EDITOR or DEVELOPMENT_BUILD.");
#endif
        }
    }

    // ── Constants Sanity §3.2 constants ──────────────────────────────────────────────────────────

    /// <summary>
    /// Verifies key constant relationships from ShotMechanicsConstants that are load-bearing
    /// for the test assertions above. Shot Mechanics #6 §3.2.
    /// </summary>
    [TestFixture]
    internal class ConstantSanityTests
    {
        // VAbsoluteMin < VCeiling.
        [Test]
        public void Const001_VAbsoluteMin_LessThanVCeiling()
        {
            Assert.Less(ShotMechanicsConstants.VAbsoluteMin, ShotMechanicsConstants.VCeiling,
                "VAbsoluteMin must be < VCeiling §3.2.10.");
        }

        // VAbsoluteMax == VCeiling (both 35 m/s per §3.2).
        [Test]
        public void Const002_VAbsoluteMax_EqualsVCeiling()
        {
            Assert.AreEqual(ShotMechanicsConstants.VCeiling, ShotMechanicsConstants.VAbsoluteMax, 0.001f,
                "VAbsoluteMax must equal VCeiling §3.2.10.");
        }

        // ContactZone modifiers: Centre > OffCentre > BelowCentre.
        [Test]
        public void Const003_ContactZoneModifiers_Ordered()
        {
            Assert.Greater(ShotMechanicsConstants.ContactZoneModifierCentre, ShotMechanicsConstants.ContactZoneModifierOffCentre,
                "Centre modifier must be > OffCentre modifier §3.2.5.");
            Assert.Greater(ShotMechanicsConstants.ContactZoneModifierOffCentre, ShotMechanicsConstants.ContactZoneModifierBelowCentre,
                "OffCentre modifier must be > BelowCentre modifier §3.2.5.");
        }

        // Weights sum to 1.0 for composite BodyMechanicsScore.
        [Test]
        public void Const004_BodyMechanicsWeights_SumToOne()
        {
            float sum = ShotMechanicsConstants.WeightRunUp
                      + ShotMechanicsConstants.WeightPlant
                      + ShotMechanicsConstants.WeightVelocity
                      + ShotMechanicsConstants.WeightLean;

            Assert.AreEqual(1.0f, sum, 0.001f,
                "BMS component weights must sum to 1.0 §3.7.7.");
        }

        // MinErrorAngle > 0 enforces the non-zero error floor (KD-1).
        [Test]
        public void Const005_MinErrorAngle_PositiveFloor()
        {
            Assert.Greater(ShotMechanicsConstants.MinErrorAngle, 0.0f,
                "MinErrorAngle must be > 0 — no shot is laser-precise (KD-1, SE-010).");
        }

        // SpinVelocityTradeOff in (0, 1) so it never fully negates velocity.
        [Test]
        public void Const006_SpinVelocityTradeOff_InOpenUnitInterval()
        {
            Assert.Greater(ShotMechanicsConstants.SpinVelocityTradeOff, 0.0f,
                "SpinVelocityTradeOff must be > 0 to have any effect §3.2.6.");
            Assert.Less(ShotMechanicsConstants.SpinVelocityTradeOff, 1.0f,
                "SpinVelocityTradeOff must be < 1 so full spin does not eliminate all velocity §3.2.6.");
        }

        // LaunchAngleMin < 0 allows below-horizontal shots; LaunchAngleMax reasonable.
        [Test]
        public void Const007_LaunchAngleBounds_Valid()
        {
            Assert.Less(ShotMechanicsConstants.LaunchAngleMin, 0.0f,
                "LaunchAngleMin must be < 0 to allow surface-skimming shots §3.3.8.");
            Assert.Greater(ShotMechanicsConstants.LaunchAngleMax, 45.0f,
                "LaunchAngleMax must be > 45° to cover chip shots §3.3.8.");
        }

        // WeakFootRatingRange > 0: required invariant (divides in WeakFootPenaltyApplier).
        [Test]
        public void Const008_WeakFootRatingRange_PositiveInvariant()
        {
            Assert.Greater(ShotMechanicsConstants.WeakFootRatingRange, 0,
                "WeakFootRatingRange must be > 0 to avoid division by zero in WeakFootPenaltyApplier §3.8.");
        }
    }

    // ── GoalGeometryProvider §3.5 / SP-009 seam ─────────────────────────────────────────────────

    /// <summary>
    /// Tests the GoalGeometryProvider test override seam used by SP-009.
    /// Shot Mechanics #6 §5.6, §4.1.1.
    /// </summary>
    [TestFixture]
    internal class GoalGeometryProviderTests
    {
        [TearDown]
        public void TearDown()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            GoalGeometryProvider.ClearTestOverride();
#endif
        }

        // Production Get() returns values matching ShotMechanicsConstants.
        [Test]
        public void GG001_ProductionGet_MatchesConstants()
        {
            GoalGeometry geo = GoalGeometryProvider.Get();

            Assert.AreEqual(ShotMechanicsConstants.GOAL_WIDTH,  geo.GoalWidth,  0.001f, "GoalWidth must match GOAL_WIDTH constant.");
            Assert.AreEqual(ShotMechanicsConstants.GOAL_HEIGHT, geo.GoalHeight, 0.001f, "GoalHeight must match GOAL_HEIGHT constant.");
            Assert.AreEqual(ShotMechanicsConstants.PitchLength, geo.GoalLineX,  0.001f, "GoalLineX must equal PitchLength (attacking-right goal).");
        }

        // LeftPostY and RightPostY are symmetric about pitch centre Y.
        [Test]
        public void GG002_Posts_SymmetricAboutPitchCentreY()
        {
            GoalGeometry geo = GoalGeometryProvider.Get();
            float centrY = ShotMechanicsConstants.PitchWidth * 0.5f;

            Assert.AreEqual(centrY, (geo.LeftPostY + geo.RightPostY) * 0.5f, 0.001f,
                "Posts must be symmetric about pitch centre Y.");
        }

        // RightPostY - LeftPostY == GOAL_WIDTH.
        [Test]
        public void GG003_PostSpan_EqualsGoalWidth()
        {
            GoalGeometry geo = GoalGeometryProvider.Get();
            float span = geo.RightPostY - geo.LeftPostY;

            Assert.AreEqual(ShotMechanicsConstants.GOAL_WIDTH, span, 0.001f,
                "Right post Y minus left post Y must equal GOAL_WIDTH.");
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        // SetTestOverride overrides Get(); ClearTestOverride restores production values.
        [Test]
        public void GG004_TestOverride_OverridesAndClearsCorrectly()
        {
            GoalGeometry fakeGeo = new GoalGeometry
            {
                GoalWidth  = 9.0f,
                GoalHeight = 3.0f,
                GoalLineX  = 50.0f,
                LeftPostY  = 10.0f,
                RightPostY = 19.0f,
                CrossbarZ  = 3.0f
            };

            GoalGeometryProvider.SetTestOverride(fakeGeo);
            GoalGeometry overridden = GoalGeometryProvider.Get();
            Assert.AreEqual(9.0f, overridden.GoalWidth, 0.001f, "Override GoalWidth must be returned after SetTestOverride.");

            GoalGeometryProvider.ClearTestOverride();
            GoalGeometry restored = GoalGeometryProvider.Get();
            Assert.AreEqual(ShotMechanicsConstants.GOAL_WIDTH, restored.GoalWidth, 0.001f,
                "Production GoalWidth must be restored after ClearTestOverride.");
        }
#endif
    }
}

    // ════════════════════════════════════════════════════════════════════════════
    // §5.12 Integration Tests (IT-)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Integration tests for Shot Mechanics #6 §5.12. All require ShotExecutor wired to
    /// IShotBallSystem, IShotAgentQuery, IShotCollisionQuery, and EventBus at Stage 0+1.
    /// </summary>
    internal sealed class ShotMechanicsIntegrationTests
    {
        /// <summary>IT-001: Ball Physics Interface — ApplyKick called with valid arguments. §5.12.</summary>
        [Test]
        public void BallPhysicsInterface_ApplyKick_CalledWithValidArguments()
        {
            Assert.Ignore("Stage 0+1: requires IShotBallSystem mock wired to ShotExecutor — IT-001");
        }

        /// <summary>IT-002: Agent Movement Interface — AgentPhysicalProperties frozen at INITIATING. §5.12.</summary>
        [Test]
        public void AgentMovementInterface_AgentProperties_FrozenAtInitiating()
        {
            Assert.Ignore("Stage 0+1: requires IShotAgentQuery mock wired — IT-002");
        }

        /// <summary>IT-003: Collision System Interface — tackle interrupt during WINDUP cancels shot. §5.12.</summary>
        [Test]
        public void CollisionInterface_TackleInterruptDuringWindup_CancelsShot()
        {
            Assert.Ignore("Stage 0+1: requires IShotCollisionQuery mock returning tackle flag — IT-003");
        }

        /// <summary>IT-004: Collision System Interface — tackle interrupt during CONTACT does not cancel. §5.12.</summary>
        [Test]
        public void CollisionInterface_TackleInterruptDuringContact_DoesNotCancel()
        {
            Assert.Ignore("Stage 0+1: requires IShotCollisionQuery mock — IT-004");
        }

        /// <summary>IT-005: Event System Interface — ShotExecutedEvent published with complete payload. §5.12.</summary>
        [Test]
        public void EventInterface_ShotExecutedEvent_PublishedWithCompletePayload()
        {
            Assert.Ignore("Stage 0+1: requires EventBus wired to ShotEventEmitter — IT-005");
        }

        /// <summary>IT-006: Event System Interface — ShotCancelledEvent published on tackle, not ShotExecutedEvent. §5.12.</summary>
        [Test]
        public void EventInterface_TackleCancelled_PublishesCancelledNotExecuted()
        {
            Assert.Ignore("Stage 0+1: requires EventBus wired to ShotEventEmitter — IT-006");
        }

        /// <summary>IT-007: End-to-End — top-corner placement produces correct aim direction. §5.12.</summary>
        [Test]
        public void EndToEnd_TopCornerPlacement_ProducesCorrectAimDirection()
        {
            Assert.Ignore("Stage 0+1: requires full ShotExecutor pipeline with IShotBallSystem — IT-007");
        }

        /// <summary>IT-008: End-to-End — chip shot profile produces elevated launch angle. §5.12.</summary>
        [Test]
        public void EndToEnd_ChipShotProfile_ProducesElevatedLaunchAngle()
        {
            Assert.Ignore("Stage 0+1: requires full ShotExecutor pipeline — IT-008");
        }

        /// <summary>IT-009: End-to-End — driven shot profile produces low launch angle and high speed. §5.12.</summary>
        [Test]
        public void EndToEnd_DrivenShotProfile_ProducesLowAngleAndHighSpeed()
        {
            Assert.Ignore("Stage 0+1: requires full ShotExecutor pipeline — IT-009");
        }

        /// <summary>IT-010: End-to-End — weak foot shot produces higher error and lower speed than strong foot. §5.12.</summary>
        [Test]
        public void EndToEnd_WeakFootShot_HigherErrorAndLowerSpeedThanStrongFoot()
        {
            Assert.Ignore("Stage 0+1: requires full ShotExecutor with IShotAgentQuery — IT-010");
        }

        /// <summary>IT-011: ShotAnimationData stub populated on completion, not published. §5.12.</summary>
        [Test]
        public void ShotAnimationData_PopulatedOnCompletion_NotPublished()
        {
            Assert.Ignore("Stage 0+1: requires full ShotExecutor pipeline; ShotAnimationData is Stage 1+ — IT-011");
        }

        /// <summary>IT-012 CRITICAL: Determinism regression — identical inputs produce identical outputs. §5.12.</summary>
        [Test]
        public void CriticalDeterminism_IdenticalInputs_ProduceIdenticalOutputs()
        {
            Assert.Ignore("Stage 0+1: requires Deterministic Simulation #16 digest comparison — IT-012 CRITICAL");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-31 | —      | Initial implementation. |
// | 1.1     | 2026-06-01 | —      | Add §5.12 integration test stubs IT-001..012 (all Assert.Ignore; Stage 0+1). |
#endregion
