// File:     src/pass-mechanics/IPassBallSystem.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §4.2, Code Standards #20
// Purpose:  Abstracts Ball Physics possession check and ApplyKick() call.
//           ERR-008 resolved as Option B: possession tracking external to BallState.

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Pass Mechanics' view of Ball Physics. Consumed by PassExecutor.
    /// Pass Mechanics #5 §4.2.
    /// </summary>
    public interface IPassBallSystem
    {
        /// <summary>
        /// Returns true if the given agent currently has ball possession.
        /// Option B (ERR-008): possession tracking external to BallState.
        /// Pass Mechanics #5 §4.2.2, §4.2.3.
        /// </summary>
        bool IsBallPossessedBy(int agentId);

        /// <summary>
        /// Applies a kick velocity and spin to the ball, transferring control from
        /// the agent to ball-flight physics. Exactly one call per pass execution
        /// (at CONTACT state). Never call with Vector3.zero velocity (§4.2.1).
        /// Pass Mechanics #5 §4.2.1.
        /// </summary>
        void ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin, int agentId, float matchTime);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                  |
// | 1.0     | 2026-05-26 | —      | Initial implementation. |
#endregion
