// File:     src/tactical-instructions/Duty.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.3, §3.4, Code Standards #20
// Purpose:  Per-agent duty. Biases positioning long-pct (#12), utility aggression
//           (#8), and the tackle COMMIT floor (#14) (FR-TI-013).

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Per-agent duty (FR-TI-013). 3-valued; <see cref="Support"/> (index 1) is the
    /// <see cref="PlayerTactic.Default"/> value. The ordinal indexes
    /// <see cref="TacticalInstructionsConstants.DutyForeOffsetM"/> and
    /// <see cref="TacticalInstructionsConstants.DutyAggressionBias"/>.
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007). Append at the end only.
    /// </summary>
    public enum Duty : byte
    {
        /// <summary>Hold position; fore-offset −Δ, aggression −0.05. §3.4.</summary>
        Defend = 0,

        /// <summary>Balanced duty (default); fore-offset 0, aggression 0. §3.4.</summary>
        Support = 1,

        /// <summary>Push forward; fore-offset +Δ, aggression +0.05. §3.4.</summary>
        Attack = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
