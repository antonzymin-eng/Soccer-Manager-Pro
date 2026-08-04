// File:     src/match-client-core/tests/PitchCameraRigTests.cs
// Created:  2026-08-04
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a, §7),
//           Testing Strategy #19, Code Standards #20
// Purpose:  Locks the camera placement: that it is above the ground, tilted back rather than straight
//           down, offset laterally, and aimed at the point it was asked to look at.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class PitchCameraRigTests
    {
        private const float Tolerance = 1e-3f;

        private static Vector2 Centre =>
            new Vector2(MatchEngineConstants.PITCH_LENGTH_M * 0.5f, MatchEngineConstants.PITCH_WIDTH_M * 0.5f);

        [Test]
        public void TheCameraLooksAtTheGroundPointItWasGiven()
        {
            var target = new Vector2(30f, 20f);

            PitchCameraPose pose = PitchCameraRig.ComputePose(target);

            Vector3 expected = PitchViewProjection.ToWorld(target, 0f);
            Assert.AreEqual(expected.x, pose.LookAt.x, Tolerance);
            Assert.AreEqual(expected.z, pose.LookAt.z, Tolerance);
            Assert.AreEqual(0f, pose.LookAt.y, Tolerance, "the aim point is on the ground plane");
        }

        [Test]
        public void TheCameraSitsAboveTheGround_AtTheConfiguredHeight()
        {
            PitchCameraPose pose = PitchCameraRig.ComputePose(Centre);

            Assert.AreEqual(MatchClientConstants.CameraHeightM, pose.Position.y, Tolerance);
            Assert.Greater(pose.Position.y, 0f, "a camera at or below the turf sees no ground plane");
        }

        [Test]
        public void TheViewIsTilted_NotStraightDown()
        {
            // The whole point of the revision. A straight-down camera puts the position directly
            // over the target, which is what the flat view did and what the ball-scaling cue existed
            // to compensate for.
            PitchCameraPose pose = PitchCameraRig.ComputePose(Centre);

            Assert.AreNotEqual(pose.Position.z, pose.LookAt.z,
                "a camera directly over its target is the flat view this replaced");
            Assert.Less(pose.Position.z, pose.LookAt.z, "the setback runs toward the near touchline");
        }

        [Test]
        public void TheSetbackMatchesTheConfiguredTilt()
        {
            // setback = height × tan(tilt), with tilt measured FROM VERTICAL. If the angle were read
            // from the horizontal instead, this is the assertion that catches it.
            PitchCameraPose pose = PitchCameraRig.ComputePose(Centre);

            float setback = pose.LookAt.z - pose.Position.z;
            float expected = MatchClientConstants.CameraHeightM
                * Mathf.Tan(MatchClientConstants.CameraTiltDegrees * Mathf.Deg2Rad);

            Assert.AreEqual(expected, setback, Tolerance);
        }

        [Test]
        public void TheCameraIsOffsetLaterally_SoTheViewIsNotPerfectlySymmetric()
        {
            PitchCameraPose pose = PitchCameraRig.ComputePose(Centre);

            Assert.AreEqual(
                MatchClientConstants.CameraLateralOffsetM,
                pose.Position.x - pose.LookAt.x, Tolerance,
                "the offset is what gives the eye an asymmetry to read depth from");
        }

        [Test]
        public void TheEffectiveTiltExceedsTheConfiguredOne_BecauseOfThatOffset()
        {
            // Documented behaviour, asserted so nobody later "corrects" the discrepancy: the
            // configured angle is the tilt in the setback plane, and the lateral offset skews it.
            Assert.Greater(
                PitchCameraRig.EffectiveTiltDegrees(),
                MatchClientConstants.CameraTiltDegrees,
                "a lateral offset adds horizontal distance, so the real angle is larger");

            Assert.Less(PitchCameraRig.EffectiveTiltDegrees(), 90f,
                "however skewed, the camera must stay above the ground plane");
        }

        [Test]
        public void ThePoseTracksItsTarget_AtBothEnds()
        {
            // Mirrored per the root CLAUDE.md trap: the rig must behave the same at either end, and
            // a centre-only test would pass on a rig that hard-coded one half of the pitch.
            foreach (Vector2 target in new[] { new Vector2(11f, 34f), new Vector2(94f, 34f) })
            {
                PitchCameraPose pose = PitchCameraRig.ComputePose(target);
                Vector3 expected = PitchViewProjection.ToWorld(target, 0f);

                Assert.AreEqual(expected.x, pose.LookAt.x, Tolerance, "look-at X at " + target);
                Assert.AreEqual(expected.x + MatchClientConstants.CameraLateralOffsetM,
                    pose.Position.x, Tolerance, "camera X at " + target);
                Assert.AreEqual(MatchClientConstants.CameraHeightM, pose.Position.y, Tolerance);
            }
        }

        [Test]
        public void TheCameraCanSeeItsOwnTarget()
        {
            // The closing-the-loop assertion: a ray from the camera to its aim point must land back
            // on the target through TryGroundHit. It ties the rig and the click inverse together, so
            // a sign error in either shows up here rather than as an un-clickable pitch in P4b.
            foreach (Vector2 target in new[] { new Vector2(11f, 34f), new Vector2(94f, 34f), Centre })
            {
                PitchCameraPose pose = PitchCameraRig.ComputePose(target);

                Assert.IsTrue(
                    PitchViewProjection.TryGroundHit(pose.Position, pose.LookAt - pose.Position, out Vector2 hit),
                    "the camera must be able to see the ground it is aimed at, target " + target);

                Assert.AreEqual(target.x, hit.x, Tolerance, "target " + target);
                Assert.AreEqual(target.y, hit.y, Tolerance, "target " + target);
            }
        }

        [Test]
        public void ThePoseCarriesTheFieldOfView_SoTheBindingChoosesNothing()
        {
            // The whole reason the field of view is here: position + look-at + fov is everything
            // Unity's Camera needs. If any one of them were left out, P4b would have to invent it
            // inside the MonoBehaviour the gate cannot compile — §12 rule 1.
            PitchCameraPose pose = PitchCameraRig.ComputePose(Centre);

            Assert.AreEqual(MatchClientConstants.CameraVerticalFovDegrees, pose.FieldOfViewDegrees, Tolerance);
            Assert.Greater(pose.FieldOfViewDegrees, 0f, "a zero field of view sees nothing");
        }

        [Test]
        public void TheVisibleGroundIsATrapezoid_ReachingFurtherBeyondTheAimPointThanInFrontOfIt()
        {
            // Not symmetry, and asserting symmetry is the mistake this guards. The far ray meets the
            // ground at a shallower angle, so it travels further before it gets there — which is the
            // real reason CameraViewHalfHeightM is documented as approximate rather than exact.
            PitchCameraRig.GroundExtentAlongTilt(out float near, out float far);

            Assert.Greater(near, 0f, "the near edge must lie in front of the aim point");
            Assert.Greater(far, near, "a tilted camera sees further beyond its aim point than in front of it");
        }

        [Test]
        public void TheVisibleGroundMatchesTheConfiguredTiltAndFieldOfView()
        {
            // Pins the formula rather than just its shape: both edges are height × tan(angle from
            // vertical), measured relative to the aim point.
            float height  = MatchClientConstants.CameraHeightM;
            float tilt    = MatchClientConstants.CameraTiltDegrees;
            float halfFov = MatchClientConstants.CameraVerticalFovDegrees * 0.5f;
            float aim     = height * Mathf.Tan(tilt * Mathf.Deg2Rad);

            PitchCameraRig.GroundExtentAlongTilt(out float near, out float far);

            Assert.AreEqual(aim - height * Mathf.Tan((tilt - halfFov) * Mathf.Deg2Rad), near, Tolerance);
            Assert.AreEqual(height * Mathf.Tan((tilt + halfFov) * Mathf.Deg2Rad) - aim, far, Tolerance);
        }

        [Test]
        public void TheCameraSeesAtLeastWhatTheClampAssumesItDoes()
        {
            // The clamp bounds the target by CameraViewHalfHeightM, which describes visible ground.
            // Nothing ties that figure to the rig's height/tilt/fov, so this is the check that the
            // two have not drifted into contradiction — a clamp permitting framing the camera cannot
            // actually deliver would push the target off the visible pitch and nothing would say so.
            PitchCameraRig.GroundExtentAlongTilt(out float near, out float far);

            Assert.GreaterOrEqual(near, MatchClientConstants.CameraViewHalfHeightM,
                "the camera sees less in front of its aim point than the clamp assumes");
            Assert.GreaterOrEqual(far, MatchClientConstants.CameraViewHalfHeightM,
                "the camera sees less beyond its aim point than the clamp assumes");
        }

        [Test]
        public void ADegenerateTarget_AimsAtTheCentreSpot()
        {
            Vector3 centreWorld = PitchViewProjection.ToWorld(Centre, 0f);

            foreach (Vector2 bad in new[]
            {
                new Vector2(float.NaN, 34f),
                new Vector2(52.5f, float.PositiveInfinity),
            })
            {
                PitchCameraPose pose = PitchCameraRig.ComputePose(bad);

                Assert.AreEqual(centreWorld.x, pose.LookAt.x, Tolerance, "target " + bad);
                Assert.AreEqual(centreWorld.z, pose.LookAt.z, Tolerance, "target " + bad);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-04 | —      | Initial creation (tilted-view revision): the placement itself,  |
// |         |            |        | the tilt-from-vertical convention, the deliberate skew the      |
// |         |            |        | lateral offset introduces, both-ends tracking, the degenerate   |
// |         |            |        | target, and the closing loop — a ray from the camera to its own |
// |         |            |        | aim point must come back through TryGroundHit as the target.    |
// | 1.1     | 2026-08-04 | —      | AR pass 2, H-1: + the field of view on the pose (the assertion  |
// |         |            |        | that a binding is left with nothing to choose), + the ground    |
// |         |            |        | extent — its formula, its ASYMMETRY (a tilted camera sees a     |
// |         |            |        | trapezoid, and asserting symmetry is the mistake to guard),     |
// |         |            |        | and the consistency check against CameraViewHalfHeightM, which  |
// |         |            |        | the follow-ball clamp uses and which nothing otherwise ties to  |
// |         |            |        | what the camera can actually see.                               |
#endregion
