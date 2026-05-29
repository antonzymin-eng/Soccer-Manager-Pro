// File:     src/pressing-ai/PrimaryPressSelector.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.3, Code Standards #20
// Purpose:  Pure static class: selects the single primary presser from eligible
//           own-team agents using minimum-cost (squared distance) assignment.

using UnityEngine;

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Selects the primary presser for one press tick.
    /// Eligibility, cost, and tie-break rules follow §3.3 exactly.
    /// Pressing AI #13 §3.3.
    /// </summary>
    public static class PrimaryPressSelector
    {
        /// <summary>
        /// Selects the eligible own-team agent with minimum squared distance to the
        /// projected interception point. Returns -1 when no eligible agent exists.
        /// </summary>
        /// <param name="snapshot">Current tick snapshot.</param>
        /// <param name="projectedInterceptionPoint">
        /// Projected interception point computed as:
        /// carrierPos + carrierVel * InterceptLookaheadTicks * DT_TACTICAL.
        /// </param>
        /// <param name="presserTargetPos">
        /// Out: world-space target position for the selected presser.
        /// Set to <paramref name="projectedInterceptionPoint"/> when a presser is found.
        /// </param>
        /// <returns>EntityId of the selected primary presser, or -1 if none eligible.</returns>
        public static int Select(
            PressingSnapshot snapshot,
            Vector2 projectedInterceptionPoint,
            out Vector2 presserTargetPos)
        {
            int   bestId   = -1;
            float bestCost = float.MaxValue;

            float triggerDistSq = PressingAIConstants.PressTriggerDistanceM
                                * PressingAIConstants.PressTriggerDistanceM;

            // Eligibility constraint 3 (§3.3 / #8 §3.1.8.2): distance is from ball-carrier.
            Vector2 carrierPos = GetCarrierPosition(snapshot);

            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];

                // Only own pressing team.
                if (a.TeamId != snapshot.PressingTeamId)
                    continue;

                // Eligibility: active agent only.
                if (!a.IsActive)
                    continue;

                // Eligibility: not goalkeeper.
                if (a.IsGoalkeeper)
                    continue;

                // Eligibility: fatigue below ceiling.
                if (a.Fatigue >= PressingAIConstants.PressFatigueCeiling)
                    continue;

                // Eligibility: within PressTriggerDistanceM of ball-carrier (#8 §3.1.8.2).
                float cDx = a.Position.x - carrierPos.x;
                float cDy = a.Position.y - carrierPos.y;
                if (cDx * cDx + cDy * cDy > triggerDistSq)
                    continue;

                // Cost: minimize distance to projected interception point (§3.3 cost function).
                float dx   = a.Position.x - projectedInterceptionPoint.x;
                float dy   = a.Position.y - projectedInterceptionPoint.y;
                float cost = dx * dx + dy * dy;

                // Select minimum cost; EntityId ascending tie-break within epsilon.
                if (bestId < 0
                    || cost < bestCost - PressingAIConstants.SpacingEpsilonM2
                    || (cost - bestCost < PressingAIConstants.SpacingEpsilonM2 && a.EntityId < bestId))
                {
                    bestId   = a.EntityId;
                    bestCost = cost;
                }
            }

            presserTargetPos = bestId >= 0 ? projectedInterceptionPoint : Vector2.zero;
            return bestId;
        }

        /// <summary>
        /// Computes the projected interception point from ball-carrier state.
        /// Formula: carrierPos + carrierVel * InterceptLookaheadTicks * DT_TACTICAL. §3.3.
        /// </summary>
        /// <param name="snapshot">Current tick snapshot.</param>
        /// <returns>Projected interception point in world space (X, Y).</returns>
        public static Vector2 ComputeInterceptionPoint(PressingSnapshot snapshot)
        {
            int carrierId = snapshot.BallCarrierEntityId;
            if (carrierId < 0)
                return new Vector2(snapshot.BallPosition.x, snapshot.BallPosition.y);

            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.EntityId != carrierId)
                    continue;
                if (!a.IsActive)
                    break;

                float dt = PressingAIConstants.InterceptLookaheadTicks * PressingAIConstants.DT_TACTICAL;
                return new Vector2(
                    a.Position.x + a.Velocity.x * dt,
                    a.Position.y + a.Velocity.y * dt);
            }

            return new Vector2(snapshot.BallPosition.x, snapshot.BallPosition.y);
        }

        /// <summary>Returns the ball-carrier's current position; falls back to ball position when carrier is unavailable.</summary>
        private static Vector2 GetCarrierPosition(PressingSnapshot snapshot)
        {
            int carrierId = snapshot.BallCarrierEntityId;
            if (carrierId < 0)
                return new Vector2(snapshot.BallPosition.x, snapshot.BallPosition.y);

            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];
                if (a.EntityId == carrierId && a.IsActive)
                    return a.Position;
            }

            return new Vector2(snapshot.BallPosition.x, snapshot.BallPosition.y);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-05-29 | —      | AR-1 H-3: eligibility distance now checks ball-carrier position per §3.3/#8 §3.1.8.2; cost unchanged (interception point). AR-1 H-1: added IsActive guard. Added GetCarrierPosition helper. |
#endregion
