// File:     src/match-client-core/PitchMarking.cs
// Created:  2026-08-03
// Modified: 2026-08-03
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
    /// frame the engine reports positions in. A renderer projects each point through
    /// <see cref="PitchViewProjection.ToView"/>; <see cref="Radius"/> needs no conversion, since the
    /// view plane is 1 unit per metre.
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

        /// <summary>Line start / rectangle corner / circle or spot centre, in pitch metres.</summary>
        public readonly Vector2 A;

        /// <summary>Line end / opposite rectangle corner, in pitch metres. Zero for circles and spots.</summary>
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

        /// <summary>An axis-aligned rectangle outline through two opposite corners.</summary>
        public static PitchMarking Rectangle(Vector2 corner, Vector2 oppositeCorner) =>
            new PitchMarking(PitchMarkingKind.Rectangle, corner, oppositeCorner, 0f);

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
#endregion
