// File:     src/perception-system/BallPerceptionEvaluator.cs
// Created:  2026-05-28
// Modified: 2026-09-17
// Author:   —
// Spec:     Perception System #7 §3.5, Code Standards #20
// Purpose:  Evaluates ball visibility: range check, FoV test, occlusion test (§3.5.1).
//           Updates BallVisible and BallPerceivedPosition in the snapshot.
//           Tracks BallStalenessFrames (§3.5.2). Ball is exempt from L_rec (OQ-2).
//           Static class. All methods are deterministic. No side effects.

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Evaluates ball visibility and manages ball staleness tracking.
    /// Ball is treated as a special entity: no L_rec; zero displacement has no FoV bearing
    /// (ERR-007-004); ordinary FoV and occlusion rules otherwise apply (OQ-2).
    /// Perception System #7 §3.5.
    /// </summary>
    public static class BallPerceptionEvaluator
    {
        /// <summary>
        /// Evaluates whether the ball is visible to the observer this heartbeat.
        /// Applies range, FoV, and occlusion tests. Ball is never subject to L_rec (OQ-2).
        /// Updates BallVisible, BallPerceivedPosition, and BallStalenessFrames.
        /// Perception System #7 §3.5.1–§3.5.2.
        /// </summary>
        /// <param name="observerPos">Observer's 2D world position.</param>
        /// <param name="facingDir">Observer's normalised 2D facing direction.</param>
        /// <param name="observerTeamId">Observer's team ID for occlusion filter.</param>
        /// <param name="effectiveFoVHalfAngleDeg">Effective FoV half-angle (degrees).</param>
        /// <param name="ballState">Current BallState from Ball Physics #1.</param>
        /// <param name="agentStates">Full agent state array indexed 0–21.</param>
        /// <param name="agentAttrs">Full agent attributes array indexed 0–21.</param>
        /// <param name="candidateIds">Nearby entity IDs from spatial hash query (range: MAX_PERCEPTION_RANGE).</param>
        /// <param name="candidateCount">Number of valid entries in candidateIds.</param>
        /// <param name="prevPerceivedPosition">Last confirmed ball position from the previous heartbeat.</param>
        /// <param name="prevStalenessFrames">Staleness counter from the previous heartbeat.</param>
        /// <param name="ballVisible">Output: true if ball is visible this tick.</param>
        /// <param name="perceivedPosition">Output: current perceived ball position (stale if invisible).</param>
        /// <param name="stalenessFrames">Output: updated staleness counter.</param>
        public static void Evaluate(
            Vector2 observerPos,
            Vector2 facingDir,
            int observerTeamId,
            float effectiveFoVHalfAngleDeg,
            BallState ballState,
            AgentState[] agentStates,
            PerceptionAgentAttributes[] agentAttrs,
            int[] candidateIds,
            int candidateCount,
            Vector2 prevPerceivedPosition,
            int prevStalenessFrames,
            out bool ballVisible,
            out Vector2 perceivedPosition,
            out int stalenessFrames)
        {
            // 2D projection: use XY components of the 3D BallState.Position (§4.3.1)
            Vector2 ballPos2D = new Vector2(ballState.Position.x, ballState.Position.y);

            // Range check
            bool inRange = (ballPos2D - observerPos).sqrMagnitude
                <= PerceptionConstants.MaxPerceptionRange * PerceptionConstants.MaxPerceptionRange;

            // ERR-007-004: A co-located ball has no meaningful bearing. In particular, a Controlled ball is
            // attached to its holder at the same XY coordinate; feeding that zero vector through
            // atan2 would invent a world-East bearing and can make the holder "lose sight" of the
            // ball solely because of facing direction.
            bool isCoLocated = (ballPos2D - observerPos).sqrMagnitude == 0.0f;

            // FoV test
            bool inFoV = inRange && (isCoLocated
                || FovCalculator.IsInFoV(
                    observerPos, facingDir, ballPos2D, effectiveFoVHalfAngleDeg));

            // Occlusion test (ball uses entity ID -1 per SpatialHashConstants.BALL_ENTITY_ID)
            bool notOccluded = inFoV && !OcclusionFilter.IsOccluded(
                observerPos, ballPos2D, PerceptionConstants.AGENT_ID_NONE,
                agentStates, candidateIds, candidateCount, observerTeamId, agentAttrs);

            ballVisible = inRange && inFoV && notOccluded;

            if (ballVisible)
            {
                perceivedPosition = ballPos2D; // Ground truth when visible
                stalenessFrames   = 0;
            }
            else
            {
                // Retain last confirmed position (INV-8: BallStalenessFrames == 0 iff BallVisible == true)
                perceivedPosition = prevPerceivedPosition;
                stalenessFrames   = prevStalenessFrames + 1;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation.                                               |
// | 1.1     | 2026-05-28 | —      | AR-1 fix L-4: removed dead-code ternary in invisible-ball else branch.  |
// | 1.2     | 2026-05-29 | —      | AR-2 fix L-1: removed unused prevBallVisible parameter (dead after L-4). |
// | 1.3     | 2026-09-17 | —      | Co-located ball has no bearing: bypass FoV angle so Controlled holders cannot lose sight of their attached ball. |
// | 1.4     | 2026-09-17 | —      | ERR-007-004 back-prop citation; behavior unchanged from v1.3. |
#endregion
