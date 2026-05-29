// File:     src/decision-tree/MatchContext.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §2.2.5, §3.1.1.1, Code Standards #20
// Purpose:  Authoritative game state delivered to the Decision Tree each heartbeat.
//           Not derived from perception — sourced from authoritative simulation state.
//           BallPosition/BallVelocity/PossessingAgentId added per §3.1.1.1 design
//           decision (possession source authority).

using UnityEngine;

namespace TacticalDirector.DecisionTree
{
    /// <summary>
    /// Authoritative match state. Populated by the simulation orchestrator each 10Hz
    /// heartbeat from ground-truth game state (not from agent perception).
    /// Decision Tree #8 §2.2.5, §3.1.1.1.
    /// </summary>
    public struct MatchContext
    {
        // ── Score and Time ────────────────────────────────────────────────────

        /// <summary>Current home team score.</summary>
        public int HomeScore;

        /// <summary>Current away team score.</summary>
        public int AwayScore;

        /// <summary>Elapsed match time in seconds.</summary>
        public float MatchTimeSeconds;

        // ── Possession ────────────────────────────────────────────────────────

        /// <summary>Which team has possession. §2.2.5.</summary>
        public PossessionState Possession;

        /// <summary>
        /// AgentId [0–21] of the possessing agent, or -1 if ball is loose.
        /// Authoritative source per §3.1.1.1 — copied from BallState.PossessingAgentId each heartbeat.
        /// </summary>
        public int PossessingAgentId;

        // ── Phase ─────────────────────────────────────────────────────────────

        /// <summary>Current match phase. Stage 0: gates action generation to OPEN_PLAY only. §2.2.5.</summary>
        public MatchPhase Phase;

        // ── Ball State ────────────────────────────────────────────────────────

        /// <summary>
        /// Authoritative ball position (XY ground plane, metres). Corner-origin coordinates.
        /// Used to compute BallZone and as HOLD dispatch target (§3.5.5).
        /// </summary>
        public Vector2 BallPosition;

        /// <summary>
        /// Ball velocity 3D (m/s). Used by INTERCEPT feasibility check (§3.1.9).
        /// Z component is vertical component; XY is ground-plane velocity.
        /// </summary>
        public Vector3 BallVelocity;

        // ── Zone ─────────────────────────────────────────────────────────────

        /// <summary>
        /// Longitudinal zone of the ball. Governs utility zone modifiers (§3.2.1.3).
        /// Pre-computed by orchestrator from BallPosition.x via PitchGeometry.ComputeFieldZone().
        /// </summary>
        public FieldZone BallZone;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
