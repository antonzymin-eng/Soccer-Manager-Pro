// File: src/positioning-ai/LineId.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec: #12 Positioning AI §3.3
// Purpose: Three-class longitudinal line partition enum.

namespace TacticalDirector.PositioningAI
{
    /// <summary>Three-class longitudinal line assignment resolved from k=3 formation partition.</summary>
    public enum LineId : byte
    {
        Defense  = 0,
        Midfield = 1,
        Attack   = 2
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
