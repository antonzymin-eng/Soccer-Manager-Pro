// File:     src/pass-mechanics/IPassAgentQuery.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §4.3, Code Standards #20
// Purpose:  Pass Mechanics' read-only view of Agent Movement attributes and state.
//           ERR-007: KickPower, WeakFootRating, Crossing absent from PlayerAttributes;
//           proxy values used until ERR-007 is resolved (§4.3.1).

using UnityEngine;

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Pass Mechanics' snapshot of the attributes consumed from Agent Movement.
    /// Pass Mechanics #5 §4.3.1.
    /// [ERR-007-PENDING] KickPower, WeakFootRating, Crossing are proxied until
    /// Agent Movement PlayerAttributes gains those fields.
    /// </summary>
    public struct PassAgentAttributes
    {
        /// <summary>Primary accuracy attribute [1–20]. Agent Movement §3.5.6.</summary>
        public float Passing;

        /// <summary>Secondary accuracy attribute; Vision proxy at Stage 0 [1–20]. Agent Movement §3.5.6.</summary>
        public float Technique;

        /// <summary>
        /// Primary velocity attribute [1–20]. Agent Movement §3.5.6.
        /// [TEMPORARY-PROXY-ERR-007] Computed as (Passing + Technique) * 0.5f until ERR-007 resolved.
        /// </summary>
        public float KickPower;

        /// <summary>
        /// Weak-foot quality rating [1–5]. Agent Movement §3.5.6.
        /// [TEMPORARY-PROXY-ERR-007] Defaults to 3 (mid-scale) until ERR-007 resolved.
        /// </summary>
        public int WeakFootRating;

        /// <summary>
        /// Cross accuracy attribute [1–20]. Agent Movement §3.5.6.
        /// [TEMPORARY-PROXY-ERR-007] Mirrors Passing until ERR-007 resolved.
        /// </summary>
        public float Crossing;

        /// <summary>Fatigue scalar [0, 1]. 0 = fully rested, 1 = fully fatigued. Agent Movement §3.5.6.</summary>
        public float Fatigue;
    }

    /// <summary>
    /// Pass Mechanics' snapshot of the agent state consumed from Agent Movement.
    /// Pass Mechanics #5 §4.3.2.
    /// </summary>
    public struct PassAgentState
    {
        /// <summary>
        /// World XY position of the agent (metres). Z component is height (not used in Stage 0).
        /// Agent Movement §3.5.3. Coordinate system: X=pitch length, Y=pitch width (CLAUDE.md §Coordinate System).
        /// </summary>
        public Vector2 Position;

        /// <summary>Agent velocity in XY plane (m/s). Agent Movement §3.5.3.</summary>
        public Vector2 Velocity;

        /// <summary>Facing direction in XY plane (normalised unit vector). Agent Movement §3.5.3.</summary>
        public Vector2 FacingDirection;
    }

    /// <summary>
    /// Pass Mechanics' read-only interface to Agent Movement data.
    /// Consumed by PassExecutor at INITIATING state. Pass Mechanics #5 §4.3.
    /// </summary>
    public interface IPassAgentQuery
    {
        /// <summary>
        /// Returns the pass-relevant attributes for the given agent.
        /// Pass Mechanics #5 §4.3.1.
        /// </summary>
        PassAgentAttributes GetAttributes(int agentId);

        /// <summary>
        /// Returns the current position, velocity, and facing direction for the given agent.
        /// Pass Mechanics #5 §4.3.2.
        /// </summary>
        PassAgentState GetState(int agentId);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                  |
// | 1.0     | 2026-05-26 | —      | Initial implementation. |
#endregion
