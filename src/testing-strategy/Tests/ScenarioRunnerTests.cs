// File:     src/testing-strategy/Tests/ScenarioRunnerTests.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.3.2 / §3.3.3 / §3.3.6 / FR-TS-023..030 /
//           FR-TS-070 / KD-7, Code Standards #20
// Purpose:  Contract tests for the Stage 0 ScenarioRunner: index refusal (FR-TS-028),
//           load-time validation (§3.3.2 / FR-TS-070), implicit-pass rejection
//           (FR-TS-030), envelope semantics (NaN, failure diagnostics), seed plumbing
//           (KD-7 / FR-TS-025), and result-shape guarantees (§3.3.3).

using System;

using NUnit.Framework;

namespace TacticalDirector.TestingStrategy.Tests
{
    [TestFixture]
    internal sealed class ScenarioRunnerTests
    {
        private const string Path = "tests/scenarios/testing-strategy/contract-probe";

        private static ScenarioManifest CreateManifest(
            string name = "contract-probe",
            int formatVersion = TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION)
        {
            return new ScenarioManifest(
                name,
                owningSpecIds: new[] { 19 },
                seed: 7UL,
                tierClassification: TestTier.TierB,
                fixtureRefs: Array.Empty<string>(),
                formatVersion: formatVersion);
        }

        private static ScenarioRunner CreateRunner(ScenarioManifest manifest, ScenarioBody body)
        {
            var entry = new ScenarioIndexEntry(Path, manifest, new ClosedLoopScenario(manifest, body));
            return new ScenarioRunner(new ScenarioIndex(new[] { entry }));
        }

        // ── Index refusal (§3.3.6 / FR-TS-028) ──

        [Test]
        public void Run_ManifestPathMissingFromIndex_ThrowsArgumentException()
        {
            ScenarioRunner runner = CreateRunner(CreateManifest(), c => c.Envelope.CheckTrue("p", true, ""));

            Assert.Throws<ArgumentException>(
                () => runner.Run("tests/scenarios/testing-strategy/not-registered", 1UL),
                "The runner must refuse scenarios missing from the index (FR-TS-028).");
        }

        [Test]
        public void Run_EmptyManifestPath_ThrowsArgumentException()
        {
            ScenarioRunner runner = CreateRunner(CreateManifest(), c => c.Envelope.CheckTrue("p", true, ""));

            Assert.Throws<ArgumentException>(() => runner.Run(string.Empty, 1UL));
        }

        // ── Load-time validation (§3.3.2 / §3.3.4 / FR-TS-070) ──

        [Test]
        public void Run_UnsupportedFormatVersion_ThrowsArgumentException()
        {
            ScenarioManifest manifest = CreateManifest(
                formatVersion: TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION + 1);
            ScenarioRunner runner = CreateRunner(manifest, c => c.Envelope.CheckTrue("p", true, ""));

            Assert.Throws<ArgumentException>(
                () => runner.Run(Path, 1UL),
                "Unknown format_version values must be rejected at load time (FR-TS-070).");
        }

        [Test]
        public void Run_NonKebabCaseName_ThrowsArgumentException()
        {
            ScenarioRunner runner = CreateRunner(
                CreateManifest(name: "Contract_Probe"), c => c.Envelope.CheckTrue("p", true, ""));

            Assert.Throws<ArgumentException>(
                () => runner.Run(Path, 1UL),
                "A.1 requires kebab-case scenario names; violation is a load-time error (§3.3.4).");
        }

        [Test]
        public void Index_DuplicateManifestPath_ThrowsArgumentException()
        {
            ScenarioManifest manifest = CreateManifest();
            var entryA = new ScenarioIndexEntry(
                Path, manifest, new ClosedLoopScenario(manifest, c => c.Envelope.CheckTrue("p", true, "")));
            var entryB = new ScenarioIndexEntry(
                Path, manifest, new ClosedLoopScenario(manifest, c => c.Envelope.CheckTrue("q", true, "")));

            Assert.Throws<ArgumentException>(
                () => new ScenarioIndex(new[] { entryA, entryB }),
                "A.1 requires names unique within the manifest.");
        }

        // ── Envelope semantics (FR-TS-030 + failure diagnostics) ──

        [Test]
        public void Run_BodyRecordsNoPredicates_FailsAsImplicitPass()
        {
            ScenarioRunner runner = CreateRunner(CreateManifest(), c => { /* no predicates */ });

            ScenarioResult result = runner.Run(Path, 1UL);

            Assert.AreEqual(ScenarioStatus.Failed, result.Status,
                "A body that records zero predicates must fail — implicit pass is forbidden (FR-TS-030).");
            StringAssert.Contains("implicit_pass_forbidden=FR-TS-030", result.Diagnostics);
        }

