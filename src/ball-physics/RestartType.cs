// File:     src/ball-physics/RestartType.cs
// Created:  2026-06-03
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Restart-classification enum returned by BallCollision.CheckBoundaries.

namespace TacticalDirector.BallPhysics
{
    /// <summary>Restart types awarded after the ball leaves the field of play.</summary>
    public enum RestartType
    {
        None,
        ThrowIn,
        GoalKick,
        Corner,
        KickOff
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-03 | —      | Extracted from BallCollision.cs as part of AR-2 L-2 file split    |
// |         |            |        | (one public type per file, src/CLAUDE.md FILE NAMING).             |
#endregion
