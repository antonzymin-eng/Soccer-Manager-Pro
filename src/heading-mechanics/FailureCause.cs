// File:     src/heading-mechanics/FailureCause.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Heading Mechanics #10 §2.2, FR-HE-028, KD-12, Code Standards #20
// Purpose:  Cause field for HeaderAttemptFailedEvent; covers all four failure categories.

namespace TacticalDirector.HeadingMechanics
{
    /// <summary>
    /// Cause of a failed header attempt. Carried by HeaderAttemptFailedEvent.FailureCause.
    /// FR-HE-028: exactly these four values. Heading Mechanics #10 §2.2 / §3.9.
    /// </summary>
    public enum FailureCause
    {
        /// <summary>Jump committed too early; ball had not yet reached the contact volume. §2.3 F-01.</summary>
        MistimedEarly,

        /// <summary>Jump committed too late; ball had already cleared the contact volume. §2.3 F-01.</summary>
        MistimedLate,

        /// <summary>Agent jump apex was below ball altitude, or ball never entered contact volume. §2.3 F-02/F-03.</summary>
        PositionedPoorly,

        /// <summary>Duel loser whose contact quality fell below MIN_CONTACT_QUALITY after disturbance. §2.3 F-04 / FR-HE-026.</summary>
        DisturbedInDuel
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
