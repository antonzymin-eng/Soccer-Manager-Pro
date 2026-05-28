// File:     src/goalkeeper-mechanics/IGoalkeeperBallSystem.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §4.2 / §4.3, Ball Physics #1 §3.1.11.2, Code Standards #20
// Purpose:  Goalkeeper Mechanics' read/write boundary to Ball Physics #1.
//           Written because both sides (GoalkeeperMechanics producer, BallPhysics consumer) are specified.

using UnityEngine;

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Ball physics interface boundary for Goalkeeper Mechanics.
    /// Both sides are specified (GoalkeeperMechanics #11, Ball Physics #1), so the interface is valid
    /// per CLAUDE.md interface-design principle (FR-CS-048/049).
    /// Goalkeeper Mechanics #11 §4.2 / §4.3.
    /// </summary>
    public interface IGoalkeeperBallSystem
    {
        /// <summary>
        /// Applies a kick velocity and spin to the ball.
        /// Ball Physics #1 §3.1.11.2.
        /// </summary>
        void ApplyKick(Vector3 velocity, Vector3 spin, int agentId, float matchTimeMs);

        /// <summary>
        /// Sets the ball possessor agent ID (catch path).
        /// Ball Physics #1 §3.1 possession surface — OI-006 verification posture per §4.3.
        /// </summary>
        void SetPossessor(int agentId);

        /// <summary>
        /// Returns the current ball possessor agent ID, or -1 if the ball is loose.
        /// Ball Physics #1 §3.1.
        /// </summary>
        int GetBallPossessorId();
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
