// File:     src/defensive-ai/LastManDetector.cs
// Created:  2026-05-29
// Modified: 2026-11-10
// Author:   —
// Spec:     Defensive AI #14 §3.8, §3.9, §4.3, ERR-014-008, Code Standards #20
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

                float d = DistToOwnGoal(snapshot.Agents[sIdx].Position.x);
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
            float ballDist = DistToOwnGoal(snapshot.BallPosition.x);
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

                float d = DistToOwnGoal(a.Position.x);
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

            float gkDist = DistToOwnGoal(snapshot.GkPosition.x);
            if (gkDist > DefensiveAIConstants.GkExpectedZoneMaxX)
            {
                result.IsGkOutOfZone      = true;
                result.AbandonedZoneCenter = ComputeAbandonedZoneCenter();

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
        /// Distance from x to the defended goal-line. #14 is canonical-only (the snapshot places the
        /// defended goal at x = 0 for both teams; see <see cref="DefensiveSnapshot"/>), so this is x
        /// itself. Never derived from a team id (ERR-014-008).
        /// </summary>
        public static float DistToOwnGoal(float x) => x;

        /// <summary>
        /// Generic two-orientation form of <see cref="DistToOwnGoal(float)"/> from #14 §3.8 / Appendix B.
        /// NOT reachable from production under the frame contract (every caller passes the canonical
        /// frame, i.e. <c>defendsX0 == true</c>); retained <c>internal</c> only because the pre-existing
        /// <c>GeometryHelperTests.DistToOwnGoal_BothTeamConventions</c> pins both branches.
        /// </summary>
        internal static float DistToOwnGoal(float x, bool defendsX0)
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
        /// Center of the abandoned GK zone in the canonical frame: (GK_EXPECTED_ZONE_MAX_X / 2,
        /// PITCH_WIDTH / 2) = (7.5, 34.0). #14 §3.9.2.
        /// </summary>
        public static Vector2 ComputeAbandonedZoneCenter()
        {
            return new Vector2(
                DefensiveAIConstants.GkExpectedZoneMaxX * 0.5f,
                DefensiveAIConstants.PITCH_WIDTH_M * 0.5f);
        }

        /// <summary>
        /// Generic two-orientation form of <see cref="ComputeAbandonedZoneCenter()"/> from #14 §3.9.2.
        /// NOT reachable from production under the frame contract (<c>defendsX0 == true</c> always);
        /// retained <c>internal</c> only because the pre-existing
        /// <c>GeometryHelperTests.AbandonedZoneCenter_DefendingX0</c> calls it by this signature.
        /// </summary>
        internal static Vector2 ComputeAbandonedZoneCenter(bool defendsX0)
        {
            Vector2 canonical = ComputeAbandonedZoneCenter();
            return defendsX0
                ? canonical
                : new Vector2(DefensiveAIConstants.PITCH_LENGTH_M - canonical.x, canonical.y);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                                  |
// | 1.1     | 2026-05-29 | —      | AR-1 M-1: guard GkEntityId < 0 before GK-zone check; Vector2.zero GkPosition gave       |
// |         |            |        |   DistToOwnGoal = 105 for x=105-defending team, falsely triggering COVER_GK_ZONE. |
// | 1.2     | 2026-11-10 | —      | ERR-014-008: removed DefendsX0(int teamId) (teamId == 0). The snapshot is in the     |
// |         |            |        |   defending team's canonical frame for BOTH teams, so the away team's last man, advancing |
// |         |            |        |   attacker and GK-zone centre were resolved against the wrong goal. Now sourced from   |
// |         |            |        |   DefensiveSnapshot.IsOwnGoalAtX0.                                                     |
// | 1.3     | 2026-11-10 | —      | W13.0 review M1: #14 is canonical-only. DistToOwnGoal(x) / ComputeAbandonedZoneCenter() are |
// |         |            |        |   the production forms; the bool defendsX0 forms become internal, marked unreachable under  |
// |         |            |        |   the frame contract, and kept only because pre-existing GeometryHelperTests pin them.     |
#endregion
