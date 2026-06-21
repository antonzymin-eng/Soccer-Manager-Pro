// File:     src/tactical-instructions/Mentality.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.4, §3.2, Code Standards #20
// Purpose:  Master risk dial. 7-valued; the consumer (#8/#15) collapses it to a
//           (StyleProfile, riskMultiplier, defensiveLineBias) triple at T2 (§3.2).

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Team mentality, the master risk dial (§3.2 / FR-TI-011). 7-valued.
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007). The ordinal indexes
    /// <see cref="TacticalInstructionsConstants.MentalityRiskMult"/> and
    /// <see cref="TacticalInstructionsConstants.MentalityLineBias"/>; reordering or
    /// mid-insertion shifts those rows and breaks the §3.2 mapping. Append new values
    /// at the end only.
    /// </summary>
    public enum Mentality : byte
    {
        /// <summary>Most conservative. risk 0.80, line bias −0.20. §3.2.</summary>
        VeryDefensive = 0,

        /// <summary>risk 0.88, line bias −0.12. §3.2.</summary>
        Defensive = 1,

        /// <summary>risk 0.94, line bias −0.05. §3.2.</summary>
        Cautious = 2,

        /// <summary>Identity row. risk 1.00, line bias 0.00 (FR-TI-031). §3.2.</summary>
        Balanced = 3,

        /// <summary>risk 1.06, line bias +0.05. §3.2.</summary>
        Positive = 4,

        /// <summary>risk 1.14, line bias +0.12. §3.2.</summary>
        Attacking = 5,

        /// <summary>Most aggressive. risk 1.20, line bias +0.20. §3.2.</summary>
        VeryAttacking = 6
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).   |
#endregion
