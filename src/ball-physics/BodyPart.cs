// File:     src/ball-physics/BodyPart.cs
// Created:  2026-06-03
// Modified: 2026-06-03 (AR-5 fix pass)
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
// | 1.0.1   | 2026-06-03 | —      | AR-5 M-1: file header Modified field added (FR-CS-056).            |
#endregion
