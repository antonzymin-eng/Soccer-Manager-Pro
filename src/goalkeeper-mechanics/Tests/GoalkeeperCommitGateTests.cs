// File:     src/goalkeeper-mechanics/Tests/GoalkeeperCommitGateTests.cs
// Created:  2026-07-28
// Modified: 2026-07-28
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §3.3.6 (ERR-011-007), Code Standards #20
// Purpose:  Unit locks for the commit-to-arrival timing gate: the shared plane-crossing
//           predictor, the lateral-need-scaled commit lead, and the ShouldCommitDive verdicts
//           (hold vs immediate vs floor vs non-closing).

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.BallPhysics;
using TacticalDirector.GoalkeeperMechanics;

namespace TacticalDirector.GoalkeeperMechanics.Tests
{
    [TestFixture]
    public class GoalkeeperCommitGateTests
    {
        private static readonly Vector2 Gk = new Vector2(2f, 34f);

        private static BallState Ball(float x, float y, float vx, float vy)
        {
            BallState b = BallState.CreateAtPosition(new Vector3(x, y, 0.11f));
            b.Velocity = new Vector3(vx, vy, 0f);
            return b;
        }

        // ── TryPredictPlaneCrossing ──────────────────────────────────────────────────────

        [Test]
        public void Predict_ClosingBall_ReturnsTimeAndCrossingY()
        {
            // 18 m out at −12 m/s toward x = 2: t = (2 − 20)/(−12) = 1.5 s; y = 30 + 2 × 1.5 = 33.
            BallState ball = Ball(20f, 30f, -12f, 2f);

            bool ok = GoalkeeperDiveKinematics.TryPredictPlaneCrossing(
                Gk, in ball, out float t, out float y);

            Assert.IsTrue(ok);
            Assert.AreEqual(1.5f, t, 1e-4f);
            Assert.AreEqual(33.0f, y, 1e-3f);
        }

        [Test]
        public void Predict_BallMovingAway_ReturnsFalse()
        {
            BallState ball = Ball(20f, 30f, +12f, 0f);

            Assert.IsFalse(GoalkeeperDiveKinematics.TryPredictPlaneCrossing(
                Gk, in ball, out _, out _));
        }

        [Test]
        public void Predict_LateralBall_ReturnsFalse()
        {
            // vx ~ 0: no crossing is ever reached; diving at an extrapolation would be a dive
            // at nothing.
            BallState ball = Ball(20f, 30f, 0f, 8f);

            Assert.IsFalse(GoalkeeperDiveKinematics.TryPredictPlaneCrossing(
                Gk, in ball, out _, out _));
        }

        [Test]
        public void Predict_CrossingBeyondHorizon_ReturnsFalse()
        {
            // 40 m out at −4 m/s: t = 9.5 s > DivePredictionHorizonS (2 s).
            BallState ball = Ball(42f, 34f, -4f, 0f);

            Assert.IsFalse(GoalkeeperDiveKinematics.TryPredictPlaneCrossing(
                Gk, in ball, out _, out _));
        }

        [Test]
        public void Predict_BallAtPlane_IsImmediateCrossingAtCurrentY()
        {
            BallState ball = Ball(2f, 31f, 0f, 0f);

            bool ok = GoalkeeperDiveKinematics.TryPredictPlaneCrossing(
                Gk, in ball, out float t, out float y);

            Assert.IsTrue(ok, "A ball at the keeper's plane is a smother, never a hold.");
            Assert.AreEqual(0f, t, 0f);
            Assert.AreEqual(31f, y, 0f);
        }

        // ── ComputeDiveCommitLeadS ───────────────────────────────────────────────────────

