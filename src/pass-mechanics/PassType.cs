// File:     src/pass-mechanics/PassType.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §3.1.2, Code Standards #20
// Purpose:  PassType enum: discrete pass type classification supplied by Decision Tree #8.

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Discrete pass type classification. Pass Mechanics #5 §3.1.2.
    /// Selected by Decision Tree #8; Pass Mechanics does not choose the type (KD-2).
    /// </summary>
    public enum PassType
    {
        /// <summary>Short-to-medium range, surface-rolling. distMax 30m.</summary>
        Ground,

        /// <summary>Firm, penetrating, low-trajectory. distMax 50m.</summary>
        Driven,

        /// <summary>High arc, long diagonal, aerial phase. distMax 60m.</summary>
        Lofted,

        /// <summary>Ground-level ball into space behind defensive line. distMax 40m.</summary>
        ThroughBall,

        /// <summary>Aerial ball into space for a runner. distMax 50m.</summary>
        AerialThrough,

        /// <summary>Wide delivery into penalty area. Sub-type specified by CrossSubType. distMax 45–50m.</summary>
        Cross,

        /// <summary>Steep-arc lob over nearby defender or goalkeeper. distMax 20m.</summary>
        Chip
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                 |
// | 1.0     | 2026-05-26 | —      | Initial implementation.                                               |
// | 1.1     | 2026-05-26 | —      | H3: CrossSubType and PassOutcome moved to own files (one-type-per-file). |
#endregion
