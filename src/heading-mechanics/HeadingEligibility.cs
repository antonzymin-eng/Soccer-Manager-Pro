// File:     src/heading-mechanics/HeadingEligibility.cs
// Created:  2026-05-28
// Modified: 2026-06-14
// Author:   —
// Spec:     Heading Mechanics #10 §3.2, KD-3, KD-17, KD-18, FR-HE-001, FR-HE-018, Code Standards #20
// Purpose:  Pure eligibility predicate. No event side effects; the §4.6 caller emits events.

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Pure eligibility predicate for a header attempt (§3.2).
    /// No event side effects — the §4.6 orchestrator inspects MistimedDirection and emits events (v0.2 M-2).
    /// Re-evaluated every 60 Hz tick until contact or attempt-window close (KD-17 / FR-HE-018).
    /// Heading Mechanics #10 §3.2.
    /// </summary>
    public static class HeadingEligibility
    {
        /// <summary>
        /// Evaluates whether the agent is eligible to head the ball on the current physics frame.
        /// Pure function: reads agent/ball/intent state; writes nothing; emits no events.
        /// Heading Mechanics #10 §3.2.
        /// </summary>
        /// <param name="agentState">Current agent kinematic state (AM #2 §3.5.1).</param>
        /// <param name="agentId">Agent identifier (unused in this predicate; reserved for §4.6 caller).</param>
        /// <param name="agentHeadZ">Agent head centre Z coordinate (m) from HeadingJumpKinematics (§3.3).</param>
        /// <param name="ball">Current BallState snapshot (Ball Physics #1).</param>
        /// <param name="ballSnapshotFrame">60 Hz frame at which ball was last queried; used for staleness check (FR-HE-033).</param>
        /// <param name="intent">HeaderIntent locked at commit (KD-17).</param>
        /// <param name="contactState">Per-attempt contact state; JumpStartFrame and IdealContactFrame are read.</param>
        /// <param name="currentFrame">Current 60 Hz frame index.</param>
        /// <param name="currentMatchTime">Current match time (s) for ball re-query on staleness.</param>
        /// <param name="ballSystem">Ball system interface for re-querying stale snapshots (FR-HE-033).</param>
        /// <param name="freshBall">Output: the ball snapshot used for this evaluation (may be re-queried).</param>
        public static EligibilityResult Evaluate(
            AgentState agentState,
            int agentId,
            float agentHeadZ,
            BallState ball,
            int ballSnapshotFrame,
            HeaderIntent intent,
            in HeaderContactState contactState,
            int currentFrame,
            float currentMatchTime,
            IHeadingBallSystem ballSystem,
            out BallState freshBall)
        {
            freshBall = ball;

            // (1) Aerial-phase check (KD-18). Agent must have left the ground.
            if (agentState.CurrentState == AgentMovementState.GROUNDED ||
                agentState.CurrentState == AgentMovementState.STUMBLING)
            {
                return new EligibilityResult
                {
                    IsEligible           = false,
                    PredictedContactFrame = -1,
                    IdealContactFrame    = contactState.IdealContactFrame,
                    MistimedDirection    = MistimedDirection.None
                };
            }

            // (2) BallState freshness (F-06 / FR-HE-033).
            if (currentFrame - ballSnapshotFrame > 1)
            {
                freshBall = ballSystem.GetBallState(currentMatchTime);
            }

            // (3) Predict ball trajectory and find candidate contact frame.
            int apexFrame        = HeadingJumpKinematics.ComputeApexFrame(contactState.JumpStartFrame);
            int idealContactFrame = apexFrame;

            Vector3 headCentre = new Vector3(
                agentState.Position.x,
                agentState.Position.y,
                agentHeadZ);

            int predictedContactFrame = FindContactFrame(
                freshBall,
                headCentre,
                currentFrame,
                apexFrame);

            if (predictedContactFrame < 0)
            {
                return new EligibilityResult
                {
                    IsEligible            = false,
                    PredictedContactFrame  = -1,
                    IdealContactFrame     = idealContactFrame,
                    MistimedDirection     = MistimedDirection.None
                };
            }

            // (4) Body-part check (KD-3): heading system is only invoked for Head contacts.
            //     Geometric confirmation: ball must be within the vertical envelope of the head zone.
            float ballZAtContact = PredictBallZ(freshBall, predictedContactFrame, currentFrame);
            if (Mathf.Abs(ballZAtContact - agentHeadZ) > HeadingMechanicsConstants.HeadContactVolumeHeightM)
            {
                return new EligibilityResult
                {
                    IsEligible            = false,
                    PredictedContactFrame  = predictedContactFrame,
                    IdealContactFrame     = idealContactFrame,
                    MistimedDirection     = MistimedDirection.None
                };
            }

            // (5) Intent-staleness check (KD-17 / FR-HE-018).
            int framesEarly = HeadingMechanicsConstants.FramesEarlyTolerance;
            int framesLate  = HeadingMechanicsConstants.FramesLateTolerance;

            if (predictedContactFrame < idealContactFrame - framesEarly)
            {
                return new EligibilityResult
                {
                    IsEligible            = false,
                    PredictedContactFrame  = predictedContactFrame,
                    IdealContactFrame     = idealContactFrame,
                    MistimedDirection     = MistimedDirection.Early
                };
            }

            if (predictedContactFrame > idealContactFrame + framesLate)
            {
                return new EligibilityResult
                {
                    IsEligible            = false,
                    PredictedContactFrame  = predictedContactFrame,
                    IdealContactFrame     = idealContactFrame,
                    MistimedDirection     = MistimedDirection.Late
                };
            }

            return new EligibilityResult
            {
                IsEligible            = true,
                PredictedContactFrame  = predictedContactFrame,
                IdealContactFrame     = idealContactFrame,
                MistimedDirection     = MistimedDirection.None
            };
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        /// <summary>
        /// Searches the attempt window for the first frame at which the ball enters HEAD_CONTACT_VOLUME.
        /// Uses parabolic ball trajectory (gravity only; spin/drag omitted for Stage 0 eligibility).
        /// Returns -1 if the ball never enters the volume within the search window.
        /// </summary>
        private static int FindContactFrame(
            BallState ball,
            Vector3 headCentre,
            int currentFrame,
            int apexFrame)
        {
            float frameS = HeadingMechanicsConstants.FrameS;
            float radius = HeadingMechanicsConstants.HeadContactVolumeRadiusM;
            float radiusSq = radius * radius;

            // Search forward from currentFrame to apexFrame + FramesLateTolerance.
            int windowStart = currentFrame;
            int windowEnd   = apexFrame + HeadingMechanicsConstants.FramesLateTolerance;

            for (int f = windowStart; f <= windowEnd; f++)
            {
                float dt = (f - currentFrame) * frameS;
                Vector3 ballPos = PredictBallPosition(ball, dt);
                float dx = ballPos.x - headCentre.x;
                float dy = ballPos.y - headCentre.y;
                float dz = ballPos.z - headCentre.z;
                if (dx * dx + dy * dy + dz * dz <= radiusSq)
                {
                    return f;
                }
            }

            return -1;
        }

        /// <summary>Predicts ball XY position at time dt (s) using gravity-only parabolic model.</summary>
        private static Vector3 PredictBallPosition(BallState ball, float dt)
        {
            return new Vector3(
                ball.Position.x + ball.Velocity.x * dt,
                ball.Position.y + ball.Velocity.y * dt,
                ball.Position.z + ball.Velocity.z * dt
                    - HeadingMechanicsConstants.KINEMATIC_HALF_COEFF * HeadingMechanicsConstants.GravityMps2 * dt * dt);
        }

        /// <summary>Predicts ball Z at a specific frame offset for the head-height check in step 4.</summary>
        private static float PredictBallZ(BallState ball, int targetFrame, int currentFrame)
        {
            float dt = (targetFrame - currentFrame) * HeadingMechanicsConstants.FrameS;
            return ball.Position.z + ball.Velocity.z * dt
                   - HeadingMechanicsConstants.KINEMATIC_HALF_COEFF * HeadingMechanicsConstants.GravityMps2 * dt * dt;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                                         |
// | 1.1     | 2026-05-28 | —      | AR-2 H-3: corrected misleading comment in FindContactFrame (now describes        |
// |         |            |        | forward-only window correctly). M-2: FrameMs/1000.0f → FrameS (×2).            |
// |         |            |        | M-6: 0.5f*GravityMps2 → KINEMATIC_HALF_COEFF*GravityMps2 (×2).                 |
// | 1.2     | 2026-06-14 | —      | AR-3 L-3: PredictBallZ still carried a raw 0.5f literal the AR-2 M-6 sweep      |
// |         |            |        | missed (claimed ×2; only PredictBallPosition was converted) → KINEMATIC_HALF_COEFF.|
#endregion
