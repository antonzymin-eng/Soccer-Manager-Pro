// File:     src/first-touch/IAgentMovementSystem.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     First Touch Mechanics #4 §4.5.3, Code Standards #20
// Purpose:  Minimal interface to set an agent's dribbling state after a touch.

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// Minimal contract for writing agent dribbling state from the first-touch system.
    /// First Touch Mechanics #4 §4.5.3.
    /// </summary>
    public interface IAgentMovementSystem
    {
        /// <summary>
        /// Sets whether the specified agent is in the dribbling locomotion state.
        /// First Touch Mechanics #4 §4.5.3.
        /// </summary>
        /// <param name="agentID">ID of the agent to update.</param>
        /// <param name="isDribbling">True to enter dribbling state; false to exit.</param>
        public void SetDribblingState(int agentID, bool isDribbling);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
#endregion
