// File:     src/goalkeeper-mechanics/FailureCause.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §2.2.4, §2.3 F-01–F-04, Code Standards #20
// Purpose:  Enumeration of failure causes emitted on SaveAttemptedEvent when the GK
//           misses or is otherwise unable to complete a save attempt.

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// Enumeration of reasons a save attempt failed or produced no effective contact.
    /// Published on SaveAttemptedEvent.FailureCause when handlingQualityScalar &lt; MIN_HANDLING_QUALITY
    /// or the GK was unable to complete the save. §2.3 F-01–F-04.
    /// Goalkeeper Mechanics #11 §2.2.4.
    /// </summary>
    public enum FailureCause
    {
        /// <summary>Contact attempted but hand never reached ball sphere. §2.3 F-01.</summary>
        MissedContact,

        /// <summary>Dive launched too early or too late; reactionWindowAchieved too low. §2.3 F-03.</summary>
        MistimedDive,

        /// <summary>Dive launched but ball travelled in the opposite direction. §2.3 F-02.</summary>
        WrongDirection,

        /// <summary>Ball was inside save volume but outside reach envelope at contact frame. §2.3 F-04.</summary>
        OutOfReach,

        /// <summary>Duel loser GK was disturbed by a competing agent claim. §3.6 / §3.5.</summary>
        DisturbedInDuel
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
