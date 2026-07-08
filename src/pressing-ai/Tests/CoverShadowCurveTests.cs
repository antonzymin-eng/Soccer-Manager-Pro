// File:     src/pressing-ai/Tests/CoverShadowCurveTests.cs
// Created:  2026-07-07
// Modified: 2026-07-07
// Author:   —
// Spec:     Pressing AI #13 §3.3–§3.5, new §7.12 (cheap-item addition, redesigned), Code Standards #20
// Purpose:  Locks CoverShadowCurve: effectiveness is an attribute average normalised to [0,1], a
//           low-attribute presser barely bends the target, a high-attribute presser bends noticeably
//           toward the carrier→receiver shadow lane, and the no-carrier/no-receiver no-op paths.

using NUnit.Framework;
using UnityEngine;

namespace TacticalDirector.PressingAI.Tests
{
    [TestFixture]
    internal class CoverShadowCurveTests
    {
        private static PressingAgentSnapshot Presser(int entityId, int teamId, float attr) =>
            new PressingAgentSnapshot
            {
                EntityId = entityId,
                TeamId = teamId,
                DefensivePositioningAttribute = attr,
                PhysicalEffortAttribute       = attr,
                MentalSharpnessAttribute      = attr,
            };

        private static PressingSnapshot BuildSnapshot(
            int carrierId, Vector2 carrierPos, int receiverId, Vector2 receiverPos, int pressingTeamId)
        {
            var snap = new PressingSnapshot();
            snap.BallCarrierEntityId = carrierId;
            snap.Agents[carrierId] = new PressingAgentSnapshot
            {
                EntityId = carrierId, TeamId = 1 - pressingTeamId, Position = carrierPos, IsActive = true,
            };
            if (receiverId >= 0)
            {
                snap.Agents[receiverId] = new PressingAgentSnapshot
                {
                    EntityId = receiverId, TeamId = 1 - pressingTeamId, Position = receiverPos, IsActive = true,
                };
            }
            return snap;
        }

        [Test]
        public void ComputeCurveEffectiveness_IsAttributeAverageOverScaleMax()
        {
            PressingAgentSnapshot presser = Presser(5, 0, 10f);
            float expected = 10f / PressingAIConstants.ATTRIBUTE_SCALE_MAX;
            Assert.AreEqual(expected, CoverShadowCurve.ComputeCurveEffectiveness(in presser), 1e-5f);
        }

        [Test]
        public void ComputeCurveEffectiveness_ClampsToUnitRange()
        {
            PressingAgentSnapshot presser = Presser(5, 0, 999f);
            Assert.AreEqual(1.0f, CoverShadowCurve.ComputeCurveEffectiveness(in presser), 1e-5f);
        }

        [Test]
        public void ApplyCurve_NoCarrier_ReturnsUnchanged()
        {
            var snap = new PressingSnapshot { BallCarrierEntityId = -1 };
            PressingAgentSnapshot presser = Presser(0, 0, 18f);
            Vector2 target = new Vector2(20f, 10f);

            Assert.AreEqual(target, CoverShadowCurve.ApplyCurve(target, snap, in presser));
        }

        [Test]
        public void ApplyCurve_NoEligibleReceiverNearCarrier_ReturnsUnchanged()
        {
            // Carrier present but no receiver registered in range.
            var snap = BuildSnapshot(carrierId: 3, carrierPos: new Vector2(50f, 34f),
                                      receiverId: -1, receiverPos: Vector2.zero, pressingTeamId: 0);
            PressingAgentSnapshot presser = Presser(0, 0, 18f);
            Vector2 target = new Vector2(48f, 32f);

            Assert.AreEqual(target, CoverShadowCurve.ApplyCurve(target, snap, in presser));
        }

        [Test]
        public void ApplyCurve_HighEffectivenessPresser_BendsTowardShadowLane()
        {
            var snap = BuildSnapshot(carrierId: 3, carrierPos: new Vector2(50f, 34f),
                                      receiverId: 4, receiverPos: new Vector2(55f, 40f), pressingTeamId: 0);
            PressingAgentSnapshot presser = Presser(0, 0, 20f); // max attributes ⇒ effectiveness = 1.0
            Vector2 target = new Vector2(50f, 34f); // straight at the carrier

            Vector2 curved = CoverShadowCurve.ApplyCurve(target, snap, in presser);

            Vector2 shadowLane = Vector2.Lerp(new Vector2(50f, 34f), new Vector2(55f, 40f),
                PressingAIConstants.CoverShadowLaneFraction);
            Vector2 expected = Vector2.Lerp(target, shadowLane, PressingAIConstants.CoverCurveBlendWeightMax);

            Assert.AreEqual(expected.x, curved.x, 1e-4f);
            Assert.AreEqual(expected.y, curved.y, 1e-4f);
            Assert.Greater(Vector2.Distance(curved, target), 0.01f,
                "A maximally-effective presser must visibly bend off the straight-line target.");
        }

        [Test]
        public void ApplyCurve_LowEffectivenessPresser_BarelyBends()
        {
            var snap = BuildSnapshot(carrierId: 3, carrierPos: new Vector2(50f, 34f),
                                      receiverId: 4, receiverPos: new Vector2(55f, 40f), pressingTeamId: 0);
            PressingAgentSnapshot poorPresser = Presser(0, 0, 1f); // minimum attributes ⇒ effectiveness ≈ 0.05
            Vector2 target = new Vector2(50f, 34f);

            Vector2 curved = CoverShadowCurve.ApplyCurve(target, snap, in poorPresser);

            Assert.Less(Vector2.Distance(curved, target), 0.5f,
                "A poor, low-attribute presser must curve only marginally — effectively a straight line.");
        }

        [Test]
        public void ApplyCurve_HigherEffectiveness_BendsMoreThanLowerEffectiveness()
        {
            var snap = BuildSnapshot(carrierId: 3, carrierPos: new Vector2(50f, 34f),
                                      receiverId: 4, receiverPos: new Vector2(55f, 40f), pressingTeamId: 0);
            Vector2 target = new Vector2(50f, 34f);

            PressingAgentSnapshot poor = Presser(0, 0, 4f);
            PressingAgentSnapshot sharp = Presser(0, 0, 18f);

            float poorBend  = Vector2.Distance(CoverShadowCurve.ApplyCurve(target, snap, in poor), target);
            float sharpBend = Vector2.Distance(CoverShadowCurve.ApplyCurve(target, snap, in sharp), target);

            Assert.Greater(sharpBend, poorBend,
                "A defensively/physically/mentally sharper presser must curve more than a poorer one.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                              |
// | 1.0     | 2026-07-07 | —      | Initial implementation, replacing BlindSideApproachTests.cs (deleted) after |
// |         |            |        |   the user-reviewed redesign to attribute-gated cover-shadow curving.       |
#endregion
