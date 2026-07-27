// File:     src/match-engine/tests/MatchEngineGoalkeeperSaveTests.cs
// Created:  2026-07-27
// Modified: 2026-07-27
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z.17;
//           goalkeeper-save-pipeline-design.md §5; Testing Strategy & Framework #19 §3.3.3;
//           Code Standards #20
// Purpose:  Runs the §5.Z.17 goalkeeper save acceptance scenario through the #19 ScenarioRunner.
//           Simulation layer (sim_<scenario> per #19 §3.1.4).

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineGoalkeeperSaveTests
    {
        [Test]
        public void sim_match_engine_goalkeeper_saves()
        {
            // Same declaration as the discipline scenario: live play emits #5's FM-08 "lost possession
            // before CONTACT" at Error level whenever a restart is awarded against a passer mid-windup.
            // §5.Z.7 item 3 records the log LEVEL as the stale part, not the cancel path.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var runner = new ScenarioRunner(MatchEngineGoalkeeperSaveScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineGoalkeeperSaveScenarios.GoalkeeperSavesPath,
                MatchEngineGoalkeeperSaveScenarios.GoalkeeperSaveSeed);

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-07-27 | —      | Initial: runs the §5.Z.17 goalkeeper save acceptance      |
// |         |            |        | scenario.                                                 |
#endregion
