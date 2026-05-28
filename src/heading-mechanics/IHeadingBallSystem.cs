// File:     src/heading-mechanics/IHeadingBallSystem.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §4.2, §4.3, Ball Physics #1 §3.1.11.2, Code Standards #20
// Purpose:  Read-only query and kick-application boundary between Heading Mechanics and Ball Physics.

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Boundary interface to Ball Physics #1 consumed by Heading Mechanics #10.
    /// Provides a fresh BallState snapshot query and the ApplyKick output surface.
    /// Ball Physics #1 §3.1.11.2. Heading Mechanics #10 §4.2 / §4.3.
    /// </summary>
    public interface IHeadingBallSystem
    {
        /// <summary>
        /// Returns a fresh BallState snapshot at the given match time.
        /// Heading Mechanics calls this when the cached snapshot is stale (F-06 / FR-HE-033).
        /// Ball Physics #1 §3.1.11.2.
        /// </summary>
        /// <param name="matchTime">Match time in seconds from kickoff.</param>
        BallState GetBallState(float matchTime);

        /// <summary>
        /// Applies an outgoing velocity and spin to the ball.
        /// Called exactly once on successful header contact per FR-HE-006 / KD-12.
        /// Never called on failed attempts.
        /// Ball Physics #1 §3.1.11.2.
        /// </summary>
        /// <param name="velocity">Outgoing velocity vector (m/s) in corner-origin world space.</param>
        /// <param name="spin">Outgoing angular velocity vector (rad/s) in corner-origin world space.</param>
        /// <param name="agentId">ID of the agent that headed the ball.</param>
        /// <param name="matchTime">Match time in seconds at the contact frame.</param>
        void ApplyKick(Vector3 velocity, Vector3 spin, int agentId, float matchTime);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
