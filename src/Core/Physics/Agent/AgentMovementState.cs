// File:     src/Core/Physics/Agent/AgentMovementState.cs
// Created:  2026-05-22
// Modified: 2026-05-22
// Author:   —
// Spec:     Agent Movement #2 §3.1.2, Code Standards #20
// Purpose:  Movement state enums used throughout the agent movement system.

namespace TacticalDirector.Core.Physics.Agent
{
    /// <summary>
    /// Seven discrete locomotion states. Physics formulas activated per §3.1.6 state-physics table.
    /// Transitions governed by AgentStateMachine; all invalid transitions are rejected.
    /// </summary>
    public enum AgentMovementState
    {
        /// <summary>Speed &lt; IDLE_ENTER (0.1 m/s). No locomotion forces applied.</summary>
        IDLE,

        /// <summary>IDLE_EXIT (0.3) ≤ speed &lt; JOG_ENTER (2.2 m/s). Linear accel/decel.</summary>
        WALKING,

        /// <summary>JOG_EXIT (1.9) ≤ speed &lt; SPRINT_ENTER (5.8 m/s). Exponential accel, aerobic gated.</summary>
        JOGGING,

        /// <summary>speed ≥ SPRINT_EXIT (5.5 m/s). Exponential accel, sprint reservoir gated.</summary>
        SPRINTING,

        /// <summary>Agent braking toward a lower target speed. Controlled or emergency mode.</summary>
        DECELERATING,

        /// <summary>Loss of balance from sharp turn or heavy collision. Momentum-only; no voluntary control.</summary>
        STUMBLING,

        /// <summary>Agent knocked down. Full recovery required before any locomotion.</summary>
        GROUNDED
    }

    /// <summary>
    /// Reason for entering GROUNDED state. Governs recovery dwell time calculation (§3.1.5).
    /// </summary>
    public enum GroundedReason
    {
        /// <summary>Involuntary knockdown from Collision System (Spec #3).</summary>
        COLLISION,

        /// <summary>Voluntary sliding tackle (Stage 1+).</summary>
        SLIDING_TACKLE,

        /// <summary>Voluntary diving header (Stage 1+).</summary>
        DIVING_HEADER
    }

    /// <summary>
    /// How the agent's facing direction is updated each frame (§3.3.4).
    /// </summary>
    public enum FacingMode
    {
        /// <summary>Facing auto-aligns to the movement velocity direction.</summary>
        AUTO_ALIGN,

        /// <summary>Facing locks to an explicit target while body moves in commanded direction.</summary>
        TARGET_LOCK
    }

    /// <summary>
    /// Deceleration mode supplied by the AI command layer (§3.5.2).
    /// </summary>
    public enum DecelerationMode
    {
        /// <summary>Controlled stop over 3.0–5.0 m (default).</summary>
        CONTROLLED,

        /// <summary>Emergency stop over 2.5–3.5 m; higher stumble risk.</summary>
        EMERGENCY
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-22 | —      | Initial implementation. |
#endregion
