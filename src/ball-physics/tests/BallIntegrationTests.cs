// File:     src/ball-physics/tests/BallIntegrationTests.cs
// Created:  2026-05-24
// Modified: 2026-06-02
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  End-to-end integration tests against §3.1.14 derived validation test cases.
//           Each test simulates a realistic scenario and verifies output within the
//           expected range derived in the spec.

using NUnit.Framework;
using UnityEngine;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.BallPhysics.Tests
{
    /// <summary>
    /// End-to-end integration tests against §3.1.14 derived validation test cases.
    /// Each test simulates a realistic scenario and verifies the output is within
    /// the specified expected range derived in the spec.
    /// </summary>
    public class BallIntegrationTests
    {
        private const float DT = 1f / 60f;

        // ── §3.1.14: Free kick curve (1.5–3.0 m lateral deviation over 25 m) ────

        [Test]
        public void FreekickTrajectory_CurvesWithinExpectedRange()
        {
            var ball = new BallState
            {
                State             = BallStateType.Airborne,
                Position          = new Vector3(25f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(22f, 0f, 6f),
                AngularVelocity   = new Vector3(0f, 0f, -12f),
                LastValidPosition = new Vector3(25f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(22f, 0f, 6f)
            };

            float startY  = ball.Position.y;
            int   maxSteps = 300;

            while (ball.Position.x < 50f
                && ball.Position.z >= BallPhysicsConstants.Ball.RADIUS
                && maxSteps-- > 0)
            {
                BallPhysicsCore.UpdateBallPhysics(
                    ref ball, DT, SurfaceType.GrassDry, Vector3.zero, null, 0f);
            }

            float lateralDeviation = Mathf.Abs(ball.Position.y - startY);

            Assert.That(lateralDeviation, Is.GreaterThan(1.5f),
                $"Ball curved only {lateralDeviation:F2} m — expected >1.5 m");
            Assert.That(lateralDeviation, Is.LessThan(3.0f),
                $"Ball curved {lateralDeviation:F2} m — expected <3.0 m");
        }

        // ── §3.1.14: Rolling distance (26–31 m on dry grass at 10 m/s) ──────────

        [Test]
        public void RollingDistance_DryGrass_StopsWithinExpectedRange()
        {
            var ball = new BallState
            {
                State             = BallStateType.Rolling,
                Position          = new Vector3(0f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(10f, 0f, 0f),
                AngularVelocity   = Vector3.zero,
                LastValidPosition = new Vector3(0f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(10f, 0f, 0f)
            };

            int maxSteps = 60 * 15;

            while (ball.State == BallStateType.Rolling && maxSteps-- > 0)
            {
                BallPhysicsCore.UpdateBallPhysics(
                    ref ball, DT, SurfaceType.GrassDry, Vector3.zero, null, 0f);
            }

            float stoppingDistance = ball.Position.x;

            Assert.That(stoppingDistance, Is.GreaterThan(26f),
                $"Ball stopped at {stoppingDistance:F1} m — expected >26 m");
            Assert.That(stoppingDistance, Is.LessThan(31f),
                $"Ball stopped at {stoppingDistance:F1} m — expected <31 m");
        }

        // ── Possession: ApplyKick + SetBallControlled round-trip ─────────────────

        [Test]
        public void Possession_SetControlled_ThenKick_TransitionsCorrectly()
        {
            var ball = BallState.CreateAtPosition(new Vector3(52.5f, 34f, BallPhysicsConstants.Ball.RADIUS));

            BallCollision.SetBallControlled(ref ball);
            Assert.AreEqual(BallStateType.Controlled, ball.State);
            Assert.AreEqual(Vector3.zero, ball.Velocity);

            KickResult kick = BallCollision.ApplyKick(
                ref ball,
                velocity:  new Vector3(20f, 0f, 5f),
                spin:      new Vector3(0f, 0f, -10f),
                agentId:   0,
                matchTime: 0f,
                logger:    null);

            Assert.AreEqual(KickResult.Applied, kick);
            Assert.AreEqual(BallStateType.Airborne, ball.State,
                "Kick with positive z velocity must produce Airborne state");
            Assert.That(ball.Velocity.magnitude, Is.GreaterThan(0f));
        }

        [Test]
        public void Possession_GroundKick_TransitionsToRolling()
        {
            var ball = BallState.CreateAtPosition(new Vector3(52.5f, 34f, BallPhysicsConstants.Ball.RADIUS));
            BallCollision.SetBallControlled(ref ball);

            KickResult kick = BallCollision.ApplyKick(
                ref ball,
                velocity:  new Vector3(10f, 0f, 0f),
                spin:      Vector3.zero,
                agentId:   1,
                matchTime: 0f);

            Assert.AreEqual(KickResult.Applied, kick);
            Assert.AreEqual(BallStateType.Rolling, ball.State);
        }

        // ── AR-1 M-5: kick rejection feedback ─────────────────────────────────────

        [Test]
        public void Possession_NaNKick_RejectedWithFeedback_BallStateUnchanged()
        {
            // Suppress the Debug.LogError that ApplyKick emits on rejection so this
            // test does not fail under NUnit's "log error fails test" default.
            UnityEngine.TestTools.LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(@"\[BallPhysics\] ApplyKick: Invalid velocity"));

            var ball = BallState.CreateAtPosition(new Vector3(52.5f, 34f, BallPhysicsConstants.Ball.RADIUS));
            BallCollision.SetBallControlled(ref ball);

            Vector3 stateBeforeVelocity = ball.Velocity;
            BallStateType stateBefore   = ball.State;

            KickResult kick = BallCollision.ApplyKick(
                ref ball,
                velocity:  new Vector3(float.NaN, 0f, 0f),
                spin:      Vector3.zero,
                agentId:   2,
                matchTime: 0f);

            Assert.AreEqual(KickResult.RejectedNonFiniteVelocity, kick);
            Assert.AreEqual(stateBefore, ball.State,
                "Rejected kick must not transition ball out of Controlled");
            Assert.AreEqual(stateBeforeVelocity, ball.Velocity,
                "Rejected kick must not mutate Velocity");
        }

        // ── Boundary detection ───────────────────────────────────────────────────

        [Test]
        public void Boundary_BallCrossesTouchline_ReturnsThrowIn()
        {
            var ball = new BallState
            {
                State    = BallStateType.Rolling,
                Position = new Vector3(52f, -BallPhysicsConstants.Ball.RADIUS - 0.01f, 0f),
                Velocity = new Vector3(5f, -2f, 0f)
            };

            var (isOut, restart) = BallCollision.CheckBoundaries(ball, lastTouchTeamID: 0);

            Assert.IsTrue(isOut);
            Assert.AreEqual(RestartType.ThrowIn, restart);
        }

        [Test]
        public void Boundary_BallEntersGoal_ReturnsKickoff()
        {
            float goalCenterY = BallPhysicsConstants.Pitch.WIDTH / 2f;
            var ball = new BallState
            {
                State    = BallStateType.Rolling,
                Position = new Vector3(
                    -BallPhysicsConstants.Ball.RADIUS - 0.01f,
                    goalCenterY,
                    BallPhysicsConstants.Ball.RADIUS),
                Velocity = new Vector3(-5f, 0f, 0f)
            };

            var (isOut, restart) = BallCollision.CheckBoundaries(ball, lastTouchTeamID: 1);

            Assert.IsTrue(isOut);
            Assert.AreEqual(RestartType.KickOff, restart);
        }

        // ── Goal post collision ───────────────────────────────────────────────────

        [Test]
        public void GoalPostCollision_ReflectsVelocity()
        {
            var ball = new BallState
            {
                Position        = new Vector3(0f, 34f, 1f),
                Velocity        = new Vector3(-15f, 0f, 0f),
                AngularVelocity = Vector3.zero,
                State           = BallStateType.Airborne
            };

            Vector3 postCenter   = new Vector3(0f, 34f, 1f);
            Vector3 contactPoint = new Vector3(0.06f, 34f, 1f);

            BallCollision.ApplyGoalPostCollision(ref ball, contactPoint, postCenter, null, 0f);

            Assert.That(ball.Velocity.x, Is.GreaterThan(0f),
                "Ball must rebound away from post");
        }

        // ── NaN recovery under full update ───────────────────────────────────────

        [Test]
        public void FullUpdate_NaNInput_RecoversWithoutException()
        {
            UnityEngine.TestTools.LogAssert.Expect(
                LogType.Error,
                new System.Text.RegularExpressions.Regex(@"\[BallPhysics\] NaN/Infinity detected"));

            var ball = new BallState
            {
                State             = BallStateType.Airborne,
                Position          = new Vector3(float.NaN, 34f, 5f),
                Velocity          = new Vector3(20f, 0f, 2f),
                AngularVelocity   = Vector3.zero,
                LastValidPosition = new Vector3(50f, 34f, 5f),
                LastValidVelocity = new Vector3(20f, 0f, 2f)
            };

            Assert.DoesNotThrow(() =>
                BallPhysicsCore.UpdateBallPhysics(ref ball, DT, SurfaceType.GrassDry, Vector3.zero, null, 0f));

            Assert.IsFalse(float.IsNaN(ball.Position.x), "Position must not be NaN after recovery");
        }

        // ── Event logging ─────────────────────────────────────────────────────────

        [Test]
        public void EventLogger_LogsBounce_EventIsRecorded()
        {
            var logger = new BallEventLogger();
            var ball   = new BallState
            {
                State             = BallStateType.Bouncing,
                Position          = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity          = new Vector3(5f, 0f, -4f),
                LastValidPosition = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                LastValidVelocity = new Vector3(5f, 0f, -4f)
            };

            BallGroundInteraction.ApplyBounce(ref ball, SurfaceType.GrassDry, logger, 10f);

            System.Collections.Generic.List<BallEvent> events = logger.ExportEvents();
            Assert.AreEqual(1, events.Count);
            Assert.AreEqual(BallEventType.Bounce, events[0].Type);
            Assert.AreEqual(10f, events[0].Timestamp, 0.001f);
            Assert.AreEqual(SurfaceType.GrassDry, events[0].Surface,
                "Typed Surface field must be populated (AR-1 H-1 zero-alloc refactor)");
        }

        // ── IT-TRJ-002 ────────────────────────────────────────────────────────────

        [Test]
        public void LongBall_DecaysRealistically()
        {
            float radius = BallPhysicsConstants.Ball.RADIUS;
            var ball = new BallState
            {
                State             = BallStateType.Airborne,
                Position          = new Vector3(0f, 34f, radius),
                Velocity          = new Vector3(25f, 0f, 8f),
                AngularVelocity   = Vector3.zero,
                LastValidPosition = new Vector3(0f, 34f, radius),
                LastValidVelocity = new Vector3(25f, 0f, 8f)
            };

            float peakHeight = radius;
            int   steps      = 0;
            int   maxSteps   = 600;

            while (ball.Position.z >= radius && steps < maxSteps)
            {
                if (ball.Position.z > peakHeight)
                    peakHeight = ball.Position.z;

                BallPhysicsCore.UpdateBallPhysics(ref ball, DT, SurfaceType.GrassDry, Vector3.zero, null, 0f);
                steps++;
            }

            float flightTime = steps * DT;

            Assert.That(peakHeight, Is.InRange(4f, 6f),
                $"Peak height {peakHeight:F2} m outside expected 4–6 m window");
            Assert.That(flightTime, Is.InRange(1.8f, 2.2f),
                $"Flight time {flightTime:F2} s outside expected 1.8–2.2 s window");
            Assert.That(ball.Velocity.magnitude, Is.LessThan(25f),
                "Drag must reduce landing speed below launch speed");
        }

        // ── IT-MBC-001 ────────────────────────────────────────────────────────────

        [Test]
        public void BouncingBall_SettlesWithinReasonableTime()
        {
            float radius = BallPhysicsConstants.Ball.RADIUS;
            var ball = new BallState
            {
                State             = BallStateType.Bouncing,
                Position          = new Vector3(52.5f, 34f, 5f + radius),
                Velocity          = new Vector3(0f, 0f, -9.9f),
                AngularVelocity   = Vector3.zero,
                LastValidPosition = new Vector3(52.5f, 34f, 5f + radius),
                LastValidVelocity = new Vector3(0f, 0f, -9.9f)
            };

            int maxSteps        = 300;
            int transitionCount = 0;
            BallStateType prevState = ball.State;

            for (int i = 0; i < maxSteps; i++)
            {
                BallPhysicsCore.UpdateBallPhysics(ref ball, DT, SurfaceType.GrassDry, Vector3.zero, null, 0f);

                if (ball.State != prevState)
                {
                    transitionCount++;
                    prevState = ball.State;
                }

                if (ball.State == BallStateType.Stationary || ball.State == BallStateType.Rolling)
                    break;
            }

            Assert.That(ball.State == BallStateType.Stationary || ball.State == BallStateType.Rolling,
                $"Ball should settle within {maxSteps} steps; actual state: {ball.State}");
            Assert.That(transitionCount, Is.LessThan(60),
                $"Too many state transitions ({transitionCount}) — ball should not oscillate excessively");
        }

        // ── IT-MBC-002 ────────────────────────────────────────────────────────────

        [Test]
        public void BouncingBall_LosesEnergyMonotonically()
        {
            float radius = BallPhysicsConstants.Ball.RADIUS;
            var ball = new BallState
            {
                State             = BallStateType.Bouncing,
                Position          = new Vector3(52.5f, 34f, 2f + radius),
                Velocity          = new Vector3(0f, 0f, -6.26f),
                AngularVelocity   = Vector3.zero,
                LastValidPosition = new Vector3(52.5f, 34f, 2f + radius),
                LastValidVelocity = new Vector3(0f, 0f, -6.26f)
            };

            float h1         = -1f;
            float h2         = -1f;
            float h3         = -1f;
            bool  risingPrev = false;
            int   bouncesSeen = 0;
            int   maxSteps   = 600;

            for (int i = 0; i < maxSteps && bouncesSeen < 3; i++)
            {
                BallPhysicsCore.UpdateBallPhysics(ref ball, DT, SurfaceType.GrassDry, Vector3.zero, null, 0f);

                bool risingNow = ball.Velocity.z > 0f;

                if (risingPrev && !risingNow && ball.State == BallStateType.Airborne)
                {
                    bouncesSeen++;
                    if      (bouncesSeen == 1) h1 = ball.Position.z;
                    else if (bouncesSeen == 2) h2 = ball.Position.z;
                    else if (bouncesSeen == 3) h3 = ball.Position.z;
                }

                risingPrev = risingNow;
            }

            Assert.That(bouncesSeen, Is.GreaterThanOrEqualTo(3),
                "Must observe at least 3 bounce peaks to compare energy loss");
            Assert.That(h1, Is.GreaterThan(h2), $"1st peak {h1:F3} m should exceed 2nd peak {h2:F3} m");
            Assert.That(h2, Is.GreaterThan(h3), $"2nd peak {h2:F3} m should exceed 3rd peak {h3:F3} m");
        }

        // ── IT-STS-001 ────────────────────────────────────────────────────────────

        [Test]
        public void StateSequence_KickToGround_ValidTransitions()
        {
            float radius = BallPhysicsConstants.Ball.RADIUS;
            var ball = BallState.CreateAtPosition(new Vector3(0f, 34f, radius));

            BallCollision.ApplyKick(ref ball, new Vector3(15f, 0f, 6f), Vector3.zero, 0, 0f);

            bool seenAirborne = false;
            bool seenGround   = false;

            for (int i = 0; i < 600; i++)
            {
                BallPhysicsCore.UpdateBallPhysics(ref ball, DT, SurfaceType.GrassDry, Vector3.zero, null, 0f);

                if (ball.State == BallStateType.Airborne) seenAirborne = true;
                if (ball.State == BallStateType.Bouncing || ball.State == BallStateType.Rolling) seenGround = true;

                if (ball.State == BallStateType.Stationary || ball.State == BallStateType.Rolling)
                    break;
            }

            Assert.IsTrue(seenAirborne, "Kick should produce Airborne state");
            Assert.IsTrue(seenGround,   "Ball should reach Bouncing or Rolling after Airborne");
        }

        // ── IT-STS-002 ────────────────────────────────────────────────────────────

        [Test]
        public void StateSequence_ChipPass_NoBounceDeadlock()
        {
            float radius = BallPhysicsConstants.Ball.RADIUS;
            var ball = BallState.CreateAtPosition(new Vector3(52.5f, 34f, radius));
            BallCollision.SetBallControlled(ref ball);

            BallCollision.ApplyKick(ref ball, new Vector3(8f, 0f, 4f), Vector3.zero, 1, 0f);

            int  bouncingStreak   = 0;
            bool deadlockDetected = false;
            bool bounced          = false;

            for (int i = 0; i < 360; i++)
            {
                BallPhysicsCore.UpdateBallPhysics(ref ball, DT, SurfaceType.GrassDry, Vector3.zero, null, 0f);

                if (ball.State == BallStateType.Bouncing)
                {
                    bounced = true;
                    bouncingStreak++;
                    if (bouncingStreak > 10)
                    {
                        deadlockDetected = true;
                        break;
                    }
                }
                else
                {
                    bouncingStreak = 0;
                }
            }

            Assert.IsTrue(bounced,           "Chip pass should produce at least one Bouncing state");
            Assert.IsFalse(deadlockDetected, "Ball must not remain stuck in Bouncing for > 10 consecutive frames");
        }

        // ── IT-COL-002 ────────────────────────────────────────────────────────────

        [Test]
        public void CrossbarCollision_DeflectsVelocityDownward()
        {
            float goalHeight = BallPhysicsConstants.Pitch.GOAL_HEIGHT;
            var ball = new BallState
            {
                Position        = new Vector3(105f, 34f, goalHeight),
                Velocity        = new Vector3(10f, 0f, 5f),
                AngularVelocity = Vector3.zero,
                State           = BallStateType.Airborne
            };

            Vector3 postCenter   = new Vector3(105f, 34f, goalHeight);
            Vector3 contactPoint = new Vector3(105f - 0.06f, 34f, goalHeight);

            BallCollision.ApplyGoalPostCollision(ref ball, contactPoint, postCenter, null, 0f);

            Assert.That(ball.Velocity.z, Is.LessThan(0f),
                "Crossbar impact should deflect velocity downward (negative Z)");
            Assert.That(ball.Velocity.magnitude, Is.GreaterThan(0f),
                "Ball must retain non-zero speed after crossbar collision");
        }

        // ── IT-LOG-001 ────────────────────────────────────────────────────────────

        [Test]
        public void EventLogger_CapturesKickEvent()
        {
            float radius = BallPhysicsConstants.Ball.RADIUS;
            var ball = BallState.CreateAtPosition(new Vector3(52.5f, 34f, radius));
            BallCollision.SetBallControlled(ref ball);

            var logger = new BallEventLogger();
            BallCollision.ApplyKick(ref ball, new Vector3(15f, 0f, 6f), Vector3.zero, 7, 5f, logger);

            System.Collections.Generic.List<BallEvent> kickEvents = logger.ExportEvents();
            Assert.That(kickEvents.Count, Is.GreaterThanOrEqualTo(1),
                "ApplyKick with logger must record at least one event");

            bool hasKickEvent = false;
            for (int i = 0; i < kickEvents.Count; i++)
            {
                if (kickEvents[i].Type == BallEventType.Kick)
                {
                    hasKickEvent = true;
                    Assert.AreEqual(BallStateType.Airborne, kickEvents[i].ResultingState,
                        "Typed ResultingState field must be populated (AR-1 H-1)");
                    break;
                }
            }

            Assert.IsTrue(hasKickEvent, "A Kick event must be recorded by the logger");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Bug fix: Boundary_BallEntersGoal_ReturnsKickoff had z=0.5f which   |
// |         |            |        | exceeded Ball.Diameter (0.22 m) making lowEnough=false — goal was  |
// |         |            |        | never detected; corrected to Ball.RADIUS (ground level).           |
// |         |            |        | Standards fix: namespace → TacticalDirector.BallPhysics.Tests;     |
// |         |            |        | ALL_CAPS constant refs → PascalCase; file header per FR-CS-056/057. |
// | 1.2     | 2026-06-01 | —      | Add 7 missing spec §5 integration tests: IT-TRJ-002, IT-MBC-001,  |
// |         |            |        | IT-MBC-002, IT-STS-001, IT-STS-002, IT-COL-002, IT-LOG-001.        |
// |         |            |        | Fix EventLogger_LogsBounce_EventIsRecorded: var → explicit          |
// |         |            |        | List<BallEvent> type per FR-CS-013.                                 |
// | 1.3     | 2026-06-02 | —      | AR-1 fixes. H-2: file header path corrected to src/ball-physics/.  |
// |         |            |        | M-4: BallStateType / SurfaceType / RestartType members renamed     |
// |         |            |        | PascalCase across all test inputs. M-5: new                        |
// |         |            |        | Possession_NaNKick_RejectedWithFeedback test locks ApplyKick's     |
// |         |            |        | KickResult return contract. H-1 follow-on: EventLogger tests now   |
// |         |            |        | also assert the typed Surface / ResultingState fields so the       |
// |         |            |        | zero-alloc refactor is covered. LogAssert.Expect added on the two  |
// |         |            |        | tests that intentionally exercise NaN-error paths so NUnit's       |
// |         |            |        | "log error fails test" default does not break them.                |
#endregion
