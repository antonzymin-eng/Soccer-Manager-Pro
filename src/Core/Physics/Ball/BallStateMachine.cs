using UnityEngine;

namespace TacticalDirector.Core.Physics.Ball
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
                    // Transitions handled externally by kick/touch events
                    return BallStateType.STATIONARY;

                case BallStateType.ROLLING:
                    if (ball.Velocity.magnitude < BallPhysicsConstants.State.MIN_VELOCITY)
                        return BallStateType.STATIONARY;
                    if (ball.Position.z > BallPhysicsConstants.State.AIRBORNE_ENTER_THRESHOLD)
                        return BallStateType.AIRBORNE;
                    if (IsOutOfBounds(ball.Position))
                        return BallStateType.OUT_OF_PLAY;
                    return BallStateType.ROLLING;

                case BallStateType.AIRBORNE:
                    if (ball.Position.z <= BallPhysicsConstants.State.AIRBORNE_EXIT_THRESHOLD &&
                        ball.Velocity.z < 0)
                        return BallStateType.BOUNCING;
                    if (IsOutOfBounds(ball.Position))
                        return BallStateType.OUT_OF_PLAY;
                    return BallStateType.AIRBORNE;

                case BallStateType.BOUNCING:
                    if (Mathf.Abs(ball.Velocity.z) < BallPhysicsConstants.State.BOUNCE_VELOCITY_THRESHOLD)
                        return BallStateType.ROLLING;
                    return BallStateType.AIRBORNE;

                case BallStateType.CONTROLLED:
                    // Transitions handled externally by agent system
                    return BallStateType.CONTROLLED;

                case BallStateType.OUT_OF_PLAY:
                    // Transitions handled externally by restart system
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
