// File:     src/ball-physics/BodyPart.cs
// Created:  2026-06-03
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Body-part enum used by deflection coefficient lookup. Consumed by
//           collision-system's ball-contact routing (BodyPart.Torso default).

namespace TacticalDirector.BallPhysics
{
    /// <summary>Body parts used for deflection coefficient lookup.</summary>
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
#endregion
