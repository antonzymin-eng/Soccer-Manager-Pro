// File:     src/match-analytics/Tests/XgLocationModelTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Analytics & Statistics #37 §3.3 / §5 (T-AN-XG-*), Code Standards #20
// Purpose:  Locks the xG location model. Because §3.3 contracts the model's SHAPE rather than its
//           illustrative [GT] coefficients, most of these assert monotonicity, symmetry and totality —
//           properties a Stage-2 refit must preserve — with the spec's three worked examples pinned
//           separately so a refit that changes them is a visible, deliberate act.

using System;

using NUnit.Framework;

using UnityEngine;

namespace TacticalDirector.MatchAnalytics.Tests
{
    [TestFixture]
    public sealed class XgLocationModelTests
    {
        private const float HalfWidth = MatchAnalyticsConstants.PITCH_WIDTH_M * 0.5f;

        private static Vector2 CentralAt(float metresOut, byte team) =>
            team == 0
                ? new Vector2(MatchAnalyticsConstants.PITCH_LENGTH_M - metresOut, HalfWidth)
                : new Vector2(metresOut, HalfWidth);

        // ── The spec's worked examples (§3.3) ────────────────────────────────────────────────────

        [Test]
        public void PenaltySpot_MatchesTheWorkedExample()
        {
            // §3.3: d = 11, θ ≈ 36.9° ⇒ z ≈ 0.277 ⇒ xG ≈ 0.569.
            float xg = XgLocationModel.Evaluate(CentralAt(11f, 0), 0);
            Assert.AreEqual(0.569f, xg, 0.01f);
        }

        [Test]
        public void SixYardTap_AndLongRangeShot_MatchTheWorkedExamples()
        {
            // §3.3: d ≈ 5.5, θ ≈ 65° ⇒ ≈ 0.80;  d ≈ 25, θ ≈ 16° ⇒ ≈ 0.17.
            Assert.AreEqual(0.80f, XgLocationModel.Evaluate(CentralAt(5.5f, 0), 0), 0.02f);
            Assert.AreEqual(0.17f, XgLocationModel.Evaluate(CentralAt(25f, 0), 0), 0.02f);
        }

        [Test]
        public void SubtendedAngle_MatchesTheWorkedGeometry()
        {
            // Each post subtends atan(3.66/11) = 18.43° from the penalty spot, so the mouth is ~36.9°.
            float theta = XgLocationModel.SubtendedGoalAngleDegrees(CentralAt(11f, 0), 0);
            Assert.AreEqual(36.87f, theta, 0.1f);
        }

        // ── The shape §3.3 actually contracts ────────────────────────────────────────────────────

        [Test]
        public void Closer_IsAlwaysWorthMore_AlongTheCentralLine()
        {
            float previous = -1f;
            for (float d = 40f; d >= 4f; d -= 2f)
            {
                float xg = XgLocationModel.Evaluate(CentralAt(d, 0), 0);
                Assert.Greater(xg, previous, "xG must rise monotonically as the shot gets closer (d=" + d + ")");
                previous = xg;
            }
        }

        [Test]
        public void WiderAngle_IsWorthMore_AtEqualDistance()
        {
            // Two shots the same distance from goal centre: one central, one out towards the touchline.
            // The central one sees more goal, so it must score higher.
            const float d = 16f;
            Vector2 central = CentralAt(d, 0);

            float dx = d * 0.5f;
            float dy = Mathf.Sqrt(d * d - dx * dx);
            var wide = new Vector2(MatchAnalyticsConstants.PITCH_LENGTH_M - dx, HalfWidth + dy);

            Assert.AreEqual(d, Vector2.Distance(wide, XgLocationModel.GoalCentre(0)), 0.01f, "equal distance");
            Assert.Greater(
                XgLocationModel.SubtendedGoalAngleDegrees(central, 0),
                XgLocationModel.SubtendedGoalAngleDegrees(wide, 0),
                "the central shot must see more of the goal");
            Assert.Greater(
                XgLocationModel.Evaluate(central, 0),
                XgLocationModel.Evaluate(wide, 0),
                "at equal distance, the wider view of goal is worth more");
        }

        /// <summary>
        /// The away side attacks the other way. Every prior home/away asymmetry defect in this project
        /// (ERR-008-002) shipped because the examples and fixtures only ever used the home team, so the
        /// mirror is asserted rather than assumed.
        /// </summary>
        [Test]
        public void BothTeams_ScoreTheirOwnMirroredPositionsIdentically()
        {
            for (float d = 6f; d <= 30f; d += 6f)
            {
                float home = XgLocationModel.Evaluate(CentralAt(d, 0), 0);
                float away = XgLocationModel.Evaluate(CentralAt(d, 1), 1);
                Assert.AreEqual(home, away, 1e-5f, "the model must be side-symmetric at d=" + d);
            }
        }

