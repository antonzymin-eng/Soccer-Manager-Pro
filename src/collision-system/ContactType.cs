// File:     src/collision-system/ContactType.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     Collision System #3 §3.3.5, §4.2.5, Code Standards #20
// Purpose:  Physical contact classification for foul detection groundwork.

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Physical contact classification used by the Referee System (Stage 1+).
    /// Collision System #3 §3.3.5.
    /// </summary>
    public enum ContactType
    {
        /// <summary>Parallel-approach shoulder contact. Typically legal. §3.3.6.</summary>
        SHOULDER_TO_SHOULDER = 0,

        /// <summary>Instigator approached from behind victim. Higher foul likelihood. §3.3.6.</summary>
        FROM_BEHIND = 1,

        /// <summary>Side approach; context-dependent. §3.3.6.</summary>
        SIDE_IMPACT = 2,

        /// <summary>Slide tackle contact (Stage 1+). Requires animation state.</summary>
        SLIDE_TACKLE = 3,

        /// <summary>Aerial challenge contact (Stage 1+). Requires jump state.</summary>
        AERIAL_CHALLENGE = 4
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
#endregion
