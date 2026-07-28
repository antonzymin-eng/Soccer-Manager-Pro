// File:     src/match-engine/tests/MatchEngineShotOutcomeTests.cs
// Created:  2026-07-27
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
            // Same declaration as the discipline / goalkeeper scenarios: live play emits #5's
            // FM-08 "lost possession before CONTACT" at Error level whenever a restart is awarded
            // against a passer mid-windup (§5.Z.7 item 3 records the log LEVEL as the stale part).
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var runner = new ScenarioRunner(MatchEngineShotOutcomeScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineShotOutcomeScenarios.ShotOutcomesPath,
                MatchEngineShotOutcomeScenarios.ShotOutcomeSeed);

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-07-27 | —      | Initial: runs the shot-outcome acceptance scenario.       |
#endregion
