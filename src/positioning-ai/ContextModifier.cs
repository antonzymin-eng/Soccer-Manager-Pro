// File: src/positioning-ai/ContextModifier.cs
// Created:  2026-05-29
// Modified: 2026-06-29
// Author:   —
// Spec: #12 Positioning AI §3.5; Tactical Instructions #21 §3.4 (FR-TI-016)
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

            // §3.5.2 (corrected — ERR-012-003): the rescale numerator carries the phase-keyed
            // baseLateral/baseVertical; the compactness denominator carries ONLY the dynamic
            // gain products (score/fatigue/intensity). The pre-fix code folded base[phase] into
            // BOTH sides, so it cancelled and the phase baseline was a no-op (all InPoss worked
            // examples used base=1.00, masking the defect). Now base[phase] survives.
            float lateralGain  = ComputeLateralGain(modifiers);
            float verticalGain = ComputeVerticalGain(modifiers);

            float lateralScale  = PositioningAIConstants.BaseLateral[(int)phase]  / lateralGain;
            float verticalScale = PositioningAIConstants.BaseVertical[(int)phase] / verticalGain;

            // #21 T2 (FR-TI-016): the manager width instruction widens/narrows the lateral
            // spread. In-possession phases consume TacticWidth; OOP phases consume TacticDefWidth.
            // Both Standard rows are scalar 1.00 (FR-TI-031), so a default tactic leaves lateralScale
            // byte-identical to pre-#21 (1.00 is exact in IEEE-754). The match-engine Phase-D writer
            // routes the live tactic onto modifiers.Width / modifiers.DefensiveWidth.
            float widthScalar = (phase == Phase.InPoss || phase == Phase.TransToAtk)
                ? TacticTranslation.WidthCompactnessScalar(modifiers.Width)
                : TacticTranslation.DefWidthCompactnessScalar(modifiers.DefensiveWidth);
            lateralScale *= widthScalar;

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
        /// Dynamic lateral gain product (phase-independent):
        ///     (1 + SCORE_ATK_GAIN × clamp(scoreDiff, -3, +3))
        ///   × (1 − FATIGUE_LATERAL_RELAX × teamMeanFatigue)
        /// §3.5.1, FR-PA-028, FR-PA-029, FR-PA-030. The phase-keyed baseLateral[phase] is
        /// applied separately as the §3.5.2 numerator (ERR-012-003) and is NOT folded in here.
        /// For InPoss (baseLateral = 1.00) this equals the historical lateralCompactness, so
        /// the §3.5.3 worked example (rescale ≈ 0.9671) is preserved.
        /// </summary>
        private static float ComputeLateralGain(ContextModifierInputs m)
        {
            float clamped = Mathf.Clamp(m.ScoreDiff, -3, 3);
            return (1f + PositioningAIConstants.SCORE_ATK_GAIN     * clamped)
                   * (1f - PositioningAIConstants.FATIGUE_LATERAL_RELAX * m.TeamMeanFatigue);
        }

        /// <summary>
        /// Dynamic vertical gain product (phase-independent):
        ///     (1 + INTENSITY_VERTICAL_GAIN × tacticalIntensity)
        /// §3.5.1, FR-PA-031. The phase-keyed baseVertical[phase] is applied separately as the
        /// §3.5.2 numerator (ERR-012-003) and is NOT folded in here.
        /// </summary>
        private static float ComputeVerticalGain(ContextModifierInputs m)
        {
            return (1f + PositioningAIConstants.INTENSITY_VERTICAL_GAIN * m.TacticalIntensity);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                                  |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                                                |
// | 1.1     | 2026-06-13 | —      | ERR-012-003: §3.5.2 baseLateral/baseVertical[phase] double-counted (numerator AND compactness factor) |
// |         |            |        | so the phase baseline cancelled to a no-op. Compactness helpers now hold dynamic gain products only;   |
// |         |            |        | base[phase] survives as the §3.5.2 numerator. InPoss (base=1.00) result unchanged (T-U-015 preserved). |
// | 1.2     | 2026-06-29 | —      | #21 T2 (FR-TI-016): lateralScale ×= phase-selected width scalar (TacticWidth in-poss /          |
// |         |            |        | TacticDefWidth OOP) via TacticTranslation. Standard ⇒ ×1.00 exact ⇒ byte-identical to pre-#21. |
#endregion
