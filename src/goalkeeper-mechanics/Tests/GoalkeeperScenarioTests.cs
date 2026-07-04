// File:     src/goalkeeper-mechanics/Tests/GoalkeeperScenarioTests.cs
// Created:  2026-07-03
// Modified: 2026-07-03
// Author:   —
// Spec:     Goalkeeper Mechanics #11 §4.6,
//           Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.1.4
//           (sim_<scenario> naming) / §3.3.3, Code Standards #20
// Purpose:  Simulation-layer executable tests for the Spec #11 closed-loop scenario
//           corpus. Each runs one scenario through ScenarioRunner.Run under its
//           canonical manifest seed and asserts the §3.3.3 result is Passed. These are
//           the first tests to actually execute GoalkeeperMechanics.TacticalTick (10 Hz)
//           and GoalkeeperMechanics.Update (60 Hz).

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.GoalkeeperMechanics.Tests
{
    [TestFixture]
    internal sealed class GoalkeeperScenarioTests
    {
        private static void RunAndAssertPassed(string manifestPath, ulong seed)
        {
            var runner = new ScenarioRunner(GoalkeeperScenarios.BuildIndex());

            ScenarioResult result = runner.Run(manifestPath, seed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status,
                "Scenario '" + manifestPath + "' failed.\n" + result.Diagnostics);
        }

        // The 10 Hz + 60 Hz orchestrator carries a committed SaveIntent through a realistic
        // Anticipate → Diving → Airborne dive launch and produces well-formed output (exactly one
        // dive-timing-jitter draw from the correct draw site under the goalkeeper domain tag; a
        // missed save mutates no ball state).
        [Test]
        public void sim_goalkeeper_save_launch_executes_dive()
        {
            RunAndAssertPassed(GoalkeeperScenarios.SaveLaunchPath, GoalkeeperScenarios.SaveLaunchSeed);
        }

        // Two independent orchestrators produce byte-identical injected-dependency logs.
        [Test]
        public void sim_goalkeeper_two_instance_determinism()
        {
            RunAndAssertPassed(GoalkeeperScenarios.DeterministicPath, GoalkeeperScenarios.DeterministicSeed);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-03 | —      | Initial implementation. |
#endregion
