// File:     src/match-engine/tests/MatchEngineDisciplineTests.cs
// Created:  2026-07-26
// Modified: 2026-09-28 (remove blanket LogAssert.ignoreFailingMessages; unexpected Error logs fail the run again — v1.1)
// Modified: 2026-07-26
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z.9;
//           foul-discipline-balance-design.md §5; Testing Strategy & Framework #19 §3.3.3; Code Standards #20
// Purpose:  Runs the §5.Z.9 discipline acceptance scenario through the #19 ScenarioRunner. Simulation
//           layer (sim_<scenario> per #19 §3.1.4).

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineDisciplineTests
    {
        [Test]
        public void sim_match_engine_discipline_plausible()
        {
            var runner = new ScenarioRunner(MatchEngineDisciplineScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineDisciplineScenarios.DisciplinePlausiblePath,
                MatchEngineDisciplineScenarios.DisciplineSeed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                              |
// | 1.0     | 2026-07-26 | —      | Initial: runs the discipline acceptance scenario. |
// | 1.1     | 2026-09-28 | —      | Removed blanket ignoreFailingMessages (stale FM-08 rationale; FM-08 is Warning since W2); measured run emitted no Error log at all. |
#endregion
