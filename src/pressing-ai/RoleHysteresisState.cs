// File:     src/pressing-ai/RoleHysteresisState.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.6, Code Standards #20
// Purpose:  Allocated-once-per-match container for per-agent role hysteresis state.
//           Holds the last committed role and dwell counter for each of the 22 agents.

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Per-team mutable container for role-hysteresis state.
    /// Allocated once at match start and reused across all ticks (FR-PR-006 zero-alloc hot path).
    /// Pressing AI #13 §3.6.
    ///
    /// Index correspondence: array index == agent's position in PressingSnapshot.Agents[].
    /// The caller maps EntityId → index via PressingSnapshot agent ordering.
    /// </summary>
    public sealed class RoleHysteresisState
    {
        /// <summary>Last committed PressRole for each agent slot. Length = capacity.</summary>
        public readonly PressRole[] LastRole;

        /// <summary>Role dwell counter for each agent slot. Incremented when candidate != lastRole. Length = capacity.</summary>
        public readonly int[] RoleDwell;

        /// <summary>Capacity (number of agent slots). Set to PressingAIConstants.SQUAD_SIZE at construction.</summary>
        public readonly int Capacity;

        /// <summary>
        /// Allocates arrays for the given capacity and initialises all roles to HoldShape.
        /// </summary>
        /// <param name="capacity">Number of agent slots; typically PressingAIConstants.SQUAD_SIZE.</param>
        public RoleHysteresisState(int capacity)
        {
            Capacity  = capacity;
            LastRole  = new PressRole[capacity];
            RoleDwell = new int[capacity];

            for (int i = 0; i < capacity; i++)
            {
                LastRole[i]  = PressRole.HoldShape;
                RoleDwell[i] = 0;
            }
        }

        /// <summary>Resets all agents to HoldShape with zero dwell counters.</summary>
        public void Reset()
        {
            for (int i = 0; i < Capacity; i++)
            {
                LastRole[i]  = PressRole.HoldShape;
                RoleDwell[i] = 0;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
