// File:     src/defensive-ai/LastManDetector.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Defensive AI #14 §3.8, §3.9, §4.3, Code Standards #20
// Purpose:  Pure static module: computes the last-man predicate (§3.8) and the
//           COVER_GK_ZONE emergency override trigger (§3.9).

using UnityEngine;

namespace TacticalDirector.DefensiveAI
{
    /// <summary>
    /// Output of <see cref="LastManDetector.Evaluate"/>.
    /// </summary>
    public struct LastManResult
    {
        /// <summary>True when the last-man threat predicate fires this tick (§3.8).</summary>
        public bool IsEmergency;

        /// <summary>Pool index of the last-man candidate agent. -1 when IsEmergency is false.</summary>
        public int LastManPoolIndex;

        /// <summary>EntityId of the last-man candidate. -1 when IsEmergency is false.</summary>
        public int LastManEntityId;

        /// <summary>EntityId of the highest-proximity advancing attacker. -1 when IsEmergency is false.</summary>
        public int AdvancingAttackerEntityId;

        /// <summary>True when the GK is out of its expected zone AND IsEmergency is true (§3.9).</summary>
        public bool IsGkOutOfZone;

        /// <summary>Pool index of the agent selected to cover the GK zone. -1 when IsGkOutOfZone is false.</summary>
        public int CoverAgentPoolIndex;

        /// <summary>EntityId of the agent selected to cover the GK zone. -1 when IsGkOutOfZone is false.</summary>
        public int CoverAgentEntityId;

        /// <summary>Center of the abandoned GK zone (world-space X, Y). Zero when IsGkOutOfZone is false.</summary>
        public Vector2 AbandonedZoneCenter;
    }

    /// <summary>
    /// Computes the last-man predicate (§3.8) and GK-zone emergency override (§3.9).
    /// Pure static; no side effects. Defensive AI #14 §3.8 / §3.9 / §4.3.
    ///
    /// <para>
    /// Last-man candidate: the HOLD_SHAPE pool agent with minimum distToOwnGoal.
    /// Tie-break: lowest EntityId (FR-DA-033 / KD-12).
    /// </para>
    /// <para>
    /// Last-man threat fires when ball is closer to own goal than lastMan + LAST_MAN_BALL_BUFFER_M
    /// AND ball distToOwnGoal &gt; LAST_MAN_OWN_HALF_MIN_X.
    /// </para>
    /// </summary>
    public static class LastManDetector
    {
        /// <summary>
        /// Evaluates the last-man and GK-zone emergency conditions.
        /// </summary>
        /// <param name="snapshot">Current tick snapshot.</param>
        /// <param name="poolBuffer">EntityId-ascending HOLD_SHAPE pool.</param>
        /// <param name="poolCount">Number of valid entries in poolBuffer.</param>
        /// <param name="result">Populated with the evaluation result.</param>
        public static void Evaluate(
            DefensiveSnapshot snapshot,
            int[]             poolBuffer,
            int               poolCount,
            out LastManResult result)
        {
            result = default;
            result.LastManPoolIndex         = -1;
            result.LastManEntityId          = -1;
            result.AdvancingAttackerEntityId = -1;
            result.CoverAgentPoolIndex      = -1;
            result.CoverAgentEntityId       = -1;

            if (poolCount == 0)
                return;

            bool defendsX0 = DefendsX0(snapshot.DefensiveTeamId);

            // Find last-man candidate: minimum distToOwnGoal with EntityId ascending tie-break.
            float minDist     = float.MaxValue;
            int   lastManIdx  = -1;
            int   lastManId   = -1;

            for (int p = 0; p < poolCount; p++)
            {
                int entityId = poolBuffer[p];
                int sIdx     = HoldShapePoolFilter.SnapshotIndexOf(snapshot, entityId);
                if (sIdx < 0)
                    continue;

                float d = DistToOwnGoal(snapshot.Agents[sIdx].Position.x, defendsX0);
                if (d < minDist || (d == minDist && entityId < lastManId))
                {
                    minDist    = d;
                    lastManIdx = p;
                    lastManId  = entityId;
                }
            }

            if (lastManIdx < 0)
                return;

            // Evaluate last-man threat predicate.
            float ballDist = DistToOwnGoal(snapshot.BallPosition.x, defendsX0);
            bool isThreat  = ballDist < (minDist + DefensiveAIConstants.LastManBallBufferM)
                          && ballDist > DefensiveAIConstants.LastManOwnHalfMinX;

            if (!isThreat)
                return;

            // Last-man threat is active.
            result.IsEmergency   = true;
            result.LastManPoolIndex = lastManIdx;
            result.LastManEntityId  = lastManId;

            // Find highest-proximity advancing attacker (opponent with smallest distToOwnGoal).
            int   advAttackerId = -1;
            float minOppDist    = float.MaxValue;
            int   defensiveTeam = snapshot.DefensiveTeamId;

            for (int i = 0; i < snapshot.AgentCount; i++)
            {
                ref readonly DefensiveAgentSnapshot a = ref snapshot.Agents[i];
                if (a.TeamId == defensiveTeam)
                    continue;
                if (!a.IsActive)
                    continue;

                float d = DistToOwnGoal(a.Position.x, defendsX0);
                if (d < minOppDist || (d == minOppDist && a.EntityId < advAttackerId))
                {
                    minOppDist    = d;
                    advAttackerId = a.EntityId;
                }
            }

            result.AdvancingAttackerEntityId = advAttackerId;

            // Evaluate GK-zone override (§3.9): only when emergency is active.
            // Guard: GkEntityId = -1 means no GK on pitch; skip zone check entirely to
            // avoid false trigger from GkPosition = Vector2.zero (§2.3 GkEntityId sentinel).
            if (snapshot.GkEntityId < 0)
                return;

            float gkDist = DistToOwnGoal(snapshot.GkPosition.x, defendsX0);
            if (gkDist > DefensiveAIConstants.GkExpectedZoneMaxX)
            {
                result.IsGkOutOfZone      = true;
                result.AbandonedZoneCenter = ComputeAbandonedZoneCenter(defendsX0);

                // Find pool agent (excluding last-man) with minimum cost to abandonedZoneCenter.
                float minCost     = float.MaxValue;
                int   coverPoolIdx = -1;
                int   coverId     = -1;

                for (int p = 0; p < poolCount; p++)
                {
                    if (p == lastManIdx)
                        continue;

                    int entityId = poolBuffer[p];
                    int sIdx     = HoldShapePoolFilter.SnapshotIndexOf(snapshot, entityId);
                    if (sIdx < 0)
                        continue;

                    float cost = DisplacementCost(snapshot.Agents[sIdx].Position, result.AbandonedZoneCenter);
                    if (cost < minCost || (cost == minCost && entityId < coverId))
                    {
                        minCost      = cost;
                        coverPoolIdx = p;
                        coverId      = entityId;
                    }
                }

                result.CoverAgentPoolIndex = coverPoolIdx;
                result.CoverAgentEntityId  = coverId;
            }
        }

