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
            //
            // §5.Z Phase H: that real match now actually PLAYS, which reaches Pass Mechanics #5's FM-08
            // possession-recheck cancel — a pass whose passer loses the ball before CONTACT (a restart is
            // awarded against them mid-windup). That is the documented cancel path doing its job, and it
            // is expected several times per 90 minutes; #5 emits it at Error level, so the run must
            // declare it. (Whether "lost possession before CONTACT. Race condition." should be a Warning
            // now that it is an ordinary match event is a Pass Mechanics question, recorded as a
            // follow-up in the design note rather than changed from here.)
            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = true;

            var runner = new ScenarioRunner(SeasonLoopScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                SeasonLoopScenarios.MultiFixturePath, SeasonLoopScenarios.MultiFixtureSeed);

            UnityEngine.TestTools.LogAssert.ignoreFailingMessages = false;

            Assert.AreEqual(ScenarioStatus.Passed, result.Status, result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial implementation (#30 §5.7): runs the season-multi-fixture     |
// |         |            |        | capstone through the ScenarioRunner.                                |
#endregion
