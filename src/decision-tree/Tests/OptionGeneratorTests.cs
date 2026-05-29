// File:     src/decision-tree/Tests/OptionGeneratorTests.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §5 (UT-01 through UT-04), Code Standards #20
// Purpose:  Unit tests for OptionGenerator. Verifies all 7 action type gates,
//           PASS candidate cap, and option set invariants (§3.1.10).

using NUnit.Framework;
using UnityEngine;
using TacticalDirector.AgentMovement;
using TacticalDirector.PerceptionSystem;

namespace TacticalDirector.DecisionTree.Tests
{
    [TestFixture]
    internal class OptionGeneratorTests
    {
        private static readonly ActionOption[] Buffer =
            new ActionOption[DecisionTreeConstants.MaxOptions];

        // ── UT-01: Possession branch always generates HOLD ────────────────────

        [Test]
        public void PossessionBranch_AlwaysGeneratesHold()
        {
            DecisionContext ctx = BuildPossessionContext();
            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);
            bool hasHold = false;
            for (int i = 0; i < count; i++)
                if (Buffer[i].Type == ActionType.HOLD) { hasHold = true; break; }
            Assert.IsTrue(hasHold, "HOLD must always be generated in possession branch");
        }

        // ── UT-02: Off-ball branch always generates MOVE ──────────────────────

