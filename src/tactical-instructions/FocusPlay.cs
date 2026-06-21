// File:     src/tactical-instructions/FocusPlay.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.4, §3.3, Code Standards #20
// Purpose:  Lateral attacking-focus preference. NEW branch (KD-11): a lateral term
//           in #8 OptionGenerator + a flank bias in #15 OverloadDetector (FR-TI-021).

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Where the team prefers to build attacks (FR-TI-021). 4-valued; <see cref="Mixed"/> (index 0)
    /// is the <see cref="TeamTactic.Balanced"/> identity (no lateral preference). NEW logic — no
    /// existing #8 hook (selection is pure max EffectiveUtility); reviewed in §5.6.
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007). Append at the end only.
    /// </summary>
    public enum FocusPlay : byte
    {
        /// <summary>No lateral preference (identity). §3.3.</summary>
        Mixed = 0,

        /// <summary>Prefer building down the left flank. §3.3.</summary>
        LeftFlank = 1,

        /// <summary>Prefer building down the right flank. §3.3.</summary>
        RightFlank = 2,

        /// <summary>Prefer playing through the middle. §3.3.</summary>
        ThroughMiddle = 3
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