        // ── Helpers ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true when the defensive team defends the x=0 goal-line.
        /// TeamId 0 (home) defends x=0; TeamId 1 (away) defends x=105.
        /// </summary>
        public static bool DefendsX0(int teamId) => teamId == 0;

        /// <summary>
        /// Distance from x to own goal-line (team-agnostic).
        /// For defending-x0 teams: distToOwnGoal = x.
        /// For defending-x105 teams: distToOwnGoal = 105 - x.
        /// </summary>
        public static float DistToOwnGoal(float x, bool defendsX0)
        {
            return defendsX0
                ? x
                : DefensiveAIConstants.PITCH_LENGTH_M - x;
        }

        /// <summary>Squared displacement cost between two positions (m²). §3.4.</summary>
        public static float DisplacementCost(Vector2 from, Vector2 to)
        {
            float dx = from.x - to.x;
            float dy = from.y - to.y;
            return dx * dx + dy * dy;
        }

        /// <summary>
        /// Center of the abandoned GK zone for the given team.
        /// For defending-x0: (GK_EXPECTED_ZONE_MAX_X / 2, PITCH_WIDTH / 2).
        /// For defending-x105: (105 - GK_EXPECTED_ZONE_MAX_X / 2, PITCH_WIDTH / 2).
        /// </summary>
        public static Vector2 ComputeAbandonedZoneCenter(bool defendsX0)
        {
            float halfZone = DefensiveAIConstants.GkExpectedZoneMaxX * 0.5f;
            float x        = defendsX0
                ? halfZone
                : DefensiveAIConstants.PITCH_LENGTH_M - halfZone;
            float y        = DefensiveAIConstants.PITCH_WIDTH_M * 0.5f;
            return new Vector2(x, y);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                                  |
// | 1.1     | 2026-05-29 | —      | AR-1 M-1: guard GkEntityId < 0 before GK-zone check; Vector2.zero GkPosition gave       |
// |         |            |        |   DistToOwnGoal = 105 for x=105-defending team, falsely triggering COVER_GK_ZONE. |
#endregion
