// File:     src/attacking-ai/WeakSideController.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Attacking AI #15 §3.7, FR-AT-015, Code Standards #20
// Purpose:  Post-assignment step ensuring exactly one WEAK_SIDE agent when pool qualifies.
//           If the role-assignment loop did not assign WEAK_SIDE, selects the non-RUNNER agent
//           with max |Y − ball.Y| (EntityId ascending tiebreak) and assigns it.

using UnityEngine;

namespace TacticalDirector.AttackingAI
{
    /// <summary>
    /// Ensures exactly one WEAK_SIDE agent when pool size ≥ MIN_WEAK_SIDE_AGENT_THRESHOLD.
    /// Pure static. Runs as §3.13 step 7 post-check.
    /// Agent selection: max |agentY − ballY| among non-RUNNER agents; EntityId tiebreak (ascending).
    /// Attacking AI #15 §3.7 (FR-AT-015).
    /// </summary>
    internal static class WeakSideController
    {
        /// <summary>
        /// Ensures one WEAK_SIDE agent is assigned if the pool qualifies.
        /// No-op when pool is too small or WEAK_SIDE is already assigned.
        /// Mutates <see cref="AttackPoolEntry.AssignedRole"/> and
        /// <see cref="AttackPoolEntry.TargetPosition"/> of the selected agent.
        /// </summary>
        public static void EnsureWeakSide(
            Vector2           ballPosition,
            Vector2           ballCarrierPosition,
            AttackPoolEntry[] pool,
            int               poolCount)
        {
            if (poolCount < AttackingAIConstants.MinWeakSideAgentThreshold) return;

            // Check if WEAK_SIDE is already assigned.
            for (int i = 0; i < poolCount; i++)
            {
                if (pool[i].AssignedRole == AttackRole.WeakSide) return;
            }

            // Compute weak-side target position (§3.7).
            float halfWidth = AttackingAIConstants.PITCH_WIDTH_M * 0.5f;
            float targetY = (ballPosition.y > halfWidth)
                ? AttackingAIConstants.WeakSideFarYM                                    // near y=0
                : AttackingAIConstants.PITCH_WIDTH_M - AttackingAIConstants.WeakSideFarYM; // near y=68
            float targetX = Mathf.Clamp(
                ballCarrierPosition.x + AttackingAIConstants.WeakSideDepthOffsetM,
                0f, AttackingAIConstants.PITCH_LENGTH_M);
            Vector2 weakTarget = new Vector2(targetX, targetY);

            // Find the non-RUNNER agent with max |Y − ballY|; EntityId ascending tiebreak
            // (pool is already EntityId-ascending, so first encounter wins ties).
            int   bestIdx      = -1;
            float maxDeviation = -1f;

            for (int i = 0; i < poolCount; i++)
            {
                ref AttackPoolEntry e = ref pool[i];
                if (e.AssignedRole == AttackRole.Runner) continue;

                float deviation = System.Math.Abs(e.Position.y - ballPosition.y);
                if (deviation > maxDeviation)
                {
                    maxDeviation = deviation;
                    bestIdx      = i;
                }
            }

            if (bestIdx >= 0)
            {
                pool[bestIdx].AssignedRole   = AttackRole.WeakSide;
                pool[bestIdx].TargetPosition = weakTarget;
                pool[bestIdx].HasRunParams   = false;
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
