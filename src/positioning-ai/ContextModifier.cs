// File: src/positioning-ai/ContextModifier.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI §3.5
// Purpose: Applies multiplicative compactness modifiers to base slots relative to the active centroid.

using UnityEngine;

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Applies context-driven compactness modifiers to each outfield agent's base slot.
    /// Operates on (slot − centroid), rescaling X (vertical depth) and Y (lateral width)
    /// independently based on phase, score difference, fatigue, and tactical intensity.
    /// Centroid uses mean of active outfield agents only (GK excluded, inactive excluded).
    /// All methods are pure static; no instance state.
    /// </summary>
    public static class ContextModifier
    {
        /// <summary>
        /// Applies lateral and vertical compactness modifiers to all active outfield slots in-place.
        /// Worked example §3.5.3: InPoss, scoreDiff=+2, fatigue=0.40 → lateral rescale = 0.9671 (3.29% tighter).
        /// </summary>
        /// <param name="outSlots">Array of current base slots (index 0=GK, 1-10=outfield); mutated in-place.</param>
        /// <param name="snapshot">Used to identify active outfield agents (IsActive, !IsGoalkeeper).</param>
        /// <param name="modifiers">Score, fatigue, intensity inputs from the orchestrator.</param>
        /// <param name="phase">Committed phase; indexes BaseLateral[] and BaseVertical[].</param>
        public static void ApplyToAll(
            Vector2[] outSlots,
            PositioningPerceptionSnapshot snapshot,
            ContextModifierInputs modifiers,
            Phase phase)
        {
            Vector2 centroid = ComputeCentroid(outSlots, snapshot);

            float lateralCompactness  = ComputeLateralCompactness(modifiers, phase);
            float verticalCompactness = ComputeVerticalCompactness(modifiers, phase);

            float lateralScale  = PositioningAIConstants.BaseLateral[(int)phase]  / lateralCompactness;
            float verticalScale = PositioningAIConstants.BaseVertical[(int)phase] / verticalCompactness;

            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly AgentPositioningData agent = ref snapshot.Agents[i];
                if (agent.IsGoalkeeper || !agent.IsActive) continue;

                int idx = agent.SlotIndex;
                Vector2 rel = outSlots[idx] - centroid;
                // §3.5.2: higher compactness → tighter → scale < 1 → rel shrinks.
                rel.y *= lateralScale;
                rel.x *= verticalScale;
                outSlots[idx] = centroid + rel;
            }
        }

        // ── Internal helpers ──────────────────────────────────────────────────

        /// <summary>
        /// Computes centroid of all active outfield agents (GK excluded, inactive excluded).
        /// Returns pitch centre when no active outfield agents are present (degenerate guard).
        /// </summary>
        private static Vector2 ComputeCentroid(Vector2[] outSlots, PositioningPerceptionSnapshot snapshot)
        {
            float sumX = 0f, sumY = 0f;
            int   count = 0;

            for (int i = 0; i < snapshot.Agents.Length; i++)
            {
                ref readonly AgentPositioningData a = ref snapshot.Agents[i];
                if (a.IsGoalkeeper || !a.IsActive) continue;
                sumX += outSlots[a.SlotIndex].x;
                sumY += outSlots[a.SlotIndex].y;
                count++;
            }

            if (count == 0)
                return new Vector2(PositioningAIConstants.PITCH_HALF_LENGTH_M, PositioningAIConstants.PITCH_HALF_WIDTH_M);

            return new Vector2(sumX / count, sumY / count);
        }

        /// <summary>
        /// lateralCompactness = baseLateral[phase]
        ///     × (1 + SCORE_ATK_GAIN × clamp(scoreDiff, -3, +3))
        ///     × (1 − FATIGUE_LATERAL_RELAX × teamMeanFatigue)
        /// §3.5.1, FR-PA-028, FR-PA-029, FR-PA-030.
        /// </summary>
        private static float ComputeLateralCompactness(ContextModifierInputs m, Phase phase)
        {
            float clamped = Mathf.Clamp(m.ScoreDiff, -3, 3);
            return PositioningAIConstants.BaseLateral[(int)phase]
                   * (1f + PositioningAIConstants.SCORE_ATK_GAIN     * clamped)
                   * (1f - PositioningAIConstants.FATIGUE_LATERAL_RELAX * m.TeamMeanFatigue);
        }

        /// <summary>
        /// verticalCompactness = baseVertical[phase]
        ///     × (1 + INTENSITY_VERTICAL_GAIN × tacticalIntensity)
        /// §3.5.1, FR-PA-031.
        /// </summary>
        private static float ComputeVerticalCompactness(ContextModifierInputs m, Phase phase)
        {
            return PositioningAIConstants.BaseVertical[(int)phase]
                   * (1f + PositioningAIConstants.INTENSITY_VERTICAL_GAIN * m.TacticalIntensity);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
