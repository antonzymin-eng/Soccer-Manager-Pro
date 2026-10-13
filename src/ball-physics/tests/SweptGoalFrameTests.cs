// File:     src/ball-physics/tests/SweptGoalFrameTests.cs
// Created:  2026-07-28
// Modified: 2026-10-11 (ERR-001-007 rebound-returns-to-play locks — v1.1)
// Author:   —
// Spec:     Ball Physics #1 §3.1.10 (ERR-001-005, ERR-001-007); shot-speed & woodwork design KD-4/KD-5;
//           Code Standards #20
// Purpose:  Unit locks for the swept goal-frame collision and the crossing-point boundary
//           adjudication. The load-bearing case is TUNNELING: at shot speeds the ball moves
//           ~0.42 m per 60 Hz tick, so a segment can enter AND exit a post's 0.17 m combined
//           radius within one tick — a discrete per-tick position test never sees the contact.

using NUnit.Framework;
using UnityEngine;

namespace TacticalDirector.BallPhysics.Tests
{
    [TestFixture]
    internal class SweptGoalFrameTests
    {
        private static float CentreY   => BallPhysicsConstants.Pitch.WIDTH * 0.5f;
        private static float GoalLineX => BallPhysicsConstants.Pitch.LENGTH;
        private static float PostY     => CentreY + BallPhysicsConstants.GoalFrame.PostAxisOffsetY;
        private static float BarZ      => BallPhysicsConstants.GoalFrame.BarAxisZ;
        private static float R         => BallPhysicsConstants.GoalFrame.SweptTestRadius;

        private static BallState Flying(Vector3 position, Vector3 velocity)
        {
            BallState ball = BallState.CreateAtPosition(position);
            ball.Velocity = velocity;
            ball.State = BallStateType.Airborne;
            return ball;
        }

        // ── The tunneling discriminator ──────────────────────────────────────────────────────

        [Test]
        public void PostStrike_SegmentFullyCrossingThePostInOneTick_IsDetected()
        {
            // Segment 104.5 → 105.4 at the post axis' y: it ENTERS and EXITS the post's combined
            // radius within one step — the exact case a discrete position test tunnels through
            // (neither endpoint overlaps the post). The swept test must catch the entry crossing.
            BallState ball = Flying(new Vector3(GoalLineX + 0.4f, PostY, 1.0f),
                                    new Vector3(30f, 0f, 0f));
            Vector3 prev = new Vector3(GoalLineX - 0.5f, PostY, 1.0f);

            bool hit = BallCollision.ApplySweptGoalFrameCollision(ref ball, prev);

            Assert.IsTrue(hit, "a segment fully crossing the post must register the strike");
            Assert.AreEqual(GoalLineX - R, ball.Position.x, 0.01f,
                "the ball must be placed at the entry contact, not the tunnelled exit");
            Assert.Less(ball.Velocity.x, 0f, "a front-face strike reflects the ball back into play");
        }

        [Test]
        public void PostStrike_FrontFace_AppliesRestitutionAndSpinRetention()
        {
            BallState ball = Flying(new Vector3(GoalLineX - 0.1f, PostY, 1.0f),
                                    new Vector3(30f, 0f, 0f));
            ball.AngularVelocity = new Vector3(0f, 10f, 0f);
            Vector3 prev = new Vector3(GoalLineX - 0.5f, PostY, 1.0f);

            bool hit = BallCollision.ApplySweptGoalFrameCollision(ref ball, prev);

            Assert.IsTrue(hit);
            // Pure normal incidence on the front face: v' = −CoR × v along X, tangentials zero.
            float expectedVx = -BallPhysicsConstants.GoalPost.CoefficientOfRestitution * 30f;
            Assert.AreEqual(expectedVx, ball.Velocity.x, 0.05f, "restitution along the normal");
            Assert.AreEqual(10f * BallPhysicsConstants.GoalPost.SpinRetention,
                ball.AngularVelocity.y, 1e-3f, "spin retention on the metal surface");
        }

        [Test]
        public void CrossbarStrike_FrontFace_Rebounds()
        {
            BallState ball = Flying(new Vector3(GoalLineX + 0.2f, CentreY, BarZ),
                                    new Vector3(25f, 0f, 0f));
            Vector3 prev = new Vector3(GoalLineX - 0.5f, CentreY, BarZ);

            bool hit = BallCollision.ApplySweptGoalFrameCollision(ref ball, prev);

            Assert.IsTrue(hit, "a bar-height ball dead on the axis strikes the crossbar");
            Assert.AreEqual(GoalLineX - R, ball.Position.x, 0.01f);
            Assert.Less(ball.Velocity.x, 0f);
        }

