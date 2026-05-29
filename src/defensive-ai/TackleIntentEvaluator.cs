// File:     src/defensive-ai/TackleIntentEvaluator.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §3.6, §4.3, Code Standards #20
// Purpose:  Pure static module: evaluates tackle intent (COMMIT/JOCKEY/HOLD) for each
//           HOLD_SHAPE agent within TACKLE_ELIGIBLE_RADIUS_M of its assigned opponent.

using UnityEngine;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Produces per-agent <see cref="TackleIntentRequest"/> values for HOLD_SHAPE agents
    /// within tackle-eligible range (§3.6 / KD-6). Pure static; zero allocation.
    /// Output is surfaced to Decision Tree #8 via the orchestrator; #14 does NOT call
    /// Collision System #3 directly (parameter-based physics principle, CLAUDE.md).
    /// Defensive AI #14 §3.6 / §4.3.
    /// </summary>
    public static class TackleIntentEvaluator
    {
        /// <summary>
        /// Evaluates tackle intent for all eligible pool agents.
        /// </summary>
        /// <param name="snapshot">Current tick snapshot.</param>
        /// <param name="poolBuffer">EntityId-ascending HOLD_SHAPE pool.</param>
        /// <param name="poolCount">Number of valid pool entries.</param>
        /// <param name="assignments">Current committed assignments (one per pool agent).</param>
        /// <param name="lastManPoolIndex">Pool index of the last-man agent (-1 if no emergency).</param>
        /// <param name="isEmergency">True when the last-man emergency is active this tick.</param>
        /// <param name="tackleBuffer">Pre-allocated output buffer.</param>
        /// <param name="tackleCount">Number of TackleIntentRequests written.</param>
        public static void Evaluate(
            DefensiveSnapshot      snapshot,
            int[]                  poolBuffer,
            int                    poolCount,
            MarkAssignment[]       assignments,
            int                    lastManPoolIndex,
            bool                   isEmergency,
            TackleIntentRequest[]  tackleBuffer,
            out int                tackleCount)
        {
            tackleCount = 0;
            bool defendsX0 = LastManDetector.DefendsX0(snapshot.DefensiveTeamId);

            for (int p = 0; p < poolCount; p++)
            {
                int agentId = poolBuffer[p];
                if (assignments[p].TargetEntityId < 0)
                    continue; // ZONAL — no specific opponent.

                int aIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, agentId);
                int oIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, assignments[p].TargetEntityId);
                if (aIdx < 0 || oIdx < 0)
                    continue;

                ref readonly DefensiveAgentSnapshot agent = ref snapshot.Agents[aIdx];
                ref readonly DefensiveAgentSnapshot opp   = ref snapshot.Agents[oIdx];

                float dx   = opp.Position.x - agent.Position.x;
                float dy   = opp.Position.y - agent.Position.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist > DefensiveAIConstants.TackleEligibleRadiusM)
                    continue; // Out of tackle range.

                // Approach angle: angle between agent→opponent direction and agent velocity.
                float approachAngle;
                float velMag = agent.Velocity.magnitude;
                if (velMag < 0.001f)
                {
                    approachAngle = Mathf.PI * 0.5f; // Agent nearly stationary → worst case.
                }
                else
                {
                    Vector2 toOppNorm = new Vector2(dx, dy) / (dist > 0.001f ? dist : 1f);
                    Vector2 velNorm   = agent.Velocity / velMag;
                    float   dot       = Mathf.Clamp(Vector2.Dot(toOppNorm, velNorm), -1f, 1f);
                    approachAngle = Mathf.Acos(dot);
                }

                // Coverage depth: own-team pool agents between agent and own goal
                // within COVERAGE_DEPTH_CORRIDOR_M lateral band.
                int coverageDepth = ComputeCoverageDepth(
                    snapshot, poolBuffer, poolCount, aIdx, p, defendsX0);

                // Last-man override: when truly last man, never COMMIT (§3.6.2 / §3.8).
                bool isLastMan = isEmergency && (p == lastManPoolIndex);
                TackleMode mode = SelectMode(coverageDepth, approachAngle, isLastMan);

                tackleBuffer[tackleCount++] = new TackleIntentRequest
                {
                    AgentEntityId  = agentId,
                    Mode           = mode,
                    TargetEntityId = assignments[p].TargetEntityId,
                    ApproachAngle  = approachAngle,
                    CoverageDepth  = (byte)Mathf.Min(coverageDepth, 255),
                };
            }
        }

        // ── Coverage depth ────────────────────────────────────────────────────

        private static int ComputeCoverageDepth(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            int               agentSnapshotIdx,
            int               agentPoolIdx,
            bool              defendsX0)
        {
            ref readonly DefensiveAgentSnapshot agent = ref snapshot.Agents[agentSnapshotIdx];
            float agentDistToGoal = LastManDetector.DistToOwnGoal(agent.Position.x, defendsX0);
            float agentY          = agent.Position.y;
            int   depth           = 0;

            for (int p = 0; p < poolCount; p++)
            {
                if (p == agentPoolIdx)
                    continue;

                int tIdx = HoldShapePoolFilter.SnapshotIndexOf(snapshot, poolBuffer[p]);
                if (tIdx < 0)
                    continue;

                ref readonly DefensiveAgentSnapshot t = ref snapshot.Agents[tIdx];

                float yDiff       = Mathf.Abs(t.Position.y - agentY);
                float tDistToGoal = LastManDetector.DistToOwnGoal(t.Position.x, defendsX0);

                if (yDiff < DefensiveAIConstants.CoverageDepthCorridorM
                 && tDistToGoal < agentDistToGoal)
                {
                    depth++;
                }
            }

            return depth;
        }

        // ── Mode selection ────────────────────────────────────────────────────

        private static TackleMode SelectMode(int coverageDepth, float approachAngle, bool isLastMan)
        {
            if (isLastMan)
            {
                // Last man must never commit alone (§3.6.2 / §3.8).
                return approachAngle < DefensiveAIConstants.TackleJockeyAngleRad
                    ? TackleMode.Jockey
                    : TackleMode.Hold;
            }

            if (coverageDepth >= DefensiveAIConstants.TackleCommitCoverageFloor)
                return TackleMode.Commit;

            if (approachAngle < DefensiveAIConstants.TackleJockeyAngleRad)
                return TackleMode.Jockey;

            return TackleMode.Hold;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
