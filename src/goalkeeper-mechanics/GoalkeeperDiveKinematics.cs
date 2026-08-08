// File:     src/goalkeeper-mechanics/GoalkeeperDiveKinematics.cs
// Created:  2026-05-28
// Modified: 2026-06-14
// Modified: 2026-07-28 (gk-contact-rate (ERR-011-007): TryPredictPlaneCrossing (the shared crossing predictor) + ComputeDiveCommitLeadS + ShouldCommitDive — the SS3.3.6 commit-to-arrival gate. See docs/tracking/gk-contact-rate-design.md)
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
        /// STAGE 0 STATUS: this §3.3.1 formula is verified in isolation (T-5.1.3.1) but is NOT
        /// invoked by GoalkeeperMechanics — the Stage 0 synthetic dive moves the hand-reach envelope
        /// via ComputeReachCenter's DIVE_LAUNCH_DISPLACEMENT_M displacement rather than applying this
        /// impulse to the GK body. It binds to AM #2 §3.5.1 kinematics at Stage 1 (KD-12 migration);
        /// do NOT also displace reachCenter once the impulse drives real body motion (double-count).
        /// §3.3.1. Goalkeeper Mechanics #11 §3.3.
        /// </summary>
        /// <param name="attrs">GK agent attributes. §3.3.1.</param>
        /// <param name="diveDirectionLateral">Signed lateral dive direction in the Y-axis (∈ {-1, 0, +1}). §3.3.1.</param>
        /// <returns>Signed dive launch impulse (m/s) along diveDirectionLateral.</returns>
        public static float ComputeDiveLaunchImpulse(GoalkeeperAgentAttributes attrs, float diveDirectionLateral)
        {
            float magnitude = GoalkeeperConstants.DiveLaunchBaseMps
                            + GoalkeeperConstants.DiveLaunchKStrength * attrs.StrengthNorm
                            + GoalkeeperConstants.DiveLaunchKAerial   * attrs.AerialNorm
                            - GoalkeeperConstants.DiveLaunchFatigueCoeff * attrs.Fatigue;

            return magnitude * diveDirectionLateral;
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
        /// Formula: reachCenter = (gkPos.x,
        ///                         gkPos.y + diveDirectionLateral × DIVE_LAUNCH_DISPLACEMENT_M × t,
        ///                         handPathZ)
        /// where t = (currentFrame - diveLaunchFrame) / diveDurationFrm ∈ [0, 1].
        /// The lateral dive axis is Y (touchline-to-touchline): the goal mouth spans Y, so the keeper
        /// dives left/right across the goal along Y, not along the goal-to-goal X axis (§1.2 / §3.3.4).
        /// §3.3.4. Goalkeeper Mechanics #11 §3.3.
        /// </summary>
        /// <param name="gkPos">Current GK XYZ position from AM #2 kinematics. §3.3.4.</param>
        /// <param name="currentFrame">Current 60 Hz physics frame. §3.3.4.</param>
        /// <param name="diveLaunchFrame">Frame at which the dive impulse was applied. §3.3.4.</param>
        /// <param name="diveDurationFrm">Total dive phase duration in frames. §3.3.4.</param>
        /// <param name="diveDirectionLateral">Signed lateral dive direction in Y (∈ {-1, 0, +1}). §3.3.4.</param>
        /// <param name="handPathZ">Hand Z (m) at currentFrame from ComputeHandPathZ. §3.3.4.</param>
        /// <returns>World-space centre of the hand reach envelope.</returns>
        public static Vector3 ComputeReachCenter(
            Vector3 gkPos,
            int currentFrame,
            int diveLaunchFrame,
            int diveDurationFrm,
            float diveDirectionLateral,
            float handPathZ)
        {
            float t = diveDurationFrm > 0
                ? Clamp01((float)(currentFrame - diveLaunchFrame) / diveDurationFrm)
                : 0.0f;

            float reachY = gkPos.y + diveDirectionLateral * GoalkeeperConstants.DiveLaunchDisplacementM * t;

            return new Vector3(gkPos.x, reachY, handPathZ);
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

        // ── Commit-to-arrival timing (ERR-011-007) ───────────────────────────────────

        /// <summary>
        /// Predicts when and where the ball will cross the keeper's own X plane — the ERR-011-003
        /// linear interception, extracted here so the dive DIRECTION (GoalkeeperMechanics) and the
        /// dive COMMIT gate (<see cref="ShouldCommitDive"/>) share one derivation and cannot drift.
        /// Returns false when the ball is not closing on the plane (moving away, lateral, or
        /// degenerate vx) or the crossing lies beyond DivePredictionHorizonS — the caller must not
        /// dive at an extrapolation of a ball that is not coming.
        /// Pure, allocation-free, deterministic (no clock read, no draw). §3.3.4 / ERR-011-007.
        /// </summary>
        /// <param name="gkPosition">Keeper XY position (world frame). §3.3.4.</param>
        /// <param name="ballState">Current ball state. §3.3.4.</param>
        /// <param name="timeToPlaneS">Out: seconds until the ball reaches the keeper's X plane.</param>
        /// <param name="predictedY">Out: the ball's Y when it crosses that plane.</param>
        /// <returns>True when a crossing is predicted inside the horizon.</returns>
        public static bool TryPredictPlaneCrossing(
            Vector2 gkPosition, in BallPhysics.BallState ballState,
            out float timeToPlaneS, out float predictedY)
        {
            float dx = gkPosition.x - ballState.Position.x;
            float vx = ballState.Velocity.x;

            timeToPlaneS = 0.0f;
            predictedY = ballState.Position.y;

            if (Mathf.Abs(vx) <= GoalkeeperConstants.DegeneracyEpsilon || dx * vx <= 0.0f)
            {
                // Ball already at/behind the plane counts as an immediate (t = 0) crossing at its
                // current Y — a smother situation, never a hold.
                if (Mathf.Abs(dx) <= GoalkeeperConstants.DegeneracyEpsilon)
                {
                    return true;
                }
                return false;
            }

            float t = dx / vx;
            if (t > GoalkeeperConstants.DivePredictionHorizonS)
            {
                return false;
            }

            timeToPlaneS = t;
            predictedY = ballState.Position.y + ballState.Velocity.y * t;
            return true;
        }

        /// <summary>
        /// The commit lead (s): how long before the ball's plane crossing the dive should launch so
        /// the hand envelope is at the predicted lateral offset when the ball arrives.
        /// Formula: clamp(lateralNeedM / DIVE_LAUNCH_DISPLACEMENT_M, DiveCommitMinLeadFrac, 1)
        ///          × DIVE_PHASE_DURATION_MS / 1000.
        /// The envelope reaches lateral offset L at fraction L / DIVE_LAUNCH_DISPLACEMENT_M of the
        /// dive, so launching at exactly that lead times full-need extension to the arrival: a
        /// corner ball gets the whole dive, a central ball a short sharp commit (floored so the
        /// lead never degenerates to zero). §3.3.4 / ERR-011-007.
        /// </summary>
        /// <param name="lateralNeedM">|predictedY − gk.y| — the lateral distance to cover (m).</param>
        /// <returns>Commit lead in seconds, ∈ [DiveCommitMinLeadFrac × D, D] for dive duration D.</returns>
        public static float ComputeDiveCommitLeadS(float lateralNeedM)
        {
            float frac = lateralNeedM / GoalkeeperConstants.DiveLaunchDisplacementM;
            if (frac < GoalkeeperConstants.DiveCommitMinLeadFrac)
            {
                frac = GoalkeeperConstants.DiveCommitMinLeadFrac;
            }
            else if (frac > 1.0f)
            {
                frac = 1.0f;
            }
            return frac * GoalkeeperConstants.DivePhaseDurationMs / GoalkeeperConstants.MS_PER_SECOND;
        }

        /// <summary>
        /// The Anticipate → Diving commit gate (ERR-011-007): a keeper with a committed SaveIntent
        /// HOLDS its coiled Anticipate posture until the ball's predicted time-to-plane is inside
        /// the commit lead, so the dive's 600 ms envelope covers the arrival instead of closing
        /// before it. A ball that is not closing (deflected away, possessed, lateral) holds — the
        /// engine's disarm path ends the episode, so the hold cannot deadlock (Anticipate keeps its
        /// ERR-011-002 → Set exit). A ball already inside the lead commits immediately: the
        /// pre-fix behaviour, preserved for close-range shots. §3.1 / §3.3.4 / ERR-011-007.
        /// </summary>
        /// <param name="gkPosition">Keeper XY position (world frame).</param>
        /// <param name="ballState">Current ball state.</param>
        /// <returns>True when the dive should launch now.</returns>
        public static bool ShouldCommitDive(Vector2 gkPosition, in BallPhysics.BallState ballState)
        {
            if (!TryPredictPlaneCrossing(gkPosition, in ballState, out float timeToPlaneS, out float predictedY))
            {
                return false;
            }

            float lateralNeedM = Mathf.Abs(predictedY - gkPosition.y);
            return timeToPlaneS <= ComputeDiveCommitLeadS(lateralNeedM);
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        private static float Clamp01(float v)
        {
            return v < 0.0f ? 0.0f : v > 1.0f ? 1.0f : v;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                        |
// | 1.1     | 2026-06-14 | —      | AR-3 M-2: lateral dive axis X→Y. The goal mouth spans Y        |
// |         |            |        | (touchline-to-touchline), so reachCenter now displaces along Y |
// |         |            |        | (gkPos.x fixed); diveDirectionX renamed diveDirectionLateral.  |
// | 1.2     | 2026-06-14 | —      | AR-6 L: doc-note on ComputeDiveLaunchImpulse — tested in       |
// |         |            |        | isolation but not invoked at Stage 0 (synthetic reachCenter    |
// |         |            |        | displacement is used); double-count hazard flagged for the     |
// |         |            |        | Stage 1 AM-kinematics binding.                                 |
// | 1.3     | 2026-07-28 | —      | gk-contact-rate (ERR-011-007): + TryPredictPlaneCrossing (the      |
// |         |            |        | one crossing predictor, shared by the dive direction and the    |
// |         |            |        | commit gate), + ComputeDiveCommitLeadS (lateral-need-scaled     |
// |         |            |        | lead, floored by DiveCommitMinLeadFrac), + ShouldCommitDive     |
// |         |            |        | (the SS3.3.6 Anticipate->Diving timing gate). Pure, draw-free.  |
#endregion
