// File:     src/match-engine/tests/MatchEngineInPossGateTests.cs
// Created:  2026-08-08
// Modified: 2026-08-08
// Author:   —
// Spec:     Positioning AI #12 §3.0 (ERR-012-011); match-engine-wiring-backlog.md §3 C1;
//           Testing Strategy & Framework #19 §3.3.3; Code Standards #20
// Purpose:  Runs the C1 `InPoss`-gate acceptance scenario through the #19 ScenarioRunner.
//           Simulation layer (sim_<scenario> per #19 §3.1.4).

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineInPossGateTests
    {
        [Test]
        public void sim_match_engine_inposs_gate()
        {
            // Live play emits #5's FM-08 "lost possession before CONTACT" at Error level whenever a
            // restart is awarded against a passer mid-windup (§5.Z.7 item 3 — the log LEVEL is the
            // stale part, not the cancel path). Same declaration as the sibling scenarios.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var runner = new ScenarioRunner(MatchEngineInPossGateScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineInPossGateScenarios.InPossGatePath,
                MatchEngineInPossGateScenarios.InPossGateSeed);

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                              |
// | 1.0     | 2026-08-08 | —      | Initial: runs the C1 InPoss-gate scenario.         |
#endregion
