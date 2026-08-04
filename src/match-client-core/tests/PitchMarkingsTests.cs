// File:     src/match-client-core/tests/PitchMarkingsTests.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a, §7),
//           Testing Strategy #19, Code Standards #20
// Purpose:  Locks the pitch-marking catalogue: its shape contract, its IFAB distances against the
//           single [FIXED] source, and — the load-bearing one — that every end-specific marking is
//           an exact mirror of its counterpart at the other end.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;
using TacticalDirector.MatchViewer;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class PitchMarkingsTests
    {
        private const float Tolerance = 1e-4f;

        private static readonly float Length = MatchEngineConstants.PITCH_LENGTH_M;
        private static readonly float Width  = MatchEngineConstants.PITCH_WIDTH_M;

        [Test]
        public void Build_ReturnsTheDeclaredCount()
        {
            Assert.AreEqual(PitchMarkings.MARKING_COUNT, PitchMarkings.Build().Length);
        }

        [Test]
        public void Build_IsDeterministic()
        {
            PitchMarking[] a = PitchMarkings.Build();
            PitchMarking[] b = PitchMarkings.Build();

            for (int i = 0; i < a.Length; i++)
            {
                Assert.AreEqual(a[i].Kind, b[i].Kind, "kind differs at " + i);
                Assert.AreEqual(a[i].A, b[i].A, "A differs at " + i);
                Assert.AreEqual(a[i].B, b[i].B, "B differs at " + i);
                Assert.AreEqual(a[i].Radius, b[i].Radius, Tolerance, "radius differs at " + i);
            }
        }

        [Test]
        public void Boundary_HalfwayLine_CentreCircleAndSpot_AreTheFirstFour()
        {
            PitchMarking[] m = PitchMarkings.Build();

            Assert.AreEqual(PitchMarkingKind.Rectangle, m[0].Kind);
            Assert.AreEqual(Vector2.zero, m[0].A);
            Assert.AreEqual(new Vector2(Length, Width), m[0].B);

            Assert.AreEqual(PitchMarkingKind.Line, m[1].Kind);
            Assert.AreEqual(Length * 0.5f, m[1].A.x, Tolerance);
            Assert.AreEqual(Length * 0.5f, m[1].B.x, Tolerance, "the halfway line must be vertical");
            Assert.AreEqual(0f, Mathf.Min(m[1].A.y, m[1].B.y), Tolerance);
            Assert.AreEqual(Width, Mathf.Max(m[1].A.y, m[1].B.y), Tolerance);

            Assert.AreEqual(PitchMarkingKind.Circle, m[2].Kind);
            Assert.AreEqual(MatchViewerConstants.CentreCircleRadiusM, m[2].Radius, Tolerance,
                "the centre circle must read its radius from the one [FIXED] IFAB catalogue");

            Assert.AreEqual(PitchMarkingKind.Spot, m[3].Kind);
            Assert.AreEqual(new Vector2(Length * 0.5f, Width * 0.5f), m[3].A);
        }

        [Test]
        public void EveryEndSpecificMarking_HasAnExactMirrorAtTheOtherEnd()
        {
            // The mirror rule from the root CLAUDE.md trap table: any team-relative geometry gets
            // asserted at BOTH ends. Entries 4..11 are four home/away pairs, home first.
            PitchMarking[] m = PitchMarkings.Build();

            for (int pair = 0; pair < 4; pair++)
            {
                int home = 4 + pair * 2;
                int away = home + 1;

                Assert.AreEqual(m[home].Kind, m[away].Kind, "pair " + pair + " differs in kind");
                Assert.AreEqual(m[home].Radius, m[away].Radius, Tolerance, "pair " + pair + " differs in radius");

                AssertMirrored(m[home].A, m[away].A, "pair " + pair + " point A");

                // B is meaningful only for the two-point kinds; a spot leaves it zeroed, and a zero
                // is not its own mirror. Assert it stays zeroed on both sides instead — the same
                // property, for a field that carries no geometry.
                if (m[home].Kind == PitchMarkingKind.Circle || m[home].Kind == PitchMarkingKind.Spot)
                {
                    Assert.AreEqual(Vector2.zero, m[home].B, "pair " + pair + " home B should be zeroed");
                    Assert.AreEqual(Vector2.zero, m[away].B, "pair " + pair + " away B should be zeroed");
                    continue;
                }

                AssertMirrored(m[home].B, m[away].B, "pair " + pair + " point B");
            }
        }

        [Test]
        public void PenaltyAndGoalAreas_MatchTheIfabCatalogue_AtBothEnds()
        {
            PitchMarking[] m = PitchMarkings.Build();

            AssertAreaBox(m[4], 0f, +1f, MatchViewerConstants.PenaltyAreaDepthM, MatchViewerConstants.PenaltyAreaWidthM, "home penalty area");
            AssertAreaBox(m[5], Length, -1f, MatchViewerConstants.PenaltyAreaDepthM, MatchViewerConstants.PenaltyAreaWidthM, "away penalty area");
            AssertAreaBox(m[6], 0f, +1f, MatchViewerConstants.GoalAreaDepthM, MatchViewerConstants.GoalAreaWidthM, "home goal area");
            AssertAreaBox(m[7], Length, -1f, MatchViewerConstants.GoalAreaDepthM, MatchViewerConstants.GoalAreaWidthM, "away goal area");
        }

        [Test]
        public void PenaltySpots_SitAtTheIfabDistanceInsideEachGoalLine()
        {
            PitchMarking[] m = PitchMarkings.Build();
            float d = MatchViewerConstants.PenaltySpotDistanceM;

            Assert.AreEqual(PitchMarkingKind.Spot, m[8].Kind);
            Assert.AreEqual(d, m[8].A.x, Tolerance);
            Assert.AreEqual(Width * 0.5f, m[8].A.y, Tolerance);

            Assert.AreEqual(PitchMarkingKind.Spot, m[9].Kind);
            Assert.AreEqual(Length - d, m[9].A.x, Tolerance);
            Assert.AreEqual(Width * 0.5f, m[9].A.y, Tolerance);
        }

        [Test]
        public void GoalMouths_SpanTheIfabGoalWidth_OnEachGoalLine()
        {
            PitchMarking[] m = PitchMarkings.Build();
            float goalWidth = MatchViewerConstants.GoalWidthM;

            foreach (int i in new[] { 10, 11 })
            {
                Assert.AreEqual(PitchMarkingKind.GoalMouth, m[i].Kind);
                Assert.AreEqual(m[i].A.x, m[i].B.x, Tolerance, "a goal mouth lies on its goal line");
                Assert.AreEqual(goalWidth, Mathf.Abs(m[i].B.y - m[i].A.y), Tolerance);
                Assert.AreEqual(Width * 0.5f, (m[i].A.y + m[i].B.y) * 0.5f, Tolerance,
                    "the goal is centred on the goal line");
            }

            Assert.AreEqual(0f, m[10].A.x, Tolerance);
            Assert.AreEqual(Length, m[11].A.x, Tolerance);
        }

        [Test]
        public void EveryMarkingPointLiesOnOrInsideThePitch()
        {
            foreach (PitchMarking marking in PitchMarkings.Build())
            {
                Assert.IsTrue(PitchViewProjection.IsOnPitch(marking.A),
                    marking.Kind + " point A is off the pitch at " + marking.A);

                if (marking.Kind == PitchMarkingKind.Circle || marking.Kind == PitchMarkingKind.Spot)
                {
                    Assert.AreEqual(Vector2.zero, marking.B, "circles and spots leave B zeroed");
                    continue;
                }

                Assert.IsTrue(PitchViewProjection.IsOnPitch(marking.B),
                    marking.Kind + " point B is off the pitch at " + marking.B);
                Assert.AreEqual(0f, marking.Radius, Tolerance, "non-round markings leave Radius zeroed");
            }
        }

        private static void AssertMirrored(Vector2 home, Vector2 away, string what)
        {
            Assert.AreEqual(Length - home.x, away.x, Tolerance, what + ": X is not mirrored about the halfway line");
            Assert.AreEqual(home.y, away.y, Tolerance, what + ": Y must be identical at both ends");
        }

        private static void AssertAreaBox(PitchMarking marking, float goalLineX, float sign, float depth, float boxWidth, string what)
        {
            Assert.AreEqual(PitchMarkingKind.Rectangle, marking.Kind, what);

            float minX = Mathf.Min(marking.A.x, marking.B.x);
            float maxX = Mathf.Max(marking.A.x, marking.B.x);
            float minY = Mathf.Min(marking.A.y, marking.B.y);
            float maxY = Mathf.Max(marking.A.y, marking.B.y);

            float expectedNear = goalLineX;
            float expectedFar  = goalLineX + sign * depth;

            Assert.AreEqual(Mathf.Min(expectedNear, expectedFar), minX, Tolerance, what + " near edge");
            Assert.AreEqual(Mathf.Max(expectedNear, expectedFar), maxX, Tolerance, what + " far edge");
            Assert.AreEqual(boxWidth, maxY - minY, Tolerance, what + " width");
            Assert.AreEqual(Width * 0.5f, (minY + maxY) * 0.5f, Tolerance, what + " is not centred");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): count, determinism, the four common     |
// |         |            |        | markings, both-ends mirror symmetry, IFAB distances against     |
// |         |            |        | MatchViewerConstants, and the unused-field-is-zero contract.    |
#endregion
