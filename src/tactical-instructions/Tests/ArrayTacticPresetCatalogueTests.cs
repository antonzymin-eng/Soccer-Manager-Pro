// File:     src/tactical-instructions/Tests/ArrayTacticPresetCatalogueTests.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Tactical Presets #26 §2.2.2 (FR-TP-002/013), KD-6; Code Standards #20
// Purpose:  Locks the data-backed catalogue's fail-loud guards (null/empty/out-of-range) and its
//           snapshot-copy-at-construction contract.

using System;

using NUnit.Framework;

namespace TacticalDirector.TacticalInstructions.Tests
{
    [TestFixture]
    internal class ArrayTacticPresetCatalogueTests
    {
        private static TacticPreset[] TwoPresets()
        {
            return new[]
            {
                new TacticPreset("A", TeamTactic.Balanced, -0.3f, -0.5f, 0.6f),
                new TacticPreset("B", TeamTactic.Balanced, 0.5f, 0f, 0f),
            };
        }

        [Test]
        public void Guards_FailLoud()
        {
            Assert.Throws<ArgumentNullException>(() => new ArrayTacticPresetCatalogue(null, 0));
            Assert.Throws<ArgumentException>(() => new ArrayTacticPresetCatalogue(new TacticPreset[0], 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new ArrayTacticPresetCatalogue(TwoPresets(), 2));
        }

        [Test]
        public void ExposesPresetsAndOrdinal()
        {
            var c = new ArrayTacticPresetCatalogue(TwoPresets(), 1);
            Assert.AreEqual(2, c.Count);
            Assert.AreEqual(1, (int)c.BalancedOrdinal);
            Assert.AreEqual("A", c.Presets[0].Name);
            Assert.AreEqual("B", c.Presets[1].Name);
        }

        [Test]
        public void SnapshotsPresetsAtConstruction()
        {
            TacticPreset[] source = TwoPresets();
            var c = new ArrayTacticPresetCatalogue(source, 1);
            source[0] = new TacticPreset("MUTATED", TeamTactic.Balanced, 0f, 0f, 0f);
            Assert.AreEqual("A", c.Presets[0].Name,
                "post-construction mutation of the caller's array must not reach the catalogue");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-24 | —      | Initial (WS-1): guard + snapshot locks for the data-backed     |
// |         |            |        |   catalogue.                                                   |
#endregion
