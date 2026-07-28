// File:     src/shot-mechanics/Tests/ShotPlacementResolverShotOutcomeTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Shot Mechanics #6 §3.5.6/§3.6.8/§3.6.9 (ERR-006-002 / ERR-006-003);
//           shot-outcome design KD-1..KD-3; Code Standards #20
// Purpose:  Locks the shot-outcome pass's placement/error changes: the §3.5.6 launch-tilted
//           aim composition, the distance-scaled error cone, and the vertical clamp that
//           bounds error without flattening the launch model.

using NUnit.Framework;

using UnityEngine;

namespace TacticalDirector.ShotMechanics.Tests
{
    [TestFixture]
    public class ShotPlacementResolverShotOutcomeTests
    {
        [Test]
        public void AimWithLaunchAngle_IsUnitVector_WithSinOfLaunchInZ()
        {
            var shooter = new Vector3(85f, 34f, 0f);
            Vector3 dir = ShotPlacementResolver.ComputeAimDirectionWithLaunchAngle(
                new Vector2(0.5f, 0.5f), shooter, launchAngleDeg: 12f);

            Assert.AreEqual(1f, dir.magnitude, 1e-4f, "Aim direction must be unit length");
            Assert.AreEqual(Mathf.Sin(12f * Mathf.Deg2Rad), dir.z, 1e-4f,
                "The vertical comes from the launch angle (§3.5.6), not from placement v");
        }

        [Test]
        public void AimWithLaunchAngle_PlacementV_DoesNotDriveVertical()
        {
            var shooter = new Vector3(85f, 34f, 0f);
            Vector3 low  = ShotPlacementResolver.ComputeAimDirectionWithLaunchAngle(
                new Vector2(0.1f, 0.05f), shooter, 8f);
            Vector3 high = ShotPlacementResolver.ComputeAimDirectionWithLaunchAngle(
                new Vector2(0.1f, 0.95f), shooter, 8f);

            Assert.AreEqual(low.z, high.z, 1e-6f,
                "§3.5.6 discards v — elevation is the launch model's alone");
        }

        [Test]
        public void ErrorOffset_ScalesWithDistance()
        {
            // The ERR-006-003 lock: the same angular error displaces twice as far at twice the
            // range — the cone is a cone. Compare the lateral goal-plane displacement produced by
            // an identical error offset at 10 m and at 20 m from the goal line.
            GoalGeometry goal = GoalGeometryProvider.Get();
            Vector2 offset = ShotErrorCalculator.ComputeErrorOffset(
                errorMagnitudeDeg: 3f, errorDirection: new Vector2(1f, 0f));

            Vector3 near = LateralAt(goal, offset, distance: 10f);
            Vector3 far  = LateralAt(goal, offset, distance: 20f);

            float nearDisp = near.y - goal.LeftPostY - 0.5f * goal.GoalWidth;
            float farDisp  = far.y  - goal.LeftPostY - 0.5f * goal.GoalWidth;

            Assert.AreEqual(Mathf.Tan(3f * Mathf.Deg2Rad) * 10f, nearDisp, 1e-3f,
                "Displacement at the goal plane must be tan(err) × distance");
            Assert.AreEqual(2f * nearDisp, farDisp, 1e-3f,
                "The same angular error must displace twice as far at twice the range");
        }

        private static Vector3 LateralAt(GoalGeometry goal, Vector2 offset, float distance)
        {
            var shooter = new Vector3(goal.GoalLineX - distance,
                                      goal.LeftPostY + 0.5f * goal.GoalWidth, 0f);
            Vector3 baseAim = ShotPlacementResolver.ComputeAimDirectionWithLaunchAngle(
                new Vector2(0.5f, 0.5f), shooter, 0f);
            Vector3 adjusted = ShotPlacementResolver.ApplyErrorOffset(baseAim, offset, shooter);
            // Re-project the adjusted direction to the goal plane to read the displacement.
            return shooter + adjusted * (distance / Mathf.Max(adjusted.x, 1e-3f));
        }

        [Test]
        public void ErrorOffset_CanCarryTheAimWideOfThePost()
        {
            // The §7.1 defect in one assertion: from 20 m, a 5° error aimed laterally from
            // u = 0.1 (0.732 m inside the post) lands tan(5°)·20 ≈ 1.75 m away — wide.
            GoalGeometry goal = GoalGeometryProvider.Get();
            var shooter = new Vector3(goal.GoalLineX - 20f,
                                      goal.LeftPostY + 0.1f * goal.GoalWidth, 0f);

            Vector3 baseAim = ShotPlacementResolver.ComputeAimDirectionWithLaunchAngle(
                new Vector2(0.1f, 0.5f), shooter, 5f);
            Vector2 offset = ShotErrorCalculator.ComputeErrorOffset(5f, new Vector2(-1f, 0f));
            Vector3 adjusted = ShotPlacementResolver.ApplyErrorOffset(baseAim, offset, shooter);

            Vector3 atPlane = shooter + adjusted * (20f / Mathf.Max(adjusted.x, 1e-3f));
            Assert.Less(atPlane.y, goal.LeftPostY,
                "A 5° lateral error from 20 m must be able to miss the goal to the side");
        }

        [Test]
        public void ErrorOffset_VerticalClamp_PreservesHighLaunchBaseTarget()
        {
            // KD-3's clamp rule: a lofted strike whose base trajectory tops 1.5 × GoalHeight at
            // the goal plane must NOT be flattened by the clamp — the clamp bounds what error can
            // ADD, never what the launch model intended. 25° from 20 m ⇒ base z ≈ 9.3 m ≫ 3.66 m.
            GoalGeometry goal = GoalGeometryProvider.Get();
            var shooter = new Vector3(goal.GoalLineX - 20f,
                                      goal.LeftPostY + 0.5f * goal.GoalWidth, 0f);

            Vector3 baseAim = ShotPlacementResolver.ComputeAimDirectionWithLaunchAngle(
                new Vector2(0.5f, 0.5f), shooter, 25f);
            Vector3 adjusted = ShotPlacementResolver.ApplyErrorOffset(
                baseAim, Vector2.zero, shooter);

            Assert.AreEqual(baseAim.z, adjusted.z, 1e-3f,
                "A zero error must leave a high-launch aim unflattened by the vertical clamp");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-27 | —      | Initial. Locks ERR-006-002/003: the §3.5.6 launch-tilt aim, the    |
// |         |            |        | distance-scaled error cone (tan(err)×distance at the goal plane,   |
// |         |            |        | wide-of-the-post reachable from 20 m at 5°), and the KD-3 vertical |
// |         |            |        | clamp that bounds error without flattening a lofted launch.        |
#endregion
