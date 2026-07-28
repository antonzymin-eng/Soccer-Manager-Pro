// File:     src/match-engine/tests/MatchEngineKeeperContactTests.cs
// Created:  2026-07-28
// Modified: 2026-07-28
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z.22;
//           gk-contact-rate-design.md §5; Testing Strategy & Framework #19 §3.3.3;
//           Code Standards #20
// Purpose:  Runs the gk-contact-rate acceptance scenario through the #19 ScenarioRunner.
//           Simulation layer (sim_<scenario> per #19 §3.1.4).

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineKeeperContactTests
    {
        [Test]
        public void sim_match_engine_keeper_contact()
        {
            // Live play emits #5's FM-08 "lost possession before CONTACT" at Error level whenever a
            // restart is awarded against a passer mid-windup (§5.Z.7 item 3 — the log LEVEL is the
            // stale part, not the cancel path). Same declaration as the sibling scenarios.
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var runner = new ScenarioRunner(MatchEngineKeeperContactScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineKeeperContactScenarios.KeeperContactPath,
                MatchEngineKeeperContactScenarios.KeeperContactSeed);

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                     |
// | 1.0     | 2026-07-28 | —      | Initial: runs the gk-contact-rate acceptance scenario.    |
#endregion
