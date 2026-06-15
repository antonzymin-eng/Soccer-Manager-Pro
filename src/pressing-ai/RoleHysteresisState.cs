// File:     src/pressing-ai/RoleHysteresisState.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.6, Code Standards #20
// Purpose:  Allocated-once-per-match container for per-agent role hysteresis state.
//           Holds the last committed role, the pending candidate role, and the dwell
//           counter for each agent, keyed by EntityId.

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Per-team mutable container for role-hysteresis state.
    /// Allocated once at match start and reused across all ticks (FR-PR-006 zero-alloc hot path).
    /// Pressing AI #13 §3.6.
    ///
    /// Index correspondence (AR-2 M-2): array index == agent EntityId. Keying on EntityId
    /// (rather than the agent's volatile position in PressingSnapshot.Agents[]) keeps each
    /// agent's dwell state attributed to that agent even when snapshot ordering shifts
    /// (substitution, re-sort). Capacity must therefore exceed the highest EntityId.
    /// </summary>
    public sealed class RoleHysteresisState
    {
        /// <summary>Last committed PressRole for each agent slot. Length = capacity.</summary>
        public readonly PressRole[] LastRole;

        /// <summary>
        /// Pending candidate role being accumulated toward commitment (AR-2 M-3).
        /// Dwell only advances while the candidate equals this pending role; a different
        /// candidate restarts the count, so a flapping candidate cannot commit.
        /// Length = capacity.
        /// </summary>
        public readonly PressRole[] PendingRole;

        /// <summary>Role dwell counter for each agent slot. Counts consecutive ticks the candidate has equalled PendingRole. Length = capacity.</summary>
        public readonly int[] RoleDwell;

        /// <summary>Capacity (number of agent slots). Indexed by EntityId; must exceed the highest EntityId.</summary>
        public readonly int Capacity;

        /// <summary>
        /// Allocates arrays for the given capacity and initialises all roles to HoldShape.
        /// </summary>
        /// <param name="capacity">Number of agent slots; must exceed the highest EntityId.</param>
        public RoleHysteresisState(int capacity)
        {
            Capacity    = capacity;
            LastRole    = new PressRole[capacity];
            PendingRole = new PressRole[capacity];
            RoleDwell   = new int[capacity];

            for (int i = 0; i < capacity; i++)
            {
                LastRole[i]    = PressRole.HoldShape;
                PendingRole[i] = PressRole.HoldShape;
                RoleDwell[i]   = 0;
            }
        }

        /// <summary>Resets all agents to HoldShape with zero dwell counters.</summary>
        public void Reset()
        {
            for (int i = 0; i < Capacity; i++)
            {
                LastRole[i]    = PressRole.HoldShape;
                PendingRole[i] = PressRole.HoldShape;
                RoleDwell[i]   = 0;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-06-15 | —      | AR-2 M-2: doc clarifies index == EntityId. AR-2 M-3: added PendingRole[] so dwell tracks a specific candidate; init/Reset cover it. |
#endregion