        // ── No false clips ───────────────────────────────────────────────────────────────────

        [Test]
        public void TrajectoryThroughTheMouth_IsNeverClipped()
        {
            // Dead-centre, 1 m up — the middle of the mouth. The frame must not touch it.
            BallState ball = Flying(new Vector3(GoalLineX + 0.3f, CentreY, 1.0f),
                                    new Vector3(25f, 0f, 0f));
            Vector3 prev = new Vector3(GoalLineX - 0.5f, CentreY, 1.0f);

            Assert.IsFalse(BallCollision.ApplySweptGoalFrameCollision(ref ball, prev),
                "a ball through the middle of the mouth must never hit the frame");
        }

        [Test]
        public void TrajectoryOverTheFrame_IsNeverClipped()
        {
            // Over the post's capped extent (z above the bar's upper edge), at the post's y.
            float overTop = BallPhysicsConstants.GoalFrame.FrameTopZ + 0.3f;
            BallState ball = Flying(new Vector3(GoalLineX + 0.3f, PostY, overTop),
                                    new Vector3(25f, 0f, 0f));
            Vector3 prev = new Vector3(GoalLineX - 0.5f, PostY, overTop);

            Assert.IsFalse(BallCollision.ApplySweptGoalFrameCollision(ref ball, prev),
                "a ball clearing the frame top must fly on");
        }

        // ── Gates ────────────────────────────────────────────────────────────────────────────

        [Test]
        public void ControlledBall_IsNeverDeflected()
        {
            BallState ball = Flying(new Vector3(GoalLineX + 0.4f, PostY, 1.0f),
                                    new Vector3(30f, 0f, 0f));
            ball.State = BallStateType.Controlled;
            Vector3 prev = new Vector3(GoalLineX - 0.5f, PostY, 1.0f);

            Assert.IsFalse(BallCollision.ApplySweptGoalFrameCollision(ref ball, prev),
                "a possessed ball is driven at the holder's feet — the frame test stands down");
        }

        [Test]
        public void DegenerateSegment_IsANoOp()
        {
            Vector3 at = new Vector3(GoalLineX - R - 0.001f, PostY, 1.0f);
            BallState ball = Flying(at, Vector3.zero);

            Assert.IsFalse(BallCollision.ApplySweptGoalFrameCollision(ref ball, at),
                "a ball that did not move cannot cross into the frame");
        }

        [Test]
        public void SegmentStartingInsideTheFrameRadius_IsANoOp()
        {
            // Starts already overlapping the post (the previous tick's test handled the approach)
            // — returning false here prevents a repeated-deflection loop against the same post.
            BallState ball = Flying(new Vector3(GoalLineX + 0.3f, PostY, 1.0f),
                                    new Vector3(30f, 0f, 0f));
            Vector3 prev = new Vector3(GoalLineX - 0.1f, PostY, 1.0f);   // 0.1 < R from the axis

            Assert.IsFalse(BallCollision.ApplySweptGoalFrameCollision(ref ball, prev));
        }

        // ── Crossing-point adjudication (KD-5) ───────────────────────────────────────────────

        [Test]
        public void RisingCrossing_UnderTheBarAtThePlane_IsAGoal_AwayEnd()
        {
            // Crosses the out-plane at z ≈ 2.30 (under the 2.44 bar) but is DETECTED at
            // z ≈ 2.60 (over). The segment's closest approach to the bar axis exceeds the
            // combined radius, so this isolates the adjudication, not the frame.
            BallState ball = Flying(new Vector3(GoalLineX + 0.47f, CentreY, 2.60f),
                                    new Vector3(25f, 0f, 20f));
            Vector3 prev = new Vector3(GoalLineX + 0.05f, CentreY, 2.25f);

            (bool isOut, RestartType restart) =
                BallCollision.CheckBoundaries(ball, prev, lastTouchTeamID: 0);

            Assert.IsTrue(isOut);
            Assert.AreEqual(RestartType.KickOff, restart,
                "the crossing happened UNDER the bar — the overshot detected position must not decide");
        }

