// File:     src/testing-strategy/Tests/CrossSpecScenarioTests.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.1.4
//           (sim_<scenario> naming) / §3.3.3 / §3.3.5 / KD-8, Code Standards #20
// Purpose:  Simulation-layer executable tests for the cross-spec scenario corpus.
//           Each test runs one scenario through ScenarioRunner.Run under its
//           canonical manifest seed and asserts the §3.3.3 result is Passed,
//           surfacing the machine-readable diagnostics on failure.

using NUnit.Framework;

namespace TacticalDirector.TestingStrategy.Tests
{
    [TestFixture]
    internal sealed class CrossSpecScenarioTests
    {
        private static void RunAndAssertPassed(string manifestPath, ulong seed)
        {
            var runner = new ScenarioRunner(CrossSpecScenarios.BuildIndex());

            ScenarioResult result = runner.Run(manifestPath, seed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status,
                "Scenario '" + manifestPath + "' failed.\n" + result.Diagnostics);
        }

        // Kick → bounce → roll across the #5 → #1 boundary (AR-7 composed surface).
        [Test]
        public void sim_crossspec_lofted_pass_kick_bounce_roll()
        {
            RunAndAssertPassed(
                CrossSpecScenarios.LoftedPassKickBounceRollPath,
                CrossSpecScenarios.LoftedPassKickBounceRollSeed);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-10 | —      | Initial implementation. |
#endregion
