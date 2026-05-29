// File: src/positioning-ai/FormationSlotRecord.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI Appendix B
// Purpose: Immutable per-slot row from a formation archetype table.

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// One row of a formation archetype table: the positional intent for a single squad slot.
    /// All three archetype tables in PositioningAIConstants use this struct as their element type.
    /// </summary>
    public readonly struct FormationSlotRecord
    {
        /// <summary>Longitudinal fraction [0,1]; multiply by PITCH_LENGTH_M to get anchor.x.</summary>
        public readonly float LongPct;

        /// <summary>Lateral fraction [0,1]; multiply by PITCH_WIDTH_M to get anchor.y.</summary>
        public readonly float LateralPct;

        /// <summary>Role used to index the 13×4 pull-factor table.</summary>
        public readonly RoleId Role;

        /// <summary>Starting line assignment; used to seed hysteresis state at match init.</summary>
        public readonly LineId DefaultLine;

        /// <summary>Starting lane assignment; used to seed hysteresis state at match init.</summary>
        public readonly LaneId DefaultLane;

        /// <summary>True for slot 0 in every family (the goalkeeper).</summary>
        public readonly bool IsGoalkeeper;

        public FormationSlotRecord(
            float longPct, float lateralPct,
            RoleId role, LineId defaultLine, LaneId defaultLane,
            bool isGoalkeeper = false)
        {
            LongPct      = longPct;
            LateralPct   = lateralPct;
            Role         = role;
            DefaultLine  = defaultLine;
            DefaultLane  = defaultLane;
            IsGoalkeeper = isGoalkeeper;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
