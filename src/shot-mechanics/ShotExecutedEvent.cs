// File:     src/shot-mechanics/ShotExecutedEvent.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §2.4.3, §4.5.2, Code Standards #20
// Purpose:  Struct event published at CONTACT state completion (Ball.ApplyKick() called).
//           Authoritative definition per §4.5.2. No ShotType field (KD-3).

using UnityEngine;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Published to the event bus at CONTACT state completion, after Ball.ApplyKick() is called.
    /// Consumed by Goalkeeper Mechanics (#11, Stage 1) and Statistics Engine (Stage 1+).
    /// No ShotType field — physical vectors carry all information needed. §2.4.3, §4.5.2, KD-3.
    /// </summary>
    public struct ShotExecutedEvent
    {
        /// <summary>Agent who took the shot. Identifies shooter for GK target tracking.</summary>
        public int ShootingAgentId;

        /// <summary>Team of shooting agent. Provided for event filtering by subscribers.</summary>
        public int TeamId;

        /// <summary>
        /// Final kick velocity (world space, m/s). GK Mechanics derives shot speed and trajectory
        /// from this alone; no separate speed field needed. §4.5.2.
        /// </summary>
        public Vector3 KickVelocity;

        /// <summary>Final spin vector (world space, rad/s). GK uses this to anticipate trajectory deviation.</summary>
        public Vector3 KickSpin;

        /// <summary>
        /// Intended goal-relative placement target (before error). (u, v) ∈ [0.0, 1.0]².
        /// Used for xG analytics and GK anticipation. Distinct from actual trajectory.
        /// </summary>
        public Vector2 IntendedTarget;

        /// <summary>Actual kick direction (unit vector) after error application. §3.6.</summary>
        public Vector3 FinalDirection;

        /// <summary>
        /// Body mechanics quality [0.0, 1.0]. GK difficulty estimation: low BMS → irregular trajectory.
        /// §3.7.
        /// </summary>
        public float BodyMechanicsScore;

        /// <summary>Power intent from ShotRequest [0.0, 1.0]. Preserved for xG model (Stage 1+).</summary>
        public float PowerIntent;

        /// <summary>Contact zone used. Preserved for trajectory class inference.</summary>
        public ContactZone ContactZone;

        /// <summary>Distance to goal (metres) at shot time.</summary>
        public float DistanceToGoal;

        /// <summary>Match time in seconds at Ball.ApplyKick() call. Replay and statistics.</summary>
        public float MatchTime;

        /// <summary>Frame number at Ball.ApplyKick(). Replay determinism.</summary>
        public int ContactFrame;

        /// <summary>
        /// True if shooter stumbled. Agent Movement subscribes and transitions to STUMBLING.
        /// GK: irregular trajectory possible. §4.3.3 Mechanism C.
        /// </summary>
        public bool StumbleTriggered;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
