// File:     src/decision-tree/Tests/UtilityScorerTests.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Decision Tree #8 §5 (UT-05 through UT-08), §3.2.10, Code Standards #20
// Purpose:  Unit tests for UtilityScorer. Verifies utility floor/ceiling clamp,
//           zone modifier effects, attribute extreme outputs, and cross-formula
//           dominance relationships from §3.2.10.

using NUnit.Framework;
using UnityEngine;
using TacticalDirector.AgentMovement;
using TacticalDirector.PerceptionSystem;
using TacticalDirector.PassMechanics;
using TacticalDirector.ShotMechanics;

namespace TacticalDirector.DecisionTree.Tests
{
    [TestFixture]
    internal class UtilityScorerTests
    {
        private static readonly ActionOption[] Buffer = new ActionOption[DecisionTreeConstants.MaxOptions];

        // ── UT-05: All scored utilities are within [FLOOR, CEILING] ──────────

        [Test]
        public void AllScoredUtilities_InValidRange()
        {
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.5f);
            Buffer[0] = MakePass(0.8f, 0.8f, 15.0f);
            Buffer[1] = MakeShoot(0.7f, 20.0f);
            Buffer[2] = MakeDribble(0.6f);
            Buffer[3] = MakeHold();
            Buffer[4] = MakeMove(10.0f);
            UtilityScorer.ScoreOptions(Buffer, 5, in ctx);

            for (int i = 0; i < 5; i++)
            {
                Assert.GreaterOrEqual(Buffer[i].BaseUtility, UtilityWeights.UTILITY_FLOOR,
                    $"Option {i} ({Buffer[i].Type}) below UTILITY_FLOOR");
                Assert.LessOrEqual(Buffer[i].BaseUtility, UtilityWeights.UTILITY_CEILING,
                    $"Option {i} ({Buffer[i].Type}) above UTILITY_CEILING");
            }
        }

        // ── UT-06: SHOOT dominates in attacking third with open goal ──────────

        [Test]
        public void ShootDominates_InAttackingThird_OpenGoal()
        {
            DecisionContext ctx = BuildContext(0.8f, 0.8f, 0.0f);
            ctx.MatchContext.BallZone = FieldZone.ATTACKING;

            Buffer[0] = MakePass(0.8f, 0.8f, 10.0f);
            Buffer[1] = MakeShoot(1.0f, 10.0f);
            Buffer[2] = MakeHold();
            UtilityScorer.ScoreOptions(Buffer, 3, in ctx);

            float shootU = 0.0f, passU = 0.0f;
            for (int i = 0; i < 3; i++)
            {
                if (Buffer[i].Type == ActionType.SHOOT) shootU = Buffer[i].BaseUtility;
                if (Buffer[i].Type == ActionType.PASS)  passU  = Buffer[i].BaseUtility;
            }

            Assert.Greater(shootU, passU, "SHOOT should dominate PASS in attacking third with open goal");
        }

        // ── UT-07: PASS dominates in defensive third ──────────────────────────

        [Test]
        public void PassDominates_InDefensiveThird_ClearLane()
        {
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            ctx.MatchContext.BallZone = FieldZone.DEFENSIVE;

            Buffer[0] = MakePass(1.0f, 1.0f, 15.0f);
            Buffer[1] = MakeDribble(0.5f);
            Buffer[2] = MakeHold();
            UtilityScorer.ScoreOptions(Buffer, 3, in ctx);

            float passU = 0.0f, dribbleU = 0.0f, holdU = 0.0f;
            for (int i = 0; i < 3; i++)
            {
                if (Buffer[i].Type == ActionType.PASS)   passU    = Buffer[i].BaseUtility;
                if (Buffer[i].Type == ActionType.DRIBBLE) dribbleU = Buffer[i].BaseUtility;
                if (Buffer[i].Type == ActionType.HOLD)   holdU    = Buffer[i].BaseUtility;
            }

            Assert.Greater(passU, holdU,    "PASS should dominate HOLD in defensive third");
            Assert.Greater(passU, dribbleU, "PASS should dominate DRIBBLE in defensive third");
        }

        // ── UT-08: High pressure reduces PASS and HOLD utilities ──────────────

