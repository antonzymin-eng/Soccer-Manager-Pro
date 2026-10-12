// File:     src/ball-physics/tests/SweptGoalFrameTests.cs
// Created:  2026-07-28
// Modified: 2026-10-12 (ERR-001-007: goal-frame/state ordering)
// Modified: 2026-07-28
// Author:   —
// Spec:     Ball Physics #1 §3.1.10 (ERR-001-005); shot-speed & woodwork design KD-4/KD-5;
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

        [TestCase(false, false)]
        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public void PhysicsUpdate_FrameBeforeFullCrossing_KeepsReboundPlayable(bool homeGoal, bool crossbar)
        {
            float goalX = homeGoal ? 0f : GoalLineX;
            float direction = homeGoal ? -1f : 1f;
            float y = crossbar ? CentreY : PostY;
            float z = crossbar ? BarZ : 1f;
            BallState ball = Flying(new Vector3(goalX - direction * 0.3f, y, z),
                new Vector3(direction * 30f, 0f, 0f));

            BallPhysicsCore.UpdateBallPhysics(ref ball, 1f / 60f, SurfaceType.GrassDry,
                Vector3.zero, null, 0f);

            Assert.AreEqual(BallStateType.Airborne, ball.State);
            Assert.IsFalse(BallStateMachine.IsOutOfBounds(ball.Position));
            Assert.Less(direction * ball.Velocity.x, 0f, "The front face must reflect into play.");
            Vector3 rebound = ball.Position;
            BallPhysicsCore.UpdateBallPhysics(ref ball, 1f / 60f, SurfaceType.GrassDry,
                Vector3.zero, null, 0f);
            Assert.AreNotEqual(rebound, ball.Position, "A rebound must keep moving on the next tick.");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PhysicsUpdate_RollingPostRebound_RemainsRollingAndMoves(bool homeGoal)
        {
            float goalX = homeGoal ? 0f : GoalLineX;
            float direction = homeGoal ? -1f : 1f;
            BallState ball = Flying(new Vector3(goalX - direction * 0.3f, PostY,
                BallPhysicsConstants.Ball.RADIUS), new Vector3(direction * 30f, 0f, 0f));
            ball.State = BallStateType.Rolling;

            BallPhysicsCore.UpdateBallPhysics(ref ball, 1f / 60f, SurfaceType.GrassDry,
                Vector3.zero, null, 0f);

            Assert.AreEqual(BallStateType.Rolling, ball.State, "The fix must retain the ground force model.");
            Assert.IsFalse(BallStateMachine.IsOutOfBounds(ball.Position));
            Assert.Less(direction * ball.Velocity.x, 0f);
            Vector3 rebound = ball.Position;
            BallPhysicsCore.UpdateBallPhysics(ref ball, 1f / 60f, SurfaceType.GrassDry,
                Vector3.zero, null, 0f);
            Assert.AreNotEqual(rebound, ball.Position);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PhysicsUpdate_FullCrossingBeforeFrameContact_PreservesTheExit(bool homeGoal)
        {
            float goalX = homeGoal ? 0f : GoalLineX;
            float direction = homeGoal ? -1f : 1f;
            BallState ball = Flying(new Vector3(goalX + direction * 0.05f, PostY + 0.4f, 1f),
                new Vector3(direction * 8.4f, -30f, 0f));
            Vector3 previous = ball.Position;

            BallPhysicsCore.UpdateBallPhysics(ref ball, 1f / 60f, SurfaceType.GrassDry,
                Vector3.zero, null, 0f);

            Assert.AreEqual(BallStateType.OutOfPlay, ball.State);
            Assert.IsTrue(BallStateMachine.IsOutOfBounds(ball.Position));
            Assert.Less(ball.Velocity.y, 0f, "A later rear-side frame contact cannot reflect an already-out ball.");
            var boundary = BallCollision.CheckBoundaries(ball, previous, homeGoal ? 1 : 0);
            Assert.IsTrue(boundary.isOut);
            Assert.AreEqual(RestartType.GoalKick, boundary.restart);
        }

        [Test]
        public void OutOfPlayBall_IsNeverReturnedToPlayByAFrameCollision()
        {
            BallState ball = Flying(new Vector3(GoalLineX + 0.4f, PostY, 1f), new Vector3(30f, 0f, 0f));
            ball.State = BallStateType.OutOfPlay;
            BallState before = ball;
            Assert.IsFalse(BallCollision.ApplySweptGoalFrameCollision(ref ball,
                new Vector3(GoalLineX - 0.5f, PostY, 1f)));
            Assert.AreEqual(before.Position, ball.Position);
            Assert.AreEqual(before.Velocity, ball.Velocity);
            Assert.AreEqual(before.State, ball.State);
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
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-28 | —      | Initial. ERR-001-005 locks: post tunneling (the discrete-test      |
// |         |            |        | discriminator), restitution/spin retention, crossbar strike,       |
// |         |            |        | mouth/over-frame non-clips, Controlled/degenerate/starts-inside    |
// |         |            |        | gates, crossing-point adjudication both ends + the position-only   |
// |         |            |        | overload's retained semantics.                                     |
// | 1.1 | 2026-10-12 | — | ERR-001-007: nine cases lock airborne/rolling post and crossbar rebounds at both goals, full-crossing precedence and absorbing OutOfPlay. |
#endregion
