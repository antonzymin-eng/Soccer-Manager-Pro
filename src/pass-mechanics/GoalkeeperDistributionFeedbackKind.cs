// File:     src/pass-mechanics/GoalkeeperDistributionFeedbackKind.cs
// Created:  2026-09-26
// Author:   —
// Spec:     Pass Mechanics #5 §2.4.4, §3.8.13; Code Standards #20
// Purpose:  Typed terminal feedback from dedicated goalkeeper distribution.

namespace TacticalDirector.PassMechanics
{
    /// <summary>Terminal outcome of the dedicated goalkeeper-distribution executor path.</summary>
    public enum GoalkeeperDistributionFeedbackKind : byte
    {
        /// <summary>Request was not accepted; no executor lifecycle started.</summary>
        Rejected = 0,
        /// <summary>Accepted request ended before a CONTACT kick.</summary>
        Cancelled = 1,
        /// <summary>CONTACT applied the kick successfully.</summary>
        Completed = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes |
// | 1.0     | 2026-09-26 | —      | W8 B terminal feedback enum; serialized ordinals are append-only. |
#endregion
