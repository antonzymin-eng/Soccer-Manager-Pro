// File:     src/match-client-core/tests/PitchMarkingsDrawablesTests.cs
// Created:  2026-08-15
// Modified: 2026-08-15
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a,
//           §5-P4b, §12 rule 1), Testing Strategy #19, Code Standards #20
// Purpose:  Locks the rectangle-to-four-lines decomposition the render binding used to do inside a
//           MonoBehaviour: that no rectangle survives, that the four edges close the outline with
//           the documented winding, and that they do so at BOTH ends of the pitch.

using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class PitchMarkingsDrawablesTests
    {
        private const float Tolerance = 1e-4f;

        private static readonly float Length = MatchEngineConstants.PITCH_LENGTH_M;

        [Test]
        public void BuildDrawables_ReturnsTheDeclaredCount()
        {
            Assert.AreEqual(PitchMarkings.DRAWABLE_COUNT, PitchMarkings.BuildDrawables().Length);
        }

        [Test]
        public void NoRectangleSurvives_SoABindingNeedsNoRectangleCase()
        {
            // The whole point of the method: the render skin instantiates one primitive per entry and
            // never reconstructs a corner. A surviving rectangle would put that arithmetic straight
            // back into the MonoBehaviour, where nothing compiles it.
            foreach (PitchMarking drawable in PitchMarkings.BuildDrawables())
            {
                Assert.AreNotEqual(PitchMarkingKind.Rectangle, drawable.Kind,
                    "BuildDrawables emitted a rectangle at " + drawable.A + " → " + drawable.B);
            }
        }

        [Test]
        public void EveryNonRectangleMarking_PassesThroughUnchangedAndInOrder()
        {
            // Order is a contract, and it extends Build()'s rather than replacing it — that is what
            // lets a binding keep assigning a material or a sorting layer by index.
            PitchMarking[] markings = PitchMarkings.Build();
            PitchMarking[] drawables = PitchMarkings.BuildDrawables();

            int n = 0;

            for (int i = 0; i < markings.Length; i++)
            {
                if (markings[i].Kind == PitchMarkingKind.Rectangle)
                {
                    n += 4;
                    continue;
                }

                Assert.AreEqual(markings[i].Kind, drawables[n].Kind, "kind differs at marking " + i);
                Assert.AreEqual(markings[i].A, drawables[n].A, "A differs at marking " + i);
                Assert.AreEqual(markings[i].B, drawables[n].B, "B differs at marking " + i);
                Assert.AreEqual(markings[i].Radius, drawables[n].Radius, Tolerance, "radius differs at marking " + i);
                n++;
            }

            Assert.AreEqual(PitchMarkings.DRAWABLE_COUNT, n, "the walk did not cover every drawable");
        }

        [Test]
        public void EveryRectangle_BecomesFourEdgesThatCloseIt()
        {
            PitchMarking[] markings = PitchMarkings.Build();
            PitchMarking[] drawables = PitchMarkings.BuildDrawables();

            int rectangles = 0;

            for (int i = 0; i < markings.Length; i++)
            {
                if (markings[i].Kind != PitchMarkingKind.Rectangle) { continue; }

                rectangles++;
                AssertClosesRectangle(drawables, DrawableIndexOf(markings, i), markings[i].A, markings[i].B,
                    "rectangle at marking " + i);
            }

            // The boundary plus the four end boxes — named so a marking silently changing kind cannot
            // make the loop above vacuously pass.
            Assert.AreEqual(5, rectangles, "expected the boundary and the four end boxes");
        }

        [Test]
        public void TheEndBoxes_CloseAtBothEnds_AndTheAwayEdgesMirrorTheHomeOnes()
        {
            // The mirror rule from the root CLAUDE.md trap table, applied to the decomposition rather
            // than to the corners: a winding or corner-synthesis error that is right at one end and
            // wrong at the other is the #8 ERR-008-002 asymmetry class, and it is exactly what a
            // rectangle built from its goal line INWARDS (the away pair) would expose.
            PitchMarking[] markings = PitchMarkings.Build();
            PitchMarking[] drawables = PitchMarkings.BuildDrawables();

            // Entries 4..7 are the two end-box pairs, home first, per Build()'s ordering contract.
            foreach (int home in new[] { 4, 6 })
            {
                int away = home + 1;

                Assert.AreEqual(PitchMarkingKind.Rectangle, markings[home].Kind, "marking " + home);
                Assert.AreEqual(PitchMarkingKind.Rectangle, markings[away].Kind, "marking " + away);

                Vector2 homeMin = markings[home].A;
                Vector2 homeMax = markings[home].B;

                // Mirroring about the halfway line swaps which corner carries which X: the away box's
                // MIN x is the mirror of the home box's MAX x.
                Vector2 awayMin = new Vector2(Length - homeMax.x, homeMin.y);
                Vector2 awayMax = new Vector2(Length - homeMin.x, homeMax.y);

                AssertClosesRectangle(drawables, DrawableIndexOf(markings, home), homeMin, homeMax,
                    "home end box (marking " + home + ")");
                AssertClosesRectangle(drawables, DrawableIndexOf(markings, away), awayMin, awayMax,
                    "away end box (marking " + away + "), mirrored from its home counterpart");
            }
        }

        /// <summary>
        /// Index into <c>BuildDrawables()</c> of the first entry produced by marking
        /// <paramref name="markingIndex"/> — walked rather than hard-coded, so the fixture states the
        /// expansion rule once and no test carries an offset that a new marking would silently break.
        /// </summary>
        private static int DrawableIndexOf(PitchMarking[] markings, int markingIndex)
        {
            int n = 0;

            for (int i = 0; i < markingIndex; i++)
            {
                n += markings[i].Kind == PitchMarkingKind.Rectangle ? 4 : 1;
            }

            return n;
        }

        private static void AssertClosesRectangle(
            PitchMarking[] drawables, int start, Vector2 min, Vector2 max, string what)
        {
            Vector2 maxXminY = new Vector2(max.x, min.y);
            Vector2 minXmaxY = new Vector2(min.x, max.y);

            // The documented winding: counter-clockwise from the min corner. Asserted as exact
            // endpoints, because closure alone cannot tell a correct box from one walked with A.y and
            // B.y transposed — that traversal closes too, as a bow-tie.
            var expected = new List<(Vector2 From, Vector2 To)>
            {
                (min,      maxXminY),
                (maxXminY, max),
                (max,      minXmaxY),
                (minXmaxY, min),
            };

            for (int edge = 0; edge < expected.Count; edge++)
            {
                PitchMarking drawable = drawables[start + edge];

                Assert.AreEqual(PitchMarkingKind.Line, drawable.Kind, what + " edge " + edge + " kind");
                AssertPoint(expected[edge].From, drawable.A, what + " edge " + edge + " start");
                AssertPoint(expected[edge].To, drawable.B, what + " edge " + edge + " end");
                Assert.AreEqual(0f, drawable.Radius, Tolerance, what + " edge " + edge + " leaves Radius zeroed");

                // Each edge begins where the previous one ended, the fourth returning to the first's
                // start: the outline is closed, with no gap at a corner and no stray diagonal.
                PitchMarking previous = drawables[start + (edge + 3) % expected.Count];
                AssertPoint(previous.B, drawable.A, what + " edge " + edge + " does not continue edge " + (edge + 3) % expected.Count);

                // Axis-aligned and non-degenerate: a marking with no length is not a marking.
                bool horizontal = Mathf.Abs(drawable.A.y - drawable.B.y) <= Tolerance;
                bool vertical = Mathf.Abs(drawable.A.x - drawable.B.x) <= Tolerance;
                Assert.IsTrue(horizontal ^ vertical, what + " edge " + edge + " is not axis-aligned");
                Assert.Greater((drawable.B - drawable.A).magnitude, Tolerance, what + " edge " + edge + " has no length");
            }
        }

        private static void AssertPoint(Vector2 expected, Vector2 actual, string what)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, what + " X (expected " + expected + ", was " + actual + ")");
            Assert.AreEqual(expected.y, actual.y, Tolerance, what + " Y (expected " + expected + ", was " + actual + ")");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-15 | —      | Initial creation (P4b AR pass H-1): the decomposition's count,  |
// |         |            |        | its no-rectangle-survives contract, pass-through ordering, the  |
// |         |            |        | exact counter-clockwise winding (closure alone cannot tell a    |
// |         |            |        | box from a transposed bow-tie, which also closes), and the      |
// |         |            |        | home/away mirror of both end-box pairs.                         |
#endregion
