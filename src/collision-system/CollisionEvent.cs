// File:     src/collision-system/CollisionEvent.cs
// Created:  2026-05-25
// Modified: 2026-09-11  [v1.4]
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

        /// <summary>
        /// First entity ID (lower ID by convention; ball = SpatialHashConstants.BALL_ENTITY_ID = -1
        /// sorts as lowest). Producer (CollisionSystem.RecordEvent) sorts the pair so consumers can
        /// rely on Entity1ID &lt;= Entity2ID.
        /// </summary>
        public int Entity1ID;

        /// <summary>Second entity ID (higher ID by convention; sorting enforced by producer).</summary>
        public int Entity2ID;

        /// <summary>
        /// Contact point in world 3-D coordinates.
        /// AGENT_AGENT: Z = 0 at Stage 0 (agents are ground-level). AGENT_BALL: Z = ball
        /// height at contact (up to CollisionPhysicsConstants.AgentReachHeight) — set by
        /// CollisionDetection.CheckAgentBallCollision. Stage 1+: agent Z from aerial geometry.
        /// </summary>
        public Vector3 ContactPoint;

        /// <summary>
        /// Impact force (N). F = impulse / CollisionPhysicsConstants.ContactDurationS (ERR-003-001).
        /// Zero for AGENT_BALL collisions (ball mass not relevant here).
        /// </summary>
        public float ImpactForce;

        /// <summary>
        /// W4 new-threat signal for AGENT_BALL events. True only when Ball Physics actually applied
        /// an agent deflection and changed the live flight; false for a mere overlap, controlled-ball
        /// contact, slow first-touch territory, separating contact, and every AGENT_AGENT event.
        /// This is event-local observation state and is not serialized independently.
        /// </summary>
        public bool BallDeflected;

        /// <summary>
        /// Foul detection data. Populated only for AGENT_AGENT collisions.
        /// Default (zeroed) for AGENT_BALL.
        /// </summary>
        public ContactForceData FoulData;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                       |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                              |
// | 1.1     | 2026-06-05 | —      | AR-1 L-3. Entity1ID / Entity2ID XML doc clarified — producer now enforces   |
// |         |            |        | Entity1ID <= Entity2ID via sort in CollisionSystem.RecordEvent.             |
// | 1.2     | 2026-06-10 | —      | AR-7 H-1 follow-through. ImpactForce doc: F = impulse × 60 Hz → F = impulse |
// |         |            |        | / ContactDurationS (ERR-003-001).                                           |
// | 1.3     | 2026-06-10 | —      | AR-9 L-2. ContactPoint doc corrected — "Stage 0: Z = 0" was wrong for       |
// |         |            |        | AGENT_BALL events, which carry the ball's contact height (≤ 2.0 m).         |
// | 1.4     | 2026-09-11 | —      | W4: BallDeflected carries applied-response/new-threat truth to consumers.   |
#endregion
