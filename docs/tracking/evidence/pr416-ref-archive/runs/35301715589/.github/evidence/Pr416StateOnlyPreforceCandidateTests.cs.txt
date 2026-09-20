using NUnit.Framework;
using UnityEngine;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.BallPhysics.Tests
{
    [TestFixture]
    public sealed class Pr416StateOnlyPreforceCandidateTests
    {
        [Test]
        public void SlowElevatedRolling_CannotBecomeStationary()
        {
            var ball = new BallState
            {
                State = BallStateType.Rolling,
                Position = new Vector3(
                    50f, 34f,
                    BallPhysicsConstants.State.AirborneEnterThreshold + 0.01f),
                Velocity = new Vector3(
                    BallPhysicsConstants.State.MinVelocity * 0.5f, 0f, 0f)
            };

            Assert.AreEqual(BallStateType.Airborne, BallStateMachine.UpdateBallState(ball));
        }

        [Test]
        public void MovingElevatedRolling_PreservesExistingRollingTrajectory()
        {
            var ball = new BallState
            {
                State = BallStateType.Rolling,
                Position = new Vector3(
                    50f, 34f,
                    BallPhysicsConstants.State.AirborneEnterThreshold + 0.01f),
                Velocity = new Vector3(5f, 0f, -0.1f)
            };

            Assert.AreEqual(BallStateType.Rolling, BallStateMachine.UpdateBallState(ball));
        }

        [Test]
        public void PhysicsTick_SlowElevatedRolling_TransitionsWithoutStopping_ThenGravityActs()
        {
            float z = BallPhysicsConstants.State.AirborneEnterThreshold + 0.05f;
            var ball = new BallState
            {
                State = BallStateType.Rolling,
                Position = new Vector3(50f, 34f, z),
                Velocity = new Vector3(
                    BallPhysicsConstants.State.MinVelocity * 0.5f, 0f, 0f),
                LastValidPosition = new Vector3(50f, 34f, z),
                LastValidVelocity = new Vector3(
                    BallPhysicsConstants.State.MinVelocity * 0.5f, 0f, 0f)
            };

            BallPhysicsCore.UpdateBallPhysics(
                ref ball, 1f / 60f, SurfaceType.GrassDry, Vector3.zero, null, 0f);

            Assert.AreEqual(BallStateType.Airborne, ball.State,
                "slow elevated Rolling must not transition to Stationary");

            BallPhysicsCore.UpdateBallPhysics(
                ref ball, 1f / 60f, SurfaceType.GrassDry, Vector3.zero, null, 1f / 60f);

            Assert.AreEqual(BallStateType.Airborne, ball.State);
            Assert.Less(ball.Velocity.z, 0f);
        }

        [Test]
        public void PhysicsTick_ElevatedStationary_StillRecoversWithGravityImmediately()
        {
            float z = BallPhysicsConstants.State.AirborneEnterThreshold + 0.05f;
            var ball = new BallState
            {
                State = BallStateType.Stationary,
                Position = new Vector3(50f, 34f, z),
                Velocity = Vector3.zero,
                LastValidPosition = new Vector3(50f, 34f, z),
                LastValidVelocity = Vector3.zero
            };

            BallPhysicsCore.UpdateBallPhysics(
                ref ball, 1f / 60f, SurfaceType.GrassDry, Vector3.zero, null, 0f);

            Assert.AreEqual(BallStateType.Airborne, ball.State);
            Assert.Less(ball.Velocity.z, 0f);
        }
    }
}
