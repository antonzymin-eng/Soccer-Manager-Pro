// File:     src/testing-strategy/Tests/ResultValidationTests.cs
// Created:  2026-09-08
// Modified: 2026-09-08
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.2, §3.3.3, §3.8; Code Standards #20
// Purpose:  Regression coverage for result discriminators and complete determinism-gate coverage.

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.TestingStrategy.Tests
{
    [TestFixture]
    internal sealed class ResultValidationTests
    {
        [Test]
        public void ScenarioResult_RejectsUnknownStatus()
        {
            var fingerprint = new EnvironmentFingerprint(1, "scheduler", "tree", "scalar", "hash", "15.1");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new ScenarioResult((ScenarioStatus)255, "bad", 0, fingerprint));
        }

        [Test]
        public void DeterminismTierResult_RejectsUnknownTier()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new DeterminismTierResult((DeterminismTierKind)255, true, 1, 0, "OK"));
        }

        [Test]
        public void GoldenVectorEntry_RejectsUnknownKind()
        {
            Assert.Throws<ArgumentOutOfRangeException>(
                () => new GoldenVectorEntry((GoldenVectorKind)255, "name", "path", "citation"));
        }

        [Test]
        public void GoldenVectorResult_RejectsDefaultEntry()
        {
            Assert.Throws<ArgumentException>(
                () => new GoldenVectorResult(default, true, 1, 0, "OK"));
        }

        [Test]
        public void DeterminismSuiteResult_PartialPassingCoverageFailsClosed()
        {
            var tiers = new[]
            {
                new DeterminismTierResult(DeterminismTierKind.Unit, true, 1, 0, "OK")
            };
            var entry = new GoldenVectorEntry(GoldenVectorKind.HkdfSha256Kat, "name", "path", "citation");
            var vectors = new[] { new GoldenVectorResult(entry, true, 1, 0, "OK") };

            var result = new DeterminismSuiteResult(tiers, vectors);

            Assert.IsFalse(result.AllPassed, "a subset of required gates must never green-light a merge");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                   |
// | 1.0     | 2026-09-08 | —      | Initial five result-validation regression cases.        |
#endregion
