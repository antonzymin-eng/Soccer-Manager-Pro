// File:     src/pressing-ai/Tests/TacticTranslationTests.cs
// Created:  2026-06-28
// Modified: 2026-06-28
// Author:   —
// Spec:     Tactical Instructions #21 §3.4, FR-TI-017 / FR-TI-031; Pressing AI #13 §3.3; Code Standards #20
// Purpose:  Locks the #21 → #13 T2 consumer seam: LineOfEngagement → press-trigger-radius
//           scalar validity + F5 clamp, the Standard identity row, the §3.4 monotone shape,
//           and the PressingSnapshot Standard-seed behaviour-neutrality contract.

using NUnit.Framework;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.PressingAI.Tests
{
    [TestFixture]
    internal class TacticTranslationTests
    {
        // ── §3.4: each LineOfEngagement maps onto its catalogue scalar (direct ordinal lookup) ──

        [Test]
        public void PressTriggerRadiusScalar_MapsEachLineToItsCatalogueRow()
        {
            Assert.AreEqual(TacticalInstructionsConstants.LineOfEngagementScalar[(int)LineOfEngagement.VeryLow],
                            TacticTranslation.PressTriggerRadiusScalar(LineOfEngagement.VeryLow));
            Assert.AreEqual(TacticalInstructionsConstants.LineOfEngagementScalar[(int)LineOfEngagement.Standard],
                            TacticTranslation.PressTriggerRadiusScalar(LineOfEngagement.Standard));
            Assert.AreEqual(TacticalInstructionsConstants.LineOfEngagementScalar[(int)LineOfEngagement.VeryHigh],
                            TacticTranslation.PressTriggerRadiusScalar(LineOfEngagement.VeryHigh));
        }

        // ── §3.4 / FR-TI-031: Standard is the exact identity (×1.0), so the seam is a no-op ──

        [Test]
        public void PressTriggerRadiusScalar_Standard_IsExactlyOne()
        {
            Assert.AreEqual(1.0f, TacticTranslation.PressTriggerRadiusScalar(LineOfEngagement.Standard));
        }

        // The default-constructed snapshot must resolve to the identity scalar — the zero-value
        // enum default is VeryLow (×0.80), so the ctor must seed Standard for behaviour-neutrality.
        [Test]
        public void DefaultSnapshot_SeedsStandard_ResolvesToIdentityScalar()
        {
            PressingSnapshot snapshot = new PressingSnapshot();
            Assert.AreEqual(LineOfEngagement.Standard, snapshot.LineOfEngagement);
            Assert.AreEqual(1.0f, TacticTranslation.PressTriggerRadiusScalar(snapshot.LineOfEngagement));
        }

        // ── §3.4 shape: the scalar is monotone in the line of engagement (the reviewable contract) ──

        [Test]
        public void PressTriggerRadiusScalar_IsMonotoneIncreasing()
        {
            Assert.Less(TacticTranslation.PressTriggerRadiusScalar(LineOfEngagement.VeryLow),
                        TacticTranslation.PressTriggerRadiusScalar(LineOfEngagement.Standard));
            Assert.Less(TacticTranslation.PressTriggerRadiusScalar(LineOfEngagement.Standard),
                        TacticTranslation.PressTriggerRadiusScalar(LineOfEngagement.VeryHigh));
        }

        // ── §3.1 F5: a widened (out-of-range) value clamps to the boldest peer, VeryHigh ──

        [Test]
        public void PressTriggerRadiusScalar_WidenedValue_ClampsToVeryHigh()
        {
            Assert.AreEqual(TacticTranslation.PressTriggerRadiusScalar(LineOfEngagement.VeryHigh),
                            TacticTranslation.PressTriggerRadiusScalar((LineOfEngagement)99));
        }
    }
}
