// File:     src/collision-system/BallCollisionHandler.cs
// Created:  2026-05-25
// Modified: 2026-06-05  [v1.1]
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
            // TODO: at Stage 0+1, delegate to the agent-contact entry point exposed by
            // Ball Physics #1 §3.1.10.1. The exact method name and signature are chosen
            // by the Ball Physics implementer at integration time — avoid hard-coding a
            // phantom symbol here (CLAUDE.md "Interface Design Principle", ERR-001/004).
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                       |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                              |
// | 1.1     | 2026-06-05 | —      | AR-5 M-1. TODO no longer names a phantom BallCollision.ApplyAgentContact     |
// |         |            |        | symbol (BallCollision only exposes ApplyKick); points at the §3.1.10.1 spec |
// |         |            |        | anchor so the Ball Physics integrator picks the signature.                  |
#endregion
