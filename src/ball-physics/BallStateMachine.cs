// File:     src/ball-physics/BallStateMachine.cs
// Created:  2026-05-24
// Modified: 2026-06-02
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
                case BallStateType.Stationary:
                    // Transitions handled externally by kick/touch events.
                    return BallStateType.Stationary;

                case BallStateType.Rolling:
                    if (ball.Velocity.magnitude < BallPhysicsConstants.State.MinVelocity)
                        return BallStateType.Stationary;
                    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
                        return BallStateType.Airborne;
                    if (IsOutOfBounds(ball.Position))
                        return BallStateType.OutOfPlay;
                    return BallStateType.Rolling;

                case BallStateType.Airborne:
                    if (ball.Position.z <= BallPhysicsConstants.State.AirborneExitThreshold &&
                        ball.Velocity.z < 0f)
                        return BallStateType.Bouncing;
                    if (IsOutOfBounds(ball.Position))
                        return BallStateType.OutOfPlay;
                    return BallStateType.Airborne;

                case BallStateType.Bouncing:
                    if (Mathf.Abs(ball.Velocity.z) < BallPhysicsConstants.State.BounceVelocityThreshold)
                        return BallStateType.Rolling;
                    return BallStateType.Airborne;

                case BallStateType.Controlled:
                    // Transitions handled externally by the agent system.
                    return BallStateType.Controlled;

                case BallStateType.OutOfPlay:
                    // Transitions handled externally by the restart system.
                    return BallStateType.OutOfPlay;

                default:
                    return BallStateType.Stationary;
            }
        }

        /// <summary>
        /// Returns true if the ball has entirely crossed a pitch boundary line and is
        /// low enough for the Stage 0 restart system to register the exit.
        /// Stage 0 z gate (Position.z &lt; Ball.Diameter, 0.22 m) mirrors
        /// BallCollision.CheckBoundaries so the state machine and the restart classifier
        /// agree on what "out" means — a high-flying ball still over the pitch buffer is
        /// neither transitioned to OutOfPlay here nor classified by CheckBoundaries.
        /// Goal-volume detection at height is a Stage 1+ deliverable.
        /// </summary>
        public static bool IsOutOfBounds(Vector3 position)
        {
            if (position.z >= BallPhysicsConstants.Ball.Diameter)
                return false;

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
// | 1.2     | 2026-06-02 | —      | AR-1 fixes. H-2: file header path corrected to src/ball-physics/.  |
// |         |            |        | H-3: IsOutOfBounds now applies the same z &lt; Ball.Diameter gate as |
// |         |            |        | BallCollision.CheckBoundaries — a high-flying ball is no longer    |
// |         |            |        | silently transitioned to OutOfPlay while CheckBoundaries returns   |
// |         |            |        | (false, None); the two definitions of "out" now agree. M-4:        |
// |         |            |        | BallStateType members renamed to PascalCase.                       |
#endregion
