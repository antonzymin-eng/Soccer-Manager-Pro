// File:     src/agent-movement/AgentState.cs
// Created:  2026-05-22
// Modified: 2026-06-09 (AR-12 fix pass)
// Author:   —
// Spec:     Agent Movement #2 §3.5.1, Code Standards #20
// Purpose:  Value-type snapshot of all agent kinematic and energy state.

using UnityEngine;

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Mutable value-type state for a single agent's physical state at one physics frame.
    /// AgentMovementSystem.Update receives and mutates this struct by ref parameter.
    /// Agent Movement #2 §3.5.1. Target size ≤256B (§4.5.1).
    ///
    /// Note: Intentionally a mutable struct (not readonly struct) because the 60 Hz pipeline
    /// mutates individual fields through a ref parameter rather than constructing new instances.
    /// Migration to readonly struct + C# 10 with-expression is deferred pending
    /// certification-platform.md C# version pinning (see src/CLAUDE.md WHAT IS NOT HERE YET).
    /// </summary>
    public struct AgentState
    {
        // — Kinematic —
        /// <summary>World XY position (meters). Z = 0 for outfield agents. Ball Physics #1 §1.2.</summary>
        public Vector2 Position;

        /// <summary>World XY velocity (m/s).</summary>
        public Vector2 Velocity;

        /// <summary>Unit vector in XY plane indicating which way the agent is facing.</summary>
        public Vector2 FacingDirection;

        // — State machine —
        /// <summary>Current locomotion state. Agent Movement #2 §3.1.2.</summary>
        public AgentMovementState CurrentState;

        /// <summary>
        /// Last distinct state before the most recent applied transition. Written only when a
        /// transition is applied (Step 3) — NOT refreshed every frame, so during a long dwell
        /// this holds the state the agent came from, not literally last frame's state.
        /// </summary>
        public AgentMovementState PreviousState;

        /// <summary>Seconds spent continuously in CurrentState.</summary>
        public float TimeInState;

        /// <summary>Reason for entering GROUNDED; valid only when CurrentState == GROUNDED. NONE otherwise.</summary>
        public GroundedReason GroundedReason;

        /// <summary>Normalised collision force [0,1]; valid only when CurrentState == GROUNDED.</summary>
        public float CollisionForce;

        // — Turning —
        /// <summary>Signed lean angle output for animation (degrees, ±MAX_LEAN_ANGLE). §3.4.
        /// Sign follows Unity XY signed-rotation convention (positive = counter-clockwise).</summary>
        public float LeanAngle;

        /// <summary>Actual achieved turn rate this frame (°/s), |signedAngle|/dt. §3.4.</summary>
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

        /// <summary>Last facing direction known to be valid (normalised). Recovered alongside position/velocity on safety failure.</summary>
        public Vector2 LastValidFacing;

        /// <summary>Cached magnitude of Velocity (m/s). Recomputed each frame; do not set directly.</summary>
        public float Speed;

        // — Oscillation guard —
        /// <summary>Anti-thrash guard for state transitions. Always pass by ref; Initialize() called by CreateAtPosition. Agent Movement #2 §3.1.7.</summary>
        public OscillationGuard OscillationGuard;

        /// <summary>Initialises state for an agent placed at a pitch position, fully rested.</summary>
        public static AgentState CreateAtPosition(Vector2 position, Vector2 facingDirection)
        {
            Vector2 normalisedFacing = facingDirection.sqrMagnitude > SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON
                ? facingDirection.normalized
                : Vector2.up;
            var state = new AgentState
            {
                Position = position,
                Velocity = Vector2.zero,
                FacingDirection = normalisedFacing,
                CurrentState = AgentMovementState.IDLE,
                PreviousState = AgentMovementState.IDLE,
                TimeInState = 0.0f,
                GroundedReason = GroundedReason.NONE,
                CollisionForce = 0.0f,
                LeanAngle = 0.0f,
                CurrentTurnRate = 0.0f,
                AerobicPool = 1.0f,
                SprintReservoir = 1.0f,
                LastValidPosition = position,
                LastValidVelocity = Vector2.zero,
                LastValidFacing = normalisedFacing,
                Speed = 0.0f
            };
            // OscillationGuard zero-init causes false-positive lockout at t=0; Initialize() sets
            // all timestamp slots to NegativeInfinity so no recent transitions are counted.
            state.OscillationGuard.Initialize();
            return state;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                              |
// | 1.0     | 2026-05-22 | —      | Initial implementation.                                                            |
// | 1.1     | 2026-05-25 | —      | H-2: namespace → TacticalDirector.AgentMovement; moved to src/agent-movement/.    |
// |         |            |        | L-2: XML doc clarified — mutable struct, mutation via ref, readonly deferral note. |
// |         |            |        | L-6: GroundedReason default changed from COLLISION to NONE (sentinel).             |
// | 1.2     | 2026-05-25 | —      | Pass-4 fix: H-3 OscillationGuard field added; H-4 Initialize() called in            |
// |         |            |        | CreateAtPosition to prevent false-positive lockout at match start.                  |
// | 1.3     | 2026-05-26 | —      | AR-2 fix: M-5 zero-vector guard on facingDirection in CreateAtPosition; falls back   |
// |         |            |        | to Vector2.up to prevent downstream NaN/zero-facing corruption.                     |
// | 1.4     | 2026-06-03 | —      | AR-4 fix: M-5 LastValidFacing field added; CreateAtPosition seeds it from the         |
// |         |            |        | normalised facing argument. Consumed by AgentSafetySystem.Validate on recovery.      |
// |         |            |        | H-4/L-1 docs on LeanAngle / CurrentTurnRate updated for signed/achieved semantics.    |
// | 1.5     | 2026-06-09 | —      | AR-12 fix: L-2 PreviousState doc corrected — field holds the last DISTINCT state      |
// |         |            |        | (written only on applied transitions), not literally last frame's state.              |
#endregion
