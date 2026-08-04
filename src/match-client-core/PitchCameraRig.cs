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

            return new PitchCameraPose(position, lookAt);
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
#endregion
