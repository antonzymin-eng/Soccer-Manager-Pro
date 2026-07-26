// File:     src/match-engine/tests/MatchEnginePlayDevelopmentTests.cs
// Created:  2026-07-26
// Modified: 2026-07-26
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z Phase H (acceptance),
//           Testing Strategy & Framework #19 §3.1.4 / §3.3.3, Code Standards #20
// Purpose:  Runs the §5.Z.5 Phase H acceptance scenario through the #19 ScenarioRunner. Simulation-layer
//           (sim_<scenario> naming per #19 §3.1.4); costs ~1.5 minutes of composed match time, which is
//           the point — the failure mode it guards against only becomes visible over minutes.

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEnginePlayDevelopmentTests
    {
        [Test]
        public void sim_match_engine_play_develops()
        {
            // A match that actually plays reaches Pass Mechanics #5's FM-08 possession-recheck cancel (a
            // pass whose passer has a restart awarded against them mid-windup). That is the documented
            // cancel path working as designed, but #5 emits it at Error level, so the run must declare it.
            // See the same note on SeasonLoopScenarioTests.sim_season_multi_fixture.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var runner = new ScenarioRunner(MatchEnginePlayDevelopmentScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEnginePlayDevelopmentScenarios.PlayDevelopsPath,
                MatchEnginePlayDevelopmentScenarios.PlayDevelopsSeed);

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial implementation — runs the Phase H acceptance scenario.      |
#endregion
