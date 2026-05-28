// File:     src/shot-mechanics/ShotAgentState.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §4.3.2, Code Standards #20
// Purpose:  Shot Mechanics' snapshot of agent physical state consumed from Agent Movement.
//           All fields are read-only from Agent Movement; Shot Mechanics never writes them.

using UnityEngine;

using TacticalDirector.AgentMovement;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Shot Mechanics' snapshot of agent physical state consumed from Agent Movement.
    /// Read once at INITIATING state and held constant for the execution pipeline.
    /// Shot Mechanics #6 §4.3.2.
    /// </summary>
    public struct ShotAgentState
    {
        /// <summary>
        /// World-space position (metres). X = pitch length, Y = pitch width, Z = height.
        /// Used for distance-to-goal (§3.2), pressure query origin (§4.4.1), and goal geometry
        /// resolution (§3.5.4). Agent Movement §3.5.3. CLAUDE.md §Coordinate System.
        /// </summary>
        public Vector3 Position;

        /// <summary>
        /// Agent velocity vector at shot initiation (m/s). Used in BodyMechanicsEvaluator
        /// for run-up angle (§3.7.3) and body lean derivation (§3.3.6). Agent Movement §3.5.3.
        /// </summary>
        public Vector3 Velocity;

        /// <summary>
        /// Facing direction in XY plane (normalised unit vector). Used for body lean penalty
        /// (§3.3.6) and spin direction basis (§3.4.3). Agent Movement §3.5.3.
        /// </summary>
        public Vector2 FacingDirection;

        /// <summary>
        /// Current locomotion state. Shot rejected (ShotOutcome.Invalid) if not in approved
        /// set: Idle, Walking, Jogging, Sprinting, Decelerating. Grounded and Stumbling are
        /// rejected — agent cannot shoot while incapacitated. §4.3.2.
        /// </summary>
        public AgentMovementState CurrentState;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
