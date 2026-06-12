// File:     src/decision-tree/Tests/DecisionContextAssemblerTests.cs
// Created:  2026-06-11
// Author:   —
// Spec:     Decision Tree #8 §2.2.4, §3.1.1, §3.2.1.3, Code Standards #20
// Purpose:  Locks the audit AR-2 H-2 (team-relative BallZone) and M-1
//           (OpponentHasBall perspective derivation) fixes at the assembler seam.
//           Both defects were invisible to home-team-only fixtures: every prior test
//           built home-team contexts, for which the home-perspective zone and the
//           AWAY_TEAM possession literal are coincidentally correct.

using NUnit.Framework;
using UnityEngine;
using TacticalDirector.AgentMovement;
using TacticalDirector.PerceptionSystem;

namespace TacticalDirector.DecisionTree.Tests
{
    [TestFixture]
    internal class DecisionContextAssemblerTests
    {
        // ── AR-2 H-2: team-relative ball zone ─────────────────────────────────

        [Test]
        public void BallZone_AwayAgent_BallNearHomeGoal_IsAttacking()
        {
            // Ball at x = 10: home DEFENSIVE, but for the away team (own goal line at
            // x = 105) it is 95 m from their own goal — their ATTACKING third
            // (§3.2.1.3 "from own goal line"). Pre-fix, the away agent consumed the
            // shared home-perspective zone and scored its shots with the 0.10
            // DEFENSIVE modifier in exactly the positions it shoots from.
            DecisionContext home = Assemble(agentId: 3,  teamId: 0, ballX: 10.0f);
            DecisionContext away = Assemble(agentId: 14, teamId: 1, ballX: 10.0f);

            Assert.AreEqual(FieldZone.DEFENSIVE, home.BallZone, "home perspective: own third");
            Assert.AreEqual(FieldZone.ATTACKING, away.BallZone, "away perspective: final third");
        }

        [Test]
        public void BallZone_MirrorBoundary_NotEnumMirrored()
        {
            // x = 37: home MIDFIELD (35 < 37 ≤ 65). For away, distance from own goal
            // line is 105 − 37 = 68 ≥ 65 ⇒ ATTACKING. Enum-mirroring a home MIDFIELD
            // value would return MIDFIELD — the home cut points {35, 65} mirror to
            // {40, 70}, so the zone must be recomputed per team (PitchGeometry doc).
            DecisionContext home = Assemble(agentId: 3,  teamId: 0, ballX: 37.0f);
            DecisionContext away = Assemble(agentId: 14, teamId: 1, ballX: 37.0f);

            Assert.AreEqual(FieldZone.MIDFIELD,  home.BallZone);
            Assert.AreEqual(FieldZone.ATTACKING, away.BallZone,
                "away zone at x=37 is ATTACKING (68 m from own goal line) — enum mirroring misclassifies the 35–40 m band");
        }

        // ── AR-2 M-1: OpponentHasBall perspective derivation ──────────────────

        [Test]
        public void OpponentHasBall_AwayAgent_HomePossession_True()
        {
            // Home agent 5 possesses. For an away agent that is OPPONENT possession;
            // pre-fix the §3.4.6 urgency gate (PossessedByTeam == AWAY_TEAM) was
            // false here — away agents never received press urgency while defending.
            DecisionContext away = Assemble(agentId: 14, teamId: 1, ballX: 50.0f, possessingAgentId: 5);
            Assert.AreEqual(PossessionState.HOME_TEAM, away.PossessedByTeam);
            Assert.IsTrue(away.OpponentHasBall);
        }

        [Test]
        public void OpponentHasBall_AwayAgent_OwnTeamPossession_False()
        {
            // Away agent 15 possesses; evaluated away agent 14 is off-ball but its
            // OWN team has the ball — no §3.4.6 urgency (pre-fix this case wrongly
            // triggered it).
            DecisionContext away = Assemble(agentId: 14, teamId: 1, ballX: 50.0f, possessingAgentId: 15);
            Assert.AreEqual(PossessionState.AWAY_TEAM, away.PossessedByTeam);
            Assert.IsFalse(away.OpponentHasBall);
        }

        [Test]
        public void OpponentHasBall_Contested_False()
        {
            DecisionContext ctx = Assemble(agentId: 3, teamId: 0, ballX: 50.0f, possessingAgentId: -1);
            Assert.AreEqual(PossessionState.CONTESTED, ctx.PossessedByTeam);
            Assert.IsFalse(ctx.OpponentHasBall, "CONTESTED is not opponent possession (§3.4.6)");
        }

        // ── Helper ────────────────────────────────────────────────────────────

        private static DecisionContext Assemble(
            int agentId, int teamId, float ballX, int possessingAgentId = 5)
        {
            var snap = new FilteredView
            {
                ObserverId            = agentId,
                FrameNumber           = 100,
                BallVisible           = true,
                BallPerceivedPosition = new Vector2(ballX, 34.0f),
                BallStalenessFrames   = 0,
                VisibleTeammates      = new PerceivedAgent[10],
                VisibleTeammatesCount = 0,
                VisibleOpponents      = new PerceivedAgent[11],
                VisibleOpponentsCount = 0,
                BlindSidePerceivedAgents = new PerceivedAgent[3]
            };
            var mc = new MatchContext
            {
                Possession        = possessingAgentId == -1 ? PossessionState.CONTESTED
                                  : possessingAgentId < 11  ? PossessionState.HOME_TEAM
                                                            : PossessionState.AWAY_TEAM,
                PossessingAgentId = possessingAgentId,
                Phase             = MatchPhase.OPEN_PLAY,
                BallPosition      = new Vector2(ballX, 34.0f),
                BallZone          = PitchGeometry.ComputeFieldZone(ballX)   // orchestrator home-perspective value
            };
            var state = new AgentState
            {
                Position        = new Vector2(52.0f, 34.0f),
                FacingDirection = Vector2.right,
                AerobicPool     = 0.8f,
                CurrentState    = AgentMovementState.IDLE
            };

            return DecisionContextAssembler.Assemble(
                snap, mc, TacticalContext.Stage0Default(new Vector2(50.0f, 34.0f)),
                DtAgentAttributes.CreateDefault(teamId), state,
                pressureScalar: 0.0f, matchSeed: 0xABCDUL);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                      |
// | 1.0     | 2026-06-11 | —      | Audit AR-2: H-2 + M-1 locks at the assembler seam.         |
#endregion
