// File:     src/project-constants/tests/GameplayConfigFileLoaderTests.cs
// Created:  2026-06-30
// Modified: 2026-06-30
// Author:   —
// Spec:     Code Standards #20 §3.2.3 (FR-CS-019), Testing Strategy #19
// Purpose:  Locks the GameplayConfigFileLoader grammar: section/key round-trip, comments + blanks,
//           empty/null → Empty (behaviour-neutral), and fail-loud on every malformed-config case.

using System;

using NUnit.Framework;

namespace TacticalDirector.ProjectConstants.Tests
{
    /// <summary>FR-CS-019 on-disk [GT] config text → GameplayConfig parser contract.</summary>
    [TestFixture]
    internal sealed class GameplayConfigFileLoaderTests
    {
        [Test]
        public void Null_ParsesToEmpty()
        {
            Assert.AreEqual(0, GameplayConfigFileLoader.Parse(null).Count);
        }

        [Test]
        public void EmptyAndCommentOnly_ParseToEmpty()
        {
            Assert.AreEqual(0, GameplayConfigFileLoader.Parse(string.Empty).Count);
            Assert.AreEqual(0, GameplayConfigFileLoader.Parse("# just a comment\n\n   \n").Count);
        }

        [Test]
        public void SectionedKeys_RoundTrip()
        {
            GameplayConfig cfg = GameplayConfigFileLoader.Parse(
                "[ball-physics]\n" +
                "MaxSubsteps = 8\n" +
                "LiftCoefficient = 0.42\n");

            Assert.AreEqual(8, cfg.GetInt("ball-physics", "MaxSubsteps", 4));
            Assert.AreEqual(0.42f, cfg.GetFloat("ball-physics", "LiftCoefficient", 0.35f), 1e-6f);
        }

        [Test]
        public void CommentsAndBlankLines_AreIgnored()
        {
            GameplayConfig cfg = GameplayConfigFileLoader.Parse(
                "# header comment\n" +
                "\n" +
                "[agent-movement]   # section comment\n" +
                "TopSpeed = 7.5   # inline comment\n");

            Assert.AreEqual(7.5f, cfg.GetFloat("agent-movement", "TopSpeed", 6.0f), 1e-6f);
            Assert.AreEqual(1, cfg.Count);
        }

        [Test]
        public void SameKeyInDifferentSections_BothKept()
        {
            GameplayConfig cfg = GameplayConfigFileLoader.Parse(
                "[ball-physics]\nMax = 8\n[agent-movement]\nMax = 3\n");

            Assert.AreEqual(8, cfg.GetInt("ball-physics", "Max", 0));
            Assert.AreEqual(3, cfg.GetInt("agent-movement", "Max", 0));
        }

        [Test]
        public void ValueContainingEquals_SplitsOnFirstOnly()
        {
            GameplayConfig cfg = GameplayConfigFileLoader.Parse("[paths]\nKey = a=b=c\n");
            Assert.AreEqual("a=b=c", cfg.GetString("paths", "Key", string.Empty));
        }

        [Test]
        public void KeyBeforeSection_Throws()
        {
            Assert.Throws<FormatException>(() => GameplayConfigFileLoader.Parse("MaxSubsteps = 8\n"));
        }

        [Test]
        public void DuplicateKeyInSection_Throws()
        {
            Assert.Throws<FormatException>(
                () => GameplayConfigFileLoader.Parse("[ball-physics]\nMax = 8\nMax = 9\n"));
        }

        [Test]
        public void LineWithoutEquals_Throws()
        {
            Assert.Throws<FormatException>(
                () => GameplayConfigFileLoader.Parse("[ball-physics]\nMaxSubsteps 8\n"));
        }

        [Test]
        public void MalformedSectionHeader_Throws()
        {
            Assert.Throws<FormatException>(() => GameplayConfigFileLoader.Parse("[ball-physics\nMax = 8\n"));
        }

        [Test]
        public void EmptyKey_Throws()
        {
            Assert.Throws<FormatException>(() => GameplayConfigFileLoader.Parse("[ball-physics]\n = 8\n"));
        }

        [Test]
        public void EmptySectionHeader_Throws()
        {
            Assert.Throws<FormatException>(() => GameplayConfigFileLoader.Parse("[]\nMax = 8\n"));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                              |
// | 1.0     | 2026-06-30 | —      | Initial implementation — GameplayConfigFileLoader grammar locks.    |
#endregion
