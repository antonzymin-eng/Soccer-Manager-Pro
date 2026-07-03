// File:     src/pressing-ai/Tests/PressingScenarioTests.cs
// Created:  2026-07-03
// Modified: 2026-07-03
// Author:   —
// Spec:     Pressing AI #13 §3.13,
//           Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.1.4
//           (sim_<scenario> naming) / §3.3.3, Code Standards #20
// Purpose:  Simulation-layer executable tests for the Spec #13 closed-loop scenario
//           corpus. Each runs one scenario through ScenarioRunner.Run under its
//           canonical manifest seed and asserts the §3.3.3 result is Passed. These are
//           the first tests to actually execute PressingAITick.Tick (the 8-step
//           orchestrator).

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.PressingAI.Tests
{
    [TestFixture]
    internal sealed class PressingScenarioTests
    {
        private static void RunAndAssertPassed(string manifestPath, ulong seed)
        {
            var runner = new ScenarioRunner(PressingScenarios.BuildIndex());

            ScenarioResult result = runner.Run(manifestPath, seed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status,
                "Scenario '" + manifestPath + "' failed.\n" + result.Diagnostics);
        }

        // The 8-step orchestrator runs over a realistic press and produces well-formed output.
        [Test]
        public void sim_pressing_press_pipeline_runs_and_assigns()
        {
            RunAndAssertPassed(PressingScenarios.PipelineRunsPath, PressingScenarios.PipelineRunsSeed);
        }

        // Two independent instances produce byte-identical directives/assignments.
        [Test]
        public void sim_pressing_two_instance_determinism()
        {
            RunAndAssertPassed(PressingScenarios.DeterministicPath, PressingScenarios.DeterministicSeed);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-03 | —      | Initial implementation. |
#endregion
