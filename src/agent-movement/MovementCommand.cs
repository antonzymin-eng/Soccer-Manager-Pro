// File:     src/agent-movement/MovementCommand.cs
// Created:  2026-05-22
// Modified: 2026-06-09 (AR-12 fix pass)
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

        /// <summary>
        /// Walk to position — low-intensity repositioning that stays below the jogging band.
        /// The commandSpeed mapping caps the integration target at JogEnter, so the agent
        /// settles at walking pace without promoting to JOGGING (AR-12 H-2 companion).
        /// </summary>
        public static MovementCommand WalkTo(Vector2 target)
        {
            return new MovementCommand(
                target,
                AgentMovementState.WALKING,
                DecelerationMode.CONTROLLED,
                FacingMode.AUTO_ALIGN,
                target,
                false);
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

        /// <summary>
        /// Sprint toward a target while keeping facing locked on it, with EMERGENCY
        /// braking. Decision Tree #8 §3.5.7 PRESS dispatch profile: a pressing agent
        /// keeps eyes on the ball carrier and accepts stumble risk for a fast stop
        /// when the ball is played away. (WalkTo / AR-13 precedent: factory added
        /// because the DT dispatch layer cannot otherwise express this profile.)
        /// </summary>
        public static MovementCommand PressSprint(Vector2 target)
        {
            return new MovementCommand(
                target,
                AgentMovementState.SPRINTING,
                DecelerationMode.EMERGENCY,
                FacingMode.TARGET_LOCK,
                target,
                false);
        }

        /// <summary>
        /// Sprint toward a point while watching a separate target (e.g. the ball),
        /// with CONTROLLED braking. Decision Tree #8 §3.5.8 INTERCEPT dispatch
        /// profile: controlled stop protects first-touch quality at the intercept
        /// point; TARGET_LOCK keeps the ball in view during the run.
        /// </summary>
        public static MovementCommand SprintWhileWatching(Vector2 target, Vector2 watchTarget)
        {
            return new MovementCommand(
                target,
                AgentMovementState.SPRINTING,
                DecelerationMode.CONTROLLED,
                FacingMode.TARGET_LOCK,
                watchTarget,
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

        /// <summary>
        /// TOOLING-ONLY factory that constructs a command with OverrideSafetyConstraints=true,
        /// for regression tests of the Step 11 override-path cache-write guard
        /// (T-AM-030..032, AR-5 M-1 / AR-7 M-2). MUST NOT be called from production game logic —
        /// disabling safety validation in a match would let a single NaN poison every downstream
        /// frame. `internal` access is granted to TacticalDirector.AgentMovement.Tests via
        /// InternalsVisibleTo in AssemblyInfo.cs.
        /// </summary>
        internal static MovementCommand ToolingOverrideOnly_NaNInjection(Vector2 target)
        {
            return new MovementCommand(
                target,
                AgentMovementState.IDLE,
                DecelerationMode.CONTROLLED,
                FacingMode.AUTO_ALIGN,
                target,
                true);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                           |
// | 1.0     | 2026-05-22 | —      | Initial implementation.                                                         |
// | 1.1     | 2026-05-25 | —      | H-2: namespace → TacticalDirector.AgentMovement; moved to src/agent-movement/. |
// | 1.2     | 2026-06-04 | —      | Test-plan landing: internal ToolingOverrideOnly_NaNInjection factory added for      |
// |         |            |        | T-AM-030..032 (AR-5 M-1 / AR-7 M-2) regression-test seam. Production game logic    |
// |         |            |        | MUST NOT call this factory — it disables Step 10 safety validation.                |
// | 1.3     | 2026-06-09 | —      | AR-12 follow-up: WalkTo factory added — the AI layer previously had no way to       |
// |         |            |        | request the WALKING band (MoveTo=JOGGING, SprintUrgent=SPRINTING, Stop=IDLE).       |
// |         |            |        | Consumed by T-AM-113 walk-band closed-loop regression test.                         |
// | 1.4     | 2026-06-11 | —      | DT #8 audit AR-2 M-5 follow-up (WalkTo precedent): PressSprint                      |
// |         |            |        | (SPRINTING/EMERGENCY/TARGET_LOCK, DT §3.5.7) and SprintWhileWatching                |
// |         |            |        | (SPRINTING/CONTROLLED/TARGET_LOCK, DT §3.5.8) factories — the DT dispatch          |
// |         |            |        | profiles for PRESS and INTERCEPT were not expressible with the existing set.        |
#endregion
