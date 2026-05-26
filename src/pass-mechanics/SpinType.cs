// File:     src/pass-mechanics/SpinType.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §3.4.5, Code Standards #20
// Purpose:  SpinType enum: dominant spin axis classification used as metadata in
//           PhysicalProfile. Actual spin vectors computed by PassVelocityCalculator §3.4.5.

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Dominant spin direction classification. Metadata field in PhysicalProfile;
    /// spin vectors are computed by PassVelocityCalculator §3.4.5.
    /// </summary>
    internal enum SpinType
    {
        /// <summary>Forward rolling spin — Ground, Driven, ThroughBall, Lofted, AerialThrough.</summary>
        Topspin,

        /// <summary>Reverse rolling spin — Chip. Checks ball on landing.</summary>
        Backspin,

        /// <summary>Horizontal axis spin causing inswing/outswing — Cross Flat, Cross Whipped.</summary>
        Sidespin,

        /// <summary>Combined topspin + sidespin — Cross High.</summary>
        Mixed
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                    |
// | 1.0     | 2026-05-26 | —      | Extracted from PhysicalProfile.cs per one-type-per-file rule (L5).      |
// |         |            |        | M5: Mixed doc corrected from backspin+sidespin to topspin+sidespin.     |
#endregion
