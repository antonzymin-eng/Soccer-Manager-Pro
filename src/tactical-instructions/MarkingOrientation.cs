// File:     src/tactical-instructions/MarkingOrientation.cs
// Created:  2026-07-07
// Modified: 2026-07-07
// Author:   —
// Spec:     Tactical Instructions #21 §2.2.1, §3.4 (new), Code Standards #20
// Purpose:  Team defensive marking orientation dial (ball-oriented vs man-oriented pressure
//           on the #14 MAN_MARK candidate radius). New Stage-1 cheap-addition tactic axis.

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Marking orientation (new §3.4 axis). 3-valued; <see cref="Balanced"/> (index 1) is identity.
    /// Maps to a scalar on the #14 MAN_MARK candidate radius (§3.4 / FR-TI-033): a ball-oriented team
    /// favours zonal/ball-side shifting (narrower man-mark radius); a man-oriented team commits tighter
    /// to individual opponents (wider man-mark radius) at the cost of shape discipline.
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-TI-007). The ordinal indexes
    /// <see cref="TacticalInstructionsConstants.MarkingOrientationScalar"/>; append at the end only.
    /// </summary>
    public enum MarkingOrientation : byte
    {
        /// <summary>Favours zonal/ball-side coverage over individual duels. scalar 0.80. §3.4.</summary>
        BallOriented = 0,

        /// <summary>Identity row. scalar 1.00 (FR-TI-031). §3.4.</summary>
        Balanced = 1,

        /// <summary>Commits tighter to individual opponents. scalar 1.20. §3.4.</summary>
        ManOriented = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                            |
// | 1.0     | 2026-07-07 | —      | Initial implementation (cheap-item addition). |
#endregion
