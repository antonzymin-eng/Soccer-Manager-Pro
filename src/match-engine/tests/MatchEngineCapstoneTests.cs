// File:     src/match-engine/tests/MatchEngineCapstoneTests.cs
// Created:  2026-06-28
// Modified: 2026-06-28
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5 Phase F,
//           Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.1.4 / §3.3.3,
//           Performance Optimization #18 FR-PO-031 / FR-PO-052 (KD-3 boundary), Code Standards #20
// Purpose:  Phase F capstone tests. (1) Runs the kickoff scenario through ScenarioRunner and
//           asserts Passed. (2) A direct two-run same-seed digest-chain equality test (mirrors the
//           Phase E precedent; also re-locks EventBus.ResetForNewMatch across two in-process
//           matches). (3) Activates the FR-PO-052 per-tick perf gate: a real per-tick measurement
//           of the kickoff loop flows through PerfGateRunner against a generous Stage-0 anchor
//           baseline. The Linux dotnet gate is NON-certifying (certification-platform.md v1.2):
//           this proves the perf-gate WIRING; the authoritative perf budget stays on the pinned
//           Windows/Unity tuple.

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.PerformanceOptimization;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Phase F capstone tests for the fully composed <see cref="MatchEngine"/> host.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineCapstoneTests
    {
        private const int  DeterminismTicks = 120;        // 2 s — enough to exercise multiple AI strides
        private const int  PerfTicks        = 600;        // 10 s, matching the capstone scenario
        private const float PerfAnchorPerTickMs = 25.0f;  // generous Stage-0 anchor (NON-certifying gate)

        /// <summary>
        /// Resets the process-static EventBus after every test. Each <c>new MatchEngine(...)</c> already
        /// resets it on the way in (Boot → <see cref="EventBus.ResetForNewMatch"/>), but the final engine
        /// constructed in a test leaves the bus in a drained state; clearing it here guarantees this
        /// fixture leaves no subscriber or boot-phase state behind for a later fixture (the static-bus
        /// fixture-order hazard the project has hit before).
        /// </summary>
        [TearDown]
        public void ResetEventBus()
        {
            EventBus.ResetForNewMatch();
        }

        // ── (1) Closed-loop scenario through the #19 runner ───────────────────────────────

        [Test]
        public void sim_crossspec_match_engine_kickoff_multi_second()
        {
            var runner = new ScenarioRunner(MatchEngineCapstoneScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineCapstoneScenarios.KickoffMultiSecondPath,
                MatchEngineCapstoneScenarios.KickoffMultiSecondSeed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status,
                "Capstone scenario failed.\n" + result.Diagnostics);
        }

        // ── (2) Direct two-run determinism digest-chain equality ──────────────────────────

        [Test]
        public void TwoSameSeedRuns_ProduceIdenticalDigestChains()
        {
            List<byte[]> chainA = RunChain(MatchEngineCapstoneScenarios.KickoffMultiSecondSeed, DeterminismTicks);
            List<byte[]> chainB = RunChain(MatchEngineCapstoneScenarios.KickoffMultiSecondSeed, DeterminismTicks);

            Assert.AreEqual(DeterminismTicks, chainA.Count);
            Assert.AreEqual(DeterminismTicks, chainB.Count);
            for (int i = 0; i < DeterminismTicks; i++)
            {
                CollectionAssert.AreEqual(chainA[i], chainB[i],
                    "Snapshot digest chain diverged at tick " + (i + 1)
                        + " — the composed host is non-deterministic across two same-seed runs.");
            }
        }

        private static List<byte[]> RunChain(ulong seed, int ticks)
        {
            var engine = new MatchEngine(seed);
            var chain = new List<byte[]>(ticks);
            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
                chain.Add(engine.CurrentSnapshotDigest);
            }
            return chain;
        }

        // ── (3) FR-PO-052 per-tick perf gate activation ───────────────────────────────────

        [Test]
        public void PerfGate_KickoffLoop_PassesAgainstStage0Anchor()
        {
            // Real measurement: time PerfTicks of the composed host and derive a per-tick p50 proxy.
            var engine = new MatchEngine(MatchEngineCapstoneScenarios.KickoffMultiSecondSeed);
            var sw = Stopwatch.StartNew();
            for (int i = 0; i < PerfTicks; i++)
            {
                engine.RunTick();
            }
            sw.Stop();
            float measuredPerTickMs = (float)sw.Elapsed.TotalMilliseconds / PerfTicks;

            // Build a current record from the measurement and a baseline at the generous Stage-0
            // anchor; both carry identical FR-PO-031 metadata (scenario / seed / platform / loop) so
            // PerfGateRunner's mismatch guard accepts the pair.
            BaselineRecord baseline = MakeRecord(PerfAnchorPerTickMs, BaselinePassFail.Pass);
            BaselineRecord current  = MakeRecord(measuredPerTickMs, BaselinePassFail.Advisory);

            // milestoneMs = NaN skips the absolute-drift check (no Stage milestone on the Linux gate);
            // the FR-PO-031 per-PR +5% check runs against the generous anchor.
            PerfGateReport report = PerfGateRunner.Run(
                specId: 16,
                loopTag: PerformanceOptimizationConstants.LOOP_TAG_PHYSICS_60HZ,
                baseline: baseline,
                current: current,
                milestoneMs: float.NaN);

            Assert.AreEqual(
                MatchEngineCapstoneScenarios.KickoffMultiSecondPath, report.ScenarioManifestId,
                "PerfGateRunner did not carry the kickoff scenario id through to the report.");
            Assert.IsTrue(report.AllPassed,
                "Per-tick perf gate failed against the Stage-0 anchor: measured "
                    + measuredPerTickMs.ToString("F4", CultureInfo.InvariantCulture)
                    + " ms/tick vs anchor " + PerfAnchorPerTickMs.ToString("F4", CultureInfo.InvariantCulture)
                    + " ms/tick (NON-certifying Linux gate).");
        }

        private static BaselineRecord MakeRecord(float perTickMs, BaselinePassFail passFail)
        {
            var manifest = new SessionManifest(
                gitSha: "stage0-dev",
                seed: MatchEngineCapstoneScenarios.KickoffMultiSecondSeed,
                environmentFingerprint: EnvironmentFingerprint.CreateStage0Dev(),
                platformPin: "linux-dotnet-noncert",
                scenarioManifestId: MatchEngineCapstoneScenarios.KickoffMultiSecondPath,
                sessionStartUtc: "2026-06-28T00:00:00Z",
                sessionEndUtc: "2026-06-28T00:00:10Z",
                hardwareCounters: new HardwareCounterSnapshot("stage0-dev-cpu", 1, "nominal"),
                harnessVersion: "phase-f-capstone-1.0");

            return new BaselineRecord(
                manifest: manifest,
                loop: LoopTag.PhysicsSixtyHz,
                p50Ms: perTickMs,
                p99Ms: perTickMs,
                perMethodAllocBytes: new Dictionary<string, ulong>(),
                passFailAtCapture: passFail,
                thresholdCited: "FR-PO-052");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-28 | —      | Initial implementation (Phase F capstone). (1) Runs the kickoff    |
// |         |            |        | scenario through ScenarioRunner → Passed. (2) Two-run same-seed    |
// |         |            |        | digest-chain equality (re-locks ResetForNewMatch). (3) FR-PO-052   |
// |         |            |        | per-tick perf-gate activation against a generous Stage-0 anchor    |
// |         |            |        | (NON-certifying Linux gate; proves wiring, not budget).            |
#endregion
