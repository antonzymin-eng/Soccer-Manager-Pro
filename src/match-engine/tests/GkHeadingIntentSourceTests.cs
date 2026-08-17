// File:     src/match-engine/tests/GkHeadingIntentSourceTests.cs
// Created:  2026-07-22
// Modified: 2026-08-09 (AR over the ERR-010-002 landing: §4.2a HeaderAimTarget locks — mirror,
//           opponent-goal-line, advancement ordering, nearer-touchline)
// Author:   —
// Spec:     GK/Heading engine-integration design supplement
//           (docs/tracking/gk-heading-engine-integration-design.md) §4; Code Standards #20
// Purpose:  Pure-function locks for GkHeadingIntentSource — the §4 save/header trigger geometry, now
//           testable in isolation (the cleaner-architecture extraction out of MatchEngine).

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.AgentMovement;

namespace TacticalDirector.MatchEngine
{
    /// <summary>§4 trigger heuristic locks — geometry only; no booted engine required.</summary>
    [TestFixture]
    public sealed class GkHeadingIntentSourceTests
    {
        // ── §4.1 SaveArmed ────────────────────────────────────────────────────────────

        [Test]
        public void SaveArmed_LooseBallDrivingAtDefendedGoal_Team0_Arms()
        {
            // Team 0 defends x = 0; ball 5 m out driving at −X (toward the goal) at 10 m/s.
            bool armed = GkHeadingIntentSource.SaveArmed(
                0, new Vector3(5f, 34f, 0.11f), new Vector3(-10f, 0f, 0f), ballLoose: true);
            Assert.IsTrue(armed);
        }

        [Test]
        public void SaveArmed_LooseBallDrivingAtDefendedGoal_Team1_Arms()
        {
            // Team 1 defends x = PITCH_LENGTH_M; ball just inside, driving at +X toward that goal.
            float x = MatchEngineConstants.PITCH_LENGTH_M - 5f;
            bool armed = GkHeadingIntentSource.SaveArmed(
                1, new Vector3(x, 34f, 0.11f), new Vector3(+10f, 0f, 0f), ballLoose: true);
            Assert.IsTrue(armed);
        }

        [Test]
        public void SaveArmed_PossessedBall_DoesNotArm()
        {
            bool armed = GkHeadingIntentSource.SaveArmed(
                0, new Vector3(5f, 34f, 0.11f), new Vector3(-10f, 0f, 0f), ballLoose: false);
            Assert.IsFalse(armed, "A possessed ball must never arm a save (the loose gate).");
        }

        [Test]
        public void SaveArmed_BallMovingAwayFromGoal_DoesNotArm()
        {
            // Team 0's goal is at x = 0, but the ball drives at +X (away from it).
            bool armed = GkHeadingIntentSource.SaveArmed(
                0, new Vector3(5f, 34f, 0.11f), new Vector3(+10f, 0f, 0f), ballLoose: true);
            Assert.IsFalse(armed, "Away-from-goal velocity must not arm (towardGoal > 0 gate).");
        }

        [Test]
        public void SaveArmed_TooSlow_DoesNotArm()
        {
            float slow = MatchEngineConstants.GkSaveTriggerMinBallSpeedMps * 0.5f;
            bool armed = GkHeadingIntentSource.SaveArmed(
                0, new Vector3(5f, 34f, 0.11f), new Vector3(-slow, 0f, 0f), ballLoose: true);
            Assert.IsFalse(armed, "Below the min ball speed must not arm.");
        }

        [Test]
        public void SaveArmed_OutOfRange_DoesNotArm()
        {
            float x = MatchEngineConstants.GkSaveTriggerRangeM + 5f;   // beyond save range of the x = 0 goal
            bool armed = GkHeadingIntentSource.SaveArmed(
                0, new Vector3(x, 34f, 0.11f), new Vector3(-10f, 0f, 0f), ballLoose: true);
            Assert.IsFalse(armed, "Beyond the goal-line range must not arm.");
        }

        // ── §4.2 NearestHeaderCandidate ───────────────────────────────────────────────

        private static AgentState AgentAt(float x, float y)
        {
            var a = new AgentState();
            a.Position = new Vector2(x, y);
            return a;
        }

