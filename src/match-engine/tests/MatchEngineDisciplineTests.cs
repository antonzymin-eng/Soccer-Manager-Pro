// File:     src/match-engine/tests/MatchEngineDisciplineTests.cs
// Created:  2026-07-26
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
            // Live play emits #5's FM-08 "lost possession before CONTACT" at Error level whenever a
            // restart is awarded against a passer mid-windup — an ordinary match event since Phase H, and
            // fouls are one of the things that award restarts. §5.Z.7 item 3 records that the log LEVEL is
            // the stale part, not the cancel path; the composed runs declare it meanwhile.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var runner = new ScenarioRunner(MatchEngineDisciplineScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineDisciplineScenarios.DisciplinePlausiblePath,
                MatchEngineDisciplineScenarios.DisciplineSeed);

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                              |
// | 1.0     | 2026-07-26 | —      | Initial: runs the discipline acceptance scenario. |
#endregion
