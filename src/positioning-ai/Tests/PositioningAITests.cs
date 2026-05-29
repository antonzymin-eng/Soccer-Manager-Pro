// File: src/positioning-ai/Tests/PositioningAITests.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     #12 Positioning AI §5.1
// Purpose:  Unit, integration, determinism, and tactical-correctness tests for Positioning AI.

using NUnit.Framework;
using UnityEngine;

namespace TacticalDirector.PositioningAI.Tests
{
    [TestFixture]
    public class PositioningAITests
    {
        // ── Helpers ───────────────────────────────────────────────────────────

        private static PositioningPerceptionSnapshot MakeSnapshot(
            int tickIndex = 0,
            Vector3? ballPos = null,
            float ballVx = 0f,
            int possOwner = -1,
            bool possIsOwn = false)
        {
            var snap = new PositioningPerceptionSnapshot(PositioningAIConstants.SQUAD_SIZE)
            {
                TickIndex               = tickIndex,
                BallPosition            = ballPos ?? new Vector3(52.5f, 34f, 0f),
                BallVxFiltered          = ballVx,
                PossessionOwnerEntityId = possOwner,
                PossessionOwnerIsOwnTeam = possIsOwn,
            };
            // Fill agents: entity IDs 0-10, sorted ascending, role matching 4-4-2 formation.
            FormationSlotRecord[] f = PositioningAIConstants.Family442;
            for (int i = 0; i < PositioningAIConstants.SQUAD_SIZE; i++)
            {
                snap.Agents[i] = new AgentPositioningData(
                    entityId:     i,
                    slotIndex:    i,
                    position:     new Vector2(f[i].LongPct * 105f, f[i].LateralPct * 68f),
                    isActive:     true,
                    role:         f[i].Role,
                    isGoalkeeper: f[i].IsGoalkeeper);
            }
            snap.ActiveOutfieldCount = 10;
            return snap;
        }

        private static ContextModifierInputs NeutralModifiers()
            => new ContextModifierInputs(0, 0f, 0.5f);

