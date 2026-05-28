// File:     src/shot-mechanics/IShotBallSystem.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §4.2, Code Standards #20
// Purpose:  Shot Mechanics' view of Ball Physics: possession check and ApplyKick() call.
//           ERR-008 Option B: possession is agent-side state, not stored on BallState. §4.2.2.

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Shot Mechanics' interface to Ball Physics. Consumed by ShotExecutor.
    /// Shot Mechanics #6 §4.2.
    /// </summary>
    public interface IShotBallSystem
    {
        /// <summary>
        /// Returns true if the given agent currently has ball possession.
        /// ERR-008 Option B: possession is agent-side state, no PossessingAgentId on BallState.
        /// Shot Mechanics #6 §4.2.2.
        /// </summary>
        bool IsBallPossessedBy(int agentId);

        /// <summary>
        /// Applies a kick velocity and spin to the ball. Exactly one call per shot execution,
        /// at CONTACT state (§4.2.1). Never call with Vector3.zero velocity.
        /// velocity.magnitude must be in [VAbsoluteMin, VAbsoluteMax] before calling.
        /// spin.magnitude must not exceed SpinAbsoluteMax (80.0 rad/s) (XC-4.2-02).
        /// Shot Mechanics #6 §4.2.1.
        /// </summary>
        void ApplyKick(ref BallState ball, Vector3 velocity, Vector3 spin, int agentId, float matchTime);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
