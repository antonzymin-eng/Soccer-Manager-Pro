// File:     src/pressing-ai/PressingSnapshot.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §4.2, Code Standards #20
// Purpose:  Allocated-once-per-match input container for PressingAITick. The match
//           orchestrator populates this struct before each 10 Hz tick.

using UnityEngine;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Complete tick-input container for the Pressing AI.
    /// Allocated once at match start; re-populated in-place each tick by the orchestrator.
    /// Pressing AI #13 §4.2.
    ///
    /// Design rationale: stand-alone input type (not a direct reference to FilteredView)
    /// because pressing-ai is in the Mechanics layer and must NOT import the AI-layer
    /// perception-system assembly (src/CLAUDE.md assembly layer taxonomy).
    /// </summary>
    public sealed class PressingSnapshot
    {
        /// <summary>Monotonically increasing tick index. Used for F1 stale-detection.</summary>
        public int TickIndex;

        /// <summary>World-space ball position (X, Y, Z). Only X and Y are used at Stage 0.</summary>
        public Vector3 BallPosition;

        /// <summary>World-space ball velocity (m/s). X, Y used for backward-pass direction.</summary>
        public Vector3 BallVelocity;

        /// <summary>
        /// EntityId of the agent currently in possession of the ball; -1 when ball is loose.
        /// Identifies the ball-carrier for trigger evaluation.
        /// </summary>
        public int BallCarrierEntityId;

        /// <summary>
        /// Normalised attacking direction for the pressing team (+X typically = attacking right).
        /// Used by BackwardPass (§3.1.2) and threat-score progression term (§3.4).
        /// </summary>
        public Vector2 AttackingDirection;

        /// <summary>
        /// TeamId of the team currently in possession (or last in possession).
        /// Used to identify which agents are opponents.
        /// </summary>
        public int PossessionTeamId;

        /// <summary>
        /// TeamId of the pressing team (the team executing this press directive).
        /// </summary>
        public int PressingTeamId;

        /// <summary>
        /// All agents (both teams) for this tick. Length = PressingAIConstants.SQUAD_SIZE (22).
        /// Allocated once at match start. The orchestrator updates fields in place.
        /// </summary>
        public readonly PressingAgentSnapshot[] Agents;

        /// <summary>
        /// Allocates agent array with the standard squad capacity.
        /// </summary>
        public PressingSnapshot()
        {
            Agents = new PressingAgentSnapshot[PressingAIConstants.SQUAD_SIZE];
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
