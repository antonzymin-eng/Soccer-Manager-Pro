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
        public void BallZone_IsMeasuredFromEachTeamsOwnGoalLine()
        {
            // The zone bounds are equal thirds (L/3, 2L/3), so the pair is self-mirroring and each
            // team's bands are simply measured from its OWN goal line. x = 20 is 20 m from home's line
            // (defensive third) and 85 m from away's (attacking third).
            //
            // This replaces an earlier test that discriminated via the 35–40 m band, which existed only
            // because AttackingZoneMinX was a hardcoded 65 against a true-third 35 — with equal thirds
            // that band is gone. The contract it was protecting is unchanged and still asserted here:
            // the zone must be recomputed per team rather than the shared home-perspective
            // MatchContext.BallZone being consumed for both (audit AR-2 H-2, which inverted every zone
            // modifier for away agents).
            DecisionContext home = Assemble(agentId: 3,  teamId: 0, ballX: 20.0f);
            DecisionContext away = Assemble(agentId: 14, teamId: 1, ballX: 20.0f);

            Assert.AreEqual(FieldZone.DEFENSIVE, home.BallZone, "20 m from home's own goal line");
            Assert.AreEqual(FieldZone.ATTACKING, away.BallZone,
                "85 m from away's own goal line — consuming the raw home-perspective value would say "
                + "DEFENSIVE here, which is the AR-2 H-2 defect");
        }

        [Test]
        public void BallZone_EqualThirds_AreSymmetricUnderMirroring()
        {
            // The property the derived bounds buy: because {L/3, 2L/3} maps to itself under
            // x -> L - x, a team's own-goal-relative band is orientation-independent. Sampling across
            // the pitch, home at x and away at L - x must agree — if a future edit reintroduces
            // unequal thirds this fails, and the orientation-dependence is back.
            foreach (float x in new[] { 5.0f, 20.0f, 34.0f, 36.0f, 52.5f, 69.0f, 71.0f, 85.0f, 100.0f })
            {
                FieldZone home = PitchGeometry.ComputeFieldZone(x, teamId: 0);
                FieldZone away = PitchGeometry.ComputeFieldZone(
                    PitchGeometry.PitchLengthM - x, teamId: 1);

                Assert.AreEqual(home, away,
                    "zone bands must not depend on attacking direction (x = " + x + ")");
            }
        }

        [Test]
        public void ZoneBounds_AreEqualThirdsOfThePitch()
        {
            // Locks the tag against the value: "[DERIVED] split pitch into thirds" was previously false
            // of AttackingZoneMinX (a hardcoded 65 gave a 40 m attacking third and a 30 m middle third).
            float third = PitchGeometry.PitchLengthM / 3.0f;

            Assert.AreEqual(third, PitchGeometry.DefensiveZoneMaxX, 1e-4f);
            Assert.AreEqual(2.0f * third, PitchGeometry.AttackingZoneMinX, 1e-4f);
            Assert.AreEqual(
                PitchGeometry.DefensiveZoneMaxX,
                PitchGeometry.PitchLengthM - PitchGeometry.AttackingZoneMinX, 1e-4f,
                "equal thirds ⇒ the boundary pair is self-mirroring");
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
// | 1.1     | 2026-07-26 | —      | Zone thirds equalised. BallZone_MirrorBoundary_NotEnumMirrored |
// |         |            |        |   discriminated via the 35–40 m band, which existed only       |
// |         |            |        |   because AttackingZoneMinX was a hardcoded 65 against a       |
// |         |            |        |   true-third 35; with equal thirds that band is gone. Replaced  |
// |         |            |        |   by BallZone_IsMeasuredFromEachTeamsOwnGoalLine (same AR-2 H-2 |
// |         |            |        |   contract, x = 20) plus two new locks: symmetry under          |
// |         |            |        |   mirroring, and the bounds actually being equal thirds.        |
#endregion
