// File:     src/attacking-ai/Tests/TacticTranslationTests.cs
// Created:  2026-06-29
// Modified: 2026-06-29
// Author:   —
// Spec:     Tactical Instructions #21 §3.3, FR-TI-021 / FR-TI-031; Attacking AI #15 §3.8; Code Standards #20
// Purpose:  Locks the #21 → #15 T2 consumer seam: FocusPlay → preferred-flank translation and the
//           AttackingSnapshot Mixed-zero-value behaviour-neutrality contract.

using NUnit.Framework;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.AttackingAI.Tests
{
    [TestFixture]
    internal class TacticTranslationTests
    {
        // ── §3.3: FocusPlay maps onto a preferred flank, or null for no lateral preference ──

        [Test]
        public void PreferredFlank_MapsEachFocusPlay()
        {
            Assert.IsNull(TacticTranslation.PreferredFlank(FocusPlay.Mixed));
            Assert.IsNull(TacticTranslation.PreferredFlank(FocusPlay.ThroughMiddle));
            Assert.AreEqual(Flank.Left,  TacticTranslation.PreferredFlank(FocusPlay.LeftFlank));
            Assert.AreEqual(Flank.Right, TacticTranslation.PreferredFlank(FocusPlay.RightFlank));
        }

        // ── FR-TI-031: Mixed (zero-value identity) expresses no preference ──

        [Test]
        public void Mixed_IsIdentity_NoPreference()
        {
            Assert.IsNull(TacticTranslation.PreferredFlank(FocusPlay.Mixed));
        }

        // The default snapshot's auto-property zero-value is Mixed, so the seam is a no-op by default.
        [Test]
        public void DefaultSnapshot_FocusPlay_IsMixedIdentity()
        {
            AttackingSnapshot snapshot = new AttackingSnapshot();
            Assert.AreEqual(FocusPlay.Mixed, snapshot.FocusPlay);
            Assert.IsNull(TacticTranslation.PreferredFlank(snapshot.FocusPlay));
        }
    }
}
