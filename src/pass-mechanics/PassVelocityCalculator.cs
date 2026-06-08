// File:     src/pass-mechanics/PassVelocityCalculator.cs
// Created:  2026-05-26
// Modified: 2026-06-08
// Author:   —
// Spec:     Pass Mechanics #5 §3.2, §3.3, §3.4, Code Standards #20
// Purpose:  Pure static calculator for kick speed (§3.2), launch angle (§3.3),
//           spin vector (§3.4), and kick velocity Vector3 construction (§3.3.6).
//           No side effects. Deterministic given identical inputs.
//           Coordinate system: Z-up (X=pitch length, Y=pitch width, Z=height).

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Computes kick speed, launch angle, spin vector, and kick velocity Vector3.
    /// Pure calculations — no state, no side effects. Pass Mechanics #5 §3.2–3.4.
    /// </summary>
    internal static class PassVelocityCalculator
    {
        // ── §3.2 — Kick Speed ───────────────────────────────────────────────────────

        /// <summary>
        /// Computes the scalar kick speed (m/s) from pass type, distance, KickPower,
        /// fatigue, weak foot penalty, and physical profile. Pass Mechanics #5 §3.2.7.
        /// Formula: V_base = vOffset + (K/ATTR_MAX) × (D/distMax) × (vMax - vOffset)
        /// V_unclamped = V_base × FatigueModifier × weakFootPowerPenalty
        /// kickSpeed = clamp(V_unclamped, vMin, vMax)
        /// </summary>
        /// <param name="intendedDistance">Distance to target in metres (> 0).</param>
        /// <param name="kickPower">Agent KickPower [1, 20]. [TEMPORARY-PROXY-ERR-007]</param>
        /// <param name="fatigue">Agent fatigue [0, 1]. 0=rested, 1=fatigued.</param>
        /// <param name="weakFootPowerPenalty">Penalty from §3.7.4. [0.85, 1.0].</param>
        /// <param name="profile">Physical profile from PassTypeProfiles (§3.1.4).</param>
        /// <returns>Kick speed in m/s clamped to [vMin, vMax].</returns>
        public static float ComputeKickSpeed(
            float intendedDistance,
            float kickPower,
            float fatigue,
            float weakFootPowerPenalty,
            in PhysicalProfile profile)
        {
            float K = Mathf.Clamp(kickPower, PassMechanicsConstants.ATTR_MIN, PassMechanicsConstants.ATTR_MAX);
            float D = Mathf.Min(Mathf.Max(intendedDistance, 0.001f), profile.DistMax);

            float powerScale = (K / PassMechanicsConstants.ATTR_MAX) * (D / profile.DistMax);
            float vBase = profile.VOffset + powerScale * (profile.VMax - profile.VOffset);

            float fatigueModifier = 1.0f - (Mathf.Clamp01(fatigue) * PassMechanicsConstants.FatiguePowerReduction);
            float vUnclamped = vBase * fatigueModifier
                * Mathf.Clamp(weakFootPowerPenalty, PassMechanicsConstants.WeakFootPowerFloorMin, 1.0f);

            float kickSpeed = Mathf.Clamp(vUnclamped, profile.VMin, profile.VMax);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (kickSpeed <= profile.VMin + 0.001f)
                Debug.Log($"[PassVelocity] kickSpeed clamped to vMin ({profile.VMin:F2}). K={K:F1}, D={D:F1}, Fatigue={fatigue:F2}");
#endif
            return kickSpeed;
        }

        // ── §3.3 — Launch Angle ─────────────────────────────────────────────────────

        /// <summary>
        /// Computes the launch angle in degrees above horizontal. Uses linear interpolation
        /// for ground-trajectory types and apex-derived formula for aerial types.
        /// Pass Mechanics #5 §3.3.3, §3.3.4.
        /// </summary>
        /// <param name="passType">Determines which formula path to use.</param>
        /// <param name="crossSubType">Sub-type for cross; used only when passType == Cross.</param>
        /// <param name="intendedDistance">Distance to target in metres.</param>
        /// <param name="profile">Physical profile containing angleMin, angleMax, distMax.</param>
        /// <returns>Launch angle in degrees, clamped to [angleMin, angleMax].</returns>
        public static float ComputeLaunchAngle(
            PassType passType,
            CrossSubType crossSubType,
            float intendedDistance,
            in PhysicalProfile profile)
        {
            float D = Mathf.Min(Mathf.Max(intendedDistance, 0.001f), profile.DistMax);
            float theta;

            if (IsAerialFormula(passType, crossSubType))
            {
                float apexH = GetApexHeight(passType);
                // θ = atan(4H / D) — §3.3.4
                float atanArg = (4.0f * apexH) / D;
                theta = Mathf.Atan(atanArg) * Mathf.Rad2Deg;
            }
            else
            {
                // θ = angleMin + (D / distMax) × (angleMax - angleMin) — §3.3.3
                theta = profile.AngleMin + (D / profile.DistMax) * (profile.AngleMax - profile.AngleMin);
            }

            float clamped = Mathf.Clamp(theta, profile.AngleMin, profile.AngleMax);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (!Mathf.Approximately(theta, clamped))
                Debug.Log($"[LaunchAngle] Clamped for {passType}: computed={theta:F1}° → clamped={clamped:F1}°");
#endif
            return clamped;
        }

        // ── §3.4 — Spin Vector ──────────────────────────────────────────────────────

        /// <summary>
        /// Computes the spin Vector3 (angular velocity, rad/s) for a pass.
        /// Axes are corrected for the project's Z-up coordinate system:
        /// X=pitch-length, Y=pitch-width, Z=up (CLAUDE.md Coordinate System).
        /// Topspin = (0, +mag, 0), Backspin = (0, −mag, 0), Sidespin = (0, 0, ±mag).
        /// With Magnus formula F ∝ Cross(ω̂, v̂) and ball moving in +X:
        ///   topspin ω=(0,1,0) → F=(0,0,-1) downward ✓
        ///   backspin ω=(0,-1,0) → F=(0,0,+1) upward ✓
        ///   sidespin ω=(0,0,±1) → F=(0,±1,0) lateral curl ✓
        /// Pass Mechanics #5 §3.4.5, §3.4.6.
        /// </summary>
        /// <param name="passType">Pass type (determines spin axis).</param>
        /// <param name="crossSubType">Sub-type for cross only.</param>
        /// <param name="technique">Agent Technique [1, 20].</param>
        /// <param name="isWeakFoot">Inverts sidespin sign for crosses.</param>
        /// <param name="profile">Physical profile with spinMagnitudeBase/Max.</param>
        /// <returns>Spin vector in rad/s for Ball.ApplyKick().</returns>
        public static Vector3 ComputeSpinVector(
            PassType passType,
            CrossSubType crossSubType,
            float technique,
            bool isWeakFoot,
            in PhysicalProfile profile)
        {
            float T = Mathf.Clamp(technique, PassMechanicsConstants.ATTR_MIN, PassMechanicsConstants.ATTR_MAX);

            float techScale = PassMechanicsConstants.TechniqueSpinMin
                            + ((T - 1.0f) / (PassMechanicsConstants.ATTR_MAX - 1.0f))
                            * (PassMechanicsConstants.TechniqueSpinMax - PassMechanicsConstants.TechniqueSpinMin);

            float spinMag = Mathf.Clamp(
                profile.SpinMagnitudeBase * techScale,
                PassMechanicsConstants.SPIN_MIN,
                profile.SpinMagnitudeMax);

            float sidespinSign = isWeakFoot ? -1.0f : 1.0f;

            switch (passType)
            {
                case PassType.Ground:
                case PassType.Driven:
                case PassType.ThroughBall:
                    // Topspin around Y axis (Z-up): F ∝ Cross((0,1,0),(1,0,0)) = (0,0,-1) downward ✓
                    return new Vector3(0f, spinMag, 0f);

                case PassType.Lofted:
                case PassType.AerialThrough:
                    // Mild topspin — reduced magnitude holds ball up slightly on descent
                    return new Vector3(0f, spinMag * PassMechanicsConstants.LoftedTopspinFraction, 0f);

                case PassType.Chip:
                    // Backspin around -Y axis: F ∝ Cross((0,-1,0),(1,0,0)) = (0,0,+1) upward ✓
                    return new Vector3(0f, -spinMag, 0f);

                case PassType.Cross:
                    if (crossSubType == CrossSubType.High)
                    {
                        // Mixed: topspin (Y) + sidespin (Z) in equal fractions
                        float mix = PassMechanicsConstants.CrossHighMixFraction;
                        return new Vector3(0f, spinMag * mix, sidespinSign * spinMag * mix);
                    }
                    else // Flat or Whipped
                    {
                        // Sidespin around Z axis: F ∝ Cross((0,0,±1),(1,0,0)) = (0,±1,0) lateral ✓
                        return new Vector3(0f, 0f, sidespinSign * spinMag);
                    }

                default:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError($"[SpinVector] Unexpected PassType: {passType}");
#endif
                    return Vector3.zero;
            }
        }

        // ── §3.3.6 — Velocity Vector3 Construction ──────────────────────────────────

        /// <summary>
        /// Constructs the full 3D kick velocity Vector3 from scalar speed, horizontal
        /// direction, and launch angle. Z-up coordinate system: vertical component → Z.
        /// Pass Mechanics #5 §3.3.6.
        /// </summary>
        /// <param name="kickSpeed">Scalar speed in m/s from §3.2.</param>
        /// <param name="kickDirection">Normalised horizontal direction (Z must be 0).</param>
        /// <param name="launchAngleDeg">Launch angle in degrees from §3.3.</param>
        /// <returns>3D velocity vector for Ball.ApplyKick().</returns>
        public static Vector3 ConstructKickVelocity(float kickSpeed, Vector3 kickDirection, float launchAngleDeg)
        {
            float thetaRad = launchAngleDeg * Mathf.Deg2Rad;
            float vHorizontal = kickSpeed * Mathf.Cos(thetaRad);
            float vVertical   = kickSpeed * Mathf.Sin(thetaRad);

            // Z-up: vertical component is Z; horizontal components are X and Y
            return new Vector3(
                kickDirection.x * vHorizontal,
                kickDirection.y * vHorizontal,
                vVertical);
        }

        // ── Helpers ─────────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the apex-derived angle formula applies (§3.3.4).
        /// Applies to: Lofted, AerialThrough, Cross (High), Chip.
        /// </summary>
        internal static bool IsAerialFormula(PassType passType, CrossSubType crossSubType)
        {
            if (passType == PassType.Cross)
                return crossSubType == CrossSubType.High;

            return passType == PassType.Lofted
                || passType == PassType.AerialThrough
                || passType == PassType.Chip;
        }

        private static float GetApexHeight(PassType passType)
        {
            if (passType == PassType.Cross)
                return PassMechanicsConstants.ApexHeightCrossHigh;

            switch (passType)
            {
                case PassType.Lofted:       return PassMechanicsConstants.ApexHeightLofted;
                case PassType.Chip:         return PassMechanicsConstants.ApexHeightChip;
                case PassType.AerialThrough: return PassMechanicsConstants.ApexHeightAerialThrough;
                default:
#if UNITY_EDITOR || DEVELOPMENT_BUILD
                    Debug.LogError($"[LaunchAngle] GetApexHeight called for non-aerial type {passType}");
#endif
                    return PassMechanicsConstants.ApexHeightLofted;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                   |
// | 1.0     | 2026-05-26 | —      | Initial implementation.                                                 |
// | 1.1     | 2026-05-26 | —      | M4: Removed unused crossSubType param from private GetApexHeight().     |
// | 1.2     | 2026-05-27 | —      | AR-1 H-1: class changed public → internal; CS0050/CS0051 compile error  |
// |         |            |        |     (public methods exposing internal PhysicalProfile type).            |
// |         |            |        | AR-1 M-2: diagnostic Debug.Log calls guarded by                         |
// |         |            |        |     #if DEVELOPMENT_BUILD || UNITY_EDITOR (FR-CS-027-034).             |
// | 1.3     | 2026-06-06 | —      | AR-2 L-4: 0.5f magic floor on weakFootPowerPenalty clamp replaced       |
// |         |            |        |     with PassMechanicsConstants.WeakFootPowerFloorMin (FR-CS-016).      |
// | 1.4     | 2026-06-08 | —      | AR-7 M-1: the two remaining ungated Debug.LogError emits with $"…"      |
// |         |            |        |     interpolation (ComputeSpinVector default arm, GetApexHeight default |
// |         |            |        |     arm) gated behind #if UNITY_EDITOR || DEVELOPMENT_BUILD (FR-CS-031). |
// |         |            |        |     The two Debug.Log calls at lines 57 and 100 were already gated by   |
// |         |            |        |     AR-1 M-2; this completes file-wide compliance with the AR-2 L-13    |
// |         |            |        |     precedent set in PassMechanicsConstants.                            |
#endregion
