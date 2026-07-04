// File:     src/perception-system/Tests/PerceptionScenarioTests.cs
// Created:  2026-07-03
// Modified: 2026-07-03
// Author:   —
// Spec:     Perception System #7 §5.11.4,
//           Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.1.4
//           (sim_<scenario> naming) / §3.3.3, Code Standards #20
// Purpose:  Simulation-layer executable tests for the Spec #7 closed-loop scenario
//           corpus. Each runs one scenario through ScenarioRunner.Run under its
//           canonical manifest seed and asserts the §3.3.3 result is Passed, surfacing
//           the machine-readable diagnostics on failure. These are the first tests to
//           actually execute PerceptionSystem.OnHeartbeat (the 22-agent orchestrator).

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.PerceptionSystem.Tests
{
    [TestFixture]
    internal sealed class PerceptionScenarioTests
    {
        private static void RunAndAssertPassed(string manifestPath, ulong seed)
        {
            var runner = new ScenarioRunner(PerceptionScenarios.BuildIndex());

            ScenarioResult result = runner.Run(manifestPath, seed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status,
                "Scenario '" + manifestPath + "' failed.\n" + result.Diagnostics);
        }

        // Full 22-agent heartbeat: every snapshot stamped, counts bounded, perception non-trivial.
        [Test]
        public void sim_perception_full_heartbeat_all_snapshots()
        {
            RunAndAssertPassed(PerceptionScenarios.AllSnapshotsPath, PerceptionScenarios.AllSnapshotsSeed);
        }

        // Cold-start awareness builds (non-decreasing, non-zero after warmup) as latency elapses.
        [Test]
        public void sim_perception_awareness_cold_start_builds()
        {
            RunAndAssertPassed(PerceptionScenarios.ColdStartPath, PerceptionScenarios.ColdStartSeed);
        }

        // Two independent instances produce byte-identical perception over the same world/sequence.
        [Test]
        public void sim_perception_two_instance_determinism()
        {
            RunAndAssertPassed(PerceptionScenarios.DeterministicPath, PerceptionScenarios.DeterministicSeed);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-03 | —      | Initial implementation. |
#endregion
