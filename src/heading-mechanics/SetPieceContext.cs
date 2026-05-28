// File:     src/heading-mechanics/SetPieceContext.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §2.2, KD-13, Code Standards #20
// Purpose:  Telemetry-only label for the delivery type; never consumed by physics formulas.

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Telemetry-only delivery-type label for HeaderExecutedEvent.SetPieceContext.
    /// Physics is uniform across all delivery types per KD-13 / FR-HE-016.
    /// Never consumed by §3.5–§3.7 physics formulas.
    /// Heading Mechanics #10 §2.2.
    /// </summary>
    public enum SetPieceContext
    {
        /// <summary>Open-play delivery (not a dead-ball set piece).</summary>
        OpenPlay,

        /// <summary>Corner-kick delivery.</summary>
        Corner,

        /// <summary>Free-kick delivery.</summary>
        FreeKick
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
