// File:     src/testing-strategy/Tests/CrossSpecMechanicsScenarioTests.cs
// Created:  2026-07-04
// Modified: 2026-07-04
// Author:   —
// Spec:     Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.1.4
//           (sim_<scenario> naming) / §3.3.3 / §3.3.5 / KD-8, Code Standards #20
// Purpose:  Simulation-layer executable tests for the cross-spec Mechanics scenario
//           corpus. Each test runs one scenario through ScenarioRunner.Run under its
//           canonical manifest seed and asserts the §3.3.3 result is Passed, surfacing
//           the machine-readable diagnostics on failure. These are the first tests to
//           execute the composed Positioning AI (#12) → Pressing AI (#13) handoff.

using NUnit.Framework;

namespace TacticalDirector.TestingStrategy.Tests
{
    [TestFixture]
    internal sealed class CrossSpecMechanicsScenarioTests
    {
        private static void RunAndAssertPassed(string manifestPath, ulong seed)
        {
            var runner = new ScenarioRunner(CrossSpecMechanicsScenarios.BuildIndex());

            ScenarioResult result = runner.Run(manifestPath, seed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status,
                "Scenario '" + manifestPath + "' failed.\n" + result.Diagnostics);
        }

        // The real #12 orchestrator drives the real #13 orchestrator: positioning's live
        // slot/line/phase output feeds pressing each tick, and both halves stay well-formed.
        [Test]
        public void sim_crossspec_positioning_feeds_pressing()
        {
            RunAndAssertPassed(
                CrossSpecMechanicsScenarios.PositioningFeedsPressingPath,
                CrossSpecMechanicsScenarios.PositioningFeedsPressingSeed);
        }

        // Two independent composed chains produce a byte-identical composite digest.
        [Test]
        public void sim_crossspec_positioning_pressing_two_instance_determinism()
        {
            RunAndAssertPassed(
                CrossSpecMechanicsScenarios.DeterministicPath,
                CrossSpecMechanicsScenarios.DeterministicSeed);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-04 | —      | Initial implementation. |
#endregion
