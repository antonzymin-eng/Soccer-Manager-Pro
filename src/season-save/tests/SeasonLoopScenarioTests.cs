// File:     src/season-save/tests/SeasonLoopScenarioTests.cs
// Created:  2026-07-26
// Modified: 2026-07-26
// Author:   —
// Spec:     Season & Competition Loop #30 §5.7; Testing Strategy & Framework #19 §3.1.4 (sim_<scenario>
//           naming), §3.3.3 (ScenarioRunner entry point); Code Standards #20
// Purpose:  Runs the season-multi-fixture capstone through the #19 ScenarioRunner — the Simulation-layer
//           test for the #30 T2 loop.

using NUnit.Framework;

using TacticalDirector.TestingStrategy;

namespace TacticalDirector.SeasonSave.Tests
{
    [TestFixture]
    internal class SeasonLoopScenarioTests
    {
        [Test]
        public void sim_season_multi_fixture()
        {
            // Boots one real MatchEngine match (~2 min): the FR-SN-013b routing proof. The rest of the
            // scenario — two full head-less seasons plus the per-day KD-8 floor — costs milliseconds.
            var runner = new ScenarioRunner(SeasonLoopScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                SeasonLoopScenarios.MultiFixturePath, SeasonLoopScenarios.MultiFixtureSeed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial implementation (#30 §5.7): runs the season-multi-fixture     |
// |         |            |        | capstone through the ScenarioRunner.                                |
#endregion
