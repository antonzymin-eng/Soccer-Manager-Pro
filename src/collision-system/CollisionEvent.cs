// File:     src/collision-system/CollisionEvent.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     Collision System #3 §3.4.2, §4.2.3, FR-07, Code Standards #20
// Purpose:  Per-collision event record published to the Event System each frame.

using UnityEngine;

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Immutable record of a single collision for replay, statistics, and foul detection.
    /// One instance per detected collision pair per frame. Collision System #3 §4.2.3.
    /// Maximum 50 events per frame (SpatialHashConstants.MaxCollisionPairs).
    /// </summary>
    public struct CollisionEvent
    {
        /// <summary>Match time when collision occurred (s from kickoff). Frame-accurate (1/60 s).</summary>
        public float MatchTime;

        /// <summary>Collision type. §4.2.3.</summary>
        public CollisionType Type;

        /// <summary>First entity ID (lower ID by convention; -1 = ball).</summary>
        public int Entity1ID;

        /// <summary>Second entity ID (higher ID by convention; -1 = ball).</summary>
        public int Entity2ID;

        /// <summary>
        /// Contact point in world 3-D coordinates.
        /// Stage 0: Z = 0 (all ground-level). Stage 1+: Z from aerial geometry.
        /// </summary>
        public Vector3 ContactPoint;

        /// <summary>
        /// Impact force (N). F = impulse × 60 Hz.
        /// Zero for AGENT_BALL collisions (ball mass not relevant here).
        /// </summary>
        public float ImpactForce;

        /// <summary>
        /// Foul detection data. Populated only for AGENT_AGENT collisions.
        /// Default (zeroed) for AGENT_BALL.
        /// </summary>
        public ContactForceData FoulData;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
#endregion
