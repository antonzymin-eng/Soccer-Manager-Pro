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

            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly PressingAgentSnapshot a = ref snapshot.Agents[i];

                // Only own pressing team.
                if (a.TeamId != snapshot.PressingTeamId)
                    continue;

                // Eligibility: not goalkeeper.
                if (a.IsGoalkeeper)
                    continue;

                // Eligibility: fatigue below ceiling.
                if (a.Fatigue >= PressingAIConstants.PressFatigueCeiling)
                    continue;

                // Eligibility: within PressTriggerDistanceM² of projected interception point.
                float dx   = a.Position.x - projectedInterceptionPoint.x;
                float dy   = a.Position.y - projectedInterceptionPoint.y;
                float cost = dx * dx + dy * dy;

                if (cost > triggerDistSq)
                    continue;

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

                float dt = PressingAIConstants.InterceptLookaheadTicks * PressingAIConstants.DT_TACTICAL;
                return new Vector2(
                    a.Position.x + a.Velocity.x * dt,
                    a.Position.y + a.Velocity.y * dt);
            }

            return new Vector2(snapshot.BallPosition.x, snapshot.BallPosition.y);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
