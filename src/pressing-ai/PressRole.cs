// File:     src/pressing-ai/PressRole.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Pressing AI #13 §3.3–§3.6, Code Standards #20
// Purpose:  Per-agent press role enum. Roles are disjoint per tick (FR-PR-014).

namespace TacticalDirector.PressingAI
{
    /// <summary>
    /// Per-agent press role assigned each 10 Hz tick.
    /// Roles are disjoint: each active agent receives exactly one role. FR-PR-014.
    /// Pressing AI #13 §3.3–§3.6.
    /// </summary>
    public enum PressRole : byte
    {
        /// <summary>Agent holds its Positioning AI formation slot. Default when no press is active.</summary>
        HoldShape = 0,

        /// <summary>Agent closes down the ball-carrier directly. §3.3.</summary>
        PrimaryPress = 1,

        /// <summary>Agent occupies a shadow lane to deny a pass to a dangerous receiver. §3.4–§3.5.</summary>
        CoverShadow = 2,
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