        [Test]
        public void CommitLead_FullNeed_IsWholeDiveDuration()
        {
            float durationS = GoalkeeperConstants.DivePhaseDurationMs / GoalkeeperConstants.MS_PER_SECOND;

            float lead = GoalkeeperDiveKinematics.ComputeDiveCommitLeadS(
                GoalkeeperConstants.DiveLaunchDisplacementM);

            Assert.AreEqual(durationS, lead, 1e-5f,
                "A corner-bound ball needs the whole dive: lead = the full duration.");
        }

        [Test]
        public void CommitLead_CentralBall_IsFlooredNotZero()
        {
            float durationS = GoalkeeperConstants.DivePhaseDurationMs / GoalkeeperConstants.MS_PER_SECOND;
            float floor = GoalkeeperConstants.DiveCommitMinLeadFrac * durationS;

            Assert.AreEqual(floor, GoalkeeperDiveKinematics.ComputeDiveCommitLeadS(0f), 1e-5f,
                "A central ball's lead floors at DiveCommitMinLeadFrac x duration — never zero "
                + "against the 10 Hz decision grid.");
        }

        [Test]
        public void CommitLead_ScalesLinearlyBetweenFloorAndFull()
        {
            float half = GoalkeeperConstants.DiveLaunchDisplacementM * 0.5f;
            float durationS = GoalkeeperConstants.DivePhaseDurationMs / GoalkeeperConstants.MS_PER_SECOND;

            Assert.AreEqual(0.5f * durationS,
                GoalkeeperDiveKinematics.ComputeDiveCommitLeadS(half), 1e-5f);
        }

        // ── ShouldCommitDive ─────────────────────────────────────────────────────────────

        [Test]
        public void Commit_ArrivalInsideLead_CommitsNow()
        {
            // t = 0.5 s to the plane, crossing 4 m off the keeper (full-need lead 0.6 s): commit.
            BallState ball = Ball(8f, 30f, -12f, 0f);

            Assert.IsTrue(GoalkeeperDiveKinematics.ShouldCommitDive(Gk, in ball));
        }

        [Test]
        public void Commit_ArrivalBeyondLead_Holds()
        {
            // t = 1.5 s to the plane: far outside every possible lead (max = 0.6 s) — the dive
            // would be over ~0.9 s before the ball arrived. Pre-ERR-011-007 this dived
            // immediately: the measured dive-early class (9 of 15 crossed episodes).
            BallState ball = Ball(20f, 30f, -12f, 0f);

            Assert.IsFalse(GoalkeeperDiveKinematics.ShouldCommitDive(Gk, in ball));
        }

        [Test]
        public void Commit_CentralFastBall_HoldsUntilFlooredLead()
        {
            // Crossing dead-centre on the keeper (need ~0): lead floors at 0.25 x 0.6 = 0.15 s.
            // At t = 0.4 s the gate holds; at t = 0.1 s it commits.
            BallState far = Ball(10f, 34f, -20f, 0f);    // t = 0.4 s
            BallState near = Ball(4f, 34f, -20f, 0f);    // t = 0.1 s

            Assert.IsFalse(GoalkeeperDiveKinematics.ShouldCommitDive(Gk, in far));
            Assert.IsTrue(GoalkeeperDiveKinematics.ShouldCommitDive(Gk, in near));
        }

        [Test]
        public void Commit_NonClosingBall_NeverCommits()
        {
            BallState away = Ball(8f, 30f, +12f, 0f);
            BallState lateral = Ball(8f, 30f, 0f, 12f);

            Assert.IsFalse(GoalkeeperDiveKinematics.ShouldCommitDive(Gk, in away));
            Assert.IsFalse(GoalkeeperDiveKinematics.ShouldCommitDive(Gk, in lateral));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-28 | —      | Initial. ERR-011-007 commit-gate locks: shared crossing predictor  |
// |         |            |        | (closing / away / lateral / horizon / at-plane), lead arithmetic   |
// |         |            |        | (full-need / floor / linear), and the hold-vs-commit verdicts incl.|
// |         |            |        | the measured dive-early geometry that must now HOLD.               |
#endregion
