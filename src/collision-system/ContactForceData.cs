// File:     src/collision-system/ContactForceData.cs
// Created:  2026-05-25
// Modified: 2026-06-10  [v1.1]
// Author:   —
// Spec:     Collision System #3 §3.3.5, §4.2.5, FR-06, Code Standards #20
// Purpose:  Raw contact force data for Referee System (Stage 1+). Populated but not consumed at Stage 0.

using UnityEngine;

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Raw contact force data embedded in CollisionEvent for Referee System consumption (Stage 1+).
    /// Stage 0: struct is populated but not acted upon. Collision System #3 §4.2.5.
    /// </summary>
    public struct ContactForceData
    {
        /// <summary>Force magnitude (N). F = impulse / ContactDurationS (ERR-003-001). §4.2.5.</summary>
        public float ForceMagnitude;

        /// <summary>Normalised force direction (instigator → victim). §4.2.5.</summary>
        public Vector3 ForceDirection;

        /// <summary>Contact classification for foul adjudication. §4.2.5.</summary>
        public ContactType Type;

        /// <summary>Agent who moved toward the other (higher approach velocity). §4.2.5.</summary>
        public int InstigatorAgentID;

        /// <summary>Agent who received contact. §4.2.5.</summary>
        public int VictimAgentID;

        /// <summary>
        /// True if victim held the ball at contact time.
        /// Stage 0: always false (no possession integration yet). Stage 1: First Touch #4.
        /// </summary>
        public bool VictimHasBall;

        /// <summary>
        /// True if instigator was attempting to play the ball.
        /// Stage 0: always false (no tactical intent yet). Stage 1: Decision Tree #8.
        /// </summary>
        public bool InstigatorPlayingBall;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                              |
// | 1.1     | 2026-06-10 | —      | AR-7 H-1 follow-through: ForceMagnitude doc force-conversion formula        |
// |         |            |        | updated (ERR-003-001). ForceDirection is now genuinely instigator→victim   |
// |         |            |        | after the CollisionSystem v1.6 call-site flip (ERR-003-002).               |
#endregion
