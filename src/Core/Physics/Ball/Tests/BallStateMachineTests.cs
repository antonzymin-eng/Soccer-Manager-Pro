using NUnit.Framework;
using UnityEngine;
using TacticalDirector.Core.Physics.Ball;

namespace TacticalDirector.Core.Physics.Ball.Tests
{
    /// <summary>
    /// Unit tests for BallStateMachine state transitions and hysteresis.
    /// Spec §3.1.3 and §3.1.14.
    /// </summary>
    public class BallStateMachineTests
    {
        // ── ROLLING transitions ──────────────────────────────────────────────────

        [Test]
        public void Rolling_BelowMinVelocity_TransitionsToStationary()
        {
            var ball = new BallState
            {
                State    = BallStateType.ROLLING,
                Position = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity = new Vector3(BallPhysicsConstants.State.MIN_VELOCITY * 0.5f, 0f, 0f)
            };

            Assert.AreEqual(BallStateType.STATIONARY, BallStateMachine.UpdateBallState(ball));
        }

        [Test]
        public void Rolling_AboveEnterThreshold_TransitionsToAirborne()
        {
            var ball = new BallState
            {
                State    = BallStateType.ROLLING,
                Position = new Vector3(50f, 34f, BallPhysicsConstants.State.AIRBORNE_ENTER_THRESHOLD + 0.01f),
                Velocity = new Vector3(5f, 0f, 0.5f)
            };

            Assert.AreEqual(BallStateType.AIRBORNE, BallStateMachine.UpdateBallState(ball));
        }

        // ── Hysteresis (§3.1.14) ─────────────────────────────────────────────────

        [Test]
        public void Hysteresis_Rolling_BelowEnterThreshold_StaysRolling()
        {
            // Position is between EXIT (0.13m) and ENTER (0.17m) thresholds
            float midBand = (BallPhysicsConstants.State.AIRBORNE_EXIT_THRESHOLD
                           + BallPhysicsConstants.State.AIRBORNE_ENTER_THRESHOLD) / 2f;

            var ball = new BallState
            {
                State    = BallStateType.ROLLING,
                Position = new Vector3(50f, 34f, midBand),
                Velocity = new Vector3(5f, 0f, 0.1f)
            };

            Assert.AreEqual(BallStateType.ROLLING, BallStateMachine.UpdateBallState(ball),
                "ROLLING should stay ROLLING below ENTER threshold");
        }

        [Test]
        public void Hysteresis_Airborne_AboveExitThreshold_StaysAirborne()
        {
            float midBand = (BallPhysicsConstants.State.AIRBORNE_EXIT_THRESHOLD
                           + BallPhysicsConstants.State.AIRBORNE_ENTER_THRESHOLD) / 2f;

            var ball = new BallState
            {
                State    = BallStateType.AIRBORNE,
                Position = new Vector3(50f, 34f, midBand),
                Velocity = new Vector3(5f, 0f, -0.1f)
            };

            Assert.AreEqual(BallStateType.AIRBORNE, BallStateMachine.UpdateBallState(ball),
                "AIRBORNE should stay AIRBORNE above EXIT threshold (hysteresis)");
        }

        [Test]
        public void Hysteresis_FullCycle_NoBouncing()
        {
            // From ROLLING at 0.15m → AIRBORNE at 0.18m → back to 0.15m stays AIRBORNE
            var ball = new BallState
            {
                State    = BallStateType.ROLLING,
                Position = new Vector3(50f, 34f, 0.15f),
                Velocity = new Vector3(5f, 0f, 0.1f)
            };

            // Still ROLLING below enter threshold
            Assert.AreEqual(BallStateType.ROLLING, BallStateMachine.UpdateBallState(ball));

            // Above enter threshold → goes AIRBORNE
            ball.Position = new Vector3(50f, 34f, 0.18f);
            Assert.AreEqual(BallStateType.AIRBORNE, BallStateMachine.UpdateBallState(ball));

            // AIRBORNE at 0.15m (between thresholds) — hysteresis keeps it AIRBORNE
            ball.State    = BallStateType.AIRBORNE;
            ball.Position = new Vector3(50f, 34f, 0.15f);
            ball.Velocity = new Vector3(5f, 0f, -0.1f);
            Assert.AreEqual(BallStateType.AIRBORNE, BallStateMachine.UpdateBallState(ball));
        }