        [Test]
        public void Run_FailingPredicate_FailsWithPredicateIdInDiagnostics()
        {
            ScenarioRunner runner = CreateRunner(CreateManifest(), c =>
            {
                c.Envelope.CheckTrue("passing-predicate", true, "");
                c.Envelope.CheckInRange("speed-band", 9.9f, 0.0f, 8.0f);
            });

            ScenarioResult result = runner.Run(Path, 1UL);

            Assert.AreEqual(ScenarioStatus.Failed, result.Status);
            StringAssert.Contains("failed predicate=speed-band", result.Diagnostics);
            StringAssert.Contains("predicates_total=2", result.Diagnostics);
            StringAssert.Contains("predicates_failed=1", result.Diagnostics);
        }

        [Test]
        public void Run_NaNValueAgainstInRangePredicate_Fails()
        {
            ScenarioRunner runner = CreateRunner(CreateManifest(), c =>
                c.Envelope.CheckInRange("nan-guard", float.NaN, 0.0f, 1.0f));

            ScenarioResult result = runner.Run(Path, 1UL);

            Assert.AreEqual(ScenarioStatus.Failed, result.Status,
                "NaN must fail an in_range predicate, never silently pass (§3.4.4 boundary saturation).");
        }

        [Test]
        public void Run_BodyThrows_FailsWithExceptionDiagnostic()
        {
            ScenarioRunner runner = CreateRunner(CreateManifest(), c =>
            {
                c.Envelope.CheckTrue("recorded-before-throw", true, "");
                throw new InvalidOperationException("subsystem blew up");
            });

            ScenarioResult result = runner.Run(Path, 1UL);

            Assert.AreEqual(ScenarioStatus.Failed, result.Status);
            StringAssert.Contains("exception=InvalidOperationException", result.Diagnostics);
        }

        // ── Passing path + result shape (§3.3.3) ──

        [Test]
        public void Run_AllPredicatesPass_ReturnsPassedWithContractFields()
        {
            ScenarioRunner runner = CreateRunner(CreateManifest(), c =>
            {
                c.Envelope.CheckEquals("state-ordinal", actual: 2, expected: 2);
                c.Envelope.CheckInRange("speed", 4.2f, 0.0f, 8.0f);
            });

            ScenarioResult result = runner.Run(Path, 1UL);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
            Assert.That(result.DurationMs, Is.GreaterThanOrEqualTo(0));
            Assert.IsNotNull(result.Fingerprint, "ScenarioResult must carry the #16 §4.8 fingerprint (§3.3.3).");
        }

        // ── Seed plumbing (KD-7 / FR-TS-025) ──

        [Test]
        public void Run_SeedIsPlumbedVerbatimIntoContextAndDiagnostics()
        {
            const ulong RunSeed = 0xDEADBEEFCAFE1234UL;
            ulong observedSeed = 0;
            bool rngAvailable = false;
            ScenarioRunner runner = CreateRunner(CreateManifest(), c =>
            {
                observedSeed = c.RunSeed;
                rngAvailable = c.Rng != null;
                c.Envelope.CheckTrue("probe", true, "");
            });

            ScenarioResult result = runner.Run(Path, RunSeed);

            Assert.AreEqual(RunSeed, observedSeed,
                "The caller-supplied seed must reach the body verbatim (KD-7).");
            Assert.IsTrue(rngAvailable,
                "The RNG service must be constructed (seeded) before the body runs (KD-7).");
            StringAssert.Contains("run_seed=" + RunSeed, result.Diagnostics);
            StringAssert.Contains("manifest_seed=7", result.Diagnostics,
                "The canonical manifest seed is recorded verbatim alongside the run seed (FR-TS-025).");
            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }

        // ── Hermeticity (FR-TS-023) ──

        [Test]
        public void Run_InvokedTwice_EachInvocationGetsAFreshEnvelope()
        {
            int invocation = 0;
            ScenarioRunner runner = CreateRunner(CreateManifest(), c =>
            {
                invocation++;
                Assert.AreEqual(0, c.Envelope.PredicateCount,
                    "Each invocation must start with a fresh envelope (FR-TS-023).");
                c.Envelope.CheckTrue("probe", true, "");
            });

            Assert.AreEqual(ScenarioStatus.Passed, runner.Run(Path, 1UL).Status);
            Assert.AreEqual(ScenarioStatus.Passed, runner.Run(Path, 2UL).Status);
            Assert.AreEqual(2, invocation);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-10 | —      | Initial implementation. |
#endregion
