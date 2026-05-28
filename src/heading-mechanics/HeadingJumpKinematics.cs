// File:     src/heading-mechanics/HeadingJumpKinematics.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §3.3, KD-4, KD-18, FR-HE-004, FR-HE-019, FR-HE-021, FR-HE-031, Code Standards #20
// Purpose:  JumpReach formula (FM-010-001) and Stage 0 synthetic parabolic Z trajectory.

using UnityEngine;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// JumpReach derivation and Stage 0 synthetic vertical-axis (Z) jump trajectory.
    /// AM #2 §3.6 defers Z>0 kinematics; #10 owns the synthetic trajectory at Stage 0 (KD-18 / FR-HE-019).
    /// JumpReach is computed once per jump phase (FR-HE-031), not per physics tick.
    /// Heading Mechanics #10 §3.3.
    /// </summary>
    public static class HeadingJumpKinematics
    {
        /// <summary>
        /// Computes JumpReach (m) — the apex head altitude above the ground for one jump phase.
        /// FM-010-001: JumpReach = JUMP_REACH_BASE_M + K_STRENGTH·Strength_norm + K_BALANCE·Balance_norm + K_HEADING·Heading_norm.
        /// Computed once per jump phase per FR-HE-031.
        /// Attributes normalised to [0, 1] using ATTR_MAX = 20 (matching Agent Movement #2 [1–20] range).
        /// Heading Mechanics #10 §3.3 / KD-4 / FR-HE-021.
        /// </summary>
        /// <param name="attrs">Agent attributes snapshot (Heading, Strength, Balance in [1, 20]).</param>
        public static float ComputeJumpReach(HeadingAgentAttributes attrs)
        {
            float headingNorm  = Clamp01Norm(attrs.Heading);
            float strengthNorm = Clamp01Norm(attrs.Strength);
            float balanceNorm  = Clamp01Norm(attrs.Balance);

            return HeadingMechanicsConstants.JUMP_REACH_BASE_M
                 + HeadingMechanicsConstants.JumpReachKStrength * strengthNorm
                 + HeadingMechanicsConstants.JumpReachKBalance  * balanceNorm
                 + HeadingMechanicsConstants.JumpReachKHeading  * headingNorm;
        }

        /// <summary>
        /// Returns the 60 Hz apex frame index for the jump that started at jumpStartFrame.
        /// apexFrame = jumpStartFrame + round(JUMP_PHASE_DURATION_MS * JUMP_APEX_FRACTION / FRAME_MS).
        /// Heading Mechanics #10 §3.3.
        /// </summary>
        /// <param name="jumpStartFrame">Frame at which the agent left the ground. Must be ≥ 0.</param>
        public static int ComputeApexFrame(int jumpStartFrame)
        {
            float apexOffsetFrames = HeadingMechanicsConstants.JumpPhaseDurationMs
                                   * HeadingMechanicsConstants.JumpApexFraction
                                   / HeadingMechanicsConstants.FrameMs;
            return jumpStartFrame + Mathf.RoundToInt(apexOffsetFrames);
        }

        /// <summary>
        /// Returns the 60 Hz frame at which the agent lands (end of aerial phase).
        /// landingFrame = jumpStartFrame + round(JUMP_PHASE_DURATION_MS / FRAME_MS).
        /// Heading Mechanics #10 §3.3.
        /// </summary>
        /// <param name="jumpStartFrame">Frame at which the agent left the ground. Must be ≥ 0.</param>
        public static int ComputeLandingFrame(int jumpStartFrame)
        {
            float totalFrames = HeadingMechanicsConstants.JumpPhaseDurationMs
                              / HeadingMechanicsConstants.FrameMs;
            return jumpStartFrame + Mathf.RoundToInt(totalFrames);
        }

        /// <summary>
        /// Returns the agent head Z coordinate (m) at the given physics frame during the aerial phase.
        /// Parabolic interpolation: agentHeadZ = jumpReachM × 4u(1−u), where u = frameOffset / totalPhaseFrames.
        /// Standing height baseline absorbed into jumpReachM (§3.3).
        /// Returns 0 when currentFrame is outside the aerial window.
        /// Heading Mechanics #10 §3.3 / KD-18 / FR-HE-019.
        /// </summary>
        /// <param name="jumpStartFrame">Frame at which the agent left the ground.</param>
        /// <param name="jumpReachM">Pre-computed JumpReach for this jump phase (from ComputeJumpReach).</param>
        /// <param name="currentFrame">Current 60 Hz physics frame.</param>
        public static float ComputeHeadZ(int jumpStartFrame, float jumpReachM, int currentFrame)
        {
            int totalPhaseFrames = Mathf.RoundToInt(
                HeadingMechanicsConstants.JumpPhaseDurationMs / HeadingMechanicsConstants.FrameMs);

            int offset = currentFrame - jumpStartFrame;
            if (offset < 0 || offset > totalPhaseFrames)
            {
                return 0.0f;
            }

            float u = (float)offset / totalPhaseFrames;
            return jumpReachM * 4.0f * u * (1.0f - u);
        }

        // ── Private helpers ──────────────────────────────────────────────────────────

        private static float Clamp01Norm(int attribute)
        {
            float clamped = Mathf.Clamp(attribute,
                HeadingMechanicsConstants.ATTR_MIN,
                HeadingMechanicsConstants.ATTR_MAX);
            return clamped / HeadingMechanicsConstants.ATTR_MAX;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
