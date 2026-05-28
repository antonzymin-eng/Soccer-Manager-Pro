// File:     src/goalkeeper-mechanics/DeliveryKind.cs
// Created:  2026-05-28
// Modified: 2026-05-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.8, KD-1, KD-16, Code Standards #20
// Purpose:  Kinematic-profile lookup label for GK distribution in DistributeIntent.
//           KD-1 carve-out: selects release height / windup / accuracy constant row
//           from the distribution table — not a physics input that gates formulas.

namespace TacticalDirector.GoalkeeperMechanics
{
    /// <summary>
    /// GK distribution delivery kind. KD-1 carve-out: used to look up the correct
    /// release-height, windup-duration, and accuracy-coeff constants from §3.8 tables.
    /// Does NOT gate any physics formula directly (CLAUDE.md / KD-1).
    /// Goalkeeper Mechanics #11 §3.8 / KD-16.
    /// </summary>
    public enum DeliveryKind
    {
        /// <summary>Overarm throw; THROW_RELEASE_HEIGHT_M / THROW_WINDUP_MS / THROW_ACCURACY_COEFF. §3.8.</summary>
        Throw,

        /// <summary>Rolling the ball along the ground; ROLL_RELEASE_HEIGHT_M / ROLL_WINDUP_MS / accuracyCoeff = 1.0. §3.8.</summary>
        Roll,

        /// <summary>Goalkeeper kick (punt or volley); KICK_RELEASE_HEIGHT_M / KICK_WINDUP_MS / KICK_ACCURACY_COEFF. §3.8.</summary>
        Kick
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-28 | —      | Initial implementation. |
#endregion
