// File:     src/first-touch/PossessionStateMachine.cs
// Created:  2026-05-25
// Modified: 2026-06-10
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
        /// Classifies the touch outcome from displacement radius, displaced ball position,
        /// velocity, and context. Priority order: INTERCEPTION → DEFLECTION → LOOSE_BALL → CONTROLLED.
        /// First Touch Mechanics #4 §3.4.2.
        /// </summary>
        /// <param name="r">Displacement radius (m).</param>
        /// <param name="newBallPos">Computed new ball world position (m) from BallDisplacementProcessor.</param>
        /// <param name="newBallVel">Computed new ball velocity (m/s) from BallDisplacementProcessor.</param>
        /// <param name="ctx">Per-touch input context (supplies BallVelocity at contact and opponent data).</param>
        internal static (TouchResult outcome, int possessingAgentId, int interceptingAgentId) Determine(
            float r,
            Vector3 newBallPos,
            Vector3 newBallVel,
            in FirstTouchContext ctx)
        {
            // Priority 1 — INTERCEPTION: poor touch within an opponent's reach of the
            // DISPLACED BALL position — §3.4.2 anchors the query at newBallPosition, not
            // the agent (ERR-004-004; the pre-AR-7 agent-anchored check mis-fired by up to
            // r = 2.0 m against the 2.5 m radius). Stage 0 approximation: the single
            // nearest-to-agent opponent from PressureEvaluator is the only candidate; the
            // full SpatialQuery + interceptor ID land with the ERR-004-002 context surface.
            if (r >= FirstTouchConstants.InterceptionThreshold
                && !float.IsPositiveInfinity(ctx.NearestOpponentDistance))
            {
                Vector2 opponentDelta = ctx.NearestOpponentPositionXY
                                      - new Vector2(newBallPos.x, newBallPos.y);
                float interceptionRadiusSq =
                    FirstTouchConstants.InterceptionRadius * FirstTouchConstants.InterceptionRadius;
                if (opponentDelta.sqrMagnitude <= interceptionRadiusSq)
                {
                    // Spec gap (ERR-004-002): FirstTouchContext does not expose the nearest opponent's
                    // agent ID. InterceptingAgentID cannot be resolved here without that data; set to
                    // AGENT_ID_NONE as a placeholder. Tracked in spec-error-log.md.
                    return (TouchResult.Interception, FirstTouchConstants.AGENT_ID_NONE, FirstTouchConstants.AGENT_ID_NONE);
                }
            }

            // Priority 2 — DEFLECTION: sharp touch retaining momentum in original direction.
            // ERR-004-005: through the public pipeline this alignment gate is effectively
            // always satisfied at r ≥ DeflectionThreshold — heavy-touch q is small, so
            // §3.3.5 momentum retention dominates the agent contribution and alignment
            // stays near 1.0. The gate is retained per spec §3.4.2; the low-alignment
            // LOOSE_BALL escape below is reachable only for degenerate (near-zero) inputs.
            if (r >= FirstTouchConstants.DeflectionThreshold)
            {
                float momentumAlignment = ComputeMomentumAlignment(newBallVel, ctx.BallVelocity);
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

            if (newXY.sqrMagnitude < FirstTouchConstants.BLEND_MIN_MAGNITUDE_SQ
                || origXY.sqrMagnitude < FirstTouchConstants.BLEND_MIN_MAGNITUDE_SQ)
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
// | 1.2     | 2026-06-06 | —      | AR-6 L-1: AR-5 L-2 local-cache superseded by FirstTouchConstants.BLEND_MIN_MAGNITUDE_SQ — predicate now reads the catalogue compile-time const directly. |
// | 1.3     | 2026-06-10 | —      | AR-7 M-1 (ERR-004-004): INTERCEPTION proximity re-anchored at the displaced ball position per §3.4.2 SpatialQuery(newBallPosition, INTERCEPTION_RADIUS) — was agent-anchored via NearestOpponentDistance (error up to r = 2.0 m against the 2.5 m radius); validity gated on finite NearestOpponentDistance instead of the PressureRadius-truncated HasNearbyOpponent flag. AR-7 L-1: unused `q` parameter dropped (classification is r-based per §3.4.2); redundant `originalBallVel` parameter dropped (ctx.BallVelocity is the same value). ERR-004-005 vacuous-alignment-gate observation documented at the DEFLECTION branch. |
#endregion
