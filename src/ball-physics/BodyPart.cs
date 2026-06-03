// File:     src/ball-physics/BodyPart.cs
// Created:  2026-06-03
// Modified: 2026-06-03 (AR-6 fix pass)
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Body-part enum used by deflection coefficient lookup. Consumed by
//           collision-system's ball-contact routing (BodyPart.Torso default).

namespace TacticalDirector.BallPhysics
{
    /// <summary>
    /// Body parts used for deflection coefficient lookup.
    /// ORDINAL STABILITY: future additions MUST be appended at the end of the enum.
    /// BodyPart is consumed cross-spec by <c>AgentBallCollisionData.BodyPart</c>
    /// (collision-system) and is the lookup key in <see cref="BodyPartCoefficients"/>;
    /// inserting a new member in the middle shifts the ordinals of every later
    /// member and breaks both the cross-spec embedded field and any persisted
    /// collision log that keys off the int value. Atomic extension MUST update
    /// <c>BallPhysicsConstants.BodyPartRetention</c>, the
    /// <see cref="BodyPartCoefficients"/> dictionary, and any cross-spec consumer
    /// in the same change.
    /// </summary>
    public enum BodyPart
    {
        Foot,
        Shin,
        Thigh,
        Torso,
        Head,
        Arm
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-03 | —      | Extracted from BallCollision.cs as part of AR-2 L-2 file split    |
// |         |            |        | (one public type per file, src/CLAUDE.md FILE NAMING).             |
// | 1.0.1   | 2026-06-03 | —      | AR-5 M-1: file header Modified field added (FR-CS-056).            |
// | 1.1     | 2026-06-03 | —      | AR-6 L-1: BodyPart XML doc gains the ORDINAL STABILITY paragraph  |
// |         |            |        | completing the full-surface sweep started by AR-3 L-2 (the other   |
// |         |            |        | five public enums got it across AR-4 L-1 and AR-5 L-1; BodyPart    |
// |         |            |        | slipped through because AR-5 L-1 named only the four enums it     |
// |         |            |        | propagated TO). BodyPart is cross-spec embedded in collision-      |
// |         |            |        | system's AgentBallCollisionData and shares the insertion-shifts-   |
// |         |            |        | ordinals hazard.                                                   |
#endregion
