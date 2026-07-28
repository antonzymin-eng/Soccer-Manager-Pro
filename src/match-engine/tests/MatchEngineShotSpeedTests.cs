// File:     src/match-engine/tests/MatchEngineShotSpeedTests.cs
// Created:  2026-07-28
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
            // Live play emits #5's FM-08 Error-level log on restart-interrupted windups
            // (§5.Z.7 item 3 — the log LEVEL is the stale part); same declaration as the
            // sibling acceptance scenarios.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var runner = new ScenarioRunner(MatchEngineShotSpeedScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineShotSpeedScenarios.ShotSpeedPath,
                MatchEngineShotSpeedScenarios.ShotSpeedSeed);

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-07-28 | —      | Initial: runs the shot-speed acceptance scenario.         |
#endregion
