// File:     src/tactical-instructions/TacticDefWidth.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.4, §3.4, Code Standards #20
// Purpose:  Out-of-possession defensive width dial. Feeds #12's OOP compactness
//           scaling (FR-TI-016).

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Out-of-possession defensive width (FR-TI-016). 3-valued; <see cref="Standard"/> (index 1)
    /// is identity. Maps to an OOP compactness scalar [0.90..1.10] consumed by #12 (§3.4).
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007). The ordinal indexes
    /// <see cref="TacticalInstructionsConstants.DefWidthScalar"/>; append at the end only.
    /// </summary>
    public enum TacticDefWidth : byte
    {
        /// <summary>Most compact defensive block. scalar 0.90. §3.4.</summary>
        Narrow = 0,

        /// <summary>Identity row. scalar 1.00 (FR-TI-031). §3.4.</summary>
        Standard = 1,

        /// <summary>Widest defensive block. scalar 1.10. §3.4.</summary>
        Wide = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
