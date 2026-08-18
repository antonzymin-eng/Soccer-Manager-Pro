// File:     src/match-client-core/PitchMarkings.cs
// Created:  2026-08-03
// Modified: 2026-08-15
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a, §5-P4b,
//           §7 "Reuse the geometry that already exists"), Ball Physics #1 §1.2 (corner-origin frame),
//           Code Standards #20
// Purpose:  Builds the pitch-marking catalogue the render skin draws, in corner-origin metres, from
//           the same MatchViewerConstants [FIXED] IFAB values the browser viewer already uses, and
//           the one-primitive-per-shape form of that catalogue a render binding instantiates from.

using System;

using UnityEngine;

using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientCore
{
    /// <summary>
    /// The IFAB markings of a pitch, as shapes a renderer can instantiate one primitive each for.
    ///
    /// <para><b>One source of truth for the markings (§7).</b> The distances come from
    /// <c>MatchViewerConstants</c>'s [FIXED] Law 1 catalogue — the same constants the browser
    /// viewer's canvas draws from — and the pitch extents from
    /// <c>MatchEngineConstants.PITCH_LENGTH_M</c>/<c>PITCH_WIDTH_M</c>. Nothing here is a second copy
    /// of a number that already exists, which is the whole reason this lives above
    /// <c>match-viewer</c> rather than beside it.</para>
    ///
    /// <para><b>Symmetry is constructed, not written twice.</b> Both ends are emitted by one loop
    /// over a sign, so a marking cannot be right at one end and wrong at the other. That is the
    /// direct structural answer to this project's home/away asymmetry defects (#8 ERR-008-002), and
    /// the tests still assert the mirror explicitly rather than trusting the loop.</para>
    ///
    /// <para><b>Allocation.</b> <see cref="Build"/> allocates its array. It is a scene-boot call —
    /// the markings never change during a match — and is not on the render path, so the zero-alloc
    /// game-loop rule does not reach it. A renderer calls this once and keeps the result.</para>
    /// </summary>
    public static class PitchMarkings
    {
        /// <summary>
        /// Number of markings <see cref="Build"/> returns: four whole-pitch markings (boundary,
        /// halfway line, centre circle, centre spot) plus four end-specific ones per end. Written as
        /// the formula rather than the literal 12 so the count and the emit loop cannot disagree.
        /// </summary>
        public const int MARKING_COUNT = WHOLE_PITCH_COUNT + PER_END_COUNT * MatchEngineConstants.TEAM_COUNT;

        /// <summary>
        /// Number of shapes <see cref="BuildDrawables"/> returns: every marking, with each rectangle
        /// replaced by the four lines that close it. Written as the formula for the same reason
        /// <see cref="MARKING_COUNT"/> is — so the count and the decomposition cannot disagree.
        /// </summary>
        public const int DRAWABLE_COUNT =
            MARKING_COUNT + RECTANGLE_COUNT * (RECTANGLE_EDGE_COUNT - 1);

        /// <summary>Markings that belong to the pitch rather than to an end.</summary>
        private const int WHOLE_PITCH_COUNT = 4;

        /// <summary>End-specific markings: penalty area, goal area, penalty spot, goal mouth.</summary>
        private const int PER_END_COUNT = 4;

        /// <summary>
        /// Rectangles <see cref="Build"/> emits: the pitch boundary, plus a penalty area and a goal
        /// area at each end. Checked against the catalogue at every <see cref="BuildDrawables"/> call
        /// rather than trusted, so adding a rectangle to <see cref="Build"/> and forgetting this
        /// number fails loud with its own message instead of running off the end of an array.
        /// </summary>
        private const int RECTANGLE_COUNT = 1 + 2 * MatchEngineConstants.TEAM_COUNT;

        /// <summary>Lines an axis-aligned rectangle outline decomposes into.</summary>
        private const int RECTANGLE_EDGE_COUNT = 4;

        /// <summary>
        /// Builds the marking catalogue in corner-origin pitch metres, in a fixed order: boundary,
        /// halfway line, centre circle, centre spot, then the penalty areas, goal areas, penalty
        /// spots and goal mouths, each as a home-end/away-end pair in that order.
        ///
        /// <para>The order is part of the contract — it is what lets a renderer assign a material or
        /// a sorting layer per index, and what lets the mirror tests pair adjacent entries.</para>
        /// </summary>
        public static PitchMarking[] Build()
        {
            float length = MatchEngineConstants.PITCH_LENGTH_M;
            float width  = MatchEngineConstants.PITCH_WIDTH_M;
            float midX   = length * 0.5f;
            float midY   = width * 0.5f;
            float spotR  = MatchClientConstants.MarkingSpotRadiusM;

            var markings = new PitchMarking[MARKING_COUNT];
            int n = 0;

            markings[n++] = PitchMarking.Rectangle(new Vector2(0f, 0f), new Vector2(length, width));
            markings[n++] = PitchMarking.Line(new Vector2(midX, 0f), new Vector2(midX, width));
            markings[n++] = PitchMarking.Circle(new Vector2(midX, midY), MatchViewerConstants.CentreCircleRadiusM);
            markings[n++] = PitchMarking.Spot(new Vector2(midX, midY), spotR);

            // One end per team — an end is the goal that team defends at kickoff. Home end first,
            // away end second, for each marking type: entries pair up as (home, away) so a mirror
            // assertion compares adjacent indices. `sign` carries the direction the area extends
            // into the field of play; `goalLineX` is the end it starts from.
            int ends = MatchEngineConstants.TEAM_COUNT;
            for (int end = 0; end < ends; end++)
            {
                float goalLineX = end == 0 ? 0f : length;
                float sign      = end == 0 ? 1f : -1f;

                markings[WHOLE_PITCH_COUNT + 0 * ends + end] = PitchMarking.Rectangle(
                    new Vector2(goalLineX, midY - MatchViewerConstants.PenaltyAreaWidthM * 0.5f),
                    new Vector2(goalLineX + sign * MatchViewerConstants.PenaltyAreaDepthM,
                                midY + MatchViewerConstants.PenaltyAreaWidthM * 0.5f));

                markings[WHOLE_PITCH_COUNT + 1 * ends + end] = PitchMarking.Rectangle(
                    new Vector2(goalLineX, midY - MatchViewerConstants.GoalAreaWidthM * 0.5f),
                    new Vector2(goalLineX + sign * MatchViewerConstants.GoalAreaDepthM,
                                midY + MatchViewerConstants.GoalAreaWidthM * 0.5f));

                markings[WHOLE_PITCH_COUNT + 2 * ends + end] = PitchMarking.Spot(
                    new Vector2(goalLineX + sign * MatchViewerConstants.PenaltySpotDistanceM, midY),
                    spotR);

                markings[WHOLE_PITCH_COUNT + 3 * ends + end] = PitchMarking.GoalMouth(
                    new Vector2(goalLineX, midY - MatchViewerConstants.GoalWidthM * 0.5f),
                    new Vector2(goalLineX, midY + MatchViewerConstants.GoalWidthM * 0.5f));
            }

            return markings;
        }

        /// <summary>
        /// Builds the catalogue in the form a render binding instantiates <b>one drawing primitive
        /// per entry</b> from: <see cref="Build"/>'s output with every
        /// <see cref="PitchMarkingKind.Rectangle"/> replaced, in place, by the four
        /// <see cref="PitchMarkingKind.Line"/> edges that close it. No rectangle survives, so a
        /// binding needs no rectangle case and no corner arithmetic at all.
        ///
        /// <para><b>Why the decomposition lives here.</b> Synthesising a rectangle's two missing
        /// corners is corner geometry, and corner geometry in the render skin is what the P4a/P4b
        /// split — and the corner-normalisation of <see cref="PitchMarking.Rectangle"/> before it —
        /// exists to keep out. A transposed <c>A.y</c>/<c>B.y</c> draws a bow-tie rather than a box,
        /// and in a <c>MonoBehaviour</c> that mistake is invisible: the CI gate cannot compile that
        /// file, let alone test it. Here it is four lines of arithmetic under the gate.</para>
        ///
        /// <para><b>Order extends <see cref="Build"/>'s contract rather than replacing it.</b>
        /// Markings keep their relative order, and a rectangle's four edges appear consecutively at
        /// its position, walking the outline counter-clockwise from the min corner: bottom (min Y),
        /// right (max X), top (max Y), left (min X). Each edge starts where the previous one ended
        /// and the fourth returns to the first's start, so the four close the loop — which is the
        /// property a binding's index-to-material correspondence and the tests both rely on.</para>
        ///
        /// <para>Allocation: a scene-boot call like <see cref="Build"/>, not a render-path one.</para>
        /// </summary>
        /// <exception cref="InvalidOperationException"><see cref="Build"/> emitted a number of
        /// rectangles <see cref="DRAWABLE_COUNT"/> is not sized for.</exception>
        public static PitchMarking[] BuildDrawables()
        {
            PitchMarking[] markings = Build();

            int rectangles = 0;

            for (int i = 0; i < markings.Length; i++)
            {
                if (markings[i].Kind == PitchMarkingKind.Rectangle)
                {
                    rectangles++;
                }
            }

            if (rectangles != RECTANGLE_COUNT)
            {
                throw new InvalidOperationException(
                    "PitchMarkings.Build() emitted " + rectangles + " rectangles, but DRAWABLE_COUNT " +
                    "is sized for " + RECTANGLE_COUNT + ". Update RECTANGLE_COUNT alongside the catalogue.");
            }

            var drawables = new PitchMarking[DRAWABLE_COUNT];
            int n = 0;

            for (int i = 0; i < markings.Length; i++)
            {
                PitchMarking marking = markings[i];

                if (marking.Kind != PitchMarkingKind.Rectangle)
                {
                    drawables[n++] = marking;
                    continue;
                }

                // A.x <= B.x and A.y <= B.y are guaranteed by PitchMarking.Rectangle, so these are
                // the min and max corners and the two synthesised ones are the remaining diagonal.
                Vector2 min = marking.A;
                Vector2 max = marking.B;
                Vector2 maxXminY = new Vector2(max.x, min.y);
                Vector2 minXmaxY = new Vector2(min.x, max.y);

                drawables[n++] = PitchMarking.Line(min, maxXminY);
                drawables[n++] = PitchMarking.Line(maxXminY, max);
                drawables[n++] = PitchMarking.Line(max, minXmaxY);
                drawables[n++] = PitchMarking.Line(minXmaxY, min);
            }

            return drawables;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): the 12-marking catalogue, built from    |
// |         |            |        | the existing [FIXED] IFAB values, both ends from one loop.      |
// |         |            |        | The centre circle's D-arc and the corner arcs are deliberately  |
// |         |            |        | absent — neither has a constant in the [FIXED] catalogue and    |
// |         |            |        | the browser viewer draws neither, so adding them would mean     |
// |         |            |        | inventing geometry here and diverging the two Views.            |
// | 1.1     | 2026-08-15 | —      | P4b AR pass H-1: + BuildDrawables()/DRAWABLE_COUNT — the        |
// |         |            |        | one-primitive-per-entry catalogue, with each rectangle          |
// |         |            |        | decomposed into the four lines that close it. The P4b binding   |
// |         |            |        | had been synthesising the two missing corners itself, inside a  |
// |         |            |        | MonoBehaviour: corner geometry back in the file AR-P4a-H1's     |
// |         |            |        | corner normalisation removed it from, where a transposed        |
// |         |            |        | A.y/B.y draws a bow-tie and nothing compiles it. It also broke  |
// |         |            |        | Build()'s index-correspondence ordering, which this restores by |
// |         |            |        | keeping the edges consecutive at their marking's position.      |
#endregion