        [Test]
        public void NearestHeaderCandidate_PicksNearestOutfielder_ToLooseAirborneBall()
        {
            var agents = new[] { AgentAt(50f, 34f), AgentAt(50.5f, 34f), AgentAt(60f, 34f) };
            var gk = new[] { false, false, false };
            var off = new[] { false, false, false };
            int nearest = GkHeadingIntentSource.NearestHeaderCandidate(
                new Vector3(50f, 34f, 1.0f), airborneLoose: true, agents, gk, off, agents.Length);
            Assert.AreEqual(0, nearest, "Agent 0 (0.0 m) is nearer than agent 1 (0.5 m).");
        }

        [Test]
        public void NearestHeaderCandidate_NotAirborneLoose_ReturnsMinusOne()
        {
            var agents = new[] { AgentAt(50f, 34f) };
            int nearest = GkHeadingIntentSource.NearestHeaderCandidate(
                new Vector3(50f, 34f, 1.0f), airborneLoose: false,
                agents, new[] { false }, new[] { false }, agents.Length);
            Assert.AreEqual(-1, nearest);
        }

        [Test]
        public void NearestHeaderCandidate_SkipsGoalkeepersAndSentOff()
        {
            // Nearest by distance is index 0 (a keeper) then index 1 (sent off); index 2 is the winner.
            var agents = new[] { AgentAt(50f, 34f), AgentAt(50.2f, 34f), AgentAt(50.4f, 34f) };
            var gk = new[] { true, false, false };
            var off = new[] { false, true, false };
            int nearest = GkHeadingIntentSource.NearestHeaderCandidate(
                new Vector3(50f, 34f, 1.0f), airborneLoose: true, agents, gk, off, agents.Length);
            Assert.AreEqual(2, nearest, "GK (0) and sent-off (1) are excluded; 2 is the nearest eligible.");
        }

        [Test]
        public void NearestHeaderCandidate_ExactTie_LaterIndexWins()
        {
            // Two eligible agents at EXACTLY equal squared distance to the ball (symmetric about x = 50):
            // the documented tie-break is the later index (the <= compare), matching the engine's original scan.
            // Halves are exactly representable, so ±0.5 squares to a bit-exact 0.25 tie; 0.5 m sits well inside
            // HeaderTriggerRangeM (default 1.5 m) so the assertion is not coupled to that tunable [GT] value.
            var agents = new[] { AgentAt(49.5f, 34f), AgentAt(50.5f, 34f) };
            var gk = new[] { false, false };
            var off = new[] { false, false };
            int nearest = GkHeadingIntentSource.NearestHeaderCandidate(
                new Vector3(50f, 34f, 1.0f), airborneLoose: true, agents, gk, off, agents.Length);
            Assert.AreEqual(1, nearest, "On an exact distance tie the later index wins (the <= compare).");
        }

        [Test]
        public void NearestHeaderCandidate_AllOutOfRange_ReturnsMinusOne()
        {
            float far = MatchEngineConstants.HeaderTriggerRangeM + 5f;
            var agents = new[] { AgentAt(50f + far, 34f) };
            int nearest = GkHeadingIntentSource.NearestHeaderCandidate(
                new Vector3(50f, 34f, 1.0f), airborneLoose: true,
                agents, new[] { false }, new[] { false }, agents.Length);
            Assert.AreEqual(-1, nearest, "Beyond head range must not select a candidate.");
        }

        // ── §4.2a HeaderAimTarget (ERR-010-002) ───────────────────────────────────────
        //
        // These are the ERR-008-002 home/away locks for this landing. The test in
        // HeadingMechanics.Tests that carried that label locks HeadingAim, which takes no team id at
        // all and ran TeamId = 0 on both sides — it could not have failed for an asymmetry. This
        // method is the landing's only team-branching geometry, and until now nothing tested it.

