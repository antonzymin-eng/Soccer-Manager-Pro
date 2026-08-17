// File:     src/performance-optimization/tests/StopwatchPerfHarnessTests.cs
// Created:  2026-07-13
// Modified: 2026-07-13
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.3.5 / §3.3.2 / §4.3.1,
//           Deterministic Simulation #16 §4.8 (EnvironmentFingerprint),
//           Testing Strategy & Framework #19, Code Standards #20
// Purpose:  Locks the concrete StopwatchPerfHarness: nearest-rank p50/p99 correctness, the
//           first-sample threshold/loop capture, the per-method alloc accumulation, the round-trip
//           into a BaselineRecord, and the fail-closed guards (empty corpus, no open session,
//           double session, corrupt sample).

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.PerformanceOptimization
{
    /// <summary>
    /// Tests for <see cref="StopwatchPerfHarness"/>, the concrete <see cref="IPerfHarness"/>.
    /// </summary>
    [TestFixture]
    public sealed class StopwatchPerfHarnessTests
    {
        private static SessionManifest CompleteManifest()
        {
            return new SessionManifest(
                gitSha: "0000000000000000000000000000000000000000",
                seed: 0x0F1E2D3C4B5A6978UL,
                environmentFingerprint: EnvironmentFingerprint.CreateStage0Dev(),
                platformPin: "linux-dotnet-noncert",
                scenarioManifestId: "tests/scenarios/unit/stopwatch-harness",
                sessionStartUtc: "2026-07-13T00:00:00Z",
                sessionEndUtc: "2026-07-13T00:00:01Z",
                hardwareCounters: new HardwareCounterSnapshot("cpu", 1, "nominal"),
                harnessVersion: "stopwatch-harness-1.0");
        }

        private static BudgetRollupEntry Declared(string subroutine = "Sub", string citation = "FR-PO-052")
        {
            return new BudgetRollupEntry(
                specId: "18",
                subroutineName: subroutine,
                loop: LoopTag.PhysicsSixtyHz,
                budgetMs: 0f,
                allocBudgetBytes: 0u,
                citation: citation);
        }

        // ── Percentile correctness ──────────────────────────────────────────────────────

        [Test]
        public void NearestRank_OnTenSamples_MatchesHandComputation()
        {
            var harness = new StopwatchPerfHarness();
            harness.BeginSession(CompleteManifest());

            // Record 1..10 out of order to also prove Finalize sorts before ranking.
            float[] order = { 5f, 1f, 9f, 3f, 7f, 2f, 10f, 4f, 8f, 6f };
            BudgetRollupEntry declared = Declared();
            foreach (float v in order)
            {
                harness.RecordTickSample(declared, v, 0u);
            }

            BaselineRecord record = harness.FinalizeSession();

            // rank(50) = ceil(0.50*10) = 5 -> sorted[4] = 5; rank(99) = ceil(0.99*10) = 10 -> sorted[9] = 10.
            Assert.AreEqual(5f, record.P50Ms);
            Assert.AreEqual(10f, record.P99Ms);
            Assert.GreaterOrEqual(record.P99Ms, record.P50Ms);
        }

        [Test]
        public void NearestRank_SingleSample_P50EqualsP99()
        {
            var harness = new StopwatchPerfHarness();
            harness.BeginSession(CompleteManifest());
            harness.RecordTickSample(Declared(), 4.2f, 0u);

            BaselineRecord record = harness.FinalizeSession();

            Assert.AreEqual(4.2f, record.P50Ms);
            Assert.AreEqual(4.2f, record.P99Ms);
        }

        // ── Record projection ───────────────────────────────────────────────────────────

        [Test]
        public void FinalizeSession_ProjectsCompleteRecord()
        {
            var harness = new StopwatchPerfHarness();
            SessionManifest manifest = CompleteManifest();
            harness.BeginSession(manifest);
            harness.RecordTickSample(Declared(subroutine: "MatchEngine.RunTick"), 3f, 128u);
            harness.RecordTickSample(Declared(subroutine: "MatchEngine.RunTick"), 5f, 64u);

            BaselineRecord record = harness.FinalizeSession();

            Assert.AreSame(manifest, record.Manifest);
            Assert.AreEqual(LoopTag.PhysicsSixtyHz, record.Loop);
            Assert.AreEqual("FR-PO-052", record.ThresholdCited);
            Assert.AreEqual(BaselinePassFail.Pass, record.PassFail);
            Assert.IsTrue(record.Manifest.IsComplete(), "Round-tripped manifest must be complete.");
            Assert.AreEqual(192ul, record.PerMethodAllocBytes["MatchEngine.RunTick"],
                "Per-method alloc bytes must accumulate across samples.");
        }

        [Test]
        public void Harness_IsReusable_AfterFinalize()
        {
            var harness = new StopwatchPerfHarness();

            harness.BeginSession(CompleteManifest());
            harness.RecordTickSample(Declared(citation: "FR-PO-001"), 2f, 0u);
            BaselineRecord first = harness.FinalizeSession();
            Assert.AreEqual("FR-PO-001", first.ThresholdCited);

            // A second session must start from clean accumulators (no leakage of the first).
            harness.BeginSession(CompleteManifest());
            harness.RecordTickSample(Declared(citation: "FR-PO-052"), 7f, 0u);
            BaselineRecord second = harness.FinalizeSession();
            Assert.AreEqual("FR-PO-052", second.ThresholdCited);
            Assert.AreEqual(7f, second.P50Ms);
        }

        // ── Fail-closed guards ──────────────────────────────────────────────────────────

        [Test]
        public void FinalizeSession_EmptyCorpus_Throws()
        {
            var harness = new StopwatchPerfHarness();
            harness.BeginSession(CompleteManifest());

            Assert.Throws<InvalidOperationException>(() => harness.FinalizeSession(),
                "An empty corpus must not project an implicit-pass baseline.");
        }

        [Test]
        public void RecordOrFinalize_WithoutSession_Throws()
        {
            var harness = new StopwatchPerfHarness();

            Assert.Throws<InvalidOperationException>(() => harness.FinalizeSession());
            Assert.Throws<InvalidOperationException>(() => harness.RecordTickSample(Declared(), 1f, 0u));
        }

        [Test]
        public void BeginSession_Null_Throws()
        {
            var harness = new StopwatchPerfHarness();
            Assert.Throws<ArgumentNullException>(() => harness.BeginSession(null));
        }

        [Test]
        public void BeginSession_IncompleteManifest_Throws()
        {
            var harness = new StopwatchPerfHarness();
            var incomplete = new SessionManifest(
                gitSha: "",                                   // empty ⇒ IsComplete() == false
                seed: 0x0F1E2D3C4B5A6978UL,
                environmentFingerprint: EnvironmentFingerprint.CreateStage0Dev(),
                platformPin: "linux-dotnet-noncert",
                scenarioManifestId: "tests/scenarios/unit/stopwatch-harness",
                sessionStartUtc: "2026-07-13T00:00:00Z",
                sessionEndUtc: "2026-07-13T00:00:01Z",
                hardwareCounters: new HardwareCounterSnapshot("cpu", 1, "nominal"),
                harnessVersion: "stopwatch-harness-1.0");

            Assert.Throws<ArgumentException>(() => harness.BeginSession(incomplete),
                "An incomplete manifest must fail closed (IPerfHarness contract).");
        }

        [Test]
        public void BeginSession_WhileOpen_Throws()
        {
            var harness = new StopwatchPerfHarness();
            harness.BeginSession(CompleteManifest());

            Assert.Throws<InvalidOperationException>(() => harness.BeginSession(CompleteManifest()));
        }

        [Test]
        public void RecordTickSample_CorruptValue_Throws()
        {
            var harness = new StopwatchPerfHarness();
            harness.BeginSession(CompleteManifest());

            Assert.Throws<ArgumentOutOfRangeException>(
                () => harness.RecordTickSample(Declared(), float.NaN, 0u), "NaN sample must fail closed.");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => harness.RecordTickSample(Declared(), -1f, 0u), "Negative sample must fail closed.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-13 | —      | Initial implementation. Nearest-rank p50/p99 correctness (incl.    |
// |         |            |        | out-of-order + single-sample), record projection + alloc           |
// |         |            |        | accumulation, reuse-after-finalize, and the empty-corpus /         |
// |         |            |        | no-session / null / double-session / corrupt-sample guards.        |
#endregion
