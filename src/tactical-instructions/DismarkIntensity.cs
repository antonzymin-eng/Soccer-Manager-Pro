// File:     src/tactical-instructions/DismarkIntensity.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Dismarking AI #23 §2.2.2, Tactical Instructions #21 §2.2.1 (back-prop ERR-021-005),
//           Code Standards #20
// Purpose:  Team dismarking-intensity dial: how aggressively off-ball attackers nudge their
//           composed positioning targets away from a perceived marker (#23 FM-DM-02).

namespace TacticalDirector.TacticalInstructions
{
    /// <summary>
    /// Dismarking intensity (#23 §2.2.2; #21-owned after back-prop ERR-021-005). 3-valued;
    /// <see cref="Off"/> (zero value) is the exact identity: offset scalar 0.0 and marked-pass
    /// penalty ×1.0, so a default match is byte-identical to pre-#23 (FR-DM-012 / KD-4).
    /// The ordinal indexes <c>PositioningAIConstants.DISMARK_INTENSITY_SCALAR</c> (FR-DM-016).
    /// ORDINAL STABILITY: byte-backed, APPEND-only (FR-DM-013); append at the end only.
    /// </summary>
    public enum DismarkIntensity : byte
    {
        /// <summary>Identity row (FR-DM-012). Offset scalar 0.0; penalty ×1.0. #23 §3.3.</summary>
        Off = 0,

        /// <summary>Moderate evasion. Offset scalar 0.6. #23 §3.3.</summary>
        Conservative = 1,

        /// <summary>Full evasion. Offset scalar 1.0. #23 §3.3.</summary>
        Aggressive = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial implementation (#23 T0 scaffolding). |
#endregion
