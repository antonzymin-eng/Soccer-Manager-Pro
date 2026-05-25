// File:     src/collision-system/CollisionManifold.cs
// Created:  2026-05-25
// Modified: 2026-05-25
// Author:   —
// Spec:     Collision System #3 §3.2.2, §4.2.1, Code Standards #20
// Purpose:  Geometry of a detected collision. Produced by narrow phase; consumed by response.

using UnityEngine;

namespace TacticalDirector.CollisionSystem
{
    /// <summary>
    /// Describes the geometric relationship between two colliding entities.
    /// Valid for one frame only. Collision System #3 §4.2.1.
    /// ContactPoint is Vector2 (XY plane); CollisionEvent uses Vector3 (Z = 0 at Stage 0).
    /// </summary>
    public struct CollisionManifold
    {
        /// <summary>
        /// Unit collision normal pointing from Entity1 toward Entity2.
        /// Impulse direction for collision response.
        /// </summary>
        public Vector2 Normal;

        /// <summary>
        /// Contact point in world XY coordinates.
        /// Weighted by entity radii: t = r1 / (r1 + r2) along the centre-to-centre line.
        /// </summary>
        public Vector2 ContactPoint;

        /// <summary>
        /// Penetration depth (m). Always positive. Used for MTV separation.</summary>
        public float PenetrationDepth;

        /// <summary>First entity ID (lower ID by convention; set by caller).</summary>
        public int Entity1ID;

        /// <summary>Second entity ID (higher ID by convention; set by caller).</summary>
        public int Entity2ID;
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes          |
// | 1.0     | 2026-05-25 | —      | Initial draft. |
#endregion
