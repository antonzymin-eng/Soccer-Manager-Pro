// File:     src/collision-system/CollisionDetection.cs
// Created:  2026-05-25
// Modified: 2026-06-05  [v1.1]
// Author:   —
// Spec:     Collision System #3 §3.2.1, §3.2.2, FR-02, FR-03, Code Standards #20
// Purpose:  Pure geometric narrow-phase tests. Static, side-effect free.

using UnityEngine;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Narrow-phase collision detection. All methods are static and pure.
    /// Collision System #3 §3.2.1.
    /// </summary>
    public static class CollisionDetection
    {
        /// <summary>
        /// Circle-circle intersection in XY plane (Z ignored). FR-02.
        /// ~20 float ops + 1 sqrt on hit only.
        /// </summary>
        /// <param name="a1">First agent snapshot.</param>
        /// <param name="a2">Second agent snapshot.</param>
        /// <param name="manifold">Output manifold — valid only when method returns true.</param>
        /// <returns>True when the hitboxes overlap.</returns>
        public static bool CheckAgentAgentCollision(
            in AgentPhysicalProperties a1,
            in AgentPhysicalProperties a2,
            out CollisionManifold manifold)
        {
            float dx = a2.Position.x - a1.Position.x;
            float dy = a2.Position.y - a1.Position.y;
            float distSq = dx * dx + dy * dy;
            float combinedR = a1.HitboxRadius + a2.HitboxRadius;

            if (distSq >= combinedR * combinedR)
            {
                manifold = default;
                return false;
            }

            float dist = Mathf.Sqrt(distSq);
            float penetration = combinedR - dist;

            Vector2 normal;
            if (dist > SpatialHashConstants.MIN_DISTANCE_EPSILON)
            {
                float inv = 1.0f / dist;
                normal = new Vector2(dx * inv, dy * inv);
            }
            else
            {
                normal = Vector2.right;
                penetration = combinedR;
            }

            float t = a1.HitboxRadius / combinedR;
            var contact = new Vector2(a1.Position.x + dx * t, a1.Position.y + dy * t);

            manifold = new CollisionManifold
            {
                Normal = normal,
                ContactPoint = contact,
                PenetrationDepth = penetration
            };

            return true;
        }

        /// <summary>
        /// Circle-sphere intersection in XY plane with Z height filter. FR-03.
        /// Ball must be within CollisionPhysicsConstants.AgentReachHeight to be reachable.
        /// ~15 float ops + 1 sqrt on hit only.
        /// </summary>
        /// <param name="agent">Agent snapshot.</param>
        /// <param name="ball">Ball state.</param>
        /// <param name="contactPoint">Output 3-D contact point — valid only when method returns true.</param>
        /// <returns>True when agent hitbox and ball overlap within reach height.</returns>
        public static bool CheckAgentBallCollision(
            in AgentPhysicalProperties agent,
            in BallState ball,
            out Vector3 contactPoint)
        {
            if (ball.Position.z > CollisionPhysicsConstants.AgentReachHeight)
            {
                contactPoint = default;
                return false;
            }

            float dx = ball.Position.x - agent.Position.x;
            float dy = ball.Position.y - agent.Position.y;
            float distSq = dx * dx + dy * dy;
            float combinedR = agent.HitboxRadius + CollisionPhysicsConstants.BallRadius;

            if (distSq >= combinedR * combinedR)
            {
                contactPoint = default;
                return false;
            }

            float dist = Mathf.Sqrt(distSq);

            if (dist > SpatialHashConstants.MIN_DISTANCE_EPSILON)
            {
                float inv = 1.0f / dist;
                contactPoint = new Vector3(
                    agent.Position.x + dx * inv * agent.HitboxRadius,
                    agent.Position.y + dy * inv * agent.HitboxRadius,
                    ball.Position.z);
            }
            else
            {
                contactPoint = new Vector3(
                    agent.Position.x + agent.HitboxRadius,
                    agent.Position.y,
                    ball.Position.z);
            }

            return true;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                          |
// | 1.0     | 2026-05-25 | —      | Initial draft.                                                                 |
// | 1.1     | 2026-06-05 | —      | AR-3 L-2 follow-through. CheckAgentAgentCollision no longer initialises        |
// |         |            |        | manifold.Entity1ID/Entity2ID (fields removed from CollisionManifold).          |
#endregion
