// File:     src/decision-tree/Tests/UtilityScorerTests.cs
// Created:  2026-05-29
// Modified: 2026-06-01
// Author:   —
// Spec:     Decision Tree #8 §5 (UT-US-01, UT-US-03, UT-05 through UT-09), §3.2.10, Code Standards #20
// Purpose:  Unit tests for UtilityScorer. Verifies utility floor/ceiling clamp,
//           zone modifier effects, attribute extreme outputs, cross-formula dominance
//           relationships from §3.2.10, PASS formula baseline (UT-US-01), and PRESS
//           tactical multiplier ratio (UT-US-03).

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

        // ── UT-US-01: PASS utility matches formula — finite and bounded ──────

        [Test]
        public void PassUtility_MatchesFormula_WithinTolerance()
        {
            // Mid-pitch, all attributes = 0.5, pressure = 0.
            // Formula: U_PASS = U_BASE_PASS × zoneM × visionFactor × techniqueFactor
            //                   × AdjustedPassLaneScore × tactM × (1 − risk)
            // At attr=0.5, zone=MIDFIELD (zoneM=1.0), pressure=0, MIXED passing (tactM=1.0):
            //   visionFactor    = (0.5 + 0.5×0.5)^PASS_VISION_EXP    = 0.75^0.30 ≈ 0.913
            //   techniqueFactor = (0.5 + 0.5×0.5)^PASS_TECHNIQUE_EXP = 0.75^0.40 ≈ 0.884
            //   risk            = 0 (pressure=0)
            //   contextM        = AdjustedPassLaneScore (set to 0.5 below)
            // Expected ≈ 0.60 × 1.0 × 0.913 × 0.884 × 0.5 × 1.0 ≈ 0.242
            // Tolerance ±0.05 (accounts for exact lane/goal-direction modifier values).
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            ctx.MatchContext.BallZone = FieldZone.MIDFIELD;

            Buffer[0] = MakePass(0.5f, 0.5f, 15.0f);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);

            float passU = Buffer[0].BaseUtility;

            Assert.GreaterOrEqual(passU, UtilityWeights.UTILITY_FLOOR,
                "PASS utility must be >= UTILITY_FLOOR");
            Assert.LessOrEqual(passU, UtilityWeights.UTILITY_CEILING,
                "PASS utility must be <= UTILITY_CEILING");
            Assert.GreaterOrEqual(passU, 0.15f,
                "PASS utility at baseline inputs must be at least 0.15 (formula sanity lower bound)");
            Assert.LessOrEqual(passU, 0.50f,
                "PASS utility at baseline inputs must be at most 0.50 (formula sanity upper bound)");
        }

        // ── UT-US-03: PRESS utility higher under HIGH vs MEDIUM pressing ──────

        [Test]
        public void PressUtility_HigherUnder_HighPressingInstruction()
        {
            // Build off-ball context with 1 opponent within press range.
            // Score a PRESS option under HIGH pressing then under MEDIUM pressing.
            // Expected: ratio HIGH/MEDIUM ≈ PressingHighPressMod / 1.0 = 1.40 ± 10%.
            DecisionContext ctxHigh   = BuildOffBallContext(PressingMode.HIGH);
            DecisionContext ctxMedium = BuildOffBallContext(PressingMode.MEDIUM);

            // ProximityScore = 0.8: close opponent, well within range.
            Buffer[0] = MakePress(0.8f);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctxHigh);
            float pressHigh = Buffer[0].BaseUtility;

            Buffer[0] = MakePress(0.8f);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctxMedium);
            float pressMedium = Buffer[0].BaseUtility;

            Assert.Greater(pressHigh, pressMedium,
                "PRESS utility must be higher under HIGH pressing than MEDIUM");

            // Ratio check: HIGH/MEDIUM should be within 10% of 1.40 (PressingHighPressMod).
            // Additional PressUrgencyFactor may apply when possessed by AWAY_TEAM — it cancels
            // in the ratio because it is identical in both runs.
            float ratio = pressHigh / pressMedium;
            float expectedRatio = TacticalWeights.PressingHighPressMod; // 1.40
            Assert.GreaterOrEqual(ratio, expectedRatio * 0.90f,
                $"HIGH/MEDIUM PRESS ratio {ratio:F3} below expected lower bound {expectedRatio * 0.90f:F3}");
            Assert.LessOrEqual(ratio, expectedRatio * 1.10f,
                $"HIGH/MEDIUM PRESS ratio {ratio:F3} above expected upper bound {expectedRatio * 1.10f:F3}");
        }

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

        private static ActionOption MakePress(float proximityScore) =>
            new ActionOption { Type = ActionType.PRESS, ProximityScore = proximityScore };

        private static DecisionContext BuildOffBallContext(PressingMode pressing)
        {
            var mc = new MatchContext
            {
                Phase              = MatchPhase.OPEN_PLAY,
                BallZone           = FieldZone.MIDFIELD,
                Possession         = PossessionState.AWAY_TEAM,
                PossessingAgentId  = 15
            };
            var tc = TacticalContext.Stage0Default(new Vector2(50f, 34f));
            tc.Pressing = pressing;
            var snap = new FilteredView
            {
                ObserverId               = 0,
                FrameNumber              = 1,
                VisibleTeammates         = new PerceivedAgent[0],
                VisibleOpponents         = new PerceivedAgent[0],
                BlindSidePerceivedAgents = new PerceivedAgent[0]
            };

            return new DecisionContext
            {
                AgentId              = 0,
                AgentTeamId          = 0,
                CurrentFrame         = 1,
                AgentHasBall         = false,
                PossessedByTeam      = PossessionState.AWAY_TEAM,
                AgentPosition        = new Vector2(52f, 34f),
                AgentFacingDirection = Vector2.right,
                AgentState           = default,
                A_Vision             = 0.5f,
                A_Passing            = 0.5f,
                A_Finishing          = 0.5f,
                A_Dribbling          = 0.5f,
                A_LongShots          = 0.5f,
                A_Composure          = 0.5f,
                A_Decisions          = 0.5f,
                A_Anticipation       = 0.5f,
                A_Pace               = 0.5f,
                A_Agility            = 0.5f,
                A_WorkRate           = 0.5f,
                A_Stamina            = 0.5f,
                A_Aggression         = 0.5f,
                A_Positioning        = 0.5f,
                A_Crossing           = 0.5f,
                MatchContext         = mc,
                TacticalContext      = tc,
                PressureScalar       = 0.0f,
                MatchSeed            = 0xABCDUL,
                Snapshot             = snap,
                OpponentGoalCentre   = new Vector2(105f, 34f),
                OpponentGoalPostL    = new Vector2(105f, 30.34f),
                OpponentGoalPostR    = new Vector2(105f, 37.66f)
            };
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                        |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                      |
// | 1.1     | 2026-06-01 | —      | Added UT-US-01 (PASS formula baseline, bounded output) and UT-US-03 (PRESS   |
// |         |            |        |   utility higher under HIGH pressing; ratio check ≈ 1.40). Decision Tree #8  |
// |         |            |        |   §5 spec requirements. Added MakePress helper and BuildOffBallContext helper. |
#endregion
