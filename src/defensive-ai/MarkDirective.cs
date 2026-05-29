// File:     src/defensive-ai/MarkDirective.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §2.2.1, Code Standards #20
// Purpose:  Team-level defensive coordination output for one tick.
//           Written by DefensiveAITick; read by the match orchestrator and by
//           Decision Tree #8 via TacticalContext.MarkDirective? (ERR-014-001).

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Per-team defensive directive produced each 10 Hz tick.
    /// Written by <see cref="DefensiveAITick"/>; consumed read-only by the match
    /// orchestrator and Decision Tree #8 (ERR-014-001). Defensive AI #14 §2.2.1.
    ///
    /// <c>offensiveLineDepth</c> is read-only from Positioning AI #12; #14 never writes it
    /// (FR-DA-012). <c>stepUpTargetDepth</c> is meaningful only when
    /// <c>offsideTrapActive</c> is true.
    /// </summary>
    public struct MarkDirective
    {
        /// <summary>TeamId this directive targets (0 = home, 1 = away).</summary>
        public int TeamId;

        /// <summary>
        /// X-coordinate of the current effective defensive line (m).
        /// Read-only mirror of Positioning AI #12 DefensiveLineDepth; never written by #14 (FR-DA-012).
        /// </summary>
        public float OffensiveLineDepth;

        /// <summary>True when the offside-trap step-up is executing this tick (§3.7).</summary>
        public bool OffsideTrapActive;

        /// <summary>
        /// Target distToOwnGoal for the DEFENSE-line step-up (m).
        /// Semantically valid only when <see cref="OffsideTrapActive"/> is true.
        /// </summary>
        public float StepUpTargetDepth;

        /// <summary>True when the last-man emergency override is active this tick (§3.8).</summary>
        public bool EmergencyFlag;

        /// <summary>Returns an all-ZONAL, no-override directive for the given team.</summary>
        public static MarkDirective Inactive(int teamId)
        {
            return new MarkDirective
            {
                TeamId             = teamId,
                OffensiveLineDepth = 0f,
                OffsideTrapActive  = false,
                StepUpTargetDepth  = 0f,
                EmergencyFlag      = false,
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
