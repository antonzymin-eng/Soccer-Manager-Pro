// File: src/positioning-ai/SpacingResolver.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI §3.6
// Purpose: Enforces minimum agent spacing via cost-based displacement, iterated up to SPACING_MAX_PASSES.

using UnityEngine;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Enforces hard minimum spacing (MIN_AGENT_SEPARATION_M = 1.5 m) between all agent slot pairs.
    /// Uses cost-based displacement: the agent whose slot is closer to its anchor is displaced.
    /// EntityId is the terminal tie-break when costs are within SPACING_EPSILON_M2.
    /// Iterates up to SPACING_MAX_PASSES per tick; non-convergence emits a dev-log warning but
    /// the partial result is still used (FR-PA-042 / failure mode F3 recovery).
    /// All methods are pure static; no instance state.
    /// </summary>
    public static class SpacingResolver
    {
        /// <summary>
        /// Applies hard-spacing enforcement in-place on outSlots.
        /// Pairs are evaluated in EntityId-ascending order (outer loop = lower EntityId first).
        /// </summary>
        /// <param name="outSlots">Current slots to enforce; index = SlotIndex. Length = SQUAD_SIZE.</param>
        /// <param name="anchors">Formation anchor per slot; used to compute displacement cost. Length = SQUAD_SIZE.</param>
        /// <param name="agentIds">EntityIds in SlotIndex order; used for tie-break. Length = SQUAD_SIZE.</param>
        public static void EnforceHardSpacing(
            Vector2[] outSlots,
            Vector2[] anchors,
            int[]     agentIds)
        {
            int squadSize = PositioningAIConstants.SQUAD_SIZE;

            for (int pass = 0; pass < PositioningAIConstants.SPACING_MAX_PASSES; pass++)
            {
                bool anyViolation = false;

                for (int i = 0; i < squadSize - 1; i++)
                {
                    if (IsSentinel(outSlots[i])) continue;

                    for (int j = i + 1; j < squadSize; j++)
                    {
                        if (IsSentinel(outSlots[j])) continue;

                        Vector2 diff  = outSlots[i] - outSlots[j];
                        float   distSq = diff.x * diff.x + diff.y * diff.y;

                        if (distSq + PositioningAIConstants.SPACING_EPSILON_M2 >=
                            PositioningAIConstants.MIN_AGENT_SEPARATION_M_SQ)
                            continue;

                        anyViolation = true;
                        Displace(outSlots, anchors, agentIds, i, j, diff, distSq);
                    }
                }

                if (!anyViolation) break;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
                if (pass == PositioningAIConstants.SPACING_MAX_PASSES - 1)
                    UnityEngine.Debug.LogWarning("[PositioningAI] Hard spacing did not converge in SPACING_MAX_PASSES passes.");
#endif
            }
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Displaces the cheaper-to-move agent (smaller |slot−anchor|²) away from the other.
        /// EntityId tie-break: higher EntityId is displaced when costs are within SPACING_EPSILON_M2.
        /// Worked example §3.6.5: A@(50.0,30.0), B@(50.8,30.6) → A displaced by 0.51 m.
        /// </summary>
        private static void Displace(
            Vector2[] outSlots, Vector2[] anchors, int[] agentIds,
            int i, int j, Vector2 diffIJ, float distSq)
        {
            float costI = SqrMagnitude(outSlots[i] - anchors[i]);
            float costJ = SqrMagnitude(outSlots[j] - anchors[j]);

            int displaceIdx;
            if (System.Math.Abs(costI - costJ) < PositioningAIConstants.SPACING_EPSILON_M2)
                // Tie-break: higher EntityId is displaced (KD-14).
                displaceIdx = agentIds[i] > agentIds[j] ? i : j;
            else
                displaceIdx = costI < costJ ? i : j;

            // Direction: displaced agent moves away from the other.
            // diffIJ = outSlots[i] - outSlots[j]
            // If displaceIdx == i: move i in direction of diffIJ (away from j).
            // If displaceIdx == j: move j in direction of -diffIJ (away from i).
            float mag = Mathf.Sqrt(distSq);
            // §3.6.3: displaceMag = MIN_AGENT_SEPARATION_M − dist + SPACING_EPSILON_M
            float displaceMag = PositioningAIConstants.MIN_AGENT_SEPARATION_M - mag
                                + PositioningAIConstants.SPACING_EPSILON_M;

            if (mag < PositioningAIConstants.SPACING_EPSILON_M)
            {
                // Perfectly overlapping — push along +X as a stable default.
                outSlots[displaceIdx] += new Vector2(displaceMag, 0f);
                return;
            }

            Vector2 unit = diffIJ / mag;
            if (displaceIdx == i)
                outSlots[i] += unit * displaceMag;
            else
                outSlots[j] -= unit * displaceMag;
        }

        private static float SqrMagnitude(Vector2 v) => v.x * v.x + v.y * v.y;

        private static bool IsSentinel(Vector2 v) => float.IsNegativeInfinity(v.x);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
