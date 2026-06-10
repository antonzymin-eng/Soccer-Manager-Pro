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
            int formatVersion = TestingStrategyConstants.SCENARIO_MANIFEST_FORMAT_VERSION,
            string[] fixtureRefs = null,
            int[] owningSpecIds = null)
        {
            return new ScenarioManifest(
                name,
                owningSpecIds: owningSpecIds ?? new[] { 19 },
                seed: 7UL,
                tierClassification: TestTier.TierB,
                fixtureRefs: fixtureRefs ?? Array.Empty<string>(),
                formatVersion: formatVersion);
        }

        private static ScenarioRunner CreateRunner(
            ScenarioManifest manifest, ScenarioBody body, string manifestPath = Path)
        {
            var entry = new ScenarioIndexEntry(
                manifestPath, manifest, new ClosedLoopScenario(manifest, body));
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
                "The manifest path is the index lookup key and must be unique.");
        }

        // AR-1 M-4: path uniqueness is not a proxy for A.1 name uniqueness.
        [Test]
        public void Index_DuplicateScenarioNameUnderDistinctPaths_ThrowsArgumentException()
        {
            ScenarioManifest manifestA = CreateManifest(owningSpecIds: new[] { 19, 2 });
            ScenarioManifest manifestB = CreateManifest();
            var entryA = new ScenarioIndexEntry(
                "tests/scenarios/cross-spec/contract-probe", manifestA,
                new ClosedLoopScenario(manifestA, c => c.Envelope.CheckTrue("p", true, "")));
            var entryB = new ScenarioIndexEntry(
                Path, manifestB,
                new ClosedLoopScenario(manifestB, c => c.Envelope.CheckTrue("q", true, "")));

            Assert.Throws<ArgumentException>(
                () => new ScenarioIndex(new[] { entryA, entryB }),
                "A.1 requires the scenario name to be unique within the manifest, "
                    + "independent of the lookup path.");
        }

        // AR-1 M-1: a ClosedLoopScenario registered under a different manifest instance
        // than the one it executes would pass load-time validation against a manifest
        // the run never uses.
        [Test]
        public void Entry_ClosedLoopScenarioUnderDifferentManifest_ThrowsArgumentException()
        {
            ScenarioManifest registered = CreateManifest();
            ScenarioManifest executed = CreateManifest(name: "other-probe");
            var scenario = new ClosedLoopScenario(executed, c => c.Envelope.CheckTrue("p", true, ""));

            Assert.Throws<ArgumentException>(
                () => new ScenarioIndexEntry(Path, registered, scenario),
                "Load-time validation must run against the manifest the scenario executes (AR-1 M-1).");
        }

        // AR-1 M-4: §3.3.5 layout — the index key terminates in the manifest name.
        [Test]
        public void Run_ManifestPathNotEndingInScenarioName_ThrowsArgumentException()
        {
            ScenarioRunner runner = CreateRunner(
                CreateManifest(name: "other-name"), c => c.Envelope.CheckTrue("p", true, ""));

            Assert.Throws<ArgumentException>(
                () => runner.Run(Path, 1UL),
                "The path a scenario is selected by must agree with the name its "
                    + "diagnostics report (§3.3.5).");
        }

        // AR-1 M-4: A.1 — cross-spec scenarios declare ≥ 2 owning specs.
        [Test]
        public void Run_CrossSpecPathWithSingleOwningSpec_ThrowsArgumentException()
        {
            const string CrossPath = "tests/scenarios/cross-spec/contract-probe";
            ScenarioRunner runner = CreateRunner(
                CreateManifest(), c => c.Envelope.CheckTrue("p", true, ""), CrossPath);

            Assert.Throws<ArgumentException>(() => runner.Run(CrossPath, 1UL));
        }

        // AR-1 M-2: the Stage 0 runner has no fixture loader; silently ignoring
        // fixture_refs is the §3.3.4-forbidden silent acceptance.
        [Test]
        public void Run_NonEmptyFixtureRefs_ThrowsArgumentException()
        {
            ScenarioRunner runner = CreateRunner(
                CreateManifest(fixtureRefs: new[] { "tests/data/fixtures/probe.fixture" }),
                c => c.Envelope.CheckTrue("p", true, ""));

            Assert.Throws<ArgumentException>(
                () => runner.Run(Path, 1UL),
                "Fixture-backed scenarios must be refused until the Stage 0+1 KD-10 loader lands.");
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

        // AR-2 L-1: a NaN range bound is harness misuse and must throw, not record a
        // failing predicate (NaN comparisons slip past the min>max guard).
        [Test]
        public void Envelope_NaNRangeBound_ThrowsArgumentException()
        {
            var envelope = new ScenarioEnvelope();

            Assert.Throws<ArgumentException>(
                () => envelope.CheckInRange("nan-bound", 0.0f, float.NaN, 1.0f));
            Assert.Throws<ArgumentException>(
                () => envelope.CheckInRange("nan-bound", 0.0f, 0.0f, float.NaN));
        }

        // AR-1 M-3: a newline in a predicate detail must not corrupt the line-oriented
        // key=value diagnostics encoding.
        [Test]
        public void Run_FailureDetailContainingNewline_IsFlattenedInDiagnostics()
        {
            ScenarioRunner runner = CreateRunner(CreateManifest(), c =>
                c.Envelope.CheckTrue("newline-detail", false, "line1\nline2"));

            ScenarioResult result = runner.Run(Path, 1UL);

            Assert.AreEqual(ScenarioStatus.Failed, result.Status);
            StringAssert.Contains("line1 line2", result.Diagnostics);
            StringAssert.DoesNotContain("line1\nline2", result.Diagnostics);
        }

        [Test]
        public void Run_BodyThrows_FailsWithExceptionAndStackDiagnostics()
        {
            ScenarioRunner runner = CreateRunner(CreateManifest(), c =>
            {
                c.Envelope.CheckTrue("recorded-before-throw", true, "");
                throw new InvalidOperationException("subsystem blew up");
            });

            ScenarioResult result = runner.Run(Path, 1UL);

            Assert.AreEqual(ScenarioStatus.Failed, result.Status);
            StringAssert.Contains("exception=InvalidOperationException", result.Diagnostics);
            StringAssert.Contains("exception_stack=", result.Diagnostics,
                "A thrown closed-loop body is nearly undiagnosable without the stack (AR-1 M-3).");
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
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-10 | —      | Initial implementation (12 contract tests).                        |
// | 1.1     | 2026-06-10 | —      | AR-1 fix-pass locks (+6 tests, 18 total): M-1 entry/scenario       |
// |         |            |        | manifest coherence; M-2 non-empty fixture_refs refusal; M-3        |
// |         |            |        | newline flattening + exception_stack line; M-4 duplicate-name      |
// |         |            |        | rejection, path↔name coherence, cross-spec ≥2 owning-spec arity.   |
// | 1.2     | 2026-06-10 | —      | AR-2 L-1 lock (+1 test, 19 total): NaN in_range bound throws.      |
#endregion
