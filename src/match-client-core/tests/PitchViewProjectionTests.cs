// File:     src/match-client-core/tests/PitchViewProjectionTests.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a, §7),
//           Ball Physics #1 §1.2, Testing Strategy #19, Code Standards #20
// Purpose:  Locks the corner-origin ⇄ centre-origin mapping: the origin convention itself, the
//           round trip, and the home/away mirror symmetry the centring exists to give.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.MatchClientCore;
using TacticalDirector.MatchEngine;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    public sealed class PitchViewProjectionTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void PitchCentre_ProjectsToTheViewOrigin()
        {
            Vector2 centre = new Vector2(
                MatchEngineConstants.PITCH_LENGTH_M * 0.5f,
                MatchEngineConstants.PITCH_WIDTH_M * 0.5f);

            Vector2 view = PitchViewProjection.ToView(centre);

            Assert.AreEqual(0f, view.x, Tolerance, "the centre spot is the view origin");
            Assert.AreEqual(0f, view.y, Tolerance);
        }

        [Test]
        public void PitchOrigin_IsACorner_NotTheCentre()
        {
            // The recorded trap (root CLAUDE.md): a "pitch centre" origin assumption. Engine (0,0) is
            // a corner, so it must project to a corner of the view rectangle — never to (0,0).
            Vector2 view = PitchViewProjection.ToView(Vector2.zero);

            Assert.AreEqual(-PitchViewProjection.HalfLengthM, view.x, Tolerance);
            Assert.AreEqual(-PitchViewProjection.HalfWidthM, view.y, Tolerance);
        }

        [Test]
        public void HomeAndAwayGoalCentres_MirrorAboutTheViewOrigin()
        {
            float midY = MatchEngineConstants.PITCH_WIDTH_M * 0.5f;

            Vector2 home = PitchViewProjection.ToView(new Vector2(0f, midY));
            Vector2 away = PitchViewProjection.ToView(new Vector2(MatchEngineConstants.PITCH_LENGTH_M, midY));

            Assert.AreEqual(-home.x, away.x, Tolerance,
                "the two goal lines must differ only in sign — that symmetry is why the view is centred");
            Assert.AreEqual(home.y, away.y, Tolerance);
        }

        [Test]
        public void ToPitch_RoundTripsToView()
        {
            var samples = new[]
            {
                new Vector2(0f, 0f),
                new Vector2(MatchEngineConstants.PITCH_LENGTH_M, MatchEngineConstants.PITCH_WIDTH_M),
                new Vector2(11f, 34f),
                new Vector2(94f, 34f),
                new Vector2(52.5f, 68f),
            };

            foreach (Vector2 pitch in samples)
            {
                Vector2 back = PitchViewProjection.ToPitch(PitchViewProjection.ToView(pitch));

                Assert.AreEqual(pitch.x, back.x, Tolerance, "round trip lost X at " + pitch);
                Assert.AreEqual(pitch.y, back.y, Tolerance, "round trip lost Y at " + pitch);
            }
        }

        [Test]
        public void ToViewGround_DropsHeight_AndAgreesWithToView()
        {
            var ball = new Vector3(30f, 40f, 6.5f);

            Vector2 ground = PitchViewProjection.ToViewGround(ball);
            Vector2 planar = PitchViewProjection.ToView(new Vector2(ball.x, ball.y));

            Assert.AreEqual(planar.x, ground.x, Tolerance);
            Assert.AreEqual(planar.y, ground.y, Tolerance,
                "height must not displace where the ball is over the pitch");
        }

        [Test]
        public void ScaleIsOnePerMetre()
        {
            Vector2 a = PitchViewProjection.ToView(new Vector2(10f, 20f));
            Vector2 b = PitchViewProjection.ToView(new Vector2(17f, 20f));

            Assert.AreEqual(7f, b.x - a.x, Tolerance, "seven metres apart must be seven view units apart");
        }

        [Test]
        public void IsOnPitch_IncludesTheBoundaries_AndExcludesBeyondThem()
        {
            Assert.IsTrue(PitchViewProjection.IsOnPitch(Vector2.zero));
            Assert.IsTrue(PitchViewProjection.IsOnPitch(new Vector2(
                MatchEngineConstants.PITCH_LENGTH_M, MatchEngineConstants.PITCH_WIDTH_M)));
            Assert.IsFalse(PitchViewProjection.IsOnPitch(new Vector2(-0.1f, 34f)));
            Assert.IsFalse(PitchViewProjection.IsOnPitch(new Vector2(52.5f, MatchEngineConstants.PITCH_WIDTH_M + 0.1f)));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-08-03 | —      | Initial creation (P4a): origin convention, corner-not-centre,   |
// |         |            |        | home/away mirror, round trip, unit scale, on-pitch predicate.   |
#endregion
