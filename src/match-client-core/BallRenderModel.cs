// File:     src/match-client-core/BallRenderModel.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a
//           "ball (sprite + shadow for height)"), Ball Physics #1 §1.2, Code Standards #20
// Purpose:  The ball's resolved draw state: where its shadow sits, where its sprite sits, and how
//           big the sprite is — the three values a top-down 2D view needs to show height.

using UnityEngine;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// The ball's fully-resolved draw state (§5-P4a).
    ///
    /// <para><b>Height in a top-down view.</b> The view plane has no third axis to put Z on, so a
    /// lofted ball is drawn as two things separating: the shadow stays at the pitch point the ball
    /// is over, and the sprite lifts away from it and grows. That keeps the shadow — not the sprite
    /// — at the position every gameplay judgement was actually made against, which matters the
    /// moment a viewer tries to read whether a cross cleared a defender.</para>
    ///
    /// <para>The scale is capped (<c>MatchClientConstants.BallMaxHeightScale</c>): a goal kick peaks
    /// around 20 m, and an uncapped ramp would draw the ball wider than the six-yard box.</para>
    /// </summary>
    public readonly struct BallRenderModel
    {
        /// <summary>
        /// Where the shadow is drawn: the ball's ground point, in the centre-origin view plane. This
        /// is the ball's true pitch position with the height component dropped.
        /// </summary>
        public readonly Vector2 ShadowPosition;

        /// <summary>
        /// Where the ball sprite is drawn: <see cref="ShadowPosition"/> lifted along the view's +Y
        /// axis in proportion to height. Equal to the shadow position when the ball is on the ground.
        /// </summary>
        public readonly Vector2 SpritePosition;

        /// <summary>Ball height above the ground, metres — as reported by the engine, not clamped.</summary>
        public readonly float HeightM;

        /// <summary>Ball sprite radius in view units, already grown for height and capped.</summary>
        public readonly float SpriteRadius;

        /// <summary>Shadow radius in view units — the ground-level ball size, which does not grow with height.</summary>
        public readonly float ShadowRadius;

        /// <summary>Constructs the ball's draw state. Built by <see cref="MatchRenderProjection"/>.</summary>
        public BallRenderModel(
            Vector2 shadowPosition,
            Vector2 spritePosition,
            float heightM,
            float spriteRadius,
            float shadowRadius)
        {
            ShadowPosition = shadowPosition;
            SpritePosition = spritePosition;
            HeightM        = heightM;
            SpriteRadius   = spriteRadius;
            ShadowRadius   = shadowRadius;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): shadow point, lifted sprite point, and  |
// |         |            |        | the height-grown (capped) sprite radius.                        |
#endregion
