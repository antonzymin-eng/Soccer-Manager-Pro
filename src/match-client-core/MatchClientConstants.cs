// File:     src/match-client-core/MatchClientConstants.cs
// Created:  2026-07-24
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P0/§5-P3/§5-P4a),
//           Code Standards #20 (constant catalogue; no magic numbers)
// Purpose:  Constant catalogue for the host-free interactive-client core: the master-plan
//           playback-speed set the UI presents (Pause is a streamer state, not a multiplier), the P3
//           interpolator snap distances, the P3 follow-ball camera tuning, the P4a camera rig
//           (height, tilt, lateral offset, field of view), and the P4a render-cue sizes.

using System;

using static TacticalDirector.ProjectConstants.GameplayConfigHolder;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// Constant catalogue for the interactive Unity client's host-free core. Presentation/pacing
    /// only — nothing here feeds the simulation or the snapshot digest. The four playback-speed
    /// steps are the master plan's set {Pause, 1, 3, 5, 10} (§3.4); Pause is delivered by the
    /// streamer's <c>Pause()</c>, the four multipliers by <c>SetSpeedMultiplier</c> (whose
    /// <c>MatchViewerConstants.MaxLiveSpeedMultiplier</c> cap is raised to ≥ 10 so 10× is not
    /// silently clamped — §5-P0). The P5 UI presents these; the core does not consume them.
    /// </summary>
    public static class MatchClientConstants
    {
        #region GT — playback-speed set (master plan §3.4: Pause / 1× / 3× / 5× / 10×)

        /// <summary>[GT] Real-time playback multiplier (1×). Config key [match-client] Speed1x.</summary>
        public static readonly float Speed1x = Config.GetFloat("match-client", "Speed1x", 1f);

        /// <summary>[GT] Fast playback multiplier (3×). Config key [match-client] Speed3x.</summary>
        public static readonly float Speed3x = Config.GetFloat("match-client", "Speed3x", 3f);

        /// <summary>[GT] Faster playback multiplier (5×). Config key [match-client] Speed5x.</summary>
        public static readonly float Speed5x = Config.GetFloat("match-client", "Speed5x", 5f);

        /// <summary>[GT] Fastest playback multiplier (10×). Config key [match-client] Speed10x. Requires <c>MatchViewerConstants.MaxLiveSpeedMultiplier</c> ≥ 10 (§5-P0).</summary>
        public static readonly float Speed10x = Config.GetFloat("match-client", "Speed10x", 10f);

        #endregion

        #region GT — P3 frame interpolation (§5-P3 / §7 "Interpolation")

        /// <summary>
        /// [GT] Displacement (m) above which the interpolator SNAPS instead of blending, for the ball.
        /// Config key [match-client] BallSnapDistanceM.
        ///
        /// <para>Not a smoothing knob — a correctness one. A restart teleports the ball to the centre
        /// spot, a goal kick to the six-yard box; blending across that draws the ball gliding the
        /// length of the pitch, which reads as a bug rather than a restart. Anything a ball can
        /// legitimately travel in one frame interval is far below this, so a jump past it is a
        /// discontinuity by construction.</para>
        /// </summary>
        public static readonly float BallSnapDistanceM =
            Config.GetFloat("match-client", "BallSnapDistanceM", 10f);

        /// <summary>
        /// [GT] Displacement (m) above which a single agent snaps rather than blends. Config key
        /// [match-client] AgentSnapDistanceM. Lower than the ball's because agents cannot move
        /// anywhere near as fast; the discontinuity it catches is a SUBSTITUTION, which swaps who
        /// occupies a roster slot and so moves that slot's rendered position instantly.
        /// </summary>
        public static readonly float AgentSnapDistanceM =
            Config.GetFloat("match-client", "AgentSnapDistanceM", 5f);

        #endregion

        #region GT — P3 follow-ball camera (§5-P3 / §7 "Rendering, camera, HUD")

        /// <summary>
        /// [GT] Radius (m) around the camera's current target inside which ball movement is ignored.
        /// Config key [match-client] CameraDeadZoneM. Without it the camera chases every jostle and
        /// the whole pitch shimmers.
        /// </summary>
        public static readonly float CameraDeadZoneM =
            Config.GetFloat("match-client", "CameraDeadZoneM", 4f);

        /// <summary>
        /// [GT] Exponential follow rate (per second) once the ball leaves the dead zone. Config key
        /// [match-client] CameraFollowRatePerSecond. Applied as <c>1 − e^(−rate·dt)</c> so the camera
        /// covers the same ground per second at 30 FPS as at 144 — a plain per-frame lerp would make
        /// camera feel a function of frame rate.
        /// </summary>
        public static readonly float CameraFollowRatePerSecond =
            Config.GetFloat("match-client", "CameraFollowRatePerSecond", 4f);

        /// <summary>
        /// [GT] Half-width (m) of the visible area along the pitch's long axis. Config key
        /// [match-client] CameraViewHalfWidthM. The target is clamped so the view never runs past the
        /// pitch by more than <see cref="CameraOverscanM"/>.
        ///
        /// <para><b>Approximate since the camera gained a tilt.</b> This pair describes an
        /// axis-aligned rectangle of visible ground, which is exactly right for a straight-down view
        /// and only roughly right for a tilted one — the real footprint is a trapezoid, deeper at the
        /// far edge. The clamp's job is keeping the target near the pitch rather than computing exact
        /// framing, so the approximation is kept deliberately rather than complicating
        /// <see cref="FollowBallCamera"/>'s pure 2D math with a projection.</para>
        /// </summary>
        public static readonly float CameraViewHalfWidthM =
            Config.GetFloat("match-client", "CameraViewHalfWidthM", 26f);

        /// <summary>
        /// [GT] Half-height (m) of the visible area across the pitch. Config key [match-client]
        /// CameraViewHalfHeightM.
        /// </summary>
        public static readonly float CameraViewHalfHeightM =
            Config.GetFloat("match-client", "CameraViewHalfHeightM", 15f);

        /// <summary>
        /// [GT] How far outside the touchlines and goal lines the view may show (m). Config key
        /// [match-client] CameraOverscanM. Zero would pin the camera hard at the boundary and make a
        /// corner look like the ball is off-centre; a small margin keeps the action framed.
        /// </summary>
        public static readonly float CameraOverscanM =
            Config.GetFloat("match-client", "CameraOverscanM", 3f);

        #endregion

        #region GT — P4a camera rig (§5-P4a / §7 "Rendering, camera, HUD")

        /// <summary>
        /// [GT] Camera height above the ground, metres. Config key [match-client] CameraHeightM.
        /// With <see cref="CameraTiltDegrees"/> and <see cref="CameraVerticalFovDegrees"/> it fixes
        /// how much pitch is in shot — <see cref="PitchCameraRig.GroundExtentAlongTilt"/> reports the
        /// result in metres. The <see cref="CameraViewHalfWidthM"/> /
        /// <see cref="CameraViewHalfHeightM"/> pair separately bounds where the camera may look (see
        /// their note on why that clamp is now approximate).
        /// </summary>
        public static readonly float CameraHeightM = RequireAtLeast(
            Config.GetFloat("match-client", "CameraHeightM", 38f), 1f, "CameraHeightM");

        /// <summary>
        /// [GT] Camera tilt measured FROM VERTICAL, degrees. Config key [match-client]
        /// CameraTiltDegrees. 0° is straight down; larger values lie the view flatter.
        ///
        /// <para>Kept modest on purpose: past roughly 40° the pitch stops reading as a tactical
        /// plan-view and starts reading as broadcast footage, where the far half of the field
        /// compresses into a band and relative positions get hard to judge. Must stay under 90°,
        /// where the camera would be level with the turf and the ground plane would vanish — that
        /// bound is enforced at boot rather than documented.</para>
        /// </summary>
        public static readonly float CameraTiltDegrees = RequireInRange(
            Config.GetFloat("match-client", "CameraTiltDegrees", 22f), 0f, 89f, "CameraTiltDegrees");

        /// <summary>
        /// [GT] Sideways offset of the camera from directly behind its target, metres. Config key
        /// [match-client] CameraLateralOffsetM.
        ///
        /// <para>The "slightly off-centre" part of the framing, and it does real work: with the
        /// camera dead-centre the two halves of the pitch project identically and the eye has no
        /// asymmetry to read depth from. It skews the effective tilt slightly — see
        /// <see cref="PitchCameraRig.EffectiveTiltDegrees"/>, which reports the real angle.</para>
        ///
        /// <para>Either sign is meaningful (the offset picks a side), so this is checked for
        /// finiteness rather than range — but it IS checked. It is the one dial that lands directly
        /// in the camera's world position, so a non-finite value here puts the camera nowhere while
        /// every assertion about the aim point still passes.</para>
        /// </summary>
        public static readonly float CameraLateralOffsetM = RequireFinite(
            Config.GetFloat("match-client", "CameraLateralOffsetM", 5f), "CameraLateralOffsetM");

        /// <summary>
        /// [GT] Vertical field of view of the match camera, degrees. Config key [match-client]
        /// CameraVerticalFovDegrees. Unity's <c>Camera.fieldOfView</c> is the vertical one, so this
        /// is assigned to it directly.
        ///
        /// <para><b>Why the core owns this at all.</b> Height and tilt decide where the camera is;
        /// the field of view decides how much of the pitch it sees, which is just as much a framing
        /// decision. Leaving it out would mean P4b picking a number inside a <c>MonoBehaviour</c> —
        /// a decision in the one place the CI gate cannot compile, which is precisely what §12
        /// rule 1 and the P4a/P4b split exist to prevent. It rides on
        /// <see cref="PitchCameraPose"/> so the binding assigns it and chooses nothing.</para>
        ///
        /// <para>The upper bound is not cosmetic: the camera's lowest ray leaves the ground entirely
        /// once <c>tilt + fov/2</c> reaches 90°, which puts the horizon in shot and sends the far
        /// edge of the visible ground to infinity. That pairing is enforced at boot.</para>
        ///
        /// <para>Declared after <see cref="CameraTiltDegrees"/> deliberately: the pairing check reads
        /// that value, and a <c>static readonly</c> field initialises in textual order. Reading a
        /// [GT] declared below would see zero and pass the check vacuously — the
        /// <c>PerceptionConstants.BASE_FOV_HALF_ANGLE</c> defect, which has shipped in this project
        /// three times.</para>
        /// </summary>
        public static readonly float CameraVerticalFovDegrees = RequireFarRayMeetsGround(
            RequireInRange(
                Config.GetFloat("match-client", "CameraVerticalFovDegrees", 60f), 1f, 170f,
                "CameraVerticalFovDegrees"),
            CameraTiltDegrees);

        #endregion

        #region GT — P4a render-cue sizing (§5-P4a / §7 "Rendering, camera, HUD")

        /// <summary>
        /// [GT] Radius (m) of a filled pitch spot — the centre spot and the two penalty spots. Config
        /// key [match-client] MarkingSpotRadiusM.
        ///
        /// <para>Presentation, not Law 1: IFAB fixes where the spots are (and
        /// <c>MatchViewerConstants.PenaltySpotDistanceM</c> carries that as [FIXED]) but describes
        /// them only as "marks", so how big to draw one is a legibility choice like every other row
        /// in this region.</para>
        /// </summary>
        public static readonly float MarkingSpotRadiusM =
            Config.GetFloat("match-client", "MarkingSpotRadiusM", 0.2f);

        /// <summary>
        /// [GT] Radius (view units, 1 unit = 1 m) of an agent's marker. Config key [match-client]
        /// AgentMarkerRadiusM.
        ///
        /// <para>Deliberately larger than a person: the default camera shows a 52 m span
        /// (2 × <see cref="CameraViewHalfWidthM"/>), so on a 1920-px-wide view one metre is ~37 px
        /// and a life-sized 0.25 m-radius marker is ~9 px across — a dot with no room for the shirt
        /// number drawn inside it. This is a legibility figure, not an anthropometric one; nothing
        /// in the simulation reads it.</para>
        /// </summary>
        public static readonly float AgentMarkerRadiusM =
            Config.GetFloat("match-client", "AgentMarkerRadiusM", 0.7f);

        /// <summary>
        /// [GT] Radius (view units) of the ring drawn around the agent in possession. Config key
        /// [match-client] PossessionRingRadiusM. Must exceed <see cref="AgentMarkerRadiusM"/> or the
        /// ring is hidden underneath the marker it annotates — enforced at boot rather than
        /// documented, so a config that breaks it fails loud instead of drawing nothing.
        /// </summary>
        public static readonly float PossessionRingRadiusM = RequireGreaterThan(
            Config.GetFloat("match-client", "PossessionRingRadiusM", 1.2f),
            AgentMarkerRadiusM, "PossessionRingRadiusM", "AgentMarkerRadiusM");

        /// <summary>[GT] Radius (view units) of the ball marker at ground level. Config key [match-client] BallMarkerRadiusM.</summary>
        public static readonly float BallMarkerRadiusM =
            Config.GetFloat("match-client", "BallMarkerRadiusM", 0.35f);

        #endregion

        /// <summary>
        /// Returns <paramref name="value"/>, or throws when it is below <paramref name="minimum"/>
        /// (a non-finite value is below every minimum and so is refused too).
        /// </summary>
        /// <exception cref="InvalidOperationException">The configured value is out of range. It
        /// surfaces as a <c>TypeInitializationException</c> at first use of this catalogue, which is
        /// the same fail-at-boot shape <c>GameplayConfigFileLoader</c> gives an unparseable value.</exception>
        internal static float RequireAtLeast(float value, float minimum, string key)
        {
            if (!(value >= minimum))
            {
                throw new InvalidOperationException(
                    "[match-client] " + key + " is " + Inv(value) + "; it must be at least " +
                    Inv(minimum) + ".");
            }

            return value;
        }

        /// <summary>
        /// Returns <paramref name="value"/>, or throws when it is NaN or infinite. For a dial whose
        /// sign is meaningful, so no range bounds it, but which still cannot be non-finite.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configured value is not finite.</exception>
        internal static float RequireFinite(float value, string key)
        {
            if (!float.IsFinite(value))
            {
                throw new InvalidOperationException(
                    "[match-client] " + key + " is " + Inv(value) + "; it must be a finite number.");
            }

            return value;
        }

        /// <summary>
        /// Returns <paramref name="fovDegrees"/>, or throws when the camera's lowest ray
        /// (<c>tilt + fov/2</c> from vertical) reaches or passes the horizontal. Past that the ray
        /// never meets the ground: the horizon is in shot and the visible ground runs to infinity, so
        /// no framing figure means anything. Two individually-legal dials can pair into it, which is
        /// why this is a pairing check rather than two range checks.
        /// </summary>
        /// <exception cref="InvalidOperationException">The pair puts the horizon in shot.</exception>
        internal static float RequireFarRayMeetsGround(float fovDegrees, float tiltDegrees)
        {
            float farRay = tiltDegrees + fovDegrees * 0.5f;

            if (!(farRay < 90f))
            {
                throw new InvalidOperationException(
                    "[match-client] CameraTiltDegrees (" + Inv(tiltDegrees) +
                    ") + CameraVerticalFovDegrees/2 (" + Inv(fovDegrees * 0.5f) + ") is " +
                    Inv(farRay) + " degrees from vertical; it must stay under 90, or the camera's " +
                    "lowest ray never meets the ground.");
            }

            return fovDegrees;
        }

        /// <summary>
        /// Returns <paramref name="value"/>, or throws when it does not exceed
        /// <paramref name="floor"/>. For invariants between two constants in this catalogue, which a
        /// per-key range check cannot express.
        /// </summary>
        /// <exception cref="InvalidOperationException">The configured value breaks the invariant.</exception>
        internal static float RequireGreaterThan(float value, float floor, string key, string floorKey)
        {
            if (!(value > floor))
            {
                throw new InvalidOperationException(
                    "[match-client] " + key + " is " + Inv(value) + "; it must exceed " + floorKey +
                    " (" + Inv(floor) + ").");
            }

            return value;
        }

        /// <summary>
        /// Returns <paramref name="value"/>, or throws when it falls outside
        /// [<paramref name="minimum"/>, <paramref name="maximum"/>].
        /// </summary>
        /// <exception cref="InvalidOperationException">The configured value is out of range.</exception>
        internal static float RequireInRange(float value, float minimum, float maximum, string key)
        {
            if (!(value >= minimum) || !(value <= maximum))
            {
                throw new InvalidOperationException(
                    "[match-client] " + key + " is " + Inv(value) + "; it must be within [" +
                    Inv(minimum) + ", " + Inv(maximum) + "].");
            }

            return value;
        }

        private static string Inv(float value) =>
            value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial creation (P0): master-plan playback-speed set          |
// |         |            |        | {1,3,5,10} as [GT] scalars via GameplayConfig. Camera /        |
// |         |            |        | render-cue tuning deferred to P3/P4 with their consumers.      |
// | 1.1     | 2026-07-27 | —      | P3 consumers landed, so their [GT] tuning lands with them: the  |
// |         |            |        | two interpolator snap distances (restart / substitution        |
// |         |            |        | discontinuities) and the five follow-ball camera rows (dead    |
// |         |            |        | zone, exponential follow rate, view half-extents, overscan).   |
// | 1.2     | 2026-08-03 | —      | P4a render model: the render-cue sizes v1.0 deferred "to P3/P4 |
// |         |            |        | with their consumers" now have consumers — agent marker and    |
// |         |            |        | possession-ring radii, ball marker radius, and the three ball-  |
// |         |            |        | height cues (view offset per metre, scale per metre, scale cap).|
// | 1.3     | 2026-08-04 | —      | AR pass M-4/M-5/L-9: boot-time validation replaces silent      |
// |         |            |        | repair and undocumented "musts" — BallMaxHeightScale must be   |
// |         |            |        | >= 1 and PossessionRingRadiusM must exceed AgentMarkerRadiusM, |
// |         |            |        | both enforced, per the [GT] loader's fail-loud contract. The   |
// |         |            |        | BallMaxHeightScale and AgentMarkerRadiusM rationales carried   |
// |         |            |        | fabricated figures (a 20 m ball is 2.8 m across, not "wider    |
// |         |            |        | than the penalty area"; a 0.25 m marker is ~9 px, not one);    |
// |         |            |        | both replaced with checked numbers, and the cap's 10 m         |
// |         |            |        | saturation limitation is now stated rather than left implicit. |
// | 1.4     | 2026-08-04 | —      | Tilted-view revision (owner call): the three ball-height dials  |
// |         |            |        | are GONE — BallHeightViewOffsetPerMetre, BallHeightScalePerMetre|
// |         |            |        | and BallMaxHeightScale existed only to fake altitude on a flat  |
// |         |            |        | plane, and a tilted camera conveys it for free. In their place  |
// |         |            |        | the camera rig's dials: CameraHeightM, CameraTiltDegrees (from  |
// |         |            |        | VERTICAL) and CameraLateralOffsetM, plus RequireInRange for the |
// |         |            |        | tilt's bound. CameraViewHalfWidth/HalfHeightM keep their values |
// |         |            |        | but are now documented as APPROXIMATE: they describe a          |
// |         |            |        | rectangle of visible ground where a tilted view sees a          |
// |         |            |        | trapezoid.                                                      |
// |         |            |        | (Row written 2026-08-04 in the following AR pass — the v1.4     |
// |         |            |        | edit landed without one, so v1.3 was left as the newest row     |
// |         |            |        | while describing constants the file no longer had.)             |
// | 1.5     | 2026-08-04 | —      | AR pass 2, H-1/M-3: + CameraVerticalFovDegrees. Height and tilt |
// |         |            |        | placed the camera but nothing said how much it SEES, so P4b     |
// |         |            |        | would have picked a field of view inside a MonoBehaviour — a    |
// |         |            |        | framing decision in the one place the gate cannot compile,      |
// |         |            |        | which is what the P4a/P4b split exists to prevent. Its bound is |
// |         |            |        | paired with the tilt (tilt + fov/2 < 90, or the lowest ray      |
// |         |            |        | never meets the ground and the visible area is unbounded).      |
// |         |            |        | M-3: CameraLateralOffsetM was the only camera dial with no      |
// |         |            |        | validation at all, and it lands straight in the camera's world  |
// |         |            |        | position — now RequireFinite (either sign is meaningful, so a   |
// |         |            |        | range would be wrong).                                          |
#endregion
