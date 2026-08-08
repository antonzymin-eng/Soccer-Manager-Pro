// File:     src/match-engine/tests/MatchEngineGkHeadingScenarioTests.cs
// Created:  2026-07-23
// Modified: 2026-07-23
// Author:   —
// Spec:     GK/Heading scenario design supplement (docs/tracking/gk-heading-scenario-design.md),
//           Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.3.3, Heading #10,
//           Goalkeeper #11, Code Standards #20
// Purpose:  Runs the GK/Heading flag-on composition scenario through ScenarioRunner and asserts Passed —
//           the deferred closed-loop scenario for the opt-in GK (#11) / Heading (#10) engine wiring.

using NUnit.Framework;

using TacticalDirector.EventSystem;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Simulation-layer closed-loop test for the opt-in GK/Heading wiring on the composed
    /// <see cref="MatchEngine"/> host.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineGkHeadingScenarioTests
    {
        /// <summary>
        /// Resets the process-static EventBus after every test. Each <c>new MatchEngine(...)</c> resets it
        /// on the way in (Boot → <see cref="EventBus.ResetForNewMatch"/>); clearing it here guarantees this
        /// fixture leaves no subscriber or boot-phase state behind for a later fixture.
        /// </summary>
        [TearDown]
        public void ResetEventBus()
        {
            EventBus.ResetForNewMatch();
        }

        [Test]
        public void sim_crossspec_gk_heading_flag_on_composition()
        {
            var runner = new ScenarioRunner(MatchEngineGkHeadingScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineGkHeadingScenarios.CompositionPath,
                MatchEngineGkHeadingScenarios.CompositionSeed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status,
                "GK/Heading flag-on composition scenario failed.\n" + result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-23 | —      | Initial implementation. Runs the GK/Heading flag-on composition    |
// |         |            |        | scenario through ScenarioRunner → Passed.                          |
#endregion
