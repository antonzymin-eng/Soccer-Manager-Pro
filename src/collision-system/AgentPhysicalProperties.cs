// File:     src/collision-system/AgentPhysicalProperties.cs
// Created:  2026-05-25
// Modified: 2026-05-25  [v1.1]
// Author:   —
// Spec:     Agent Movement #2 §3.5.4, Collision System #3 §2.3.2, Code Standards #20
// Purpose:  Read-only snapshot of an agent's physical state for one collision frame.
//           Provisional location: will migrate to src/agent-movement/ once that assembly
//           exposes it (Agent Movement #2 §3.5.4 defines the contract).

using UnityEngine;

using TacticalDirector.AgentMovement;

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Per-agent physical snapshot consumed by the collision pipeline each frame.
    /// Built by the match simulator from AgentState + PlayerAttributes before calling
    /// CollisionSystem.UpdateCollisions(). Collision System #3 §2.3.2.
    /// </summary>
    public struct AgentPhysicalProperties
    {
        /// <summary>World position (m). Z = 0 for ground-based agents. Ball Physics #1 §1.2.</summary>
        public Vector3 Position;

        /// <summary>World velocity (m/s). Z = 0 at Stage 0.</summary>
        public Vector3 Velocity;

        /// <summary>
        /// Collision hitbox radius (m).
        /// Formula: 0.3525 + (Strength - 1) × 0.0075. Agent Movement #2 §3.5.4.3.
        /// Range: 0.3525m (Strength 1) – 0.50m (Strength 20).
        /// </summary>
        public float HitboxRadius;

        /// <summary>
        /// Body mass (kg).
        /// Formula: 72.5 + (Strength - 1) × 1.5. Agent Movement #2 §3.5.4.2.
        /// Range: 72.5kg (Strength 1) – 100.0kg (Strength 20).
        /// </summary>
        public float Mass;

        /// <summary>Strength attribute [1–20]. Collision System #3 §3.3.1 fall threshold.</summary>
        public int Strength;

        /// <summary>Agility attribute [1–20]. Grounded duration calculation. §3.3.1.</summary>
        public int Agility;

        /// <summary>True when agent is in GROUNDED state; treated as static obstacle. FR-02.</summary>
        public bool IsGrounded;

        /// <summary>
        /// Builds a snapshot from existing AgentState + PlayerAttributes.
        /// Converts Vector2 position/velocity to Vector3 (Z = 0).
        /// </summary>
        public static AgentPhysicalProperties From(in AgentState state, in PlayerAttributes attrs)
        {
            int strength = attrs.Strength;
            return new AgentPhysicalProperties
            {
                Position = new Vector3(state.Position.x, state.Position.y, 0f),
                Velocity = new Vector3(state.Velocity.x, state.Velocity.y, 0f),
                HitboxRadius = AgentPhysicsSnapshotConstants.HitboxRadiusBase
                             + (strength - 1) * AgentPhysicsSnapshotConstants.HitboxRadiusPerStrength,
                Mass = AgentPhysicsSnapshotConstants.MassBase
                     + (strength - 1) * AgentPhysicsSnapshotConstants.MassPerStrength,
                Strength = strength,
                Agility = attrs.Agility,
                IsGrounded = state.CurrentState == AgentMovementState.GROUNDED
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                        |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                               |
// | 1.1     | 2026-05-25 | —      | H-3: magic literals replaced with AgentPhysicsSnapshotConstants (FR-CS-016). |
#endregion