        [Test]
        public void OffBallBranch_AlwaysGeneratesMove()
        {
            DecisionContext ctx = BuildOffBallContext();
            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);
            bool hasMove = false;
            for (int i = 0; i < count; i++)
                if (Buffer[i].Type == ActionType.MOVE_TO_POSITION) { hasMove = true; break; }
            Assert.IsTrue(hasMove, "MOVE_TO_POSITION must always be generated in off-ball branch");
        }

        // ── UT-03: No PASS when no visible teammates ──────────────────────────

        [Test]
        public void PassNotGenerated_WhenNoVisibleTeammates()
        {
            DecisionContext ctx = BuildPossessionContext();
            // Ensure zero teammates
            ctx.Snapshot.VisibleTeammatesCount = 0;
            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);
            for (int i = 0; i < count; i++)
                Assert.AreNotEqual(ActionType.PASS, Buffer[i].Type, "PASS must not be generated without visible teammates");
        }

        // ── UT-04: No SHOOT when out of shooting range ────────────────────────

        [Test]
        public void ShootNotGenerated_WhenOutOfRange()
        {
            DecisionContext ctx = BuildPossessionContext();
            ctx.AgentPosition     = new Vector2(0.0f, 34.0f);   // own goal line — far from target
            ctx.OpponentGoalCentre = new Vector2(105.0f, 34.0f);
            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);
            for (int i = 0; i < count; i++)
                Assert.AreNotEqual(ActionType.SHOOT, Buffer[i].Type, "SHOOT must not be generated when out of range");
        }

        // ── UT-05: PRESS gate — no PRESS when no opponents ───────────────────

        [Test]
        public void PressNotGenerated_WhenNoVisibleOpponents()
        {
            DecisionContext ctx = BuildOffBallContext();
            ctx.Snapshot.VisibleOpponentsCount = 0;
            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);
            for (int i = 0; i < count; i++)
                Assert.AreNotEqual(ActionType.PRESS, Buffer[i].Type, "PRESS must not be generated without visible opponents");
        }

        // ── UT-06: Option count never exceeds MaxOptions ──────────────────────

        [Test]
        public void OptionCount_NeverExceedsMaxOptions()
        {
            DecisionContext ctx = BuildPossessionContext();
            ctx.Snapshot.VisibleTeammatesCount = 10;
            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);
            Assert.LessOrEqual(count, DecisionTreeConstants.MaxOptions);
        }

        // ── UT-07: Decisions=1 caps PASS candidates at 2 ─────────────────────

        [Test]
        public void DecisionsCap_LowDecisions_LimitsCandidates()
        {
            DecisionContext ctx = BuildPossessionContext();
            ctx.A_Decisions = 0.0f;  // Decisions=1 → cap=2
            ctx.Snapshot.VisibleTeammatesCount = 5;
            // Fill teammates with valid positions
            for (int i = 0; i < 5; i++)
                ctx.Snapshot.VisibleTeammates[i] = new PerceivedAgent
                {
                    AgentId = 10 + i,
                    PerceivedPosition = new Vector2(50.0f + i * 2.0f, 34.0f),
                    PerceivedVelocity = Vector2.zero,
                    ConfidenceScore = 1.0f
                };

            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);

            int passCount = 0;
            for (int i = 0; i < count; i++)
                if (Buffer[i].Type == ActionType.PASS) passCount++;

            Assert.LessOrEqual(passCount, 2, "Decisions=1 should cap PASS candidates at 2");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static DecisionContext BuildPossessionContext()
        {
            var ctx = BuildBaseContext();
            ctx.AgentHasBall      = true;
            ctx.PossessedByTeam   = PossessionState.HOME_TEAM;
            return ctx;
        }

        private static DecisionContext BuildOffBallContext()
        {
            var ctx = BuildBaseContext();
            ctx.AgentHasBall    = false;
            ctx.StaminaAvailable = true;
            ctx.PossessedByTeam  = PossessionState.AWAY_TEAM;
            ctx.MatchContext.PossessingAgentId = 15;

            // Place an opponent nearby for PRESS tests
            ctx.Snapshot.VisibleOpponentsCount = 1;
            ctx.Snapshot.VisibleOpponents = new PerceivedAgent[1]
            {
                new PerceivedAgent
                {
                    AgentId = 15,
                    PerceivedPosition = new Vector2(50.0f, 34.0f),
                    PerceivedVelocity = Vector2.zero,
                    ConfidenceScore = 1.0f
                }
            };

            return ctx;
        }

        private static DecisionContext BuildBaseContext()
        {
            var snap = new FilteredView
            {
                ObserverId            = 5,
                FrameNumber           = 100,
                ForcedRefreshThisTick = false,
                BallVisible           = true,
                BallPerceivedPosition = new Vector2(52.0f, 34.0f),
                BallStalenessFrames   = 0,
                VisibleTeammates      = new PerceivedAgent[10],
                VisibleTeammatesCount = 0,
                VisibleOpponents      = new PerceivedAgent[11],
                VisibleOpponentsCount = 0,
                BlindSidePerceivedAgents = new PerceivedAgent[3],
                BlindSidePerceivedAgentsCount = 0
            };

            var mc = new MatchContext
            {
                HomeScore          = 0,
                AwayScore          = 0,
                MatchTimeSeconds   = 900.0f,
                Possession         = PossessionState.HOME_TEAM,
                PossessingAgentId  = 5,
                Phase              = MatchPhase.OPEN_PLAY,
                BallPosition       = new Vector2(52.0f, 34.0f),
                BallVelocity       = Vector3.zero,
                BallZone           = FieldZone.MIDFIELD
            };

            var tc = TacticalContext.Stage0Default(new Vector2(50.0f, 34.0f));
            var agentState = new AgentState
            {
                Position       = new Vector2(52.0f, 34.0f),
                Velocity       = Vector2.zero,
                FacingDirection = Vector2.right,
                AerobicPool    = 0.80f,
                CurrentState   = AgentMovementState.IDLE
            };

            return new DecisionContext
            {
                Snapshot           = snap,
                AgentId            = 5,
                CurrentFrame       = 100,
                AgentTeamId        = 0,
                AgentHasBall       = true,
                PossessedByTeam    = PossessionState.HOME_TEAM,
                StaminaAvailable   = true,
                AgentState         = agentState,
                AgentPosition      = new Vector2(52.0f, 34.0f),
                AgentFacingDirection = Vector2.right,
                A_Vision      = 0.5f,
                A_Passing     = 0.5f,
                A_Finishing   = 0.5f,
                A_Dribbling   = 0.5f,
                A_LongShots   = 0.5f,
                A_Composure   = 0.5f,
                A_Decisions   = 0.5f,
                A_Anticipation = 0.5f,
                A_Pace        = 0.5f,
                A_Agility     = 0.5f,
                A_WorkRate    = 0.5f,
                A_Stamina     = 0.5f,
                A_Aggression  = 0.5f,
                A_Positioning = 0.5f,
                A_Crossing    = 0.5f,
                MatchContext      = mc,
                TacticalContext   = tc,
                PressureScalar    = 0.0f,
                MatchSeed         = 0xDEADBEEFCAFEBABEUL,
                OpponentGoalCentre = new Vector2(105.0f, 34.0f),
                OpponentGoalPostL  = new Vector2(105.0f, 30.34f),
                OpponentGoalPostR  = new Vector2(105.0f, 37.66f)
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
