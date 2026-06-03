// File:     src/ball-physics/tests/EnumOrdinalStabilityTests.cs
// Created:  2026-06-03
// Modified: 2026-06-03 (AR-6 fix pass)
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Locks the int ordinals of all six public enums in the ball-physics
//           assembly so the APPEND-only contract documented across AR-3 L-2,
//           AR-4 L-1, AR-5 L-1, and AR-6 L-1 is mechanically enforced. Any future
//           insertion in the middle of an enum fails the test suite immediately;
//           appends at the end fall through (and the new ordinal must be added to
//           the matching test).

using NUnit.Framework;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.BallPhysics.Tests
{
    /// <summary>
    /// Ordinal-stability locks for the six public enums in ball-physics.
    /// Documentation (the ORDINAL STABILITY paragraphs on each enum) sets the
    /// expectation; this file mechanically enforces it. AR-6 L-3.
    /// </summary>
    public class EnumOrdinalStabilityTests
    {
        [Test]
        public void BallStateType_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)BallStateType.Stationary);
            Assert.AreEqual(1, (int)BallStateType.Rolling);
            Assert.AreEqual(2, (int)BallStateType.Airborne);
            Assert.AreEqual(3, (int)BallStateType.Bouncing);
            Assert.AreEqual(4, (int)BallStateType.Controlled);
            Assert.AreEqual(5, (int)BallStateType.OutOfPlay);
        }

        [Test]
        public void BallEventType_OrdinalsAreStable()
        {
            // See BallEventLogger.cs HISTORICAL NOTE: ordinals were renumbered
            // atomically with the AR-2 L-3 drop of four unused members. The
            // APPEND-only rule applies from AR-3 L-2 forward.
            Assert.AreEqual(0, (int)BallEventType.PositionSnapshot);
            Assert.AreEqual(1, (int)BallEventType.Kick);
            Assert.AreEqual(2, (int)BallEventType.Bounce);
            Assert.AreEqual(3, (int)BallEventType.GoalPostHit);
            Assert.AreEqual(4, (int)BallEventType.Goal);
        }

        [Test]
        public void SurfaceType_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)SurfaceType.GrassDry);
            Assert.AreEqual(1, (int)SurfaceType.GrassWet);
            Assert.AreEqual(2, (int)SurfaceType.GrassLong);
            Assert.AreEqual(3, (int)SurfaceType.Artificial);
            Assert.AreEqual(4, (int)SurfaceType.Frozen);
        }

        [Test]
        public void RestartType_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)RestartType.None);
            Assert.AreEqual(1, (int)RestartType.ThrowIn);
            Assert.AreEqual(2, (int)RestartType.GoalKick);
            Assert.AreEqual(3, (int)RestartType.Corner);
            Assert.AreEqual(4, (int)RestartType.KickOff);
        }

        [Test]
        public void KickResult_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)KickResult.Applied);
            Assert.AreEqual(1, (int)KickResult.RejectedNonFiniteVelocity);
        }

        [Test]
        public void BodyPart_OrdinalsAreStable()
        {
            // BodyPart is also embedded cross-spec in
            // collision-system/AgentBallCollisionData.BodyPart — shifting the
            // ordinals would break that consumer in addition to BodyPartCoefficients.
            Assert.AreEqual(0, (int)BodyPart.Foot);
            Assert.AreEqual(1, (int)BodyPart.Shin);
            Assert.AreEqual(2, (int)BodyPart.Thigh);
            Assert.AreEqual(3, (int)BodyPart.Torso);
            Assert.AreEqual(4, (int)BodyPart.Head);
            Assert.AreEqual(5, (int)BodyPart.Arm);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-03 | —      | Initial implementation. AR-6 L-3: mechanically enforces the       |
// |         |            |        | APPEND-only ordinal-stability contract on all six public enums in |
// |         |            |        | ball-physics. Complements the AR-3 L-2 / AR-4 L-1 / AR-5 L-1 /    |
// |         |            |        | AR-6 L-1 documentation paragraphs by converting them into tests.   |
#endregion
