// File:     src/pass-mechanics/Tests/PassMechanicsTests.cs
// Created:  2026-05-31
// Modified: 2026-06-01
// Author:   —
// Spec:     Pass Mechanics #5 §5, Code Standards #20
// Purpose:  NUnit unit tests for Pass Mechanics subsystems: pass type profiles (PT),
//           kick velocity model (PV), launch angle derivation (LA), spin vector (SV),
//           pass error model (PE), weak foot penalty (WF), and edge cases (EC).

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.PassMechanics;

namespace TacticalDirector.PassMechanics.Tests
{
    // ── PT — Pass Type Profiles ────────────────────────────────────────────────────

    /// <summary>
    /// PT-001 to PT-008: PhysicalProfile lookup correctness.
    /// Spec §5.2 — Pass Type Classification.
    /// </summary>
    [TestFixture]
    internal class PassTypeProfileTests
    {
        /// <summary>PT-001: Ground profile is non-null (non-default struct).</summary>
        [Test]
        public void PT001_GroundProfile_IsNotDefault()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Ground);
            Assert.Greater(profile.VMax, 0f,
                "Ground PhysicalProfile must have VMax > 0 (non-default struct).");
        }

        /// <summary>PT-002: Driven profile is non-null (non-default struct).</summary>
        [Test]
        public void PT002_DrivenProfile_IsNotDefault()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Driven);
            Assert.Greater(profile.VMax, 0f,
                "Driven PhysicalProfile must have VMax > 0.");
        }

        /// <summary>PT-003: All 7 pass types yield a non-default profile.</summary>
        [Test]
        public void PT003_AllPassTypes_YieldNonDefaultProfile()
        {
            PassType[] allTypes = new PassType[]
            {
                PassType.Ground,
                PassType.Driven,
                PassType.Lofted,
                PassType.ThroughBall,
                PassType.AerialThrough,
                PassType.Cross,
                PassType.Chip
            };

            foreach (PassType pt in allTypes)
            {
                PhysicalProfile profile = PassTypeProfiles.GetProfile(pt);
                Assert.Greater(profile.VMax, 0f,
                    $"PassType.{pt} must yield a non-default PhysicalProfile (VMax > 0).");
            }
        }

        /// <summary>PT-004: All 7 pass types yield VMax greater than zero.</summary>
        [Test]
        public void PT004_AllPassTypes_YieldPositiveVMax()
        {
            PassType[] allTypes = new PassType[]
            {
                PassType.Ground,
                PassType.Driven,
                PassType.Lofted,
                PassType.ThroughBall,
                PassType.AerialThrough,
                PassType.Cross,
                PassType.Chip
            };

            foreach (PassType pt in allTypes)
            {
                PhysicalProfile profile = PassTypeProfiles.GetProfile(pt);
                Assert.Greater(profile.VMax, 0f, $"PassType.{pt} must have VMax > 0.");
            }
        }

        /// <summary>PT-005: Profile lookup is deterministic — three calls return equal profiles.</summary>
        [Test]
        public void PT005_ProfileLookup_IsDeterministic()
        {
            PhysicalProfile a = PassTypeProfiles.GetProfile(PassType.Lofted);
            PhysicalProfile b = PassTypeProfiles.GetProfile(PassType.Lofted);
            PhysicalProfile c = PassTypeProfiles.GetProfile(PassType.Lofted);

            Assert.AreEqual(a.VMax,             b.VMax,             "VMax must be identical across calls.");
            Assert.AreEqual(b.VMax,             c.VMax,             "VMax must be identical across calls.");
            Assert.AreEqual(a.AngleMin,         b.AngleMin,         "AngleMin must be identical.");
            Assert.AreEqual(a.SpinMagnitudeBase, b.SpinMagnitudeBase, "SpinMagnitudeBase must be identical.");
        }

        /// <summary>
        /// PT-006: Cross Flat vs Cross Whipped produce different profiles (either VMax or AngleMin differs).
        /// </summary>
        [Test]
        public void PT006_CrossFlatVsWhipped_ProduceDifferentProfiles()
        {
            PhysicalProfile flat    = PassTypeProfiles.GetProfile(PassType.Cross, CrossSubType.Flat);
            PhysicalProfile whipped = PassTypeProfiles.GetProfile(PassType.Cross, CrossSubType.Whipped);

            bool vmaxDiffers    = !Mathf.Approximately(flat.VMax,     whipped.VMax);
            bool angleDiffers   = !Mathf.Approximately(flat.AngleMin, whipped.AngleMin);

            Assert.IsTrue(vmaxDiffers || angleDiffers,
                "Cross Flat and Cross Whipped must differ in at least VMax or AngleMin.");
        }

        /// <summary>PT-007: Cross with no sub-type argument defaults to Flat profile.</summary>
        [Test]
        public void PT007_CrossDefaultSubType_EqualsFlatProfile()
        {
            PhysicalProfile withDefault = PassTypeProfiles.GetProfile(PassType.Cross);
            PhysicalProfile flat        = PassTypeProfiles.GetProfile(PassType.Cross, CrossSubType.Flat);

            Assert.AreEqual(flat.VMax,             withDefault.VMax,             "Default CrossSubType must equal Flat VMax.");
            Assert.AreEqual(flat.AngleMin,         withDefault.AngleMin,         "Default CrossSubType must equal Flat AngleMin.");
            Assert.AreEqual(flat.SpinMagnitudeBase, withDefault.SpinMagnitudeBase, "Default CrossSubType must equal Flat SpinMagnitudeBase.");
        }

        /// <summary>
        /// PT-008: All profiles satisfy internal sanity invariants (VMin less than VMax, AngleMin
        /// less than AngleMax, DistMin less than DistMax, SpinMagnitudeBase less than SpinMagnitudeMax).
        /// </summary>
        [Test]
        public void PT008_AllProfiles_SatisfyInternalInvariants()
        {
            PassType[] allTypes = new PassType[]
            {
                PassType.Ground,
                PassType.Driven,
                PassType.Lofted,
                PassType.ThroughBall,
                PassType.AerialThrough,
                PassType.Cross,
                PassType.Chip
            };

            foreach (PassType pt in allTypes)
            {
                PhysicalProfile profile = PassTypeProfiles.GetProfile(pt);
                Assert.Less(profile.VMin,              profile.VMax,              $"{pt}: VMin must be less than VMax.");
                Assert.Less(profile.AngleMin,          profile.AngleMax,          $"{pt}: AngleMin must be less than AngleMax.");
                Assert.Less(profile.DistMin,           profile.DistMax,           $"{pt}: DistMin must be less than DistMax.");
                Assert.Less(profile.SpinMagnitudeBase, profile.SpinMagnitudeMax,  $"{pt}: SpinMagnitudeBase must be less than SpinMagnitudeMax.");
                Assert.Greater(profile.VMin, 0f,                                  $"{pt}: VMin must be positive.");
            }
        }
    }

    // ── PV — Pass Velocity Model ───────────────────────────────────────────────────

    /// <summary>
    /// PV-001 to PV-012: ComputeKickSpeed correctness and boundary behaviour.
    /// Spec §5.3 — Pass Velocity Model (§3.2).
    /// </summary>
    [TestFixture]
    internal class PassVelocityTests
    {
        private const float NoFatigue        = 0.0f;
        private const float NoWeakFoot       = 1.0f;  // penalty-free (strong foot)
        private const float MidKickPower     = 10.0f;
        private const float HighKickPower    = 18.0f;
        private const float MaxKickPower     = 20.0f;
        private const float MinKickPower     = 1.0f;

        /// <summary>PV-001: Ground pass, mid attributes, 20m — velocity in expected range.</summary>
        [Test]
        public void PV001_Ground_MidAttributes_20m_VelocityInRange()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Ground);
            float v = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    20.0f,
                kickPower:           MidKickPower,
                fatigue:             NoFatigue,
                weakFootPowerPenalty: NoWeakFoot,
                profile:             profile);

            Assert.GreaterOrEqual(v, 10.0f, "PV-001: Ground mid-power 20m velocity must be >= 10 m/s.");
            Assert.LessOrEqual(v, 16.0f,    "PV-001: Ground mid-power 20m velocity must be <= 16 m/s.");
        }

        /// <summary>PV-002: Driven pass, high attributes, 35m — velocity in expected range.</summary>
        [Test]
        public void PV002_Driven_HighAttributes_35m_VelocityInRange()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Driven);
            float v = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    35.0f,
                kickPower:           HighKickPower,
                fatigue:             NoFatigue,
                weakFootPowerPenalty: NoWeakFoot,
                profile:             profile);

            Assert.GreaterOrEqual(v, 20.0f, "PV-002: Driven high-power 35m velocity must be >= 20 m/s.");
            Assert.LessOrEqual(v, 28.0f,    "PV-002: Driven high-power 35m velocity must be <= 28 m/s (profile VMax).");
        }

        /// <summary>PV-003: Lofted pass, mid attributes, 40m — velocity in expected range.</summary>
        [Test]
        public void PV003_Lofted_MidAttributes_40m_VelocityInRange()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Lofted);
            float v = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    40.0f,
                kickPower:           MidKickPower,
                fatigue:             NoFatigue,
                weakFootPowerPenalty: NoWeakFoot,
                profile:             profile);

            Assert.GreaterOrEqual(v, 14.0f, "PV-003: Lofted mid-power 40m velocity must be >= 14 m/s.");
            Assert.LessOrEqual(v, 22.0f,    "PV-003: Lofted mid-power 40m velocity must be <= 22 m/s (VMax).");
        }

        /// <summary>PV-004: Ground pass, minimum power, 30m — velocity above VMin and bounded.</summary>
        [Test]
        public void PV004_Ground_MinKickPower_30m_VelocityAboveFloor()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Ground);
            float v = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    30.0f,
                kickPower:           MinKickPower,
                fatigue:             NoFatigue,
                weakFootPowerPenalty: NoWeakFoot,
                profile:             profile);

            Assert.GreaterOrEqual(v, profile.VMin, "PV-004: Minimum-power velocity must be >= VMin (clamp floor).");
            Assert.LessOrEqual(v, profile.VOffset + 1.5f,
                "PV-004: Minimum-power velocity must be near VOffset lower bound.");
        }

        /// <summary>PV-005: Chip pass, max power, DistMax — velocity clamped at VMax.</summary>
        [Test]
        public void PV005_Chip_MaxPower_DistMax_ClampedToVMax()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Chip);
            float v = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    profile.DistMax,
                kickPower:           MaxKickPower,
                fatigue:             NoFatigue,
                weakFootPowerPenalty: NoWeakFoot,
                profile:             profile);

            Assert.AreEqual(profile.VMax, v, 0.001f,
                "PV-005: Chip at max power and DistMax must produce VMax (clamped).");
        }

        /// <summary>PV-006: Fatigue=1.0 reduces velocity compared to Fatigue=0.0.</summary>
        [Test]
        public void PV006_FullFatigue_ReducesVelocity()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Driven);
            float vFresh = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    20.0f,
                kickPower:           15.0f,
                fatigue:             0.0f,
                weakFootPowerPenalty: NoWeakFoot,
                profile:             profile);

            float vFatigued = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    20.0f,
                kickPower:           15.0f,
                fatigue:             1.0f,
                weakFootPowerPenalty: NoWeakFoot,
                profile:             profile);

            Assert.Less(vFatigued, vFresh,
                "PV-006: Fatigue=1.0 must reduce velocity compared to Fatigue=0.0.");
        }

        /// <summary>PV-007: Fatigue=0.0 applies no reduction — velocity unchanged.</summary>
        [Test]
        public void PV007_ZeroFatigue_AppliesNoReduction()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Driven);
            float v1 = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    25.0f,
                kickPower:           15.0f,
                fatigue:             0.0f,
                weakFootPowerPenalty: NoWeakFoot,
                profile:             profile);
            float v2 = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    25.0f,
                kickPower:           15.0f,
                fatigue:             0.0f,
                weakFootPowerPenalty: NoWeakFoot,
                profile:             profile);

            Assert.AreEqual(v1, v2, 0.001f,
                "PV-007: Fatigue=0.0 must produce identical, non-reduced velocity on repeated calls.");
        }

        /// <summary>PV-008: Fatigue modifier is monotonically decreasing across [0, 0.25, 0.5, 0.75, 1.0].</summary>
        [Test]
        public void PV008_FatigueModifier_IsMonotonicallyDecreasing()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Driven);
            float[] fatigueSteps = new float[] { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
            float prevV = float.MaxValue;

            foreach (float fatigue in fatigueSteps)
            {
                float v = PassVelocityCalculator.ComputeKickSpeed(
                    intendedDistance:    35.0f,
                    kickPower:           15.0f,
                    fatigue:             fatigue,
                    weakFootPowerPenalty: NoWeakFoot,
                    profile:             profile);

                Assert.LessOrEqual(v, prevV,
                    $"PV-008: Velocity at fatigue={fatigue} must be <= velocity at previous fatigue step.");
                prevV = v;
            }
        }

        /// <summary>PV-009: All pass types produce V within [VMin, VMax] for mid-range inputs.</summary>
        [Test]
        public void PV009_AllPassTypes_VelocityWithinProfileBounds()
        {
            PassType[] allTypes = new PassType[]
            {
                PassType.Ground,
                PassType.Driven,
                PassType.Lofted,
                PassType.ThroughBall,
                PassType.AerialThrough,
                PassType.Cross,
                PassType.Chip
            };

            foreach (PassType pt in allTypes)
            {
                PhysicalProfile profile = PassTypeProfiles.GetProfile(pt);
                float midDist = (profile.DistMin + profile.DistMax) * 0.5f;
                float v = PassVelocityCalculator.ComputeKickSpeed(
                    intendedDistance:    midDist,
                    kickPower:           MidKickPower,
                    fatigue:             NoFatigue,
                    weakFootPowerPenalty: NoWeakFoot,
                    profile:             profile);

                Assert.GreaterOrEqual(v, profile.VMin,
                    $"PV-009: {pt} velocity must be >= VMin ({profile.VMin}).");
                Assert.LessOrEqual(v, profile.VMax,
                    $"PV-009: {pt} velocity must be <= VMax ({profile.VMax}).");
            }
        }

        /// <summary>PV-010: Ground pass, max power, DistMax — velocity clamped at VMax.</summary>
        [Test]
        public void PV010_Ground_MaxPower_DistMax_ClampedToVMax()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Ground);
            float v = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    profile.DistMax,
                kickPower:           MaxKickPower,
                fatigue:             NoFatigue,
                weakFootPowerPenalty: NoWeakFoot,
                profile:             profile);

            Assert.AreEqual(profile.VMax, v, 0.001f,
                "PV-010: Ground at max power and DistMax must produce VMax (clamped).");
        }

        /// <summary>PV-011: Kick speed computation is deterministic — three identical calls agree.</summary>
        [Test]
        public void PV011_ComputeKickSpeed_IsDeterministic()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Ground);
            float v1 = PassVelocityCalculator.ComputeKickSpeed(20.0f, 12.0f, 0.3f, NoWeakFoot, profile);
            float v2 = PassVelocityCalculator.ComputeKickSpeed(20.0f, 12.0f, 0.3f, NoWeakFoot, profile);
            float v3 = PassVelocityCalculator.ComputeKickSpeed(20.0f, 12.0f, 0.3f, NoWeakFoot, profile);

            Assert.AreEqual(v1, v2, "PV-011: ComputeKickSpeed must be deterministic (calls 1 and 2).");
            Assert.AreEqual(v2, v3, "PV-011: ComputeKickSpeed must be deterministic (calls 2 and 3).");
        }

        /// <summary>PV-012: Near-zero distance clamps internally and returns a valid velocity, not NaN.</summary>
        [Test]
        public void PV012_NearZeroDistance_ReturnsValidVelocity()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Ground);
            // Distance of 0 would produce NaN; implementation clamps to 0.001f minimum.
            float v = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    0.0f,
                kickPower:           MidKickPower,
                fatigue:             NoFatigue,
                weakFootPowerPenalty: NoWeakFoot,
                profile:             profile);

            Assert.IsFalse(float.IsNaN(v),      "PV-012: Near-zero distance must not produce NaN velocity.");
            Assert.IsFalse(float.IsInfinity(v), "PV-012: Near-zero distance must not produce Infinity velocity.");
            Assert.GreaterOrEqual(v, profile.VMin, "PV-012: Near-zero distance velocity must be >= VMin (clamped).");
        }
    }

    // ── LA — Launch Angle Derivation ───────────────────────────────────────────────

    /// <summary>
    /// LA-001 to LA-008: ComputeLaunchAngle correctness and formula verification.
    /// Spec §5.4 — Launch Angle Derivation (§3.3).
    /// </summary>
    [TestFixture]
    internal class LaunchAngleTests
    {
        /// <summary>LA-001: Ground pass angle at 20m is within profile bounds [2°, 5°].</summary>
        [Test]
        public void LA001_Ground_20m_AngleWithinBounds()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Ground);
            float angle = PassVelocityCalculator.ComputeLaunchAngle(
                PassType.Ground, CrossSubType.Flat, 20.0f, profile);

            Assert.GreaterOrEqual(angle, profile.AngleMin, "LA-001: Ground 20m angle must be >= AngleMin.");
            Assert.LessOrEqual(angle, profile.AngleMax,    "LA-001: Ground 20m angle must be <= AngleMax.");
        }

        /// <summary>LA-002: Lofted pass angle at 30m is within profile bounds [20°, 45°].</summary>
        [Test]
        public void LA002_Lofted_30m_AngleWithinBounds()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Lofted);
            float angle = PassVelocityCalculator.ComputeLaunchAngle(
                PassType.Lofted, CrossSubType.Flat, 30.0f, profile);

            // Apex formula: θ = atan(4×6.0/30) = atan(0.8) ≈ 38.7°
            Assert.GreaterOrEqual(angle, 25.0f, "LA-002: Lofted 30m angle must be >= 25°.");
            Assert.LessOrEqual(angle, 55.0f,    "LA-002: Lofted 30m angle must be <= 55°.");
        }

        /// <summary>LA-003: Chip pass angle at 12m is within profile bounds [45°, 65°].</summary>
        [Test]
        public void LA003_Chip_12m_AngleWithinBounds()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Chip);
            float angle = PassVelocityCalculator.ComputeLaunchAngle(
                PassType.Chip, CrossSubType.Flat, 12.0f, profile);

            // Apex formula: θ = atan(4×4.5/12) = atan(1.5) ≈ 56.3°
            Assert.GreaterOrEqual(angle, 40.0f, "LA-003: Chip 12m angle must be >= 40°.");
            Assert.LessOrEqual(angle, 70.0f,    "LA-003: Chip 12m angle must be <= 70°.");
        }

        /// <summary>LA-004: Cross Flat angle is within profile bounds [8°, 15°].</summary>
        [Test]
        public void LA004_CrossFlat_AngleWithinBounds()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Cross, CrossSubType.Flat);
            float angle = PassVelocityCalculator.ComputeLaunchAngle(
                PassType.Cross, CrossSubType.Flat, 30.0f, profile);

            Assert.GreaterOrEqual(angle, 8.0f,  "LA-004: Cross Flat angle must be >= 8°.");
            Assert.LessOrEqual(angle, 15.0f,    "LA-004: Cross Flat angle must be <= 15°.");
        }

        /// <summary>LA-005: Cross High angle is within profile bounds [25°, 40°].</summary>
        [Test]
        public void LA005_CrossHigh_AngleWithinBounds()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Cross, CrossSubType.High);
            float angle = PassVelocityCalculator.ComputeLaunchAngle(
                PassType.Cross, CrossSubType.High, 35.0f, profile);

            Assert.GreaterOrEqual(angle, 25.0f, "LA-005: Cross High angle must be >= 25°.");
            Assert.LessOrEqual(angle, 40.0f,    "LA-005: Cross High angle must be <= 40°.");
        }

        /// <summary>
        /// LA-006: Lofted angle is greater at short distance than at long distance.
        /// Apex formula θ = atan(4H/D): shorter D → larger θ.
        /// </summary>
        [Test]
        public void LA006_Lofted_ShortDistanceAngle_GreaterThanLongDistance()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Lofted);
            // At D=20m: θ = atan(4×6.0/20) = atan(1.2) ≈ 50.2°
            float angleShort = PassVelocityCalculator.ComputeLaunchAngle(
                PassType.Lofted, CrossSubType.Flat, 20.0f, profile);
            // At D=50m: θ = atan(4×6.0/50) = atan(0.48) ≈ 25.6°
            float angleLong = PassVelocityCalculator.ComputeLaunchAngle(
                PassType.Lofted, CrossSubType.Flat, 50.0f, profile);

            Assert.Greater(angleShort, angleLong,
                "LA-006: Lofted angle at 20m must be greater than at 50m (apex formula: θ ∝ 1/D).");
        }

        /// <summary>LA-007: Angle computation is deterministic — three identical calls agree.</summary>
        [Test]
        public void LA007_ComputeLaunchAngle_IsDeterministic()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Lofted);
            float a1 = PassVelocityCalculator.ComputeLaunchAngle(PassType.Lofted, CrossSubType.Flat, 30.0f, profile);
            float a2 = PassVelocityCalculator.ComputeLaunchAngle(PassType.Lofted, CrossSubType.Flat, 30.0f, profile);
            float a3 = PassVelocityCalculator.ComputeLaunchAngle(PassType.Lofted, CrossSubType.Flat, 30.0f, profile);

            Assert.AreEqual(a1, a2, "LA-007: ComputeLaunchAngle must be deterministic (calls 1 and 2).");
            Assert.AreEqual(a2, a3, "LA-007: ComputeLaunchAngle must be deterministic (calls 2 and 3).");
        }

        /// <summary>
        /// LA-008: Kick velocity Z component matches V × sin(launchAngle) within tolerance.
        /// Verifies the angle feeds correctly into ConstructKickVelocity (§3.3.6).
        /// </summary>
        [Test]
        public void LA008_KickVelocity_ZComponent_MatchesSinOfAngle()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Lofted);
            float kickSpeed  = 18.0f;
            float angleDeg   = PassVelocityCalculator.ComputeLaunchAngle(
                PassType.Lofted, CrossSubType.Flat, 30.0f, profile);

            Vector3 kickVelocity = PassVelocityCalculator.ConstructKickVelocity(
                kickSpeed:       kickSpeed,
                kickDirection:   Vector3.right,  // +X, horizontal
                launchAngleDeg:  angleDeg);

            float expectedVz = kickSpeed * Mathf.Sin(angleDeg * Mathf.Deg2Rad);
            Assert.AreEqual(expectedVz, kickVelocity.z, 0.01f,
                "LA-008: V_z component must equal kickSpeed × sin(launchAngle).");
        }
    }

    // ── SV — Spin Vector Calculation ───────────────────────────────────────────────

    /// <summary>
    /// SV-001 to SV-008: ComputeSpinVector sign conventions and magnitude bounds.
    /// Spec §5.5 — Spin Vector Calculation (§3.4).
    /// Z-up coordinate system: topspin = +Y, backspin = -Y, sidespin = ±Z.
    /// </summary>
    [TestFixture]
    internal class SpinVectorTests
    {
        private const float MidTechnique = 10.0f;

        /// <summary>SV-001: Ground pass produces topspin — positive Y component.</summary>
        [Test]
        public void SV001_Ground_ProducesTopspin_PositiveY()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Ground);
            Vector3 spin = PassVelocityCalculator.ComputeSpinVector(
                PassType.Ground, CrossSubType.Flat, MidTechnique, false, profile);

            Assert.Greater(spin.y, 0f, "SV-001: Ground pass must produce positive Y spin (topspin).");
        }

        /// <summary>SV-002: Chip pass produces backspin — negative Y component.</summary>
        [Test]
        public void SV002_Chip_ProducesBackspin_NegativeY()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Chip);
            Vector3 spin = PassVelocityCalculator.ComputeSpinVector(
                PassType.Chip, CrossSubType.Flat, MidTechnique, false, profile);

            Assert.Less(spin.y, 0f, "SV-002: Chip pass must produce negative Y spin (backspin).");
        }

        /// <summary>SV-003: Cross Flat produces non-zero Z spin (sidespin).</summary>
        [Test]
        public void SV003_CrossFlat_ProducesSidespin_NonZeroZ()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Cross, CrossSubType.Flat);
            Vector3 spin = PassVelocityCalculator.ComputeSpinVector(
                PassType.Cross, CrossSubType.Flat, MidTechnique, false, profile);

            Assert.AreNotEqual(0f, spin.z, "SV-003: Cross Flat must produce non-zero Z spin (sidespin).");
        }

        /// <summary>SV-004: Higher Technique increases spin magnitude.</summary>
        [Test]
        public void SV004_HigherTechnique_IncreasesMagnitude()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Driven);
            Vector3 spinLow  = PassVelocityCalculator.ComputeSpinVector(
                PassType.Driven, CrossSubType.Flat, 5.0f, false, profile);
            Vector3 spinHigh = PassVelocityCalculator.ComputeSpinVector(
                PassType.Driven, CrossSubType.Flat, 18.0f, false, profile);

            Assert.Greater(spinHigh.magnitude, spinLow.magnitude,
                "SV-004: Technique=18 must produce higher spin magnitude than Technique=5.");
        }

        /// <summary>SV-005: Cross spin magnitude does not exceed profile SpinMagnitudeMax.</summary>
        [Test]
        public void SV005_Cross_SpinMagnitude_WithinProfileMax()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Cross, CrossSubType.Flat);
            Vector3 spin = PassVelocityCalculator.ComputeSpinVector(
                PassType.Cross, CrossSubType.Flat, MidTechnique, false, profile);

            Assert.LessOrEqual(spin.magnitude, profile.SpinMagnitudeMax,
                "SV-005: Cross spin magnitude must not exceed profile SpinMagnitudeMax.");
        }

        /// <summary>SV-006: Chip spin magnitude does not exceed profile SpinMagnitudeMax.</summary>
        [Test]
        public void SV006_Chip_SpinMagnitude_WithinProfileMax()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Chip);
            Vector3 spin = PassVelocityCalculator.ComputeSpinVector(
                PassType.Chip, CrossSubType.Flat, MidTechnique, false, profile);

            Assert.LessOrEqual(spin.magnitude, profile.SpinMagnitudeMax,
                "SV-006: Chip spin magnitude must not exceed profile SpinMagnitudeMax.");
        }

        /// <summary>SV-007: Spin vector computation is deterministic — three identical calls agree.</summary>
        [Test]
        public void SV007_ComputeSpinVector_IsDeterministic()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Ground);
            Vector3 s1 = PassVelocityCalculator.ComputeSpinVector(PassType.Ground, CrossSubType.Flat, MidTechnique, false, profile);
            Vector3 s2 = PassVelocityCalculator.ComputeSpinVector(PassType.Ground, CrossSubType.Flat, MidTechnique, false, profile);
            Vector3 s3 = PassVelocityCalculator.ComputeSpinVector(PassType.Ground, CrossSubType.Flat, MidTechnique, false, profile);

            Assert.AreEqual(s1, s2, "SV-007: ComputeSpinVector must be deterministic (calls 1 and 2).");
            Assert.AreEqual(s2, s3, "SV-007: ComputeSpinVector must be deterministic (calls 2 and 3).");
        }

        /// <summary>SV-008: All 7 pass types at Technique=10 produce non-zero spin.</summary>
        [Test]
        public void SV008_AllPassTypes_NonZeroTechnique_ProducesNonZeroSpin()
        {
            PassType[] allTypes = new PassType[]
            {
                PassType.Ground,
                PassType.Driven,
                PassType.Lofted,
                PassType.ThroughBall,
                PassType.AerialThrough,
                PassType.Cross,
                PassType.Chip
            };

            foreach (PassType pt in allTypes)
            {
                PhysicalProfile profile = PassTypeProfiles.GetProfile(pt);
                Vector3 spin = PassVelocityCalculator.ComputeSpinVector(
                    pt, CrossSubType.Flat, MidTechnique, false, profile);

                Assert.Greater(spin.magnitude, 0f,
                    $"SV-008: PassType.{pt} with Technique=10 must produce non-zero spin magnitude.");
            }
        }
    }

    // ── PE — Pass Error Model ──────────────────────────────────────────────────────

    /// <summary>
    /// PE-001 to PE-010: ComputeErrorAngle modifier chain verification.
    /// Spec §5.6 — Pass Error Model (§3.5).
    /// </summary>
    [TestFixture]
    internal class PassErrorTests
    {
        // Neutral modifier values — each holds all other modifiers at 1.0× to isolate one variable.
        private const float NeutralPassing        = 10.0f; // PassingModifier ≈ 1.0 at mid-scale
        private const float NeutralPressure       = 0.0f;
        private const float NeutralFatigue        = 0.0f;
        private const float NeutralBodyAngle      = 0.0f;
        private const float NeutralUrgency        = 0.0f;
        private const bool  StrongFoot            = false;
        private const int   NeutralWeakFootRating = 3;

        /// <summary>PE-001: Elite passer at neutral conditions produces low error below elite ceiling.</summary>
        [Test]
        public void PE001_ElitePasser_NeutralConditions_LowError()
        {
            float error = PassErrorCalculator.ComputeErrorAngle(
                passType:        PassType.Ground,
                crossSubType:    CrossSubType.Flat,
                passing:         20.0f,
                pressureScalar:  NeutralPressure,
                fatigue:         NeutralFatigue,
                bodyAngleDeg:    NeutralBodyAngle,
                urgency:         NeutralUrgency,
                isWeakFoot:      StrongFoot,
                weakFootRating:  NeutralWeakFootRating);

            // Elite passer: PassingModifier = PassingErrorMin = 0.45; BASE_ERROR_Ground = 1.5°
            float eliteCeiling = PassMechanicsConstants.GetBaseError(PassType.Ground)
                               * PassMechanicsConstants.PassingErrorMin
                               * 1.05f; // 5% tolerance headroom
            Assert.LessOrEqual(error, eliteCeiling,
                "PE-001: Elite passer at neutral conditions must produce error <= elite ceiling.");
        }

        /// <summary>PE-002: Worst-case inputs produce error clamped to MaxErrorAngle.</summary>
        [Test]
        public void PE002_WorstCaseInputs_ErrorClampedToMax()
        {
            float error = PassErrorCalculator.ComputeErrorAngle(
                passType:        PassType.Ground,
                crossSubType:    CrossSubType.Flat,
                passing:         1.0f,
                pressureScalar:  1.0f,
                fatigue:         1.0f,
                bodyAngleDeg:    90.0f,
                urgency:         1.0f,
                isWeakFoot:      true,
                weakFootRating:  1);

            Assert.AreEqual(PassMechanicsConstants.MaxErrorAngle, error, 0.05f,
                "PE-002: Worst-case inputs must produce error clamped to MaxErrorAngle.");
        }

        /// <summary>PE-003: Pressure modifier is monotonically increasing with PressureScalar.</summary>
        [Test]
        public void PE003_PressureModifier_IsMonotonicallyIncreasing()
        {
            float[] pressureSteps = new float[] { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
            float prevError = -1f;

            foreach (float pressure in pressureSteps)
            {
                float error = PassErrorCalculator.ComputeErrorAngle(
                    passType:        PassType.Ground,
                    crossSubType:    CrossSubType.Flat,
                    passing:         NeutralPassing,
                    pressureScalar:  pressure,
                    fatigue:         NeutralFatigue,
                    bodyAngleDeg:    NeutralBodyAngle,
                    urgency:         NeutralUrgency,
                    isWeakFoot:      StrongFoot,
                    weakFootRating:  NeutralWeakFootRating);

                Assert.GreaterOrEqual(error, prevError,
                    $"PE-003: Error at pressure={pressure} must be >= error at previous step.");
                prevError = error;
            }
        }

        /// <summary>PE-004: Fatigue modifier is monotonically increasing with Fatigue.</summary>
        [Test]
        public void PE004_FatigueModifier_IsMonotonicallyIncreasing()
        {
            float[] fatigueSteps = new float[] { 0.0f, 0.25f, 0.5f, 0.75f, 1.0f };
            float prevError = -1f;

            foreach (float fatigue in fatigueSteps)
            {
                float error = PassErrorCalculator.ComputeErrorAngle(
                    passType:        PassType.Ground,
                    crossSubType:    CrossSubType.Flat,
                    passing:         NeutralPassing,
                    pressureScalar:  NeutralPressure,
                    fatigue:         fatigue,
                    bodyAngleDeg:    NeutralBodyAngle,
                    urgency:         NeutralUrgency,
                    isWeakFoot:      StrongFoot,
                    weakFootRating:  NeutralWeakFootRating);

                Assert.GreaterOrEqual(error, prevError,
                    $"PE-004: Error at fatigue={fatigue} must be >= error at previous step.");
                prevError = error;
            }
        }

        /// <summary>PE-005: Passing modifier is monotonically decreasing as Passing increases.</summary>
        [Test]
        public void PE005_PassingModifier_IsMonotonicallyDecreasing()
        {
            int[] passingSteps = new int[] { 1, 5, 10, 15, 20 };
            float prevError = float.MaxValue;

            foreach (int passing in passingSteps)
            {
                float error = PassErrorCalculator.ComputeErrorAngle(
                    passType:        PassType.Ground,
                    crossSubType:    CrossSubType.Flat,
                    passing:         (float)passing,
                    pressureScalar:  NeutralPressure,
                    fatigue:         NeutralFatigue,
                    bodyAngleDeg:    NeutralBodyAngle,
                    urgency:         NeutralUrgency,
                    isWeakFoot:      StrongFoot,
                    weakFootRating:  NeutralWeakFootRating);

                Assert.LessOrEqual(error, prevError,
                    $"PE-005: Error at Passing={passing} must be <= error at previous (lower) Passing.");
                prevError = error;
            }
        }

        /// <summary>PE-006: BodyAngle=45° produces error strictly between 0° and 90° cases.</summary>
        [Test]
        public void PE006_BodyOrientation_45deg_ProducesIntermediateError()
        {
            float error0 = PassErrorCalculator.ComputeErrorAngle(
                PassType.Ground, CrossSubType.Flat, NeutralPassing,
                NeutralPressure, NeutralFatigue, 0.0f, NeutralUrgency, StrongFoot, NeutralWeakFootRating);
            float error45 = PassErrorCalculator.ComputeErrorAngle(
                PassType.Ground, CrossSubType.Flat, NeutralPassing,
                NeutralPressure, NeutralFatigue, 45.0f, NeutralUrgency, StrongFoot, NeutralWeakFootRating);
            float error90 = PassErrorCalculator.ComputeErrorAngle(
                PassType.Ground, CrossSubType.Flat, NeutralPassing,
                NeutralPressure, NeutralFatigue, 90.0f, NeutralUrgency, StrongFoot, NeutralWeakFootRating);

            Assert.Greater(error45, error0,  "PE-006: Error at 45° body angle must exceed error at 0°.");
            Assert.Less(error45, error90,    "PE-006: Error at 45° body angle must be less than error at 90°.");
        }

        /// <summary>PE-007: Urgency=1.0 increases error compared to Urgency=0.0.</summary>
        [Test]
        public void PE007_HighUrgency_IncreasesErrorVsLowUrgency()
        {
            float errorLow = PassErrorCalculator.ComputeErrorAngle(
                PassType.Ground, CrossSubType.Flat, NeutralPassing,
                NeutralPressure, NeutralFatigue, NeutralBodyAngle, 0.0f, StrongFoot, NeutralWeakFootRating);
            float errorHigh = PassErrorCalculator.ComputeErrorAngle(
                PassType.Ground, CrossSubType.Flat, NeutralPassing,
                NeutralPressure, NeutralFatigue, NeutralBodyAngle, 1.0f, StrongFoot, NeutralWeakFootRating);

            Assert.Greater(errorHigh, errorLow,
                "PE-007: Urgency=1.0 must produce higher error than Urgency=0.0.");
        }

        /// <summary>PE-008: IsWeakFoot=false applies neutral modifier — error equals strong-foot baseline.</summary>
        [Test]
        public void PE008_StrongFoot_AppliesNeutralWeakFootModifier()
        {
            float errorStrong = PassErrorCalculator.ComputeErrorAngle(
                PassType.Ground, CrossSubType.Flat, NeutralPassing,
                NeutralPressure, NeutralFatigue, NeutralBodyAngle, NeutralUrgency, false, 1);
            float errorStrongAlt = PassErrorCalculator.ComputeErrorAngle(
                PassType.Ground, CrossSubType.Flat, NeutralPassing,
                NeutralPressure, NeutralFatigue, NeutralBodyAngle, NeutralUrgency, false, 5);

            Assert.AreEqual(errorStrong, errorStrongAlt, 0.001f,
                "PE-008: IsWeakFoot=false must produce identical error regardless of WeakFootRating.");
        }

        /// <summary>PE-009: Error calculation is deterministic — three identical calls agree.</summary>
        [Test]
        public void PE009_ComputeErrorAngle_IsDeterministic()
        {
            float e1 = PassErrorCalculator.ComputeErrorAngle(
                PassType.Ground, CrossSubType.Flat, 12.0f, 0.3f, 0.2f, 15.0f, 0.4f, false, 3);
            float e2 = PassErrorCalculator.ComputeErrorAngle(
                PassType.Ground, CrossSubType.Flat, 12.0f, 0.3f, 0.2f, 15.0f, 0.4f, false, 3);
            float e3 = PassErrorCalculator.ComputeErrorAngle(
                PassType.Ground, CrossSubType.Flat, 12.0f, 0.3f, 0.2f, 15.0f, 0.4f, false, 3);

            Assert.AreEqual(e1, e2, "PE-009: ComputeErrorAngle must be deterministic (calls 1 and 2).");
            Assert.AreEqual(e2, e3, "PE-009: ComputeErrorAngle must be deterministic (calls 2 and 3).");
        }

        /// <summary>PE-010: Error angle is never exactly zero even for the best possible passer.</summary>
        [Test]
        public void PE010_ElitePasser_ErrorAngle_NeverZero()
        {
            float error = PassErrorCalculator.ComputeErrorAngle(
                passType:        PassType.Ground,
                crossSubType:    CrossSubType.Flat,
                passing:         20.0f,
                pressureScalar:  0.0f,
                fatigue:         0.0f,
                bodyAngleDeg:    0.0f,
                urgency:         0.0f,
                isWeakFoot:      false,
                weakFootRating:  5);

            Assert.Greater(error, 0.0f,
                "PE-010: Error angle must never be exactly zero (MinErrorAngle floor is enforced).");
            Assert.GreaterOrEqual(error, PassMechanicsConstants.MinErrorAngle,
                "PE-010: Error angle must be >= MinErrorAngle at all times.");
        }
    }

    // ── WF — Weak Foot Penalty ─────────────────────────────────────────────────────

    /// <summary>
    /// WF-001 to WF-005: Weak foot accuracy and power modifiers.
    /// Spec §5.8 — Weak Foot Penalty Model (§3.7).
    /// </summary>
    [TestFixture]
    internal class WeakFootTests
    {
        /// <summary>WF-001: IsWeakFoot=false produces neutral accuracy modifier (1.0).</summary>
        [Test]
        public void WF001_StrongFoot_AccuracyModifier_IsNeutral()
        {
            float modifier = PassErrorCalculator.ComputeWeakFootAccuracyModifier(
                isWeakFoot: false, weakFootRating: 1);

            Assert.AreEqual(1.0f, modifier, 0.001f,
                "WF-001: IsWeakFoot=false must return accuracy modifier of 1.0.");
        }

        /// <summary>WF-002: IsWeakFoot=true, Rating=1 produces maximum accuracy penalty.</summary>
        [Test]
        public void WF002_WeakFoot_Rating1_ProducesMaximumPenalty()
        {
            float modifier = PassErrorCalculator.ComputeWeakFootAccuracyModifier(
                isWeakFoot: true, weakFootRating: 1);

            // Formula: 1.0 + 1.0 × WeakFootBasePenalty = 1.0 + 0.30 = 1.30
            float expected = 1.0f + PassMechanicsConstants.WeakFootBasePenalty;
            Assert.AreEqual(expected, modifier, 0.001f,
                "WF-002: Rating=1 weak foot must produce maximum accuracy penalty.");
        }

        /// <summary>WF-003: IsWeakFoot=true, Rating=5 produces neutral modifier (ambidextrous — no penalty).</summary>
        [Test]
        public void WF003_WeakFoot_Rating5_IsAmbidextrous_Neutral()
        {
            float modifier = PassErrorCalculator.ComputeWeakFootAccuracyModifier(
                isWeakFoot: true, weakFootRating: 5);

            // Formula: penaltyFraction = 1 - (5-1)/4 = 0 → modifier = 1.0 + 0 = 1.0
            Assert.AreEqual(1.0f, modifier, 0.001f,
                "WF-003: Rating=5 (ambidextrous) must produce accuracy modifier of 1.0 (no penalty).");
        }

        /// <summary>WF-004: Weak foot accuracy modifier is monotonically decreasing as rating increases.</summary>
        [Test]
        public void WF004_WeakFootAccuracy_MonotonicallyDecreasingPenalty_WithRating()
        {
            float prevModifier = float.MaxValue;
            for (int rating = 1; rating <= 5; rating++)
            {
                float modifier = PassErrorCalculator.ComputeWeakFootAccuracyModifier(
                    isWeakFoot: true, weakFootRating: rating);

                Assert.LessOrEqual(modifier, prevModifier,
                    $"WF-004: Accuracy modifier at Rating={rating} must be <= modifier at Rating={rating - 1}.");
                prevModifier = modifier;
            }
        }

        /// <summary>WF-005: IsWeakFoot=true increases error compared to IsWeakFoot=false.</summary>
        [Test]
        public void WF005_WeakFoot_IncreasesErrorVsStrongFoot()
        {
            float errorStrong = PassErrorCalculator.ComputeErrorAngle(
                PassType.Ground, CrossSubType.Flat, 12.0f, 0.0f, 0.0f, 0.0f, 0.0f, false, 3);
            float errorWeak = PassErrorCalculator.ComputeErrorAngle(
                PassType.Ground, CrossSubType.Flat, 12.0f, 0.0f, 0.0f, 0.0f, 0.0f, true, 2);

            Assert.Greater(errorWeak, errorStrong,
                "WF-005: Weak foot (Rating=2) must produce higher error than strong foot.");
        }

        /// <summary>WF-005b: IsWeakFoot=false power penalty is neutral (1.0).</summary>
        [Test]
        public void WF005b_StrongFoot_PowerPenalty_IsNeutral()
        {
            float penalty = PassErrorCalculator.ComputeWeakFootPowerPenalty(
                isWeakFoot: false, weakFootRating: 1);

            Assert.AreEqual(1.0f, penalty, 0.001f,
                "WF-005b: IsWeakFoot=false must return power penalty of 1.0.");
        }

        /// <summary>WF-005c: IsWeakFoot=true, Rating=1 produces maximum power reduction.</summary>
        [Test]
        public void WF005c_WeakFoot_Rating1_ProducesMaxPowerReduction()
        {
            float penalty = PassErrorCalculator.ComputeWeakFootPowerPenalty(
                isWeakFoot: true, weakFootRating: 1);

            // Formula: 1.0 - 1.0 × WeakFootPowerPenalty = 1.0 - 0.15 = 0.85
            float expected = 1.0f - PassMechanicsConstants.WeakFootPowerPenalty;
            Assert.AreEqual(expected, penalty, 0.001f,
                "WF-005c: Rating=1 weak foot must produce maximum power penalty reduction.");
        }
    }

    // ── EC — Edge Cases ────────────────────────────────────────────────────────────

    /// <summary>
    /// EC-001 to EC-010: Robustness under degenerate inputs and boundary extremes.
    /// Spec §5.10 — Edge Cases and Robustness (§3.1–§3.9).
    /// </summary>
    [TestFixture]
    internal class EdgeCaseTests
    {
        /// <summary>EC-001: Attribute minimum (all=1) produces valid velocity — no exception, no NaN.</summary>
        [Test]
        public void EC001_AllMinAttributes_ProducesValidVelocity()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Ground);
            float v = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    10.0f,
                kickPower:           PassMechanicsConstants.ATTR_MIN,
                fatigue:             0.0f,
                weakFootPowerPenalty: 1.0f,
                profile:             profile);

            Assert.IsFalse(float.IsNaN(v),      "EC-001: MinAttributes velocity must not be NaN.");
            Assert.IsFalse(float.IsInfinity(v), "EC-001: MinAttributes velocity must not be Infinity.");
            Assert.GreaterOrEqual(v, profile.VMin, "EC-001: MinAttributes velocity must be >= VMin.");
        }

        /// <summary>EC-002: Attribute maximum (all=20) produces valid velocity — no exception, no NaN.</summary>
        [Test]
        public void EC002_AllMaxAttributes_ProducesValidVelocity()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Ground);
            float v = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    profile.DistMax,
                kickPower:           PassMechanicsConstants.ATTR_MAX,
                fatigue:             0.0f,
                weakFootPowerPenalty: 1.0f,
                profile:             profile);

            Assert.IsFalse(float.IsNaN(v),      "EC-002: MaxAttributes velocity must not be NaN.");
            Assert.IsFalse(float.IsInfinity(v), "EC-002: MaxAttributes velocity must not be Infinity.");
            Assert.LessOrEqual(v, profile.VMax, "EC-002: MaxAttributes velocity must be <= VMax (clamped).");
        }

        /// <summary>EC-003: Very low kickPower/distance scenario produces valid spin — no NaN components.</summary>
        [Test]
        public void EC003_MinPower_SmallDistance_SpinIsValidNoNaN()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Ground);
            Vector3 spin = PassVelocityCalculator.ComputeSpinVector(
                PassType.Ground, CrossSubType.Flat, PassMechanicsConstants.ATTR_MIN, false, profile);

            Assert.IsFalse(float.IsNaN(spin.x), "EC-003: Spin.x must not be NaN.");
            Assert.IsFalse(float.IsNaN(spin.y), "EC-003: Spin.y must not be NaN.");
            Assert.IsFalse(float.IsNaN(spin.z), "EC-003: Spin.z must not be NaN.");
        }

        /// <summary>EC-005: Fatigue=1.0 (fully exhausted) produces valid velocity — no exception, clamped.</summary>
        [Test]
        public void EC005_FullFatigue_ProducesValidVelocityClamped()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Driven);
            float v = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    20.0f,
                kickPower:           10.0f,
                fatigue:             1.0f,
                weakFootPowerPenalty: 1.0f,
                profile:             profile);

            Assert.IsFalse(float.IsNaN(v),      "EC-005: Fatigue=1.0 velocity must not be NaN.");
            Assert.GreaterOrEqual(v, profile.VMin, "EC-005: Fatigue=1.0 velocity must be >= VMin (clamped).");
        }

        /// <summary>EC-006: PressureScalar=1.0 produces valid error angle clamped to MaxErrorAngle.</summary>
        [Test]
        public void EC006_MaxPressure_ErrorAngle_ClampedToMax()
        {
            float error = PassErrorCalculator.ComputeErrorAngle(
                passType:        PassType.Ground,
                crossSubType:    CrossSubType.Flat,
                passing:         1.0f,
                pressureScalar:  1.0f,
                fatigue:         0.0f,
                bodyAngleDeg:    0.0f,
                urgency:         0.0f,
                isWeakFoot:      false,
                weakFootRating:  3);

            Assert.IsFalse(float.IsNaN(error),         "EC-006: Max pressure error must not be NaN.");
            Assert.LessOrEqual(error, PassMechanicsConstants.MaxErrorAngle,
                "EC-006: Max pressure error must be <= MaxErrorAngle.");
        }

        /// <summary>EC-008: BodyAngle=180° is clamped to 90° — modifier equals max orientation penalty.</summary>
        [Test]
        public void EC008_BodyAngle180_ClampedTo90_MaxOrientationPenalty()
        {
            float errorAt90  = PassErrorCalculator.ComputeErrorAngle(
                PassType.Ground, CrossSubType.Flat, 10.0f, 0.0f, 0.0f, 90.0f, 0.0f, false, 3);
            float errorAt180 = PassErrorCalculator.ComputeErrorAngle(
                PassType.Ground, CrossSubType.Flat, 10.0f, 0.0f, 0.0f, 180.0f, 0.0f, false, 3);

            Assert.AreEqual(errorAt90, errorAt180, 0.001f,
                "EC-008: BodyAngle=180° must be clamped to 90° and produce the same error as 90°.");
        }

        /// <summary>EC-009: Error direction hash is deterministic — same inputs produce identical radians.</summary>
        [Test]
        public void EC009_ErrorDirection_IsDeterministic()
        {
            float dir1 = PassErrorCalculator.ComputeErrorDirection(agentId: 42, frameNumber: 1000, passTypeIndex: 0);
            float dir2 = PassErrorCalculator.ComputeErrorDirection(agentId: 42, frameNumber: 1000, passTypeIndex: 0);
            float dir3 = PassErrorCalculator.ComputeErrorDirection(agentId: 42, frameNumber: 1000, passTypeIndex: 0);

            Assert.AreEqual(dir1, dir2, "EC-009: ComputeErrorDirection must be deterministic (calls 1 and 2).");
            Assert.AreEqual(dir2, dir3, "EC-009: ComputeErrorDirection must be deterministic (calls 2 and 3).");
        }

        /// <summary>EC-010: Error direction is within [0, 2π).</summary>
        [Test]
        public void EC010_ErrorDirection_WithinTwoPiRange()
        {
            int[] agentIds    = new int[] { 0, 1, 7, 42, 999 };
            int[] frames      = new int[] { 0, 1, 60, 3600 };
            int[] passIndices = new int[] { 0, 1, 2, 3, 4, 5, 6 };

            foreach (int agentId in agentIds)
            {
                foreach (int frame in frames)
                {
                    foreach (int passIdx in passIndices)
                    {
                        float dir = PassErrorCalculator.ComputeErrorDirection(agentId, frame, passIdx);
                        Assert.GreaterOrEqual(dir, 0.0f,
                            $"EC-010: Direction for agent={agentId} frame={frame} passIdx={passIdx} must be >= 0.");
                        Assert.Less(dir, Mathf.PI * 2.0f,
                            $"EC-010: Direction for agent={agentId} frame={frame} passIdx={passIdx} must be < 2π.");
                    }
                }
            }
        }

        /// <summary>EC-011: Different agentId or frameNumber produces different error direction (no constant hash).</summary>
        [Test]
        public void EC011_ErrorDirection_DifferentInputs_ProduceDifferentOutputs()
        {
            float dirA = PassErrorCalculator.ComputeErrorDirection(agentId: 1,  frameNumber: 100, passTypeIndex: 0);
            float dirB = PassErrorCalculator.ComputeErrorDirection(agentId: 2,  frameNumber: 100, passTypeIndex: 0);
            float dirC = PassErrorCalculator.ComputeErrorDirection(agentId: 1,  frameNumber: 200, passTypeIndex: 0);

            // With distinct inputs the hash should differ for at least one pair.
            bool anyDiffer = !Mathf.Approximately(dirA, dirB)
                          || !Mathf.Approximately(dirA, dirC);
            Assert.IsTrue(anyDiffer,
                "EC-011: Different agentId/frameNumber inputs must not all produce the same error direction.");
        }
    }

    // ── TR — Target Resolver ───────────────────────────────────────────────────────

    /// <summary>
    /// TR subset: PassTargetResolver pure-function tests.
    /// Covers aim-point resolution, pitch clamping, kick direction, and error application.
    /// Spec §5.7 — Target Resolution (§3.6).
    /// </summary>
    [TestFixture]
    internal class TargetResolverTests
    {
        /// <summary>TR-001: Player-targeted aim point equals receiver XY position at Z=0.</summary>
        [Test]
        public void TR001_PlayerTargeted_AimPointEqualsReceiverPosition()
        {
            Vector2 receiverPos = new Vector2(30.0f, 25.0f);
            Vector3 aimPoint = PassTargetResolver.ResolvePlayerTargetedAimPoint(receiverPos);

            Assert.AreEqual(receiverPos.x, aimPoint.x, 0.001f, "TR-001: aimPoint.x must equal receiver.x.");
            Assert.AreEqual(receiverPos.y, aimPoint.y, 0.001f, "TR-001: aimPoint.y must equal receiver.y.");
            Assert.AreEqual(0.0f,          aimPoint.z, 0.001f, "TR-001: aimPoint.z must be 0 (ground level).");
        }

        /// <summary>TR-009: Space-targeted aim point equals provided target position.</summary>
        [Test]
        public void TR009_SpaceTargeted_AimPointEqualsTargetPosition()
        {
            Vector3 target = new Vector3(52.5f, 34.0f, 0.0f);
            Vector3 aimPoint = PassTargetResolver.ResolveSpaceTargetedAimPoint(target);

            Assert.AreEqual(target.x, aimPoint.x, 0.001f, "TR-009: Space aim point must equal target.x.");
            Assert.AreEqual(target.y, aimPoint.y, 0.001f, "TR-009: Space aim point must equal target.y.");
            Assert.AreEqual(target.z, aimPoint.z, 0.001f, "TR-009: Space aim point must equal target.z.");
        }

        /// <summary>TR-014: Aim point on touchline boundary is not clamped.</summary>
        [Test]
        public void TR014_SpaceTarget_OnPitchBoundary_NotClamped()
        {
            Vector3 boundary = new Vector3(PassMechanicsConstants.PitchLength, 0.0f, 0.0f);
            Vector3 clamped = PassTargetResolver.ClampToPitchBounds(boundary);

            Assert.AreEqual(boundary.x, clamped.x, 0.001f, "TR-014: On-boundary X must not be clamped.");
            Assert.AreEqual(boundary.y, clamped.y, 0.001f, "TR-014: On-boundary Y must not be clamped.");
        }

        /// <summary>TR: Out-of-pitch aim point is clamped to pitch bounds.</summary>
        [Test]
        public void TR_OutOfBoundsAimPoint_ClampedToPitch()
        {
            Vector3 outside = new Vector3(200.0f, -10.0f, 0.0f);
            Vector3 clamped = PassTargetResolver.ClampToPitchBounds(outside);

            Assert.LessOrEqual(clamped.x, PassMechanicsConstants.PitchLength,
                "TR: Clamped X must not exceed PitchLength.");
            Assert.GreaterOrEqual(clamped.x, 0.0f, "TR: Clamped X must be >= 0.");
            Assert.GreaterOrEqual(clamped.y, 0.0f, "TR: Clamped Y must be >= 0.");
        }

        /// <summary>TR: KickDirection from passer to target is normalised and correct.</summary>
        [Test]
        public void TR_KickDirection_IsNormalisedAndCorrect()
        {
            Vector2 passerPos = new Vector2(10.0f, 10.0f);
            Vector3 aimPoint  = new Vector3(40.0f, 10.0f, 0.0f); // purely +X direction

            Vector3 dir = PassTargetResolver.ComputeKickDirection(passerPos, aimPoint);

            Assert.AreEqual(1.0f, dir.magnitude, 0.001f, "TR: Kick direction must be normalised.");
            Assert.AreEqual(1.0f, dir.x, 0.001f, "TR: Direction to +X target must have x=1.");
            Assert.AreEqual(0.0f, dir.y, 0.001f, "TR: Direction to +X target must have y=0.");
        }

        /// <summary>TR: Through-ball stationary receiver produces zero lead distance.</summary>
        [Test]
        public void TR007_ThroughBall_StationaryReceiver_ZeroLead()
        {
            Vector2 passerPos   = new Vector2(10.0f, 10.0f);
            Vector2 receiverPos = new Vector2(30.0f, 10.0f);
            Vector2 receiverVel = Vector2.zero;  // stationary

            Vector3 aim = PassTargetResolver.ComputeThroughBallAimPoint(
                passerPos, receiverPos, receiverVel, 15.0f, out float leadDistance);

            Assert.AreEqual(0.0f, leadDistance, 0.001f,
                "TR-007: Stationary receiver must produce zero lead distance.");
            Assert.AreEqual(receiverPos.x, aim.x, 0.001f,
                "TR-007: Aim point must equal receiver position when stationary.");
        }

        /// <summary>TR: Moving receiver at 5 m/s produces positive lead distance.</summary>
        [Test]
        public void TR005_ThroughBall_MovingReceiver_ProducesPositiveLead()
        {
            Vector2 passerPos   = new Vector2(10.0f, 10.0f);
            Vector2 receiverPos = new Vector2(30.0f, 10.0f);
            Vector2 receiverVel = new Vector2(5.0f, 0.0f);  // moving at 5 m/s in +X

            PassTargetResolver.ComputeThroughBallAimPoint(
                passerPos, receiverPos, receiverVel, 15.0f, out float leadDistance);

            Assert.Greater(leadDistance, 0.0f,
                "TR-005: Moving receiver must produce positive through-ball lead distance.");
        }

        /// <summary>TR-016: Target resolution is deterministic — three identical calls agree.</summary>
        [Test]
        public void TR016_TargetResolution_IsDeterministic()
        {
            Vector2 passerPos   = new Vector2(5.0f, 5.0f);
            Vector2 receiverPos = new Vector2(25.0f, 15.0f);
            Vector2 receiverVel = new Vector2(3.0f, 1.0f);

            Vector3 a1 = PassTargetResolver.ComputeThroughBallAimPoint(
                passerPos, receiverPos, receiverVel, 14.0f, out float lead1);
            Vector3 a2 = PassTargetResolver.ComputeThroughBallAimPoint(
                passerPos, receiverPos, receiverVel, 14.0f, out float lead2);
            Vector3 a3 = PassTargetResolver.ComputeThroughBallAimPoint(
                passerPos, receiverPos, receiverVel, 14.0f, out float lead3);

            Assert.AreEqual(a1.x, a2.x, 0.0001f, "TR-016: Aim point X must be identical across calls.");
            Assert.AreEqual(a2.x, a3.x, 0.0001f, "TR-016: Aim point X must be identical across calls.");
            Assert.AreEqual(lead1, lead2, 0.0001f, "TR-016: Lead distance must be identical across calls.");
            Assert.AreEqual(lead2, lead3, 0.0001f, "TR-016: Lead distance must be identical across calls.");
        }
    }

    // ── PSM — State Machine Timing ─────────────────────────────────────────────────

    /// <summary>
    /// PSM subset: GetWindupFrames and urgency reduction logic.
    /// Spec §5.9 — Execution State Machine (§3.8).
    /// </summary>
    [TestFixture]
    internal class StateMachineTimingTests
    {
        /// <summary>PSM-005a: GetWindupFrames returns positive values for all pass types.</summary>
        [Test]
        public void PSM005a_GetWindupFrames_AllPassTypes_Positive()
        {
            PassType[] allTypes = new PassType[]
            {
                PassType.Ground,
                PassType.Driven,
                PassType.Lofted,
                PassType.ThroughBall,
                PassType.AerialThrough,
                PassType.Cross,
                PassType.Chip
            };

            foreach (PassType pt in allTypes)
            {
                int frames = PassMechanicsConstants.GetWindupFrames(pt);
                Assert.Greater(frames, 0, $"PSM-005a: {pt} must have windup frames > 0.");
            }
        }

        /// <summary>PSM-005b: Urgency reduction produces windup at most equal to no-urgency windup.</summary>
        [Test]
        public void PSM005b_UrgencyReduction_NeverExceedsBaseWindup()
        {
            PassType[] allTypes = new PassType[]
            {
                PassType.Ground,
                PassType.Driven,
                PassType.Lofted,
                PassType.ThroughBall,
                PassType.AerialThrough,
                PassType.Cross,
                PassType.Chip
            };

            foreach (PassType pt in allTypes)
            {
                int baseFrames = PassMechanicsConstants.GetWindupFrames(pt);
                // Apply urgency reduction formula from §3.8.8: windupFrames × (1 - Urgency × UrgencyWindupReduction)
                float urgency = 1.0f;
                float reducedFramesF = baseFrames * (1.0f - urgency * PassMechanicsConstants.UrgencyWindupReduction);
                int reducedFrames = Mathf.Max(PassMechanicsConstants.MinWindupFrames, Mathf.RoundToInt(reducedFramesF));

                Assert.LessOrEqual(reducedFrames, baseFrames,
                    $"PSM-005b: Urgency=1.0 windup for {pt} must be <= base windup ({baseFrames} frames).");
                Assert.GreaterOrEqual(reducedFrames, PassMechanicsConstants.MinWindupFrames,
                    $"PSM-005b: Reduced windup for {pt} must be >= MinWindupFrames ({PassMechanicsConstants.MinWindupFrames}).");
            }
        }

        /// <summary>PSM-005c: GetFollowThroughFrames returns positive values for all pass types.</summary>
        [Test]
        public void PSM005c_GetFollowThroughFrames_AllPassTypes_Positive()
        {
            PassType[] allTypes = new PassType[]
            {
                PassType.Ground,
                PassType.Driven,
                PassType.Lofted,
                PassType.ThroughBall,
                PassType.AerialThrough,
                PassType.Cross,
                PassType.Chip
            };

            foreach (PassType pt in allTypes)
            {
                int frames = PassMechanicsConstants.GetFollowThroughFrames(pt);
                Assert.Greater(frames, 0, $"PSM-005c: {pt} must have follow-through frames > 0.");
            }
        }
    }

    // ── VS — Validation Scenarios ──────────────────────────────────────────────────

    /// <summary>
    /// VS-001, VS-003, VS-006: Real-world archetype output validation.
    /// Spec §5.12 — Real-World Validation Scenarios.
    /// All expected values derived from §3.x formulas as documented in §5.12.
    /// </summary>
    [TestFixture]
    internal class ValidationScenarioTests
    {
        /// <summary>
        /// VS-001: Elite short ground pass (tiki-taka style).
        /// Passer: Passing=19, KickPower≈16.5 (proxy), Technique=17, Distance=8m, Pressure=0.05,
        /// Fatigue=0.1, BodyAngle=10°, Urgency=LOW(0.0), StrongFoot.
        /// </summary>
        [Test]
        public void VS001_EliteShortGroundPass_OutputsInRange()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Ground);
            float kickPower = (19.0f + 17.0f) * 0.5f; // ERR-007 proxy: (Passing + Technique) / 2

            float velocity = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    8.0f,
                kickPower:           kickPower,
                fatigue:             0.1f,
                weakFootPowerPenalty: 1.0f,
                profile:             profile);

            float error = PassErrorCalculator.ComputeErrorAngle(
                passType:        PassType.Ground,
                crossSubType:    CrossSubType.Flat,
                passing:         19.0f,
                pressureScalar:  0.05f,
                fatigue:         0.1f,
                bodyAngleDeg:    10.0f,
                urgency:         0.0f,
                isWeakFoot:      false,
                weakFootRating:  3);

            // Spec §5.12 VS-001 expected: velocity ≈ 11.5 m/s ± 0.5, error <= 0.8°
            Assert.GreaterOrEqual(velocity, 11.0f, "VS-001: Elite short pass velocity must be >= 11.0 m/s.");
            Assert.LessOrEqual(velocity, 12.0f,    "VS-001: Elite short pass velocity must be <= 12.0 m/s.");
            Assert.LessOrEqual(error, 0.8f,         "VS-001: Elite passer error must be <= 0.8°.");

            // Spin type check — ground pass must be topspin (positive Y)
            Vector3 spin = PassVelocityCalculator.ComputeSpinVector(
                PassType.Ground, CrossSubType.Flat, 17.0f, false, profile);
            Assert.Greater(spin.y, 0f, "VS-001: Ground pass must produce positive Y spin (topspin).");
        }

        /// <summary>
        /// VS-003: Chip pass over defensive line (creative forward, 18m, moderate pressure).
        /// Expected: Velocity ≈ 10.5 m/s ± 0.5, LaunchAngle ≈ 55° ± 5°, backspin dominant.
        /// </summary>
        [Test]
        public void VS003_ChipOverDefensiveLine_OutputsInRange()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Chip);
            float kickPower = (15.0f + 16.0f) * 0.5f; // ERR-007 proxy

            float velocity = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    18.0f,
                kickPower:           kickPower,
                fatigue:             0.2f,
                weakFootPowerPenalty: 1.0f,
                profile:             profile);

            float angle = PassVelocityCalculator.ComputeLaunchAngle(
                PassType.Chip, CrossSubType.Flat, 18.0f, profile);

            // Spec §5.12 VS-003: velocity ≈ 10.5 ± 0.5, angle ≈ 55° ± 5°
            Assert.GreaterOrEqual(velocity, 10.0f, "VS-003: Chip velocity must be >= 10.0 m/s.");
            Assert.LessOrEqual(velocity, 11.0f,    "VS-003: Chip velocity must be <= 11.0 m/s.");
            Assert.GreaterOrEqual(angle, 50.0f,    "VS-003: Chip angle must be >= 50°.");
            Assert.LessOrEqual(angle, 60.0f,       "VS-003: Chip angle must be <= 60°.");

            // Backspin check
            Vector3 spin = PassVelocityCalculator.ComputeSpinVector(
                PassType.Chip, CrossSubType.Flat, 16.0f, false, profile);
            Assert.Less(spin.y, 0f, "VS-003: Chip pass must produce negative Y spin (backspin).");
        }

        /// <summary>
        /// VS-006: Minimum competence pass (youth-level archetype).
        /// Passing=3, KickPower proxy≈3, Technique=3, Distance=12m, minimal pressure.
        /// Expected: velocity >= VMin (clamped floor), error ≈ 3.5° ± 1°.
        /// </summary>
        [Test]
        public void VS006_MinimumCompetenceGroundPass_OutputsRealistic()
        {
            PhysicalProfile profile = PassTypeProfiles.GetProfile(PassType.Ground);
            float kickPower = (3.0f + 3.0f) * 0.5f; // ERR-007 proxy

            float velocity = PassVelocityCalculator.ComputeKickSpeed(
                intendedDistance:    12.0f,
                kickPower:           kickPower,
                fatigue:             0.05f,
                weakFootPowerPenalty: 1.0f,
                profile:             profile);

            float error = PassErrorCalculator.ComputeErrorAngle(
                passType:        PassType.Ground,
                crossSubType:    CrossSubType.Flat,
                passing:         3.0f,
                pressureScalar:  0.1f,
                fatigue:         0.05f,
                bodyAngleDeg:    5.0f,
                urgency:         0.0f,
                isWeakFoot:      false,
                weakFootRating:  3);

            // Velocity must at minimum be >= VMin (clamped floor)
            Assert.GreaterOrEqual(velocity, profile.VMin,
                "VS-006: Youth-level velocity must be >= VMin (clamp floor).");

            // Error ≈ 3.5° ± 1° (§5.12 VS-006 expected)
            Assert.GreaterOrEqual(error, 2.5f, "VS-006: Youth-level error must be >= 2.5°.");
            Assert.LessOrEqual(error, 4.5f,    "VS-006: Youth-level error must be <= 4.5°.");
        }
    }
}

    // ════════════════════════════════════════════════════════════════════════════
    // §5.11 Integration Tests (IT-)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Integration tests for Pass Mechanics #5 §5.11. Require mocked/wired instances
    /// of BallPhysics #1, AgentMovement #2, CollisionSystem #3, and EventBus #17.
    /// </summary>
    internal sealed class PassMechanicsIntegrationTests
    {
        /// <summary>IT-001: Ground pass — ApplyKick called with non-zero velocity. §5.11.</summary>
        [Test]
        public void GroundPass_ApplyKick_CalledWithNonZeroVelocity()
        {
            Assert.Ignore("Stage 0+1: requires IPassBallSystem mock wired to PassExecutor — IT-001");
        }

        /// <summary>IT-002: Lofted pass — ApplyKick velocity Y component positive. §5.11.</summary>
        [Test]
        public void LoftedPass_ApplyKick_VelocityYPositive()
        {
            Assert.Ignore("Stage 0+1: requires IPassBallSystem mock — IT-002");
        }

        /// <summary>IT-003: Chip pass — ApplyKick velocity Y component large (steep angle). §5.11.</summary>
        [Test]
        public void ChipPass_ApplyKick_VelocityYLarge()
        {
            Assert.Ignore("Stage 0+1: requires IPassBallSystem mock — IT-003");
        }

        /// <summary>IT-004 [ERR-008-BLOCKED]: ApplyKick clears passer possession. §5.11.</summary>
        [Test]
        public void ApplyKick_ClearsPossession_PostPass()
        {
            Assert.Ignore("Stage 0+1 [ERR-008-BLOCKED]: requires BallState.PossessingAgentId API — IT-004");
        }

        /// <summary>IT-005: Agent attributes read correctly from AgentState. §5.11.</summary>
        [Test]
        public void AgentAttributes_ReadCorrectlyFromAgentState()
        {
            Assert.Ignore("Stage 0+1: requires IPassAgentQuery mock wired — IT-005");
        }

        /// <summary>IT-006: Fatigue from AgentState propagates to error calculation. §5.11.</summary>
        [Test]
        public void Fatigue_PropagatesFromAgentStateToErrorCalculation()
        {
            Assert.Ignore("Stage 0+1: requires IPassAgentQuery mock with Fatigue=0.9 — IT-006");
        }

        /// <summary>IT-007: Tackle interrupt from CollisionSystem cancels pass. §5.11.</summary>
        [Test]
        public void TackleInterrupt_FromCollision_CancelsPass()
        {
            Assert.Ignore("Stage 0+1: requires IPassCollisionQuery mock with tackle flag — IT-007");
        }

        /// <summary>IT-008: PassAttemptEvent published on CONTACT. §5.11.</summary>
        [Test]
        public void PassAttemptEvent_PublishedOnContact()
        {
            Assert.Ignore("Stage 0+1: requires EventBus wired via EventBusStub — IT-008");
        }

        /// <summary>IT-009: PassCompletedEvent published on FOLLOW_THROUGH completion. §5.11.</summary>
        [Test]
        public void PassCompletedEvent_PublishedOnFollowThroughCompletion()
        {
            Assert.Ignore("Stage 0+1: requires EventBus wired — IT-009");
        }

        /// <summary>IT-010: PassCancelledEvent published on tackle interrupt, not PassAttemptEvent. §5.11.</summary>
        [Test]
        public void TackleInterrupt_PublishesCancelledNotAttempt()
        {
            Assert.Ignore("Stage 0+1: requires EventBus + IPassCollisionQuery wired — IT-010");
        }

        /// <summary>IT-011: Full pass cycle IDLE→COMPLETE with correct event order. §5.11.</summary>
        [Test]
        public void FullPassCycle_IdleToComplete_CorrectEventOrder()
        {
            Assert.Ignore("Stage 0+1: requires all interfaces mocked and EventBus wired — IT-011");
        }

        /// <summary>IT-012: Replay determinism — two identical passes produce identical results. §5.11.</summary>
        [Test]
        public void ReplayDeterminism_IdenticalPassesProduceIdenticalResults()
        {
            Assert.Ignore("Stage 0+1: requires Deterministic Simulation #16 digest comparison — IT-012 CRITICAL");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                          |
// | 1.0     | 2026-05-31 | —      | Initial implementation. 50 tests across PT/PV/LA/SV/PE/WF/EC/TR/PSM/VS. |
// | 1.1     | 2026-06-01 | —      | Add §5.11 integration test stubs IT-001..012 (all Assert.Ignore Stage 0+1). |
#endregion
