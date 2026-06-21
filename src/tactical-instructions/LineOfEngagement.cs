// File:     src/tactical-instructions/LineOfEngagement.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.4, §3.4, Code Standards #20
// Purpose:  How high up the pitch the team begins to press. Scales #13 press
//           trigger distances (FR-TI-017).

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Line of engagement (FR-TI-017). 5-valued; <see cref="Standard"/> (index 2) is identity.
    /// Maps to a scalar [0.80..1.20] on #13 press-trigger radius (§3.4).
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007). The ordinal indexes
    /// <see cref="TacticalInstructionsConstants.LineOfEngagementScalar"/>; append at the end only.
    /// </summary>
    public enum LineOfEngagement : byte
    {
        /// <summary>Engage deepest (drop off). scalar 0.80. §3.4.</summary>
        VeryLow = 0,

        /// <summary>scalar 0.90. §3.4.</summary>
        Low = 1,

        /// <summary>Identity row. scalar 1.00 (FR-TI-031). §3.4.</summary>
        Standard = 2,

        /// <summary>scalar 1.10. §3.4.</summary>
        High = 3,

        /// <summary>Engage highest (press from the front). scalar 1.20. §3.4.</summary>
        VeryHigh = 4
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
