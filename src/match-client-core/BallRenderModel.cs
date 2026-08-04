// File:     src/match-client-core/BallRenderModel.cs
// Created:  2026-08-03
// Modified: 2026-08-04
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
        /// Where the ball is drawn, in world space — the pitch point it is over, lifted by its
        /// height along +Y.
        /// </summary>
        public readonly Vector3 WorldPosition;

        /// <summary>
        /// Where the shadow is drawn: the same pitch point, on the ground plane (world Y = 0). Equal
        /// to <see cref="WorldPosition"/> when the ball is on the turf.
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
#endregion
