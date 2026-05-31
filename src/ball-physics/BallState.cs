// File:     src/Core/Physics/Ball/BallState.cs
// Created:  2026-05-24
// Modified: 2026-05-31
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Ball state struct and state-machine enum. Passed by ref throughout
//           the physics pipeline; never allocated on the heap per frame.

using System.Runtime.InteropServices;

using UnityEngine;

namespace TacticalDirector.BallPhysics
{
    /// <summary>
    /// Ball state machine states. Determines which physics forces are applied each frame.
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
    /// Total: 64 bytes (fits in a single cache line).
    /// WARNING: Struct fields default to zero. Use CreateAtPosition() to ensure valid
    /// initialisation, especially for LastValidPosition.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct BallState
    {
        public Vector3 Position;            // (x, y, z) in metres
        public Vector3 Velocity;            // (vx, vy, vz) in m/s
        public Vector3 AngularVelocity;     // (ωx, ωy, ωz) in rad/s
        public BallStateType State;
        public Vector3 LastValidPosition;
        public Vector3 LastValidVelocity;

        /// <summary>Creates a stationary ball at the given position with all validation fields initialised.</summary>
        public static BallState CreateAtPosition(Vector3 position)
        {
            return new BallState
            {
                Position          = position,
                Velocity          = Vector3.zero,
                AngularVelocity   = Vector3.zero,
                State             = BallStateType.STATIONARY,
                LastValidPosition = position,
                LastValidVelocity = Vector3.zero
            };
        }

        /// <summary>Creates a ball at the centre spot for kickoff.</summary>
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

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Fix pass: namespace → TacticalDirector.BallPhysics; file header    |
// |         |            |        | added per FR-CS-056/057.                                           |
// | 1.2     | 2026-05-31 | —      | AR-4 L: added [StructLayout(LayoutKind.Sequential)] — BallState    |
// |         |            |        | is embedded via MemoryMarshal.Write inside HeaderExecutedEvent and  |
// |         |            |        | SaveAttemptedEvent (both explicitly Sequential). Explicit attribute  |
// |         |            |        | pins the CLR layout contract; default-Sequential is correct today   |
// |         |            |        | but the annotation is required when the struct is blittable-treated.|
#endregion
