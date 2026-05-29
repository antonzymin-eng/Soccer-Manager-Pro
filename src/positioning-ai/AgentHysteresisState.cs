// File: src/positioning-ai/AgentHysteresisState.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI §3.3, §3.4, §3.8, FR-PA-038
// Purpose: Per-agent dwell counters and committed line/lane membership; part of the determinism digest.

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Per-agent persistent hysteresis state authoritative across ticks (FR-PA-038).
    /// Included verbatim in the per-tick determinism digest per #16 §6.2.
    /// </summary>
    public struct AgentHysteresisState
    {
        // ── Line membership ──────────────────────────────────────────────────────

        /// <summary>Committed line assignment; consumed by Stage-1+ GetLine() accessor.</summary>
        public LineId CurrentLine;

        /// <summary>Candidate line pending the dwell requirement.</summary>
        public LineId CandidateLine;

        /// <summary>Consecutive ticks the candidate line has been observed. Commits at LINE_DWELL_TICKS.</summary>
        public int LineDwellCount;

        // ── Lane membership ──────────────────────────────────────────────────────

        /// <summary>Committed lane assignment; consumed by Stage-1+ GetLane() accessor.</summary>
        public LaneId CurrentLane;

        /// <summary>Candidate lane observed outside the LANE_HYSTERESIS_M dead zone.</summary>
        public LaneId CandidateLane;

        /// <summary>Consecutive ticks the candidate lane has been observed outside the dead zone.</summary>
        public int LaneDwellCount;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
