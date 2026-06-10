// File:     src/agent-movement/Tests/AgentMovementScenarioTests.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Agent Movement #2 test-plan.md §3.12 (T-AM-110..115),
//           Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.1.4
//           (sim_<scenario> naming) / §3.3.3, Code Standards #20
// Purpose:  Simulation-layer executable tests for the Spec #2 closed-loop scenario
//           corpus. Each test runs one scenario through ScenarioRunner.Run under its
//           canonical manifest seed and asserts the §3.3.3 result is Passed,
//           surfacing the machine-readable diagnostics on failure. Method names use
//           the #19 §3.1.4 sim_<scenario> convention — these are the project's first
//           Simulation-layer tests.

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.AgentMovement.Tests
{
    [TestFixture]
    internal sealed class AgentMovementScenarioTests
    {
        private static void RunAndAssertPassed(string manifestPath, ulong seed)
        {
            var runner = new ScenarioRunner(AgentMovementScenarios.BuildIndex());

            ScenarioResult result = runner.Run(manifestPath, seed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status,
                "Scenario '" + manifestPath + "' failed.\n" + result.Diagnostics);
        }

        // T-AM-110 (PRIMARY AR-12 H-1 lock): launch from rest.
        [Test]
        public void sim_agentmovement_launch_from_rest()
        {
            RunAndAssertPassed(
                AgentMovementScenarios.LaunchFromRestPath,
                AgentMovementScenarios.LaunchFromRestSeed);
        }

        // T-AM-111 (PRIMARY AR-12 H-2 lock): jog command respects the band ceiling.
        [Test]
        public void sim_agentmovement_jog_band_respect()
        {
            RunAndAssertPassed(
                AgentMovementScenarios.JogBandRespectPath,
                AgentMovementScenarios.JogBandRespectSeed);
        }

        // T-AM-112 (PRIMARY AR-12 H-3 lock): bounded stop, no Zeno tail.
        [Test]
        public void sim_agentmovement_bounded_stop()
        {
            RunAndAssertPassed(
                AgentMovementScenarios.BoundedStopPath,
                AgentMovementScenarios.BoundedStopSeed);
        }

        // T-AM-113 (AR-12 H-2 walking band): WalkTo settles in WALKING, no escalation.
        [Test]
        public void sim_agentmovement_walk_band_stability()
        {
            RunAndAssertPassed(
                AgentMovementScenarios.WalkBandStabilityPath,
                AgentMovementScenarios.WalkBandStabilitySeed);
        }

        // T-AM-114 (AR-13 M-1): exhausted agent's jog command degrades to the walking band.
        [Test]
        public void sim_agentmovement_exhausted_command_degradation()
        {
            RunAndAssertPassed(
                AgentMovementScenarios.ExhaustedDegradationPath,
                AgentMovementScenarios.ExhaustedDegradationSeed);
        }

        // T-AM-115 (AR-13 M-2): DT HOLD shape at own position keeps a resting agent at rest.
        [Test]
        public void sim_agentmovement_hold_at_own_position()
        {
            RunAndAssertPassed(
                AgentMovementScenarios.HoldAtOwnPositionPath,
                AgentMovementScenarios.HoldAtOwnPositionSeed);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-10 | —      | Initial implementation. |
#endregion
