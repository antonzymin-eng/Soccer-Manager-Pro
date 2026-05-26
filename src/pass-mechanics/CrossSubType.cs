// File:     src/pass-mechanics/CrossSubType.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §3.1.2, Code Standards #20
// Purpose:  CrossSubType enum: cross delivery sub-type selection.

namespace TacticalDirector.PassMechanics
{
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

        /// <summary>High trajectory, mixed topspin/sidespin, hanging cross for headed finish.</summary>
        High
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-26 | —      | Extracted from PassType.cs per one-type-per-file rule (H3).        |
// |         |            |        | M5: High doc corrected from backspin/sidespin to topspin/sidespin. |
#endregion
