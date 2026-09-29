// File:     src/match-engine/tests/MatchEngineKeeperConversionTests.cs
// Created:  2026-07-28
// Modified: 2026-09-28 (remove blanket LogAssert.ignoreFailingMessages; unexpected Error logs fail the run again — v1.1)
// Modified: 2026-07-28
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z.20;
//           gk-catch-parry-conversion-design.md §5; Testing Strategy & Framework #19 §3.3.3;
//           Code Standards #20
// Purpose:  Runs the gk-catch-parry-conversion acceptance scenario through the #19
//           ScenarioRunner. Simulation layer (sim_<scenario> per #19 §3.1.4).

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineKeeperConversionTests
    {
        [Test]
        public void sim_match_engine_keeper_conversion()
        {
            var runner = new ScenarioRunner(MatchEngineKeeperConversionScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineKeeperConversionScenarios.KeeperConversionPath,
                MatchEngineKeeperConversionScenarios.KeeperConversionSeed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-07-28 | —      | Initial: runs the gk-catch-parry-conversion acceptance    |
// |         |            |        | scenario.                                                 |
// | 1.1     | 2026-09-28 | —      | Removed blanket ignoreFailingMessages (stale FM-08 rationale; FM-08 is Warning since W2); measured run hit only #6 FM-03 (Warning via PR #467). |
#endregion
