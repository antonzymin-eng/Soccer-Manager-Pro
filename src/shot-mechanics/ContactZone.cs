// File:     src/shot-mechanics/ContactZone.cs
// Created:  2026-05-27
// Modified: 2026-05-27
// Author:   —
// Spec:     Shot Mechanics #6 §2.4.1, Code Standards #20
// Purpose:  Enum describing where on the ball the foot contacts.
//           Drives BaseAngle (§3.3), SpinBase (§3.4), ContactZone modifier (§3.2).

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Physical contact zone on the ball surface. Shot Mechanics #6 §2.4.1.
    /// Selected by Decision Tree (#8). Shot Mechanics does not choose this value.
    /// </summary>
    public enum ContactZone
    {
        /// <summary>Foot strikes centre-mass of ball. Low driven trajectory, high topspin base.</summary>
        Centre,

        /// <summary>Foot strikes below ball centre. Lifted trajectory, high backspin base. Chip or loft.</summary>
        BelowCentre,

        /// <summary>Foot strikes off-centre. Curling trajectory, high sidespin base.</summary>
        OffCentre
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation. |
#endregion
