// File:     src/match-engine/tests/GkHeadingIntentSourceTests.cs
// Created:  2026-07-22
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
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-22 | —      | Initial — pure §4 save/header trigger geometry locks.          |
// | 1.1     | 2026-07-22 | —      | +NearestHeaderCandidate_ExactTie_LaterIndexWins — locks the    |
// |         |            |        | documented <= later-index-wins tie-break (code-AR follow-up).  |
#endregion
