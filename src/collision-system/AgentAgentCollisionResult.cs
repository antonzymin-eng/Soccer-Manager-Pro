// File:     src/collision-system/AgentAgentCollisionResult.cs
// Created:  2026-05-25
// Modified: 2026-06-10  [v1.2]
// Author:   —
// Spec:     Collision System #3 §3.3.3, Code Standards #20
// Purpose:  Internal result struct from CollisionResponse; contains both agents' outcomes.

using UnityEngine;

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Velocity impulses, position corrections, and state triggers for both agents in a
    /// single agent-agent collision. Collision System #3 §3.3.3.
    /// Consumed immediately after CollisionResponse.CalculateAgentAgentResponse().
    /// </summary>
    public struct AgentAgentCollisionResult
    {
        // ── Agent 1 ──────────────────────────────────────────────────────────

        /// <summary>Velocity impulse for agent 1 (m/s). Added to current velocity.</summary>
        public Vector3 VelocityImpulse1;

        /// <summary>Position correction for agent 1 (m). Mass-weighted MTV share.</summary>
        public Vector3 PositionCorrection1;

        /// <summary>True if agent 1 should enter GROUNDED state.</summary>
        public bool TriggerGrounded1;

        /// <summary>True if agent 1 should enter STUMBLING state.</summary>
        public bool TriggerStumble1;

        // ── Agent 2 ──────────────────────────────────────────────────────────

        /// <summary>Velocity impulse for agent 2 (m/s).</summary>
        public Vector3 VelocityImpulse2;

        /// <summary>Position correction for agent 2 (m).</summary>
        public Vector3 PositionCorrection2;

        /// <summary>True if agent 2 should enter GROUNDED state.</summary>
        public bool TriggerGrounded2;

        /// <summary>True if agent 2 should enter STUMBLING state.</summary>
        public bool TriggerStumble2;

        // ── Shared ───────────────────────────────────────────────────────────

        /// <summary>Impact force (N) = impulse / ContactDurationS (ERR-003-001). Used for foul data and fall thresholds.</summary>
        public float ImpactForce;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                   |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                          |
// | 1.1     | 2026-05-25 | —      | Removed GroundedDuration1/2 (duration now computed by AgentStateMachine, not Collision). |
// | 1.2     | 2026-06-10 | —      | AR-7 H-1 follow-through: ImpactForce doc force-conversion formula updated (ERR-003-001). |
#endregion
