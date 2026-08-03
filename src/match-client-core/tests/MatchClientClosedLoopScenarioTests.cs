// File:     src/match-client-core/tests/MatchClientClosedLoopScenarioTests.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Interactive Unity client (docs/tracking/interactive-unity-client-design.md §5-P6 / §9),
//           Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.1.4 (sim_<scenario>
//           naming) / §3.3.3 / §3.3.5 / KD-8, Code Standards #20
// Purpose:  Simulation-layer executable tests for the §5-P6 closed-loop scenarios. Each runs one
//           scenario through ScenarioRunner.Run under its canonical manifest seed and asserts the
//           §3.3.3 result is Passed, surfacing the machine-readable diagnostics on failure.

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchClientCore.Tests
{
    [TestFixture]
    internal sealed class MatchClientClosedLoopScenarioTests
    {
        private static void RunAndAssertPassed(string manifestPath, ulong seed)
        {
            var runner = new ScenarioRunner(MatchClientClosedLoopScenarios.BuildIndex());

            ScenarioResult result = runner.Run(manifestPath, seed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status,
                "Scenario '" + manifestPath + "' failed.\n" + result.Diagnostics);
        }

        // Same MatchSetup + same tick-stamped log => digest-identical; and a command-free control run
        // must NOT match, so the log is proven load-bearing rather than merely present (§5-P6 (a)).
        [Test]
        public void sim_crossspec_match_client_command_log_replay()
        {
            RunAndAssertPassed(
                MatchClientClosedLoopScenarios.CommandLogReplayPath,
                MatchClientClosedLoopScenarios.CommandLogReplaySeed);
        }

        // save@N -> restore -> replay the post-N tick-stamped commands == the uninterrupted run
        // (§5-P6 (b)).
        [Test]
        public void sim_crossspec_match_client_save_restore_replay()
        {
            RunAndAssertPassed(
                MatchClientClosedLoopScenarios.SaveRestoreReplayPath,
                MatchClientClosedLoopScenarios.SaveRestoreReplaySeed);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-08-03 | —      | Initial implementation. |
#endregion
