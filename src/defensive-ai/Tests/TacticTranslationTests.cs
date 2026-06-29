// File:     src/defensive-ai/Tests/TacticTranslationTests.cs
// Created:  2026-06-29
// Modified: 2026-06-29
// Author:   —
// Spec:     Tactical Instructions #21 §3.4, FR-TI-022 / FR-TI-031; Defensive AI #14 §3.7; Code Standards #20
// Purpose:  Locks the #21 → #14 T2 consumer seam: OffsideTrap → trap-request passthrough and the
//           DefensiveSnapshot false-seed behaviour-neutrality contract (KD-9 request-not-guarantee).

using NUnit.Framework;

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
    }
}
