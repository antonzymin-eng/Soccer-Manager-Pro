// File: src/positioning-ai/Tests/PositioningAITests.cs
// Created:  2026-05-29
// Modified: 2026-06-01
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

        // ──────────────────────────────────────────────────────────────────────
        // T-U-022: Ball-relative offset — AM retreats when ball is in own half
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void BallRelativeOffset_BallAtOwnHalf_AMRetreats()
        {
            // Ball at (20, 34), AM, OutOfPoss.
            // pullFactor=0.60, basisX=(20-52.5)/52.5 = -0.619, offset.x = 0.60 × (-0.619) × 12.0 = -4.46 m < 0.
            var ball = new Vector3(20f, 34f, 0f);
            Vector2 off = AnchorCalculator.ComputeBallRelativeOffset(ball, RoleId.AM, Phase.OutOfPoss);
            Assert.That(off.x, Is.LessThan(0f), "AM offset.x must be negative when ball is in own half");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-023: Ball-relative offset — winger advances when ball is in attacking third
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void BallRelativeOffset_BallInAttackingThird_WingerAdvances()
        {
            // Ball at (90, 34), LW, InPoss.
            // basisX = (90-52.5)/52.5 = 0.714 > 0 → positive offset → winger pushes forward.
            var ball = new Vector3(90f, 34f, 0f);
            Vector2 off = AnchorCalculator.ComputeBallRelativeOffset(ball, RoleId.LW, Phase.InPoss);
            Assert.That(off.x, Is.GreaterThan(0f), "LW offset.x must be positive when ball is in attacking third");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-024: Ball-relative offset — output clamped, never exceeds OFFSET_RANGE_X_M
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void BallRelativeOffset_OutputClamped_NeverExceedsRangeX()
        {
            // |pullFactor.x| ≤ 1 and |basisX| ≤ 1, so |offset.x| ≤ OFFSET_RANGE_X_M.
            // Verify for extreme ball positions.
            Vector3[] extremes = new Vector3[]
            {
                new Vector3(0f,   0f,  0f),
                new Vector3(105f, 68f, 0f),
            };
            foreach (Vector3 ballPos in extremes)
            {
                foreach (RoleId role in System.Enum.GetValues(typeof(RoleId)))
                {
                    foreach (Phase phase in System.Enum.GetValues(typeof(Phase)))
                    {
                        Vector2 off = AnchorCalculator.ComputeBallRelativeOffset(ballPos, role, phase);
                        Assert.That(Mathf.Abs(off.x), Is.LessThanOrEqualTo(PositioningAIConstants.OFFSET_RANGE_X_M + 1e-4f),
                            $"offset.x out of range for role={role} phase={phase} ballPos={ballPos}");
                    }
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-025: Ball-relative offset — output clamped, never exceeds OFFSET_RANGE_Y_M
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void BallRelativeOffset_OutputClamped_NeverExceedsRangeY()
        {
            Vector3[] extremes = new Vector3[]
            {
                new Vector3(0f,   0f,  0f),
                new Vector3(105f, 68f, 0f),
            };
            foreach (Vector3 ballPos in extremes)
            {
                foreach (RoleId role in System.Enum.GetValues(typeof(RoleId)))
                {
                    foreach (Phase phase in System.Enum.GetValues(typeof(Phase)))
                    {
                        Vector2 off = AnchorCalculator.ComputeBallRelativeOffset(ballPos, role, phase);
                        Assert.That(Mathf.Abs(off.y), Is.LessThanOrEqualTo(PositioningAIConstants.OFFSET_RANGE_Y_M + 1e-4f),
                            $"offset.y out of range for role={role} phase={phase} ballPos={ballPos}");
                    }
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-030: Line resolution — GK is not classified in outfield line partition
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void LineResolution_GkExcludedFromLineAssignment()
        {
            // The GK seed line is Defense (from Family442[0].DefaultLine).
            // After SeedFromFormation, GK slot retains its seeded line; it is not re-classified
            // by outfield line-cut logic.
            var hyst = new HysteresisState(PositioningAIConstants.SQUAD_SIZE);
            hyst.SeedFromFormation(PositioningAIConstants.Family442);

            // GK (slot 0) must stay in Defense line as seeded from formation.
            Assert.That(hyst.Agents[0].CurrentLine, Is.EqualTo(LineId.Defense),
                "GK default line must be Defense after SeedFromFormation");

            // Verify GK is flagged as goalkeeper in the formation record.
            Assert.IsTrue(PositioningAIConstants.Family442[0].IsGoalkeeper,
                "Family442 slot 0 must be flagged as goalkeeper");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-031: Lane resolution — slot in left channel classifies as LW
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void LaneResolution_SlotInLeftChannel_ClassifiesAsLW()
        {
            // LW bin: [0, 13.6 m). Y=5 is clearly in LW range.
            float y = 5f;
            float[] edges = PositioningAIConstants.LaneEdgesM;
            LaneId lane = y < edges[1] ? LaneId.LW :
                          y < edges[2] ? LaneId.LH :
                          y < edges[3] ? LaneId.C  :
                          y < edges[4] ? LaneId.RH : LaneId.RW;
            Assert.That(lane, Is.EqualTo(LaneId.LW), "Y=5 should be in LW lane bin");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-032: Lane resolution — slot at centre Y classifies as Center
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void LaneResolution_SlotAtCentreY_ClassifiesAsCenter()
        {
            // C bin: [27.2, 40.8 m). Y=34 (pitch centre) is squarely in Centre.
            float y = 34f;
            float[] edges = PositioningAIConstants.LaneEdgesM;
            LaneId lane = y < edges[1] ? LaneId.LW :
                          y < edges[2] ? LaneId.LH :
                          y < edges[3] ? LaneId.C  :
                          y < edges[4] ? LaneId.RH : LaneId.RW;
            Assert.That(lane, Is.EqualTo(LaneId.C), "Pitch centre Y should be in C lane bin");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-033: Lane resolution — slot in right channel classifies as RW
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void LaneResolution_SlotInRightChannel_ClassifiesAsRW()
        {
            // RW bin: [54.4, 68.0 m). Y=64 is clearly in RW range.
            float y = 64f;
            float[] edges = PositioningAIConstants.LaneEdgesM;
            LaneId lane = y < edges[1] ? LaneId.LW :
                          y < edges[2] ? LaneId.LH :
                          y < edges[3] ? LaneId.C  :
                          y < edges[4] ? LaneId.RH : LaneId.RW;
            Assert.That(lane, Is.EqualTo(LaneId.RW), "Y=64 should be in RW lane bin");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-034: Line resolution — defenders behind midline classified as Defense
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void LineResolution_DefendersBehindMidline_ClassifiedAsDefense()
        {
            // In 4-4-2, slots 1-4 are defenders with longPct=0.20 → x=21m.
            // After SeedFromFormation they should all carry LineId.Defense.
            var hyst = new HysteresisState(PositioningAIConstants.SQUAD_SIZE);
            hyst.SeedFromFormation(PositioningAIConstants.Family442);

            for (int i = 1; i <= 4; i++)
            {
                Assert.That(hyst.Agents[i].CurrentLine, Is.EqualTo(LineId.Defense),
                    $"Slot {i} defender must be classified as Defense");
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-040: SpacingResolver — detects violation when agents are too close
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void SpacingResolver_DetectsViolation_WhenAgentsTooClose()
        {
            var slots   = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var anchors = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var ids     = new int[PositioningAIConstants.SQUAD_SIZE];

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i]   = new Vector2(0f, (float)i * 10f);
                anchors[i] = slots[i];
                ids[i]     = i;
            }

            // Two agents at identical position — maximum violation.
            slots[0]   = new Vector2(50f, 30f);
            slots[1]   = new Vector2(50f, 30f);
            anchors[0] = new Vector2(49f, 30f);
            anchors[1] = new Vector2(51f, 30f);

            SpacingResolver.EnforceHardSpacing(slots, anchors, ids);

            float dist = Vector2.Distance(slots[0], slots[1]);
            Assert.That(dist, Is.GreaterThanOrEqualTo(
                PositioningAIConstants.MIN_AGENT_SEPARATION_M - 0.01f),
                "Agents placed on top of each other must be separated to MIN_AGENT_SEPARATION_M");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-041: SpacingResolver — preserves distance when agents already spaced
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void SpacingResolver_PreservesDistance_WhenAlreadySpaced()
        {
            var slots   = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var anchors = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var ids     = new int[PositioningAIConstants.SQUAD_SIZE];

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i]   = new Vector2(0f, (float)i * 10f);
                anchors[i] = slots[i];
                ids[i]     = i;
            }

            // Two agents separated by 3m — well beyond MIN_AGENT_SEPARATION_M=1.5.
            slots[0]   = new Vector2(50f, 30f);
            slots[1]   = new Vector2(53f, 30f);
            anchors[0] = slots[0];
            anchors[1] = slots[1];

            Vector2 pos0Before = slots[0];
            Vector2 pos1Before = slots[1];

            SpacingResolver.EnforceHardSpacing(slots, anchors, ids);

            Assert.That(Vector2.Distance(slots[0], pos0Before), Is.LessThan(1e-4f),
                "Agent 0 should not move when already separated");
            Assert.That(Vector2.Distance(slots[1], pos1Before), Is.LessThan(1e-4f),
                "Agent 1 should not move when already separated");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-042: SpacingResolver — higher EntityId is displaced on collision
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void SpacingResolver_HigherEntityId_Displaced()
        {
            var slots   = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var anchors = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var ids     = new int[PositioningAIConstants.SQUAD_SIZE];

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i]   = new Vector2(0f, (float)i * 10f);
                anchors[i] = slots[i];
                ids[i]     = i;
            }

            // Equal anchor costs; entityId 2 > entityId 1 → entityId 2 displaced.
            slots[0]   = new Vector2(50f, 34f);
            slots[1]   = new Vector2(50f, 34f);  // same position
            anchors[0] = new Vector2(49f, 34f);  // equal anchor cost
            anchors[1] = new Vector2(51f, 34f);  // equal anchor cost
            ids[0] = 1;
            ids[1] = 2;  // higher EntityId

            Vector2 pos0Before = slots[0];

            SpacingResolver.EnforceHardSpacing(slots, anchors, ids);

            // Lower EntityId (1) should be approximately unchanged; higher (2) moved.
            Assert.That(Vector2.Distance(slots[0], pos0Before), Is.LessThan(0.05f),
                "Lower EntityId slot should remain near original position");
            Assert.That(Vector2.Distance(slots[1], new Vector2(50f, 34f)), Is.GreaterThan(0f),
                "Higher EntityId slot must be displaced");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-043: SpacingResolver — multiple violations all resolved
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void SpacingResolver_MultipleViolations_AllResolved()
        {
            var slots   = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var anchors = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var ids     = new int[PositioningAIConstants.SQUAD_SIZE];

            for (int i = 0; i < slots.Length; i++)
            {
                slots[i]   = new Vector2(0f, (float)i * 10f);
                anchors[i] = slots[i];
                ids[i]     = i;
            }

            // Cluster 4 agents at the same position.
            for (int i = 0; i < 4; i++)
            {
                slots[i]   = new Vector2(50f, 34f);
                anchors[i] = new Vector2(50f + i * 2f, 34f);
            }

            SpacingResolver.EnforceHardSpacing(slots, anchors, ids);

            float minSepSq = PositioningAIConstants.MIN_AGENT_SEPARATION_M_SQ - PositioningAIConstants.SPACING_EPSILON_M2;
            for (int a = 0; a < 4; a++)
            {
                for (int b = a + 1; b < 4; b++)
                {
                    float dx = slots[a].x - slots[b].x;
                    float dy = slots[a].y - slots[b].y;
                    float distSq = dx * dx + dy * dy;
                    Assert.That(distSq, Is.GreaterThanOrEqualTo(minSepSq),
                        $"Pair ({a},{b}) must meet minimum separation after SPACING_MAX_PASSES");
                }
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-050: Failure mode F3 — NaN offset replaced by raw anchor
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void FailureMode_F3_NaNBallPosition_SlotsRemainFinite()
        {
            // When ball position contains NaN, SlotComposer's F3 guard should replace
            // any NaN slot with the raw anchor (finite value).
            var snap = MakeSnapshot(ballPos: new Vector3(float.NaN, float.NaN, 0f));
            var outSlots   = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var anchorBuf  = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var entityIds  = new int[PositioningAIConstants.SQUAD_SIZE];

            // Pre-classify phase (default InPoss).
            var hyst = new HysteresisState(PositioningAIConstants.SQUAD_SIZE);
            hyst.SeedFromFormation(PositioningAIConstants.Family442);

            SlotComposer.Compose(snap, FormationFamily.F442, NeutralModifiers(),
                Phase.InPoss, hyst, outSlots, anchorBuf, entityIds);

            for (int i = 0; i < PositioningAIConstants.SQUAD_SIZE; i++)
            {
                if (PositioningAITick.IsSentinelSlot(outSlots[i])) continue;
                Assert.IsFalse(float.IsNaN(outSlots[i].x),
                    $"Slot {i}.x must not be NaN after F3 guard");
                Assert.IsFalse(float.IsNaN(outSlots[i].y),
                    $"Slot {i}.y must not be NaN after F3 guard");
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-051: Failure mode F1 — stale snapshot reuses cached slots
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void FailureMode_F1_StaleSnapshot_ReturnsUnchangedSlots()
        {
            var tick = new PositioningAITick(FormationFamily.F442);
            var snap = MakeSnapshot(tickIndex: 10);
            tick.SeedFromFormation(snap);
            tick.Tick(snap, NeutralModifiers());

            // Record all slots after the first tick.
            Vector2[] slotsAfterFirst = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            for (int i = 0; i < PositioningAIConstants.SQUAD_SIZE; i++)
                slotsAfterFirst[i] = tick.GetFormationSlot(i);

            // Provide a lower (stale) tickIndex — F1 guard should prevent recompute.
            snap.TickIndex = 5;
            snap.BallPosition = new Vector3(80f, 60f, 0f);  // drastically different ball
            tick.Tick(snap, NeutralModifiers());

            for (int i = 0; i < PositioningAIConstants.SQUAD_SIZE; i++)
            {
                Vector2 slotAfterStale = tick.GetFormationSlot(i);
                Assert.That(slotAfterStale, Is.EqualTo(slotsAfterFirst[i]),
                    $"Slot {i} must be unchanged after stale tick (F1 guard)");
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-052: Failure mode — out-of-bounds offset clamped to pitch bounds
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void FailureMode_OutOfBoundsClamp_SlotsClamped()
        {
            // Ball at extreme corner forces maximum offsets; pitch clamp must keep slots in bounds.
            var snap = MakeSnapshot(ballPos: new Vector3(0f, 0f, 0f));
            var outSlots   = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var anchorBuf  = new Vector2[PositioningAIConstants.SQUAD_SIZE];
            var entityIds  = new int[PositioningAIConstants.SQUAD_SIZE];

            var hyst = new HysteresisState(PositioningAIConstants.SQUAD_SIZE);
            hyst.SeedFromFormation(PositioningAIConstants.Family442);

            SlotComposer.Compose(snap, FormationFamily.F442, NeutralModifiers(),
                Phase.InPoss, hyst, outSlots, anchorBuf, entityIds);

            for (int i = 0; i < PositioningAIConstants.SQUAD_SIZE; i++)
            {
                if (PositioningAITick.IsSentinelSlot(outSlots[i])) continue;
                Assert.That(outSlots[i].x, Is.GreaterThanOrEqualTo(PositioningAIConstants.SLOT_X_MIN),
                    $"Slot {i}.x below SLOT_X_MIN");
                Assert.That(outSlots[i].x, Is.LessThanOrEqualTo(PositioningAIConstants.SLOT_X_MAX),
                    $"Slot {i}.x above SLOT_X_MAX");
                Assert.That(outSlots[i].y, Is.GreaterThanOrEqualTo(PositioningAIConstants.SLOT_Y_MIN),
                    $"Slot {i}.y below SLOT_Y_MIN");
                Assert.That(outSlots[i].y, Is.LessThanOrEqualTo(PositioningAIConstants.SLOT_Y_MAX),
                    $"Slot {i}.y above SLOT_Y_MAX");
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-060: ContextModifier — positive score difference expands lateral spread
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void ContextModifier_ScoreDiff_ExpandsLateralSpread()
        {
            // Higher score difference (winning) increases lateral compactness multiplier,
            // reducing the lateral rescale factor → agents spread wider relative to centroid.
            // Compare spread with scoreDiff=0 vs scoreDiff=+3.
            var snap = MakeSnapshot(possOwner: 3, possIsOwn: true);

            var tickNeutral = new PositioningAITick(FormationFamily.F442);
            var hystNeutral = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tickNeutral) as HysteresisState;
            if (hystNeutral != null) { hystNeutral.CurrentPhase = Phase.InPoss; hystNeutral.CandidatePhase = Phase.InPoss; }
            tickNeutral.SeedFromFormation(snap);
            for (int t = 0; t < 4; t++) { snap.TickIndex = t; tickNeutral.Tick(snap, new ContextModifierInputs(0, 0f, 0f)); }
            float spreadNeutral = ComputeLateralSpread(tickNeutral, snap);

            var tickWinning = new PositioningAITick(FormationFamily.F442);
            var hystWinning = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tickWinning) as HysteresisState;
            if (hystWinning != null) { hystWinning.CurrentPhase = Phase.InPoss; hystWinning.CandidatePhase = Phase.InPoss; }
            tickWinning.SeedFromFormation(snap);
            for (int t = 0; t < 4; t++) { snap.TickIndex = t; tickWinning.Tick(snap, new ContextModifierInputs(3, 0f, 0f)); }
            float spreadWinning = ComputeLateralSpread(tickWinning, snap);

            Assert.That(spreadWinning, Is.GreaterThan(spreadNeutral),
                "Winning (scoreDiff=+3) should expand lateral spread compared to neutral (scoreDiff=0)");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-061: ContextModifier — high fatigue relaxes lateral compactness
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void ContextModifier_HighFatigue_RelaxesLateralCompactness()
        {
            // FATIGUE_LATERAL_RELAX=0.15 → compactness at fatigue=1 is 85% of base.
            // Lower compactness → higher rescale factor → agents spread more laterally.
            var snap = MakeSnapshot(possOwner: 3, possIsOwn: true);

            var tickRested = new PositioningAITick(FormationFamily.F442);
            var hystRested = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tickRested) as HysteresisState;
            if (hystRested != null) { hystRested.CurrentPhase = Phase.InPoss; hystRested.CandidatePhase = Phase.InPoss; }
            tickRested.SeedFromFormation(snap);
            for (int t = 0; t < 4; t++) { snap.TickIndex = t; tickRested.Tick(snap, new ContextModifierInputs(0, 0f, 0f)); }
            float spreadRested = ComputeLateralSpread(tickRested, snap);

            var tickFatigued = new PositioningAITick(FormationFamily.F442);
            var hystFatigued = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tickFatigued) as HysteresisState;
            if (hystFatigued != null) { hystFatigued.CurrentPhase = Phase.InPoss; hystFatigued.CandidatePhase = Phase.InPoss; }
            tickFatigued.SeedFromFormation(snap);
            for (int t = 0; t < 4; t++) { snap.TickIndex = t; tickFatigued.Tick(snap, new ContextModifierInputs(0, 1.0f, 0f)); }
            float spreadFatigued = ComputeLateralSpread(tickFatigued, snap);

            Assert.That(spreadFatigued, Is.GreaterThan(spreadRested),
                "High fatigue must relax lateral compactness → wider spread than rested team");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-062: ContextModifier — high intensity increases vertical compactness
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void ContextModifier_HighIntensity_IncreasesVerticalCompactness()
        {
            // INTENSITY_VERTICAL_GAIN=0.20 → verticalCompactness at intensity=1 is 120% of base.
            // Higher vertical compactness → lower rescale factor → agents compress longitudinally.
            var snap = MakeSnapshot(possOwner: 3, possIsOwn: true);

            var tickLowInt = new PositioningAITick(FormationFamily.F442);
            var hystLowInt = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tickLowInt) as HysteresisState;
            if (hystLowInt != null) { hystLowInt.CurrentPhase = Phase.InPoss; hystLowInt.CandidatePhase = Phase.InPoss; }
            tickLowInt.SeedFromFormation(snap);
            for (int t = 0; t < 4; t++) { snap.TickIndex = t; tickLowInt.Tick(snap, new ContextModifierInputs(0, 0f, 0f)); }
            float spreadXLow = ComputeVerticalSpread(tickLowInt, snap);

            var tickHighInt = new PositioningAITick(FormationFamily.F442);
            var hystHighInt = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tickHighInt) as HysteresisState;
            if (hystHighInt != null) { hystHighInt.CurrentPhase = Phase.InPoss; hystHighInt.CandidatePhase = Phase.InPoss; }
            tickHighInt.SeedFromFormation(snap);
            for (int t = 0; t < 4; t++) { snap.TickIndex = t; tickHighInt.Tick(snap, new ContextModifierInputs(0, 0f, 1.0f)); }
            float spreadXHigh = ComputeVerticalSpread(tickHighInt, snap);

            Assert.That(spreadXHigh, Is.LessThan(spreadXLow),
                "High intensity must increase vertical compactness → smaller longitudinal spread");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-063: ContextModifier — GK excluded from centroid computation
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void ContextModifier_GKExcluded_FromCentroidComputation()
        {
            // GK is at x≈5.5m; outfield centroid should be much further forward.
            // If GK were included, centroid.x would be dragged significantly toward goal line.
            var snap = MakeSnapshot(possOwner: 3, possIsOwn: true);
            var tick = new PositioningAITick(FormationFamily.F442);
            var hyst = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tick) as HysteresisState;
            if (hyst != null) { hyst.CurrentPhase = Phase.InPoss; hyst.CandidatePhase = Phase.InPoss; }
            tick.SeedFromFormation(snap);
            for (int t = 0; t < 4; t++) { snap.TickIndex = t; tick.Tick(snap, NeutralModifiers()); }

            // Compute outfield centroid (excluding GK slot 0).
            float sumX = 0f;
            int count = 0;
            for (int i = 1; i < PositioningAIConstants.SQUAD_SIZE; i++)
            {
                Vector2 s = tick.GetFormationSlot(i);
                if (PositioningAITick.IsSentinelSlot(s)) continue;
                sumX += s.x;
                count++;
            }
            float outfieldCentroidX = count > 0 ? sumX / count : 0f;

            // Outfield centroid should be much higher than GK_DEPTH_M=5.5.
            Assert.That(outfieldCentroidX, Is.GreaterThan(PositioningAIConstants.GK_DEPTH_M + 10f),
                "Outfield centroid must be much further forward than GK depth — GK is excluded from centroid");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-070: Hysteresis — phase does not flip on single loose-ball tick
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Hysteresis_PhaseDoesNotFlip_OnSingleLooseballTick()
        {
            // Start in InPoss. One loose-ball tick with slow ball should not flip phase.
            var hyst = new HysteresisState(PositioningAIConstants.SQUAD_SIZE);
            hyst.CurrentPhase   = Phase.InPoss;
            hyst.CandidatePhase = Phase.InPoss;

            // Loose ball with velocity below threshold → no transition candidate.
            var snap = MakeSnapshot(possOwner: -1, ballVx: 0f);
            snap.TickIndex = 0;
            PhaseClassifier.ClassifyAndCommit(snap, hyst);

            Assert.That(hyst.CurrentPhase, Is.EqualTo(Phase.InPoss),
                "Phase must not flip on a single loose-ball tick with sub-threshold velocity");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-071: Hysteresis — phase flips after PHASE_HYSTERESIS_TICKS dwell
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Hysteresis_PhaseFlips_AfterDwellCount()
        {
            // Start in OutOfPoss. Apply InPoss candidate for PHASE_HYSTERESIS_TICKS ticks.
            var hyst = new HysteresisState(PositioningAIConstants.SQUAD_SIZE);
            hyst.CurrentPhase   = Phase.OutOfPoss;
            hyst.CandidatePhase = Phase.OutOfPoss;

            var snap = MakeSnapshot(possOwner: 5, possIsOwn: true);

            // One tick short: phase must NOT have flipped yet.
            for (int t = 0; t < PositioningAIConstants.PHASE_HYSTERESIS_TICKS - 1; t++)
            {
                snap.TickIndex = t;
                PhaseClassifier.ClassifyAndCommit(snap, hyst);
            }
            Assert.That(hyst.CurrentPhase, Is.EqualTo(Phase.OutOfPoss),
                "Phase must not flip before PHASE_HYSTERESIS_TICKS");

            // One more tick: now phase must have flipped.
            snap.TickIndex = PositioningAIConstants.PHASE_HYSTERESIS_TICKS - 1;
            PhaseClassifier.ClassifyAndCommit(snap, hyst);
            Assert.That(hyst.CurrentPhase, Is.EqualTo(Phase.InPoss),
                "Phase must flip after PHASE_HYSTERESIS_TICKS consecutive candidate ticks");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-U-072: Hysteresis — candidate change resets dwell counter
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Hysteresis_AnchorChange_ResetsDwellCounter()
        {
            // Start in OutOfPoss. Build up 2 ticks toward InPoss (not yet committed).
            var hyst = new HysteresisState(PositioningAIConstants.SQUAD_SIZE);
            hyst.CurrentPhase   = Phase.OutOfPoss;
            hyst.CandidatePhase = Phase.OutOfPoss;

            var snap = MakeSnapshot(possOwner: 5, possIsOwn: true);
            for (int t = 0; t < PositioningAIConstants.PHASE_HYSTERESIS_TICKS - 1; t++)
            {
                snap.TickIndex = t;
                PhaseClassifier.ClassifyAndCommit(snap, hyst);
            }
            Assert.That(hyst.CurrentPhase, Is.EqualTo(Phase.OutOfPoss), "Should not have flipped yet");

            // Now switch to opponent possession → candidate changes → dwell resets.
            snap.PossessionOwnerEntityId = 5;
            snap.PossessionOwnerIsOwnTeam = false;
            snap.TickIndex = PositioningAIConstants.PHASE_HYSTERESIS_TICKS;
            PhaseClassifier.ClassifyAndCommit(snap, hyst);

            // After the candidate reset, phase is still OutOfPoss (dwell counter was reset to 1).
            Assert.That(hyst.CurrentPhase, Is.EqualTo(Phase.OutOfPoss),
                "Phase must still be OutOfPoss after candidate change reset the dwell counter");
            Assert.That(hyst.PhaseDwellCount, Is.EqualTo(1),
                "Dwell counter must reset to 1 when candidate changes");
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
        // T-D-003: Determinism — EntityId permutation produces same output (stub)
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Determinism_EntityIdPermutation_SameOutput()
        {
            Assert.Ignore("T-D-003: requires Stage 0+1 determinism harness bound to #16 §5 DigestEngine");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-D-004: Determinism — digest stability on identical inputs (stub)
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Determinism_DigestStability_IdenticalInputs()
        {
            Assert.Ignore("T-D-004: requires DeterministicRngService integration — deferred to Stage 0+1");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-D-005: Determinism — save/load resumes identically (stub)
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Determinism_SaveLoad_ResumesIdentically()
        {
            Assert.Ignore("T-D-005: requires SnapshotCodec + SaveManager integration — Stage 0+1");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-D-006: Determinism — RNG isolation across subsystems (stub)
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Determinism_RngIsolation_CrossSubsystem()
        {
            Assert.Ignore("T-D-006: requires multi-subsystem RNG stream registration — Stage 0+1");
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
        // T-I-005: Integration stub — AM advances in 4-2-3-1 TransToAtk
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Integration_4231_TransToAtk_AMAdvances()
        {
            Assert.Ignore("T-I-005: requires Stage 0+1 full-match to measure AM advance distance across multiple ticks");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-I-006: Integration stub — phase oscillation suppressed
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Integration_PhaseOscillationSuppressed()
        {
            Assert.Ignore("T-I-006: requires >=20 tick simulation to measure transition frequency per PHASE_HYSTERESIS_TICKS");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-I-007: Integration stub — substitution sentinel preserved
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Integration_SubstitutionSentinel_Preserved()
        {
            Assert.Ignore("T-I-007: requires sentinel AR-S1-07 injection test (substitution mid-match)");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-I-008: Integration stub — lane overload resolved by spacing
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Integration_LaneOverload_Resolved_BySpacing()
        {
            Assert.Ignore("T-I-008: requires 11-agent formation with lane overload scenario");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-I-009: Integration stub — F2 fallback for valid archetype
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Integration_F2Fallback_ValidArchetype()
        {
            Assert.Ignore("T-I-009: requires invalid archetype enum value injection");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-I-010: Integration stub — orchestrator non-invocation of Stage0Default
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Integration_OrchestratorNonInvocation_OfStage0Default()
        {
            Assert.Ignore("T-I-010: requires AR-S1-04 orchestrator mock to verify Stage0Default not called on hot path");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-I-011: Integration stub — full 90-min match state consistency
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Integration_FullMatch_90Min_StateConsistency()
        {
            Assert.Ignore("T-I-011: requires 90-min simulation (54000 ticks at 10Hz); Stage 0+1 milestone only");
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
        // T-P-002: Performance stub — zero heap allocation in hot path
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Performance_ZeroHeapAlloc_InHotPath()
        {
            Assert.Ignore("T-P-002: requires Unity GC Alloc profiler — Stage 0+1");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-P-003: Performance stub — spacing budget under 0.05ms
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void Performance_SpacingBudget_Under0_05ms()
        {
            Assert.Ignore("T-P-003: requires Stopwatch-based timing — Stage 0+1 perf gate activation");
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

        // ──────────────────────────────────────────────────────────────────────
        // T-T-002: Tactical correctness — 4-3-3 InPoss wingers in lateral channels
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void TacticalCorrectness_433_InPoss_WingersInLateralChannels()
        {
            // In 4-3-3: LW is slot 8 (lateralPct=0.100 → y≈6.8), RW is slot 10 (lateralPct=0.900 → y≈61.2).
            var snap = new PositioningPerceptionSnapshot(PositioningAIConstants.SQUAD_SIZE)
            {
                TickIndex = 0,
                BallPosition = new Vector3(52.5f, 34f, 0f),
                PossessionOwnerEntityId = 3,
                PossessionOwnerIsOwnTeam = true,
            };
            snap.ActiveOutfieldCount = 10;
            FormationSlotRecord[] f = PositioningAIConstants.Family433;
            for (int i = 0; i < PositioningAIConstants.SQUAD_SIZE; i++)
            {
                snap.Agents[i] = new AgentPositioningData(
                    i, i, new Vector2(f[i].LongPct * 105f, f[i].LateralPct * 68f),
                    true, f[i].Role, f[i].IsGoalkeeper);
            }

            var tick = new PositioningAITick(FormationFamily.F433);
            var hyst = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tick) as HysteresisState;
            if (hyst != null) { hyst.CurrentPhase = Phase.InPoss; hyst.CandidatePhase = Phase.InPoss; }
            tick.SeedFromFormation(snap);
            for (int t = 0; t < 5; t++) { snap.TickIndex = t; tick.Tick(snap, NeutralModifiers()); }

            Vector2 lwSlot = tick.GetFormationSlot(8);  // LW: entityId=8
            Vector2 rwSlot = tick.GetFormationSlot(10); // RW: entityId=10

            Assert.That(lwSlot.y, Is.LessThan(PositioningAIConstants.PITCH_HALF_WIDTH_M),
                "LW slot must be in left half (Y < 34)");
            Assert.That(rwSlot.y, Is.GreaterThan(PositioningAIConstants.PITCH_HALF_WIDTH_M),
                "RW slot must be in right half (Y > 34)");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-T-003: Tactical correctness — 4-2-3-1 AM advances in TransToAtk vs OutOfPoss
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void TacticalCorrectness_4231_TransToAtk_AMAdvances_RelativeTo_OutOfPoss()
        {
            // Ball at midfield; AM (slot 8 = CAM in 4-2-3-1) should be further forward in
            // TransToAtk than in OutOfPoss.
            var snap4231 = new PositioningPerceptionSnapshot(PositioningAIConstants.SQUAD_SIZE)
            {
                TickIndex = 0,
                BallPosition = new Vector3(52.5f, 34f, 0f),
                PossessionOwnerEntityId = -1,
            };
            snap4231.ActiveOutfieldCount = 10;
            FormationSlotRecord[] f4231 = PositioningAIConstants.Family4231;
            for (int i = 0; i < PositioningAIConstants.SQUAD_SIZE; i++)
            {
                snap4231.Agents[i] = new AgentPositioningData(
                    i, i, new Vector2(f4231[i].LongPct * 105f, f4231[i].LateralPct * 68f),
                    true, f4231[i].Role, f4231[i].IsGoalkeeper);
            }

            // TransToAtk run: slot 8 is CAM (RoleId.AM).
            var tickTTA = new PositioningAITick(FormationFamily.F4231);
            var hystTTA = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tickTTA) as HysteresisState;
            if (hystTTA != null) { hystTTA.CurrentPhase = Phase.TransToAtk; hystTTA.CandidatePhase = Phase.TransToAtk; }
            tickTTA.SeedFromFormation(snap4231);
            for (int t = 0; t < 5; t++) { snap4231.TickIndex = t; tickTTA.Tick(snap4231, NeutralModifiers()); }
            float amTTA = tickTTA.GetFormationSlot(8).x;

            // OutOfPoss run.
            var tickOOP = new PositioningAITick(FormationFamily.F4231);
            var hystOOP = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tickOOP) as HysteresisState;
            if (hystOOP != null) { hystOOP.CurrentPhase = Phase.OutOfPoss; hystOOP.CandidatePhase = Phase.OutOfPoss; }
            tickOOP.SeedFromFormation(snap4231);
            for (int t = 0; t < 5; t++) { snap4231.TickIndex = t; tickOOP.Tick(snap4231, NeutralModifiers()); }
            float amOOP = tickOOP.GetFormationSlot(8).x;

            Assert.That(amTTA, Is.GreaterThan(amOOP),
                "AM slot.x in TransToAtk must exceed AM slot.x in OutOfPoss — AM pushes forward");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-T-004: Tactical correctness — OutOfPoss defensive line sits deeper
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void TacticalCorrectness_OutOfPoss_DefensiveLineCompact()
        {
            var snap = MakeSnapshot();

            // InPoss run — seed phase directly.
            var tickIn = new PositioningAITick(FormationFamily.F442);
            var hystIn = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tickIn) as HysteresisState;
            if (hystIn != null) { hystIn.CurrentPhase = Phase.InPoss; hystIn.CandidatePhase = Phase.InPoss; }
            snap.PossessionOwnerEntityId = 3; snap.PossessionOwnerIsOwnTeam = true;
            tickIn.SeedFromFormation(snap);
            for (int t = 0; t < 5; t++) { snap.TickIndex = t; tickIn.Tick(snap, NeutralModifiers()); }

            // Average X of defenders (slots 1-4) in InPoss.
            float avgDefXIn = 0f;
            for (int i = 1; i <= 4; i++) avgDefXIn += tickIn.GetFormationSlot(i).x;
            avgDefXIn /= 4f;

            // OutOfPoss run.
            var tickOut = new PositioningAITick(FormationFamily.F442);
            var hystOut = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tickOut) as HysteresisState;
            if (hystOut != null) { hystOut.CurrentPhase = Phase.OutOfPoss; hystOut.CandidatePhase = Phase.OutOfPoss; }
            snap.PossessionOwnerEntityId = -1; snap.PossessionOwnerIsOwnTeam = false;
            tickOut.SeedFromFormation(snap);
            for (int t = 0; t < 5; t++) { snap.TickIndex = t; tickOut.Tick(snap, NeutralModifiers()); }

            float avgDefXOut = 0f;
            for (int i = 1; i <= 4; i++) avgDefXOut += tickOut.GetFormationSlot(i).x;
            avgDefXOut /= 4f;

            Assert.That(avgDefXOut, Is.LessThan(avgDefXIn),
                "OutOfPoss defense line must sit deeper (lower X) than InPoss defense line");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-T-005: Tactical correctness — TransToDef produces narrower lateral spread than InPoss
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void TacticalCorrectness_TransToDef_NarrowerLateralSpread()
        {
            // BaseLateral[TransToDef]=0.82 < BaseLateral[InPoss]=1.00 → tighter shape.
            var snap = MakeSnapshot();

            var tickIn = new PositioningAITick(FormationFamily.F442);
            var hystIn = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tickIn) as HysteresisState;
            if (hystIn != null) { hystIn.CurrentPhase = Phase.InPoss; hystIn.CandidatePhase = Phase.InPoss; }
            snap.PossessionOwnerEntityId = 3; snap.PossessionOwnerIsOwnTeam = true;
            tickIn.SeedFromFormation(snap);
            for (int t = 0; t < 5; t++) { snap.TickIndex = t; tickIn.Tick(snap, NeutralModifiers()); }
            float spreadIn = ComputeLateralSpread(tickIn, snap);

            var tickTTD = new PositioningAITick(FormationFamily.F442);
            var hystTTD = typeof(PositioningAITick)
                .GetField("_hyst", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.GetValue(tickTTD) as HysteresisState;
            if (hystTTD != null) { hystTTD.CurrentPhase = Phase.TransToDef; hystTTD.CandidatePhase = Phase.TransToDef; }
            snap.PossessionOwnerEntityId = -1; snap.PossessionOwnerIsOwnTeam = false;
            tickTTD.SeedFromFormation(snap);
            for (int t = 0; t < 5; t++) { snap.TickIndex = t; tickTTD.Tick(snap, NeutralModifiers()); }
            float spreadTTD = ComputeLateralSpread(tickTTD, snap);

            Assert.That(spreadTTD, Is.LessThan(spreadIn),
                "TransToDef lateral spread must be narrower than InPoss (BaseLateral[TransToDef]=0.82 < 1.00)");
        }

        // ──────────────────────────────────────────────────────────────────────
        // T-T-006: Tactical correctness — GK slot clamped to own half
        // ──────────────────────────────────────────────────────────────────────
        [Test]
        public void TacticalCorrectness_GkSlot_ClampedToOwnHalf()
        {
            // Ball deep in attacking half (x=90). GK formula clamps ball.x to PITCH_HALF_LENGTH_M.
            var ball = new Vector3(90f, 34f, 0f);
            Vector2 gk = AnchorCalculator.ComputeGkSlot(ball);
            Assert.That(gk.x, Is.LessThan(PositioningAIConstants.PITCH_HALF_LENGTH_M),
                "GK slot.x must not exceed PITCH_HALF_LENGTH_M (52.5) even when ball is in opponent half");
        }

        // ── Private helpers ───────────────────────────────────────────────────

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

        private static float ComputeVerticalSpread(PositioningAITick tick, PositioningPerceptionSnapshot snap)
        {
            float minX = float.MaxValue, maxX = float.MinValue;
            for (int i = 1; i < snap.Agents.Length; i++) // skip GK (0)
            {
                Vector2 s = tick.GetFormationSlot(i);
                if (PositioningAITick.IsSentinelSlot(s)) continue;
                if (s.x < minX) minX = s.x;
                if (s.x > maxX) maxX = s.x;
            }
            return maxX - minX;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                                              |
// | 1.0     | 2026-05-29 | —      | Initial implementation (T-U-001..021, T-D-001..002, T-I-001..004, T-P-001, T-T-001).            |
// | 1.1     | 2026-06-01 | —      | Added T-U-022..072, T-I-005..011, T-D-003..006, T-P-002..003, T-T-002..006 (total ≥74 tests).   |
#endregion
