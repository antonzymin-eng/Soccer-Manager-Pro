// File:     src/match-client-core/PitchMarking.cs
// Created:  2026-08-03
// Modified: 2026-08-16 (P4b AR round 5, M25: the class doc's "at zero height" placement instruction
//           is corrected to the M16 marking BAND it was replaced by two rounds ago)
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a, §7
//           "Reuse the geometry that already exists"), Ball Physics #1 §1.2 (corner-origin frame),
//           Code Standards #20
// Purpose:  One pitch marking as a shape plus up to three geometric values, in corner-origin pitch
//           metres. The unit the render skin instantiates one drawing primitive per.

using UnityEngine;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// One IFAB pitch marking, in <b>corner-origin pitch metres</b> (Ball Physics #1 §1.2) — the same
    /// frame the engine reports positions in. A renderer places each point on the ground plane
    /// through <see cref="PitchViewProjection.ToWorld"/>, whose height argument is what lays a marking
    /// ON the turf rather than standing it upright in the world's XY plane.
    /// <see cref="Radius"/> needs no conversion, since the world is 1 unit per metre.
    ///
    /// <para><b>That height is a BAND, not zero (M16).</b> This doc used to say "at zero height", which
    /// was true only until every drawable sharing one plane turned out to z-fight: the boundary, the
    /// penalty area, the goal area and the goal mouth all overlap at each goal line, the centre spot
    /// sits exactly on the halfway line, and the centre circle crosses it twice. A renderer therefore
    /// places drawable <c>i</c> of <see cref="PitchMarkings.BuildDrawables"/>'s stated, deterministic
    /// order at <c>MatchClientConstants.MarkingLayerHeightM + i *
    /// MatchClientConstants.MarkingLayerStepM</c> — the lowest of the four ordered M12 ground layers,
    /// spread into a band by index. The band's total rise is boot-validated in
    /// <c>MatchClientConstants</c> to stay comfortably below the next layer up
    /// (<c>BallShadowLayerHeightM</c>), so it can never grow into it. The marking's own geometry is
    /// unaffected: nothing here carries a height field, and the band is a pure rendering-order choice
    /// applied at placement.</para>
    ///
    /// <para><b>Which fields are meaningful depends on <see cref="Kind"/></b>, and
    /// <see cref="PitchMarkingKind"/> documents each case. The alternative — a type per shape behind
    /// an interface — would put a virtual call and a cast in the render loop for four shapes that
    /// between them need two points and a scalar. The cost of this shape is that an unused field is
    /// zero rather than absent; the tests assert that, so a consumer reading <c>B</c> on a circle
    /// gets a defined value rather than whatever was left over.</para>
    ///
    /// <para>Presentation-only: nothing here is read by the simulation, serialized into a snapshot,
    /// or fed into a digest.</para>
    /// </summary>
    public readonly struct PitchMarking
    {
        /// <summary>The shape this marking describes, which fixes how the fields below are read.</summary>
        public readonly PitchMarkingKind Kind;

        /// <summary>
        /// Line start / circle or spot centre / <b>rectangle MIN corner</b>, in pitch metres.
        /// </summary>
        public readonly Vector2 A;

        /// <summary>
        /// Line end / <b>rectangle MAX corner</b>, in pitch metres. Zero for circles and spots.
        ///
        /// <para>For a <see cref="PitchMarkingKind.Rectangle"/> the ordering is guaranteed:
        /// <c>A.x &lt;= B.x</c> and <c>A.y &lt;= B.y</c>, so <c>B - A</c> is a non-negative extent a
        /// renderer can use directly. That guarantee is the whole point — the away-end boxes are
        /// built from their goal line inwards and so arrive with descending X, and a binding that
        /// took the corners as given would draw those two rectangles inverted while the home pair
        /// looked right. Normalising here keeps that decision out of the render skin, where the CI
        /// gate cannot reach it.</para>
        ///
        /// <para>Lines and goal mouths are NOT normalised: a line has a direction, and a goal mouth
        /// is post-to-post.</para>
        /// </summary>
        public readonly Vector2 B;

        /// <summary>Circle or spot radius, in metres. Zero for lines, rectangles and goal mouths.</summary>
        public readonly float Radius;

        private PitchMarking(PitchMarkingKind kind, Vector2 a, Vector2 b, float radius)
        {
            Kind   = kind;
            A      = a;
            B      = b;
            Radius = radius;
        }

        /// <summary>A straight marking line between two pitch points.</summary>
        public static PitchMarking Line(Vector2 from, Vector2 to) =>
            new PitchMarking(PitchMarkingKind.Line, from, to, 0f);

        /// <summary>
        /// An axis-aligned rectangle outline through two opposite corners, in either order. The
        /// corners are normalised so <see cref="A"/> is always the min and <see cref="B"/> the max
        /// — see <see cref="B"/> for why that is not merely tidiness.
        /// </summary>
        public static PitchMarking Rectangle(Vector2 corner, Vector2 oppositeCorner) =>
            new PitchMarking(
                PitchMarkingKind.Rectangle,
                Vector2.Min(corner, oppositeCorner),
                Vector2.Max(corner, oppositeCorner),
                0f);

        /// <summary>A stroked circle.</summary>
        public static PitchMarking Circle(Vector2 centre, float radius) =>
            new PitchMarking(PitchMarkingKind.Circle, centre, Vector2.zero, radius);

        /// <summary>A filled spot.</summary>
        public static PitchMarking Spot(Vector2 centre, float radius) =>
            new PitchMarking(PitchMarkingKind.Spot, centre, Vector2.zero, radius);

        /// <summary>The goal mouth, post to post.</summary>
        public static PitchMarking GoalMouth(Vector2 post, Vector2 otherPost) =>
            new PitchMarking(PitchMarkingKind.GoalMouth, post, otherPost, 0f);
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): the marking value type, with a named    |
// |         |            |        | factory per shape so the field-meaning contract is stated at    |
// |         |            |        | every construction site rather than by parameter position.      |
// | 1.1     | 2026-08-04 | —      | AR pass H-1: Rectangle NORMALISES its corners (A = min, B =    |
// |         |            |        | max). The end boxes are built from their goal line inwards, so |
// |         |            |        | the away pair arrived with descending X and a binding taking   |
// |         |            |        | B − A as an extent would have drawn those two inverted while   |
// |         |            |        | the home pair looked right — the #8 ERR-008-002 asymmetry      |
// |         |            |        | class, in a decision the CI gate cannot reach once it lives in |
// |         |            |        | a MonoBehaviour. Lines and goal mouths stay unnormalised: a    |
// |         |            |        | line has direction and a goal mouth is post-to-post.           |
// | 1.2     | 2026-08-04 | —      | AR pass 2, M-2: the class doc still sent the render skin to     |
// |         |            |        | ToView, the flat-view 2D mapping, for markings that must lie   |
// |         |            |        | on the GROUND plane — following it would have stood every      |
// |         |            |        | marking upright in the world XY plane. Now ToWorld at zero     |
// |         |            |        | height. Doc only; no field, factory or value changed.          |
// | 1.3     | 2026-08-16 | —      | P4b AR round 5, M25: the class doc still sent a renderer to     |
// |         |            |        | ToWorld "at zero height", which round 3's M16 replaced with     |
// |         |            |        | the marking BAND (MarkingLayerHeightM + i * MarkingLayerStepM   |
// |         |            |        | over BuildDrawables' stated order) precisely because one shared |
// |         |            |        | plane z-fought wherever two drawables overlap. Following the    |
// |         |            |        | stale text would have reinstated that bug. Doc only; no field,  |
// |         |            |        | factory or value changed.                                       |
#endregion
