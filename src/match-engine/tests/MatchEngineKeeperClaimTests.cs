// File:     src/match-engine/tests/MatchEngineKeeperClaimTests.cs
// Created:  2026-08-03
// Modified: 2026-08-03
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z.23;
//           gk-conversion-at-contact-design.md §5; Testing Strategy & Framework #19 §3.3.3;
//           Code Standards #20
// Purpose:  Runs the conversion-at-contact acceptance scenario through the #19 ScenarioRunner.
//           Simulation layer (sim_<scenario> per #19 §3.1.4).

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineKeeperClaimTests
    {
        [Test]
        public void sim_match_engine_keeper_claim()
        {
            // Live play emits #5's FM-08 "lost possession before CONTACT" at Error level whenever a
            // restart is awarded against a passer mid-windup (§5.Z.7 item 3 — the log LEVEL is the
            // stale part, not the cancel path). Same declaration as the sibling scenarios.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var runner = new ScenarioRunner(MatchEngineKeeperClaimScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineKeeperClaimScenarios.KeeperClaimPath,
                MatchEngineKeeperClaimScenarios.KeeperClaimSeed);

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-08-03 | —      | Initial: runs the keeper-claim acceptance scenario.       |
#endregion
