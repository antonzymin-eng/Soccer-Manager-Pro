// File:     src/collision-system/AgentBallCollisionData.cs
// Created:  2026-05-25
// Modified: 2026-07-27 (shot-outcome pass)
// Author:   —
// Spec:     Collision System #3 §3.3.4, §4.2.4, FR-03, Code Standards #20
// Purpose:  Data passed to Ball Physics when agent-ball contact is detected.

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Interface contract between Collision System and Ball Physics (Spec #1).
    /// Ball Physics uses this to compute deflection. Collision System #3 §4.2.4.
    /// </summary>
    public struct AgentBallCollisionData
    {
        /// <summary>Contact point in world 3-D coordinates (m).</summary>
        public Vector3 ContactPoint;

        /// <summary>Agent world-space position at moment of contact (m). The deflection normal is
        /// derived from it (Ball Physics models the body as a vertical cylinder — shot-outcome
        /// design KD-6); Z is ignored by the consumer.</summary>
        public Vector3 AgentPosition;

        /// <summary>Agent velocity at moment of contact (m/s). Used for momentum transfer.</summary>
        public Vector3 AgentVelocity;

        /// <summary>
        /// Body part that contacted the ball. Ball Physics #1 §3.1.10.1.
        /// Stage 0: always BodyPart.Torso. Stage 1+: height-based detection.
        /// </summary>
        public BodyPart BodyPart;

        /// <summary>Agent index (0–21) for possession and statistics.</summary>
        public int AgentID;

        /// <summary>Team ID for possession tracking.</summary>
        public int TeamID;

        /// <summary>
        /// True if agent is the designated goalkeeper.
        /// Stage 0: flag set but not acted upon. Stage 1+: Goalkeeper Mechanics #11.
        /// </summary>
        public bool IsGoalkeeper;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
// | 1.1     | 2026-07-27 | —      | Shot-outcome design KD-6: + AgentPosition (deflection-normal input;         |
// |         |            |        | populated by CollisionSystem.ProcessAgentBall from the snapshot).           |
#endregion
