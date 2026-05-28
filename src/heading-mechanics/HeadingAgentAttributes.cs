// File:     src/heading-mechanics/HeadingAgentAttributes.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §3.3–§3.7, Agent Movement #2 §3.5.6, Code Standards #20
// Purpose:  Heading Mechanics' snapshot of the three agent attributes and fatigue consumed by
//           §3.3 JumpReach, §3.4 contact quality, §3.5 power, and §3.7 duel scoring.

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Heading Mechanics' snapshot of agent attributes consumed from Agent Movement.
    /// Read once per jump phase; held constant for the full heading pipeline.
    /// Attributes are integers in [1, 20]; normalisation to [0, 1] uses HeadingMechanicsConstants.ATTR_MAX.
    /// Agent Movement #2 §3.5.6. Heading Mechanics #10 §3.3–§3.7.
    /// </summary>
    public struct HeadingAgentAttributes
    {
        /// <summary>Header quality attribute [1–20]. Governs jump reach, contact quality, duel score. Agent Movement §3.5.6.</summary>
        public int Heading;

        /// <summary>Muscular power attribute [1–20]. Governs jump reach, outgoing speed, duel score. Agent Movement §3.5.6.</summary>
        public int Strength;

        /// <summary>Stability attribute [1–20]. Governs jump reach, duel score. Agent Movement §3.5.6.</summary>
        public int Balance;

        /// <summary>
        /// Fatigue scalar [0, 1]. 0 = fully rested, 1 = fully fatigued.
        /// CLAUDE.md fatigue convention: 0 = rested, 1 = fatigued (FR-HE-011 / KD-9).
        /// Applied as a power penalty coefficient in §3.5.
        /// </summary>
        public float Fatigue;

        /// <summary>
        /// Team identifier (0 or 1). Used by §3.8 own-goal-shape flag to identify the agent's
        /// defended goal. Team 0 defends goal at X = 0; team 1 defends goal at X = PitchLengthM.
        /// Ball Physics #1 §1.2 coordinate system.
        /// </summary>
        public int TeamId;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
