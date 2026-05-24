// File:     src/Core/Physics/Ball/BallStateMachine.cs
// Created:  2026-05-24
// Modified: 2026-05-24
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Pure state-transition logic for the ball state machine.
//           No physics calculations; only reads BallState fields.

using UnityEngine;

namespace TacticalDirector.BallPhysics
{
    /// <summary>
    /// State transition logic for the ball state machine.
    /// Pure state logic — no physics calculations.
    /// </summary>
    public static class BallStateMachine
    {
        /// <summary>
        /// Updates ball state based on current physics conditions.
        /// Uses hysteresis to prevent rapid oscillation at boundaries.
        /// </summary>
        public static BallStateType UpdateBallState(BallState ball)
        {
            switch (ball.State)
            {
                case BallStateType.STATIONARY:
                    // Transitions handled externally by kick/touch events.
                    return BallStateType.STATIONARY;

                case BallStateType.ROLLING:
                    if (ball.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity)
                        return BallStateType.STATIONARY;
                    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
                        return BallStateType.AIRBORNE;
                    if (IsOutOfBounds(ball.Position))
                        return BallStateType.OUT_OF_PLAY;
                    return BallStateType.ROLLING;

                case BallStateType.AIRBORNE:
                    if (ball.Position.z <= BallPhysicsConstants.State.AirborneExitThreshold &&
                        ball.Velocity.z < 0f)
                        return BallStateType.BOUNCING;
                    if (IsOutOfBounds(ball.Position))
                        return BallStateType.OUT_OF_PLAY;
                    return BallStateType.AIRBORNE;

                case BallStateType.BOUNCING:
                    if (Mathf.Abs(ball.Velocity.z) < BallPhysicsConstants.State.BounceVelocityThreshold)
                        return BallStateType.ROLLING;
                    return BallStateType.AIRBORNE;

                case BallStateType.CONTROLLED:
                    // Transitions handled externally by the agent system.
                    return BallStateType.CONTROLLED;

                case BallStateType.OUT_OF_PLAY:
                    // Transitions handled externally by the restart system.
                    return BallStateType.OUT_OF_PLAY;

                default:
                    return BallStateType.STATIONARY;
            }
        }

        /// <summary>
        /// Returns true if the ball has entirely crossed a pitch boundary line.
        /// </summary>
        public static bool IsOutOfBounds(Vector3 position)
        {
            float r = BallPhysicsConstants.Ball.RADIUS;
            return position.x < -r
                || position.x > BallPhysicsConstants.Pitch.LENGTH + r
                || position.y < -r
                || position.y > BallPhysicsConstants.Pitch.WIDTH + r;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Fix pass: namespace → TacticalDirector.BallPhysics; ALL_CAPS       |
// |         |            |        | constant refs → PascalCase; file header per FR-CS-056/057.         |
#endregion
