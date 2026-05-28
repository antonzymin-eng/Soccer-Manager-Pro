// File:     src/heading-mechanics/HeaderContactState.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §2.2, §3.3, §3.4, §3.6, Code Standards #20
// Purpose:  Per-frame mutable state for a single agent's heading attempt across 60 Hz ticks.

using UnityEngine;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Per-frame internal state for one agent's heading attempt.
    /// Allocated once per active intent; mutated across 60 Hz ticks until contact or failure.
    /// Heading Mechanics #10 §2.2.
    /// </summary>
    public struct HeaderContactState
    {
        /// <summary>
        /// 60 Hz frame index at which the agent first left the ground for this header.
        /// Written exactly once by §4.6 on the first frame where: HeaderIntent is latched AND
        /// agent.movementState ∉ {GROUNDED, STUMBLING} AND currentFrame ≥ intent.AttemptCommittedTick × 6.
        /// -1 means not yet set. §3.3 / v0.2 M-3.
        /// </summary>
        public int JumpStartFrame;

        /// <summary>Re-evaluated every 60 Hz tick: the frame at which the ball is predicted to enter HEAD_CONTACT_VOLUME. §3.2.</summary>
        public int PredictedContactFrame;

        /// <summary>Apex-aligned ideal contact frame against which timingOffsetMs is measured. §3.2 / §3.4.</summary>
        public int IdealContactFrame;

        /// <summary>
        /// Set by §4.6 on the tick where currentFrame == PredictedContactFrame.
        /// Written before §3.4 contact quality computation. §2.2 v0.2 M-4.
        /// </summary>
        public int ActualContactFrame;

        /// <summary>Signed timing offset in ms (positive = late). Computed and stored at the contact frame. §3.4.</summary>
        public float TimingOffsetMs;

        /// <summary>2-D head-local contact point error (m). Computed and stored at the contact frame. §3.4.</summary>
        public Vector2 ContactPointError;

        /// <summary>Continuous contact quality scalar [0, 1]. Computed and stored at the contact frame. §3.4.</summary>
        public float ContactQualityScalar;

        /// <summary>Disturbance factor [0, DUEL_DISTURBANCE_MAX] applied by §3.7 to losers. Zero for uncontested headers.</summary>
        public float DisturbanceFactor;

        /// <summary>
        /// Cached JumpReach (m) computed once per jump phase per FR-HE-031.
        /// Set when JumpStartFrame is first assigned; never recalculated during the same jump.
        /// </summary>
        public float JumpReachM;

        /// <summary>
        /// Facing direction from the previous 60 Hz frame.
        /// Used by HeadingSpinTransfer to compute head angular velocity via finite difference (§3.6 / FR-HE-032).
        /// Initialised to agent.FacingDirection at JumpStartFrame.
        /// </summary>
        public Vector2 PrevFrameFacingDirection;

        /// <summary>Initialises all fields to their sentinel / zero values for a new attempt.</summary>
        public static HeaderContactState CreateNew()
        {
            return new HeaderContactState
            {
                JumpStartFrame       = -1,
                PredictedContactFrame = -1,
                IdealContactFrame    = -1,
                ActualContactFrame   = -1,
                TimingOffsetMs       = 0.0f,
                ContactPointError    = Vector2.zero,
                ContactQualityScalar = 0.0f,
                DisturbanceFactor    = 0.0f,
                JumpReachM           = 0.0f,
                PrevFrameFacingDirection = Vector2.zero
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                            |
// | 1.0     | 2026-05-28 | —      | Initial implementation. Added JumpReachM (FR-HE-031 once-per-jump cache) and     |
// |         |            |        | PrevFrameFacingDirection (§3.6 finite-difference angular velocity, FR-HE-032).   |
#endregion
