// File:     src/match-engine/tests/MatchEngineShotOutcomeTests.cs
// Created:  2026-07-27
// Modified: 2026-09-28 (remove blanket LogAssert.ignoreFailingMessages; unexpected Error logs fail the run again — v1.1)
// Modified: 2026-07-27
// Author:   —
// Spec:     Shot-outcome distribution design §5; Match Engine design note §5.Z.18;
//           Testing Strategy & Framework #19 §3.3.3; Code Standards #20
// Purpose:  Runs the shot-outcome acceptance scenario through the #19 ScenarioRunner.
//           Simulation layer (sim_<scenario> per #19 §3.1.4).

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineShotOutcomeTests
    {
        [Test]
        public void sim_match_engine_shot_outcomes()
        {
            var runner = new ScenarioRunner(MatchEngineShotOutcomeScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineShotOutcomeScenarios.ShotOutcomesPath,
                MatchEngineShotOutcomeScenarios.ShotOutcomeSeed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-07-27 | —      | Initial: runs the shot-outcome acceptance scenario.       |
// | 1.1     | 2026-09-28 | —      | Removed blanket ignoreFailingMessages (stale FM-08 rationale; FM-08 is Warning since W2); measured run emitted no Error log at all. |
#endregion
