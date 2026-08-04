// File:     src/decision-tree/Tests/OptionGeneratorTests.cs
// Created:  2026-05-29
// Modified: 2026-06-01
// Modified: 2026-08-04 (ERR-008-020 — §3.1.3.3 pass-lane threat model locks, home + away)
// Author:   —
// Spec:     Decision Tree #8 §5 (UT-OG-01 through UT-OG-07), Code Standards #20
// Purpose:  Unit tests for OptionGenerator. Verifies all 7 action type gates,
//           PASS candidate cap, stale-snapshot INTERCEPT rejection, pitch-bounds
//           invariant, and option set invariants (§3.1.10).

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

        // ── ERR-008-013: SAVE is the sole off-ball option when available ──────

        [Test]
        public void OffBallBranch_SaveAvailable_YieldsExactlyOneSaveOption()
        {
            // The threatened keeper (SaveAvailable set only under the flag) commits to the save: the
            // off-ball branch short-circuits to SAVE alone, so it is selected regardless of composure
            // noise / mentality / role tiebreak (AR-4 — a must-happen action must not depend on
            // out-scoring INTERCEPT, which can reach the clamp ceiling under an aggressive tactic).
            DecisionContext ctx = BuildOffBallContext();
            ctx.TacticalContext.SaveAvailable = true;
            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);
            Assert.AreEqual(1, count, "SaveAvailable must yield exactly one off-ball option.");
            Assert.AreEqual(ActionType.SAVE, Buffer[0].Type, "That sole option must be SAVE.");
        }

        [Test]
        public void OffBallBranch_SaveNotAvailable_GeneratesNoSave()
        {
            // Flag-off / non-keeper: the off-ball branch is byte-identical to pre-integration — no SAVE.
            DecisionContext ctx = BuildOffBallContext();   // SaveAvailable defaults false (Stage0Default)
            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);
            for (int i = 0; i < count; i++)
                Assert.AreNotEqual(ActionType.SAVE, Buffer[i].Type,
                    "SAVE must never be generated when a save is not available.");
        }

        // ── ERR-008-014: the loose-ball collect is the sole off-ball option ───

        [Test]
        public void OffBallBranch_LooseBallCollector_YieldsExactlyOneInterceptOption()
        {
            // The designated collector must COMMIT. Measured on neutral attributes the collect scores
            // ~0.35 against MOVE_TO_POSITION's ~0.21 — a gap of 0.14 that sits inside the +/-0.15
            // composure-noise band, so leaving it to out-score the alternatives made the collector
            // flip-flop between chasing the ball and returning to its slot, and play stopped with the
            // ball lying untouched. Same must-happen shape, and same fix, as SAVE (AR-4).
            DecisionContext ctx = BuildOffBallContext();
            ctx.MatchContext.PossessingAgentId = DecisionTreeConstants.NoPossessorAgentId;
            ctx.MatchContext.BallVelocity      = Vector3.zero;
            ctx.TacticalContext.LooseBallCollector = true;

            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);

            Assert.AreEqual(1, count, "The designated collector must have exactly one option.");
            Assert.AreEqual(ActionType.INTERCEPT, Buffer[0].Type, "That sole option must be the collect.");
            Assert.AreEqual(ctx.MatchContext.BallPosition, Buffer[0].TargetPosition,
                "The collect targets the authoritative ball position — the host designated from ground " +
                "truth, so a stale perceived position could send the collector to the wrong place.");
        }

        [Test]
        public void OffBallBranch_NotTheCollector_GeneratesTheOrdinaryOffBallSet()
        {
            // Identity: an agent that is not the designated collector is unaffected, so a match in which
            // no ball is ever loose-and-at-rest behaves exactly as it did pre-Phase-H.
            DecisionContext ctx = BuildOffBallContext();
            ctx.MatchContext.PossessingAgentId = DecisionTreeConstants.NoPossessorAgentId;
            ctx.MatchContext.BallVelocity      = Vector3.zero;
            // LooseBallCollector defaults false (Stage0Default).

            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);

            bool hasMove = false;
            for (int i = 0; i < count; i++)
                if (Buffer[i].Type == ActionType.MOVE_TO_POSITION) { hasMove = true; break; }
            Assert.IsTrue(hasMove, "A non-collector keeps its ordinary off-ball options.");
        }

        [Test]
        public void InterceptNotGenerated_WhenSlowBallIsPossessed()
        {
            // The §3.1.9.1 minimum-ball-speed gate's real purpose, restated: teammates must not converge
            // on a ball their own carrier is standing over (a carried ball is also slow). Chasing an
            // opponent's carrier is PRESS's job. Only the LOOSE case is exempt, and it routes through the
            // collector short-circuit above rather than through this path.
            DecisionContext ctx = BuildOffBallContext();   // PossessingAgentId = 15 (possessed)
            ctx.MatchContext.BallVelocity = Vector3.zero;

            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);

            for (int i = 0; i < count; i++)
                Assert.AreNotEqual(ActionType.INTERCEPT, Buffer[i].Type,
                    "A slow POSSESSED ball must not generate an intercept.");
        }

        // ── ERR-008-016: PowerIntent floor-plus-modulation (shot-speed design KD-1) ──

        [Test]
        public void Shoot_PowerIntent_NeverBelowTheFloor()
        {
            // The pre-fix product form pinned nearly every shot at its own 0.1 clamp floor
            // (goalOpening × A_Finishing ≤ ~0.3 for a neutral player under any occlusion),
            // composing into measured shot speeds of 7–10 m/s. The floor is the contract:
            // a deliberate shot is always struck hard.
            DecisionContext ctx = BuildShootingContext(finishing: 0.5f);
            ActionOption shoot = GetShootOption(in ctx);
            Assert.GreaterOrEqual(shoot.PowerIntent, UtilityWeights.POWER_INTENT_FLOOR,
                "PowerIntent must never fall below the §3.5.3 floor");
        }

        [Test]
        public void Shoot_PowerIntent_OpenGoalEliteFinisher_ReachesFullPower()
        {
            // goalOpening = 1.0 (no blockers) × A_Finishing = 1.0 ⇒ floor + (1 − floor) = 1.0
            // exactly — the top of the old formula's direction is preserved.
            DecisionContext ctx = BuildShootingContext(finishing: 1.0f);
            ActionOption shoot = GetShootOption(in ctx);
            Assert.AreEqual(1.0f, shoot.PowerIntent, 1e-5f,
                "an elite finisher with an open goal strikes at full power");
        }

        [Test]
        public void Shoot_PowerIntent_BetterFinisherStrikesHarder()
        {
            // Monotonicity above the floor: same opening, higher finishing ⇒ higher intent.
            DecisionContext lo = BuildShootingContext(finishing: 0.3f);
            DecisionContext hi = BuildShootingContext(finishing: 0.9f);
            Assert.Greater(GetShootOption(in hi).PowerIntent, GetShootOption(in lo).PowerIntent,
                "opening × finishing must still modulate the band above the floor");
        }

        private static DecisionContext BuildShootingContext(float finishing)
        {
            DecisionContext ctx = BuildPossessionContext();
            ctx.AgentPosition = new Vector2(92.0f, 34.0f);        // in range, open arc
            ctx.AgentState.Position = ctx.AgentPosition;
            ctx.A_Finishing = finishing;
            return ctx;
        }

        private static ActionOption GetShootOption(in DecisionContext ctx)
        {
            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);
            for (int i = 0; i < count; i++)
                if (Buffer[i].Type == ActionType.SHOOT) return Buffer[i];
            Assert.Fail("expected a SHOOT candidate from the shooting context");
            return default;
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

        // ── AR-2 M-9 lock: a binding Decisions cap selects by PROXIMITY ───────

        [Test]
        public void DecisionsCap_BindsByProximity_NotSnapshotOrder()
        {
            // §3.1.3.6: when the cap binds, teammates are evaluated closest-first.
            // The two NEAREST teammates sit at snapshot indices 3 and 4 — the pre-fix
            // snapshot-order iteration would have picked indices 0 and 1 instead.
            DecisionContext ctx = BuildPossessionContext();
            ctx.A_Decisions = 0.0f;  // Decisions=1 → cap = 2
            ctx.Snapshot.VisibleTeammatesCount = 5;
            float[] xOffsets = { 20.0f, 18.0f, 16.0f, 4.0f, 6.0f };  // metres ahead of agent
            for (int i = 0; i < 5; i++)
                ctx.Snapshot.VisibleTeammates[i] = new PerceivedAgent
                {
                    AgentId = 10 + i,
                    PerceivedPosition = new Vector2(52.0f + xOffsets[i], 34.0f),
                    PerceivedVelocity = Vector2.zero,
                    ConfidenceScore = 1.0f
                };

            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);

            int passCount = 0;
            bool sawNearest = false, sawSecondNearest = false;
            for (int i = 0; i < count; i++)
            {
                if (Buffer[i].Type != ActionType.PASS) continue;
                passCount++;
                if (Buffer[i].TargetAgentId == 13) sawNearest       = true;  // 4m — index 3
                if (Buffer[i].TargetAgentId == 14) sawSecondNearest = true;  // 6m — index 4
            }

            Assert.AreEqual(2, passCount, "Decisions=1 caps PASS candidates at 2");
            Assert.IsTrue(sawNearest && sawSecondNearest,
                "A binding cap must select the CLOSEST teammates (§3.1.3.6), not snapshot order");
        }

        // ── AR-2 L lock: INV-GEN-06 dribble look-ahead clamped to pitch ───────

        [Test]
        public void DribbleTarget_NearTouchline_ClampedToPitchBounds()
        {
            DecisionContext ctx = BuildPossessionContext();
            ctx.AgentPosition = new Vector2(103.0f, 66.5f);          // near corner
            ctx.AgentState.Position = ctx.AgentPosition;
            ctx.AgentFacingDirection = new Vector2(1f, 1f).normalized; // 5m look-ahead exits pitch

            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);
            for (int i = 0; i < count; i++)
            {
                if (Buffer[i].Type != ActionType.DRIBBLE) continue;
                Vector2 tp = Buffer[i].TargetPosition;
                Assert.LessOrEqual(tp.x, 105.0f, "INV-GEN-06: dribble target x within pitch");
                Assert.LessOrEqual(tp.y, 68.0f,  "INV-GEN-06: dribble target y within pitch");
            }
        }

        // ── UT-OG-04: INTERCEPT not generated when ball snapshot is stale ─────

        [Test]
        public void InterceptNotGenerated_WhenBallSnapshotStale()
        {
            DecisionContext ctx = BuildOffBallContext();
            ctx.Snapshot.BallStalenessFrames = 1;
            ctx.Snapshot.BallVisible = true;
            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);
            for (int i = 0; i < count; i++)
                Assert.AreNotEqual(ActionType.INTERCEPT, Buffer[i].Type,
                    "INTERCEPT must not be generated when BallStalenessFrames > 0");
        }

        // ── UT-OG-06: All TargetPositions within pitch bounds ─────────────────

        [Test]
        public void AllTargetPositions_WithinPitchBounds()
        {
            DecisionContext ctx = BuildPossessionContext();
            ctx.Snapshot.VisibleTeammatesCount = 3;
            ctx.Snapshot.VisibleTeammates = new PerceivedAgent[10];
            ctx.Snapshot.VisibleTeammates[0] = new PerceivedAgent
            {
                AgentId = 20,
                PerceivedPosition = new Vector2(30.0f, 20.0f),
                PerceivedVelocity = Vector2.zero,
                ConfidenceScore = 1.0f
            };
            ctx.Snapshot.VisibleTeammates[1] = new PerceivedAgent
            {
                AgentId = 21,
                PerceivedPosition = new Vector2(70.0f, 50.0f),
                PerceivedVelocity = Vector2.zero,
                ConfidenceScore = 1.0f
            };
            ctx.Snapshot.VisibleTeammates[2] = new PerceivedAgent
            {
                AgentId = 22,
                PerceivedPosition = new Vector2(90.0f, 34.0f),
                PerceivedVelocity = Vector2.zero,
                ConfidenceScore = 1.0f
            };

            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);

            for (int i = 0; i < count; i++)
            {
                Vector2 tp = Buffer[i].TargetPosition;
                Assert.GreaterOrEqual(tp.x, 0.0f,
                    $"Option {i} ({Buffer[i].Type}) TargetPosition.x below pitch min (0)");
                Assert.LessOrEqual(tp.x, 105.0f,
                    $"Option {i} ({Buffer[i].Type}) TargetPosition.x above pitch max (105)");
                Assert.GreaterOrEqual(tp.y, 0.0f,
                    $"Option {i} ({Buffer[i].Type}) TargetPosition.y below pitch min (0)");
                Assert.LessOrEqual(tp.y, 68.0f,
                    $"Option {i} ({Buffer[i].Type}) TargetPosition.y above pitch max (68)");
            }
        }

        // ── ERR-008-020: §3.1.3.3 pass-lane threat model ──────────────────────
        // Geometry shared by all lane tests: passer at (52,34) → teammate at (62,34),
        // one defender near the lane midpoint (57,34+perp). Mirrored away-side variant
        // uses the leftward lane (62,34) → (52,34) per the home-team-only-example trap.

        [Test]
        public void PassLane_AverageDefender_AtLaneCentre_ScoresExactlyOneThreatUnit()
        {
            // Doctrine P5 pivot: an ability-neutral defender dead-centre in the lane
            // costs exactly what one binary interceptor cost pre-ERR-008-020.
            float score = LaneScoreWithDefender(perp: 0.0f, attrs: null,
                anticipation: 10, pace: 10, visionA: 0.5f);
            Assert.AreEqual(1.0f - 1.0f / UtilityWeights.PASS_LANE_DIVISOR, score, 1e-4f,
                "A neutral defender at lane centre must count as exactly one threat unit.");
        }

        [Test]
        public void PassLane_ComputedAverageDefender_IsWeightNeutral()
        {
            // AR-1 M-1: the pivot row above goes through the null-view guard and never
            // runs the ability computation. This lock drives the COMPUTED path with a
            // true league-average defender (Anticipation 10 + Pace 11 ⇒ mean01 = 0.5
            // exactly) under a Vision-20 passer, so any deviation of the MIN..MAX
            // midpoint from 1.0 — or any defect in the ability formula — surfaces here.
            DtAgentAttributes[] attrs = BuildSquadAttributes();
            float score = LaneScoreWithDefender(perp: 0.0f, attrs: attrs,
                anticipation: 10, pace: 11, visionA: 1.0f);
            Assert.AreEqual(1.0f - 1.0f / UtilityWeights.PASS_LANE_DIVISOR, score, 1e-4f,
                "A computed league-average defender must be weight-neutral (doctrine P5).");
        }

        [Test]
        public void PassLane_AbilityConstants_MidpointIsExactlyNeutral()
        {
            // §3.1.3.3 constants-table MUST: the MIN..MAX midpoint is 1.0 so the
            // average defender neither loosens nor tightens passing. The declared
            // ranges admit violating pairs, so the invariant needs its own lock.
            Assert.AreEqual(1.0f,
                (UtilityWeights.INTERCEPTOR_ABILITY_MIN + UtilityWeights.INTERCEPTOR_ABILITY_MAX) / 2.0f,
                1e-6f,
                "INTERCEPTOR_ABILITY_MIN/MAX must stay centred on 1.0 (P5 pivot).");
        }

        [Test]
        public void PassLane_ThreatIsContinuous_AcrossTheOldCorridorEdge()
        {
            // The pre-fix cliff: perp 0.79 m counted 1.0, perp 0.81 m counted 0.0 — a
            // 0.33 lane-score jump over 2 cm. The falloff must make that step small.
            float scoreInside  = LaneScoreWithDefender(perp: 0.79f, attrs: null,
                anticipation: 10, pace: 10, visionA: 0.5f);
            float scoreOutside = LaneScoreWithDefender(perp: 0.81f, attrs: null,
                anticipation: 10, pace: 10, visionA: 0.5f);
            Assert.Less(Mathf.Abs(scoreOutside - scoreInside), 0.05f,
                "2 cm of defender position must not step the lane score (ERR-008-020 cliff).");
            Assert.Greater(scoreOutside, scoreInside,
                "Threat must still decrease as the defender moves off the lane.");
        }

        // AR-1 L: the expected elite-vs-poor lane-score gap for a full-fidelity passer
        // and a lane-centre defender, derived from the constants rather than hardcoded:
        // weights span MIN..MAX, so scores differ by (MAX − MIN) / DIVISOR (0.2667 at
        // current values). Tests assert half of it so a legitimate [GT] retune shrinks
        // the margin without false-failing, while a lost discrimination still fails.
        private static float ExpectedSightedGap =>
            (UtilityWeights.INTERCEPTOR_ABILITY_MAX - UtilityWeights.INTERCEPTOR_ABILITY_MIN)
            / UtilityWeights.PASS_LANE_DIVISOR;

        [Test]
        public void PassLane_SightedPasser_RespectsEliteAndExploitsPoorDefender()
        {
            DtAgentAttributes[] attrs = BuildSquadAttributes();
            float scoreElite = LaneScoreWithDefender(perp: 0.0f, attrs: attrs,
                anticipation: 20, pace: 20, visionA: 1.0f);
            float scorePoor  = LaneScoreWithDefender(perp: 0.0f, attrs: attrs,
                anticipation: 1, pace: 1, visionA: 1.0f);
            Assert.Less(scoreElite, scorePoor - ExpectedSightedGap * 0.5f,
                "A Vision-20 passer must rate the lane through an elite defender markedly " +
                "worse than the identical lane through a poor one.");
        }

        [Test]
        public void PassLane_LowVisionPasser_BarelySeparatesDefenders()
        {
            // Doctrine P2: Vision is discrimination fidelity. At the floor the passer
            // reads every defender as near-average — today's attribute-blind behaviour.
            DtAgentAttributes[] attrs = BuildSquadAttributes();
            float sightedGap = LaneScoreWithDefender(0.0f, attrs, 1, 1, visionA: 1.0f)
                             - LaneScoreWithDefender(0.0f, attrs, 20, 20, visionA: 1.0f);
            float blindGap   = LaneScoreWithDefender(0.0f, attrs, 1, 1, visionA: 0.0f)
                             - LaneScoreWithDefender(0.0f, attrs, 20, 20, visionA: 0.0f);
            Assert.Greater(blindGap, 0.0f,
                "Even at the fidelity floor a sliver of discrimination survives (floor > 0).");
            Assert.Less(blindGap, sightedGap * 0.5f,
                "A Vision-1 passer must separate elite from poor defenders far less than a Vision-20 one.");
        }

        [Test]
        public void PassLane_NullAttributeView_IsAbilityNeutral()
        {
            // Unwired host / legacy context: every defender reads as 1.0 — the
            // pre-ERR-008-020 weighting, continuous falloff only.
            float scoreA = LaneScoreWithDefender(perp: 0.0f, attrs: null,
                anticipation: 20, pace: 20, visionA: 1.0f);
            float scoreB = LaneScoreWithDefender(perp: 0.0f, attrs: null,
                anticipation: 1, pace: 1, visionA: 1.0f);
            Assert.AreEqual(scoreA, scoreB, 1e-6f,
                "Without an attribute view, defender identity must not affect the lane.");
        }

        [Test]
        public void PassLane_AwayMirror_SightedPasserSeparatesDefenders()
        {
            // Away-team mirror of the discrimination case (home-team-only worked
            // examples shipped three asymmetry defects — CLAUDE.md trap table).
            float scoreElite = AwayLaneScoreWithDefender(anticipation: 20, pace: 20);
            float scorePoor  = AwayLaneScoreWithDefender(anticipation: 1, pace: 1);
            Assert.Less(scoreElite, scorePoor - ExpectedSightedGap * 0.5f,
                "The away-side lane must discriminate defender ability identically to the home side.");
        }

        private static DtAgentAttributes[] BuildSquadAttributes()
        {
            var attrs = new DtAgentAttributes[22];
            for (int i = 0; i < 22; i++)
                attrs[i] = DtAgentAttributes.CreateDefault(teamId: i < 11 ? 0 : 1);
            return attrs;
        }

        // Home-side lane fixture: passer 5 at (52,34) → teammate 7 at (62,34);
        // defender 15 at (57, 34+perp). Returns the generated PASS option's PassLaneScore.
        private static float LaneScoreWithDefender(
            float perp, DtAgentAttributes[] attrs, int anticipation, int pace, float visionA)
        {
            DecisionContext ctx = BuildPossessionContext();
            ctx.A_Vision = visionA;
            if (attrs != null)
            {
                attrs[15].Anticipation = anticipation;
                attrs[15].Pace         = pace;
                ctx.AllAgentAttributes = attrs;
            }
            ctx.Snapshot.VisibleTeammatesCount = 1;
            ctx.Snapshot.VisibleTeammates[0] = new PerceivedAgent
            {
                AgentId = 7,
                PerceivedPosition = new Vector2(62.0f, 34.0f),
                PerceivedVelocity = Vector2.zero,
                ConfidenceScore = 1.0f
            };
            ctx.Snapshot.VisibleOpponentsCount = 1;
            ctx.Snapshot.VisibleOpponents[0] = new PerceivedAgent
            {
                AgentId = 15,
                PerceivedPosition = new Vector2(57.0f, 34.0f + perp),
                PerceivedVelocity = Vector2.zero,
                ConfidenceScore = 1.0f
            };
            return ExtractPassLaneScore(in ctx);
        }

        // Away-side mirror: passer 16 (team 1) at (62,34) facing left → teammate 18 at
        // (52,34); home defender 3 at (57,34). Same lane length, opposite direction.
        private static float AwayLaneScoreWithDefender(int anticipation, int pace)
        {
            DecisionContext ctx = BuildPossessionContext();
            ctx.AgentId       = 16;
            ctx.AgentTeamId   = 1;
            ctx.A_Vision      = 1.0f;
            ctx.AgentPosition = new Vector2(62.0f, 34.0f);
            ctx.AgentFacingDirection = Vector2.left;
            ctx.Snapshot.ObserverId  = 16;
            ctx.MatchContext.PossessingAgentId = 16;
            ctx.PossessedByTeam   = PossessionState.AWAY_TEAM;
            ctx.OpponentGoalCentre = new Vector2(0.0f, 34.0f);
            ctx.OpponentGoalPostL  = new Vector2(0.0f, 37.66f);
            ctx.OpponentGoalPostR  = new Vector2(0.0f, 30.34f);

            DtAgentAttributes[] attrs = BuildSquadAttributes();
            attrs[3].Anticipation = anticipation;
            attrs[3].Pace         = pace;
            ctx.AllAgentAttributes = attrs;

            ctx.Snapshot.VisibleTeammatesCount = 1;
            ctx.Snapshot.VisibleTeammates[0] = new PerceivedAgent
            {
                AgentId = 18,
                PerceivedPosition = new Vector2(52.0f, 34.0f),
                PerceivedVelocity = Vector2.zero,
                ConfidenceScore = 1.0f
            };
            ctx.Snapshot.VisibleOpponentsCount = 1;
            ctx.Snapshot.VisibleOpponents[0] = new PerceivedAgent
            {
                AgentId = 3,
                PerceivedPosition = new Vector2(57.0f, 34.0f),
                PerceivedVelocity = Vector2.zero,
                ConfidenceScore = 1.0f
            };
            return ExtractPassLaneScore(in ctx);
        }

        private static float ExtractPassLaneScore(in DecisionContext ctx)
        {
            int count = OptionGenerator.GenerateOptions(in ctx, Buffer);
            for (int i = 0; i < count; i++)
                if (Buffer[i].Type == ActionType.PASS)
                    return Buffer[i].PassLaneScore;
            Assert.Fail("Expected a PASS option to be generated for the lane fixture.");
            return -1.0f;
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
// | Version | Date       | Author | Notes                                                                     |
// | 1.0     | 2026-05-29 | —      | Initial implementation.                                                   |
// | 1.1     | 2026-06-01 | —      | Added UT-OG-04 (INTERCEPT rejected when BallStalenessFrames > 0) and     |
// |         |            |        |   UT-OG-06 (all TargetPositions within pitch bounds). Decision Tree #8    |
// |         |            |        |   §5 spec requirements.                                                   |
// | 1.2     | 2026-06-11 | —      | Audit AR-2: M-9 proximity-cap lock (binding Decisions cap selects        |
// |         |            |        |   closest-first per §3.1.3.6); INV-GEN-06 dribble-clamp lock.             |
// | 1.4     | 2026-07-28 | —      | ERR-008-016 locks: PowerIntent never below POWER_INTENT_FLOOR, open-goal    |
// |         |            |        | elite finisher = 1.0 exactly, monotone in finishing.                        |
// | 1.5     | 2026-08-04 | —      | ERR-008-020 locks: neutral-defender-at-centre = exactly one threat unit     |
// |         |            |        | (P5 pivot), no 2 cm cliff at the old 0.8 m edge, Vision-20 separates        |
// |         |            |        | elite/poor defenders while Vision-1 barely does (P2), null attribute view   |
// |         |            |        | is ability-neutral, and the discrimination case mirrored to the away side.  |
// | 1.6     | 2026-08-04 | —      | ERR-008-020 AR-1: M-1 — the v1.5 pivot lock went through the null-view      |
// |         |            |        | guard and never ran the ability computation; + the COMPUTED-path pivot      |
// |         |            |        | (Ant 10/Pace 11 ⇒ mean01 = 0.5 exactly) and the MIN/MAX midpoint-is-1.0     |
// |         |            |        | invariant lock. L — discrimination margins derived from the constants       |
// |         |            |        | (ExpectedSightedGap × 0.5) instead of a hardcoded 0.15.                     |
#endregion
