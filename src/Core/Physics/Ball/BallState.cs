using UnityEngine;

namespace TacticalDirector.Core.Physics.Ball
{
    /// <summary>
    /// Ball state machine states. Determines which physics forces are applied.
    /// </summary>
    public enum BallStateType
    {
        STATIONARY,
        ROLLING,
        AIRBORNE,
        BOUNCING,
        CONTROLLED,
        OUT_OF_PLAY
    }

    /// <summary>
    /// Ball physics state. Stored as struct for cache efficiency.
    /// Total: 64 bytes (fits in single cache line).
    /// WARNING: Struct fields default to zero. Use CreateAtPosition() to ensure valid
    /// initialization, especially for LastValidPosition.
    /// </summary>
    public struct BallState
    {
        public Vector3 Position;            // (x, y, z) in meters
        public Vector3 Velocity;            // (vx, vy, vz) in m/s
        public Vector3 AngularVelocity;     // (ωx, ωy, ωz) in rad/s
        public BallStateType State;
        public Vector3 LastValidPosition;
        public Vector3 LastValidVelocity;

        public static BallState CreateAtPosition(Vector3 position)
        {
            return new BallState
            {
                Position = position,
                Velocity = Vector3.zero,
                AngularVelocity = Vector3.zero,
                State = BallStateType.STATIONARY,
                LastValidPosition = position,
                LastValidVelocity = Vector3.zero
            };
        }

        public static BallState CreateForKickoff()
        {
            Vector3 centerSpot = new Vector3(
                BallPhysicsConstants.Pitch.LENGTH / 2f,
                BallPhysicsConstants.Pitch.WIDTH / 2f,
                BallPhysicsConstants.Ball.RADIUS);
            return CreateAtPosition(centerSpot);
        }
    }
}
