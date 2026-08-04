// File:     src/match-client-core/tests/PitchViewProjectionTests.cs
// Created:  2026-08-03
// Modified: 2026-08-04
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P4a, §7),
//           Ball Physics #1 §1.2, Testing Strategy #19, Code Standards #20
// Purpose:  Locks the corner-origin ⇄ centre-origin mapping: the origin convention, the engine-Y →
//           world-Z axis swap, the home/away mirror, and the ray/ground-plane click inverse.

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
        public void ToWorldGround_DropsHeight_AndAgreesWithToView()
        {
            var ball = new Vector3(30f, 40f, 6.5f);

            Vector3 ground = PitchViewProjection.ToWorldGround(ball);
            Vector2 planar = PitchViewProjection.ToView(new Vector2(ball.x, ball.y));

            Assert.AreEqual(planar.x, ground.x, Tolerance);
            Assert.AreEqual(0f, ground.y, Tolerance, "a shadow sits ON the ground plane");
            Assert.AreEqual(planar.y, ground.z, Tolerance,
                "height must not displace where the ball is over the pitch");
        }

        [Test]
        public void ToWorld_SwapsTheEngineYAxisIntoWorldZ_AndHeightIntoWorldY()
        {
            // The axis swap is the whole trap: engine Y runs across the pitch, world Y runs UP.
            // Getting it backwards lays the pitch on its side, and every downstream position would
            // still look plausible in isolation.
            Vector3 world = PitchViewProjection.ToWorld(new Vector2(30f, 40f), 6.5f);

            Assert.AreEqual(30f - PitchViewProjection.HalfLengthM, world.x, Tolerance, "pitch X → world X");
            Assert.AreEqual(6.5f, world.y, Tolerance, "height → world Y");
            Assert.AreEqual(40f - PitchViewProjection.HalfWidthM, world.z, Tolerance, "pitch Y → world Z");
        }

        [Test]
        public void ToWorld_AtZeroHeight_SitsOnTheGroundPlane()
        {
            Vector3 world = PitchViewProjection.ToWorld(new Vector2(11f, 34f), 0f);

            Assert.AreEqual(0f, world.y, Tolerance);
        }

        [Test]
        public void TryGroundHit_RoundTripsAPositionStraightDown()
        {
            // The simplest case, and the one a straight-down camera would produce.
            var pitch = new Vector2(70f, 20f);
            Vector3 above = PitchViewProjection.ToWorld(pitch, 30f);

            Assert.IsTrue(PitchViewProjection.TryGroundHit(above, Vector3.down, out Vector2 hit));

            Assert.AreEqual(pitch.x, hit.x, Tolerance);
            Assert.AreEqual(pitch.y, hit.y, Tolerance);
        }

        [Test]
        public void TryGroundHit_FollowsAnObliqueRay_NotJustTheColumnBelowTheCamera()
        {
            // Under a tilted camera the hit is NOT beneath the origin — that is the entire reason
            // this is a ray intersection rather than the old subtraction. A test that only fired
            // straight down would pass on the wrong implementation.
            var origin = new Vector3(0f, 20f, -20f);
            var direction = new Vector3(0f, -1f, 1f);   // 45°, so the hit is 20 m ahead in Z

            Assert.IsTrue(PitchViewProjection.TryGroundHit(origin, direction, out Vector2 hit));

            Assert.AreEqual(PitchViewProjection.HalfLengthM, hit.x, Tolerance);
            Assert.AreEqual(PitchViewProjection.HalfWidthM, hit.y, Tolerance,
                "the ray should land on the centre spot, not below the camera");
        }

        [Test]
        public void TryGroundHit_IsIndifferentToRayLength()
        {
            var origin = new Vector3(5f, 10f, 5f);

            Assert.IsTrue(PitchViewProjection.TryGroundHit(origin, new Vector3(1f, -1f, 2f), out Vector2 unit));
            Assert.IsTrue(PitchViewProjection.TryGroundHit(origin, new Vector3(7f, -7f, 14f), out Vector2 scaled));

            Assert.AreEqual(unit.x, scaled.x, Tolerance, "direction need not be normalised");
            Assert.AreEqual(unit.y, scaled.y, Tolerance);
        }

        [Test]
        public void TryGroundHit_ReportsAPointBeyondTheTouchline_RatherThanRefusingIt()
        {
            // A click off the pitch is a real answer; IsOnPitch is the separate question. Clamping
            // here would silently move a click onto the pitch and mislead whoever consumes it.
            var origin = new Vector3(0f, 10f, -200f);

            Assert.IsTrue(PitchViewProjection.TryGroundHit(origin, new Vector3(0f, -1f, 0f), out Vector2 hit));
            Assert.IsFalse(PitchViewProjection.IsOnPitch(hit));
        }

        [Test]
        public void TryGroundHit_RefusesARayThatCannotReachTheGround()
        {
            var origin = new Vector3(0f, 10f, 0f);

            // Parallel to the turf.
            Assert.IsFalse(PitchViewProjection.TryGroundHit(origin, new Vector3(1f, 0f, 0f), out Vector2 parallel));
            Assert.AreEqual(Vector2.zero, parallel, "a refused hit must not leave a stale position behind");

            // Pointing up, away from the ground: the intersection is BEHIND the origin.
            Assert.IsFalse(PitchViewProjection.TryGroundHit(origin, Vector3.up, out _));

            // Camera below the turf looking further down — also behind.
            Assert.IsFalse(PitchViewProjection.TryGroundHit(new Vector3(0f, -5f, 0f), Vector3.down, out _));

            // Degenerate origin / direction.
            Assert.IsFalse(PitchViewProjection.TryGroundHit(new Vector3(float.NaN, 10f, 0f), Vector3.down, out _));
            Assert.IsFalse(PitchViewProjection.TryGroundHit(origin, new Vector3(float.NaN, -1f, 0f), out _));
            Assert.IsFalse(PitchViewProjection.TryGroundHit(origin, Vector3.zero, out _));
        }

        [Test]
        public void TryGroundHit_InvertsToWorld_ForBothEnds()
        {
            // Mirrored per the root CLAUDE.md trap: a home-only round trip would pass on a mapping
            // that had the width axis inverted.
            foreach (Vector2 pitch in new[] { new Vector2(16.5f, 20f), new Vector2(88.5f, 48f) })
            {
                Vector3 camera = PitchViewProjection.ToWorld(pitch, 25f) + new Vector3(3f, 0f, -12f);
                Vector3 target = PitchViewProjection.ToWorld(pitch, 0f);

                Assert.IsTrue(PitchViewProjection.TryGroundHit(camera, target - camera, out Vector2 hit));

                Assert.AreEqual(pitch.x, hit.x, Tolerance, "X at " + pitch);
                Assert.AreEqual(pitch.y, hit.y, Tolerance, "Y at " + pitch);
            }
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
// | 1.1     | 2026-08-04 | —      | Tilted-view revision: + the ToWorld axis-swap lock (engine Y → |
// |         |            |        | world Z, height → world Y — an inverted swap lays the pitch on |
// |         |            |        | its side and every position still looks plausible alone), and + |
// |         |            |        | TryGroundHit: oblique rays (a straight-down-only test would    |
// |         |            |        | pass the old subtraction), scale-independence, off-pitch hits  |
// |         |            |        | reported not clamped, every refusal case, and a both-ends round |
// |         |            |        | trip. ToViewGround's test follows it to ToWorldGround.         |
#endregion
