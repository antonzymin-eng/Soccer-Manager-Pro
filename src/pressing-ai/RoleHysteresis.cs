// File:     src/pressing-ai/RoleHysteresis.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.6, Code Standards #20
// Purpose:  Pure static class: applies role-dwell hysteresis to candidate role
//           assignments, preventing rapid oscillation between PressRole values.

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Applies per-agent role-dwell hysteresis per §3.6.
    /// The committed role is held until the candidate role has been stable for
    /// <see cref="PressingAIConstants.RoleDwellTicks"/> consecutive ticks.
    /// Pressing AI #13 §3.6.
    /// </summary>
    public static class RoleHysteresis
    {
        /// <summary>
        /// Returns the committed role for one agent after applying hysteresis.
        /// Mutates the relevant fields in <paramref name="hystState"/> in place.
        /// </summary>
        /// <param name="agentIdx">Index into <see cref="RoleHysteresisState"/> arrays.</param>
        /// <param name="candidateRole">Proposed role for this tick before hysteresis.</param>
        /// <param name="hystState">Persistent hysteresis state; mutated in place.</param>
        /// <returns>Committed role after applying dwell guard.</returns>
        public static PressRole Commit(int agentIdx, PressRole candidateRole, RoleHysteresisState hystState)
        {
            PressRole lastRole  = hystState.LastRole[agentIdx];
            int       roleDwell = hystState.RoleDwell[agentIdx];

            if (candidateRole == lastRole)
            {
                // Candidate matches committed role — reset dwell, hold.
                hystState.RoleDwell[agentIdx] = 0;
                return lastRole;
            }

            // Candidate differs — accumulate dwell.
            roleDwell++;
            hystState.RoleDwell[agentIdx] = roleDwell;

            if (roleDwell >= PressingAIConstants.RoleDwellTicks)
            {
                // Dwell threshold reached — commit new role.
                hystState.LastRole[agentIdx]  = candidateRole;
                hystState.RoleDwell[agentIdx] = 0;
                return candidateRole;
            }

            // Not yet committed — hold current role.
            return lastRole;
        }

        /// <summary>
        /// Forces all agents to HoldShape and resets hysteresis state.
        /// Used by DisengageResolver and InvariantEnforcer when an F5 is required.
        /// </summary>
        /// <param name="hystState">Hysteresis state to reset.</param>
        public static void ForceAllHoldShape(RoleHysteresisState hystState)
        {
            for (int i = 0; i < hystState.Capacity; i++)
            {
                hystState.LastRole[i]  = PressRole.HoldShape;
                hystState.RoleDwell[i] = 0;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