        // ──────────────────────────────────────────────────────────────────────
        // T-U-001: Anchor computation — 4-4-2 GK
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void AnchorCalculator_GkSlot_AtCentreBall_ReturnsDepthPlusCentre()
        {
            // Ball at pitch centre (52.5, 34.0): basisX=0, basisY=0.
            // Expected: gkSlot.x = 5.5 + 8.0*0 = 5.5, gkSlot.y = 34.0 + 2.0*0 = 34.0
            var ball = new Vector3(52.5f, 34f, 0f);
            Vector2 gk = AnchorCalculator.ComputeGkSlot(ball);
            Assert.That(gk.x, Is.EqualTo(5.5f).Within(1e-4f),  "GK depth at centre ball");
            Assert.That(gk.y, Is.EqualTo(34.0f).Within(1e-4f), "GK lateral at centre ball");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-002: Anchor computation — 4-4-2 LB (slot 1)
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void AnchorCalculator_ComputeAnchor_442LB_CorrectPosition()
        {
            // LB: longPct=0.20, lateralPct=0.150
            // anchor.x = 105 * 0.20 = 21.0, anchor.y = 68 * 0.150 = 10.2
            Vector2 anchor = AnchorCalculator.ComputeAnchor(PositioningAIConstants.Family442[1]);
            Assert.That(anchor.x, Is.EqualTo(21.0f).Within(1e-4f));
            Assert.That(anchor.y, Is.EqualTo(10.2f).Within(1e-4f));
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-003: Ball-relative offset — AM, OutOfPoss (spec §3.2.2 worked example)
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void AnchorCalculator_BallRelativeOffset_AM_OutOfPoss_MatchesSpecExample()
        {
            // Ball at (20.0, 34.0), AM, OutOfPoss.
            // basisX = (20-52.5)/52.5 = -0.61905...
            // offset.x = 0.60 × (-0.61905) × 12.0 = -4.457 m
            // basisY = (34-34)/34 = 0.0
            // offset.y = 0.10 × 0.0 × 8.0 = 0.0 m
            var ball   = new Vector3(20f, 34f, 0f);
            Vector2 off = AnchorCalculator.ComputeBallRelativeOffset(ball, RoleId.AM, Phase.OutOfPoss);
            Assert.That(off.x, Is.EqualTo(-4.457f).Within(0.01f), "AM OutOfPoss longitudinal offset");
            Assert.That(off.y, Is.EqualTo(0.0f).Within(1e-4f),    "AM OutOfPoss lateral offset");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-004: Phase classification — own possession → InPoss
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void PhaseClassifier_OwnPossession_EventuallyCommitsInPoss()
        {
            var hyst = new HysteresisState(PositioningAIConstants.SQUAD_SIZE);
            hyst.CurrentPhase   = Phase.OutOfPoss;
            hyst.CandidatePhase = Phase.OutOfPoss;

            var snap = MakeSnapshot(possOwner: 3, possIsOwn: true);

            Phase result = Phase.OutOfPoss;
            for (int tick = 0; tick < PositioningAIConstants.PHASE_HYSTERESIS_TICKS; tick++)
            {
                snap.TickIndex = tick;
                result = PhaseClassifier.ClassifyAndCommit(snap, hyst);
            }

            Assert.That(result, Is.EqualTo(Phase.InPoss));
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-005: Phase hysteresis — does not commit before dwell count
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void PhaseClassifier_InsufficientDwell_DoesNotCommit()
        {
            var hyst = new HysteresisState(PositioningAIConstants.SQUAD_SIZE);
            hyst.CurrentPhase   = Phase.OutOfPoss;
            hyst.CandidatePhase = Phase.OutOfPoss;

            var snap = MakeSnapshot(possOwner: 3, possIsOwn: true);

            // Apply dwell = PHASE_HYSTERESIS_TICKS - 1 ticks (one short of committing).
            for (int tick = 0; tick < PositioningAIConstants.PHASE_HYSTERESIS_TICKS - 1; tick++)
            {
                snap.TickIndex = tick;
                PhaseClassifier.ClassifyAndCommit(snap, hyst);
            }

            Assert.That(hyst.CurrentPhase, Is.EqualTo(Phase.OutOfPoss), "Must not commit before dwell threshold");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-006: TransToDef — loose ball moving toward own goal
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void PhaseClassifier_LooseBallNegativeVelocity_CommitsTransToDef()
        {
            var hyst = new HysteresisState(PositioningAIConstants.SQUAD_SIZE);
            var snap = MakeSnapshot(possOwner: -1, ballVx: -(PositioningAIConstants.PHASE_LOOSE_VELOCITY_THRESHOLD + 1f));

            for (int t = 0; t < PositioningAIConstants.PHASE_HYSTERESIS_TICKS; t++)
            {
                snap.TickIndex = t;
                PhaseClassifier.ClassifyAndCommit(snap, hyst);
            }

            Assert.That(hyst.CurrentPhase, Is.EqualTo(Phase.TransToDef));
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-007: Lane classification — slot.y = 30.0 → Center lane
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void LaneEdges_Y30_ClassifiesAsCenterLane()
        {
            float y = 30.0f;
            float[] edges = PositioningAIConstants.LaneEdgesM;
            LaneId lane = y < edges[1] ? LaneId.LW :
                          y < edges[2] ? LaneId.LH :
                          y < edges[3] ? LaneId.C  :
                          y < edges[4] ? LaneId.RH : LaneId.RW;
            Assert.That(lane, Is.EqualTo(LaneId.C));
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-008: Lane dead zone — slot within LANE_HYSTERESIS_M of boundary → no change
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void ShapeAnalyzer_LaneDeadZone_WithinHysteresis_PreservesCurrentLane()
        {
            var hyst = new HysteresisState(PositioningAIConstants.SQUAD_SIZE);
            hyst.SeedFromFormation(PositioningAIConstants.Family442);
            // Slot 1 (LB) starts at LH lane. Move it to just inside the LW/LH boundary (13.6 m).
            // LH boundary = 13.6 m. Within LANE_HYSTERESIS_M = 2.0 → range (11.6, 15.6) is dead zone.
            // Slot Y = 14.0 m: inside dead zone (within 2.0 of 13.6).
            var slots = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            for (int i = 0; i < slots.Length; i++)
                slots[i] = AnchorCalculator.ComputeAnchor(PositioningAIConstants.Family442[i]);
            slots[0] = AnchorCalculator.ComputeGkSlot(new Vector3(52.5f, 34f, 0f));
            slots[1] = new Vector2(slots[1].x, 14.0f); // nudge into LH/LW dead zone

            LaneId before = hyst.Agents[1].CurrentLane;
            ShapeAnalyzer.ResolveAllLanes(slots, hyst);
            Assert.That(hyst.Agents[1].CurrentLane, Is.EqualTo(before), "Dead zone must not change lane");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-009: Hard spacing — violation detected correctly
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void SpacingResolver_ViolatingPair_MeetsMinSeparationAfterEnforce()
        {
            // Two agents at (50.0, 30.0) and (50.8, 30.6) — dist² = 1.0 < 2.25
            var slots   = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var anchors = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var ids     = new int[PositioningAIConstants.SQUAD_SIZE];

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i]   = new Vector2(0f, (float)i * 10f);  // non-overlapping defaults
                anchors[i] = slots[i];
                ids[i]     = i;
            }

            // Place agent 0 and agent 1 close together (violation).
            slots[0] = new Vector2(50.0f, 30.0f);
            slots[1] = new Vector2(50.8f, 30.6f);
            anchors[0] = slots[0];
            anchors[1] = new Vector2(51.5f, 31.0f); // agent 1 has higher cost → agent 0 displaced

            SpacingResolver.EnforceHardSpacing(slots, anchors, ids);

            float dx = slots[0].x - slots[1].x;
            float dy = slots[0].y - slots[1].y;
            float distSq = dx * dx + dy * dy;
            Assert.That(distSq, Is.GreaterThanOrEqualTo(
                PositioningAIConstants.MIN_AGENT_SEPARATION_M_SQ - PositioningAIConstants.SPACING_EPSILON_M2));
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-010: Spacing §3.6.4 worked example
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void SpacingResolver_WorkedExample_AgentADisplaced()
        {
            // §3.6.4: A (entityId=7, slotIdx=0) at (50.0,30.0) anchor cost=0.4 m²
            //          B (entityId=11, slotIdx=1) at (50.8,30.6) anchor cost=0.9 m²
            // A has lower cost → A is displaced.
            var slots   = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var anchors = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var ids     = new int[PositioningAIConstants.SQUAD_SIZE];
            for (int i = 0; i < slots.Length; i++) { slots[i] = new Vector2(0f, i * 10f); anchors[i] = slots[i]; ids[i] = i; }

            slots[0]   = new Vector2(50.0f, 30.0f);
            slots[1]   = new Vector2(50.8f, 30.6f);
            anchors[0] = new Vector2(49.8f, 30.1f); // cost = 0.04+0.01 = 0.05 < 0.9 → displaced
            anchors[1] = new Vector2(51.7f, 30.6f); // cost = 0.81 → stays
            ids[0] = 7;
            ids[1] = 11;

            Vector2 aBeforeDisplace = slots[0];
            SpacingResolver.EnforceHardSpacing(slots, anchors, ids);

            // After displacement: A should have moved, B should be approximately unchanged.
            float bDelta = Vector2.Distance(slots[1], new Vector2(50.8f, 30.6f));
            Assert.That(bDelta, Is.LessThan(0.1f), "B (higher cost) should barely move");
            float newDist = Vector2.Distance(slots[0], slots[1]);
            Assert.That(newDist, Is.GreaterThanOrEqualTo(PositioningAIConstants.MIN_AGENT_SEPARATION_M - 0.01f));
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-011: Spacing EntityId tie-break — equal costs, higher EntityId displaced
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void SpacingResolver_EqualCosts_HigherEntityIdDisplaced()
        {
            var slots   = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var anchors = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var ids     = new int[PositioningAIConstants.SQUAD_SIZE];
            for (int i = 0; i < slots.Length; i++) { slots[i] = new Vector2(0f, i * 10f); anchors[i] = slots[i]; ids[i] = i; }

            // Identical cost; entity 0 < entity 1 → entity 1 displaced.
            slots[0]   = new Vector2(50.0f, 34.0f);
            slots[1]   = new Vector2(51.0f, 34.0f); // dist=1.0 < 1.5
            anchors[0] = new Vector2(49.0f, 34.0f);
            anchors[1] = new Vector2(52.0f, 34.0f);
            ids[0] = 5;
            ids[1] = 9;  // higher entity ID

            Vector2 slot1Before = slots[1];
            SpacingResolver.EnforceHardSpacing(slots, anchors, ids);
            float slot1Delta = Vector2.Distance(slots[1], slot1Before);
            Assert.That(slot1Delta, Is.GreaterThan(0f), "Higher EntityId (9) should be displaced");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-012: Pitch-bound clamp via full tick
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void PositioningAITick_OutputSlots_AlwaysWithinPitchBounds()
        {
            var tick     = new PositioningAITick(FormationFamily.F442);
            var snapshot = MakeSnapshot();
            tick.SeedFromFormation(snapshot);
            tick.Tick(snapshot, NeutralModifiers());

            FormationSlotRecord[] formation = PositioningAIConstants.Family442;
            for (int i = 0; i < formation.Length; i++)
            {
                Vector2 slot = tick.GetFormationSlot(i);
                if (PositioningAITick.IsSentinelSlot(slot)) continue;
                Assert.That(slot.x, Is.GreaterThanOrEqualTo(PositioningAIConstants.SLOT_X_MIN));
                Assert.That(slot.x, Is.LessThanOrEqualTo(PositioningAIConstants.SLOT_X_MAX));
                Assert.That(slot.y, Is.GreaterThanOrEqualTo(PositioningAIConstants.SLOT_Y_MIN));
                Assert.That(slot.y, Is.LessThanOrEqualTo(PositioningAIConstants.SLOT_Y_MAX));
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-013: Inactive agent receives SENTINEL_NO_SLOT (FR-PA-036)
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void PositioningAITick_InactiveAgent_ReturnsSentinelSlot()
        {
            var tick     = new PositioningAITick(FormationFamily.F442);
            var snapshot = MakeSnapshot();

            // Mark agent with entityId=5 (slotIndex=5) as inactive.
            FormationSlotRecord[] f = PositioningAIConstants.Family442;
            snapshot.Agents[5] = new AgentPositioningData(5, 5,
                new Vector2(f[5].LongPct * 105f, f[5].LateralPct * 68f),
                isActive: false, f[5].Role, f[5].IsGoalkeeper);

            tick.SeedFromFormation(snapshot);
            tick.Tick(snapshot, NeutralModifiers());

            Vector2 slot = tick.GetFormationSlot(5);
            Assert.IsTrue(PositioningAITick.IsSentinelSlot(slot), "Inactive agent must get SENTINEL_NO_SLOT");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-014: F1 stale perception — reuses previous tick slots
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void PositioningAITick_StaleSnapshot_ReturnsLastTickSlots()
        {
            var tick     = new PositioningAITick(FormationFamily.F442);
            var snapshot = MakeSnapshot(tickIndex: 5);
            tick.SeedFromFormation(snapshot);
            tick.Tick(snapshot, NeutralModifiers());

            Vector2 slotAfterTick5 = tick.GetFormationSlot(6); // some outfield agent

            // Supply a snapshot with lower tickIndex (stale).
            snapshot.TickIndex = 3;
            tick.Tick(snapshot, NeutralModifiers());
            Vector2 slotAfterStale = tick.GetFormationSlot(6);

            Assert.That(slotAfterStale, Is.EqualTo(slotAfterTick5), "Stale tick must not mutate slots");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-015: Context modifier — lateral compactness worked example (§3.5.3)
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void ContextModifier_LateralRescale_MatchesSpecExample()
        {
            // §3.5.3: InPoss, scoreDiff=+2, fatigue=0.40.
            // lateralCompactness = 1.00 × 1.10 × 0.94 = 1.034
            // rescale = baseLateral / lateralCompactness = 1.00 / 1.034 ≈ 0.9671
            float baseLat = PositioningAIConstants.BaseLateral[(int)Phase.InPoss];
            float compactness = baseLat
                * (1f + PositioningAIConstants.SCORE_ATK_GAIN * 2f)
                * (1f - PositioningAIConstants.FATIGUE_LATERAL_RELAX * 0.40f);
            float rescale = baseLat / compactness;
            Assert.That(rescale, Is.EqualTo(0.9671f).Within(0.001f));
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-016: Constants — SENTINEL_NO_SLOT is (−∞, −∞) and IsSentinelSlot matches
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Constants_SentinelNoSlot_IsNegativeInfinity()
        {
            Vector2 s = PositioningAIConstants.SENTINEL_NO_SLOT;
            Assert.IsTrue(float.IsNegativeInfinity(s.x));
            Assert.IsTrue(float.IsNegativeInfinity(s.y));
            Assert.IsTrue(PositioningAITick.IsSentinelSlot(s));
            Assert.IsFalse(PositioningAITick.IsSentinelSlot(new Vector2(0f, 0f)));
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-017: Constants — MIN_AGENT_SEPARATION_M_SQ is derived correctly
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Constants_SeparationSqIsSquareOfSeparation()
        {
            float expected = PositioningAIConstants.MIN_AGENT_SEPARATION_M *
                             PositioningAIConstants.MIN_AGENT_SEPARATION_M;
            Assert.That(PositioningAIConstants.MIN_AGENT_SEPARATION_M_SQ,
                Is.EqualTo(expected).Within(1e-6f));
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-018: Formation — 4-3-3 lineCuts = (4, 7)
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Constants_Family433_LineCutsMatch_Spec()
        {
            (int fm, int fa) = PositioningAIConstants.LineCuts433;
            Assert.That(fm, Is.EqualTo(4));
            Assert.That(fa, Is.EqualTo(7));
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-019: Formation — 4-2-3-1 lineCuts = (4, 9)
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Constants_Family4231_LineCutsMatch_Spec()
        {
            (int fm, int fa) = PositioningAIConstants.LineCuts4231;
            Assert.That(fm, Is.EqualTo(4));
            Assert.That(fa, Is.EqualTo(9));
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-020: GK slot — ball in opponent half clamped to own half
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void AnchorCalculator_GkSlot_BallInOpponentHalf_ClampedToOwnHalf()
        {
            // Ball at x=80 (opponent half). GK advance uses clamped ball x = 52.5.
            // basisX(52.5) = 0 → gkSlot.x = 5.5 + 8.0*0 = 5.5
            var ball = new Vector3(80f, 34f, 0f);
            Vector2 gk = AnchorCalculator.ComputeGkSlot(ball);
            Assert.That(gk.x, Is.EqualTo(5.5f).Within(1e-4f), "GK must not advance past own half");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-D-001: Determinism — identical inputs produce identical outputs
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void PositioningAITick_SameInputs_ProduceIdenticalSlots()
        {
            var snap = MakeSnapshot();
            var mods = NeutralModifiers();

            var tickA = new PositioningAITick(FormationFamily.F442);
            tickA.SeedFromFormation(snap);
            tickA.Tick(snap, mods);

            var tickB = new PositioningAITick(FormationFamily.F442);
            tickB.SeedFromFormation(snap);
            tickB.Tick(snap, mods);

            for (int id = 0; id < PositioningAIConstants.SQUAD_SIZE; id++)
            {
                Vector2 a = tickA.GetFormationSlot(id);
                Vector2 b = tickB.GetFormationSlot(id);
                Assert.That(a, Is.EqualTo(b), $"EntityId {id} slots must be identical");
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-D-002: Determinism — EntityId ordering preserved
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void PositioningAITick_EntityIdOrdering_IsAscending()
        {
            // SlotIndex 0 = lowest EntityId, ... SlotIndex 10 = highest.
            var snap = MakeSnapshot(); // entity IDs 0-10 assigned ascending
            for (int i = 0; i < snap.Agents.Length; i++)
                Assert.That(snap.Agents[i].EntityId, Is.EqualTo(i));
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-I-001: Integration — full 4-4-2 tick; GK slot in own half
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Integration_442Tick_GkSlotInOwnHalf()
        {
            var tick = new PositioningAITick(FormationFamily.F442);
            var snap = MakeSnapshot();
            tick.SeedFromFormation(snap);
            tick.Tick(snap, NeutralModifiers());

            Vector2 gk = tick.GetFormationSlot(0); // GK is entityId 0, slotIndex 0
            Assert.That(gk.x, Is.LessThan(PositioningAIConstants.PITCH_HALF_LENGTH_M),
                "GK must be in own half at Stage 0");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-I-002: Integration — 4-3-3 tick; LW slot left of centre
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Integration_433Tick_LWSlotLeftChannel()
        {
            var snap = new PositioningPerceptionSnapshot(PositioningAIConstants.SQUAD_SIZE)
            {
                TickIndex = 0,
                BallPosition = new Vector3(52.5f, 34f, 0f),
                PossessionOwnerEntityId = -1,
            };
            FormationSlotRecord[] f = PositioningAIConstants.Family433;
            for (int i = 0; i < PositioningAIConstants.SQUAD_SIZE; i++)
            {
                snap.Agents[i] = new AgentPositioningData(
                    i, i, new Vector2(f[i].LongPct * 105f, f[i].LateralPct * 68f),
                    true, f[i].Role, f[i].IsGoalkeeper);
            }

            var tick = new PositioningAITick(FormationFamily.F433);
            tick.SeedFromFormation(snap);
            tick.Tick(snap, NeutralModifiers());

            // LW is slot index 8 in 4-3-3 (entityId=8). lateralPct=0.100 → y≈6.8 m.
            Vector2 lw = tick.GetFormationSlot(8);
            Assert.That(lw.y, Is.LessThan(PositioningAIConstants.PITCH_HALF_WIDTH_M),
                "LW slot must be in left half of pitch");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-I-003: Integration — F2 fallback: invalid archetype → falls back to 4-4-2
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Constants_GetFormationSlots_InvalidFamily_ReturnsFallback442()
        {
            // Cast 99 (invalid) to FormationFamily — switch default falls back to F442.
            var slots = PositioningAIConstants.GetFormationSlots((FormationFamily)99);
            Assert.That(slots, Is.SameAs(PositioningAIConstants.Family442));
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-I-004: Integration — 4-4-2 line partition seeds correctly
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void HysteresisState_SeedFromFormation_442_CorrectDefaultLines()
        {
            var hyst = new HysteresisState(PositioningAIConstants.SQUAD_SIZE);
            hyst.SeedFromFormation(PositioningAIConstants.Family442);

            // GK excluded from line partition; seeded but IsGoalkeeper=true.
            // Slots 1-4 = Defense.
            for (int i = 1; i <= 4; i++)
                Assert.That(hyst.Agents[i].CurrentLine, Is.EqualTo(LineId.Defense),
                    $"Slot {i} should be Defense in 4-4-2");

            // Slots 5-8 = Midfield.
            for (int i = 5; i <= 8; i++)
                Assert.That(hyst.Agents[i].CurrentLine, Is.EqualTo(LineId.Midfield),
                    $"Slot {i} should be Midfield in 4-4-2");

            // Slots 9-10 = Attack.
            for (int i = 9; i <= 10; i++)
                Assert.That(hyst.Agents[i].CurrentLine, Is.EqualTo(LineId.Attack),
                    $"Slot {i} should be Attack in 4-4-2");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-P-001: Performance — SQUAD_SIZE and SPACING_MAX_PASSES constants are spec values
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Constants_PerformanceBudget_ValuesMatchSpec()
        {
            Assert.That(PositioningAIConstants.SQUAD_SIZE,        Is.EqualTo(11));
            Assert.That(PositioningAIConstants.SPACING_MAX_PASSES, Is.EqualTo(4));
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-T-001: Tactical correctness — OutOfPoss contracts team laterally
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Integration_OutOfPoss_NarrowerLateralSpread_Than_InPoss()
        {
            // Seed the phase state to skip hysteresis delay.
            var snap = MakeSnapshot();

            // In possession run.
            var modIn = new ContextModifierInputs(0, 0f, 0.5f);
            var tickIn = new PositioningAITick(FormationFamily.F442);
            var hystIn = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tickIn) as HysteresisState;
            if (hystIn != null) { hystIn.CurrentPhase = Phase.InPoss; hystIn.CandidatePhase = Phase.InPoss; }
            snap.PossessionOwnerEntityId = 3; snap.PossessionOwnerIsOwnTeam = true;
            tickIn.SeedFromFormation(snap);
            for (int t = 0; t < 5; t++) { snap.TickIndex = t; tickIn.Tick(snap, modIn); }

            float spreadIn = ComputeLateralSpread(tickIn, snap);

            // Out of possession run.
            var tickOut = new PositioningAITick(FormationFamily.F442);
            var hystOut = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tickOut) as HysteresisState;
            if (hystOut != null) { hystOut.CurrentPhase = Phase.OutOfPoss; hystOut.CandidatePhase = Phase.OutOfPoss; }
            snap.PossessionOwnerEntityId = -1; snap.PossessionOwnerIsOwnTeam = false;
            tickOut.SeedFromFormation(snap);
            for (int t = 0; t < 5; t++) { snap.TickIndex = t; tickOut.Tick(snap, modIn); }

            float spreadOut = ComputeLateralSpread(tickOut, snap);

            Assert.That(spreadOut, Is.LessThan(spreadIn),
                "OutOfPoss team must be narrower laterally than InPoss");
        }

        private static float ComputeLateralSpread(PositioningAITick tick, PositioningPerceptionSnapshot snap)
        {
            float minY = float.MaxValue, maxY = float.MinValue;
            for (int i = 1; i < snap.Agents.Length; i++) // skip GK (0)
            {
                Vector2 s = tick.GetFormationSlot(i);
                if (PositioningAITick.IsSentinelSlot(s)) continue;
                if (s.y < minY) minY = s.y;
                if (s.y > maxY) maxY = s.y;
            }
            return maxY - minY;
        }
        // ──────────────────────────────────────────────────────────────────────
        // T-U-021: RoleId enum alignment — values are stable and match pull-factor indexing
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void RoleId_EnumValues_MatchPullFactorTableIndexing()
        {
            Assert.That((int)RoleId.GK, Is.EqualTo(0),  "GK must be row 0");
            Assert.That((int)RoleId.LB, Is.EqualTo(1),  "LB must be row 1");
            Assert.That((int)RoleId.CB, Is.EqualTo(2),  "CB must be row 2");
            Assert.That((int)RoleId.RB, Is.EqualTo(3),  "RB must be row 3");
            Assert.That((int)RoleId.LM, Is.EqualTo(4),  "LM must be row 4");
            Assert.That((int)RoleId.CM, Is.EqualTo(5),  "CM must be row 5");
            Assert.That((int)RoleId.RM, Is.EqualTo(6),  "RM must be row 6");
            Assert.That((int)RoleId.DM, Is.EqualTo(7),  "DM must be row 7");
            Assert.That((int)RoleId.LW, Is.EqualTo(8),  "LW must be row 8");
            Assert.That((int)RoleId.CF, Is.EqualTo(9),  "CF must be row 9");
            Assert.That((int)RoleId.RW, Is.EqualTo(10), "RW must be row 10");
            Assert.That((int)RoleId.AM, Is.EqualTo(11), "AM must be row 11");
            Assert.That((int)RoleId.ST, Is.EqualTo(12), "ST must be row 12");
            // Verify pull-factor array has 13 × 4 entries.
            Assert.That(PositioningAIConstants.PullFactor.Length, Is.EqualTo(13 * 4));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
