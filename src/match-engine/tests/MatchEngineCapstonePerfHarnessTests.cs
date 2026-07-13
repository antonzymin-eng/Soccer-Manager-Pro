// File:     src/match-engine/tests/MatchEngineCapstonePerfHarnessTests.cs
// Created:  2026-07-13
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.3.5 / FR-PO-052,
//           Match Engine design note §5 Phase F, cert-run-runbook.md (Step 2),
//           Testing Strategy & Framework #19, Code Standards #20
// Purpose:  Proves the FR-PO-052 perf harness executes the REAL MatchEngine capstone and yields a
//           real (non-certifying) per-tick p50/p99 — the exact gap over the synthetic 0.000 stub.
//           Shape + real-execution only: there is NO certified budget at Stage 0, so this asserts
//           no budget/threshold verdict (that would fabricate a certification). The certified
//           capture is the same code on the pinned host at runCount = BaselineSampleCount (100),
//           invoked via the Unity batch-mode command (src/CLAUDE.md / cert-run-runbook.md Step 2).

using System;

using NUnit.Framework;

using TacticalDirector.PerformanceOptimization;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Tests for <see cref="MatchEngineCapstonePerfHarness"/> — the real per-tick perf capture over
    /// the kickoff capstone.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineCapstonePerfHarnessTests
    {
        // Run count: env override for the pinned-host certified capture (operator sets 100 =
        // BaselineSampleCount); unset/unparseable ⇒ 2 (CI-fast — 2×600 = 1200 ticks, the same
        // order as the capstone's own two-run body). Env reads are harness/test code, not game
        // logic, so the no-DateTime.Now / no-System.Random determinism rule does not apply.
        private const int DefaultCiRunCount = 2;

        private static int ResolveRunCount()
        {
            string raw = Environment.GetEnvironmentVariable("TD_PERF_RUN_COUNT");
            if (!string.IsNullOrEmpty(raw)
                && int.TryParse(raw, out int parsed)
                && parsed >= 1)
            {
                return parsed;
            }

            return DefaultCiRunCount;
        }

        [Test]
        public void Harness_RunsRealEngine_ProducesFiniteMetrics()
        {
            int runCount = ResolveRunCount();

            BaselineRecord record = MatchEngineCapstonePerfHarness.CaptureBaseline(runCount);

            // Real execution over >=1200 full-engine ticks: a max-side percentile of exactly 0 is
            // impossible, so p99 > 0 strictly. p50 may in principle round to 0 on a coarse timer
            // (BaselineRecord permits a 0-valued percentile), so it is asserted >= 0 only.
            Assert.IsTrue(float.IsFinite(record.P50Ms), "p50 must be finite.");
            Assert.IsTrue(float.IsFinite(record.P99Ms), "p99 must be finite.");
            Assert.Greater(record.P99Ms, 0f, "p99 over real full-engine ticks must be strictly positive.");
            Assert.GreaterOrEqual(record.P50Ms, 0f);
            Assert.GreaterOrEqual(record.P99Ms, record.P50Ms, "p99 must be >= p50.");

            Assert.AreEqual(
                MatchEngineCapstoneScenarios.KickoffMultiSecondPath, record.Manifest.ScenarioManifestId);
            Assert.AreEqual(LoopTag.PhysicsSixtyHz, record.Loop);
            Assert.AreEqual("FR-PO-052", record.ThresholdCited);
            Assert.AreEqual(
                CertifiedPerfBaseline.LinuxNonCertPlatformPin, record.Manifest.PlatformPin,
                "A Linux/CI capture must be stamped non-cert so it can never masquerade as certified.");
            Assert.IsTrue(record.Manifest.IsComplete(), "The capture manifest must be complete.");

            // Surface the measured numbers for the pinned-host operator to transcribe into the
            // .cert.md corpus entry (cert-run-runbook.md Step 3).
            TestContext.WriteLine(
                $"FR-PO-052 capstone per-tick (runCount={runCount}, NON-CERT): " +
                $"p50={record.P50Ms:F4} ms  p99={record.P99Ms:F4} ms");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-13 | —      | Initial implementation. Asserts the FR-PO-052 harness runs the     |
// |         |            |        | real capstone engine and yields finite p50/p99 with p99 > 0,       |
// |         |            |        | stamped non-cert; no budget verdict (none exists at Stage 0). Env  |
// |         |            |        | TD_PERF_RUN_COUNT switches CI-fast (2) vs host capture (100).       |
#endregion
