// File:     src/pass-mechanics/CrossSubType.cs
// Created:  2026-05-26
// Modified: 2026-06-08
// Author:   —
// Spec:     Pass Mechanics #5 §3.1.2, Code Standards #20
// Purpose:  CrossSubType enum: cross delivery sub-type selection.

namespace TacticalDirector.PassMechanics
{
    /// <summary>
    /// Cross delivery sub-type. Only evaluated when PassType == Cross.
    /// Defaults to Flat when not specified (KD-6). Pass Mechanics #5 §3.1.2.
    ///
    /// ORDINAL STABILITY: the enum's backing int ordinal lands in PassAttemptEvent
    /// (Tier A, ordinal 0x0C) payloads as digest input per FR-DS-009. New members
    /// MUST be APPENDED — never inserted in the middle — to preserve replay / save /
    /// analytics compatibility. Sibling enums CancelReason (PassCancelledEvent) and
    /// PassType (both events + ComputeErrorDirection hash input) share this contract.
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
// | 1.1     | 2026-06-08 | —      | AR-7 L-3: ORDINAL STABILITY paragraph added — backing int ordinal  |
// |         |            |        |     is embedded in PassAttemptEvent (Tier A 0x0C) payload and lands |
// |         |            |        |     in the FR-DS-009 digest. APPEND-only rule applies. Parallel to  |
// |         |            |        |     CancelReason v1.4 and PassType v1.2.                            |
#endregion
