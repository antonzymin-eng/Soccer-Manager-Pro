// File:     src/ball-physics/RestartType.cs
// Created:  2026-06-03
// Modified: 2026-06-03 (AR-4 fix pass)
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Restart-classification enum returned by BallCollision.CheckBoundaries.

namespace TacticalDirector.BallPhysics
{
    /// <summary>
    /// Restart types awarded after the ball leaves the field of play.
    /// ORDINAL STABILITY: future additions MUST be appended at the end of the enum.
    /// RestartType is returned from <c>BallCollision.CheckBoundaries</c> and is
    /// consumed by Stage 1+ restart logs / replay streams; inserting a new restart
    /// in the middle shifts the ordinals of every later member and breaks any
    /// int-keyed deserialiser.
    /// </summary>
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
// | 1.1     | 2026-06-03 | —      | AR-4 L-1: RestartType XML doc gains an explicit ORDINAL STABILITY |
// |         |            |        | paragraph parallel to BallEventType / BallStateType / SurfaceType. |
#endregion
