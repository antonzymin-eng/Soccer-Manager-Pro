// File:     src/project-constants/tests/GameplayConfigTests.cs
// Created:  2026-06-30
// Modified: 2026-06-30
// Author:   —
// Spec:     Code Standards #20 §3.2.3 (FR-CS-019), Testing Strategy #19
// Purpose:  Locks the GameplayConfig read contract: absent key → fallback, present key → parsed value,
//           present-but-malformed → FormatException (fail loud), case-insensitive lookup, ctor guards.

using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace TacticalDirector.ProjectConstants.Tests
{
    /// <summary>FR-CS-019 GameplayConfig getter / fallback / fail-loud contract.</summary>
    [TestFixture]
    internal sealed class GameplayConfigTests
    {
        private static GameplayConfig Make(params (string composedKey, string value)[] pairs)
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach ((string composedKey, string value) in pairs)
            {
                dict[composedKey] = value;
            }
            return new GameplayConfig(dict);
        }

        [Test]
        public void AbsentKey_ReturnsFallback()
        {
            GameplayConfig cfg = GameplayConfig.Empty;
            Assert.AreEqual(4, cfg.GetInt("ball-physics", "MaxSubsteps", 4));
            Assert.AreEqual(0.35f, cfg.GetFloat("ball-physics", "LiftCoefficient", 0.35f), 1e-6f);
            Assert.IsTrue(cfg.GetBool("ball-physics", "EnableMagnus", true));
            Assert.AreEqual("grass", cfg.GetString("ball-physics", "Surface", "grass"));
            Assert.IsFalse(cfg.Has("ball-physics", "MaxSubsteps"));
        }

        [Test]
        public void PresentKey_ReturnsParsedValue()
        {
            GameplayConfig cfg = Make(
                ("ball-physics.MaxSubsteps", "8"),
                ("ball-physics.LiftCoefficient", "0.42"),
                ("ball-physics.EnableMagnus", "false"),
                ("ball-physics.Surface", "turf"));

            Assert.AreEqual(8, cfg.GetInt("ball-physics", "MaxSubsteps", 4));
            Assert.AreEqual(0.42f, cfg.GetFloat("ball-physics", "LiftCoefficient", 0.35f), 1e-6f);
            Assert.IsFalse(cfg.GetBool("ball-physics", "EnableMagnus", true));
            Assert.AreEqual("turf", cfg.GetString("ball-physics", "Surface", "grass"));
            Assert.IsTrue(cfg.Has("ball-physics", "MaxSubsteps"));
        }

        [Test]
        public void Lookup_IsCaseInsensitive()
        {
            GameplayConfig cfg = Make(("Ball-Physics.MaxSubsteps", "8"));
            Assert.AreEqual(8, cfg.GetInt("ball-physics", "maxsubsteps", 4));
        }

        [Test]
        public void MalformedFloat_ThrowsFormatException()
        {
            GameplayConfig cfg = Make(("ball-physics.LiftCoefficient", "not-a-number"));
            Assert.Throws<FormatException>(() => cfg.GetFloat("ball-physics", "LiftCoefficient", 0.35f));
        }

        [Test]
        public void MalformedInt_ThrowsFormatException()
        {
            GameplayConfig cfg = Make(("ball-physics.MaxSubsteps", "4.5"));
            Assert.Throws<FormatException>(() => cfg.GetInt("ball-physics", "MaxSubsteps", 4));
        }

        [Test]
        public void MalformedBool_ThrowsFormatException()
        {
            GameplayConfig cfg = Make(("ball-physics.EnableMagnus", "yes"));
            Assert.Throws<FormatException>(() => cfg.GetBool("ball-physics", "EnableMagnus", true));
        }

        [Test]
        public void NullEntries_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new GameplayConfig(null));
        }

        [Test]
        public void EmptySectionOrKey_ThrowsArgumentException()
        {
            GameplayConfig cfg = GameplayConfig.Empty;
            Assert.Throws<ArgumentException>(() => cfg.GetInt("", "MaxSubsteps", 4));
            Assert.Throws<ArgumentException>(() => cfg.GetInt("ball-physics", "", 4));
        }

        [Test]
        public void Empty_HasNoEntries()
        {
            Assert.AreEqual(0, GameplayConfig.Empty.Count);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                              |
// | 1.0     | 2026-06-30 | —      | Initial implementation — GameplayConfig read-contract locks.    |
#endregion
