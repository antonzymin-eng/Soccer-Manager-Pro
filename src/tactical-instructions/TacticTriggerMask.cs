// File:     src/tactical-instructions/TacticTriggerMask.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.4, §3.1, Code Standards #20
// Purpose:  Local [Flags] analogue of #13 TriggerFlags (KD-2). Gates which press
//           triggers are active; the consumer translates bitwise 1:1 by flag name,
//           dropping unknown bits (FR-TI-018 / §3.1). This layer never references
//           the #13 enum.

using System;

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Active-press-trigger mask (FR-TI-018). Local analogue of #13 <c>TriggerFlags</c>; an unset
    /// flag disables that trigger. The consumer translates bitwise 1:1 by flag name (§3.1).
    /// <see cref="None"/> is the <see cref="TeamTactic.Balanced"/> identity.
    /// BIT-POSITION STABILITY (FR-TI-007): byte-backed [Flags]; bit positions are APPEND-only and the
    /// mask is capped at the 8-flag byte ceiling. Do not renumber existing flags.
    /// </summary>
    [Flags]
    public enum TacticTriggerMask : byte
    {
        /// <summary>No press triggers active (identity). §3.1.</summary>
        None = 0,

        /// <summary>Trigger on a bad first touch → #13 TriggerFlags.BadTouch. §3.1.</summary>
        BadTouch = 1 << 0,

        /// <summary>Trigger on a backward pass → #13 TriggerFlags.BackwardPass. §3.1.</summary>
        BackwardPass = 1 << 1,

        /// <summary>Trigger on a sideline trap → #13 TriggerFlags.SidelineTrap. §3.1.</summary>
        SidelineTrap = 1 << 2,

        /// <summary>Trigger on a weak receiver → #13 TriggerFlags.WeakReceiver. §3.1.</summary>
        WeakReceiver = 1 << 3
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
