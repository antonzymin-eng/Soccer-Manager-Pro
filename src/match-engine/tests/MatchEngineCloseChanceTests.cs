// File:     src/match-engine/tests/MatchEngineCloseChanceTests.cs
// Created:  2026-08-04
// Modified: 2026-08-04
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z.24;
//           close-chance-creation-design.md §5; Testing Strategy & Framework #19 §3.3.3;
//           Code Standards #20
// Purpose:  Runs the close-chance-creation acceptance scenario through the #19 ScenarioRunner.
//           Simulation layer (sim_<scenario> per #19 §3.1.4).

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineCloseChanceTests
    {
        [Test]
        public void sim_match_engine_close_chance()
        {
            // Live play emits #5's FM-08 "lost possession before CONTACT" at Error level whenever a
            // restart is awarded against a passer mid-windup (§5.Z.7 item 3 — the log LEVEL is the
            // stale part, not the cancel path). Same declaration as the sibling scenarios.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var runner = new ScenarioRunner(MatchEngineCloseChanceScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineCloseChanceScenarios.CloseChancePath,
                MatchEngineCloseChanceScenarios.CloseChanceSeed);

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-08-04 | —      | Initial: runs the close-chance-creation scenario.         |
#endregion
