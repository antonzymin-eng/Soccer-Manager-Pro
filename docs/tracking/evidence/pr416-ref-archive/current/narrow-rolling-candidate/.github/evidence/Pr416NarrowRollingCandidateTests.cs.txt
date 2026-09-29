using NUnit.Framework;
using UnityEngine;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.BallPhysics.Tests
{
    [TestFixture]
    public sealed class Pr416NarrowRollingCandidateTests
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
        public void PhysicsTick_SlowElevatedRolling_AppliesGravityImmediately()
        {
            var ball = new BallState
            {
                State = BallStateType.Rolling,
                Position = new Vector3(
                    50f, 34f,
                    BallPhysicsConstants.State.AirborneEnterThreshold + 0.05f),
                Velocity = new Vector3(
                    BallPhysicsConstants.State.MinVelocity * 0.5f, 0f, 0f),
                LastValidPosition = new Vector3(
                    50f, 34f,
                    BallPhysicsConstants.State.AirborneEnterThreshold + 0.05f),
                LastValidVelocity = new Vector3(
                    BallPhysicsConstants.State.MinVelocity * 0.5f, 0f, 0f)
            };

            BallPhysicsCore.UpdateBallPhysics(
                ref ball, 1f / 60f, SurfaceType.GrassDry, Vector3.zero, null, 0f);

            Assert.AreEqual(BallStateType.Airborne, ball.State);
            Assert.Less(ball.Velocity.z, 0f);
        }
    }
}