        [Test]
        public void RisingCrossing_UnderTheBarAtThePlane_IsAGoal_HomeEnd()
        {
            // The mirrored home-goal case (the ERR-008-002 lesson: mirror team-relative geometry).
            BallState ball = Flying(new Vector3(-0.47f, CentreY, 2.60f),
                                    new Vector3(-25f, 0f, 20f));
            Vector3 prev = new Vector3(-0.05f, CentreY, 2.25f);

            (bool isOut, RestartType restart) =
                BallCollision.CheckBoundaries(ball, prev, lastTouchTeamID: 1);

            Assert.IsTrue(isOut);
            Assert.AreEqual(RestartType.KickOff, restart);
        }

        [Test]
        public void PositionOnlyOverload_KeepsDetectedPositionSemantics()
        {
            // The per-position overload (BallStateMachine.IsOutOfBounds's contract class)
            // degenerates to prev == pos: the same ball adjudicates at the detected position —
            // over the bar, goal kick. Documents the retained old semantics, not a defect.
            BallState ball = Flying(new Vector3(GoalLineX + 0.47f, CentreY, 2.60f),
                                    new Vector3(25f, 0f, 20f));

            (bool isOut, RestartType restart) =
                BallCollision.CheckBoundaries(ball, lastTouchTeamID: 0);

            Assert.IsTrue(isOut);
            Assert.AreEqual(RestartType.GoalKick, restart);
        }

        // ── ERR-001-007: a rebound that returns an OutOfPlay ball to the pitch ───────────────

        private const float Dt = 1f / 60f;

        /// <summary>
        /// One engine physics step in RunPhysicsPhase order: integrate (the state machine runs
        /// inside, on the integrated position), then the swept frame test against the tick's
        /// starting position. Returns the post-integration, pre-rebound state.
        /// </summary>
        private static BallStateType Step(ref BallState ball, out bool hit)
        {
            Vector3 prev = ball.Position;
            BallPhysicsCore.UpdateBallPhysics(ref ball, Dt, SurfaceType.GrassDry, Vector3.zero, null, 0f);
            BallStateType integrated = ball.State;
            hit = BallCollision.ApplySweptGoalFrameCollision(ref ball, prev);
            return integrated;
        }

        /// <summary>The restart seam's precondition: a ball is OutOfPlay exactly when it is out.</summary>
        private static void AssertStateAgreesWithBoundary(BallState ball, string context)
        {
            Assert.AreEqual(BallStateMachine.IsOutOfBounds(ball.Position),
                ball.State == BallStateType.OutOfPlay,
                context + ": OutOfPlay must hold exactly when the ball is out of bounds, or the "
                + "restart check (which reads the position) never releases it. pos=" + ball.Position
                + " state=" + ball.State);
        }

        private static void AssertReturnedToPlay(
            BallState ball, BallStateType integrated, bool hit, BallStateType expected, string context)
        {
            Assert.IsTrue(hit, context + ": the segment must strike the frame");
            Assert.AreEqual(BallStateType.OutOfPlay, integrated,
                context + ": precondition — integration alone carried the ball past the line");
            Assert.IsFalse(BallStateMachine.IsOutOfBounds(ball.Position),
                context + ": the rebound contact point is on the pitch side of the line");
            Assert.AreEqual(expected, ball.State, context + ": post-rebound state");
            AssertStateAgreesWithBoundary(ball, context);

            Vector3 before = ball.Position;
            Step(ref ball, out _);
            Assert.AreNotEqual(before, ball.Position,
                context + ": the ball must keep moving after the rebound, not freeze");
        }

        [Test]
        public void PostRebound_FromBeyondTheLine_ReturnsTheBallToPlay_HomeGoal()
        {
            // The captured Unity deadlock (seed 0x0F1E2D3C4B5A6978, tick 56,955 -> 56,956): an
            // airborne ball crosses the home goal line into the outside of the near post. Before
            // ERR-001-007 the rebound left it OutOfPlay at x = 0.027 with no restart, frozen.
            BallState ball = BallState.CreateAtPosition(new Vector3(0.0341889262f, 30.1042576f, 0.468117356f));
            ball.Velocity = new Vector3(-12.580143f, 14.4362392f, -2.21744323f);
            ball.State = BallStateType.Airborne;

            BallStateType integrated = Step(ref ball, out bool hit);

            AssertReturnedToPlay(ball, integrated, hit, BallStateType.Airborne, "home near post");
        }

