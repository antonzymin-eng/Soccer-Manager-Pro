// File:     src/shot-mechanics/IShotAgentQuery.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §4.3, Code Standards #20
// Purpose:  Shot Mechanics' read-only interface to Agent Movement attributes and state.
//           Consumed by ShotExecutor at INITIATING state. §4.3.

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Shot Mechanics' read-only interface to Agent Movement data.
    /// Consumed by ShotExecutor at INITIATING state. Shot Mechanics #6 §4.3.
    /// </summary>
    public interface IShotAgentQuery
    {
        /// <summary>
        /// Returns the shot-relevant attributes for the given agent.
        /// Shot Mechanics #6 §4.3.1.
        /// </summary>
        ShotAgentAttributes GetAttributes(int agentId);

        /// <summary>
        /// Returns the current position, velocity, facing direction, and movement state
        /// for the given agent. Shot Mechanics #6 §4.3.2.
        /// </summary>
        ShotAgentState GetState(int agentId);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
