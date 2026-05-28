// File:     src/shot-mechanics/ShotPlacementResolver.cs
// Created:  2026-05-27
// Modified: 2026-05-28
// Author:   —
// Spec:     Shot Mechanics #6 §3.5, Code Standards #20
// Purpose:  Translates goal-relative PlacementTarget (u, v) into a world-space aim
//           direction unit vector. Reads goal geometry via GoalGeometryProvider.Get() only —
//           never directly from ShotMechanicsConstants (§4.1.1 mandate). Pure static.

using UnityEngine;

namespace TacticalDirector.ShotMechanics
{
    /// <summary>
    /// Resolves goal-relative placement target (u, v) to world-space aim direction unit vector.
    /// Reads goal geometry exclusively via GoalGeometryProvider.Get(). §3.5, §4.1.1.
    /// Shot Mechanics #6 §3.5.
    /// </summary>
    public static class ShotPlacementResolver
    {
        /// <summary>
        /// Converts PlacementTarget (u, v) to a world-space aim direction (unit vector).
        /// u ∈ [0, 1]: left post → right post. v ∈ [0, 1]: ground → crossbar.
        /// Shot Mechanics #6 §3.5.
        /// </summary>
        /// <param name="placementTarget">Goal-relative normalised target [0,1]².</param>
        /// <param name="shooterPosition">Agent world-space position at INITIATING.</param>
        /// <returns>Unit vector from shooter toward goal target, encoding horizontal aim.</returns>
        public static Vector3 ComputeAimDirection(Vector2 placementTarget, Vector3 shooterPosition)
        {
            GoalGeometry goal = GoalGeometryProvider.Get();

            // §3.5 — Bilinear interpolation: (u, v) → world-space goal mouth point
            float targetY = goal.LeftPostY + placementTarget.x * goal.GoalWidth;
            float targetZ = placementTarget.y * goal.GoalHeight;
            var   targetPoint = new Vector3(goal.GoalLineX, targetY, targetZ);

            Vector3 delta = targetPoint - shooterPosition;
            float   mag   = delta.magnitude;

            if (mag < ShotMechanicsConstants.AimDirectionEpsilon)
            {
                Debug.LogWarning("[ShotMechanics] §3.5: shooter is at the goal line — using forward direction.");
                return Vector3.forward;
            }

            return delta / mag;
        }

        /// <summary>
        /// Applies a (u, v) error offset in goal-relative space and returns the updated aim direction.
        /// Shot Mechanics #6 §3.6.8.
        /// </summary>
        /// <param name="baseAimDirection">Pre-error world-space aim direction (unit vector).</param>
        /// <param name="errorOffset">Error offset in goal-relative (u, v) space.</param>
        /// <param name="shooterPosition">Agent world-space position.</param>
        /// <returns>Error-adjusted aim direction unit vector.</returns>
        public static Vector3 ApplyErrorOffset(
            Vector3 baseAimDirection,
            Vector2 errorOffset,
            Vector3 shooterPosition)
        {
            GoalGeometry goal = GoalGeometryProvider.Get();

            // Recover approximate intended (u, v) from base aim direction and reproject with error
            float dist = Mathf.Max(goal.GoalLineX - shooterPosition.x, ShotMechanicsConstants.GoalLineDistanceFloor);

            // Compute approximate u, v from base direction
            Vector3 baseTarget = shooterPosition + baseAimDirection * (dist / Mathf.Max(baseAimDirection.x, ShotMechanicsConstants.AimDirectionComponentEpsilon));

            // Apply error offset in goal space: u → Y axis, v → Z axis
            float newTargetY = Mathf.Clamp(baseTarget.y + errorOffset.x * goal.GoalWidth,
                                           goal.LeftPostY  - goal.GoalWidth  * ShotMechanicsConstants.PlacementErrorHClampFraction,
                                           goal.RightPostY + goal.GoalWidth  * ShotMechanicsConstants.PlacementErrorHClampFraction);
            float newTargetZ = Mathf.Clamp(baseTarget.z + errorOffset.y * goal.GoalHeight,
                                           0.0f,
                                           goal.GoalHeight * ShotMechanicsConstants.PlacementErrorVClampFraction);

            var   adjustedTarget = new Vector3(goal.GoalLineX, newTargetY, newTargetZ);
            Vector3 delta = adjustedTarget - shooterPosition;
            float   mag   = delta.magnitude;

            return (mag < ShotMechanicsConstants.AimDirectionEpsilon) ? baseAimDirection : delta / mag;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-27 | —      | Initial implementation.                                       |
// | 1.1     | 2026-05-28 | —      | M-4: Z clamp lower bound -goal.GoalHeight→0.0f in               |
// |         |            |        |   ApplyErrorOffset (negative Z aim is below ground).            |
// | 1.2     | 2026-05-28 | —      | M-5: Magic literals 0.5f/1.5f in ApplyErrorOffset replaced with      |
// |         |            |        |   PlacementErrorHClampFraction/VClampFraction constants.             |
// | 1.3     | 2026-05-28 | —      | L-1: 1e-4f epsilon literals (×2) → AimDirectionEpsilon constant.      |
// | 1.4     | 2026-05-28 | —      | L-2: 0.1f/0.001f magic literals in ApplyErrorOffset → GoalLineDistanceFloor/  |
// |         |            |        |   AimDirectionComponentEpsilon constants.                                  |
#endregion
