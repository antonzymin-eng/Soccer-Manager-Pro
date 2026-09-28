// File:     src/match-engine/tests/MatchEngineShotSpeedTests.cs
// Created:  2026-07-28
// Modified: 2026-09-28 (remove blanket LogAssert.ignoreFailingMessages; unexpected Error logs fail the run again — v1.1)
// Modified: 2026-07-28
// Author:   —
// Spec:     Shot speed & physical woodwork design §5; Match Engine design note §5.Z;
//           Testing Strategy & Framework #19 §3.3.3; Code Standards #20
// Purpose:  Runs the shot-speed / woodwork acceptance scenario through the #19 ScenarioRunner.
//           Simulation layer (sim_<scenario> per #19 §3.1.4).

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineShotSpeedTests
    {
        [Test]
        public void sim_match_engine_shot_speed()
        {
            var runner = new ScenarioRunner(MatchEngineShotSpeedScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineShotSpeedScenarios.ShotSpeedPath,
                MatchEngineShotSpeedScenarios.ShotSpeedSeed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-07-28 | —      | Initial: runs the shot-speed acceptance scenario.         |
// | 1.1     | 2026-09-28 | —      | Removed blanket ignoreFailingMessages (stale FM-08 rationale; FM-08 is Warning since W2); measured run hit only #6 FM-03 (Warning via PR #467). |
#endregion