        [Test]
        public void PostRebound_FromBeyondTheLine_ReturnsTheBallToPlay_AwayGoal()
        {
            // The same strike point-mirrored to the away goal (x -> 105 - x, y -> 68 - y).
            float w = BallPhysicsConstants.Pitch.WIDTH;
            BallState ball = BallState.CreateAtPosition(
                new Vector3(GoalLineX - 0.0341889262f, w - 30.1042576f, 0.468117356f));
            ball.Velocity = new Vector3(12.580143f, -14.4362392f, -2.21744323f);
            ball.State = BallStateType.Airborne;

            BallStateType integrated = Step(ref ball, out bool hit);

            AssertReturnedToPlay(ball, integrated, hit, BallStateType.Airborne, "away near post");
        }

        [Test]
        public void PostRebound_GroundBall_ReturnsToRolling_BothGoals()
        {
            float w = BallPhysicsConstants.Pitch.WIDTH;
            float rest = BallPhysicsConstants.Ball.RADIUS;

            BallState home = BallState.CreateAtPosition(new Vector3(0.03f, 30.10f, rest));
            home.Velocity = new Vector3(-12f, 14f, 0f);
            home.State = BallStateType.Rolling;
            BallStateType homeIntegrated = Step(ref home, out bool homeHit);
            AssertReturnedToPlay(home, homeIntegrated, homeHit, BallStateType.Rolling, "home ground ball");

            BallState away = BallState.CreateAtPosition(new Vector3(GoalLineX - 0.03f, w - 30.10f, rest));
            away.Velocity = new Vector3(12f, -14f, 0f);
            away.State = BallStateType.Rolling;
            BallStateType awayIntegrated = Step(ref away, out bool awayHit);
            AssertReturnedToPlay(away, awayIntegrated, awayHit, BallStateType.Rolling, "away ground ball");
        }

        [Test]
        public void CrossbarRebound_FromBeyondTheLine_ReturnsTheBallToPlay_BothGoals()
        {
            // A flat ball at bar height crosses the line within the tick; the front face of the
            // bar turns it back. The contact point is in play, so it must not stay OutOfPlay.
            BallState home = Flying(new Vector3(0.2f, CentreY, 2.45f), new Vector3(-20f, 0f, 0f));
            BallStateType homeIntegrated = Step(ref home, out bool homeHit);
            AssertReturnedToPlay(home, homeIntegrated, homeHit, BallStateType.Airborne, "home crossbar");
            Assert.Greater(home.Velocity.x, 0f, "home crossbar: reflected back into the pitch");

            BallState away = Flying(new Vector3(GoalLineX - 0.2f, CentreY, 2.45f), new Vector3(20f, 0f, 0f));
            BallStateType awayIntegrated = Step(ref away, out bool awayHit);
            AssertReturnedToPlay(away, awayIntegrated, awayHit, BallStateType.Airborne, "away crossbar");
            Assert.Less(away.Velocity.x, 0f, "away crossbar: reflected back into the pitch");
        }

        [Test]
        public void FrameStrikeWithoutALineCrossing_KeepsTheIntegratedState()
        {
            // Control: an existing front-face strike that never crossed the line is untouched by
            // the ERR-001-007 re-derivation (Airborne in, Airborne out).
            BallState ball = Flying(new Vector3(GoalLineX - 0.5f, PostY, 1.0f), new Vector3(30f, 0f, 0f));
            BallStateType integrated = Step(ref ball, out bool hit);

            Assert.IsTrue(hit);
            Assert.AreEqual(BallStateType.Airborne, integrated);
            Assert.AreEqual(BallStateType.Airborne, ball.State);
            AssertStateAgreesWithBoundary(ball, "no line crossing");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-28 | —      | Initial. ERR-001-005 locks: post tunneling (the discrete-test      |
// |         |            |        | discriminator), restitution/spin retention, crossbar strike,       |
// |         |            |        | mouth/over-frame non-clips, Controlled/degenerate/starts-inside    |
// |         |            |        | gates, crossing-point adjudication both ends + the position-only   |
// |         |            |        | overload's retained semantics.                                     |
// | 1.1     | 2026-10-11 | —      | ERR-001-007 locks: a frame rebound that returns a ball the state   |
// |         |            |        | machine already set OutOfPlay to the pitch must leave it in play   |
// |         |            |        | and moving — the captured Unity post strike (home) and its point   |
// |         |            |        | mirror (away), a ground ball at both goals, a crossbar strike at   |
// |         |            |        | both goals, and a no-crossing control.                             |
#endregion