        // ── AIRBORNE transitions ─────────────────────────────────────────────────

        [Test]
        public void Airborne_BelowExitThreshold_WithDownwardVelocity_TransitionsToBouncing()
        {
            var ball = new BallState
            {
                State    = BallStateType.AIRBORNE,
                Position = new Vector3(50f, 34f, BallPhysicsConstants.State.AIRBORNE_EXIT_THRESHOLD - 0.01f),
                Velocity = new Vector3(5f, 0f, -2f)
            };

            Assert.AreEqual(BallStateType.BOUNCING, BallStateMachine.UpdateBallState(ball));
        }

        [Test]
        public void Airborne_BelowExitThreshold_WithUpwardVelocity_StaysAirborne()
        {
            // Ball is rising — should not trigger BOUNCING
            var ball = new BallState
            {
                State    = BallStateType.AIRBORNE,
                Position = new Vector3(50f, 34f, BallPhysicsConstants.State.AIRBORNE_EXIT_THRESHOLD - 0.01f),
                Velocity = new Vector3(5f, 0f, 2f)
            };

            Assert.AreEqual(BallStateType.AIRBORNE, BallStateMachine.UpdateBallState(ball));
        }

        // ── BOUNCING transitions ─────────────────────────────────────────────────

        [Test]
        public void Bouncing_HighRebound_TransitionsToAirborne()
        {
            var ball = new BallState
            {
                State    = BallStateType.BOUNCING,
                Velocity = new Vector3(5f, 0f, BallPhysicsConstants.State.BOUNCE_VELOCITY_THRESHOLD + 0.5f)
            };

            Assert.AreEqual(BallStateType.AIRBORNE, BallStateMachine.UpdateBallState(ball));
        }

        [Test]
        public void Bouncing_LowRebound_TransitionsToRolling()
        {
            var ball = new BallState
            {
                State    = BallStateType.BOUNCING,
                Velocity = new Vector3(5f, 0f, BallPhysicsConstants.State.BOUNCE_VELOCITY_THRESHOLD * 0.5f)
            };

            Assert.AreEqual(BallStateType.ROLLING, BallStateMachine.UpdateBallState(ball));
        }

        // ── Out of bounds ────────────────────────────────────────────────────────

        [Test]
        public void OutOfBounds_BeyondTouchline_ReturnsTrue()
        {
            Vector3 pos = new Vector3(52f, BallPhysicsConstants.Pitch.WIDTH + BallPhysicsConstants.Ball.RADIUS + 0.01f, 0f);
            Assert.IsTrue(BallStateMachine.IsOutOfBounds(pos));
        }

        [Test]
        public void OutOfBounds_InsidePitch_ReturnsFalse()
        {
            Vector3 pos = new Vector3(52f, 34f, 0f);
            Assert.IsFalse(BallStateMachine.IsOutOfBounds(pos));
        }

        // ── Locked states ────────────────────────────────────────────────────────

        [Test]
        public void Stationary_AlwaysReturnsStationary()
        {
            var ball = new BallState { State = BallStateType.STATIONARY };
            Assert.AreEqual(BallStateType.STATIONARY, BallStateMachine.UpdateBallState(ball));
        }

        [Test]
        public void Controlled_AlwaysReturnsControlled()
        {
            var ball = new BallState { State = BallStateType.CONTROLLED };
            Assert.AreEqual(BallStateType.CONTROLLED, BallStateMachine.UpdateBallState(ball));
        }

        [Test]
        public void OutOfPlay_AlwaysReturnsOutOfPlay()
        {
            var ball = new BallState { State = BallStateType.OUT_OF_PLAY };
            Assert.AreEqual(BallStateType.OUT_OF_PLAY, BallStateMachine.UpdateBallState(ball));
        }
    }
}
