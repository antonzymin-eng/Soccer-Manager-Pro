// File:     src/tactical-instructions/InstrBias.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.2, §3.4, Code Standards #20
// Purpose:  Three-way per-action instruction bias. Each PlayerInstructions bias
//           modulates exactly its named #8 term; Default is the identity (FR-TI-014).

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Per-action instruction bias (FR-TI-014). 3-valued; <see cref="Default"/> (index 1) is the
    /// multiplicative identity. The ordinal indexes
    /// <see cref="TacticalInstructionsConstants.InstrBiasMult"/> ({0.85, 1.00, 1.15}).
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007). Append at the end only.
    /// </summary>
    public enum InstrBias : byte
    {
        /// <summary>Do this less. ×0.85. §3.4.</summary>
        Less = 0,

        /// <summary>Follow the team default. ×1.00 (identity). §3.4.</summary>
        Default = 1,

        /// <summary>Do this more. ×1.15. §3.4.</summary>
        More = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
