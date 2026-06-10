// File:     src/ball-physics/tests/BallPhysicsScenarioTests.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Ball Physics #1 §3.1.8 / §3.1.10,
//           Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.1.4
//           (sim_<scenario> naming) / §3.3.3, Code Standards #20
// Purpose:  Simulation-layer executable tests for the Spec #1 closed-loop scenario
//           corpus. Each test runs one scenario through ScenarioRunner.Run under its
//           canonical manifest seed and asserts the §3.3.3 result is Passed,
//           surfacing the machine-readable diagnostics on failure. Method names use
//           the #19 §3.1.4 sim_<scenario> convention.

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.BallPhysics.Tests
{
    [TestFixture]
    internal sealed class BallPhysicsScenarioTests
    {
        private static void RunAndAssertPassed(string manifestPath, ulong seed)
        {
            var runner = new ScenarioRunner(BallPhysicsScenarios.BuildIndex());

            ScenarioResult result = runner.Run(manifestPath, seed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status,
                "Scenario '" + manifestPath + "' failed.\n" + result.Diagnostics);
        }

        // PRIMARY AR-7 H-1 lock (ERR-001-001): dropped ball rebounds and settles.
        [Test]
        public void sim_ballphysics_drop_and_rebound()
        {
            RunAndAssertPassed(
                BallPhysicsScenarios.DropAndReboundPath,
                BallPhysicsScenarios.DropAndReboundSeed);
        }

        // PRIMARY AR-7 H-2 lock: fast descent bounces and grounds out, no hover deadlock.
        [Test]
        public void sim_ballphysics_fast_descent_grounds_out()
        {
            RunAndAssertPassed(
                BallPhysicsScenarios.FastDescentGroundsOutPath,
                BallPhysicsScenarios.FastDescentGroundsOutSeed);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-10 | —      | Initial implementation. |
#endregion
