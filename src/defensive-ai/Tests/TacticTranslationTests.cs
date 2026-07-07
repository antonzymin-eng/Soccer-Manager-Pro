// File:     src/defensive-ai/Tests/TacticTranslationTests.cs
// Created:  2026-06-29
// Modified: 2026-07-07
// Author:   —
// Spec:     Tactical Instructions #21 §3.4, FR-TI-022 / FR-TI-031 / FR-TI-033; Defensive AI #14 §3.7; Code Standards #20
// Purpose:  Locks the #21 → #14 T2 consumer seam: OffsideTrap → trap-request passthrough, the
//           MarkingOrientation → MAN_MARK radius scalar (cheap-item addition), and both
//           DefensiveSnapshot default-seed behaviour-neutrality contracts.

using NUnit.Framework;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.DefensiveAI.Tests
{
    [TestFixture]
    internal class TacticTranslationTests
    {
        // ── §3.4: OffsideTrap is a bool passthrough onto the #14 request flag ──

        [Test]
        public void OffsideTrapRequested_IsIdentityPassthrough()
        {
            Assert.IsFalse(TacticTranslation.OffsideTrapRequested(false));
            Assert.IsTrue(TacticTranslation.OffsideTrapRequested(true));
        }

        // ── FR-TI-031: the default snapshot seeds the false identity (no manager trap request) ──

        [Test]
        public void DefaultSnapshot_OffsideTrapRequested_IsFalseIdentity()
        {
            DefensiveSnapshot snapshot = new DefensiveSnapshot();
            Assert.IsFalse(snapshot.OffsideTrapRequested,
                "Default DefensiveSnapshot must carry the false OffsideTrap identity (FR-TI-031).");
            Assert.AreEqual(snapshot.OffsideTrapRequested,
                            TacticTranslation.OffsideTrapRequested(false));
        }

        // ── Cheap-item addition (FR-TI-033): MarkingOrientation → MAN_MARK candidate radius scalar ──

        [Test]
        public void MarkRadiusScalar_BalancedIsIdentity()
        {
            Assert.AreEqual(1.0f, TacticTranslation.MarkRadiusScalar(MarkingOrientation.Balanced));
        }

        [Test]
        public void MarkRadiusScalar_MonotoneBallToMan()
        {
            float ball = TacticTranslation.MarkRadiusScalar(MarkingOrientation.BallOriented);
            float bal  = TacticTranslation.MarkRadiusScalar(MarkingOrientation.Balanced);
            float man  = TacticTranslation.MarkRadiusScalar(MarkingOrientation.ManOriented);
            Assert.Less(ball, bal, "BallOriented must narrow the radius relative to Balanced.");
            Assert.Greater(man, bal, "ManOriented must widen the radius relative to Balanced.");
        }

        [Test]
        public void MarkRadiusScalar_OutOfRangeOrdinal_ClampsToBoldestPeer()
        {
            float atMax = TacticTranslation.MarkRadiusScalar(MarkingOrientation.ManOriented);
            float beyond = TacticTranslation.MarkRadiusScalar((MarkingOrientation)99);
            Assert.AreEqual(atMax, beyond, "F5: an out-of-range ordinal must clamp to the boldest peer.");
        }

        [Test]
        public void DefaultSnapshot_MarkingOrientation_IsBalancedIdentity()
        {
            DefensiveSnapshot snapshot = new DefensiveSnapshot();
            Assert.AreEqual(MarkingOrientation.Balanced, snapshot.MarkingOrientation,
                "Default DefensiveSnapshot must ctor-seed Balanced — the enum zero-value is BallOriented, " +
                "not identity (same zero-value-trap class as #13's LineOfEngagement).");
        }
    }
}
