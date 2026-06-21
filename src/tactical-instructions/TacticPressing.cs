// File:     src/tactical-instructions/TacticPressing.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.4, §3.1, Code Standards #20
// Purpose:  Local pressing-mode analogue of #8 PressingMode (KD-2). The consumer
//           translates Low→LOW, Medium→MEDIUM, High→HIGH (FR-TI-004 / §3.1).
//           This layer never references the #8 enum.

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Team pressing intensity (FR-TI-004). Local analogue of #8 <c>PressingMode</c>; the consumer
    /// translates it downward (§3.1). <see cref="Medium"/> (index 1) is the
    /// <see cref="TeamTactic.Balanced"/> identity.
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007). Append at the end only; a Stage-1
    /// widening beyond the three #8 peers is clamped to nearest by the consumer (F5).
    /// </summary>
    public enum TacticPressing : byte
    {
        /// <summary>Low press → #8 PressingMode.LOW. §3.1.</summary>
        Low = 0,

        /// <summary>Medium press (identity) → #8 PressingMode.MEDIUM. §3.1.</summary>
        Medium = 1,

        /// <summary>High press → #8 PressingMode.HIGH. §3.1.</summary>
        High = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