        [Test]
        public void HeaderAimTarget_MirrorsAboutTheHalfwayLine()
        {
            float length = MatchEngineConstants.PITCH_LENGTH_M;

            // Every quadrant, both halves, both sides of the width centre line.
            Vector2[] homePositions =
            {
                new Vector2(10f, 10f), new Vector2(10f, 58f),
                new Vector2(52.5f, 34f), new Vector2(95f, 12f), new Vector2(95f, 60f),
            };

            foreach (Vector2 home in homePositions)
            {
                Vector2 away = new Vector2(length - home.x, home.y);

                Vector3 homeTarget = GkHeadingIntentSource.HeaderAimTarget(in home, 0);
                Vector3 awayTarget = GkHeadingIntentSource.HeaderAimTarget(in away, 1);

                Assert.That(awayTarget.x, Is.EqualTo(length - homeTarget.x).Within(1e-3f),
                    $"x must mirror for {home}");
                Assert.That(awayTarget.y, Is.EqualTo(homeTarget.y).Within(1e-3f),
                    $"y must not mirror for {home}");
            }
        }

        [Test]
        public void HeaderAimTarget_AimsAtTheOpponentGoalLine_ForBothTeams()
        {
            Vector2 spot = new Vector2(40f, 20f);

            Assert.That(GkHeadingIntentSource.HeaderAimTarget(in spot, 0).x,
                Is.EqualTo(MatchEngineConstants.PITCH_LENGTH_M).Within(1e-3f));
            Assert.That(GkHeadingIntentSource.HeaderAimTarget(in spot, 1).x,
                Is.EqualTo(0f).Within(1e-3f));
        }

        /// <summary>
        /// The advancement lerp: deep ⇒ toward the nearer touchline, advanced ⇒ toward goal centre,
        /// continuous between. Asserted as a strict ordering rather than on values, so a re-tuning of
        /// the curve does not break the lock but a loss of the mechanism does.
        /// </summary>
        [Test]
        public void HeaderAimTarget_AdvancementMovesTheAimFromTouchlineTowardGoalCentre()
        {
            float centreY = MatchEngineConstants.PITCH_WIDTH_M * 0.5f;
            float previousDistanceFromCentre = float.MaxValue;

            // Team 0, fixed y in the −Y half, walking up the pitch.
            for (float x = 5f; x <= 100f; x += 5f)
            {
                Vector2 spot = new Vector2(x, 10f);
                float distanceFromCentre = centreY - GkHeadingIntentSource.HeaderAimTarget(in spot, 0).y;

                Assert.That(distanceFromCentre, Is.LessThan(previousDistanceFromCentre),
                    $"advancing to x = {x} must aim strictly nearer goal centre");
                Assert.That(distanceFromCentre, Is.GreaterThanOrEqualTo(0f),
                    "the lerp must never cross the centre line");
                previousDistanceFromCentre = distanceFromCentre;
            }
        }

        [Test]
        public void HeaderAimTarget_PicksTheNearerTouchline_OnEitherSideOfTheWidthCentre()
        {
            float centreY = MatchEngineConstants.PITCH_WIDTH_M * 0.5f;

            Vector2 deepLow = new Vector2(8f, 10f);
            Vector2 deepHigh = new Vector2(8f, 58f);

            float lowSide = GkHeadingIntentSource.HeaderAimTarget(in deepLow, 0).y;
            float highSide = GkHeadingIntentSource.HeaderAimTarget(in deepHigh, 0).y;

            Assert.That(lowSide, Is.LessThan(centreY), "a header in the −Y half clears toward y = 0");
            Assert.That(highSide, Is.GreaterThan(centreY), "a header in the +Y half clears toward y = W");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-22 | —      | Initial — pure §4 save/header trigger geometry locks.          |
// | 1.1     | 2026-07-22 | —      | +NearestHeaderCandidate_ExactTie_LaterIndexWins — locks the    |
// |         |            |        | documented <= later-index-wins tie-break (code-AR follow-up).  |
// | 1.2     | 2026-08-09 | —      | AR over the ERR-010-002 landing: 4 HeaderAimTarget locks. That |
// |         |            |        | method landed with NO test at all — the landing's only team-   |
// |         |            |        | branching geometry, in an assembly whose fixture already       |
// |         |            |        | existed — while a HeadingMechanics.Tests case labelled the     |
// |         |            |        | ERR-008-002 home/away lock exercised team-AGNOSTIC code at     |
// |         |            |        | TeamId = 0 on both sides. The mirror lock now lives here,      |
// |         |            |        | where the team branch is.                                      |
#endregion
