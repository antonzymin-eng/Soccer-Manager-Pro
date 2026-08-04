// File:     src/match-client-core/PitchCameraRig.cs
// Created:  2026-08-04
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a, §7
//           "Rendering, camera, HUD", §12 rule 1), Code Standards #20
// Purpose:  Turns a follow-ball target in pitch metres into the camera's world placement — height,
//           tilt back from vertical, and the lateral offset that makes the view slightly oblique.

using UnityEngine;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// Places the match camera: high above the pitch, tilted back from straight-down, and pushed
    /// slightly to one side (§7 "Rendering, camera, HUD").
    ///
    /// <para><b>The shape of the view.</b> Straight top-down reads as a diagram and flattens height
    /// to nothing; a broadcast-height camera reads as television and hides the shape of the team.
    /// A modest tilt keeps the tactical plan-view legible while giving the eye enough perspective to
    /// see a ball leave the ground — and perspective then does the height cue for free, which is why
    /// nothing in the render model scales the ball with altitude any more.</para>
    ///
    /// <para><b>Where the tilt comes from.</b> The camera sits <c>CameraHeightM</c> above the ground
    /// and is set back horizontally by <c>height × tan(tilt)</c>, so <c>CameraTiltDegrees</c> is
    /// measured <b>from vertical</b>: 0° is straight down, and larger values lie the view flatter.
    /// The setback runs along −Z (the near touchline side), so the camera looks across the pitch's
    /// width rather than down its length.</para>
    ///
    /// <para><b>The lateral offset is not cosmetic.</b> Pushing the camera along X by
    /// <c>CameraLateralOffsetM</c> breaks the perfect symmetry of a centred view, which is what makes
    /// depth readable at all — with the camera dead-centre, the two halves of the pitch project
    /// identically and the eye has nothing to judge distance against. It does mean the effective tilt
    /// is <c>atan(√(setback² + offset²) ÷ height)</c> rather than the configured angle; the
    /// configured value is the tilt in the setback plane, and the offset skews it slightly. That is
    /// the intent, not an error, and <see cref="ComputePose"/>'s tests assert the skew explicitly so
    /// nobody later "fixes" it.</para>
    ///
    /// <para><b>Field of view belongs here too.</b> Where the camera is and how much it sees are one
    /// framing decision, not two, and splitting them would leave the second half to be invented in
    /// the <c>MonoBehaviour</c> the CI gate cannot compile — §12 rule 1. It travels on
    /// <see cref="PitchCameraPose"/>; <see cref="GroundExtentAlongTilt"/> says what it buys, in
    /// metres of visible pitch.</para>
    ///
    /// <para>Pure and stateless: the caller owns the target (from <see cref="FollowBallCamera"/>) and
    /// gets back a placement. Nothing here reads a clock or reaches the simulation.</para>
    /// </summary>
    public static class PitchCameraRig
    {
        /// <summary>
        /// Camera placement for a look-at target given in pitch metres (corner-origin).
        /// </summary>
        /// <param name="targetPitchXY">
        /// Where the camera should be aimed, normally <see cref="FollowBallCamera.ComputeTarget"/>'s
        /// output. A non-finite target aims at the centre spot rather than sending the camera
        /// somewhere undefined — the same degenerate-input posture <see cref="FollowBallCamera"/>
        /// takes, and for the same reason: a frozen-but-watchable view beats a lost one.
        /// </param>
        public static PitchCameraPose ComputePose(Vector2 targetPitchXY)
        {
            Vector2 target = IsFinite(targetPitchXY) ? targetPitchXY : PitchCentre;

            Vector3 lookAt = PitchViewProjection.ToWorld(target, 0f);

            float height  = MatchClientConstants.CameraHeightM;
            float setback = height * Mathf.Tan(MatchClientConstants.CameraTiltDegrees * Mathf.Deg2Rad);

            var position = new Vector3(
                lookAt.x + MatchClientConstants.CameraLateralOffsetM,
                height,
                lookAt.z - setback);

            return new PitchCameraPose(position, lookAt, MatchClientConstants.CameraVerticalFovDegrees);
        }

        /// <summary>
        /// How far the visible ground reaches in front of and beyond the aim point, along the tilt
        /// axis (world Z), in metres.
        ///
        /// <para><b>The two are not equal, and that is the point.</b> A tilted camera sees a
        /// trapezoid, not a rectangle: the far edge is stretched by the shallower angle its ray meets
        /// the ground at, so it lies further beyond the aim point than the near edge lies in front of
        /// it. Both edges come from <c>height × tan(angle from vertical)</c> — the near edge at
        /// <c>tilt − fov/2</c>, the far at <c>tilt + fov/2</c> — measured relative to the aim point at
        /// <c>height × tan(tilt)</c>.</para>
        ///
        /// <para>Exposed so the framing has a number attached rather than a guess. It is what makes
        /// the field of view assertable at all, and it is the honest version of what
        /// <c>CameraViewHalfHeightM</c> approximates for the clamp (see that constant's note).</para>
        /// </summary>
        /// <param name="nearM">Distance from the aim point to the near edge of visible ground.</param>
        /// <param name="farM">Distance from the aim point to the far edge.</param>
        public static void GroundExtentAlongTilt(out float nearM, out float farM)
        {
            float height = MatchClientConstants.CameraHeightM;
            float tilt   = MatchClientConstants.CameraTiltDegrees;
            float halfFov = MatchClientConstants.CameraVerticalFovDegrees * 0.5f;

            float aim = height * Mathf.Tan(tilt * Mathf.Deg2Rad);

            // The near ray may point past the nadir (tilt < fov/2), in which case tan goes negative
            // and the near edge is behind the point directly below the camera. That is a real view,
            // not a degenerate one, and the subtraction below stays correct through it. The FAR ray
            // is the one that must not reach 90°, and the catalogue refuses that pairing at boot.
            nearM = aim - height * Mathf.Tan((tilt - halfFov) * Mathf.Deg2Rad);
            farM  = height * Mathf.Tan((tilt + halfFov) * Mathf.Deg2Rad) - aim;
        }

        /// <summary>
        /// The camera's actual angle from vertical in degrees, once the lateral offset is taken into
        /// account. Exposed because it is the number a framing decision is really made against, and
        /// because it differs from the configured <c>CameraTiltDegrees</c> by design.
        /// </summary>
        public static float EffectiveTiltDegrees()
        {
            float height  = MatchClientConstants.CameraHeightM;
            float setback = height * Mathf.Tan(MatchClientConstants.CameraTiltDegrees * Mathf.Deg2Rad);
            float lateral = MatchClientConstants.CameraLateralOffsetM;

            float horizontal = Mathf.Sqrt(setback * setback + lateral * lateral);

            return Mathf.Atan2(horizontal, height) * Mathf.Rad2Deg;
        }

        private static Vector2 PitchCentre =>
            new Vector2(PitchViewProjection.HalfLengthM, PitchViewProjection.HalfWidthM);

        private static bool IsFinite(Vector2 v) =>
            float.IsFinite(v.x) && float.IsFinite(v.y);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-04 | —      | Initial creation (tilted-view revision, owner call): the        |
// |         |            |        | camera placement decision — height, tilt from vertical, and     |
// |         |            |        | the lateral offset that makes depth readable — resolved in a    |
// |         |            |        | gate-compiled assembly rather than in the MonoBehaviour the CI  |
// |         |            |        | gate can never see (§12 rule 1). Returns two world points, not  |
// |         |            |        | a rotation: Quaternion is not in the shim's surface.            |
// | 1.1     | 2026-08-04 | —      | AR pass 2, H-1: the rig placed the camera but never said how    |
// |         |            |        | much it sees, so the field of view — as much a framing decision |
// |         |            |        | as the height and the tilt — would have been chosen in the      |
// |         |            |        | binding, exactly the leak the P4a/P4b split closes. It is now   |
// |         |            |        | on the pose, and + GroundExtentAlongTilt gives it a number:     |
// |         |            |        | the near and far reach of visible ground, asymmetric because a  |
// |         |            |        | tilted camera sees a trapezoid.                                 |
#endregion
