// File:     src/ball-physics/tests/SurfacePropertiesTests.cs
// Created:  2026-06-03
// Author:   —
// Spec:     Ball Physics #1, Code Standards #20
// Purpose:  Unit tests for the four SurfaceProperties.Get* methods — locks the AR-1
//           L-4 throw-on-unknown contract and the round-trip into the
//           BallPhysicsConstants.SurfaceCoR / SurfaceFriction / Rolling /
//           SurfaceSpinRetention catalogues.

using System;

using NUnit.Framework;

using TacticalDirector.BallPhysics;

namespace TacticalDirector.BallPhysics.Tests
{
    /// <summary>
    /// Lookup-contract tests for <see cref="SurfaceProperties"/>.
    /// AR-4 L-2 closes the test-coverage gap on the AR-1 L-4 throw branches.
    /// </summary>
    public class SurfacePropertiesTests
    {
        private const SurfaceType UnknownSurface = (SurfaceType)100;

        // ── GetCoefficientOfRestitution ──────────────────────────────────────────

        [Test]
        public void GetCoefficientOfRestitution_AllKnownSurfaces_RoundTripsCatalogue()
        {
            Assert.AreEqual(BallPhysicsConstants.SurfaceCoR.GrassDry,
                SurfaceProperties.GetCoefficientOfRestitution(SurfaceType.GrassDry));
            Assert.AreEqual(BallPhysicsConstants.SurfaceCoR.GrassWet,
                SurfaceProperties.GetCoefficientOfRestitution(SurfaceType.GrassWet));
            Assert.AreEqual(BallPhysicsConstants.SurfaceCoR.GrassLong,
                SurfaceProperties.GetCoefficientOfRestitution(SurfaceType.GrassLong));
            Assert.AreEqual(BallPhysicsConstants.SurfaceCoR.Artificial,
                SurfaceProperties.GetCoefficientOfRestitution(SurfaceType.Artificial));
            Assert.AreEqual(BallPhysicsConstants.SurfaceCoR.Frozen,
                SurfaceProperties.GetCoefficientOfRestitution(SurfaceType.Frozen));
        }

        [Test]
        public void GetCoefficientOfRestitution_UnknownSurface_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SurfaceProperties.GetCoefficientOfRestitution(UnknownSurface),
                "GetCoefficientOfRestitution MUST fail fast on unknown SurfaceType (AR-1 L-4).");
        }

        // ── GetFrictionCoefficient ───────────────────────────────────────────────

        [Test]
        public void GetFrictionCoefficient_AllKnownSurfaces_RoundTripsCatalogue()
        {
            Assert.AreEqual(BallPhysicsConstants.SurfaceFriction.GrassDry,
                SurfaceProperties.GetFrictionCoefficient(SurfaceType.GrassDry));
            Assert.AreEqual(BallPhysicsConstants.SurfaceFriction.GrassWet,
                SurfaceProperties.GetFrictionCoefficient(SurfaceType.GrassWet));
            Assert.AreEqual(BallPhysicsConstants.SurfaceFriction.GrassLong,
                SurfaceProperties.GetFrictionCoefficient(SurfaceType.GrassLong));
            Assert.AreEqual(BallPhysicsConstants.SurfaceFriction.Artificial,
                SurfaceProperties.GetFrictionCoefficient(SurfaceType.Artificial));
            Assert.AreEqual(BallPhysicsConstants.SurfaceFriction.Frozen,
                SurfaceProperties.GetFrictionCoefficient(SurfaceType.Frozen));
        }

        [Test]
        public void GetFrictionCoefficient_UnknownSurface_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SurfaceProperties.GetFrictionCoefficient(UnknownSurface),
                "GetFrictionCoefficient MUST fail fast on unknown SurfaceType (AR-1 L-4).");
        }

        // ── GetRollingResistance ─────────────────────────────────────────────────

        [Test]
        public void GetRollingResistance_AllKnownSurfaces_RoundTripsCatalogue()
        {
            Assert.AreEqual(BallPhysicsConstants.Rolling.ResistanceGrassDry,
                SurfaceProperties.GetRollingResistance(SurfaceType.GrassDry));
            Assert.AreEqual(BallPhysicsConstants.Rolling.ResistanceGrassWet,
                SurfaceProperties.GetRollingResistance(SurfaceType.GrassWet));
            Assert.AreEqual(BallPhysicsConstants.Rolling.ResistanceGrassLong,
                SurfaceProperties.GetRollingResistance(SurfaceType.GrassLong));
            Assert.AreEqual(BallPhysicsConstants.Rolling.ResistanceArtificial,
                SurfaceProperties.GetRollingResistance(SurfaceType.Artificial));
            Assert.AreEqual(BallPhysicsConstants.Rolling.ResistanceFrozen,
                SurfaceProperties.GetRollingResistance(SurfaceType.Frozen));
        }

        [Test]
        public void GetRollingResistance_UnknownSurface_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SurfaceProperties.GetRollingResistance(UnknownSurface),
                "GetRollingResistance MUST fail fast on unknown SurfaceType (AR-1 L-4).");
        }

        // ── GetSpinRetention ─────────────────────────────────────────────────────

        [Test]
        public void GetSpinRetention_AllKnownSurfaces_RoundTripsCatalogue()
        {
            Assert.AreEqual(BallPhysicsConstants.SurfaceSpinRetention.GrassDry,
                SurfaceProperties.GetSpinRetention(SurfaceType.GrassDry));
            Assert.AreEqual(BallPhysicsConstants.SurfaceSpinRetention.GrassWet,
                SurfaceProperties.GetSpinRetention(SurfaceType.GrassWet));
            Assert.AreEqual(BallPhysicsConstants.SurfaceSpinRetention.GrassLong,
                SurfaceProperties.GetSpinRetention(SurfaceType.GrassLong));
            Assert.AreEqual(BallPhysicsConstants.SurfaceSpinRetention.Artificial,
                SurfaceProperties.GetSpinRetention(SurfaceType.Artificial));
            Assert.AreEqual(BallPhysicsConstants.SurfaceSpinRetention.Frozen,
                SurfaceProperties.GetSpinRetention(SurfaceType.Frozen));
        }

        [Test]
        public void GetSpinRetention_UnknownSurface_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => SurfaceProperties.GetSpinRetention(UnknownSurface),
                "GetSpinRetention MUST fail fast on unknown SurfaceType (AR-1 L-4).");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-03 | —      | Initial implementation. AR-4 L-2: locks the AR-1 L-4 throw-on-    |
// |         |            |        | unknown contracts on all four SurfaceProperties.Get* methods plus  |
// |         |            |        | the round-trip into the SurfaceCoR / SurfaceFriction / Rolling /   |
// |         |            |        | SurfaceSpinRetention catalogues.                                   |
#endregion
