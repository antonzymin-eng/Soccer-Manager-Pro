// File:     src/first-touch/Tests/FirstTouchScenarioTests.cs
// Created:  2026-06-10
// Modified: 2026-06-10
// Author:   —
// Spec:     First Touch Mechanics #4 §3.3 / §3.4,
//           Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.1.4
//           (sim_<scenario> naming) / §3.3.3, Code Standards #20
// Purpose:  Simulation-layer executable tests for the Spec #4 closed-loop scenario
//           corpus. Each test runs one scenario through ScenarioRunner.Run under its
//           canonical manifest seed and asserts the §3.3.3 result is Passed,
//           surfacing the machine-readable diagnostics on failure.

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.FirstTouch.Tests
{
    [TestFixture]
    internal sealed class FirstTouchScenarioTests
    {
        private static void RunAndAssertPassed(string manifestPath, ulong seed)
        {
            var runner = new ScenarioRunner(FirstTouchScenarios.BuildIndex());

            ScenarioResult result = runner.Run(manifestPath, seed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status,
                "Scenario '" + manifestPath + "' failed.\n" + result.Diagnostics);
        }

        // PRIMARY AR-7 H-1 lock (ERR-004-003): heavy touch runs on along the travel
        // path with displacement and velocity coherent.
        [Test]
        public void sim_firsttouch_heavy_touch_runs_on()
        {
            RunAndAssertPassed(
                FirstTouchScenarios.HeavyTouchRunsOnPath,
                FirstTouchScenarios.HeavyTouchRunsOnSeed);
        }

        // PRIMARY AR-7 M-1 lock (ERR-004-004) + §3.4.5 redirect + Frame N+1 chain.
        [Test]
        public void sim_firsttouch_interception_chain_anchors_at_displaced_ball()
        {
            RunAndAssertPassed(
                FirstTouchScenarios.InterceptionChainPath,
                FirstTouchScenarios.InterceptionChainSeed);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-06-10 | —      | Initial implementation. |
#endregion
