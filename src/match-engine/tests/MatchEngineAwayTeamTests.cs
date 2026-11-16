// File:     src/match-engine/tests/MatchEngineAwayTeamTests.cs
// Created:  2026-06-28
// Modified: 2026-11-10
// Author:   —
// Spec:     Decision Tree #8 audit-report.md (away-team closed-loop follow-up), Tactical Instructions
//           #21 §3.1/§3.2/§4.6, Testing Strategy & Framework #19 §3.1.1 (Simulation layer) / §3.3.3,
//           Code Standards #20
// Purpose:  Runs the away-team tactic-mirror scenario through ScenarioRunner and asserts Passed —
//           the Decision Tree #8 audit's deferred away-team closed-loop check, enabled by the #21
//           runtime-activation single-writer. Also locks the Defensive AI #14 canonical-frame contract
//           (ERR-014-008): a mirrored kickoff yields mirrored #14 mark assignments.

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.DefensiveAI;
using TacticalDirector.DeterministicSim;
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

        /// <summary>
        /// ERR-014-008. The engine hands Defensive AI #14 every position in the defending team's canonical
        /// attack-toward-+X frame, so mirroring the whole match (swap the roles of the two teams) must give
        /// the defending team the same #14 mark assignments whichever team it is. Engine A: the home team
        /// holds the ball and the AWAY team defends. Engine B: the exact 180 degree mirror, the away team
        /// holds the ball and the HOME team defends. Away agent 11 + k in A and home agent k in B must carry
        /// the same mode, the same relative target and the same canonical target position at every tick.
        /// Before the fix #14 decided "own goal" from the team id, so the away team defending (A) ranked
        /// threat and runner direction against x = 105 and diverged from the home team defending (B).
        /// The stock kickoff formation keeps every opponent beyond the 15 m MAN_MARK radius (a first run
        /// produced no non-ZONAL mark at all) and a loose ball puts both teams in phase InPoss, which
        /// forces all-ZONAL, so the outfield is re-placed each tick on a comb astride the halfway line
        /// (home k at x = 44 + 0.6 k, away k at its exact mirror; opponents 10 to 12 m apart at distinct x)
        /// and the ball is given to the central player of the possessing team.
        /// </summary>
        /// <summary>Re-places every outfield agent on the comb (stationary); the layout is exactly centre-symmetric.</summary>
        private static void PlaceMirroredComb(MatchEngine engine)
        {
            int perTeam = MatchEngineConstants.PLAYERS_PER_TEAM;
            for (int k = 1; k < perTeam; k++)
            {
                Vector2 home = new Vector2(44.0f + 0.6f * k, 8.0f + 5.2f * k);
                Vector2 away = new Vector2(
                    MatchEngineConstants.PITCH_LENGTH_M - home.x,
                    MatchEngineConstants.PITCH_WIDTH_M - home.y);

                AgentState h = engine.TestOnly_AgentSnapshot(k);
                h.Position = home;
                h.Velocity = Vector2.zero;
                engine.TestOnly_SetAgent(k, h);

                AgentState a = engine.TestOnly_AgentSnapshot(perTeam + k);
                a.Position = away;
                a.Velocity = Vector2.zero;
                engine.TestOnly_SetAgent(perTeam + k, a);
            }
        }

        [Test]
        public void MirroredKickoff_YieldsMirroredDefensiveAssignments()
        {
            int ticks = 12 * DeterministicSimConstants.AI_PHASE_STRIDE;
            int perTeam = MatchEngineConstants.PLAYERS_PER_TEAM;
            const int Carrier = 5;

            var homeHolds = new MatchEngine(0x0123456789ABCDEFUL);   // A: away team defends.
            var awayHolds = new MatchEngine(0x0123456789ABCDEFUL);   // B: home team defends (mirror).
            int nonZonalPairs = 0;

            for (int tick = 0; tick < ticks; tick++)
            {
                PlaceMirroredComb(homeHolds);
                homeHolds.TestOnly_SetPossession(Carrier);
                homeHolds.RunTick();

                PlaceMirroredComb(awayHolds);
                awayHolds.TestOnly_SetPossession(perTeam + Carrier);
                awayHolds.RunTick();

                for (int k = 0; k < perTeam; k++)
                {
                    MarkAssignment awayDefending = homeHolds.TestOnly_MarkAssignment(perTeam + k);
                    MarkAssignment homeDefending = awayHolds.TestOnly_MarkAssignment(k);

                    Assert.AreEqual(homeDefending.Mode, awayDefending.Mode,
                        $"tick {tick} slot {k}: away-defending #14 mode must mirror home-defending.");
                    Assert.AreEqual(homeDefending.TargetEntityId < 0, awayDefending.TargetEntityId < 0,
                        $"tick {tick} slot {k}: a target exists for one side iff it exists for the other.");
                    if (homeDefending.TargetEntityId >= 0)
                    {
                        Assert.AreEqual(homeDefending.TargetEntityId - perTeam, awayDefending.TargetEntityId,
                            $"tick {tick} slot {k}: away-defending target must be the mirror of home-defending's.");
                        Assert.AreEqual(homeDefending.TargetPosition.x, awayDefending.TargetPosition.x, 0.01f,
                            $"tick {tick} slot {k}: canonical target X.");
                        Assert.AreEqual(homeDefending.TargetPosition.y, awayDefending.TargetPosition.y, 0.01f,
                            $"tick {tick} slot {k}: canonical target Y.");
                    }

                    if (homeDefending.Mode != MarkMode.Zonal)
                    {
                        nonZonalPairs++;
                    }
                }
            }

            Assert.Greater(nonZonalPairs, 0,
                "The scenario must exercise at least one non-ZONAL mark, or the mirror check is vacuous.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-28 | —      | Initial implementation. Runs the away-team tactic-mirror scenario  |
// |         |            |        | through ScenarioRunner → Passed (Decision Tree #8 deferred         |
// |         |            |        | away-team closed-loop follow-up, enabled by #21 runtime activation).|
// | 1.1     | 2026-11-10 | —      | ERR-014-008: MirroredKickoff_YieldsMirroredDefensiveAssignments — a centre-symmetric  |
// |         |            |        | kickoff gives home agent k and away agent 11 + k identical #14 assignments (reads the  |
// |         |            |        | new TestOnly_MarkAssignment seam).                                                     |
#endregion
