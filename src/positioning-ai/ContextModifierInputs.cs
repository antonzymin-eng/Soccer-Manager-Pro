// File: src/positioning-ai/ContextModifierInputs.cs
// Created:  2026-05-29
// Modified: 2026-06-29
// Author:   —
// Spec: #12 Positioning AI §3.5; Tactical Instructions #21 §3.4 (FR-TI-016)
// Purpose: External context values supplied by the match orchestrator each tactical tick.

using TacticalDirector.TacticalInstructions;

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

        /// <summary>
        /// #21 T2 routing field (FR-TI-016): the team's in-possession <see cref="TacticWidth"/>.
        /// ContextModifier translates it through
        /// <see cref="TacticTranslation.WidthCompactnessScalar"/> for in-possession phases. The
        /// zero-value enum default is <c>VeryNarrow</c>, so the identity-seeding 3-arg constructor
        /// sets <see cref="TacticWidth.Standard"/> explicitly (scalar 1.00 ⇒ behaviour-neutral,
        /// FR-TI-031). The match-engine Phase-D writer routes the active tactic here via the 5-arg
        /// constructor (MatchEngine v1.20); default Balanced ⇒ Standard ⇒ scalar 1.00.
        /// </summary>
        public readonly TacticWidth Width;

        /// <summary>
        /// #21 T2 routing field (FR-TI-016): the team's out-of-possession <see cref="TacticDefWidth"/>.
        /// Translated through <see cref="TacticTranslation.DefWidthCompactnessScalar"/> for the OOP
        /// phases. The zero-value enum default is <c>Narrow</c>, so the identity-seeding 3-arg
        /// constructor sets <see cref="TacticDefWidth.Standard"/> explicitly (scalar 1.00 ⇒
        /// behaviour-neutral, FR-TI-031).
        /// </summary>
        public readonly TacticDefWidth DefensiveWidth;

        /// <summary>
        /// Identity-seeding constructor (pre-#21 surface): seeds <see cref="Width"/> /
        /// <see cref="DefensiveWidth"/> to <c>Standard</c> so every existing caller stays
        /// byte-identical (scalar 1.00). The match-engine Phase-D writer uses the 5-arg overload.
        /// </summary>
        public ContextModifierInputs(int scoreDiff, float teamMeanFatigue, float tacticalIntensity)
            : this(scoreDiff, teamMeanFatigue, tacticalIntensity,
                   TacticWidth.Standard, TacticDefWidth.Standard)
        {
        }

        /// <summary>
        /// Full constructor including the #21 width routing fields (match-engine Phase-D writer).
        /// </summary>
        public ContextModifierInputs(
            int scoreDiff, float teamMeanFatigue, float tacticalIntensity,
            TacticWidth width, TacticDefWidth defensiveWidth)
        {
            ScoreDiff         = scoreDiff;
            TeamMeanFatigue   = teamMeanFatigue;
            TacticalIntensity = tacticalIntensity;
            Width             = width;
            DefensiveWidth    = defensiveWidth;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                            |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                          |
// | 1.1     | 2026-06-29 | —      | #21 T2: + Width / DefensiveWidth routing fields (FR-TI-016); 3-arg |
// |         |            |        |   ctor seeds Standard (scalar 1.00 ⇒ neutral); 5-arg ctor added.  |
// | 1.2     | 2026-06-29 | —      | Doc: match-engine Phase-D writer landed (MatchEngine v1.20),      |
// |         |            |        |   routes the active tactic Width / DefWidth via the 5-arg ctor.   |
#endregion
