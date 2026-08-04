// File:     src/goalkeeper-mechanics/GoalkeeperRushDispatch.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Modified: 2026-08-04 (wiring backlog W1 / ERR-011-010: + §3.7.0 ComputeRushCommitDistanceM — how far from his own goal a keeper comes out, from his OneVsOne / Composure / fatigue. §3.7 had delegated the decision to Decision Tree #8, which has no keeper model and cannot acquire one, so it had no owner and CommitRushIntent sat uncalled. See docs/tracking/gk-rush-trigger-design.md)
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
        /// Computes how far from his own goal this keeper will come out (m) — the §3.7.0 rush-commit
        /// distance (ERR-011-010).
        ///
        /// <para>§3.7's state entry delegated the rush DECISION to Decision Tree #8, which has no
        /// goalkeeper model and structurally cannot acquire one, so the condition had no owner and
        /// <c>CommitRushIntent</c> sat uncalled. #11 takes it back here, exactly as §3.3.6 took back the
        /// dive-commit timing for the same reason.</para>
        ///
        /// <para>Formula: <c>RUSH_COMMIT_BASE_M + RUSH_COMMIT_K_ONE_VS_ONE · OneVsOne_norm
        /// + RUSH_COMMIT_K_COMPOSURE · Composure_norm − RUSH_COMMIT_FATIGUE_PENALTY_M · fatigue</c>,
        /// clamped to <c>[RUSH_COMMIT_MIN_DISTANCE_M, RUSH_COMMIT_MAX_DISTANCE_M]</c>. The keeper is the
        /// only judge of when to leave his goal, so the decision is his attributes and nothing else: an
        /// aggressive, composed sweeper-keeper commits from the edge of the area, a timid or spent one
        /// barely leaves his line.</para>
        ///
        /// <para><c>OneVsOne</c> is consumed here for the commit DECISION. FR-GK-024 constrains the 1v1
        /// SAVE (§3.2 <c>requiredReactionMs</c>, §3.5 <c>attrFactor</c>) to closed-form coefficients with
        /// no alternative path; deciding whether to come out is not a save, and those two formulas are
        /// untouched.</para>
        /// §3.7.0. Goalkeeper Mechanics #11 §3.7.
        /// </summary>
        /// <param name="attrs">GK agent attributes (fatigue 0 = rested, 1 = spent). §3.7.0.</param>
        /// <returns>Distance from the keeper's own goal (m) within which he will commit a rush.</returns>
        public static float ComputeRushCommitDistanceM(GoalkeeperAgentAttributes attrs)
        {
            float distanceM = GoalkeeperConstants.RushCommitBaseM
                            + GoalkeeperConstants.RushCommitKOneVsOne * attrs.OneVsOneNorm
                            + GoalkeeperConstants.RushCommitKComposure * attrs.ComposureNorm
                            - GoalkeeperConstants.RushCommitFatiguePenaltyM * attrs.Fatigue;

            return Mathf.Clamp(
                distanceM,
                GoalkeeperConstants.RushCommitMinDistanceM,
                GoalkeeperConstants.RushCommitMaxDistanceM);
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
// | 1.1     | 2026-08-04 | —      | Wiring backlog W1 (ERR-011-010): + ComputeRushCommitDistanceM,  |
// |         |            |        | #11 §3.7.0. The WHEN of a rush — clamp(base + k1v1·OneVsOne +   |
// |         |            |        | kComposure·Composure − fatiguePenalty·fatigue). §3.7 had        |
// |         |            |        | delegated it to Decision Tree #8, which has no goalkeeper model |
// |         |            |        | and structurally cannot acquire one (ActionType ordinal 8       |
// |         |            |        | overflows the 3-bit composure-noise field), so the condition    |
// |         |            |        | belonged to nobody and every one-on-one was a stationary keeper |
// |         |            |        | on his line. Consumed for the commit DECISION only —           |
// |         |            |        | FR-GK-024's closed-form constraint on the 1v1 SAVE formulas is  |
// |         |            |        | untouched.                                                      |
#endregion
