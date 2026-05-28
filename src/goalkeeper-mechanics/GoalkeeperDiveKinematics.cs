// File:     src/goalkeeper-mechanics/GoalkeeperDiveKinematics.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.3, KD-12, Code Standards #20
// Purpose:  Stage 0 synthetic dive trajectory (XY launch impulse + Z parabolic arc) owned
//           by Spec #11 per KD-12. All methods are pure static with no side effects.

using UnityEngine;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Stage 0 synthetic GK dive trajectory formulas per §3.3 / KD-12.
    /// AM #2 §3.6 defers Z>0 kinematics; #11 owns the dive's Z component until Stage 1 migration.
    /// All methods are deterministic and side-effect-free.
    /// Goalkeeper Mechanics #11 §3.3.
    /// </summary>
    public static class GoalkeeperDiveKinematics
    {
        /// <summary>
        /// Computes the dive launch impulse (m/s) along the dive direction.
        /// Formula: DIVE_LAUNCH_BASE_MPS + DIVE_LAUNCH_K_STRENGTH × Strength_norm
        ///          + DIVE_LAUNCH_K_AERIAL × Aerial_norm - DIVE_LAUNCH_FATIGUE_COEFF × fatigue.
        /// §3.3.1. Goalkeeper Mechanics #11 §3.3.
        /// </summary>
        /// <param name="attrs">GK agent attributes. §3.3.1.</param>
        /// <param name="diveDirectionX">Signed dive direction in X-axis (∈ {-1, 0, +1}). §3.3.1.</param>
        /// <returns>Signed dive launch impulse (m/s) along diveDirectionX.</returns>
        public static float ComputeDiveLaunchImpulse(GoalkeeperAgentAttributes attrs, float diveDirectionX)
        {
            float magnitude = GoalkeeperConstants.DiveLaunchBaseMps
                            + GoalkeeperConstants.DiveLaunchKStrength * attrs.StrengthNorm
                            + GoalkeeperConstants.DiveLaunchKAerial   * attrs.AerialNorm
                            - GoalkeeperConstants.DiveLaunchFatigueCoeff * attrs.Fatigue;

            return magnitude * diveDirectionX;
        }

        /// <summary>
        /// Computes the peak hand Z (m) including timing-jitter perturbation.
        /// Formula: DIVE_PEAK_Z_BASE_M + DIVE_PEAK_Z_K_AERIAL × Aerial_norm
        ///          + DIVE_PEAK_Z_K_STRENGTH × Strength_norm - DIVE_FATIGUE_PEAK_Z_COEFF × fatigue
        ///          + diveTimingJitterMs × DIVE_JITTER_PEAK_Z_COEFF.
        /// §3.3.3. Goalkeeper Mechanics #11 §3.3.
        /// </summary>
        /// <param name="attrs">GK agent attributes. §3.3.3.</param>
        /// <param name="diveTimingJitterMs">Gaussian timing jitter (ms) from §3.3.2. §3.3.3.</param>
        /// <returns>Peak hand Z (m) at dive apex.</returns>
        public static float ComputePeakHandZ(GoalkeeperAgentAttributes attrs, float diveTimingJitterMs)
        {
            return GoalkeeperConstants.DivePeakZBaseM
                 + GoalkeeperConstants.DivePeakZKAerial   * attrs.AerialNorm
                 + GoalkeeperConstants.DivePeakZKStrength  * attrs.StrengthNorm
                 - GoalkeeperConstants.DiveFatiguePeakZCoeff * attrs.Fatigue
                 + diveTimingJitterMs * GoalkeeperConstants.DiveJitterPeakZCoeff;
        }

        /// <summary>
        /// Computes the hand Z position (m) at the given physics frame using the synthetic parabolic trajectory.
        /// Formula: handPathZ = peakHandZ × max(0, 1 - ((currentFrame - apexFrame) / halfDurationFrm)²).
        /// Returns 0 outside the dive window.
        /// §3.3.3. Goalkeeper Mechanics #11 §3.3.
        /// </summary>
        /// <param name="currentFrame">Current 60 Hz physics frame. §3.3.3.</param>
        /// <param name="diveLaunchFrame">Frame at which the dive impulse was applied. §3.3.3.</param>
        /// <param name="diveDurationFrm">Total dive phase duration in frames (from ComputeDiveDurationFrames). §3.3.3.</param>
        /// <param name="peakHandZ">Peak hand Z (m) from ComputePeakHandZ. §3.3.3.</param>
        /// <returns>Hand Z (m) at currentFrame. 0 outside [diveLaunchFrame, diveLaunchFrame + diveDurationFrm].</returns>
        public static float ComputeHandPathZ(int currentFrame, int diveLaunchFrame, int diveDurationFrm, float peakHandZ)
        {
            int offset = currentFrame - diveLaunchFrame;
            if (offset < 0 || offset > diveDurationFrm || diveDurationFrm <= 0)
            {
                return 0.0f;
            }

            int apexFrame   = diveLaunchFrame + diveDurationFrm / 2;
            int halfDuration = diveDurationFrm / 2;

            if (halfDuration <= 0)
            {
                return 0.0f;
            }

            float u = (float)(currentFrame - apexFrame) / halfDuration;
            float parabolicFactor = 1.0f - u * u;
            return peakHandZ * (parabolicFactor > 0.0f ? parabolicFactor : 0.0f);
        }

        /// <summary>
        /// Computes the reach radius (m) of the hand envelope during this dive.
        /// Formula: (ARM_LENGTH_M + DIVE_BODY_REACH_EXTENSION_M)
        ///          + REACH_K_HANDLING × Handling_norm + REACH_K_AERIAL × Aerial_norm
        ///          + REACH_K_STRENGTH × Strength_norm - REACH_FATIGUE_COEFF × fatigue.
        /// §3.3.4. Goalkeeper Mechanics #11 §3.3.
        /// </summary>
        /// <param name="attrs">GK agent attributes. §3.3.4.</param>
        /// <returns>Reach radius (m) of the hand envelope at any frame in the dive.</returns>
        public static float ComputeReachRadius(GoalkeeperAgentAttributes attrs)
        {
            float reachRadiusBase = GoalkeeperConstants.ArmLengthM
                                  + GoalkeeperConstants.DiveBodyReachExtensionM;

            return reachRadiusBase
                 + GoalkeeperConstants.ReachKHandling  * attrs.HandlingNorm
                 + GoalkeeperConstants.ReachKAerial    * attrs.AerialNorm
                 + GoalkeeperConstants.ReachKStrength   * attrs.StrengthNorm
                 - GoalkeeperConstants.ReachFatigueCoeff * attrs.Fatigue;
        }

        /// <summary>
        /// Computes the world-space centre of the hand reach envelope at the given frame.
        /// Formula: reachCenter = (gkPos.x + diveDirectionX × DIVE_LAUNCH_DISPLACEMENT_M × t,
        ///                         gkPos.y, handPathZ)
        /// where t = (currentFrame - diveLaunchFrame) / diveDurationFrm ∈ [0, 1].
        /// §3.3.4. Goalkeeper Mechanics #11 §3.3.
        /// </summary>
        /// <param name="gkPos">Current GK XYZ position from AM #2 kinematics. §3.3.4.</param>
        /// <param name="currentFrame">Current 60 Hz physics frame. §3.3.4.</param>
        /// <param name="diveLaunchFrame">Frame at which the dive impulse was applied. §3.3.4.</param>
        /// <param name="diveDurationFrm">Total dive phase duration in frames. §3.3.4.</param>
        /// <param name="diveDirectionX">Signed dive direction in X (∈ {-1, 0, +1}). §3.3.4.</param>
        /// <param name="handPathZ">Hand Z (m) at currentFrame from ComputeHandPathZ. §3.3.4.</param>
        /// <returns>World-space centre of the hand reach envelope.</returns>
        public static Vector3 ComputeReachCenter(
            Vector3 gkPos,
            int currentFrame,
            int diveLaunchFrame,
            int diveDurationFrm,
            float diveDirectionX,
            float handPathZ)
        {
            float t = diveDurationFrm > 0
                ? Clamp01((float)(currentFrame - diveLaunchFrame) / diveDurationFrm)
                : 0.0f;

            float reachX = gkPos.x + diveDirectionX * GoalkeeperConstants.DiveLaunchDisplacementM * t;

            return new Vector3(reachX, gkPos.y, handPathZ);
        }

        /// <summary>
        /// Computes the dive phase duration in 60 Hz physics frames.
        /// Formula: round(DIVE_PHASE_DURATION_MS / FRAME_MS).
        /// Stage 0: flat duration; attribute-scaling deferred per §7.4.
        /// §3.3.3. Goalkeeper Mechanics #11 §3.3.
        /// </summary>
        /// <returns>Dive phase duration in physics frames.</returns>
        public static int ComputeDiveDurationFrames()
        {
            return Mathf.RoundToInt(GoalkeeperConstants.DivePhaseDurationMs / GoalkeeperConstants.FrameMs);
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        private static float Clamp01(float v)
        {
            return v < 0.0f ? 0.0f : v > 1.0f ? 1.0f : v;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
