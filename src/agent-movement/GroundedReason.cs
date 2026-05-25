// File:     src/agent-movement/GroundedReason.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     Agent Movement #2 §3.1.2, §3.1.5, Code Standards #20
// Purpose:  Reason for entering GROUNDED state; governs recovery dwell time calculation.

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Reason for entering GROUNDED state. Governs recovery dwell time calculation (§3.1.5).
    /// NONE is the zero-value sentinel; valid only when CurrentState != GROUNDED.
    /// </summary>
    public enum GroundedReason
    {
        /// <summary>Not grounded; valid only when CurrentState != GROUNDED.</summary>
        NONE = 0,

        /// <summary>Involuntary knockdown from Collision System (Spec #3).</summary>
        COLLISION,

        /// <summary>Voluntary sliding tackle (Stage 1+).</summary>
        SLIDING_TACKLE,

        /// <summary>Voluntary diving header (Stage 1+).</summary>
        DIVING_HEADER
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                              |
// | 1.0     | 2026-05-25 | —      | Extracted from AgentMovementState.cs (one-public-type-per-file, FR-CS rule).       |
#endregion
