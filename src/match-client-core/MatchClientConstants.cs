// File:     src/match-client-core/MatchClientConstants.cs
// Created:  2026-07-24
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P0/§5-P3/§5-P4a),
//           Code Standards #20 (constant catalogue; no magic numbers)
// Purpose:  Constant catalogue for the host-free interactive-client core: the master-plan
//           playback-speed set the UI presents (Pause is a streamer state, not a multiplier), the P3
//           interpolator snap distances, the P3 follow-ball camera tuning, and the P4a render-cue sizes.

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

        /// <summary>
        /// [GT] View-plane offset applied to the ball sprite per metre of ball height, along the
        /// view's +Y axis. Config key [match-client] BallHeightViewOffsetPerMetre.
        ///
        /// <para>A top-down 2D view has nowhere to put Z, so height is drawn as separation between
        /// the ball sprite and its ground shadow — the shadow stays on the pitch point the ball is
        /// actually over, which is the position every gameplay judgement was made against.</para>
        /// </summary>
        public static readonly float BallHeightViewOffsetPerMetre =
            Config.GetFloat("match-client", "BallHeightViewOffsetPerMetre", 0.5f);

        /// <summary>
        /// [GT] Extra ball-marker scale per metre of ball height. Config key [match-client]
        /// BallHeightScalePerMetre. The 2D analogue of the browser viewer's
        /// <c>BallRadiusPerMetreHeightPx</c> cue, expressed as a multiplier rather than pixels
        /// because the Unity view has no fixed pixels-per-metre.
        /// </summary>
        public static readonly float BallHeightScalePerMetre =
            Config.GetFloat("match-client", "BallHeightScalePerMetre", 0.15f);

        /// <summary>
        /// [GT] Ceiling on the height-derived ball scale. Config key [match-client] BallMaxHeightScale.
        ///
        /// <para>At the shipped values the ramp reaches this ceiling at
        /// (<see cref="BallMaxHeightScale"/> − 1) ÷ <see cref="BallHeightScalePerMetre"/> = <b>10 m</b>,
        /// and a goal kick peaks around 20 m — where an uncapped ramp would draw the ball at
        /// 0.35 × 4 = 1.4 m radius, i.e. 2.8 m across. That is roughly two marker-widths and reads
        /// as a beach ball rather than as height, which is what the cap is for. <b>Known limitation:
        /// above 10 m the sprite stops growing, so the height cue saturates for the upper half of a
        /// goal kick's arc</b> — the shadow separation (<see cref="BallHeightViewOffsetPerMetre"/>)
        /// keeps conveying height past that point, and retuning the pair is a [GT] balance decision,
        /// not a code one.</para>
        ///
        /// <para>Must be at least 1: a ceiling below 1 would shrink a grounded ball. Enforced at
        /// boot, per the [GT] loader's fail-loud contract — a value that cannot mean what it says is
        /// a config error, not something to silently repair into "no cap".</para>
        /// </summary>
        public static readonly float BallMaxHeightScale = RequireAtLeast(
            Config.GetFloat("match-client", "BallMaxHeightScale", 2.5f), 1f, "BallMaxHeightScale");

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
#endregion
