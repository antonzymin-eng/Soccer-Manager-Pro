// File:     src/agent-movement/AgentMovementState.cs
// Created:  2026-05-22
// Modified: 2026-05-25
// Author:   —
// Spec:     Agent Movement #2 §3.1.2, Code Standards #20
// Purpose:  Seven discrete locomotion states used throughout the agent movement system.

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Seven discrete locomotion states. Physics formulas activated per §3.1.6 state-physics table.
    /// Transitions governed by AgentStateMachine; all invalid transitions are rejected.
    /// </summary>
    public enum AgentMovementState
    {
        /// <summary>Speed &lt; IdleEnter (0.1 m/s). No locomotion forces applied.</summary>
        IDLE,

        /// <summary>IdleExit (0.3) ≤ speed &lt; JogEnter (2.2 m/s). Linear accel/decel.</summary>
        WALKING,

        /// <summary>JogExit (1.9) ≤ speed &lt; SprintEnter (5.8 m/s). Exponential accel, aerobic gated.</summary>
        JOGGING,

        /// <summary>speed ≥ SprintExit (5.5 m/s). Exponential accel, sprint reservoir gated.</summary>
        SPRINTING,

        /// <summary>Agent braking toward a lower target speed. Controlled or emergency mode.</summary>
        DECELERATING,

        /// <summary>Loss of balance from sharp turn or heavy collision. Momentum-only; no voluntary control.</summary>
        STUMBLING,

        /// <summary>Agent knocked down. Full recovery required before any locomotion.</summary>
        GROUNDED
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                |
// | 1.0     | 2026-05-22 | —      | Initial implementation (included GroundedReason, FacingMode, DecelerationMode).      |
// | 1.1     | 2026-05-25 | —      | H-2: namespace → TacticalDirector.AgentMovement; moved to src/agent-movement/.       |
// |         |            |        | L-6: GroundedReason.NONE sentinel added (in GroundedReason.cs).                      |
// | 1.2     | 2026-05-25 | —      | Pass-3: GroundedReason / FacingMode / DecelerationMode extracted to own files.        |
#endregion
