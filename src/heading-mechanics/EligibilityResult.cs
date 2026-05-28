// File:     src/heading-mechanics/EligibilityResult.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §3.2, Code Standards #20
// Purpose:  Return type of HeadingEligibility.Evaluate; carries all four outputs of the pure predicate.

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Return type of HeadingEligibility.Evaluate.
    /// The predicate is pure (no event side effects — §3.2 v0.2 M-2); the §4.6 caller
    /// inspects MistimedDirection and emits the failed event when IsEligible = false.
    /// Heading Mechanics #10 §3.2.
    /// </summary>
    public struct EligibilityResult
    {
        /// <summary>True when all eligibility conditions are satisfied and the contact should proceed.</summary>
        public bool IsEligible;

        /// <summary>
        /// 60 Hz frame index at which the ball is predicted to enter HEAD_CONTACT_VOLUME.
        /// -1 when the ball never intersects the contact volume during the attempt window.
        /// </summary>
        public int PredictedContactFrame;

        /// <summary>Apex-aligned ideal contact frame against which timingOffsetMs is measured in §3.4.</summary>
        public int IdealContactFrame;

        /// <summary>
        /// Encodes the timing drift direction when IsEligible = false due to intent staleness.
        /// None when IsEligible = true or failure is spatial (PositionedPoorly), not temporal.
        /// </summary>
        public MistimedDirection MistimedDirection;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