        [Test]
        public void ShootingAtTheWrongGoal_IsWorthNearlyNothing()
        {
            // A team-0 position 11 m from the team-1 goal, scored as a team-0 shot: it is ~94 m from the
            // goal team 0 is attacking, so the model must treat it as hopeless rather than excellent.
            Vector2 atOwnEnd = CentralAt(11f, 1);
            Assert.Less(XgLocationModel.Evaluate(atOwnEnd, 0), 0.01f);
        }

        [Test]
        public void OutputIsAlwaysAProbability_AcrossTheWholePitchAndBeyond()
        {
            for (float x = -20f; x <= MatchAnalyticsConstants.PITCH_LENGTH_M + 20f; x += 5f)
            {
                for (float y = -20f; y <= MatchAnalyticsConstants.PITCH_WIDTH_M + 20f; y += 5f)
                {
                    for (byte team = 0; team < 2; team++)
                    {
                        float xg = XgLocationModel.Evaluate(new Vector2(x, y), team);
                        Assert.IsFalse(float.IsNaN(xg), "xG must be a number at (" + x + "," + y + ")");
                        Assert.GreaterOrEqual(xg, 0f);
                        Assert.LessOrEqual(xg, 1f);
                    }
                }
            }
        }

        [Test]
        public void OnAPost_IsTotal_NotUndefined()
        {
            // The subtended angle is undefined standing exactly on a post; the function must still
            // return a probability rather than NaN.
            Vector2 post = XgLocationModel.GoalCentre(0)
                         + new Vector2(0f, MatchAnalyticsConstants.GOAL_WIDTH_M * 0.5f);

            float xg = XgLocationModel.Evaluate(post, 0);
            Assert.IsFalse(float.IsNaN(xg));
            Assert.GreaterOrEqual(xg, 0f);
            Assert.LessOrEqual(xg, 1f);
        }

        [Test]
        public void Evaluate_IsPure_SameInputSameOutput()
        {
            Vector2 s = CentralAt(14f, 0);
            float first = XgLocationModel.Evaluate(s, 0);
            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(first, XgLocationModel.Evaluate(s, 0), 0f, "the model is stateless");
            }
        }

        // ── Fail-loud gates (F1 / F2) ────────────────────────────────────────────────────────────

        [Test]
        public void NonFiniteOrigin_Throws()
        {
            Assert.Throws<ArgumentException>(() => XgLocationModel.Evaluate(new Vector2(float.NaN, 34f), 0));
            Assert.Throws<ArgumentException>(() => XgLocationModel.Evaluate(new Vector2(52f, float.PositiveInfinity), 0));
            Assert.Throws<ArgumentException>(() => XgLocationModel.Evaluate(new Vector2(float.NegativeInfinity, 34f), 1));
        }

        [Test]
        public void UnknownTeam_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => XgLocationModel.Evaluate(CentralAt(11f, 0), 2));
        }

        // ── The catalogue mirrors the pitch rather than restating it ─────────────────────────────

        [Test]
        public void PitchConstants_MirrorBallPhysics_NotALocalCopy()
        {
            Assert.AreEqual(BallPhysics.BallPhysicsConstants.Pitch.GOAL_WIDTH, MatchAnalyticsConstants.GOAL_WIDTH_M);
            Assert.AreEqual(BallPhysics.BallPhysicsConstants.Pitch.LENGTH,     MatchAnalyticsConstants.PITCH_LENGTH_M);
            Assert.AreEqual(BallPhysics.BallPhysicsConstants.Pitch.WIDTH,      MatchAnalyticsConstants.PITCH_WIDTH_M);
            Assert.AreEqual(
                MatchAnalyticsConstants.HEATMAP_COLS * MatchAnalyticsConstants.HEATMAP_ROWS,
                MatchAnalyticsConstants.HEATMAP_BINS_PER_TEAM,
                "HEATMAP_BINS_PER_TEAM is [DERIVED] and must never be set independently");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-27 | —      | Initial creation (#37 T0, T-AN-XG-*): the three §3.3 worked    |
// |         |            |        | examples pinned, plus the shape properties a Stage-2 refit     |
// |         |            |        | must preserve — distance/angle monotonicity, home/away         |
// |         |            |        | mirror symmetry, totality over and beyond the pitch, purity,   |
// |         |            |        | and the F1/F2 gates.                                           |
#endregion
