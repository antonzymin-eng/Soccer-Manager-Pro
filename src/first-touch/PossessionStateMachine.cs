// File:     src/first-touch/PossessionStateMachine.cs
// Created:  2026-05-25
// Modified: 2026-06-06
// Author:   —
// Spec:     First Touch Mechanics #4 §3.4.2, Code Standards #20
// Purpose:  Priority-ordered state machine that classifies a touch into INTERCEPTION, DEFLECTION, LOOSE_BALL, or CONTROLLED.

using UnityEngine;

namespace TacticalDirector.FirstTouch
{
    /// <summary>
    /// Determines the possession outcome of a first-touch attempt using priority-ordered
    /// threshold checks. First Touch Mechanics #4 §3.4.2.
    /// </summary>
    internal static class PossessionStateMachine
    {
        /// <summary>
        /// Classifies the touch outcome from displacement radius, velocity, and context.
        /// Priority order: INTERCEPTION → DEFLECTION → LOOSE_BALL → CONTROLLED.
        /// First Touch Mechanics #4 §3.4.2.
        /// </summary>
        /// <param name="q">Control quality scalar [0,1].</param>
        /// <param name="r">Displacement radius (m).</param>
        /// <param name="newBallVel">Computed new ball velocity (m/s) from BallDisplacementProcessor.</param>
        /// <param name="originalBallVel">Ball velocity at the moment of touch (m/s).</param>
        /// <param name="ctx">Per-touch input context.</param>
        internal static (TouchResult outcome, int possessingAgentId, int interceptingAgentId) Determine(
            float q,
            float r,
            Vector3 newBallVel,
            Vector3 originalBallVel,
            in FirstTouchContext ctx)
        {
            // Priority 1 — INTERCEPTION: poor touch within an opponent's reach.
            if (r >= FirstTouchConstants.InterceptionThreshold
                && ctx.HasNearbyOpponent
                && ctx.NearestOpponentDistance <= FirstTouchConstants.InterceptionRadius)
            {
                // Spec gap (ERR-004-002): FirstTouchContext does not expose the nearest opponent's
                // agent ID. InterceptingAgentID cannot be resolved here without that data; set to
                // AGENT_ID_NONE as a placeholder. Tracked in spec-error-log.md.
                return (TouchResult.Interception, FirstTouchConstants.AGENT_ID_NONE, FirstTouchConstants.AGENT_ID_NONE);
            }

            // Priority 2 — DEFLECTION: sharp touch retaining momentum in original direction.
            if (r >= FirstTouchConstants.DeflectionThreshold)
            {
                float momentumAlignment = ComputeMomentumAlignment(newBallVel, originalBallVel);
                if (momentumAlignment >= FirstTouchConstants.DeflectionAlignmentMin)
                {
                    return (TouchResult.Deflection, FirstTouchConstants.AGENT_ID_NONE, FirstTouchConstants.AGENT_ID_NONE);
                }
            }

            // Priority 3 — LOOSE_BALL: ball displaced beyond controlled range.
            if (r >= FirstTouchConstants.LooseBallThreshold)
            {
                return (TouchResult.LooseBall, FirstTouchConstants.AGENT_ID_NONE, FirstTouchConstants.AGENT_ID_NONE);
            }

            // Priority 4 — CONTROLLED: default outcome.
            return (TouchResult.Controlled, ctx.AgentID, FirstTouchConstants.AGENT_ID_NONE);
        }

        /// <summary>
        /// Returns the dot product of the XY-normalised new and original ball velocity vectors.
        /// Returns 0 when either vector is near-zero. First Touch Mechanics #4 §3.4.2.
        /// </summary>
        private static float ComputeMomentumAlignment(Vector3 newVel, Vector3 originalVel)
        {
            Vector2 newXY = new Vector2(newVel.x, newVel.y);
            Vector2 origXY = new Vector2(originalVel.x, originalVel.y);

            float blendThreshSq = FirstTouchConstants.BLEND_MIN_MAGNITUDE * FirstTouchConstants.BLEND_MIN_MAGNITUDE;
            if (newXY.sqrMagnitude < blendThreshSq || origXY.sqrMagnitude < blendThreshSq)
            {
                return 0.0f;
            }

            return Vector2.Dot(newXY.normalized, origXY.normalized);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                                                                                                |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                                                                                                       |
// | 1.1     | 2026-06-06 | —      | AR-5 M-1: TouchResult enum members renamed PascalCase (Controlled/LooseBall/Deflection/Interception). L-2: BlendMinMagnitude² cached in local. L-4: TODO replaced with ERR-004-002 anchor. M-2 follow-on: BlendMinMagnitude → BLEND_MIN_MAGNITUDE. |
#endregion
