// File:     src/first-touch/OrientationDetector.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     First Touch Mechanics #4 §3.6, Code Standards #20
// Purpose:  Detects whether an agent is half-turn oriented relative to the incoming ball direction.

using UnityEngine;

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// Determines whether the receiving agent is half-turn oriented relative to ball travel.
    /// First Touch Mechanics #4 §3.6.
    /// </summary>
    internal static class OrientationDetector
    {
        /// <summary>
        /// Returns true when the angle between the agent's facing direction and the
        /// incoming ball's XY velocity falls within the half-turn window [HalfTurnAngleMin, HalfTurnAngleMax].
        /// First Touch Mechanics #4 §3.6.
        /// </summary>
        /// <param name="agentFacing">Normalised XY facing direction of the agent.</param>
        /// <param name="ballVelocityXY">XY component of the ball's velocity (m/s); magnitude may be non-unit.</param>
        internal static bool IsHalfTurnOriented(Vector2 agentFacing, Vector2 ballVelocityXY)
        {
            if (ballVelocityXY.sqrMagnitude < FirstTouchConstants.BlendMinMagnitude * FirstTouchConstants.BlendMinMagnitude)
            {
                return false;
            }

            // ApproachDir is where the ball came FROM (§3.6.2): negate velocity to get approach.
            Vector2 approachDir = new Vector2(-ballVelocityXY.x, -ballVelocityXY.y).normalized;

            // Dot product gives cos(angle); clamp for numerical safety before Acos.
            float dot = Mathf.Clamp(Vector2.Dot(agentFacing, approachDir), -1.0f, 1.0f);
            float angleDeg = Mathf.Acos(dot) * Mathf.Rad2Deg;

            return angleDeg >= FirstTouchConstants.HalfTurnAngleMin
                && angleDeg <= FirstTouchConstants.HalfTurnAngleMax;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                    |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                           |
// | 1.1     | 2026-05-26 | —      | H-8 fix: ball velocity negated to obtain approach direction per §3.6.2. |
#endregion
