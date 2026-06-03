// File:     src/ball-physics/tests/BodyPartCoefficientsTests.cs
// Created:  2026-06-03
// Modified: 2026-06-03 (AR-5 fix pass)
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Unit tests for BodyPartCoefficients.Get — locks the AR-1 L-4 throw-on-
//           unknown contract and the AR-3 M-1 catalogue round-trip into the new
//           BallPhysicsConstants.BodyPartRetention block.

using System;

using NUnit.Framework;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.BallPhysics.Tests
{
    /// <summary>
    /// Lookup-contract tests for <see cref="BodyPartCoefficients"/>.
    /// AR-4 L-2 closes the test-coverage gap on the AR-1 L-4 throw branch and the
    /// AR-3 M-1 catalogue rewiring.
    /// </summary>
    public class BodyPartCoefficientsTests
    {
        [Test]
        public void Get_AllKnownBodyParts_RoundTripsCatalogueValues()
        {
            (float speed, float spin) foot  = BodyPartCoefficients.Get(BodyPart.Foot);
            (float speed, float spin) shin  = BodyPartCoefficients.Get(BodyPart.Shin);
            (float speed, float spin) thigh = BodyPartCoefficients.Get(BodyPart.Thigh);
            (float speed, float spin) torso = BodyPartCoefficients.Get(BodyPart.Torso);
            (float speed, float spin) head  = BodyPartCoefficients.Get(BodyPart.Head);
            (float speed, float spin) arm   = BodyPartCoefficients.Get(BodyPart.Arm);

            Assert.AreEqual(BallPhysicsConstants.BodyPartRetention.FootSpeed,  foot.speed);
            Assert.AreEqual(BallPhysicsConstants.BodyPartRetention.FootSpin,   foot.spin);
            Assert.AreEqual(BallPhysicsConstants.BodyPartRetention.ShinSpeed,  shin.speed);
            Assert.AreEqual(BallPhysicsConstants.BodyPartRetention.ShinSpin,   shin.spin);
            Assert.AreEqual(BallPhysicsConstants.BodyPartRetention.ThighSpeed, thigh.speed);
            Assert.AreEqual(BallPhysicsConstants.BodyPartRetention.ThighSpin,  thigh.spin);
            Assert.AreEqual(BallPhysicsConstants.BodyPartRetention.TorsoSpeed, torso.speed);
            Assert.AreEqual(BallPhysicsConstants.BodyPartRetention.TorsoSpin,  torso.spin);
            Assert.AreEqual(BallPhysicsConstants.BodyPartRetention.HeadSpeed,  head.speed);
            Assert.AreEqual(BallPhysicsConstants.BodyPartRetention.HeadSpin,   head.spin);
            Assert.AreEqual(BallPhysicsConstants.BodyPartRetention.ArmSpeed,   arm.speed);
            Assert.AreEqual(BallPhysicsConstants.BodyPartRetention.ArmSpin,    arm.spin);
        }

        [Test]
        public void Get_UnknownBodyPart_ThrowsArgumentOutOfRangeException()
        {
            // Cast-from-int to simulate a caller that bypasses the enum surface
            // (e.g., deserialised int from a stale save file).
            BodyPart unknown = (BodyPart)100;

            Assert.Throws<ArgumentOutOfRangeException>(
                () => BodyPartCoefficients.Get(unknown),
                "BodyPartCoefficients.Get MUST fail fast on unknown enum values (AR-1 L-4).");
        }

        [Test]
        public void Get_EveryDeclaredBodyPart_HasCatalogueEntry()
        {
            // Future-proofing: enumerate every declared BodyPart and assert the
            // catalogue has an entry. Catches the case where a new enum member is
            // added without a matching BodyPartCoefficients dictionary row — the
            // hardcoded round-trip test above would silently miss this gap.
            foreach (BodyPart part in Enum.GetValues(typeof(BodyPart)))
            {
                Assert.DoesNotThrow(
                    () => BodyPartCoefficients.Get(part),
                    $"BodyPart.{part} has no entry in BodyPartCoefficients catalogue — extend BodyPartRetention + BodyPartCoefficients atomically.");
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-03 | —      | Initial implementation. AR-4 L-2: closes the test-coverage gap on |
// |         |            |        | the AR-1 L-4 throw-on-unknown contract and the AR-3 M-1            |
// |         |            |        | BodyPartRetention catalogue round-trip.                            |
// | 1.1     | 2026-06-03 | —      | AR-5 fixes. M-1: file header Modified field added (FR-CS-056).    |
// |         |            |        | L-2: new Get_EveryDeclaredBodyPart_HasCatalogueEntry test iterates |
// |         |            |        | Enum.GetValues(typeof(BodyPart)) so a future enum extension        |
// |         |            |        | without a matching catalogue row fails the test suite immediately. |
#endregion
