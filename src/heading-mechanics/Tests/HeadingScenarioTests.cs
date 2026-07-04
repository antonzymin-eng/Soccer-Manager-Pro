// File:     src/heading-mechanics/Tests/HeadingScenarioTests.cs
// Created:  2026-07-04
// Modified: 2026-07-04
// Author:   —
// Spec:     Heading Mechanics #10 §4.6,
//           Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.1.4
//           (sim_<scenario> naming) / §3.3.3, Code Standards #20
// Purpose:  Simulation-layer executable tests for the Spec #10 closed-loop scenario corpus. Each
//           runs one scenario through ScenarioRunner.Run under its canonical manifest seed and
//           asserts the §3.3.3 result is Passed. These are the first tests to actually execute
//           HeadingMechanics.CommitIntent + HeadingMechanics.Update (the 60 Hz orchestrator).

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.HeadingMechanics.Tests
{
    [TestFixture]
    internal sealed class HeadingScenarioTests
    {
        private static void RunAndAssertPassed(string manifestPath, ulong seed)
        {
            var runner = new ScenarioRunner(HeadingScenarios.BuildIndex());

            ScenarioResult result = runner.Run(manifestPath, seed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status,
                "Scenario '" + manifestPath + "' failed.\n" + result.Diagnostics);
        }

        // The 60 Hz orchestrator launches a committed header, runs the §3.2 eligibility pipeline, and
        // — the ball being out of reach — reaches no contact: no randomness drawn, no ball mutation.
        [Test]
        public void sim_heading_committed_header_runs_publish_free()
        {
            RunAndAssertPassed(HeadingScenarios.RunsPublishFreePath, HeadingScenarios.RunsPublishFreeSeed);
        }

        // Two independent orchestrators produce byte-identical injected-dependency logs.
        [Test]
        public void sim_heading_two_instance_determinism()
        {
            RunAndAssertPassed(HeadingScenarios.DeterministicPath, HeadingScenarios.DeterministicSeed);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-04 | —      | Initial implementation. |
#endregion
