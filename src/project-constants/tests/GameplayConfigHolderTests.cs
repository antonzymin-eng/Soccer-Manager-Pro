// File:     src/project-constants/tests/GameplayConfigHolderTests.cs
// Created:  2026-06-30
// Modified: 2026-06-30
// Author:   —
// Spec:     Code Standards #20 §3.2.3 (FR-CS-019), Testing Strategy #19
// Purpose:  Locks the GameplayConfigHolder boot-sequencing contract: Empty default, Bind takes
//           effect before first read, lock-on-first-read, and Bind-after-lock fails loud.

using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace TacticalDirector.ProjectConstants.Tests
{
    /// <summary>FR-CS-019 GameplayConfigHolder boot-binding / lock-on-first-read contract.</summary>
    [TestFixture]
    internal sealed class GameplayConfigHolderTests
    {
        [SetUp]
        public void SetUp()
        {
            GameplayConfigHolder.ResetForTests();
        }

        [TearDown]
        public void TearDown()
        {
            GameplayConfigHolder.ResetForTests();
        }

        [Test]
        public void Unbound_ResolvesToEmpty()
        {
            Assert.AreSame(GameplayConfig.Empty, GameplayConfigHolder.Config);
        }

        [Test]
        public void Bind_BeforeFirstRead_TakesEffect()
        {
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["ball-physics.MaxSubsteps"] = "12"
            };
            var config = new GameplayConfig(dict);

            GameplayConfigHolder.Bind(config);

            Assert.AreSame(config, GameplayConfigHolder.Config);
            Assert.AreEqual(12, GameplayConfigHolder.Config.GetInt("ball-physics", "MaxSubsteps", 4));
        }

        [Test]
        public void Bind_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => GameplayConfigHolder.Bind(null));
        }

        [Test]
        public void Bind_AfterFirstRead_Throws()
        {
            GameplayConfig unused = GameplayConfigHolder.Config; // locks the binding
            Assert.Throws<InvalidOperationException>(
                () => GameplayConfigHolder.Bind(GameplayConfig.Empty));
        }

        [Test]
        public void ResetForTests_ClearsLockAndBinding()
        {
            GameplayConfig unused = GameplayConfigHolder.Config; // locks
            GameplayConfigHolder.ResetForTests();

            // No throw: the lock was cleared, so Bind is accepted again.
            var config = new GameplayConfig(new Dictionary<string, string>());
            Assert.DoesNotThrow(() => GameplayConfigHolder.Bind(config));
            Assert.AreSame(config, GameplayConfigHolder.Config);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                          |
// | 1.0     | 2026-06-30 | —      | Initial implementation.                        |
#endregion