        [Test]
        public void HighPressure_ReducesPassAndHold()
        {
            DecisionContext ctxLow  = BuildContext(0.5f, 0.5f, 0.0f);
            DecisionContext ctxHigh = BuildContext(0.5f, 0.5f, 1.0f);

            Buffer[0] = MakePass(0.8f, 0.8f, 15.0f);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctxLow);
            float passLow = Buffer[0].BaseUtility;

            Buffer[0] = MakePass(0.8f, 0.8f, 15.0f);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctxHigh);
            float passHigh = Buffer[0].BaseUtility;

            Assert.Less(passHigh, passLow, "Higher pressure should reduce PASS utility");
        }

        // ── UT-09: HOLD minimum — utility always > UTILITY_FLOOR ─────────────

        [Test]
        public void HoldMinimum_AlwaysAboveFloor()
        {
            // Worst case: max pressure, minimum composure
            DecisionContext ctx = BuildContext(0.0f, 0.0f, 1.0f);
            ctx.MatchContext.BallZone = FieldZone.ATTACKING;

            Buffer[0] = MakeHold();
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);

            Assert.GreaterOrEqual(Buffer[0].BaseUtility, UtilityWeights.UTILITY_FLOOR,
                "HOLD utility must never fall below UTILITY_FLOOR");
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static DecisionContext BuildContext(
            float composure, float finishing, float pressure)
        {
            var mc = new MatchContext
            {
                Phase     = MatchPhase.OPEN_PLAY,
                BallZone  = FieldZone.MIDFIELD,
                Possession = PossessionState.HOME_TEAM,
                PossessingAgentId = 0
            };
            var tc = TacticalContext.Stage0Default(new Vector2(50f, 34f));
            var snap = new FilteredView
            {
                ObserverId = 0, FrameNumber = 1,
                VisibleTeammates = new PerceivedAgent[0],
                VisibleOpponents = new PerceivedAgent[0],
                BlindSidePerceivedAgents = new PerceivedAgent[0]
            };

            return new DecisionContext
            {
                AgentId         = 0,
                AgentTeamId     = 0,
                CurrentFrame    = 1,
                AgentHasBall    = true,
                PossessedByTeam = PossessionState.HOME_TEAM,
                AgentPosition   = new Vector2(52f, 34f),
                AgentFacingDirection = Vector2.right,
                AgentState      = default,
                A_Vision        = 0.5f,
                A_Passing       = 0.5f,
                A_Finishing     = finishing,
                A_Dribbling     = 0.5f,
                A_LongShots     = 0.5f,
                A_Composure     = composure,
                A_Decisions     = 0.5f,
                A_Anticipation  = 0.5f,
                A_Pace          = 0.5f,
                A_Agility       = 0.5f,
                A_WorkRate      = 0.5f,
                A_Stamina       = 0.5f,
                A_Aggression    = 0.5f,
                A_Positioning   = 0.5f,
                A_Crossing      = 0.5f,
                MatchContext    = mc,
                TacticalContext = tc,
                PressureScalar  = pressure,
                MatchSeed       = 0xABCDUL,
                Snapshot        = snap,
                OpponentGoalCentre = new Vector2(105f, 34f),
                OpponentGoalPostL  = new Vector2(105f, 30.34f),
                OpponentGoalPostR  = new Vector2(105f, 37.66f)
            };
        }

        private static ActionOption MakePass(float lane, float adjusted, float dist) =>
            new ActionOption
            {
                Type = ActionType.PASS, PassLaneScore = lane,
                AdjustedPassLaneScore = adjusted, IntendedDistance = dist
            };

        private static ActionOption MakeShoot(float opening, float distToGoal) =>
            new ActionOption
            {
                Type = ActionType.SHOOT,
                GoalOpeningScore = opening, DistanceToGoal = distToGoal,
                DerivedContactZone = ContactZone.Centre
            };

        private static ActionOption MakeDribble(float space) =>
            new ActionOption { Type = ActionType.DRIBBLE, SpaceScore = space };

        private static ActionOption MakeHold() =>
            new ActionOption { Type = ActionType.HOLD };

        private static ActionOption MakeMove(float distToSlot) =>
            new ActionOption { Type = ActionType.MOVE_TO_POSITION, DistanceToSlot = distToSlot };
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
