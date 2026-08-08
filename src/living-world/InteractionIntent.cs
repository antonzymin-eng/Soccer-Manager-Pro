// File:     src/living-world/InteractionIntent.cs
// Created:  2026-06-21
// Modified: 2026-08-08
// Author:   —
// Spec:     Living World System #22 §2.2.6, §3.3, Appendix C, Code Standards #20
// Purpose:  InteractionIntent enum: graph-/event-driven intent, distinct from surface text.

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// The graph-/event-driven intent of an off-pitch interaction, separate from its surface text
    /// (#22 §3.3, KD-6). One intent maps to many phrasings via deterministic template expansion drawn
    /// from the world.text RNG sub-stream.
    ///
    /// Named InteractionIntent to avoid the existing Intent / AttackIntent / DistributeIntent enums in
    /// the gameplay assemblies (#22 §3.3). OPEN ROSTER (§2.2.6): indexed against the authored corpus.
    /// ORDINAL STABILITY (FR-LW-028): APPEND-only. Seed members are illustrative.
    /// </summary>
    public enum InteractionIntent : byte
    {
        /// <summary>No / unspecified intent (neutral default).</summary>
        None = 0,

        /// <summary>Media wants to provoke on title-pressure (a vol-2 §7.1 trap class).</summary>
        MediaProvokeTitlePressure = 1,

        /// <summary>Player questions their playing time.</summary>
        PlayerQuestionsMinutes = 2,

        /// <summary>Board signals confidence / patience.</summary>
        BoardSignalsConfidence = 3,
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-06-21 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
