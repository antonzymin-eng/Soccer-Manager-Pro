// File:     src/attacking-ai/Tests/AttackingScenarioTests.cs
// Created:  2026-07-03
// Modified: 2026-07-03
// Author:   —
// Spec:     Attacking AI #15 §3.13,
//           Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.1.4
//           (sim_<scenario> naming) / §3.3.3, Code Standards #20
// Purpose:  Simulation-layer executable tests for the Spec #15 closed-loop scenario
//           corpus. Each runs one scenario through ScenarioRunner.Run under its
//           canonical manifest seed and asserts the §3.3.3 result is Passed. These are
//           the first tests to actually execute AttackingAITick.Tick (the 10-step
//           orchestrator).

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.AttackingAI.Tests
{
    [TestFixture]
    internal sealed class AttackingScenarioTests
    {
        private static void RunAndAssertPassed(string manifestPath, ulong seed)
        {
            var runner = new ScenarioRunner(AttackingScenarios.BuildIndex());

            ScenarioResult result = runner.Run(manifestPath, seed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status,
                "Scenario '" + manifestPath + "' failed.\n" + result.Diagnostics);
        }

        // The 10-step orchestrator runs over a realistic IN_POSSESSION attack and produces
        // well-formed output (defined roles, coherent run-params, runner cap held).
        [Test]
        public void sim_attacking_attack_pipeline_runs_and_assigns()
        {
            RunAndAssertPassed(AttackingScenarios.PipelineRunsPath, AttackingScenarios.PipelineRunsSeed);
        }

        // Two independent instances produce byte-identical directives/intents.
        [Test]
        public void sim_attacking_two_instance_determinism()
        {
            RunAndAssertPassed(AttackingScenarios.DeterministicPath, AttackingScenarios.DeterministicSeed);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-03 | —      | Initial implementation. |
#endregion
