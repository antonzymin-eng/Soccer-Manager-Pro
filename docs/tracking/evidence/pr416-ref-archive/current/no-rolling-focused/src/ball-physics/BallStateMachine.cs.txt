using UnityEngine;
namespace TacticalDirector.BallPhysics
{
    public static class BallStateMachine
    {
        public static BallStateType UpdateBallState(BallState ball)
        {
            switch (ball.State)
            {
                case BallStateType.Stationary:
                    if (ball.Position.z > BallPhysicsConstants.State.AirborneEnterThreshold)
                        return BallStateType.Airborne;
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
                    if (ball.Position.z <= BallPhysicsConstants.State.AirborneExitThreshold && ball.Velocity.z < 0f)
                        return BallStateType.Bouncing;
                    if (IsOutOfBounds(ball.Position)) return BallStateType.OutOfPlay;
                    return BallStateType.Airborne;
                case BallStateType.Bouncing:
                    if (Mathf.Abs(ball.Velocity.z) < BallPhysicsConstants.State.BounceVelocityThreshold)
                        return BallStateType.Rolling;
                    return BallStateType.Airborne;
                case BallStateType.Controlled: return BallStateType.Controlled;
                case BallStateType.OutOfPlay: return BallStateType.OutOfPlay;
                default: return BallStateType.Stationary;
            }
        }
        public static bool IsOutOfBounds(Vector3 position)
        {
            float r = BallPhysicsConstants.Ball.RADIUS;
            return position.x < -r || position.x > BallPhysicsConstants.Pitch.LENGTH + r || position.y < -r || position.y > BallPhysicsConstants.Pitch.WIDTH + r;
        }
    }
}
