// File:     src/agent-movement/GroundedReason.cs
// Created:  2026-05-25
// Modified: 2026-06-09 (AR-12 fix pass)
// Author:   —
// Spec:     Agent Movement #2 §3.1.2, §3.1.5, Code Standards #20
// Purpose:  Reason for entering GROUNDED state; governs recovery dwell time calculation.

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Reason for entering GROUNDED state. Governs recovery dwell time calculation (§3.1.5).
    /// NONE is the zero-value sentinel; valid only when CurrentState != GROUNDED.
    ///
    /// STAGE 0 PRODUCER NOTE (AR-12 L-1): COLLISION is the only reason any Stage 0 code path
    /// assigns (AgentMovementSystem Step 3 collision entry). SLIDING_TACKLE and DIVING_HEADER
    /// have no producer until the voluntary-action dispatchers land (Spec #3 contact types /
    /// Spec #14 tackle intent, Stage 1+); their dwell multipliers in
    /// AgentStateMachine.CalculateGroundedDwell are dormant by design, not dead code.
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
// | 1.1     | 2026-06-09 | —      | AR-12 fix: L-1 Stage 0 producer note added — only COLLISION has a live producer;   |
// |         |            |        | SLIDING_TACKLE / DIVING_HEADER are Stage 1+ dormant, wired by Spec #3 / #14.       |
#endregion
