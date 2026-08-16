// File:     src/match-client-core/BallRenderModel.cs
// Created:  2026-08-03
// Modified: 2026-08-16 (P4b AR round 5, M23: WorldPosition/ShadowPosition docs restated for the
//           raised floor — radius plus the topmost M12/M16 ground layer, not the radius alone)
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a
//           "ball (+ ground shadow)"), Ball Physics #1 §1.2, Code Standards #20
// Purpose:  The ball's resolved draw state: where the ball is in world space, where its shadow sits
//           on the ground beneath it, and how big to draw both.

using UnityEngine;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// The ball's fully-resolved draw state (§5-P4a).
    ///
    /// <para><b>Height is a real axis now.</b> Under the tilted camera the client uses, a lofted ball
    /// is simply placed higher in the world and the camera projects it — so there is no size ramp, no
    /// scale ceiling, and no faked vertical offset. The earlier flat-view model needed all three to
    /// suggest height on a plane that had nowhere to put it; a perspective camera does the job
    /// without a tuning dial, and does it correctly at every altitude instead of saturating.</para>
    ///
    /// <para><b>The shadow still earns its place.</b> With any tilt at all, a high ball's screen
    /// position separates from the pitch point it is actually over — which is the point every
    /// gameplay judgement was made against. The shadow marks that point. It is the one cue the
    /// camera cannot supply on its own, which is why it survived the simplification when the size and
    /// offset cues did not.</para>
    ///
    /// <para>Radii are constants, deliberately: a ball only has to be visible on the pitch and above
    /// it, and the projection handles apparent size from there.</para>
    /// </summary>
    public readonly struct BallRenderModel
    {
        /// <summary>
        /// Where the ball is drawn, in world space — the pitch point it is over, lifted along +Y by
        /// its height, THEN floored so the drawn sphere clears everything drawn on the turf (M17,
        /// raised at M23): <c>Y == max(HeightM sanitised to non-negative, Radius +
        /// MatchClientConstants.AgentMarkerLayerHeightM)</c>. <see cref="Radius"/> is a legibility
        /// figure (0.35 m by default), not the engine's own ~0.11 m ball, and the prefab draws a
        /// genuine sphere of that radius centred here — so below that floor, this is NOT the raw
        /// physics height: it is the lowest Y at which a sphere of that size can sit without its
        /// underside sinking through the ground.
        ///
        /// <para>M23: the added term is the HIGHEST of the four ordered M12/M16 ground layers. M17
        /// floored on <see cref="Radius"/> alone, which clears the bare ground PLANE but not the
        /// layers drawn above it — and round 4's M19 rescaled those layers from millimetres to
        /// centimetres, putting the ball's own shadow (0.08 m), the possession ring (0.081 m) and the
        /// agent marker (0.082 m) all INSIDE a 0.35 m sphere floored at 0.35 m.</para>
        /// </summary>
        public readonly Vector3 WorldPosition;

        /// <summary>
        /// Where the shadow is drawn: the same pitch point, on the ground plane (world Y = 0) — the
        /// TRUE ground point the ball is over, unaffected by <see cref="WorldPosition"/>'s M17/M23
        /// floor. No longer equal to <see cref="WorldPosition"/> even when the ball is on the turf
        /// (whenever <see cref="Radius"/> is positive, as every configured default is): at rest the
        /// floor keeps <see cref="WorldPosition"/>'s Y at 0.432 m (0.35 m radius + the 0.082 m agent-
        /// marker layer, at the shipped defaults) while this stays at 0 — the gap between the two IS
        /// the visual cue that the ball is sitting on the ground, not floating at its drawn size's
        /// natural centre height. The renderer lifts the shadow onto its OWN ground layer
        /// (<c>BallShadowLayerHeightM</c>) when it places it; this field is the unlifted ground point.
        /// </summary>
        public readonly Vector3 ShadowPosition;

        /// <summary>
        /// Ball height above the ground in metres, <b>as reported by the engine — not clamped or
        /// sanitised</b>. <see cref="WorldPosition"/> treats a negative or non-finite height as
        /// ground level, so a consumer reading this field for anything but display must guard it.
        /// </summary>
        public readonly float HeightM;

        /// <summary>Ball radius in world units (1 unit = 1 m). Constant — perspective does the rest.</summary>
        public readonly float Radius;

        /// <summary>Shadow radius in world units. Constant, and equal to <see cref="Radius"/> today.</summary>
        public readonly float ShadowRadius;

        /// <summary>Constructs the ball's draw state. Built by <see cref="MatchRenderProjection"/>.</summary>
        public BallRenderModel(
            Vector3 worldPosition,
            Vector3 shadowPosition,
            float heightM,
            float radius,
            float shadowRadius)
        {
            WorldPosition  = worldPosition;
            ShadowPosition = shadowPosition;
            HeightM        = heightM;
            Radius         = radius;
            ShadowRadius   = shadowRadius;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): shadow point, lifted sprite point, and  |
// |         |            |        | the height-grown (capped) sprite radius.                        |
// | 1.1     | 2026-08-04 | —      | AR pass M-5: the cap's stated rationale was numerically wrong   |
// |         |            |        | ("wider than the six-yard box" — an uncapped 20 m ball is 2.8 m |
// |         |            |        | across). Replaced with the real figures plus the 10 m           |
// |         |            |        | saturation point, and HeightM is documented as unsanitised.     |
// | 1.2     | 2026-08-04 | —      | Tilted-view revision (owner call): height is a real world axis  |
// |         |            |        | under a tilted camera, so SpritePosition/SpriteRadius and the   |
// |         |            |        | whole size ramp are GONE — with them the three [GT] dials and   |
// |         |            |        | the v1.1 saturation limitation they were tuned against. What    |
// |         |            |        | remains is a world position, a ground shadow (which perspective |
// |         |            |        | cannot supply and which marks where the ball actually is), and  |
// |         |            |        | two constant radii.                                             |
// | 1.3     | 2026-08-16 | —      | P4b AR round 4, M21: doc-only. Round 3's M17 (see               |
// |         |            |        | MatchRenderProjection.ProjectBall) floored WorldPosition.Y on   |
// |         |            |        | Radius instead of the raw height, but this file's XML docs were |
// |         |            |        | never updated to match — ShadowPosition still claimed to equal  |
// |         |            |        | WorldPosition "when the ball is on the turf" (now never true:   |
// |         |            |        | WorldPosition.Y sits at Radius, ShadowPosition.Y stays 0), and   |
// |         |            |        | WorldPosition's own doc still said only "lifted by its height"  |
// |         |            |        | with no mention of the floor. Both corrected to state the exact |
// |         |            |        | Y == max(sanitised HeightM, Radius) relationship.                |
// | 1.4     | 2026-08-16 | —      | P4b AR round 5, M23: doc-only, tracking the raised floor in     |
// |         |            |        | MatchRenderProjection.ProjectBall. The v1.3 rows above stated   |
// |         |            |        | max(sanitised HeightM, Radius), which cleared only the bare     |
// |         |            |        | ground plane; round 4's M19 rescaled the four M12/M16 ground    |
// |         |            |        | layers to centimetres, so the formula is now max(sanitised      |
// |         |            |        | HeightM, Radius + AgentMarkerLayerHeightM) — 0.432 m at rest on |
// |         |            |        | the shipped defaults, not 0.35 m. ShadowPosition's doc also now |
// |         |            |        | says explicitly that the renderer lifts the shadow onto         |
// |         |            |        | BallShadowLayerHeightM itself; this field stays the unlifted    |
// |         |            |        | true ground point.                                              |
#endregion
