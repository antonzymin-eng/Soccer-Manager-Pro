// File:     src/match-engine/tests/MatchEnginePlayDevelopmentTests.cs
// Created:  2026-07-26
// Modified: 2026-09-28 (remove blanket LogAssert.ignoreFailingMessages; unexpected Error logs fail the run again — v1.1)
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
            var runner = new ScenarioRunner(MatchEnginePlayDevelopmentScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEnginePlayDevelopmentScenarios.PlayDevelopsPath,
                MatchEnginePlayDevelopmentScenarios.PlayDevelopsSeed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial implementation — runs the Phase H acceptance scenario.      |
// | 1.1     | 2026-09-28 | —      | Removed blanket ignoreFailingMessages (stale FM-08 rationale; FM-08 is Warning since W2); measured run emitted no Error log at all. |
#endregion
