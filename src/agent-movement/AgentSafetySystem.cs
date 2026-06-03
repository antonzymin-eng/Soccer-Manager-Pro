// File:     src/agent-movement/AgentSafetySystem.cs
// Created:  2026-05-22
// Modified: 2026-06-03
// Author:   —
// Spec:     Agent Movement #2 §4.1.3, §4.3.1, Code Standards #20
// Purpose:  Post-integration validation: NaN detection, speed clamp, facing sanity, boundary enforcement.

using UnityEngine;

namespace TacticalDirector.AgentMovement
{
    /// <summary>
    /// Post-integration safety checks executed every physics frame (§4.4.1 Step 10).
    /// All methods are static. Invalid values snap to last valid recorded state.
    /// Agent Movement #2 §4.1.3.
    /// </summary>
    public static class AgentSafetySystem
    {
        /// <summary>
        /// Returns true when position, velocity, or facing contains NaN or Infinity, or when
        /// facing degenerates to a zero vector (downstream Normalize would return NaN). Facing is
        /// included so that corrupted facing produced by upstream bugs is recovered alongside
        /// position/velocity rather than propagating into every later MovementAngleDeg call.
        /// Agent Movement #2 §4.1.3.
        /// </summary>
        public static bool HasInvalidValues(Vector2 position, Vector2 velocity, Vector2 facing)
        {
            if (float.IsNaN(position.x) || float.IsNaN(position.y)
                || float.IsNaN(velocity.x) || float.IsNaN(velocity.y)
                || float.IsNaN(facing.x) || float.IsNaN(facing.y)
                || float.IsInfinity(position.x) || float.IsInfinity(position.y)
                || float.IsInfinity(velocity.x) || float.IsInfinity(velocity.y)
                || float.IsInfinity(facing.x) || float.IsInfinity(facing.y))
            {
                return true;
            }

            return facing.sqrMagnitude < SafetyConstants.VELOCITY_SQR_MAGNITUDE_EPSILON;
        }

        /// <summary>
        /// Clamps speed to MAX_SPEED_CLAMP and returns corrected velocity.
        /// Direction is preserved. Agent Movement #2 §4.3.1.
        /// </summary>
        public static Vector2 ClampVelocity(Vector2 velocity)
        {
            float speed = velocity.magnitude;

            if (speed > MovementThresholds.MAX_SPEED_CLAMP)
            {
                return velocity * (MovementThresholds.MAX_SPEED_CLAMP / speed);
            }

            return velocity;
        }

        /// <summary>
        /// Clamps position to within pitch boundaries plus exterior buffer.
        /// Agents are allowed slightly outside the pitch boundary (goal area, corner flag).
        /// Agent Movement #2 §4.3.1.
        /// </summary>
        public static Vector2 ClampToPitch(Vector2 position)
        {
            float buffer = SafetyConstants.PitchBoundaryBuffer;
            float x = Mathf.Clamp(position.x, -buffer, SafetyConstants.PitchLengthX + buffer);
            float y = Mathf.Clamp(position.y, -buffer, SafetyConstants.PitchWidthY + buffer);
            return new Vector2(x, y);
        }

        /// <summary>
        /// Full validation pass: checks for NaN/Inf or zero facing, clamps speed, clamps to pitch.
        /// If invalid, returns lastValidPosition / lastValidVelocity / lastValidFacing and sets
        /// wasRecovered. lastValidFacing is expected to be normalised by the caller.
        /// Agent Movement #2 §4.1.3.
        /// </summary>
        public static void Validate(
            ref Vector2 position,
            ref Vector2 velocity,
            ref Vector2 facing,
            Vector2 lastValidPosition,
            Vector2 lastValidVelocity,
            Vector2 lastValidFacing,
            out bool wasRecovered)
        {
            if (HasInvalidValues(position, velocity, facing))
            {
                position = lastValidPosition;
                velocity = lastValidVelocity;
                facing = lastValidFacing;
                wasRecovered = true;
                return;
            }

            velocity = ClampVelocity(velocity);
            position = ClampToPitch(position);
            wasRecovered = false;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                         |
// | 1.0     | 2026-05-22 | —      | Initial implementation.                                                                       |
// | 1.1     | 2026-05-25 | —      | H-2: namespace → TacticalDirector.AgentMovement; moved to src/agent-movement/.               |
// |         |            |        | M-2: PitchLengthX/PitchWidthY/buffer moved from inline literals to SafetyConstants catalogue. |
// | 1.2     | 2026-06-03 | —      | AR-4 fix: M-5 HasInvalidValues / Validate now include FacingDirection in NaN/Inf detection    |
// |         |            |        | and zero-vector check; recovery snaps facing to lastValidFacing. Closes the silent NaN-       |
// |         |            |        | propagation path through MovementAngleDeg / RotateFacingToward on corrupted facing input.    |
#endregion
