// File:     src/tactical-instructions/Tests/InCodeTacticPresetCatalogueTests.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Tactical Presets #26 §2.2.2 (FR-TP-002/013), KD-6; Code Standards #20
// Purpose:  WS-1 neutrality proof — the default in-code catalogue delivers today's EXACT preset data
//           (dials + affinity + Count + BalancedOrdinal). Host-independent (compares values, not a
//           float-physics digest), so it survives across hosts/SDKs; combined with the ManagerAITests
//           B.1/B.2 formula locks it establishes byte-identity of the refactored manager AI by
//           construction (every consumer became a pass-through returning the same value it read before).

using NUnit.Framework;

namespace TacticalDirector.TacticalInstructions.Tests
{
    [TestFixture]
    internal class InCodeTacticPresetCatalogueTests
    {
        private static ITacticPresetCatalogue Catalogue => new InCodeTacticPresetCatalogue();

        [Test]
        public void Default_MirrorsTacticPresetLibraryScalars()
        {
            ITacticPresetCatalogue c = Catalogue;
            Assert.AreEqual(TacticPresetLibrary.Count, c.Count);
            Assert.AreEqual(TacticPresetLibrary.BalancedOrdinal, c.BalancedOrdinal);
            Assert.AreEqual(5, c.Count, "Stage-0+1 catalogue is the pinned five-preset ladder.");
            Assert.AreEqual(2, (int)c.BalancedOrdinal, "Balanced is the ladder midpoint (A.1).");
        }

        [Test]
        public void Default_DeliversEveryPresetVerbatim_IncludingAffinity()
        {
            // The load-bearing contents-equality check: for every ordinal the default catalogue
            // delivers the exact pre-refactor data — each TeamTactic dial field-by-field, the
            // Players reference, AND the three A.3 affinity scalars seeded from the constants. All
            // exact and host-independent; a mismatch here is precisely the neutrality break the
            // refactor could introduce.
            ITacticPresetCatalogue c = Catalogue;
            Assert.AreEqual(TacticPresetLibrary.Presets.Count, c.Count);
            for (int p = 0; p < c.Count; p++)
            {
                TacticPreset expected = TacticPresetLibrary.Presets[p];
                TacticPreset actual = c.Presets[p];

                Assert.AreEqual(expected.Name, actual.Name, "Name @" + p);
                Assert.AreSame(expected.Players, actual.Players, "Players @" + p);

                AssertTeamEqual(expected.Team, actual.Team, p);

                Assert.AreEqual(expected.BaseFit, actual.BaseFit, "BaseFit @" + p);
                Assert.AreEqual(expected.AggrAffinity, actual.AggrAffinity, "AggrAffinity @" + p);
                Assert.AreEqual(expected.CautAffinity, actual.CautAffinity, "CautAffinity @" + p);

                // …and the affinity is exactly the ordinal-indexed constants (the seed source).
                Assert.AreEqual(TacticalPresetsConstants.BaseFit[p], actual.BaseFit, "BaseFit==const @" + p);
                Assert.AreEqual(TacticalPresetsConstants.AggrAffinity[p], actual.AggrAffinity, "Aggr==const @" + p);
                Assert.AreEqual(TacticalPresetsConstants.CautAffinity[p], actual.CautAffinity, "Caut==const @" + p);
            }
        }

        private static void AssertTeamEqual(in TeamTactic e, in TeamTactic a, int p)
        {
            Assert.AreEqual(e.Mentality, a.Mentality, "Mentality @" + p);
            Assert.AreEqual(e.Formation, a.Formation, "Formation @" + p);
            Assert.AreEqual(e.Tempo, a.Tempo, "Tempo @" + p);
            Assert.AreEqual(e.Width, a.Width, "Width @" + p);
            Assert.AreEqual(e.Passing, a.Passing, "Passing @" + p);
            Assert.AreEqual(e.Pressing, a.Pressing, "Pressing @" + p);
            Assert.AreEqual(e.LineOfEngagement, a.LineOfEngagement, "LineOfEngagement @" + p);
            Assert.AreEqual(e.DefensiveLine, a.DefensiveLine, "DefensiveLine @" + p);
            Assert.AreEqual(e.DefensiveWidth, a.DefensiveWidth, "DefensiveWidth @" + p);
            Assert.AreEqual(e.TransitionWon, a.TransitionWon, "TransitionWon @" + p);
            Assert.AreEqual(e.TransitionLost, a.TransitionLost, "TransitionLost @" + p);
            Assert.AreEqual(e.OffsideTrap, a.OffsideTrap, "OffsideTrap @" + p);
            Assert.AreEqual(e.TriggerPressMask, a.TriggerPressMask, "TriggerPressMask @" + p);
            Assert.AreEqual(e.FocusPlay, a.FocusPlay, "FocusPlay @" + p);
            Assert.AreEqual(e.GkDistribution, a.GkDistribution, "GkDistribution @" + p);
            Assert.AreEqual(e.TimeWasting, a.TimeWasting, "TimeWasting @" + p);
            Assert.AreEqual(e.MarkingOrientation, a.MarkingOrientation, "MarkingOrientation @" + p);
            Assert.AreEqual(e.DismarkIntensity, a.DismarkIntensity, "DismarkIntensity @" + p);
            Assert.AreEqual(e.BuildUpStructure, a.BuildUpStructure, "BuildUpStructure @" + p);
            Assert.AreEqual(e.RotationFreedom, a.RotationFreedom, "RotationFreedom @" + p);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial (WS-1): the host-independent contents-equality         |
// |         |            |        |   neutrality proof for the injectable-catalogue refactor.      |
#endregion
