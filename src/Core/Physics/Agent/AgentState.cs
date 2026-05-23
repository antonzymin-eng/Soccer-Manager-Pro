// File:     src/Core/Physics/Agent/AgentState.cs
// Created:  2026-05-22
// Modified: 2026-05-22
// Author:   —
// Spec:     Agent Movement #2 §3.5.1, Code Standards #20
// Purpose:  Value-type snapshot of all agent kinematic and energy state.

using UnityEngine;

namespace TacticalDirector.Core.Physics.Agent
{
    /// <summary>
    /// Immutable snapshot of a single agent's physical state at one physics frame.
    /// All locomotion systems receive and return this struct by value.
    /// Agent Movement #2 §3.5.1. Target size ≤256B (§4.5.1).
    /// </summary>
    public struct AgentState
    {
        // — Kinematic —
        /// <summary>World XY position (meters). Z = 0 for outfield agents. Ball Physics #1 §1.2.</summary>
        public Vector2 Position;

        /// <summary>World XY velocity (m/s).</summary>
        public Vector2 Velocity;

        /// <summary>Unit vector in XY plane. Z component is always 0 for outfield agents.</summary>
        public Vector2 FacingDirection;

        // — State machine —
        /// <summary>Current locomotion state. Agent Movement #2 §3.1.2.</summary>
        public AgentMovementState CurrentState;

        /// <summary>State from the previous frame.</summary>
        public AgentMovementState PreviousState;

        /// <summary>Seconds spent continuously in CurrentState.</summary>
        public float TimeInState;

        /// <summary>Reason for entering GROUNDED; valid only when CurrentState == GROUNDED.</summary>
        public GroundedReason GroundedReason;

        /// <summary>Normalised collision force [0,1]; valid only when CurrentState == GROUNDED.</summary>
        public float CollisionForce;

        // — Turning —
        /// <summary>Current lean angle output for animation (degrees). §3.4.</summary>
        public float LeanAngle;

        /// <summary>Current achieved turn rate (°/s).</summary>
        public float CurrentTurnRate;

        // — Dual-energy fatigue —
        /// <summary>Aerobic pool [0.0 spent → 1.0 fresh]. §3.1.3. 0=spent, 1=fresh.</summary>
        public float AerobicPool;

        /// <summary>Sprint reservoir [0.0 depleted → 1.0 full]. §3.1.3.</summary>
        public float SprintReservoir;

        // — Safety/recovery —
        /// <summary>Last position known to be valid. Written by AgentSafetySystem after each successful frame.</summary>
        public Vector2 LastValidPosition;

        /// <summary>Last velocity known to be valid.</summary>
        public Vector2 LastValidVelocity;

        /// <summary>Cached magnitude of Velocity (m/s). Recomputed each frame; do not set directly.</summary>
        public float Speed;

        /// <summary>Initialises state for an agent placed at a pitch position, fully rested.</summary>
        public static AgentState CreateAtPosition(Vector2 position, Vector2 facingDirection)
        {
            return new AgentState
            {
                Position = position,
                Velocity = Vector2.zero,
                FacingDirection = facingDirection.normalized,
                CurrentState = AgentMovementState.IDLE,
                PreviousState = AgentMovementState.IDLE,
                TimeInState = 0.0f,
                GroundedReason = GroundedReason.COLLISION,
                CollisionForce = 0.0f,
                LeanAngle = 0.0f,
                CurrentTurnRate = 0.0f,
                AerobicPool = 1.0f,
                SprintReservoir = 1.0f,
                LastValidPosition = position,
                LastValidVelocity = Vector2.zero,
                Speed = 0.0f
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-22 | —      | Initial implementation. |
#endregion
