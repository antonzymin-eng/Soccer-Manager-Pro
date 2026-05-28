// File:     src/goalkeeper-mechanics/GoalkeeperRushDispatch.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.7, KD-15, Code Standards #20
// Purpose:  Rush launch impulse and per-frame position update. Intent-staleness policy: rushTarget
//           is locked at commit and never re-read during the rush (KD-15).

using UnityEngine;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// GK rush dispatch formulas per §3.7 / KD-15.
    /// RushTarget is locked at commit time (intent-staleness policy — mirrors Heading #10 KD-17).
    /// All methods are pure static with no side effects.
    /// Goalkeeper Mechanics #11 §3.7.
    /// </summary>
    public static class GoalkeeperRushDispatch
    {
        /// <summary>
        /// Computes the rush launch speed (m/s).
        /// Formula: RUSH_LAUNCH_BASE_MPS + RUSH_LAUNCH_K_PACE × Pace_norm
        ///          - RUSH_COMMIT_FATIGUE_COEFF × fatigue.
        /// §3.7.1. Goalkeeper Mechanics #11 §3.7.
        /// </summary>
        /// <param name="attrs">GK agent attributes. §3.7.1.</param>
        /// <returns>Rush launch speed (m/s).</returns>
        public static float ComputeRushLaunchMps(GoalkeeperAgentAttributes attrs)
        {
            return GoalkeeperConstants.RushLaunchBaseMps
                 + GoalkeeperConstants.RushLaunchKPace * attrs.PaceNorm
                 - GoalkeeperConstants.RushCommitFatigueCoeff * attrs.Fatigue;
        }

        /// <summary>
        /// Advances GK position one physics frame along the locked rush direction.
        /// Formula: desiredDir = normalize(rushTarget - gkPos); gkPos += desiredDir × rushLaunchMps × FRAME_MS / 1000.
        /// No abort check or contact detection here — orchestrator handles those per §3.7.2.
        /// §3.7.2. Goalkeeper Mechanics #11 §3.7.
        /// </summary>
        /// <param name="gkPos">Current GK world-space position (mutated in-place). §3.7.2.</param>
        /// <param name="rushTarget">Locked rush target (locked at commit; never updated mid-rush per KD-15). §3.7.2.</param>
        /// <param name="rushLaunchMps">Rush launch speed (m/s) from ComputeRushLaunchMps. §3.7.2.</param>
        public static void UpdateRushFrame(ref Vector3 gkPos, Vector3 rushTarget, float rushLaunchMps)
        {
            Vector3 delta = rushTarget - gkPos;
            float sqLen   = delta.sqrMagnitude;

            if (sqLen < GoalkeeperConstants.DEGENERACY_EPSILON_SQ)
            {
                // Already at target — no movement
                return;
            }

            Vector3 desiredDir = delta / Mathf.Sqrt(sqLen);
            float   stepM      = rushLaunchMps * GoalkeeperConstants.FrameMs / GoalkeeperConstants.MS_PER_SECOND;

            // Do not overshoot the target
            float distToTarget = Mathf.Sqrt(sqLen);
            if (stepM > distToTarget)
            {
                stepM = distToTarget;
            }

            gkPos += desiredDir * stepM;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
