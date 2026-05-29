// File:     src/defensive-ai/MarkHysteresisState.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §2.2.4, §3.11, Code Standards #20
// Purpose:  Per-agent hysteresis state for mark-assignment transitions. Maintained across
//           ticks; contributes to the per-tick determinism digest (FR-DA-004 / #16 §6.2).

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Per-agent mutable state for mark-assignment hysteresis (§3.11).
    /// Tracks how long an agent has held its current assignment and gates transitions
    /// per the Agent Movement #2 §3.1 dwell-time pattern (KD-11).
    ///
    /// Contributes to the per-tick determinism digest (FR-DA-004 / #16 §6.2 / KD-10).
    /// Defensive AI #14 §2.2.4.
    /// </summary>
    public struct MarkHysteresisState
    {
        /// <summary>
        /// Ticks remaining before re-evaluation is permitted for the current non-ZONAL
        /// assignment. Decrements each tick when &gt; 0; transition is blocked until zero.
        /// Set to MarkDwellTicks on mode commitment; reset to 0 by emergency overrides.
        /// </summary>
        public int DwellCounter;

        /// <summary>
        /// The candidate mark mode being evaluated for a potential transition.
        /// Reset when a new competing candidate appears.
        /// </summary>
        public MarkMode CandidateMode;

        /// <summary>
        /// EntityId of the candidate opponent (-1 if candidate is ZONAL).
        /// Reset when CandidateMode changes.
        /// </summary>
        public int CandidateTargetEntityId;

        /// <summary>
        /// Consecutive ticks the current candidate has been consistently preferred.
        /// Transitions commit when HoldTicks &gt;= MarkDwellTicks.
        /// Reset to 0 on candidate change or committed transition.
        /// </summary>
        public int HoldTicks;

        /// <summary>Returns a zeroed hysteresis state with ZONAL candidate defaults.</summary>
        public static MarkHysteresisState Default()
        {
            return new MarkHysteresisState
            {
                DwellCounter          = 0,
                CandidateMode         = MarkMode.Zonal,
                CandidateTargetEntityId = -1,
                HoldTicks             = 0,
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
