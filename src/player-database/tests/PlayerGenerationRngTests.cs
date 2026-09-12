// File:     src/player-database/tests/PlayerGenerationRngTests.cs
// Created:  2026-09-08
// Modified: 2026-09-08
// Author:   —
// Spec:     Squad/Player Data Layer #27 §3; Code Standards #20
// Purpose:  Regression coverage for deterministic generation helper input boundaries.

using System;

using NUnit.Framework;

namespace TacticalDirector.PlayerDatabase.Tests
{
    [TestFixture]
    internal sealed class PlayerGenerationRngTests
    {
        [Test]
        public void DrawBounded_RejectsNullRng() =>
            Assert.Throws<ArgumentNullException>(() => PlayerGenerationRng.DrawBounded(null, 0, 0, 1));

        [TestCase(0)]
        [TestCase(-1)]
        public void DrawBounded_RejectsNonPositiveBoundBeforeDrawing(int bound) =>
            Assert.Throws<ArgumentOutOfRangeException>(() => PlayerGenerationRng.DrawBounded(null, 0, 0, bound));

        [Test]
        public void Clamp_RejectsInvertedRange() =>
            Assert.Throws<ArgumentException>(() => PlayerGenerationRng.Clamp(5, 10, 1));
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                      |
// | 1.0     | 2026-09-08 | —      | Initial helper-boundary regression tests.  |
#endregion
