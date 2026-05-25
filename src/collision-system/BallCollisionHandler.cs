// File:     src/collision-system/BallCollisionHandler.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     Collision System #3 §3.4.3, FR-03, Code Standards #20
// Purpose:  Stub routing agent-ball contact to Ball Physics deflection logic.
//           Implementation body lives in Ball Physics #1 §3.1.10.1.

using TacticalDirector.BallPhysics;

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Routes agent-ball collision data to Ball Physics for deflection calculation.
    /// Collision System owns detection; Ball Physics owns the response. §3.4.3.
    /// </summary>
    public static class BallCollisionHandler
    {
        /// <summary>
        /// Invoked when an agent hitbox overlaps the ball.
        /// Ball Physics #1 §3.1.10.1 computes the deflection; this is the call site only.
        /// </summary>
        public static void OnAgentCollision(ref BallState ball, in AgentBallCollisionData data)
        {
            // TODO: delegate to BallPhysics.BallCollision.ApplyAgentContact(ref ball, data)
            // once the cross-assembly integration is wired in Stage 0+1.
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
#endregion
