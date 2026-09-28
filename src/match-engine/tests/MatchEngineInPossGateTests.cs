// File:     src/match-engine/tests/MatchEngineInPossGateTests.cs
// Created:  2026-08-08
// Modified: 2026-09-28 (remove blanket LogAssert.ignoreFailingMessages; unexpected Error logs fail the run again — v1.1)
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
            var runner = new ScenarioRunner(MatchEngineInPossGateScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineInPossGateScenarios.InPossGatePath,
                MatchEngineInPossGateScenarios.InPossGateSeed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                              |
// | 1.0     | 2026-08-08 | —      | Initial: runs the C1 InPoss-gate scenario.         |
// | 1.1     | 2026-09-28 | —      | Removed blanket ignoreFailingMessages (stale FM-08 rationale; FM-08 is Warning since W2); measured run emitted no Error log at all. |
#endregion
