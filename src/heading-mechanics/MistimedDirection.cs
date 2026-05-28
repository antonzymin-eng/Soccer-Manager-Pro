// File:     src/heading-mechanics/MistimedDirection.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §3.2, KD-17, Code Standards #20
// Purpose:  Cause direction returned by EligibilityPredicate when isEligible = false.

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Encodes why the eligibility predicate returned isEligible = false due to timing.
    /// Heading Mechanics #10 §3.2. The §4.6 caller inspects this and emits the failed event.
    /// </summary>
    public enum MistimedDirection
    {
        /// <summary>Attempt is eligible, or failed for a non-timing reason (e.g. PositionedPoorly).</summary>
        None,

        /// <summary>predictedContactFrame arrived too early (ball passed before the apex window).</summary>
        Early,

        /// <summary>predictedContactFrame arrived too late (ball has passed through the head zone).</summary>
        Late
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
