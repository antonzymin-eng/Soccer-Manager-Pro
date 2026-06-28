// File:     src/match-engine/tests/MatchEngineAwayTeamTests.cs
// Created:  2026-06-28
// Modified: 2026-06-28
// Author:   —
// Spec:     Decision Tree #8 audit-report.md (away-team closed-loop follow-up), Tactical Instructions
//           #21 §3.1/§3.2/§4.6, Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.3.3,
//           Code Standards #20
// Purpose:  Runs the away-team tactic-mirror scenario through ScenarioRunner and asserts Passed —
//           the Decision Tree #8 audit's deferred away-team closed-loop check, enabled by the #21
//           runtime-activation single-writer.

using NUnit.Framework;

using TacticalDirector.EventSystem;
using TacticalDirector.TestingStrategy;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Away-team closed-loop test for the fully composed <see cref="MatchEngine"/> host.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineAwayTeamTests
    {
        /// <summary>
        /// Resets the process-static EventBus after every test. Each <c>new MatchEngine(...)</c>
        /// resets it on the way in (Boot → <see cref="EventBus.ResetForNewMatch"/>); clearing it here
        /// guarantees this fixture leaves no subscriber or boot-phase state behind for a later fixture.
        /// </summary>
        [TearDown]
        public void ResetEventBus()
        {
            EventBus.ResetForNewMatch();
        }

        [Test]
        public void sim_crossspec_away_team_tactic_mirror()
        {
            var runner = new ScenarioRunner(MatchEngineAwayTeamScenarios.BuildIndex());

            ScenarioResult result = runner.Run(
                MatchEngineAwayTeamScenarios.TacticMirrorPath,
                MatchEngineAwayTeamScenarios.TacticMirrorSeed);

            Assert.AreEqual(ScenarioStatus.Passed, result.Status,
                "Away-team tactic-mirror scenario failed.\n" + result.Diagnostics);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-28 | —      | Initial implementation. Runs the away-team tactic-mirror scenario  |
// |         |            |        | through ScenarioRunner → Passed (Decision Tree #8 deferred         |
// |         |            |        | away-team closed-loop follow-up, enabled by #21 runtime activation).|
#endregion
