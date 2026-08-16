// File:     src/match-client-core/MatchClientConstants.cs
// Created:  2026-07-24
// Modified: 2026-08-16 (P4b AR round 3, M16: + MarkingLayerStepM and its
//           RequireMarkingBandFitsBelowShadowLayer pairing check)
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P0/§5-P3/§5-P4a/§5-P4b/§5-P5),
//           Code Standards #20 (constant catalogue; no magic numbers)
// Purpose:  Constant catalogue for the host-free interactive-client core: the master-plan
//           playback-speed set the UI presents (Pause is a streamer state, not a multiplier), the P3
//           interpolator snap distances, the P3 follow-ball camera tuning, the P4a camera rig
//           (height, tilt, lateral offset, field of view), and the P4a render-cue sizes.

using System;

using TacticalDirector.MatchViewer;

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
        public static readonly float Speed1x = RequireStreamerAcceptsSpeed(
            Config.GetFloat("match-client", "Speed1x", 1f), "Speed1x");

        /// <summary>[GT] Fast playback multiplier (3×). Config key [match-client] Speed3x.</summary>
        public static readonly float Speed3x = RequireStreamerAcceptsSpeed(
            Config.GetFloat("match-client", "Speed3x", 3f), "Speed3x");

        /// <summary>[GT] Faster playback multiplier (5×). Config key [match-client] Speed5x.</summary>
        public static readonly float Speed5x = RequireStreamerAcceptsSpeed(
            Config.GetFloat("match-client", "Speed5x", 5f), "Speed5x");

        /// <summary>[GT] Fastest playback multiplier (10×). Config key [match-client] Speed10x. Requires <c>MatchViewerConstants.MaxLiveSpeedMultiplier</c> ≥ 10 (§5-P0) — now enforced at load rather than left as prose.</summary>
        public static readonly float Speed10x = RequireStreamerAcceptsSpeed(
            Config.GetFloat("match-client", "Speed10x", 10f), "Speed10x");

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
        ///
        /// <para>L8: also paired against <see cref="CameraTiltDegrees"/> via
        /// <see cref="RequireTiltOrOffsetNonzero"/> — each dial is individually legal at zero, but
        /// BOTH at zero sits the camera directly above its look-at target, where
        /// <c>Transform.LookAt</c>'s forward is antiparallel to the default up vector, an undefined
        /// rotation. Declared after <see cref="CameraTiltDegrees"/> deliberately, for the same
        /// static-init-order reason <see cref="CameraVerticalFovDegrees"/> is declared after it too —
        /// a pairing check that read a [GT] declared below it would see zero and pass vacuously.</para>
        /// </summary>
        public static readonly float CameraLateralOffsetM = RequireTiltOrOffsetNonzero(
            RequireFinite(Config.GetFloat("match-client", "CameraLateralOffsetM", 5f), "CameraLateralOffsetM"),
            CameraTiltDegrees);

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
        /// [GT] Width (m) of a drawn marking line — the boundary, the halfway line and the end-box
        /// edges (not the goal mouth; see <see cref="GoalMouthWidthM"/>). Config key [match-client]
        /// MarkingLineWidthM.
        ///
        /// <para>Presentation, not Law 1, like <see cref="MarkingSpotRadiusM"/> above: IFAB caps a
        /// real line at 12 cm, and at the default framing (a 52 m span,
        /// 2 × <see cref="CameraViewHalfWidthM"/>, so ~37 px per metre on a 1920-px-wide view) that is
        /// about 4 px — legible standing still, but a line that thin crawls and drops out as the
        /// follow camera moves. The drawn line is deliberately wider than the painted one; nothing in
        /// the simulation reads it.</para>
        ///
        /// <para>It exists as a constant at all because the binding assigns it: a marking prefab is
        /// authored at unit cross-section, so without this the width is whatever the prefab happened
        /// to be scaled to — a tuning value living in a scene asset, where neither the gate nor the
        /// config file can see it.</para>
        ///
        /// <para>M10/L7: bounded to a small positive span — a marking line "shouldn't sanely exceed a
        /// metre or so" of drawn width, and zero or negative would make the boundary invisible while
        /// every gate above still reported success.</para>
        /// </summary>
        public static readonly float MarkingLineWidthM = RequireInRange(
            Config.GetFloat("match-client", "MarkingLineWidthM", 0.2f), 0.01f, 1f, "MarkingLineWidthM");

        /// <summary>
        /// [GT] Width (m) of the drawn goal mouth — post to post. Config key [match-client]
        /// GoalMouthWidthM.
        ///
        /// <para>M10: <see cref="TacticalDirector.MatchClientCore.PitchMarkingKind.GoalMouth"/>'s own
        /// doc is explicit that it is "drawn heavier than a marking line... goal furniture, not a
        /// Law 1 marking" — the round-1 binding shared <see cref="MarkingLineWidthM"/> and
        /// <c>_markingLinePrefab</c> for both cases, which is exactly the "may want it in a different
        /// colour or as a mesh" distinction that member's doc calls out, collapsed back to one. This
        /// is the goal mouth's own dial, deliberately wider by default so it reads as furniture
        /// rather than turf paint.</para>
        /// </summary>
        public static readonly float GoalMouthWidthM = RequireInRange(
            Config.GetFloat("match-client", "GoalMouthWidthM", 0.3f), 0.01f, 1f, "GoalMouthWidthM");

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
        /// [GT] Blend factor (0 = no change, 1 = solid white) lightening a goalkeeper's marker
        /// towards white. Config key [match-client] GoalkeeperTintFactor.
        ///
        /// <para>AR pass M-2 over the P4b binding: <c>AgentRenderModel.IsGoalkeeper</c> was resolved
        /// upstream but read by nobody. A shade of the agent's own team colour needs no new palette
        /// entry or prefab slot — <c>UnityEngine.Color</c> is not in the CI shim's surface (see
        /// <c>AgentRenderModel</c>'s class doc), so the blend itself lives in the binding; this dial
        /// is just how far it goes.</para>
        /// </summary>
        public static readonly float GoalkeeperTintFactor = RequireInRange(
            Config.GetFloat("match-client", "GoalkeeperTintFactor", 0.35f), 0f, 1f, "GoalkeeperTintFactor");

        /// <summary>
        /// [GT] Blend factor (0 = no change, 1 = solid black) darkening a sent-off player's marker
        /// towards black. Config key [match-client] SentOffTintFactor.
        ///
        /// <para>Same AR pass M-2 as <see cref="GoalkeeperTintFactor"/> above:
        /// <c>AgentRenderModel.IsSentOff</c> documents that a dismissed player keeps a position on
        /// the pitch and must be MARKED, not hidden or drawn identically to a player still in the
        /// game — a darkened marker is that mark.</para>
        /// </summary>
        public static readonly float SentOffTintFactor = RequireInRange(
            Config.GetFloat("match-client", "SentOffTintFactor", 0.55f), 0f, 1f, "SentOffTintFactor");

        #endregion

        #region GT — ground-layer heights (M12: coplanar ground objects z-fight)

        /// <summary>
        /// [GT] World-Y height (m) at which pitch markings are drawn. Config key [match-client]
        /// MarkingLayerHeightM.
        ///
        /// <para>M12: four ground layers — markings, the ball's shadow, the possession ring and the
        /// agent marker — were all placed at world Y = 0, which z-fights in a real renderer wherever
        /// two of them overlap (a shadow crossing a painted line; a ring sitting under the marker it
        /// annotates). Zero is legal here and is today's default: a painted line sits ON the turf, and
        /// every layer above this one is defined to sit strictly above it (see the three
        /// <c>RequireGreaterThan</c> checks below), so this is the one layer nothing else needs to
        /// clear.</para>
        /// </summary>
        public static readonly float MarkingLayerHeightM =
            RequireAtLeast(Config.GetFloat("match-client", "MarkingLayerHeightM", 0f), 0f, "MarkingLayerHeightM");

        /// <summary>
        /// [GT] World-Y height (m) at which the ball's ground shadow is drawn. Config key
        /// [match-client] BallShadowLayerHeightM. Must exceed <see cref="MarkingLayerHeightM"/> — the
        /// shadow is an opaque disc that regularly crosses a painted line (a ball rolling over the
        /// six-yard box, say), and two coplanar opaque surfaces z-fight.
        /// </summary>
        public static readonly float BallShadowLayerHeightM = RequireGreaterThan(
            Config.GetFloat("match-client", "BallShadowLayerHeightM", 0.001f),
            MarkingLayerHeightM, "BallShadowLayerHeightM", "MarkingLayerHeightM");

        /// <summary>
        /// [GT] World-Y height (m) at which the possession ring is drawn. Config key [match-client]
        /// PossessionRingLayerHeightM. Must exceed <see cref="BallShadowLayerHeightM"/> — the ring is
        /// drawn around whichever agent is in possession, and that agent's marker sits over the same
        /// ground point the ball's shadow can also occupy, so the ring is ordered above the shadow
        /// the way it is ordered below the marker (see <see cref="AgentMarkerLayerHeightM"/>).
        /// </summary>
        public static readonly float PossessionRingLayerHeightM = RequireGreaterThan(
            Config.GetFloat("match-client", "PossessionRingLayerHeightM", 0.002f),
            BallShadowLayerHeightM, "PossessionRingLayerHeightM", "BallShadowLayerHeightM");

        /// <summary>
        /// [GT] World-Y height (m) at which an agent marker is drawn. Config key [match-client]
        /// AgentMarkerLayerHeightM. Must exceed <see cref="PossessionRingLayerHeightM"/> — the ring is
        /// explicitly meant to sit visually UNDER the marker it annotates (the marker is the player;
        /// the ring is drawn around him), so the marker is topmost of the four ground layers.
        /// </summary>
        public static readonly float AgentMarkerLayerHeightM = RequireGreaterThan(
            Config.GetFloat("match-client", "AgentMarkerLayerHeightM", 0.003f),
            PossessionRingLayerHeightM, "AgentMarkerLayerHeightM", "PossessionRingLayerHeightM");

        /// <summary>
        /// [GT] Height (m) between successive drawables within the marking layer band (M16). Config
        /// key [match-client] MarkingLayerStepM.
        ///
        /// <para>M12's four ground layers keep markings/shadow/ring/marker from z-fighting BETWEEN
        /// classes, but every entry <see cref="PitchMarkings.BuildDrawables"/> returns was still
        /// placed at the exact same <see cref="MarkingLayerHeightM"/> — so the boundary, the penalty
        /// areas, the goal areas and the goal mouths all overlap at each goal line, the centre spot
        /// sits exactly on the halfway line, and the centre circle crosses it twice. This turns the
        /// marking layer from a plane into a band: drawable index <c>i</c> is placed at
        /// <c>MarkingLayerHeightM + i * MarkingLayerStepM</c> — <see cref="PitchMarkings.BuildDrawables"/>'s
        /// ordering is already a stated, deterministic contract (its own doc), so indexing into it is
        /// safe.</para>
        ///
        /// <para>Bounded so <see cref="PitchMarkings.DRAWABLE_COUNT"/> steps of it stay comfortably
        /// below the clearance to <see cref="BallShadowLayerHeightM"/> (see
        /// <see cref="RequireMarkingBandFitsBelowShadowLayer"/>) — otherwise the top of the marking
        /// band would itself z-fight with the ball's shadow, reopening the exact hazard M12 fixed one
        /// layer up.</para>
        /// </summary>
        public static readonly float MarkingLayerStepM = RequireMarkingBandFitsBelowShadowLayer(
            Config.GetFloat("match-client", "MarkingLayerStepM", 0.00001f),
            PitchMarkings.DRAWABLE_COUNT, MarkingLayerHeightM, BallShadowLayerHeightM, "MarkingLayerStepM");

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
        /// Returns <paramref name="lateralOffsetM"/>, or throws when it is zero AND
        /// <paramref name="tiltDegrees"/> is also zero. Each dial is individually legal at zero — a
        /// straight-down tilt, or dead-centre framing — but together they sit the camera directly
        /// above its look-at target, where <c>Transform.LookAt</c>'s forward direction is antiparallel
        /// to the default world-up vector: an undefined/degenerate rotation. Two individually-legal
        /// dials pairing into an illegal camera is the same shape <see cref="RequireFarRayMeetsGround"/>
        /// guards, which is why this follows it rather than being folded into either dial's own range
        /// check.
        /// </summary>
        /// <exception cref="InvalidOperationException">Both dials are zero.</exception>
        internal static float RequireTiltOrOffsetNonzero(float lateralOffsetM, float tiltDegrees)
        {
            if (tiltDegrees == 0f && lateralOffsetM == 0f)
            {
                throw new InvalidOperationException(
                    "[match-client] CameraTiltDegrees is 0 and CameraLateralOffsetM is 0; the camera " +
                    "would sit directly above its look-at target, making Transform.LookAt's forward " +
                    "antiparallel to the default up vector — an undefined rotation. Set at least one " +
                    "of the two config keys away from zero.");
            }

            return lateralOffsetM;
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
        /// Returns <paramref name="stepM"/>, or throws when <paramref name="drawableCount"/> steps of
        /// it would push the TOP of the marking band (M16) past the MIDPOINT of the clearance between
        /// <paramref name="baseHeightM"/> and <paramref name="shadowHeightM"/> — "comfortably below"
        /// the shadow layer, not merely under it. Two individually-legal values (a small step, a large
        /// drawable count) can pair into a band that reaches the next ground layer up, which is the
        /// same shape <see cref="RequireFarRayMeetsGround"/> and <see cref="RequireTiltOrOffsetNonzero"/>
        /// guard, so this follows their shape rather than being folded into a per-key range check.
        /// </summary>
        /// <exception cref="InvalidOperationException">The band does not fit comfortably below the
        /// shadow layer.</exception>
        internal static float RequireMarkingBandFitsBelowShadowLayer(
            float stepM, int drawableCount, float baseHeightM, float shadowHeightM, string key)
        {
            float bandTopM = baseHeightM + stepM * drawableCount;
            float halfClearanceM = baseHeightM + (shadowHeightM - baseHeightM) * 0.5f;

            if (!(bandTopM < halfClearanceM))
            {
                throw new InvalidOperationException(
                    "[match-client] " + key + " (" + Inv(stepM) + ") * PitchMarkings.DRAWABLE_COUNT (" +
                    Inv(drawableCount) + ") puts the top of the marking band at " + Inv(bandTopM) +
                    "m, which is not comfortably below BallShadowLayerHeightM (" + Inv(shadowHeightM) +
                    "m) — it must stay under the midpoint of the clearance above MarkingLayerHeightM (" +
                    Inv(halfClearanceM) + "m), or the highest drawable in the marking band would " +
                    "z-fight with the ball's shadow.");
            }

            return stepM;
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

        /// <summary>
        /// Returns <paramref name="multiplier"/>, or throws when the streamer would refuse it — i.e.
        /// when it falls outside [<c>MatchViewerConstants.MinLiveSpeedMultiplier</c>,
        /// <c>MatchViewerConstants.MaxLiveSpeedMultiplier</c>].
        ///
        /// <para>A pairing check across two catalogues, in the shape of
        /// <see cref="RequireFarRayMeetsGround"/>: each dial is individually legal, and it is the pair
        /// that is wrong. <c>LiveMatchStreamer.SetSpeedMultiplier</c> already fail-louds on an
        /// out-of-range multiplier, so without this the failure surfaces the first time a viewer
        /// clicks that speed button, mid-match — and only that button, so a cap misconfigured to 5
        /// ships a 10× control that throws and a 1×/3×/5× set that works. Checking at load turns "the
        /// fastest button crashes during a match" into "the process refuses to start", which is the
        /// §5-P0 note about the cap needing to be ≥ 10 turned from prose into an assertion.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException">The streamer would reject the multiplier.</exception>
        internal static float RequireStreamerAcceptsSpeed(float multiplier, string key)
        {
            float minimum = MatchViewerConstants.MinLiveSpeedMultiplier;
            float maximum = MatchViewerConstants.MaxLiveSpeedMultiplier;

            if (!(multiplier >= minimum) || !(multiplier <= maximum))
            {
                throw new InvalidOperationException(
                    "[match-client] " + key + " is " + Inv(multiplier) +
                    ", which LiveMatchStreamer.SetSpeedMultiplier would reject: [match-viewer] " +
                    "MinLiveSpeedMultiplier/MaxLiveSpeedMultiplier admit only [" + Inv(minimum) +
                    ", " + Inv(maximum) + "]. Raise the cap or lower the speed step.");
            }

            return multiplier;
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
// | 1.6     | 2026-08-07 | —      | §5-P5 host-free half: the four playback speeds are now paired-  |
// |         |            |        | checked against the streamer's own [Min,Max] cap at load        |
// |         |            |        | (RequireStreamerAcceptsSpeed). SetSpeedMultiplier already       |
// |         |            |        | fail-louds, so a cap below a step used to surface as one speed  |
// |         |            |        | button throwing mid-match while the others worked — the §5-P0   |
// |         |            |        | "cap must be >= 10" note was prose with nothing enforcing it.   |
// | 1.7     | 2026-08-15 | —      | P4b AR pass H-2: + MarkingLineWidthM. The binding set a marking |
// |         |            |        | line's LENGTH and left its width at whatever localScale.x the   |
// |         |            |        | prefab happened to carry — a tuning value living in a scene     |
// |         |            |        | asset, invisible to the gate and unreachable from the config    |
// |         |            |        | file. Now assigned explicitly from here.                        |
// | 1.8     | 2026-08-15 | —      | P4b AR pass M-2: + GoalkeeperTintFactor / SentOffTintFactor.    |
// |         |            |        | AgentRenderModel.IsGoalkeeper/IsSentOff were resolved upstream  |
// |         |            |        | but read by nobody; both are now bound as a lighten/darken of   |
// |         |            |        | the agent's own team colour, so no new palette entry or prefab  |
// |         |            |        | slot is needed (Color is not in the CI shim's surface, so the   |
// |         |            |        | blend itself lives in MatchClientBehaviour — these are only the |
// |         |            |        | [GT] amounts).                                                  |
// | 1.9     | 2026-08-15 | —      | P4b AR round 2, Medium/Low pass. M10: + GoalMouthWidthM — the   |
// |         |            |        | goal mouth is goal furniture, not a Law 1 marking line          |
// |         |            |        | (PitchMarkingKind.GoalMouth's own doc), and now has its own     |
// |         |            |        | drawn width instead of sharing MarkingLineWidthM. M12: + the    |
// |         |            |        | four ground-layer height [GT]s (Marking/BallShadow/             |
// |         |            |        | PossessionRing/AgentMarker), millimetre-scale and strictly      |
// |         |            |        | ascending via the existing RequireGreaterThan chain shape, so   |
// |         |            |        | the four coplanar ground layers a real renderer would z-fight   |
// |         |            |        | (markings, the ball's shadow, the possession ring, the agent    |
// |         |            |        | marker) resolve to a named, boot-validated ordering instead of  |
// |         |            |        | an inline literal the binding would otherwise have to invent.   |
// |         |            |        | L7: MarkingLineWidthM (and the new GoalMouthWidthM from the     |
// |         |            |        | start) now go through RequireInRange like every other [GT]      |
// |         |            |        | render-cue sibling, instead of an unvalidated Config.GetFloat.  |
// |         |            |        | L8: + RequireTiltOrOffsetNonzero, the CameraTiltDegrees /       |
// |         |            |        | CameraLateralOffsetM pairing check in RequireFarRayMeetsGround's|
// |         |            |        | own shape — both dials are individually legal at zero, but      |
// |         |            |        | together they put the camera directly above its look-at target,|
// |         |            |        | where Transform.LookAt's forward is antiparallel to the default |
// |         |            |        | up vector.                                                       |
// | 1.10    | 2026-08-16 | —      | P4b AR round 3, M16: + MarkingLayerStepM and                    |
// |         |            |        | RequireMarkingBandFitsBelowShadowLayer. The four M12 ground-     |
// |         |            |        | layer heights stopped markings z-fighting BETWEEN classes, but  |
// |         |            |        | every PitchMarkings.BuildDrawables() entry still landed at the  |
// |         |            |        | exact same MarkingLayerHeightM, so drawables z-fought WITHIN     |
// |         |            |        | their own class (goal-line overlaps, the centre spot on the     |
// |         |            |        | halfway line, the centre circle crossing it twice). The new     |
// |         |            |        | [GT] turns the marking layer into a band; its pairing check     |
// |         |            |        | follows RequireFarRayMeetsGround's shape — the band's TOP must  |
// |         |            |        | stay comfortably (under the midpoint of the clearance) below    |
// |         |            |        | BallShadowLayerHeightM, or it would reopen the exact M12 hazard  |
// |         |            |        | one layer up.                                                    |
#endregion
