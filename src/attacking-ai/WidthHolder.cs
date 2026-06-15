// File:     src/attacking-ai/WidthHolder.cs
// Created:  2026-05-29
// Modified: 2026-06-15
// Author:   —
// Spec:     Attacking AI #15 §3.6, FR-AT-014, Code Standards #20
// Purpose:  Enforces MIN_WIDTH_HOLDERS near-touchline agents. If fewer than required, promotes
//           the closest-to-touchline non-RUNNER agents to HOLD_WIDTH and sets target positions.

using UnityEngine;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Near-touchline width-holding enforcement. Pure static.
    /// Runs after the main role-assignment loop (§3.13 step 6).
    /// Promotes non-RUNNER pool agents to HOLD_WIDTH if the near-side count is below
    /// MIN_WIDTH_HOLDERS, using ascending |Y − nearTouchlineY| with EntityId tiebreak.
    /// Attacking AI #15 §3.6 (FR-AT-014).
    /// </summary>
    internal static class WidthHolder
    {
        /// <summary>
        /// Enforces width-holding in <paramref name="pool"/>. Mutates AssignedRole and
        /// TargetPosition of any promoted agents.
        /// </summary>
        /// <param name="ballPosition">Ball position (X, Y) in metres.</param>
        /// <param name="ballCarrierPosition">Ball carrier position (X, Y); used for target X.</param>
        /// <param name="pool">Attacking pool; EntityId-ascending; mutated in place.</param>
        /// <param name="poolCount">Number of valid entries.</param>
        public static void Enforce(
            Vector2         ballPosition,
            Vector2         ballCarrierPosition,
            AttackPoolEntry[] pool,
            int             poolCount)
        {
            float halfWidth = AttackingAIConstants.PITCH_WIDTH_M * 0.5f;
            bool  ballOnHighSide = ballPosition.y >= halfWidth;

            // Near-touchline Y: 4 m from the touchline on the ball's side.
            float nearTouchlineY = ballOnHighSide
                ? AttackingAIConstants.PITCH_WIDTH_M - AttackingAIConstants.TouchlineHoldDistM
                : AttackingAIConstants.TouchlineHoldDistM;

            Vector2 holdTarget = new Vector2(ballCarrierPosition.x, nearTouchlineY);

            // Count agents already on the near side holding width.
            int nearSideCount = 0;
            for (int i = 0; i < poolCount; i++)
            {
                ref AttackPoolEntry e = ref pool[i];
                if (e.AssignedRole != AttackRole.HoldWidth && e.AssignedRole != AttackRole.WeakSide)
                    continue;
                if (IsOnNearSide(e.Position.y, ballPosition.y, halfWidth))
                    nearSideCount++;
            }

            // Promote candidates until MIN_WIDTH_HOLDERS is met or no candidates remain.
            while (nearSideCount < AttackingAIConstants.MinWidthHolders)
            {
                // Find the closest-to-near-touchline non-RUNNER agent (ascending |Y − nearTouchlineY|,
                // EntityId tiebreak — EntityId already ascending in pool so first match wins ties).
                int   bestIdx  = -1;
                float bestDist = float.MaxValue;

                for (int i = 0; i < poolCount; i++)
                {
                    ref AttackPoolEntry e = ref pool[i];
                    if (e.AssignedRole == AttackRole.Runner) continue;
                    // Skip agents already counted in nearSideCount (HoldWidth or WeakSide on near side).
                    if ((e.AssignedRole == AttackRole.HoldWidth || e.AssignedRole == AttackRole.WeakSide)
                        && IsOnNearSide(e.Position.y, ballPosition.y, halfWidth))
                        continue;

                    float dist = System.Math.Abs(e.Position.y - nearTouchlineY);
                    if (dist < bestDist)
                    {
                        bestDist = dist;
                        bestIdx  = i;
                    }
                }

                if (bestIdx < 0) break; // no eligible candidates

                pool[bestIdx].AssignedRole      = AttackRole.HoldWidth;
                pool[bestIdx].TargetPosition    = holdTarget;
                pool[bestIdx].HasTargetPosition = true;
                pool[bestIdx].HasRunParams      = false;
                nearSideCount++;
            }

            // Set target positions for any HOLD_WIDTH agents that don't yet have one.
            for (int i = 0; i < poolCount; i++)
            {
                ref AttackPoolEntry e = ref pool[i];
                if (e.AssignedRole == AttackRole.HoldWidth && !e.HasTargetPosition)
                {
                    e.TargetPosition    = holdTarget;
                    e.HasTargetPosition = true;
                }
            }
        }

        private static bool IsOnNearSide(float agentY, float ballY, float halfWidth)
        {
            return (ballY >= halfWidth) ? (agentY >= halfWidth) : (agentY < halfWidth);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
// | 1.1     | 2026-05-29 | —      | AR-1 M-2: extended promotion-loop skip condition to include near-side WeakSide agents (already counted in nearSideCount, must not be re-promoted). |
// | 1.2     | 2026-06-15 | —      | AR-4 L-2: replaced the fragile `TargetPosition == Vector2.zero` default-fill guard with the explicit HasTargetPosition flag (a legitimate target can sit at the pitch origin); sets the flag on promoted agents. |
#endregion
