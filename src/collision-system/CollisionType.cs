// File:     src/collision-system/CollisionType.cs
// Created:  2026-05-25
// Modified: 2026-06-05  [v1.1]
// Author:   —
// Spec:     Collision System #3 §3.4.2, §4.2.3, Code Standards #20
// Purpose:  Collision type classification published in CollisionEvent.

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Collision type for CollisionEvent. Collision System #3 §4.2.3.
    /// <para>
    /// ORDINAL STABILITY: members are APPEND-only. CollisionEvent.Type is published
    /// through ICollisionEventConsumer and embedded in any persisted event stream;
    /// inserting a new member in the middle shifts all subsequent ordinals and
    /// breaks replay parity, analytics pipelines, and FR-DS-009 digest compatibility.
    /// New members MUST be added at the end with the next available ordinal.
    /// </para>
    /// </summary>
    public enum CollisionType
    {
        /// <summary>Two field players collided. Foul data populated. §4.2.3.</summary>
        AGENT_AGENT = 0,

        /// <summary>Agent contacted the ball. No foul data. §4.2.3.</summary>
        AGENT_BALL = 1,

        /// <summary>Goalkeeper-specific collision (Stage 1+). Goalkeeper Mechanics #11.</summary>
        AGENT_GOALKEEPER = 2,

        /// <summary>Aerial duel (Stage 1+). Heading Mechanics #10.</summary>
        AERIAL_DUEL = 3
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                            |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                   |
// | 1.1     | 2026-06-05 | —      | AR-5 L-1. ORDINAL STABILITY paragraph added — enum is embedded in CollisionEvent |
// |         |            |        | published via ICollisionEventConsumer; APPEND-only contract documented.          |
#endregion
