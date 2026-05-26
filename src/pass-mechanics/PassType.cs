// File:     src/pass-mechanics/PassType.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §3.1.2, §2.4.2, Code Standards #20
// Purpose:  All pass-mechanics enum definitions: PassType, CrossSubType, PassOutcome.

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

    /// <summary>
    /// Cross delivery sub-type. Only evaluated when PassType == Cross.
    /// Defaults to Flat when not specified (KD-6). Pass Mechanics #5 §3.1.2.
    /// </summary>
    public enum CrossSubType
    {
        /// <summary>Default. Low trajectory, maximum pace, sidespin curl.</summary>
        Flat,

        /// <summary>Mid-trajectory, strong sidespin, sharp swing through air.</summary>
        Whipped,

        /// <summary>High trajectory, mixed backspin/sidespin, hanging cross for headed finish.</summary>
        High
    }

    /// <summary>
    /// Outcome of a pass execution attempt. Pass Mechanics #5 §2.4.2.
    /// </summary>
    public enum PassOutcome
    {
        /// <summary>Ball.ApplyKick() was called; ball is in flight.</summary>
        Completed,

        /// <summary>Tackle interrupt during WINDUP; ball not kicked.</summary>
        Cancelled,

        /// <summary>PassRequest failed validation (programming error in caller; logged only).</summary>
        Invalid
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                  |
// | 1.0     | 2026-05-26 | —      | Initial implementation. |
#endregion
