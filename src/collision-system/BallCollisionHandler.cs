// File:     src/collision-system/BallCollisionHandler.cs
// Created:  2026-05-25
// Modified: 2026-07-27 (shot-outcome pass)
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
        /// Ball Physics #1 §3.1.10.1 computes the deflection (<see
        /// cref="BallCollision.ApplyAgentDeflection"/> — the entry point the former TODO deferred,
        /// ERR-003-007); this method owns the DETECTION-side gates (shot-outcome design KD-6):
        /// a Controlled ball is possession, not a deflection, and a ball below the
        /// <c>AgentDeflection.MinBallSpeedMps</c> gate is left to the first-touch control model
        /// (#4) so pass receptions keep routing through control while shots deflect. The
        /// response-side gates (separating contact, degenerate normal) live in Ball Physics.
        /// </summary>
        public static void OnAgentCollision(ref BallState ball, in AgentBallCollisionData data)
        {
            if (ball.State == BallStateType.Controlled)
            {
                return;
            }

            if (ball.Velocity.sqrMagnitude
                < BallPhysicsConstants.AgentDeflection.MinBallSpeedMps
                  * BallPhysicsConstants.AgentDeflection.MinBallSpeedMps)
            {
                return;
            }

            BallCollision.ApplyAgentDeflection(ref ball, data.AgentPosition, data.BodyPart);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                       |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                              |
// | 1.1     | 2026-06-05 | —      | AR-5 M-1. TODO no longer names a phantom BallCollision.ApplyAgentContact     |
// |         |            |        | symbol (BallCollision only exposes ApplyKick); points at the §3.1.10.1 spec |
// |         |            |        | anchor so the Ball Physics integrator picks the signature.                  |
// | 1.2     | 2026-07-27 | —      | ERR-003-007 (shot-outcome design KD-6): the TODO body is live —             |
// |         |            |        | delegates to BallCollision.ApplyAgentDeflection, with the detection-side    |
// |         |            |        | gates here (Controlled ball = possession; sub-MinBallSpeedMps contact =     |
// |         |            |        | first-touch territory, so reception is untouched).                          |
#endregion
