// File:     src/tactical-instructions/TacticPassing.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.4, §3.1, Code Standards #20
// Purpose:  Local passing-style analogue of #8 PassingStyle (KD-2). The consumer
//           translates Short→SHORT, Mixed→MIXED, Direct→DIRECT (FR-TI-004 / §3.1).
//           This layer never references the #8 enum.

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Team passing style (FR-TI-004). Local analogue of #8 <c>PassingStyle</c>; the consumer
    /// translates it downward (§3.1). <see cref="Mixed"/> (index 1) is the
    /// <see cref="TeamTactic.Balanced"/> identity.
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007). Append at the end only; a Stage-1
    /// widening beyond the three #8 peers is clamped to nearest by the consumer (F5).
    /// </summary>
    public enum TacticPassing : byte
    {
        /// <summary>Short passing → #8 PassingStyle.SHORT. §3.1.</summary>
        Short = 0,

        /// <summary>Mixed passing (identity) → #8 PassingStyle.MIXED. §3.1.</summary>
        Mixed = 1,

        /// <summary>Direct passing → #8 PassingStyle.DIRECT. §3.1.</summary>
        Direct = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
