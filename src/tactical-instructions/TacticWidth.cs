// File:     src/tactical-instructions/TacticWidth.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.4, §3.4, Code Standards #20
// Purpose:  In-possession width dial. Feeds #12's existing lateral-compactness
//           scaling via a new ContextModifierInputs field (FR-TI-016 / KD-11) —
//           no new positioning branch.

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// In-possession team width (FR-TI-016). 5-valued; <see cref="Standard"/> (index 2) is identity.
    /// Maps to a compactness scalar [0.85..1.15] consumed by #12 (§3.4).
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007). The ordinal indexes
    /// <see cref="TacticalInstructionsConstants.WidthScalar"/>; append at the end only.
    /// </summary>
    public enum TacticWidth : byte
    {
        /// <summary>Narrowest shape. scalar 0.85. §3.4.</summary>
        VeryNarrow = 0,

        /// <summary>scalar 0.92. §3.4.</summary>
        Narrow = 1,

        /// <summary>Identity row. scalar 1.00 (FR-TI-031). §3.4.</summary>
        Standard = 2,

        /// <summary>scalar 1.08. §3.4.</summary>
        Wide = 3,

        /// <summary>Widest shape. scalar 1.15. §3.4.</summary>
        VeryWide = 4
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
