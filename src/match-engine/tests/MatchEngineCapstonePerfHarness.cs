// File:     src/match-engine/tests/MatchEngineCapstonePerfHarness.cs
// Created:  2026-07-13
// Modified: 2026-07-13
// Author:   —
// Spec:     Performance Optimization Strategy #18 §3.3.5 / §3.4.4 / FR-PO-052,
//           Match Engine design note (docs/tracking/match-engine-design.md) §5 Phase F,
//           certification-platform.md / cert-run-runbook.md (Step 2), Code Standards #20
// Purpose:  The FR-PO-052 per-tick perf capture — boots a REAL MatchEngine (the Phase-F capstone
//           construction) and times each of its RunTick calls with a Stopwatch, feeding the samples
//           into the concrete StopwatchPerfHarness to project a per-tick p50/p99 BaselineRecord.
//           This is the real harness that replaces the synthetic tools/perf-harness/run.sh stub
//           (which runs no src/ code and records p50=0.000/p99=0.000 — cert-run-runbook.md P1).
//
//           NON-CERTIFYING on the Linux gate: the produced BaselineRecord is stamped with
//           CertifiedPerfBaseline.LinuxNonCertPlatformPin so a Linux number can NEVER masquerade as
//           certified. The certified capture runs this same code on the pinned host (Windows 11 /
//           Unity 6000.4.9f1 / DX11 / Mono) via the Unity batch-mode command documented in
//           src/CLAUDE.md "BUILD AND TEST COMMANDS" and cert-run-runbook.md Step 2, at
//           runCount = BaselineSampleCount (100).

using System;
using System.Diagnostics;

using TacticalDirector.DeterministicSim;
using TacticalDirector.PerformanceOptimization;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Real per-tick perf capture for the Match Engine kickoff capstone. Constructs the engine the
    /// same way <see cref="MatchEngineCapstoneScenarios"/> does and times its 60 Hz tick, projecting
    /// a per-tick p50/p99 <see cref="BaselineRecord"/> via <see cref="StopwatchPerfHarness"/>.
    /// </summary>
    internal static class MatchEngineCapstonePerfHarness
    {
        // Placeholder session-manifest fields for a NON-certifying capture. A Linux number is not
        // authoritative regardless of these values (the platform pin marks it non-cert); the pinned
        // host supplies the real git SHA / hardware counters / timestamps when it certifies.
        private const string NonCertGitSha = "0000000000000000000000000000000000000000";
        private const string NonCertHarnessVersion = "match-engine-capstone-perf-1.0";

        /// <summary>
        /// Runs the capstone engine <paramref name="runCount"/> times (each = one full
        /// <see cref="MatchEngineCapstoneScenarios.KickoffMultiSecondTicks"/>-tick match) and returns
        /// the per-tick p50/p99 <see cref="BaselineRecord"/> over all timed ticks.
        /// </summary>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="runCount"/> &lt; 1.</exception>
        internal static BaselineRecord CaptureBaseline(int runCount)
        {
            if (runCount < 1)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(runCount), runCount, "runCount must be at least 1.");
            }

            var harness = new StopwatchPerfHarness();
            harness.BeginSession(BuildNonCertManifest());

            // budgetMs: 0f = "no certified budget declared at capture" (finite, ctor-valid, never
            // surfaces in the record). Capture pass/fail is always Pass; regression gating against a
            // real budget is RegressionGate's later job.
            var declared = new BudgetRollupEntry(
                specId: "16",
                subroutineName: "MatchEngine.RunTick",
                loop: LoopTag.PhysicsSixtyHz,
                budgetMs: 0f,
                allocBudgetBytes: 0u,
                citation: "FR-PO-052");

            var sw = new Stopwatch();

            for (int run = 0; run < runCount; run++)
            {
                var engine = new MatchEngine(MatchEngineCapstoneScenarios.KickoffMultiSecondSeed);

                for (int t = 1; t <= MatchEngineCapstoneScenarios.KickoffMultiSecondTicks; t++)
                {
                    // Time ONLY RunTick; RecordTickSample (and its List.Add) runs outside the timed
                    // span so harness bookkeeping never pollutes the measurement. allocBytes = 0 —
                    // the hot-path alloc budget is a [FIXED] 0 bytes/tick; per-tick alloc
                    // measurement is deferred (Unity/Mono GC-counter behaviour is unverified here).
                    sw.Restart();
                    engine.RunTick();
                    sw.Stop();

                    harness.RecordTickSample(declared, (float)sw.Elapsed.TotalMilliseconds, 0u);
                }
            }

            return harness.FinalizeSession();
        }

        private static SessionManifest BuildNonCertManifest()
        {
            return new SessionManifest(
                gitSha: NonCertGitSha,
                seed: MatchEngineCapstoneScenarios.KickoffMultiSecondSeed,
                environmentFingerprint: EnvironmentFingerprint.CreateStage0Dev(),
                platformPin: CertifiedPerfBaseline.LinuxNonCertPlatformPin,
                scenarioManifestId: MatchEngineCapstoneScenarios.KickoffMultiSecondPath,
                sessionStartUtc: "2026-07-13T00:00:00Z",
                sessionEndUtc: "2026-07-13T00:00:01Z",
                hardwareCounters: new HardwareCounterSnapshot("linux-noncert", 1, "noncert"),
                harnessVersion: NonCertHarnessVersion);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-13 | —      | Initial implementation. Real FR-PO-052 per-tick capture: boots the |
// |         |            |        | capstone MatchEngine, Stopwatch-times each RunTick, projects a      |
// |         |            |        | non-cert p50/p99 BaselineRecord via StopwatchPerfHarness. Replaces  |
// |         |            |        | the synthetic run.sh stub (P1). Certified capture = same code on    |
// |         |            |        | the pinned host at runCount = BaselineSampleCount.                  |
#endregion
