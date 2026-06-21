// File:     src/tactical-instructions/Tempo.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.4, §3.3, Code Standards #20
// Purpose:  Forward-vs-retain tempo dial. NEW #8 branch (KD-11): feeds the §3.3
//           tempoActionBias utility factor + an OptionGenerator breadth bias.
//           NOT a tick-rate change and NOT a decision threshold (FR-TI-015).

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Team tempo (FR-TI-015). 5-valued; <see cref="Standard"/> (index 2) is the identity row.
    /// Higher tempo raises PASS/SHOOT relative to HOLD via the §3.3 product; it is NOT a tick-rate
    /// or threshold change (the 10 Hz / 60 Hz loop invariant is preserved).
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007). The ordinal indexes
    /// <see cref="TacticalInstructionsConstants.TempoActionBias"/> and
    /// <see cref="TacticalInstructionsConstants.TempoBreadthScalar"/>; append at the end only.
    /// </summary>
    public enum Tempo : byte
    {
        /// <summary>Slowest build-up. §3.3.</summary>
        VerySlow = 0,

        /// <summary>Slow build-up. §3.3.</summary>
        Slow = 1,

        /// <summary>Identity row — all §3.3 tempo factors 1.0 (FR-TI-031). §3.3.</summary>
        Standard = 2,

        /// <summary>Quicker ball progression. §3.3.</summary>
        Fast = 3,

        /// <summary>Fastest, most direct progression. §3.3.</summary>
        VeryFast = 4
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
