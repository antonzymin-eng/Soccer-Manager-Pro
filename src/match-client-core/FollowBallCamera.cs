// File:     src/match-client-core/FollowBallCamera.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P3, §7
//           "Rendering, camera, HUD"), Ball Physics #1 §1.2 (corner-origin frame), Code Standards #20
// Purpose:  Pure follow-ball camera target math — dead zone, frame-rate-independent smoothing, and a
//           clamp that keeps the view on the pitch. Host-free so it is unit-testable without Unity.

using UnityEngine;

using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// Computes where a follow-ball camera should be looking, given where it is looking now.
    ///
    /// <para>Three behaviours, each earning its place:</para>
    /// <list type="bullet">
    /// <item><b>Dead zone.</b> Ball movement inside <c>CameraDeadZoneM</c> of the current target is
    /// ignored. A camera locked rigidly to the ball shakes constantly, because the ball is being
    /// jostled constantly.</item>
    /// <item><b>Exponential smoothing at <c>1 − e^(−rate·dt)</c>.</b> Not a per-frame lerp: that
    /// makes camera feel a function of frame rate, so the same build follows twice as fast on a
    /// 120 Hz display as on a 60 Hz one.</item>
    /// <item><b>Pitch clamp.</b> The target is bounded so the visible rectangle never runs more than
    /// <c>CameraOverscanM</c> past the pitch. When the view is wider than the pitch on an axis the
    /// clamp centres it there instead — otherwise the min bound exceeds the max and the camera
    /// jitters between two impossible corners.</item>
    /// </list>
    ///
    /// <para>Pure and stateless: the caller owns the current target and feeds it back in. Nothing
    /// here reads a clock, and nothing reaches the simulation.</para>
    /// </summary>
    public static class FollowBallCamera
    {
        /// <summary>
        /// Next camera target, given the current one and where the ball is.
        /// </summary>
        /// <param name="currentTarget">Where the camera is looking now (pitch metres).</param>
        /// <param name="ballXY">Ball position in the pitch plane (metres, corner-origin).</param>
        /// <param name="deltaSeconds">Wall-clock seconds since the last camera update.</param>
        /// <returns>
        /// The clamped, smoothed target. A non-finite ball position or a non-positive / non-finite
        /// <paramref name="deltaSeconds"/> holds the camera where it is (re-clamped) rather than
        /// flinging it somewhere undefined.
        /// </returns>
        public static Vector2 ComputeTarget(Vector2 currentTarget, Vector2 ballXY, float deltaSeconds)
        {
            Vector2 from = IsFinite(currentTarget) ? currentTarget : PitchCentre;

            if (!IsFinite(ballXY)) return Clamp(from);
            if (!(deltaSeconds > 0f) || float.IsInfinity(deltaSeconds)) return Clamp(from);

            Vector2 desired = DesiredTarget(from, ballXY);

            float rate = MatchClientConstants.CameraFollowRatePerSecond;
            // A non-positive or non-finite rate means "do not smooth"; snapping is the honest
            // degenerate behaviour, and it keeps a misconfigured build watchable rather than frozen.
            float t = (rate > 0f && !float.IsInfinity(rate))
                ? 1f - Mathf.Exp(-rate * deltaSeconds)
                : 1f;

            return Clamp(Vector2.Lerp(from, desired, Mathf.Clamp01(t)));
        }

        /// <summary>
        /// Where the camera would ultimately settle for this ball position, before smoothing: the
        /// current target while the ball is inside the dead zone, otherwise the point that puts the
        /// ball exactly on the dead-zone edge. Exposed because it is the property tests assert
        /// against — the settling point, not one frame of the approach.
        /// </summary>
        public static Vector2 DesiredTarget(Vector2 currentTarget, Vector2 ballXY)
        {
            Vector2 offset = ballXY - currentTarget;
            float dead = MatchClientConstants.CameraDeadZoneM;

            if (!(dead > 0f)) return ballXY;
            if (offset.sqrMagnitude <= dead * dead) return currentTarget;

            // Pull the target only as far as it takes to bring the ball back to the dead-zone edge,
            // so the camera trails the ball instead of centring it — the framing a broadcast uses.
            float distance = offset.magnitude;
            return currentTarget + offset * ((distance - dead) / distance);
        }

        /// <summary>
        /// Clamps a target so the visible rectangle stays within the pitch plus
        /// <c>CameraOverscanM</c>, centring the axis instead when the view is wider than the pitch.
        /// </summary>
        public static Vector2 Clamp(Vector2 target)
        {
            return new Vector2(
                ClampAxis(target.x, MatchEngineConstants.PITCH_LENGTH_M, MatchClientConstants.CameraViewHalfWidthM),
                ClampAxis(target.y, MatchEngineConstants.PITCH_WIDTH_M,  MatchClientConstants.CameraViewHalfHeightM));
        }

        private static float ClampAxis(float value, float pitchExtent, float viewHalfExtent)
        {
            float overscan = Mathf.Max(0f, MatchClientConstants.CameraOverscanM);
            float half     = Mathf.Max(0f, viewHalfExtent);

            float min = half - overscan;
            float max = pitchExtent - half + overscan;

            // The view covers more than the pitch on this axis: there is no position that satisfies
            // both bounds, so centre it. Clamping to a crossed range would snap to whichever bound the
            // comparison happened to apply last.
            if (min > max) return pitchExtent * 0.5f;

            return Mathf.Clamp(value, min, max);
        }

        private static Vector2 PitchCentre =>
            new Vector2(MatchEngineConstants.PITCH_LENGTH_M * 0.5f, MatchEngineConstants.PITCH_WIDTH_M * 0.5f);

        private static bool IsFinite(Vector2 v) => float.IsFinite(v.x) && float.IsFinite(v.y);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (P3): dead zone, frame-rate-independent       |
// |         |            |        | exponential follow, and a pitch clamp that centres rather than |
// |         |            |        | oscillating when the view is wider than the pitch.             |
#endregion
