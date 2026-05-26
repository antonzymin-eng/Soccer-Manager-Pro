// File:     src/first-touch/IBallPhysicsSystem.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     First Touch Mechanics #4 §4.5.2, Code Standards #20
// Purpose:  Minimal interface to set ball position and velocity after a touch.

using UnityEngine;

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// Minimal contract for writing ball state from the first-touch system.
    /// First Touch Mechanics #4 §4.5.2.
    /// </summary>
    public interface IBallPhysicsSystem
    {
        /// <summary>
        /// Sets the ball's world position and velocity after a touch is applied.
        /// First Touch Mechanics #4 §4.5.2.
        /// </summary>
        /// <param name="newPosition">New world position of the ball (m).</param>
        /// <param name="newVelocity">New world velocity of the ball (m/s).</param>
        public void SetBallState(Vector3 newPosition, Vector3 newVelocity);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
#endregion
