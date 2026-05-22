// File:     src/Core/Physics/Agent/AgentSafetySystem.cs
// Created:  2026-05-22
// Modified: 2026-05-22
// Author:   —
// Spec:     Agent Movement #2 §4.1.3, §4.3.1, Code Standards #20
// Purpose:  Post-integration validation: NaN detection, speed clamp, boundary enforcement.

using UnityEngine;

namespace TacticalDirector.Core.Physics.Agent
{
    /// <summary>
    /// Post-integration safety checks executed every physics frame (§4.4.1 Step 10).
    /// All methods are static. Invalid values snap to last valid recorded state.
    /// Agent Movement #2 §4.1.3.
    /// </summary>
    public static class AgentSafetySystem
    {
        /// <summary>
        /// Pitch dimensions used for boundary enforcement. Agent Movement #2 §4.3.1.
        /// Width matches Ball Physics #1 §1.2 coordinate system (X = length, Y = width).
        /// </summary>
        private static readonly float PitchLengthX = 105.0f;
        private static readonly float PitchWidthY = 68.0f;

        /// <summary>
        /// Returns true when position or velocity contains NaN or Infinity.
        /// Agent Movement #2 §4.1.3.
        /// </summary>
        public static bool HasInvalidValues(Vector2 position, Vector2 velocity)
        {
            return float.IsNaN(position.x) || float.IsNaN(position.y)
                || float.IsNaN(velocity.x) || float.IsNaN(velocity.y)
                || float.IsInfinity(position.x) || float.IsInfinity(position.y)
                || float.IsInfinity(velocity.x) || float.IsInfinity(velocity.y);
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
        /// Clamps position to within pitch boundaries plus a small exterior buffer.
        /// Agents are allowed slightly outside the pitch boundary (goal area, corner flag).
        /// Agent Movement #2 §4.3.1.
        /// </summary>
        public static Vector2 ClampToPitch(Vector2 position)
        {
            float buffer = 5.0f;
            float x = Mathf.Clamp(position.x, -buffer, PitchLengthX + buffer);
            float y = Mathf.Clamp(position.y, -buffer, PitchWidthY + buffer);
            return new Vector2(x, y);
        }

        /// <summary>
        /// Full validation pass: checks for NaN/Inf, clamps speed, clamps to pitch.
        /// If invalid, returns lastValidPosition / lastValidVelocity and sets wasRecovered.
        /// Agent Movement #2 §4.1.3.
        /// </summary>
        public static void Validate(
            ref Vector2 position,
            ref Vector2 velocity,
            Vector2 lastValidPosition,
            Vector2 lastValidVelocity,
            out bool wasRecovered)
        {
            if (HasInvalidValues(position, velocity))
            {
                position = lastValidPosition;
                velocity = lastValidVelocity;
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
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-22 | —      | Initial implementation. |
#endregion
