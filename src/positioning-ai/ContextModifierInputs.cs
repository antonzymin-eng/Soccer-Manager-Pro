// File: src/positioning-ai/ContextModifierInputs.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI §3.5
// Purpose: External context values supplied by the match orchestrator each tactical tick.

namespace TacticalDirector.PositioningAI
{
    /// <summary>
    /// Per-tick modifier inputs consumed by ContextModifier to scale compactness.
    /// Supplied by the match orchestrator; not derived inside the positioning module.
    /// </summary>
    public readonly struct ContextModifierInputs
    {
        /// <summary>
        /// Goal difference from own team's perspective. Positive = own team leading.
        /// Clamped to [-3, +3] before use per §3.5.1 FR-PA-017.
        /// </summary>
        public readonly int ScoreDiff;

        /// <summary>
        /// Team mean fatigue [0, 1]. 0 = fully rested, 1 = fully fatigued per CLAUDE.md fatigue convention.
        /// FR-PA-016.
        /// </summary>
        public readonly float TeamMeanFatigue;

        /// <summary>Tactical intensity [0, 1] set by per-archetype GT default at Stage 0. FR-PA-018, FR-PA-032.</summary>
        public readonly float TacticalIntensity;

        public ContextModifierInputs(int scoreDiff, float teamMeanFatigue, float tacticalIntensity)
        {
            ScoreDiff         = scoreDiff;
            TeamMeanFatigue   = teamMeanFatigue;
            TacticalIntensity = tacticalIntensity;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
