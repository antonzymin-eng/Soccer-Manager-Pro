// File:     src/pass-mechanics/PassOutcome.cs
// Created:  2026-05-26
// Modified: 2026-05-26
// Author:   —
// Spec:     Pass Mechanics #5 §2.4.2, Code Standards #20
// Purpose:  PassOutcome enum: result classification of a pass execution attempt.

namespace TacticalDirector.PassMechanics
{
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
// | Version | Date       | Author | Notes                                                       |
// | 1.0     | 2026-05-26 | —      | Extracted from PassType.cs per one-type-per-file rule (H3). |
#endregion
