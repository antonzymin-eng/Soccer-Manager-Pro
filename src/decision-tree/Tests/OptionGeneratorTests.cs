// File:     src/decision-tree/Tests/OptionGeneratorTests.cs
// Created:  2026-05-29
// Modified: 2026-06-01
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
#endregion
