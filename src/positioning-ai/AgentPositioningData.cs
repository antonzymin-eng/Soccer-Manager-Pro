// File: src/positioning-ai/AgentPositioningData.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI §4.3
// Purpose: Per-agent input consumed from the perception snapshot each positioning tick.

using UnityEngine;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Immutable per-agent snapshot read by PositioningAITick each 10 Hz tick.
    /// Slot index into the output Span&lt;Vector2&gt; is determined by the agent's position
    /// in the EntityId-ascending enumeration order (0 = lowest EntityId in the squad).
    /// </summary>
    public readonly struct AgentPositioningData
    {
        /// <summary>Stable agent identifier; used for deterministic sort order. Positive, never reused per #2 XC-002-001.</summary>
        public readonly int EntityId;

        /// <summary>Index into the outSlots span and HysteresisState.Agents array. Always [0, squadSize).</summary>
        public readonly int SlotIndex;

        /// <summary>Current world-space 2-D position (X = longitudinal, Y = lateral). Pitch corner-origin per #1 §1.2.</summary>
        public readonly Vector2 Position;

        /// <summary>False for substituted or red-carded agents; these receive SENTINEL_NO_SLOT. FR-PA-036.</summary>
        public readonly bool IsActive;

        /// <summary>Determines row in the 13×4 pull-factor table.</summary>
        public readonly RoleId Role;

        /// <summary>True only for the goalkeeper slot (index 0). GK slot computed via dedicated formula §3.3.3.</summary>
        public readonly bool IsGoalkeeper;

        public AgentPositioningData(
            int entityId, int slotIndex, Vector2 position,
            bool isActive, RoleId role, bool isGoalkeeper)
        {
            EntityId    = entityId;
            SlotIndex   = slotIndex;
            Position    = position;
            IsActive    = isActive;
            Role        = role;
            IsGoalkeeper = isGoalkeeper;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
