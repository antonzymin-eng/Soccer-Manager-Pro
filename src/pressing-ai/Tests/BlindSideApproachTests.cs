// File:     src/pressing-ai/Tests/BlindSideApproachTests.cs
// Created:  2026-07-07
// Modified: 2026-07-07
// Author:   —
// Spec:     Pressing AI #13 §3.3, new §7.12 (cheap-item addition), Code Standards #20
// Purpose:  Locks BlindSideApproach.ApplyBias: the blind-side direction is opposite the carrier's
//           facing, no-carrier/inactive-carrier/degenerate-facing are no-ops, and the bias magnitude
//           matches PressingAIConstants.BlindSideApproachBiasM.

using NUnit.Framework;
using UnityEngine;

namespace TacticalDirector.PressingAI.Tests
{
    [TestFixture]
    internal class BlindSideApproachTests
    {
        private static PressingSnapshot BuildSnapshot(int carrierId, Vector2 carrierFacing, bool carrierActive = true)
        {
            var snap = new PressingSnapshot();
            snap.BallCarrierEntityId = carrierId;
            if (carrierId >= 0)
            {
                snap.Agents[carrierId] = new PressingAgentSnapshot
                {
                    EntityId = carrierId,
                    TeamId   = 1,
                    Position = new Vector2(50f, 34f),
                    Facing   = carrierFacing,
                    IsActive = carrierActive,
                };
            }
            return snap;
        }

        [Test]
        public void ApplyBias_NudgesTowardOppositeOfCarrierFacing()
        {
            var snap = BuildSnapshot(carrierId: 5, carrierFacing: new Vector2(1f, 0f));
            Vector2 target = new Vector2(50f, 34f);

            Vector2 biased = BlindSideApproach.ApplyBias(target, snap);

            Vector2 expected = target + new Vector2(-1f, 0f) * PressingAIConstants.BlindSideApproachBiasM;
            Assert.AreEqual(expected.x, biased.x, 1e-5f);
            Assert.AreEqual(expected.y, biased.y, 1e-5f);
        }

        [Test]
        public void ApplyBias_NoCarrier_ReturnsUnchanged()
        {
            var snap = BuildSnapshot(carrierId: -1, carrierFacing: Vector2.zero);
            Vector2 target = new Vector2(20f, 10f);

            Assert.AreEqual(target, BlindSideApproach.ApplyBias(target, snap));
        }

        [Test]
        public void ApplyBias_InactiveCarrier_ReturnsUnchanged()
        {
            var snap = BuildSnapshot(carrierId: 3, carrierFacing: new Vector2(0f, 1f), carrierActive: false);
            Vector2 target = new Vector2(20f, 10f);

            Assert.AreEqual(target, BlindSideApproach.ApplyBias(target, snap));
        }

        [Test]
        public void ApplyBias_DegenerateFacing_ReturnsUnchanged()
        {
            var snap = BuildSnapshot(carrierId: 3, carrierFacing: Vector2.zero);
            Vector2 target = new Vector2(20f, 10f);

            Assert.AreEqual(target, BlindSideApproach.ApplyBias(target, snap));
        }

        [Test]
        public void ApplyBias_NormalisesNonUnitFacing()
        {
            // A non-unit facing (e.g. (2, 0)) must still produce a bias of exactly
            // BlindSideApproachBiasM magnitude, not scaled by the facing's own length.
            var snap = BuildSnapshot(carrierId: 5, carrierFacing: new Vector2(2f, 0f));
            Vector2 target = Vector2.zero;

            Vector2 biased = BlindSideApproach.ApplyBias(target, snap);

            Assert.AreEqual(PressingAIConstants.BlindSideApproachBiasM, biased.magnitude, 1e-5f);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-07-07 | —      | Initial implementation (cheap-item addition). |
#endregion
