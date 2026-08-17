// File:     src/shot-mechanics/ShotPlacementResolver.cs
// Created:  2026-05-27
// Modified: 2026-07-27
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
        /// Converts PlacementTarget u to a world-space aim direction whose vertical comes from the
        /// launch angle — the §3.5.6 composition: the horizontal unit vector toward the u target,
        /// tilted upward by <paramref name="launchAngleDeg"/> in the vertical plane of the shot.
        /// The placement v does NOT drive the vertical (§3.5.6 discards it; the launch-angle model
        /// §3.3 owns elevation) — ERR-006-002 / shot-outcome design KD-2.
        /// Shot Mechanics #6 §3.5.6.
        /// </summary>
        /// <param name="placementTarget">Goal-relative normalised target [0,1]²; only u is consumed.</param>
        /// <param name="shooterPosition">Agent world-space position at INITIATING.</param>
        /// <param name="launchAngleDeg">§3.3 launch angle (degrees above horizontal).</param>
        /// <returns>Unit vector toward the u target at the launch elevation (Z-up).</returns>
        public static Vector3 ComputeAimDirectionWithLaunchAngle(
            Vector2 placementTarget,
            Vector3 shooterPosition,
            float launchAngleDeg)
        {
            GoalGeometry goal = GoalGeometryProvider.Get();

            float targetY = goal.LeftPostY + placementTarget.x * goal.GoalWidth;
            var   horizontal = new Vector2(goal.GoalLineX - shooterPosition.x,
                                           targetY        - shooterPosition.y);
            float dist = horizontal.magnitude;

            if (dist < ShotMechanicsConstants.AimDirectionEpsilon)
            {
                Debug.LogWarning("[ShotMechanics] §3.5.6: shooter is at the goal line — using forward direction.");
                return Vector3.forward;
            }

            Vector2 hu  = horizontal / dist;
            float   rad = launchAngleDeg * Mathf.Deg2Rad;
            float   cos = Mathf.Cos(rad);
            // Unit by construction: (hu·cos)² summed with sin² = cos² + sin² = 1.
            return new Vector3(hu.x * cos, hu.y * cos, Mathf.Sin(rad));
        }

        /// <summary>
        /// Applies an angular error offset and returns the updated aim direction. The offset is the
        /// tangent-plane form <c>errorDirection × tan(errorMagDeg·Deg2Rad)</c> (dimensionless,
        /// per metre of range), so the displacement at the goal plane is
        /// <c>tan(err) × distance</c> — a true error CONE that widens with range, not a fixed
        /// goal-fraction (ERR-006-003 / shot-outcome design KD-3).
        /// Shot Mechanics #6 §3.6.8/§3.6.9.
        /// </summary>
        /// <param name="baseAimDirection">Pre-error world-space aim direction (unit vector).</param>
        /// <param name="errorOffset">Angular error as tangent offsets (x → lateral, y → vertical).</param>
        /// <param name="shooterPosition">Agent world-space position.</param>
        /// <returns>Error-adjusted aim direction unit vector.</returns>
        public static Vector3 ApplyErrorOffset(
            Vector3 baseAimDirection,
            Vector2 errorOffset,
            Vector3 shooterPosition)
        {
            GoalGeometry goal = GoalGeometryProvider.Get();

            // Recover the intended goal-plane target from the base aim direction and reproject with error
            float dist = Mathf.Max(goal.GoalLineX - shooterPosition.x, ShotMechanicsConstants.GoalLineDistanceFloor);

            // Compute approximate target point from base direction
            Vector3 baseTarget = shooterPosition + baseAimDirection * (dist / Mathf.Max(baseAimDirection.x, ShotMechanicsConstants.AimDirectionComponentEpsilon));

            // Apply error in metres at the goal plane: tangent offset × range (KD-3).
            // The vertical clamp preserves the launch model's own intent: under the §3.5.6
            // composition baseTarget.z = tan(launch)·dist legitimately exceeds 1.5 × GoalHeight for
            // lofted strikes, and clamping the BASE would silently flatten the launch model — the
            // clamp bounds what ERROR can add, never what the launch intended (KD-3).
            float newTargetY = Mathf.Clamp(baseTarget.y + errorOffset.x * dist,
                                           goal.LeftPostY  - goal.GoalWidth  * ShotMechanicsConstants.PlacementErrorHClampFraction,
                                           goal.RightPostY + goal.GoalWidth  * ShotMechanicsConstants.PlacementErrorHClampFraction);
            float newTargetZ = Mathf.Clamp(baseTarget.z + errorOffset.y * dist,
                                           0.0f,
                                           Mathf.Max(baseTarget.z,
                                                     goal.GoalHeight * ShotMechanicsConstants.PlacementErrorVClampFraction));

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
// | 1.5     | 2026-07-27 | —      | Shot-outcome pass (ERR-006-002/003, design KD-2/KD-3). New                 |
// |         |            |        |   ComputeAimDirectionWithLaunchAngle — the §3.5.6 composition (vertical    |
// |         |            |        |   from launch angle, not from v). ApplyErrorOffset now applies the error   |
// |         |            |        |   as tan(err)×distance at the goal plane (a true cone) instead of a fixed  |
// |         |            |        |   goal-fraction, and the vertical clamp preserves a base target above      |
// |         |            |        |   1.5×GoalHeight (bounds error, never the launch model's own intent).      |
#endregion
