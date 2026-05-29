// File:     src/perception-system/OcclusionFilter.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Perception System #7 §3.2, Code Standards #20
// Purpose:  Computes shadow cone geometry for opponent occluders (§3.2.3) and tests
//           whether a candidate entity falls inside any shadow cone (§3.2.4).
//           Opponents only at Stage 0 — teammate shadow cones deferred (OQ-1).
//           Static class. All methods are deterministic. No side effects.

using UnityEngine;

using TacticalDirector.AgentMovement;

namespace TacticalDirector.PerceptionSystem
{
    /// <summary>
    /// Computes shadow cone occlusion geometry per §3.2. Tests candidate entities against
    /// opponent shadow cones. Conservative approximation: slightly over-occludes at very close
    /// range — agents err toward caution, never false confidence (KD-3).
    /// Opponents only at Stage 0 (OQ-1). Perception System #7 §3.2.
    /// </summary>
    public static class OcclusionFilter
    {
        /// <summary>
        /// Computes the shadow cone half-angle (degrees) that an occluder casts from the observer's
        /// perspective. Clamped to MIN_SHADOW_HALF_ANGLE floor.
        /// Perception System #7 §3.2.3.
        /// </summary>
        /// <param name="observerPos">Observer's 2D world position.</param>
        /// <param name="occluderPos">Occluder's 2D world position.</param>
        public static float ComputeShadowHalfAngleDeg(Vector2 observerPos, Vector2 occluderPos)
        {
            float distance = (occluderPos - observerPos).magnitude;
            float safeDist = Mathf.Max(distance, PerceptionConstants.AGENT_BODY_RADIUS + PerceptionConstants.OCCLUDER_DIST_GUARD);
            float sinHalf  = Mathf.Min(PerceptionConstants.AGENT_BODY_RADIUS / safeDist, 1.0f);
            float halfAngleDeg = Mathf.Asin(sinHalf) * PerceptionConstants.RAD_TO_DEG;
            return Mathf.Max(halfAngleDeg, PerceptionConstants.MIN_SHADOW_HALF_ANGLE);
        }

        /// <summary>
        /// Returns true if the target entity at targetPos is occluded from the observer by any
        /// opponent in the supplied candidate list. Depth ordering is enforced: occluder must be
        /// closer to the observer than the target (§3.2.4).
        /// Perception System #7 §3.2.4.
        /// </summary>
        /// <param name="observerPos">Observer's 2D world position.</param>
        /// <param name="targetPos">Target entity's 2D world position.</param>
        /// <param name="targetId">Agent ID of the target (to skip self-occlusion). Use -1 for ball.</param>
        /// <param name="agentStates">Full agent state array indexed 0–21.</param>
        /// <param name="candidateIds">Entity IDs returned by the spatial hash query (may include ball as -1).</param>
        /// <param name="candidateCount">Number of valid entries in candidateIds.</param>
        /// <param name="observerTeamId">Observer's team ID. Only opposite-team agents cast shadow cones (Stage 0).</param>
        public static bool IsOccluded(
            Vector2 observerPos,
            Vector2 targetPos,
            int targetId,
            AgentState[] agentStates,
            int[] candidateIds,
            int candidateCount,
            int observerTeamId,
            PerceptionAgentAttributes[] agentAttrs)
        {
            float targetDistSq = (targetPos - observerPos).sqrMagnitude;
            float targetBearing = Mathf.Atan2(
                targetPos.y - observerPos.y,
                targetPos.x - observerPos.x) * PerceptionConstants.RAD_TO_DEG;

            for (int i = 0; i < candidateCount; i++)
            {
                int occluderId = candidateIds[i];

                // Skip: ball entities, the target itself, and self
                if (occluderId < 0 || occluderId == targetId)
                {
                    continue;
                }

                // Skip: only opponents cast shadow cones at Stage 0 (OQ-1)
                if (agentAttrs[occluderId].TeamId == observerTeamId)
                {
                    continue;
                }

                Vector2 occluderPos  = agentStates[occluderId].Position;
                float occluderDistSq = (occluderPos - observerPos).sqrMagnitude;

                // Depth ordering: occluder must be closer than target
                if (occluderDistSq >= targetDistSq)
                {
                    continue;
                }

                float shadowHalfAngle = ComputeShadowHalfAngleDeg(observerPos, occluderPos);
                float occluderBearing = Mathf.Atan2(
                    occluderPos.y - observerPos.y,
                    occluderPos.x - observerPos.x) * PerceptionConstants.RAD_TO_DEG;

                float bearingDiff = Mathf.Abs(targetBearing - occluderBearing);
                if (bearingDiff > PerceptionConstants.HALF_CIRCLE_DEG)
                {
                    bearingDiff = PerceptionConstants.FULL_CIRCLE_DEG - bearingDiff;
                }

                if (bearingDiff <= shadowHalfAngle)
                {
                    return true;
                }
            }

            return false;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
