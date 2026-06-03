// File:     src/ball-physics/tests/BallStateMachineTests.cs
// Created:  2026-05-24
// Modified: 2026-06-02
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Unit tests for BallStateMachine state transitions and hysteresis.
//           Covers Spec §3.1.3 and §3.1.14.

using NUnit.Framework;
using UnityEngine;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.BallPhysics.Tests
{
    /// <summary>
    /// Unit tests for BallStateMachine state transitions and hysteresis.
    /// Spec §3.1.3 and §3.1.14.
    /// </summary>
    public class BallStateMachineTests
    {
        // ── Rolling transitions ──────────────────────────────────────────────────

        [Test]
        public void Rolling_BelowMinVelocity_TransitionsToStationary()
        {
            var ball = new BallState
            {
                State    = BallStateType.Rolling,
                Position = new Vector3(50f, 34f, BallPhysicsConstants.Ball.RADIUS),
                Velocity = new Vector3(BallPhysicsConstants.State.MinVelocity * 0.5f, 0f, 0f)
            };

            Assert.AreEqual(BallStateType.Stationary, BallStateMachine.UpdateBallState(ball));
        }

        [Test]
        public void Rolling_AboveEnterThreshold_TransitionsToAirborne()
        {
            var ball = new BallState
            {
                State    = BallStateType.Rolling,
                Position = new Vector3(50f, 34f, BallPhysicsConstants.State.AirborneEnterThreshold + 0.01f),
                Velocity = new Vector3(5f, 0f, 0.5f)
            };

            Assert.AreEqual(BallStateType.Airborne, BallStateMachine.UpdateBallState(ball));
        }

        // ── Hysteresis (§3.1.14) ─────────────────────────────────────────────────

        [Test]
        public void Hysteresis_Rolling_BelowEnterThreshold_StaysRolling()
        {
            float midBand = (BallPhysicsConstants.State.AirborneExitThreshold
                           + BallPhysicsConstants.State.AirborneEnterThreshold) / 2f;

            var ball = new BallState
            {
                State    = BallStateType.Rolling,
                Position = new Vector3(50f, 34f, midBand),
                Velocity = new Vector3(5f, 0f, 0.1f)
            };

            Assert.AreEqual(BallStateType.Rolling, BallStateMachine.UpdateBallState(ball),
                "Rolling should stay Rolling below ENTER threshold");
        }

        [Test]
        public void Hysteresis_Airborne_AboveExitThreshold_StaysAirborne()
        {
            float midBand = (BallPhysicsConstants.State.AirborneExitThreshold
                           + BallPhysicsConstants.State.AirborneEnterThreshold) / 2f;

            var ball = new BallState
            {
                State    = BallStateType.Airborne,
                Position = new Vector3(50f, 34f, midBand),
                Velocity = new Vector3(5f, 0f, -0.1f)
            };

            Assert.AreEqual(BallStateType.Airborne, BallStateMachine.UpdateBallState(ball),
                "Airborne should stay Airborne above EXIT threshold (hysteresis)");
        }

        [Test]
        public void Hysteresis_FullCycle_NoBouncing()
        {
            var ball = new BallState
            {
                State    = BallStateType.Rolling,
                Position = new Vector3(50f, 34f, 0.15f),
                Velocity = new Vector3(5f, 0f, 0.1f)
            };

            Assert.AreEqual(BallStateType.Rolling, BallStateMachine.UpdateBallState(ball));

            ball.Position = new Vector3(50f, 34f, 0.18f);
            Assert.AreEqual(BallStateType.Airborne, BallStateMachine.UpdateBallState(ball));

            ball.State    = BallStateType.Airborne;
            ball.Position = new Vector3(50f, 34f, 0.15f);
            ball.Velocity = new Vector3(5f, 0f, -0.1f);
            Assert.AreEqual(BallStateType.Airborne, BallStateMachine.UpdateBallState(ball));
        }

        // ── Airborne transitions ─────────────────────────────────────────────────

        [Test]
        public void Airborne_BelowExitThreshold_WithDownwardVelocity_TransitionsToBouncing()
        {
            var ball = new BallState
            {
                State    = BallStateType.Airborne,
                Position = new Vector3(50f, 34f, BallPhysicsConstants.State.AirborneExitThreshold - 0.01f),
                Velocity = new Vector3(5f, 0f, -2f)
            };

            Assert.AreEqual(BallStateType.Bouncing, BallStateMachine.UpdateBallState(ball));
        }

        [Test]
        public void Airborne_BelowExitThreshold_WithUpwardVelocity_StaysAirborne()
        {
            var ball = new BallState
            {
                State    = BallStateType.Airborne,
                Position = new Vector3(50f, 34f, BallPhysicsConstants.State.AirborneExitThreshold - 0.01f),
                Velocity = new Vector3(5f, 0f, 2f)
            };

            Assert.AreEqual(BallStateType.Airborne, BallStateMachine.UpdateBallState(ball));
        }

        // ── Bouncing transitions ─────────────────────────────────────────────────

        [Test]
        public void Bouncing_HighRebound_TransitionsToAirborne()
        {
            var ball = new BallState
            {
                State    = BallStateType.Bouncing,
                Velocity = new Vector3(5f, 0f, BallPhysicsConstants.State.BounceVelocityThreshold + 0.5f)
            };

            Assert.AreEqual(BallStateType.Airborne, BallStateMachine.UpdateBallState(ball));
        }

        [Test]
        public void Bouncing_LowRebound_TransitionsToRolling()
        {
            var ball = new BallState
            {
                State    = BallStateType.Bouncing,
                Velocity = new Vector3(5f, 0f, BallPhysicsConstants.State.BounceVelocityThreshold * 0.5f)
            };

            Assert.AreEqual(BallStateType.Rolling, BallStateMachine.UpdateBallState(ball));
        }

        // ── Out of bounds ────────────────────────────────────────────────────────

        [Test]
        public void OutOfBounds_BeyondTouchline_ReturnsTrue()
        {
            // z must be below Ball.Diameter to satisfy the Stage 0 lowEnough gate
            // (AR-1 H-3: BallStateMachine.IsOutOfBounds now mirrors CheckBoundaries).
            Vector3 pos = new Vector3(
                52f,
                BallPhysicsConstants.Pitch.WIDTH + BallPhysicsConstants.Ball.RADIUS + 0.01f,
                BallPhysicsConstants.Ball.RADIUS);
            Assert.IsTrue(BallStateMachine.IsOutOfBounds(pos));
        }

        [Test]
        public void OutOfBounds_InsidePitch_ReturnsFalse()
        {
            Vector3 pos = new Vector3(52f, 34f, BallPhysicsConstants.Ball.RADIUS);
            Assert.IsFalse(BallStateMachine.IsOutOfBounds(pos));
        }

        [Test]
        public void OutOfBounds_HighAboveTouchline_ReturnsFalse()
        {
            // AR-1 H-3: high-flying ball over the touchline is NOT classified as out
            // at Stage 0 — the z gate (z < Ball.Diameter) keeps it in play until it
            // descends, matching CheckBoundaries.
            Vector3 pos = new Vector3(
                52f,
                BallPhysicsConstants.Pitch.WIDTH + 5f,
                3f);
            Assert.IsFalse(BallStateMachine.IsOutOfBounds(pos),
                "High-flying ball over the touchline must stay in play at Stage 0 (z gate matches CheckBoundaries)");
        }

        // ── Locked states ────────────────────────────────────────────────────────

        [Test]
        public void Stationary_AlwaysReturnsStationary()
        {
            var ball = new BallState { State = BallStateType.Stationary };
            Assert.AreEqual(BallStateType.Stationary, BallStateMachine.UpdateBallState(ball));
        }

        [Test]
        public void Controlled_AlwaysReturnsControlled()
        {
            var ball = new BallState { State = BallStateType.Controlled };
            Assert.AreEqual(BallStateType.Controlled, BallStateMachine.UpdateBallState(ball));
        }

        [Test]
        public void OutOfPlay_AlwaysReturnsOutOfPlay()
        {
            var ball = new BallState { State = BallStateType.OutOfPlay };
            Assert.AreEqual(BallStateType.OutOfPlay, BallStateMachine.UpdateBallState(ball));
        }

        // ── UT-STM-008 ───────────────────────────────────────────────────────────

        [Test]
        public void OutOfBounds_BallPartiallyOverLine_StillInPlay()
        {
            // Spec §5.2.4 UT-STM-008: ball must ENTIRELY cross — centre y must be < -RADIUS.
            Vector3 pos = new Vector3(52f, -0.05f, BallPhysicsConstants.Ball.RADIUS);
            Assert.IsFalse(BallStateMachine.IsOutOfBounds(pos),
                "Ball centre at y=-0.05 has not fully crossed the touchline (RADIUS=0.11)");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-05-24 | —      | Initial implementation.                                            |
// | 1.1     | 2026-05-24 | —      | Fix pass: namespace → TacticalDirector.BallPhysics.Tests; ALL_CAPS |
// |         |            |        | constant refs → PascalCase; file header per FR-CS-056/057.         |
// | 1.2     | 2026-06-01 | —      | Add UT-STM-008: ball partially over touchline still in play         |
// |         |            |        | (spec §5.2.4 — centre must clear by full RADIUS).                  |
// | 1.3     | 2026-06-02 | —      | AR-1 fixes. H-2: file header path corrected to src/ball-physics/.  |
// |         |            |        | H-3: OutOfBounds_BeyondTouchline_ReturnsTrue z lifted to ground    |
// |         |            |        | level (was 0f); new OutOfBounds_HighAboveTouchline_ReturnsFalse    |
// |         |            |        | locks the z-gate alignment with CheckBoundaries; OutOfBounds_Inside|
// |         |            |        | Pitch z bumped to ground level. M-4: BallStateType members renamed |
// |         |            |        | PascalCase across all assertions.                                  |
#endregion
