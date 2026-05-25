// File:     src/agent-movement/MovementCommand.cs
// Created:  2026-05-22
// Modified: 2026-05-25
// Author:   —
// Spec:     Agent Movement #2 §3.5.3, Code Standards #20
// Purpose:  Command struct issued by the AI layer each tactical heartbeat (10 Hz).

using UnityEngine;

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Immutable command struct from the AI/tactical layer to the movement system.
    /// Issued at the 10 Hz heartbeat; physics interpolates between commands at 60 Hz.
    /// Zero-allocation: struct with no reference-type fields on the hot path.
    /// </summary>
    public readonly struct MovementCommand
    {
        /// <summary>World-space destination (XY pitch plane). Agent Movement #2 §3.5.3.</summary>
        public readonly Vector2 TargetPosition;

        /// <summary>Target locomotion state requested by AI. Agent Movement #2 §3.5.3.</summary>
        public readonly AgentMovementState DesiredState;

        /// <summary>Braking profile when decelerating. Agent Movement #2 §3.5.3.</summary>
        public readonly DecelerationMode DecelerationMode;

        /// <summary>How the agent's facing direction is governed this frame. Agent Movement #2 §3.5.3.</summary>
        public readonly FacingMode FacingMode;

        /// <summary>
        /// World-space look-at target when FacingMode is TARGET_LOCK.
        /// Ignored in AUTO_ALIGN mode. Agent Movement #2 §3.5.3.
        /// </summary>
        public readonly Vector2 FacingTarget;

        /// <summary>
        /// When true, post-integration safety boundary clamps are bypassed (editor/replay tooling only).
        /// MUST NOT be set true in production game logic. Agent Movement #2 §3.5.3.
        /// </summary>
        public readonly bool OverrideSafetyConstraints;

        private MovementCommand(
            Vector2 targetPosition,
            AgentMovementState desiredState,
            DecelerationMode decelerationMode,
            FacingMode facingMode,
            Vector2 facingTarget,
            bool overrideSafety)
        {
            TargetPosition = targetPosition;
            DesiredState = desiredState;
            DecelerationMode = decelerationMode;
            FacingMode = facingMode;
            FacingTarget = facingTarget;
            OverrideSafetyConstraints = overrideSafety;
        }

        /// <summary>Move to position at whatever speed the state machine allows.</summary>
        public static MovementCommand MoveTo(Vector2 target)
        {
            return new MovementCommand(
                target,
                AgentMovementState.JOGGING,
                DecelerationMode.CONTROLLED,
                FacingMode.AUTO_ALIGN,
                target,
                false);
        }

        /// <summary>Sprint urgently toward target; AI requests sprint state.</summary>
        public static MovementCommand SprintUrgent(Vector2 target)
        {
            return new MovementCommand(
                target,
                AgentMovementState.SPRINTING,
                DecelerationMode.CONTROLLED,
                FacingMode.AUTO_ALIGN,
                target,
                false);
        }

        /// <summary>Decelerate to a stop at current position using controlled braking.</summary>
        public static MovementCommand Stop(Vector2 currentPosition)
        {
            return new MovementCommand(
                currentPosition,
                AgentMovementState.IDLE,
                DecelerationMode.CONTROLLED,
                FacingMode.AUTO_ALIGN,
                currentPosition,
                false);
        }

        /// <summary>Jockey laterally while keeping facing locked on a target (e.g. a ball carrier).</summary>
        public static MovementCommand StrafeWhileWatching(Vector2 target, Vector2 watchTarget)
        {
            return new MovementCommand(
                target,
                AgentMovementState.JOGGING,
                DecelerationMode.CONTROLLED,
                FacingMode.TARGET_LOCK,
                watchTarget,
                false);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                           |
// | 1.0     | 2026-05-22 | —      | Initial implementation.                                                         |
// | 1.1     | 2026-05-25 | —      | H-2: namespace → TacticalDirector.AgentMovement; moved to src/agent-movement/. |
#endregion
