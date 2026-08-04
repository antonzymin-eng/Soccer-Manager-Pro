// File:     src/match-client-core/PitchMarkings.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a, §7
//           "Reuse the geometry that already exists"), Ball Physics #1 §1.2 (corner-origin frame),
//           Code Standards #20
// Purpose:  Builds the pitch-marking catalogue the render skin draws, in corner-origin metres, from
//           the same MatchViewerConstants [FIXED] IFAB values the browser viewer already uses.

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

        /// <summary>Markings that belong to the pitch rather than to an end.</summary>
        private const int WHOLE_PITCH_COUNT = 4;

        /// <summary>End-specific markings: penalty area, goal area, penalty spot, goal mouth.</summary>
        private const int PER_END_COUNT = 4;

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
#endregion
