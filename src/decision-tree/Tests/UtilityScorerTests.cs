// File:     src/decision-tree/Tests/UtilityScorerTests.cs
// Created:  2026-05-29
// Modified: 2026-06-01
// Modified: 2026-07-07 (cheap-item addition: rest-defense risk dampener test; half-spaces
// Modified: 2026-07-28 (ERR-008-017 — DistanceQuality_SHOOT locks)
//           test added then reverted after user review — see VersionHistory)
// Author:   —
// Spec:     Decision Tree #8 §5 (UT-US-01, UT-US-03, UT-05 through UT-09), §3.2.10, new §3.2/§7.7, Code Standards #20
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

        // ── ERR-008-013: scoring SAVE must not crash on the 7-wide tactic tables ──

        [Test]
        public void Save_ScoredUnderNonIdentityTactic_DoesNotThrow_AndYieldsFiniteBase()
        {
            // ComputeUtility exempts SAVE from PlayerTacticActionMultiplier — which indexes the 7-wide
            // RoleWeightModifiers/TempoActionBias tables by the action ordinal. Without that guard,
            // scoring a SAVE option (a = 7) reads out of bounds → IndexOutOfRangeException. A non-identity
            // per-agent tactic is set to prove the guard, not the table, is what runs.
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            ctx.TacticalContext.PlayerTactic = TacticalDirector.TacticalInstructions.PlayerTactic.Default(
                TacticalDirector.TacticalInstructions.PlayerRole.BallWinningMid);
            ctx.TacticalContext.Tempo = TacticalDirector.TacticalInstructions.Tempo.VeryFast;

            Buffer[0] = new ActionOption { Type = ActionType.SAVE, TargetPosition = Vector2.zero };
            Assert.DoesNotThrow(() => UtilityScorer.ScoreOptions(Buffer, 1, in ctx),
                "Scoring SAVE must not index the 7-wide per-agent tactic tables out of bounds.");
            Assert.IsTrue(Buffer[0].BaseUtility > 0f && float.IsFinite(Buffer[0].BaseUtility),
                "SAVE yields a finite positive base utility (U_BASE_SAVE), not the floor or NaN.");
        }

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
            ctx.BallZone = FieldZone.MIDFIELD;

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
            DecisionContext ctxHigh = BuildOffBallContext(PressingMode.HIGH);
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
            ctx.BallZone = FieldZone.ATTACKING;

            Buffer[0] = MakePass(0.8f, 0.8f, 10.0f);
            Buffer[1] = MakeShoot(1.0f, 10.0f);
            Buffer[2] = MakeHold();
            UtilityScorer.ScoreOptions(Buffer, 3, in ctx);

            float shootU = 0.0f, passU = 0.0f;
            for (int i = 0; i < 3; i++)
            {
                if (Buffer[i].Type == ActionType.SHOOT) shootU = Buffer[i].BaseUtility;
                if (Buffer[i].Type == ActionType.PASS) passU = Buffer[i].BaseUtility;
            }

            Assert.Greater(shootU, passU, "SHOOT should dominate PASS in attacking third with open goal");
        }

        // ── UT-07: PASS dominates in defensive third ──────────────────────────

        [Test]
        public void PassDominates_InDefensiveThird_ClearLane()
        {
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            ctx.BallZone = FieldZone.DEFENSIVE;

            Buffer[0] = MakePass(1.0f, 1.0f, 15.0f);
            Buffer[1] = MakeDribble(0.5f);
            Buffer[2] = MakeHold();
            UtilityScorer.ScoreOptions(Buffer, 3, in ctx);

            float passU = 0.0f, dribbleU = 0.0f, holdU = 0.0f;
            for (int i = 0; i < 3; i++)
            {
                if (Buffer[i].Type == ActionType.PASS) passU = Buffer[i].BaseUtility;
                if (Buffer[i].Type == ActionType.DRIBBLE) dribbleU = Buffer[i].BaseUtility;
                if (Buffer[i].Type == ActionType.HOLD) holdU = Buffer[i].BaseUtility;
            }

            Assert.Greater(passU, holdU, "PASS should dominate HOLD in defensive third");
            Assert.Greater(passU, dribbleU, "PASS should dominate DRIBBLE in defensive third");
        }

        // ── UT-08: High pressure reduces PASS and HOLD utilities ──────────────

        [Test]
        public void HighPressure_ReducesPassAndHold()
        {
            DecisionContext ctxLow = BuildContext(0.5f, 0.5f, 0.0f);
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
            ctx.BallZone = FieldZone.ATTACKING;

            Buffer[0] = MakeHold();
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);

            Assert.GreaterOrEqual(Buffer[0].BaseUtility, UtilityWeights.UTILITY_FLOOR,
                "HOLD utility must never fall below UTILITY_FLOOR");
        }

        // ── Cheap-item addition (new §3.2/§7.7): rest-defense risk dampener,
        // redesigned after user review to gate on the ball carrier's own
        // tactical awareness (A_Decisions/A_Anticipation) rather than a flat
        // team-wide penalty — an unaware carrier takes the risky action
        // anyway; the manager, not the AI, must correct a genuine tactical flaw.

        [Test]
        public void RestDefenseInsufficient_DampensPassShootDribble_ButNotHold()
        {
            DecisionContext sufficient = BuildContext(0.5f, 0.5f, 0.0f);
            Assert.IsTrue(sufficient.TacticalContext.RestDefenseSufficient,
                "Stage0Default must seed the sufficient identity (no dampening).");

            DecisionContext insufficient = sufficient;
            TacticalContext tc = insufficient.TacticalContext;
            tc.RestDefenseSufficient = false;
            insufficient.TacticalContext = tc;

            Buffer[0] = MakePass(0.5f, 0.5f, 15.0f);
            UtilityScorer.ScoreOptions(Buffer, 1, in sufficient);
            float passSufficient = Buffer[0].BaseUtility;

            Buffer[0] = MakePass(0.5f, 0.5f, 15.0f);
            UtilityScorer.ScoreOptions(Buffer, 1, in insufficient);
            float passInsufficient = Buffer[0].BaseUtility;

            Assert.Less(passInsufficient, passSufficient,
                "Insufficient rest-defense coverage must dampen PASS utility when the " +
                "carrier has non-zero awareness (BuildContext seeds A_Decisions/A_Anticipation = 0.5).");

            Buffer[0] = MakeHold();
            UtilityScorer.ScoreOptions(Buffer, 1, in sufficient);
            float holdSufficient = Buffer[0].BaseUtility;

            Buffer[0] = MakeHold();
            UtilityScorer.ScoreOptions(Buffer, 1, in insufficient);
            float holdInsufficient = Buffer[0].BaseUtility;

            Assert.AreEqual(holdSufficient, holdInsufficient, 1e-6f,
                "HOLD must not be dampened by rest-defense coverage (only PASS/SHOOT/DRIBBLE are).");
        }

        [Test]
        public void RestDefenseInsufficient_ObliviousCarrier_TakesNoDampening()
        {
            DecisionContext insufficient = BuildContext(0.5f, 0.5f, 0.0f);
            TacticalContext tc = insufficient.TacticalContext;
            tc.RestDefenseSufficient = false;
            insufficient.TacticalContext = tc;
            insufficient.A_Decisions = 0.0f;
            insufficient.A_Anticipation = 0.0f;

            DecisionContext sufficient = insufficient;
            TacticalContext tcSuff = sufficient.TacticalContext;
            tcSuff.RestDefenseSufficient = true;
            sufficient.TacticalContext = tcSuff;

            Buffer[0] = MakePass(0.5f, 0.5f, 15.0f);
            UtilityScorer.ScoreOptions(Buffer, 1, in sufficient);
            float passSufficient = Buffer[0].BaseUtility;

            Buffer[0] = MakePass(0.5f, 0.5f, 15.0f);
            UtilityScorer.ScoreOptions(Buffer, 1, in insufficient);
            float passInsufficient = Buffer[0].BaseUtility;

            Assert.AreEqual(passSufficient, passInsufficient, 1e-6f,
                "A carrier with zero awareness (Decisions=Anticipation=0) must take no " +
                "dampening at all — the risk is invisible to them, not silently corrected.");
        }

        // ── Dismarking #23 §3.4 (FM-DM-03): marked-pass-target penalty ────────

        [Test]
        public void MarkedPassTarget_Penalised_ExactWorkedExample()
        {
            // #23 §3.4 worked example: opponent perceived 0.9 m from the pass target ⇒
            // targetProx01 = 1 − 0.9/3.0 = 0.7; awareness 0.8 ⇒ mult = 1 − 0.3×0.56 = 0.832.
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            TacticalContext tc = ctx.TacticalContext;
            tc.DismarkIntensity = TacticalInstructions.DismarkIntensity.Aggressive;
            ctx.TacticalContext = tc;
            ctx.A_Decisions = 0.8f;
            ctx.A_Anticipation = 0.8f;

            var target = new Vector2(60f, 34f);

            // Free target (no visible opponents) — the baseline.
            Buffer[0] = MakePassTo(target);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);
            float freeU = Buffer[0].BaseUtility;

            // Marked target: one perceived opponent 0.9 m away.
            ctx.Snapshot = ViewWithOpponentAt(new Vector2(60f, 34.9f));
            Buffer[0] = MakePassTo(target);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);
            float markedU = Buffer[0].BaseUtility;

            Assert.AreEqual(0.832f, markedU / freeU, 1e-4f,
                "FM-DM-03 worked example: Lerp(1.0, 0.7, 0.7×0.8) = 0.832 relative to a free target");
        }

        [Test]
        public void MarkedPassTarget_OffDial_IsExactIdentity()
        {
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            Assert.AreEqual(TacticalInstructions.DismarkIntensity.Off, ctx.TacticalContext.DismarkIntensity,
                "Stage0Default must seed the Off identity (FR-DM-012).");

            var target = new Vector2(60f, 34f);

            Buffer[0] = MakePassTo(target);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);
            float freeU = Buffer[0].BaseUtility;

            // Opponent right on top of the target — at Off the penalty must not apply at all.
            ctx.Snapshot = ViewWithOpponentAt(target);
            Buffer[0] = MakePassTo(target);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);
            float markedU = Buffer[0].BaseUtility;

            Assert.AreEqual(freeU, markedU, 0f,
                "Off dial must be the exact ×1.0 identity (FR-DM-012) — bitwise-equal utility");
        }

        [Test]
        public void MarkedPassTarget_UnawarePasser_TakesNoPenalty()
        {
            // FR-DM-010: awareness01 scales the penalty — an unaware passer plays the marked
            // pass anyway (mirrors the rest-defense design).
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            TacticalContext tc = ctx.TacticalContext;
            tc.DismarkIntensity = TacticalInstructions.DismarkIntensity.Aggressive;
            ctx.TacticalContext = tc;
            ctx.A_Decisions = 0.0f;
            ctx.A_Anticipation = 0.0f;

            var target = new Vector2(60f, 34f);

            Buffer[0] = MakePassTo(target);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);
            float freeU = Buffer[0].BaseUtility;

            ctx.Snapshot = ViewWithOpponentAt(target);
            Buffer[0] = MakePassTo(target);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);
            float markedU = Buffer[0].BaseUtility;

            Assert.AreEqual(freeU, markedU, 1e-6f,
                "Zero-awareness passer must take no marked-target penalty (FR-DM-010)");
        }

        [Test]
        public void MarkedPassTarget_DistantOpponent_NoPenalty()
        {
            // An opponent beyond MarkedPassRadiusM (3.0 m) contributes zero proximity.
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            TacticalContext tc = ctx.TacticalContext;
            tc.DismarkIntensity = TacticalInstructions.DismarkIntensity.Aggressive;
            ctx.TacticalContext = tc;

            var target = new Vector2(60f, 34f);

            Buffer[0] = MakePassTo(target);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);
            float freeU = Buffer[0].BaseUtility;

            ctx.Snapshot = ViewWithOpponentAt(new Vector2(60f, 34f + TacticalWeights.MarkedPassRadiusM + 1f));
            Buffer[0] = MakePassTo(target);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);
            float markedU = Buffer[0].BaseUtility;

            Assert.AreEqual(freeU, markedU, 1e-6f,
                "An opponent outside the marking radius must contribute no penalty");
        }

        private static ActionOption MakePassTo(Vector2 targetPosition) =>
            new ActionOption
            {
                Type = ActionType.PASS,
                PassLaneScore = 0.5f,
                AdjustedPassLaneScore = 0.5f,
                IntendedDistance = 10.0f,
                TargetPosition = targetPosition
            };

        private static FilteredView ViewWithOpponentAt(Vector2 perceivedPosition)
        {
            return new FilteredView
            {
                ObserverId = 0,
                FrameNumber = 1,
                VisibleTeammates = new PerceivedAgent[0],
                VisibleOpponents = new[]
                {
                    new PerceivedAgent { AgentId = 15, PerceivedPosition = perceivedPosition }
                },
                VisibleOpponentsCount = 1,
                BlindSidePerceivedAgents = new PerceivedAgent[0]
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static DecisionContext BuildContext(
            float composure, float finishing, float pressure)
        {
            var mc = new MatchContext
            {
                Phase = MatchPhase.OPEN_PLAY,
                BallZone = FieldZone.MIDFIELD,
                Possession = PossessionState.HOME_TEAM,
                PossessingAgentId = 0
            };
            var tc = TacticalContext.Stage0Default(new Vector2(50f, 34f));
            var snap = new FilteredView
            {
                ObserverId = 0,
                FrameNumber = 1,
                VisibleTeammates = new PerceivedAgent[0],
                VisibleOpponents = new PerceivedAgent[0],
                BlindSidePerceivedAgents = new PerceivedAgent[0]
            };

            return new DecisionContext
            {
                AgentId = 0,
                AgentTeamId = 0,
                CurrentFrame = 1,
                AgentHasBall = true,
                PossessedByTeam = PossessionState.HOME_TEAM,
                AgentPosition = new Vector2(52f, 34f),
                AgentFacingDirection = Vector2.right,
                AgentState = default,
                A_Vision = 0.5f,
                A_Passing = 0.5f,
                A_Finishing = finishing,
                A_Dribbling = 0.5f,
                A_LongShots = 0.5f,
                A_Composure = composure,
                A_Decisions = 0.5f,
                A_Anticipation = 0.5f,
                A_Pace = 0.5f,
                A_Agility = 0.5f,
                A_WorkRate = 0.5f,
                A_Stamina = 0.5f,
                A_Aggression = 0.5f,
                A_Positioning = 0.5f,
                A_Crossing = 0.5f,
                MatchContext = mc,
                BallZone = FieldZone.MIDFIELD,   // scorer reads the team-relative ctx field (AR-2 H-2)
                TacticalContext = tc,
                PressureScalar = pressure,
                MatchSeed = 0xABCDUL,
                Snapshot = snap,
                OpponentGoalCentre = new Vector2(105f, 34f),
                OpponentGoalPostL = new Vector2(105f, 30.34f),
                OpponentGoalPostR = new Vector2(105f, 37.66f)
            };
        }

        private static ActionOption MakePass(float lane, float adjusted, float dist) =>
            new ActionOption
            {
                Type = ActionType.PASS,
                PassLaneScore = lane,
                AdjustedPassLaneScore = adjusted,
                IntendedDistance = dist
            };

        private static ActionOption MakeShoot(float opening, float distToGoal) =>
            new ActionOption
            {
                Type = ActionType.SHOOT,
                GoalOpeningScore = opening,
                DistanceToGoal = distToGoal,
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
                Phase = MatchPhase.OPEN_PLAY,
                BallZone = FieldZone.MIDFIELD,
                Possession = PossessionState.AWAY_TEAM,
                PossessingAgentId = 15
            };
            var tc = TacticalContext.Stage0Default(new Vector2(50f, 34f));
            tc.Pressing = pressing;
            var snap = new FilteredView
            {
                ObserverId = 0,
                FrameNumber = 1,
                VisibleTeammates = new PerceivedAgent[0],
                VisibleOpponents = new PerceivedAgent[0],
                BlindSidePerceivedAgents = new PerceivedAgent[0]
            };

            return new DecisionContext
            {
                AgentId = 0,
                AgentTeamId = 0,
                CurrentFrame = 1,
                AgentHasBall = false,
                PossessedByTeam = PossessionState.AWAY_TEAM,
                AgentPosition = new Vector2(52f, 34f),
                AgentFacingDirection = Vector2.right,
                AgentState = default,
                A_Vision = 0.5f,
                A_Passing = 0.5f,
                A_Finishing = 0.5f,
                A_Dribbling = 0.5f,
                A_LongShots = 0.5f,
                A_Composure = 0.5f,
                A_Decisions = 0.5f,
                A_Anticipation = 0.5f,
                A_Pace = 0.5f,
                A_Agility = 0.5f,
                A_WorkRate = 0.5f,
                A_Stamina = 0.5f,
                A_Aggression = 0.5f,
                A_Positioning = 0.5f,
                A_Crossing = 0.5f,
                MatchContext = mc,
                BallZone = FieldZone.MIDFIELD,
                OpponentHasBall = true,   // home agent (team 0), AWAY_TEAM possesses
                TacticalContext = tc,
                PressureScalar = 0.0f,
                MatchSeed = 0xABCDUL,
                Snapshot = snap,
                OpponentGoalCentre = new Vector2(105f, 34f),
                OpponentGoalPostL = new Vector2(105f, 30.34f),
                OpponentGoalPostR = new Vector2(105f, 37.66f)
            };
        }

        // ── AR-2 M-1 lock: §3.4.6 press urgency follows OPPONENT possession ──

        [Test]
        public void PressUrgency_AppliesOnlyUnderOpponentPossession()
        {
            // Same context, urgency flag flipped: the ratio must equal
            // PressUrgencyFactor exactly (all other terms identical).
            DecisionContext ctxOpp = BuildOffBallContext(PressingMode.MEDIUM);
            ctxOpp.OpponentHasBall = true;
            DecisionContext ctxOwn = BuildOffBallContext(PressingMode.MEDIUM);
            ctxOwn.OpponentHasBall = false;   // own-team (or contested) possession

            Buffer[0] = MakePress(0.8f);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctxOpp);
            float pressOpp = Buffer[0].BaseUtility;

            Buffer[0] = MakePress(0.8f);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctxOwn);
            float pressOwn = Buffer[0].BaseUtility;

            Assert.AreEqual(TacticalWeights.PressUrgencyFactor, pressOpp / pressOwn, 1e-4f,
                "§3.4.6 urgency must multiply PRESS only under opponent possession " +
                "(AR-2 M-1: was keyed to the absolute AWAY_TEAM literal)");
        }

        // ── AR-2 M-4 lock: midfield long-shot gate uses the SHIFTED form ──────

        [Test]
        public void ShootMidfield_LongShotsRaw12_GetsLongModifier()
        {
            // Raw LongShots = 12 → A = 11/19 ≈ 0.579 → shifted ≈ 0.789 > 0.75 ⇒ the
            // 0.55 midfield modifier applies (§3.2.3.4: effective threshold raw ≥ 11).
            // Under the pre-fix raw-form comparison (0.579 < 0.75) the shot was
            // suppressed to SHOOT_ZONE_MID_SHORT = 0.05.
            // Distance sits INSIDE the sweet range (ERR-008-017): the option's distance is
            // incidental to this lock's intent (the shifted-form zone gate), and at the
            // former 28 m the DistanceQuality decay pushed the suppressed branch into the
            // UTILITY_FLOOR clamp, corrupting the pure zone-modifier ratio.
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            ctx.BallZone = FieldZone.MIDFIELD;
            ctx.A_LongShots = 11.0f / 19.0f;

            Buffer[0] = MakeShoot(0.7f, 10.0f);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);
            float withLong = Buffer[0].BaseUtility;

            ctx.A_LongShots = 0.0f;   // raw 1 → shifted 0.5 < 0.75 ⇒ suppressed
            Buffer[0] = MakeShoot(0.7f, 10.0f);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);
            float withoutLong = Buffer[0].BaseUtility;

            float expectedRatio = UtilityWeights.SHOOT_ZONE_MID_LONG / UtilityWeights.SHOOT_ZONE_MID_SHORT;
            Assert.AreEqual(expectedRatio, withLong / withoutLong, expectedRatio * 0.01f,
                "Midfield long-shot gate must compare the shifted attribute form (§3.2.3.1)");
        }

        // ── AR-2 M-3 lock: SHOOT risk driven by (1 − GoalOpeningScore) ─────────

        [Test]
        public void ShootRisk_ScalesWithBlockedGoal_NotFinishing()
        {
            // At opening = 1.0 the §3.2.3.1 risk term is zero regardless of pressure;
            // pre-fix the (1 − A_Finishing) form produced a nonzero penalty here.
            DecisionContext ctxP0 = BuildContext(0.5f, 0.2f, 0.0f);
            ctxP0.BallZone = FieldZone.ATTACKING;
            DecisionContext ctxP1 = BuildContext(0.5f, 0.2f, 1.0f);
            ctxP1.BallZone = FieldZone.ATTACKING;

            Buffer[0] = MakeShoot(1.0f, 12.0f);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctxP0);
            float openNoPressure = Buffer[0].BaseUtility;

            Buffer[0] = MakeShoot(1.0f, 12.0f);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctxP1);
            float openMaxPressure = Buffer[0].BaseUtility;

            Assert.AreEqual(openNoPressure, openMaxPressure, 1e-5f,
                "With a fully open goal, RiskPenalty_SHOOT = (1−1.0)×P×coeff = 0 at any pressure (§3.2.3.1)");
        }

        // ── ERR-008-017 locks: DistanceQuality_SHOOT (shot-volume design KD-V2/KD-V3) ────

        private float ScoreShootAt(float distToGoal, in DecisionContext ctx)
        {
            Buffer[0] = MakeShoot(0.7f, distToGoal);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);
            return Buffer[0].BaseUtility;
        }

        [Test]
        public void ShootDistance_InsideSweetRange_IsDistanceIndifferent()
        {
            // distQ = 1.0 for every d ≤ SHOOT_SWEET_RANGE_M, so utilities at 0 m (the KD-V3
            // direct-injection default), mid-sweet and the knee itself are bitwise equal —
            // which is also the proof that every pre-ERR-008-017 close-range calibration
            // (§5.Z.17/§5.Z.19) is untouched.
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            ctx.BallZone = FieldZone.ATTACKING;

            float atZero = ScoreShootAt(0.0f, in ctx);
            float atMid = ScoreShootAt(UtilityWeights.SHOOT_SWEET_RANGE_M * 0.5f, in ctx);
            float atKnee = ScoreShootAt(UtilityWeights.SHOOT_SWEET_RANGE_M, in ctx);

            Assert.AreEqual(atZero, atMid, 0.0f, "inside the sweet range distance must be inert");
            Assert.AreEqual(atZero, atKnee, 0.0f, "the knee point itself is inside the range (d ≤ SWEET)");
        }

        [Test]
        public void ShootDistance_DecaysMonotonicallyBeyondSweetRange()
        {
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            ctx.BallZone = FieldZone.ATTACKING;

            float knee = ScoreShootAt(UtilityWeights.SHOOT_SWEET_RANGE_M, in ctx);
            float mid1 = ScoreShootAt(18.0f, in ctx);
            float mid2 = ScoreShootAt(26.0f, in ctx);
            float far = ScoreShootAt(34.0f, in ctx);

            Assert.Greater(knee, mid1, "utility must fall beyond the sweet range");
            Assert.Greater(mid1, mid2, "decay must be monotone");
            Assert.Greater(mid2, far, "decay must be monotone to the range-gate boundary");

            // Continuity at the knee: an epsilon step beyond loses ~nothing.
            float justBeyond = ScoreShootAt(UtilityWeights.SHOOT_SWEET_RANGE_M + 0.01f, in ctx);
            Assert.AreEqual(knee, justBeyond, knee * 0.01f,
                "distQ must be continuous at the knee (hyperbolic form, no cliff)");
        }

        [Test]
        public void ShootDistance_HalfQuality_OneFalloffBeyondSweetRange()
        {
            // distQ(SWEET + FALLOFF) = FALLOFF / (FALLOFF + FALLOFF) = 0.5 exactly — the
            // shape's defining point (the §3.2.3.1 worked derivation).
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            ctx.BallZone = FieldZone.ATTACKING;

            float atKnee = ScoreShootAt(UtilityWeights.SHOOT_SWEET_RANGE_M, in ctx);
            float atHalf = ScoreShootAt(
                UtilityWeights.SHOOT_SWEET_RANGE_M + UtilityWeights.SHOOT_DIST_FALLOFF_M, in ctx);

            Assert.AreEqual(atKnee * 0.5f, atHalf, atKnee * 1e-4f,
                "one falloff length beyond the sweet range must score exactly half the knee utility");
        }

        [Test]
        public void LongRangeOpenShot_LosesToModeratePass_CloseShotDoesNot()
        {
            // The discriminating comparison ERR-008-017 exists for: pre-fix an open 30 m shot
            // (≈ 0.36 at neutral attributes) beat a moderate pass (≈ 0.24) and shots clustered
            // at the range-gate boundary (measured means 30–34 m); post-fix the same long shot
            // loses to that pass while the close shot still wins.
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            ctx.BallZone = FieldZone.ATTACKING;

            Buffer[0] = MakePass(0.5f, 0.5f, 15.0f);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);
            float moderatePass = Buffer[0].BaseUtility;

            float longShot = ScoreShootAt(30.0f, in ctx);
            float closeShot = ScoreShootAt(10.0f, in ctx);

            Assert.Less(longShot, moderatePass,
                "an open 30 m shot must LOSE to a moderate pass (pre-ERR-008-017 it won)");
            Assert.Greater(closeShot, moderatePass,
                "an open close-range shot must still beat the same pass");
        }

        [Test]
        public void ShootDistanceConstants_ShapeGuards()
        {
            // The knee must sit strictly inside the un-bonused range gate, or the term never
            // bites for low-LongShots shooters; both lengths must be positive for the
            // hyperbolic form to be defined and bounded (0, 1].
            Assert.Greater(UtilityWeights.SHOOT_SWEET_RANGE_M, 0.0f);
            Assert.Greater(UtilityWeights.SHOOT_DIST_FALLOFF_M, 0.0f);
            Assert.Less(UtilityWeights.SHOOT_SWEET_RANGE_M, UtilityWeights.BASE_SHOOT_RANGE,
                "SHOOT_SWEET_RANGE_M must sit inside BASE_SHOOT_RANGE (§3.1.4.2) or the decay is unreachable");
        }

        // ── ERR-008-018 locks: DirectionQuality_DRIBBLE (close-chance-creation design KD-CC2) ──

        /// <summary>A DRIBBLE option carrying an explicit unit direction. The context's agent sits at
        /// (52, 34) with the opponent goal at (105, 34), so +X is exactly goalward.</summary>
        private static ActionOption MakeDribbleToward(float space, Vector2 direction) =>
            new ActionOption
            {
                Type = ActionType.DRIBBLE,
                SpaceScore = space,
                BestDribbleDirection = direction
            };

        private float ScoreDribbleToward(Vector2 direction, in DecisionContext ctx)
        {
            Buffer[0] = MakeDribbleToward(0.8f, direction);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);
            return Buffer[0].BaseUtility;
        }

        [Test]
        public void DribbleDirection_UnsetDirection_IsExactIdentity()
        {
            // The KD-V3 degenerate-input contract restated for ERR-008-018: an option that never
            // sets BestDribbleDirection (every pre-existing direct-injection fixture in this file,
            // via MakeDribble) must score EXACTLY as if the term were absent — i.e. identically to
            // a straight-at-goal dribble, not at the perpendicular midpoint. This is the assertion
            // that proves the other 20 tests in this fixture are untouched by the new factor.
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            ctx.BallZone = FieldZone.ATTACKING;

            Buffer[0] = MakeDribble(0.8f);
            UtilityScorer.ScoreOptions(Buffer, 1, in ctx);
            float unset = Buffer[0].BaseUtility;

            float goalward = ScoreDribbleToward(Vector2.right, in ctx);

            Assert.AreEqual(goalward, unset, 0.0f,
                "an unset BestDribbleDirection must resolve to the exact ×1.0 identity (KD-V3)");
        }

        [Test]
        public void DribbleDirection_IsMonotoneInCosineToGoal()
        {
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            ctx.BallZone = FieldZone.ATTACKING;

            float goalward = ScoreDribbleToward(Vector2.right, in ctx);
            float square = ScoreDribbleToward(Vector2.up, in ctx);
            float retreating = ScoreDribbleToward(Vector2.left, in ctx);

            Assert.Greater(goalward, square,
                "a dribble at the goal must outscore a square one at equal space");
            Assert.Greater(square, retreating,
                "a square dribble must outscore one straight back at equal space");
        }

        [Test]
        public void DribbleDirection_Retreating_ScoresExactlyTheFloorFractionOfGoalward()
        {
            // The shape's defining points: cosine +1 ⇒ ×1.0, cosine −1 ⇒ ×FLOOR, cosine 0 ⇒ the
            // midpoint. SpaceScore is held equal across all three, so the ratio isolates the term.
            DecisionContext ctx = BuildContext(0.5f, 0.5f, 0.0f);
            ctx.BallZone = FieldZone.ATTACKING;

            float goalward = ScoreDribbleToward(Vector2.right, in ctx);
            float retreating = ScoreDribbleToward(Vector2.left, in ctx);
            float square = ScoreDribbleToward(Vector2.up, in ctx);

            float floor = UtilityWeights.DRIBBLE_GOAL_DIR_MIN_MODIFIER;
            Assert.AreEqual(goalward * floor, retreating, goalward * 1e-4f,
                "a directly-retreating dribble keeps exactly DRIBBLE_GOAL_DIR_MIN_MODIFIER of its utility");
            Assert.AreEqual(goalward * (floor + (1.0f - floor) * 0.5f), square, goalward * 1e-4f,
                "a square dribble sits exactly at the midpoint of the floor and 1.0");
        }

        [Test]
        public void DribbleDirection_IsMirroredForTheAwayTeam()
        {
            // CLAUDE.md trap table, "Home-team-only worked examples": three home/away asymmetry
            // defects (#8 ERR-008-002) shipped because every spec example and every fixture used the
            // home team. DirectionQuality_DRIBBLE reads ctx.OpponentGoalCentre, which is already
            // team-resolved upstream — but that is exactly the reasoning that failed last time, so
            // the away side gets its own lock. An away agent attacks x = 0, so −X is goalward and
            // the entire home/away pair must be an exact reflection.
            DecisionContext home = BuildContext(0.5f, 0.5f, 0.0f);
            home.BallZone = FieldZone.ATTACKING;

            DecisionContext away = BuildContext(0.5f, 0.5f, 0.0f);
            away.BallZone = FieldZone.ATTACKING;
            away.AgentTeamId = 1;
            away.AgentPosition = new Vector2(
                PitchLengthM - home.AgentPosition.x, home.AgentPosition.y);
            away.OpponentGoalCentre = new Vector2(0f, home.OpponentGoalCentre.y);
            away.OpponentGoalPostL = new Vector2(0f, home.OpponentGoalPostL.y);
            away.OpponentGoalPostR = new Vector2(0f, home.OpponentGoalPostR.y);

            // Goalward for each side, then retreating for each side.
            Assert.AreEqual(ScoreDribbleToward(Vector2.right, in home),
                            ScoreDribbleToward(Vector2.left, in away), 1e-6f,
                "a goalward dribble must score identically for the away team attacking x = 0");
            Assert.AreEqual(ScoreDribbleToward(Vector2.left, in home),
                            ScoreDribbleToward(Vector2.right, in away), 1e-6f,
                "a retreating dribble must score identically for the away team");

            // And the away side must actually be discriminating, not uniformly flat.
            Assert.Greater(ScoreDribbleToward(Vector2.left, in away),
                           ScoreDribbleToward(Vector2.right, in away),
                "the away team's goalward dribble (−X) must outscore its retreating one");
        }

        /// <summary>Pitch length (m) — local to the away-mirror fixture, which has to place the away
        /// agent at the reflection of the home agent's position.</summary>
        private const float PitchLengthM = 105.0f;

        [Test]
        public void DribbleDirectionFloor_IsWeakerThanThePassFloor_AndInsideItsShapeBounds()
        {
            // The DRIBBLE floor is deliberately WEAKER (numerically higher) than §3.1.3.5's PASS
            // floor, and the asymmetry is measured rather than incidental: suppressing the dribble
            // pushes the carrier onto HOLD, which has no timeout, and at floors 0.50 and 0.65 one
            // seed in six stalled with mean final-third episodes of 28.6 s and 17.5 s against a
            // healthy 5.1 s (close-chance-creation-design.md §8). Until the HOLD stall is fixed the
            // DRIBBLE floor must not be pushed down to the PASS floor — this assertion is what makes
            // that a deliberate, reviewable decision rather than a drift.
            Assert.Greater(UtilityWeights.DRIBBLE_GOAL_DIR_MIN_MODIFIER,
                UtilityWeights.GOAL_DIR_MIN_MODIFIER,
                "the DRIBBLE directional floor is intentionally weaker than the PASS one (HOLD-stall evidence)");

            Assert.Greater(UtilityWeights.DRIBBLE_GOAL_DIR_MIN_MODIFIER, 0.0f,
                "a zero floor would make a retreating dribble worthless rather than merely worse");
            Assert.Less(UtilityWeights.DRIBBLE_GOAL_DIR_MIN_MODIFIER, 1.0f,
                "1.0 disables the term entirely; above 1.0 would REWARD retreating");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                        |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                      |
// | 1.1     | 2026-06-01 | —      | Added UT-US-01 (PASS formula baseline, bounded output) and UT-US-03 (PRESS   |
// |         |            |        |   utility higher under HIGH pressing; ratio check ≈ 1.40). Decision Tree #8  |
// |         |            |        |   §5 spec requirements. Added MakePress helper and BuildOffBallContext helper. |
// | 1.2     | 2026-06-11 | —      | Audit AR-2: zone writes moved to ctx.BallZone (scorer input after H-2);        |
// |         |            |        |   helpers seed BallZone/OpponentHasBall; new locks — M-1 press urgency under   |
// |         |            |        |   opponent possession, M-4 shifted-form midfield gate (raw 12 passes),         |
// |         |            |        |   M-3 SHOOT risk zero at full goal opening.                                    |
// | 1.3     | 2026-07-07 | —      | Cheap-item addition: RestDefenseInsufficient_DampensPassShootDribble_ButNotHold. |
// | 1.4     | 2026-07-07 | —      | Reverted after user review: HalfSpaceLane_BoostsPassUtility_RelativeToCentral- |
// |         |            |        |   AndWide REMOVED (half-spaces need tactical/player instructions, not a flat  |
// |         |            |        |   bonus). RestDefense test redesigned for the awareness gate: added new       |
// |         |            |        |   RestDefenseInsufficient_ObliviousCarrier_TakesNoDampening.                  |
// | 1.5     | 2026-07-11 | —      | Dismarking #23 §3.4 (FM-DM-03) locks: exact 0.832 worked-example ratio;       |
// |         |            |        |   Off-dial bitwise identity (FR-DM-012); zero-awareness passer no-penalty     |
// |         |            |        |   (FR-DM-010); out-of-radius opponent no-penalty.                             |
// | 1.6     | 2026-07-28 | —      | ERR-008-017 DistanceQuality_SHOOT locks: sweet-range distance indifference    |
// |         |            |        |   (bitwise — proves close-range calibration untouched); monotone decay +      |
// |         |            |        |   knee continuity; exact half-quality at SWEET + FALLOFF; the discriminating  |
// |         |            |        |   long-vs-close-vs-pass comparison; [GT] shape guards.                        |
// | 1.7     | 2026-08-04 | —      | ERR-008-018 DirectionQuality_DRIBBLE locks (close-chance-creation design      |
// |         |            |        |   KD-CC2): unset-direction exact identity (the KD-V3 contract — also the      |
// |         |            |        |   proof the other fixtures here are untouched); monotone in the cosine to     |
// |         |            |        |   goal; exact floor / midpoint ratios; and the anchoring invariant that the   |
// |         |            |        |   PASS and DRIBBLE directional floors do not silently diverge.                |
#endregion
