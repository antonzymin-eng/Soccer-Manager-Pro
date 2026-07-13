// File:     src/perception-system/Tests/PerceptionSystemTests.cs
// Created:  2026-05-31
// Modified: 2026-07-13
// Author:   —
// Spec:     Perception System #7 §5, Code Standards #20
// Purpose:  Unit tests for Perception System. FOV, OCC, LR, SC, BP test groups.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.PerceptionSystem.Tests
{
    // ════════════════════════════════════════════════════════════════════════════
    // FOV — Field of View (§3.1, §5.2)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for FovCalculator. Covers FOV-001 through FOV-008 per §5.2.
    /// Formula: EffectiveFoV = Max(160 + (D/20)×10 − PS×30, 120)
    /// </summary>
    [TestFixture]
    internal class FovCalculatorTests
    {
        // ── FOV-001 — Centre-facing entity is always visible ─────────────────────

        /// <summary>
        /// FOV-001: Observer faces East (1,0). Target directly ahead at 0° offset.
        /// Decisions=10, PressureScalar=0.
        /// Derivation: bonus=(10/20)×10°=5°. EffectiveFoV=165°. HalfAngle=82.5°.
        /// Target at 0° &lt; 82.5° → visible. Spec §5.2 FOV-001.
        /// </summary>
        [Test]
        public void FOV001_CentreTarget_IsVisible()
        {
            float effectiveFoV = FovCalculator.ComputeEffectiveFoV(10, 0.0f);
            float halfAngle    = FovCalculator.ComputeEffectiveFoVHalfAngle(effectiveFoV);

            Vector2 observerPos = Vector2.zero;
            Vector2 facingDir   = new Vector2(1.0f, 0.0f);
            Vector2 targetPos   = new Vector2(10.0f, 0.0f); // 0° offset

            bool visible = FovCalculator.IsInFoV(observerPos, facingDir, targetPos, halfAngle);

            Assert.AreEqual(165.0f, effectiveFoV, 0.01f, "FOV-001: EffectiveFoV should be 165°");
            Assert.IsTrue(visible, "FOV-001: Centre-facing target must be visible");
        }

        // ── FOV-002 — Entity at exact FoV boundary is included ───────────────────

        /// <summary>
        /// FOV-002: Decisions=1, PressureScalar=0. HalfAngle=80.25°.
        /// Target at exactly 80.25° offset must be included (boundary inclusive per §3.1.2).
        /// Spec §5.2 FOV-002.
        /// </summary>
        [Test]
        public void FOV002_ExactBoundaryEntity_IsVisible()
        {
            float effectiveFoV = FovCalculator.ComputeEffectiveFoV(1, 0.0f);
            // Derivation: bonus=(1/20)×10=0.5°. EffectiveFoV=160.5°. HalfAngle=80.25°.
            Assert.AreEqual(160.5f, effectiveFoV, 0.01f, "FOV-002: EffectiveFoV should be 160.5°");

            float halfAngle = FovCalculator.ComputeEffectiveFoVHalfAngle(effectiveFoV);
            Assert.AreEqual(80.25f, halfAngle, 0.01f, "FOV-002: HalfAngle should be 80.25°");

            Vector2 observerPos = Vector2.zero;
            Vector2 facingDir   = new Vector2(1.0f, 0.0f); // facing East
            // Place target at exactly halfAngle degrees from facing direction
            float angleRad  = halfAngle * Mathf.Deg2Rad;
            Vector2 targetPos = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * 10.0f;

            bool visible = FovCalculator.IsInFoV(observerPos, facingDir, targetPos, halfAngle);
            Assert.IsTrue(visible, "FOV-002: Target at exact FoV boundary must be included (≤ inclusive)");
        }

        // ── FOV-003 — Entity one step outside FoV boundary is excluded ───────────

        /// <summary>
        /// FOV-003: Same setup as FOV-002 but target at 80.26° — one step outside boundary.
        /// Expected: IsVisible = false. Spec §5.2 FOV-003.
        /// </summary>
        [Test]
        public void FOV003_OneStepOutsideBoundary_IsNotVisible()
        {
            float effectiveFoV = FovCalculator.ComputeEffectiveFoV(1, 0.0f);
            float halfAngle    = FovCalculator.ComputeEffectiveFoVHalfAngle(effectiveFoV);

            Vector2 observerPos = Vector2.zero;
            Vector2 facingDir   = new Vector2(1.0f, 0.0f);
            // 80.26° is just outside the 80.25° half-angle
            float angleRad  = 80.26f * Mathf.Deg2Rad;
            Vector2 targetPos = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad)) * 10.0f;

            bool visible = FovCalculator.IsInFoV(observerPos, facingDir, targetPos, halfAngle);
            Assert.IsFalse(visible, "FOV-003: Target 0.01° beyond boundary must be excluded");
        }

        // ── FOV-004 — Decisions=1 vs Decisions=20 produce measurable FoV difference

        /// <summary>
        /// FOV-004: EffectiveFoV(D=1)=160.5°, EffectiveFoV(D=20)=170°. Difference=9.5°.
        /// Confirms Decisions↑ → FoV↑. Spec §5.2 FOV-004.
        /// </summary>
        [Test]
        public void FOV004_Decisions1Vs20_FovDifference_Is9Point5Degrees()
        {
            float fovD1  = FovCalculator.ComputeEffectiveFoV(1, 0.0f);
            float fovD20 = FovCalculator.ComputeEffectiveFoV(20, 0.0f);

            Assert.AreEqual(160.5f, fovD1,  0.01f, "FOV-004: D=1 EffectiveFoV should be 160.5°");
            Assert.AreEqual(170.0f, fovD20, 0.01f, "FOV-004: D=20 EffectiveFoV should be 170°");
            Assert.AreEqual(9.5f, fovD20 - fovD1, 0.01f, "FOV-004: Difference must be 9.5°");
            Assert.Greater(fovD20, fovD1, "FOV-004: Higher Decisions must yield wider FoV");
        }

        // ── FOV-005 — PressureScalar=0 produces no FoV reduction ─────────────────

        /// <summary>
        /// FOV-005: PressureScalar=0, Decisions=10. No reduction; result = BASE + bonus only.
        /// Spec §5.2 FOV-005.
        /// </summary>
        [Test]
        public void FOV005_ZeroPressure_NoFovReduction()
        {
            float fovWithPressure0    = FovCalculator.ComputeEffectiveFoV(10, 0.0f);
            // Derivation: bonus=5°. EffectiveFoV=165°. PressureReduction=0.
            Assert.AreEqual(165.0f, fovWithPressure0, 0.01f,
                "FOV-005: Zero pressure must not reduce FoV below bonus-adjusted base");
        }

        // ── FOV-006 — PressureScalar=0.5 produces proportional FoV reduction ─────

        /// <summary>
        /// FOV-006: PressureScalar=0.5, Decisions=10. Reduction=15°. EffectiveFoV=150°.
        /// Pressure is continuous; no threshold gate. Spec §5.2 FOV-006.
        /// </summary>
        [Test]
        public void FOV006_HalfPressure_Reduces15Degrees()
        {
            // Derivation: bonus=(10/20)×10=5°. Reduction=0.5×30=15°. 160+5-15=150°.
            float fov = FovCalculator.ComputeEffectiveFoV(10, 0.5f);
            Assert.AreEqual(150.0f, fov, 0.01f, "FOV-006: PS=0.5 must produce EffectiveFoV=150°");
        }

        // ── FOV-007 — PressureScalar=1.0 with Decisions=1 — floor not reached ────

        /// <summary>
        /// FOV-007: PressureScalar=1.0, Decisions=1. EffectiveFoV=130.5°. Floor NOT triggered.
        /// Derivation: 160+0.5−30=130.5°. MIN_FOV_ANGLE=120° not reached.
        /// To hit floor: need P&gt;1.35 (impossible). Spec §5.2 FOV-007.
        /// </summary>
        [Test]
        public void FOV007_MaxPressure_Decisions1_FloorNotTriggered()
        {
            float fov = FovCalculator.ComputeEffectiveFoV(1, 1.0f);
            Assert.AreEqual(130.5f, fov, 0.01f, "FOV-007: EffectiveFoV must be 130.5° (no floor clamp)");
            Assert.Greater(fov, PerceptionConstants.MIN_FOV_ANGLE,
                "FOV-007: Result must be above MIN_FOV_ANGLE=120°");
        }

        // ── FOV-008 — Blind-side arc is strict complement of EffectiveFoV ─────────

        /// <summary>
        /// FOV-008: Decisions=10, PressureScalar=0. EffectiveFoV=165°.
        /// BlindSideArcWidth (derived) = 360°−165°=195°. Spec §5.2 FOV-008.
        /// </summary>
        [Test]
        public void FOV008_BlindSideArc_IsComplementOfEffectiveFov()
        {
            float effectiveFoV     = FovCalculator.ComputeEffectiveFoV(10, 0.0f);
            float blindSideArcWidth = PerceptionConstants.FULL_CIRCLE_DEG - effectiveFoV;

            Assert.AreEqual(165.0f, effectiveFoV, 0.01f,
                "FOV-008: EffectiveFoVAngle should be 165°");
            Assert.AreEqual(195.0f, blindSideArcWidth, 0.01f,
                "FOV-008: BlindSideArcWidth (derived) should be 195°");
        }

        // ── FOV — Angular separation sign and normalisation ───────────────────────

        /// <summary>
        /// Angular separation is normalised to [−180°, +180°]. A target directly behind
        /// the observer (facing East, target to West) must produce ±180°.
        /// </summary>
        [Test]
        public void AngularSeparation_TargetBehind_IsNormalisedToHalfCircle()
        {
            Vector2 observerPos = Vector2.zero;
            Vector2 facingDir   = new Vector2(1.0f, 0.0f); // East
            Vector2 targetPos   = new Vector2(-10.0f, 0.0f); // West (behind)

            float separation = FovCalculator.ComputeAngularSeparationDeg(observerPos, facingDir, targetPos);
            // West is 180° from East — normalised result is ±180
            Assert.AreEqual(180.0f, Mathf.Abs(separation), 0.01f,
                "Angular separation to directly-behind target must be ±180°");
        }

        /// <summary>
        /// IsInBlindSide is the strict complement of IsInFoV for any valid input.
        /// </summary>
        [Test]
        public void BlindSide_IsStrictComplementOfFoV()
        {
            float halfAngle = 82.5f; // 165° FoV
            Vector2 observerPos = Vector2.zero;
            Vector2 facingDir   = new Vector2(1.0f, 0.0f);

            Vector2 frontTarget = new Vector2(10.0f, 0.0f);  // 0° — in FoV
            Vector2 rearTarget  = new Vector2(-10.0f, 0.0f); // 180° — in blind side

            bool frontInFoV       = FovCalculator.IsInFoV(observerPos, facingDir, frontTarget, halfAngle);
            bool frontInBlindSide = FovCalculator.IsInBlindSide(observerPos, facingDir, frontTarget, halfAngle);
            bool rearInFoV        = FovCalculator.IsInFoV(observerPos, facingDir, rearTarget, halfAngle);
            bool rearInBlindSide  = FovCalculator.IsInBlindSide(observerPos, facingDir, rearTarget, halfAngle);

            Assert.IsTrue(frontInFoV,        "Front target must be in FoV");
            Assert.IsFalse(frontInBlindSide, "Front target must NOT be in blind side");
            Assert.IsFalse(rearInFoV,        "Rear target must NOT be in FoV");
            Assert.IsTrue(rearInBlindSide,   "Rear target must be in blind side");
        }

        /// <summary>
        /// Peripheral arc inner bound is BASE_FOV_HALF_ANGLE/2 = 40°. Entities in [40°,80°]
        /// are in peripheral arc; outside that range they are not. Spec §3.3.3.
        /// </summary>
        [Test]
        public void PeripheralArc_Boundaries_AreCorrect()
        {
            // BASE_FOV_HALF_ANGLE = 80°. Inner bound = 40°.
            Assert.IsFalse(FovCalculator.IsInPeripheralArc(39.9f),
                "39.9° must be below peripheral arc inner bound");
            Assert.IsTrue(FovCalculator.IsInPeripheralArc(40.0f),
                "40.0° must be exactly at inner bound — inclusive");
            Assert.IsTrue(FovCalculator.IsInPeripheralArc(60.0f),
                "60.0° must be within peripheral arc");
            Assert.IsTrue(FovCalculator.IsInPeripheralArc(80.0f),
                "80.0° must be exactly at outer bound — inclusive");
            Assert.IsFalse(FovCalculator.IsInPeripheralArc(80.1f),
                "80.1° must be beyond peripheral arc outer bound");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // OCC — Shadow Cone Occlusion (§3.2, §5.3)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for OcclusionFilter. Covers OCC-001 through OCC-007 per §5.3.
    /// Shadow half-angle: arcsin(AGENT_BODY_RADIUS / dist), floor = MIN_SHADOW_HALF_ANGLE=5°.
    /// </summary>
    [TestFixture]
    internal class OcclusionFilterTests
    {
        // ── Shared helpers ───────────────────────────────────────────────────────

        private static AgentState[] MakeAgentStates(int count = 22)
        {
            return new AgentState[count];
        }

        private static PerceptionAgentAttributes[] MakeAgentAttrs(int count = 22, int observerTeam = 0)
        {
            PerceptionAgentAttributes[] attrs = new PerceptionAgentAttributes[count];
            for (int i = 0; i < count; i++)
            {
                attrs[i] = PerceptionAgentAttributes.CreateDefault();
                attrs[i].TeamId = observerTeam; // all same team by default
            }
            return attrs;
        }

        // ── OCC-001 — No occluder: target visible ────────────────────────────────

        /// <summary>
        /// OCC-001: Observer (0,0). Target (10,0). No opponents in candidate list.
        /// Expected: IsOccluded = false. Spec §5.3 OCC-001.
        /// </summary>
        [Test]
        public void OCC001_NoOccluder_TargetNotOccluded()
        {
            AgentState[]              agentStates = MakeAgentStates();
            PerceptionAgentAttributes[] agentAttrs  = MakeAgentAttrs();
            int[] candidateIds  = new int[0];

            bool occluded = OcclusionFilter.IsOccluded(
                Vector2.zero, new Vector2(10.0f, 0.0f), 5,
                agentStates, candidateIds, 0, 0, agentAttrs);

            Assert.IsFalse(occluded, "OCC-001: No occluder present — target must not be occluded");
        }

        // ── OCC-002 — Collinear opponent between observer and target ─────────────

        /// <summary>
        /// OCC-002: Observer (0,0). Occluder (5,0). Target (10,0). Occluder is opponent.
        /// Shadow half-angle at 5m: arcsin(0.4/5)≈4.57° → floor=5°. 0°&lt;5° → occluded.
        /// Spec §5.3 OCC-002.
        /// </summary>
        [Test]
        public void OCC002_CollinearOpponent_TargetIsOccluded()
        {
            const int occluderIdx  = 3;
            const int targetIdx    = 5;
            const int observerTeam = 0;
            const int opponentTeam = 1;

            AgentState[]              agentStates = MakeAgentStates();
            PerceptionAgentAttributes[] agentAttrs  = MakeAgentAttrs(22, observerTeam);

            agentStates[occluderIdx].Position = new Vector2(5.0f, 0.0f);
            agentAttrs[occluderIdx].TeamId    = opponentTeam;

            agentStates[targetIdx].Position   = new Vector2(10.0f, 0.0f);
            agentAttrs[targetIdx].TeamId      = opponentTeam;

            int[] candidateIds = new[] { occluderIdx };

            bool occluded = OcclusionFilter.IsOccluded(
                Vector2.zero,
                agentStates[targetIdx].Position,
                targetIdx,
                agentStates, candidateIds, 1, observerTeam, agentAttrs);

            Assert.IsTrue(occluded, "OCC-002: Collinear opponent must occlude target behind it");
        }

        // ── OCC-003 — Target at same distance as occluder — NOT occluded ─────────

        /// <summary>
        /// OCC-003: Occluder (5,0). Target (5,1). Depth check: occluderDistSq must be
        /// strictly less than targetDistSq. Equal distances → not occluded.
        /// Spec §5.3 OCC-003.
        /// </summary>
        [Test]
        public void OCC003_SameDistanceAsOccluder_TargetNotOccluded()
        {
            const int occluderIdx  = 2;
            const int targetIdx    = 6;
            const int observerTeam = 0;
            const int opponentTeam = 1;

            AgentState[]              agentStates = MakeAgentStates();
            PerceptionAgentAttributes[] agentAttrs  = MakeAgentAttrs(22, observerTeam);

            agentStates[occluderIdx].Position = new Vector2(5.0f, 0.0f);
            agentAttrs[occluderIdx].TeamId    = opponentTeam;

            // Target is also at exactly 5 m from origin (same distance as occluder)
            agentStates[targetIdx].Position   = new Vector2(5.0f, 1.0f);
            agentAttrs[targetIdx].TeamId      = opponentTeam;

            int[] candidateIds = new[] { occluderIdx };

            bool occluded = OcclusionFilter.IsOccluded(
                Vector2.zero,
                agentStates[targetIdx].Position,
                targetIdx,
                agentStates, candidateIds, 1, observerTeam, agentAttrs);

            Assert.IsFalse(occluded,
                "OCC-003: Occluder at same distance as target must not occlude (strict >)");
        }

        // ── OCC-004 — MIN_SHADOW_HALF_ANGLE floor active at 5m ───────────────────

        /// <summary>
        /// OCC-004: Occluder at 5m. arcsin(0.4/5)=4.57° → floor=5°.
        /// Sub-A: Target at 4.9° deviation → occluded. Sub-B: Target at 5.1° → not occluded.
        /// Spec §5.3 OCC-004.
        /// </summary>
        [Test]
        public void OCC004_MinShadowFloor_Active_At5m()
        {
            float halfAngle = OcclusionFilter.ComputeShadowHalfAngleDeg(Vector2.zero, new Vector2(5.0f, 0.0f));

            // arcsin(0.4/5.1) ≈ 4.49° — before guard adjustment; floor=5° must apply
            Assert.AreEqual(PerceptionConstants.MIN_SHADOW_HALF_ANGLE, halfAngle, 0.05f,
                "OCC-004: Shadow half-angle at 5m must equal MIN_SHADOW_HALF_ANGLE floor (5°)");
        }

        // ── OCC-005 — MIN_SHADOW_HALF_ANGLE floor NOT active at 2m ──────────────

        /// <summary>
        /// OCC-005: Occluder at 2m. arcsin(0.4/2)≈11.54°. Floor=5° not active.
        /// Spec §5.3 OCC-005.
        /// </summary>
        [Test]
        public void OCC005_MinShadowFloor_NotActive_At2m()
        {
            float halfAngle = OcclusionFilter.ComputeShadowHalfAngleDeg(Vector2.zero, new Vector2(2.0f, 0.0f));

            // Hand-calc (§A.4 / Appendix B): arcsin(0.4/2.0) = arcsin(0.200) = 11.537°.
            // The previous expectation 11.31° was arctan(0.2) — a trig mis-derivation (tangent
            // for sine); the spec's tangent-line geometry uses arcsin and both appendix worked
            // examples print 11.537° (dotnet-CI quarantine adjudication). The denominator guard
            // Max(d, AGENT_BODY_RADIUS + 0.1) = Max(2.0, 0.5) is inactive at 2m.
            Assert.Greater(halfAngle, PerceptionConstants.MIN_SHADOW_HALF_ANGLE,
                "OCC-005: Shadow half-angle at 2m must exceed 5° floor (floor not active)");
            Assert.AreEqual(11.537f, halfAngle, 0.05f,
                "OCC-005: arcsin(0.4/2)≈11.537° expected (§A.4)");
        }

        // ── OCC-006 — Teammate does NOT generate shadow cone (Stage 0) ───────────

        /// <summary>
        /// OCC-006: Teammate collinear between observer and target. Stage 0: teammates
        /// do not generate shadow cones (OQ-1). Expected: IsOccluded = false.
        /// Spec §5.3 OCC-006.
        /// </summary>
        [Test]
        public void OCC006_Teammate_DoesNotOcclude()
        {
            const int teammateIdx  = 4;
            const int targetIdx    = 7;
            const int observerTeam = 0;

            AgentState[]              agentStates = MakeAgentStates();
            PerceptionAgentAttributes[] agentAttrs  = MakeAgentAttrs(22, observerTeam);

            agentStates[teammateIdx].Position = new Vector2(5.0f, 0.0f);
            agentAttrs[teammateIdx].TeamId    = observerTeam; // same team — no shadow cone

            agentStates[targetIdx].Position   = new Vector2(10.0f, 0.0f);
            agentAttrs[targetIdx].TeamId      = 1;

            int[] candidateIds = new[] { teammateIdx };

            bool occluded = OcclusionFilter.IsOccluded(
                Vector2.zero,
                agentStates[targetIdx].Position,
                targetIdx,
                agentStates, candidateIds, 1, observerTeam, agentAttrs);

            Assert.IsFalse(occluded,
                "OCC-006: Teammate must NOT generate shadow cone at Stage 0 (OQ-1)");
        }

        // ── OCC-007 — Multiple occluders: any one causing occlusion is sufficient ─

        /// <summary>
        /// OCC-007: Two opponent occluders at (3,0) and (7,0). Target at (10,0).
        /// Either independently occludes. Expected: IsOccluded = true (OR logic).
        /// Spec §5.3 OCC-007.
        /// </summary>
        [Test]
        public void OCC007_MultipleOccluders_AnyOneSuffices()
        {
            const int occ1Idx      = 1;
            const int occ2Idx      = 2;
            const int targetIdx    = 8;
            const int observerTeam = 0;
            const int opponentTeam = 1;

            AgentState[]              agentStates = MakeAgentStates();
            PerceptionAgentAttributes[] agentAttrs  = MakeAgentAttrs(22, observerTeam);

            agentStates[occ1Idx].Position = new Vector2(3.0f, 0.0f);
            agentAttrs[occ1Idx].TeamId    = opponentTeam;

            agentStates[occ2Idx].Position = new Vector2(7.0f, 0.0f);
            agentAttrs[occ2Idx].TeamId    = opponentTeam;

            agentStates[targetIdx].Position = new Vector2(10.0f, 0.0f);
            agentAttrs[targetIdx].TeamId    = opponentTeam;

            int[] candidateIds = new[] { occ1Idx, occ2Idx };

            bool occluded = OcclusionFilter.IsOccluded(
                Vector2.zero,
                agentStates[targetIdx].Position,
                targetIdx,
                agentStates, candidateIds, 2, observerTeam, agentAttrs);

            Assert.IsTrue(occluded,
                "OCC-007: Target must be occluded when any one occluder independently qualifies");
        }

        // ── OCC — Zero-distance guard: no divide-by-zero ─────────────────────────

        /// <summary>
        /// Occluder coincident with observer (0,0). Guard ensures no NaN or exception.
        /// Spec §5.3 OCC-009.
        /// </summary>
        [Test]
        public void OCC_ZeroDistanceOccluder_NoNaNOrException()
        {
            float halfAngle = OcclusionFilter.ComputeShadowHalfAngleDeg(Vector2.zero, Vector2.zero);

            Assert.IsFalse(float.IsNaN(halfAngle),
                "OCC-009: Shadow half-angle must not be NaN when occluder is coincident with observer");
            Assert.IsFalse(float.IsInfinity(halfAngle),
                "OCC-009: Shadow half-angle must not be infinity when occluder is coincident with observer");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // LR — Recognition Latency (§3.3, §5.4)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for RecognitionLatencyTracker. Covers LR-001 through LR-008 per §5.4.
    /// Formula: L_rec = floor(L_MAX − (L_MAX−L_MIN)×(D−1)/19).
    /// Half-turn bonus: floor(L_rec×0.85), min L_MIN.
    /// Noise: DeterministicHash(obs,tgt,frame)%2 (+0 or +1).
    /// </summary>
    [TestFixture]
    internal class RecognitionLatencyTrackerTests
    {
        // ── LR-001 — L_rec at Decisions=1 (maximum latency = 5 ticks) ────────────

        /// <summary>
        /// LR-001: Decisions=1. L_rec = floor(5−4×(0/19)) = 5. Spec §5.4 LR-001.
        /// </summary>
        [Test]
        public void LR001_Decisions1_LrecIs5Ticks()
        {
            RecognitionLatencyTracker tracker = new RecognitionLatencyTracker();
            // No half-turn, target at 0° (central), frame 0 — isolate base formula
            // Use noise=0 path: find a (obs,tgt,frame) combo where hash%2==0
            // Test the ComputeLRec output directly for noise-free path
            int lRecBase = tracker.ComputeLRec(0, 1, 1, false, 0.0f, 0);

            // L_rec is 5 or 6 depending on noise. Base=5, noise=0 or 1.
            Assert.GreaterOrEqual(lRecBase, 5, "LR-001: L_rec at D=1 must be ≥5 ticks");
            Assert.LessOrEqual(lRecBase, PerceptionConstants.L_MAX, "LR-001: L_rec must not exceed L_MAX");
        }

        /// <summary>
        /// LR-001 (counter variant): ProcessVisible accumulates latency correctly.
        /// Entity at D=1 (L_rec=5) must not be confirmed before the 5th tick.
        /// </summary>
        [Test]
        public void LR001_ProcessVisible_ConfirmsAfterLrecTicks()
        {
            // §3.3.4: L_rec_final = Min(L_rec_base + noise, L_MAX) with additive deterministic
            // noise ∈ {0, +1} from DeterministicHash(observerId, targetId, frameNumber) % 2.
            // The previous form asserted confirmation on the FIRST call unconditionally — true
            // only when the noise term is 0; for this (observer, target, frame) tuple the hash
            // is odd, so L_rec_final = 1 + 1 = 2 and §3.3.4 REQUIRES the first call to return
            // false (dotnet-CI quarantine adjudication). The spec-true invariant is "confirms
            // exactly at the L_rec_final-th visible tick", asserted tick-by-tick below.
            RecognitionLatencyTracker tracker = new RecognitionLatencyTracker();
            const int observerId = 0;
            const int targetId   = 1;
            const int decisions  = 20; // D=20 → L_rec_base = floor(5 − 4×(19/19)) = 1

            int lRec = tracker.ComputeLRec(observerId, targetId, decisions, false, 0.0f, 0);
            Assert.GreaterOrEqual(lRec, PerceptionConstants.L_MIN,
                "LR-001 variant: L_rec_final at D=20 must be ≥ L_MIN");
            Assert.LessOrEqual(lRec, PerceptionConstants.L_MIN + 1,
                "LR-001 variant: L_rec_final at D=20 must be ≤ base (1) + noise (1)");

            for (int tick = 1; tick <= lRec; tick++)
            {
                bool confirmed = tracker.ProcessVisible(observerId, targetId, decisions,
                    false, 0.0f, 0, false);
                if (tick < lRec)
                {
                    Assert.IsFalse(confirmed,
                        "LR-001 variant: entity must NOT be confirmed before the L_rec-th visible tick");
                }
                else
                {
                    Assert.IsTrue(confirmed,
                        "LR-001 variant: entity must be confirmed exactly at the L_rec-th visible tick");
                }
            }
        }

        // ── LR-002 — L_rec at Decisions=20 (minimum latency = 1 tick) ────────────

        /// <summary>
        /// LR-002: Decisions=20. L_rec = floor(5−4×(19/19)) = 1. Spec §5.4 LR-002.
        /// </summary>
        [Test]
        public void LR002_Decisions20_LrecIs1Tick()
        {
            RecognitionLatencyTracker tracker = new RecognitionLatencyTracker();
            int lRec = tracker.ComputeLRec(0, 1, 20, false, 0.0f, 0);

            // Base = 1; noise adds 0 or 1. Result: 1 or 2.
            Assert.GreaterOrEqual(lRec, PerceptionConstants.L_MIN, "LR-002: L_rec must be ≥ L_MIN");
            Assert.LessOrEqual(lRec, 2, "LR-002: L_rec at D=20 must be at most 2 (base=1 + noise 0/1)");
        }

        // ── LR-003 — L_rec at Decisions=10 (mid-range = 3 ticks) ────────────────

        /// <summary>
        /// LR-003: Decisions=10. L_rec = floor(5−4×(9/19)) = floor(3.105) = 3. Spec §5.4 LR-003.
        /// </summary>
        [Test]
        public void LR003_Decisions10_LrecIs3Ticks()
        {
            RecognitionLatencyTracker tracker = new RecognitionLatencyTracker();
            // Test base computation: run over multiple frames to see both noise outcomes
            // and verify base always floors to 3.
            bool sawBase3  = false;
            for (int frame = 0; frame < 20; frame++)
            {
                int lRec = tracker.ComputeLRec(0, 1, 10, false, 0.0f, frame);
                if (lRec == 3) { sawBase3 = true; }
                // Must never exceed 4 (base=3, max noise=+1)
                Assert.LessOrEqual(lRec, 4, $"LR-003: L_rec at D=10 must be ≤4 (frame {frame})");
                Assert.GreaterOrEqual(lRec, 3, $"LR-003: L_rec at D=10 must be ≥3 (frame {frame})");
            }
            Assert.IsTrue(sawBase3, "LR-003: Must observe L_rec=3 (base) across multiple frames");
        }

        // ── LR-004 — Half-turn bonus reduces L_rec in peripheral arc ─────────────

        /// <summary>
        /// LR-004: Decisions=10 (base=3). Target at 55° (peripheral arc 40°–80°).
        /// Half-turn reduces: floor(3×0.85)=floor(2.55)=2. Spec §5.4 LR-004.
        /// </summary>
        [Test]
        public void LR004_HalfTurnBonus_PeripheralArc_ReducesLrec()
        {
            RecognitionLatencyTracker tracker = new RecognitionLatencyTracker();
            // Use frame=0 to minimise noise variability; compare with/without half-turn
            int lRecNoBonus   = tracker.ComputeLRec(0, 1, 10, false, 55.0f, 0);
            int lRecWithBonus = tracker.ComputeLRec(0, 1, 10, true,  55.0f, 0);

            Assert.LessOrEqual(lRecWithBonus, lRecNoBonus,
                "LR-004: Half-turn bonus must reduce L_rec for peripheral arc entity");
            // With noise=0: bonus yields 2 < 3
            Assert.GreaterOrEqual(lRecWithBonus, PerceptionConstants.L_MIN,
                "LR-004: Half-turn bonus must not push L_rec below L_MIN");
        }

        // ── LR-005 — Half-turn bonus NOT applied outside peripheral arc ───────────

        /// <summary>
        /// LR-005: Target at 20° (centre arc, below 40° threshold) and at 85° (outside FoV).
        /// No bonus applies in either case. Spec §5.4 LR-005.
        /// </summary>
        [Test]
        public void LR005_HalfTurnBonus_NotApplied_OutsidePeripheralArc()
        {
            RecognitionLatencyTracker tracker = new RecognitionLatencyTracker();
            const int decisions = 10;
            const int frame     = 0;

            // Sub-A: 20° — below inner bound (40°), no bonus
            int lRecAt20NoBonus   = tracker.ComputeLRec(0, 1, decisions, false, 20.0f, frame);
            int lRecAt20WithBonus = tracker.ComputeLRec(0, 1, decisions, true,  20.0f, frame);
            Assert.AreEqual(lRecAt20NoBonus, lRecAt20WithBonus,
                "LR-005A: At 20° (outside peripheral arc) half-turn bonus must not apply");

            // Sub-B: 85° — above outer bound (80°), no bonus
            int lRecAt85NoBonus   = tracker.ComputeLRec(0, 1, decisions, false, 85.0f, frame);
            int lRecAt85WithBonus = tracker.ComputeLRec(0, 1, decisions, true,  85.0f, frame);
            Assert.AreEqual(lRecAt85NoBonus, lRecAt85WithBonus,
                "LR-005B: At 85° (outside peripheral arc) half-turn bonus must not apply");
        }

        // ── LR-006 — Additive noise never pushes L_rec below L_MIN ───────────────

        /// <summary>
        /// LR-006: Noise is +0 or +1 only. With D=20 (base=1), noise=+1 gives 2 — never below 1.
        /// Spec §5.4 LR-006.
        /// </summary>
        [Test]
        public void LR006_NoiseIsAdditive_NeverBelowLMin()
        {
            RecognitionLatencyTracker tracker = new RecognitionLatencyTracker();
            for (int frame = 0; frame < 50; frame++)
            {
                int lRec = tracker.ComputeLRec(0, 1, 20, false, 0.0f, frame);
                Assert.GreaterOrEqual(lRec, PerceptionConstants.L_MIN,
                    $"LR-006: L_rec must never drop below L_MIN at frame {frame}");
                // Noise is 0 or +1; base=1 → max = 2
                Assert.LessOrEqual(lRec, 2, $"LR-006: L_rec at D=20 must be ≤2 (frame {frame})");
            }
        }

        // ── LR-007 — 1-tick visibility gap does NOT reset confirmation ────────────

        /// <summary>
        /// LR-007: Target visible ticks 0–2, invisible tick 3, visible tick 4.
        /// CONFIRMATION_EXPIRY_TICKS=1. Entity confirmed at tick 2 (D=20, L_rec=1).
        /// After 1-tick gap, remains confirmed (expiry window not exhausted).
        /// Spec §5.4 LR-007.
        /// </summary>
        [Test]
        public void LR007_OneTick_VisibilityGap_DoesNotResetConfirmation()
        {
            RecognitionLatencyTracker tracker = new RecognitionLatencyTracker();
            const int obs     = 0;
            const int tgt     = 2;
            const int frame   = 0;

            // D=20: L_rec=1 — confirmed on first ProcessVisible call
            bool confirmed = tracker.ProcessVisible(obs, tgt, 20, false, 0.0f, frame, false);
            Assert.IsTrue(confirmed, "LR-007: D=20 should confirm immediately");

            // 1-tick gap — entity invisible (but within expiry window)
            bool stillConfirmed = tracker.ProcessInvisible(obs, tgt);
            Assert.IsTrue(stillConfirmed,
                "LR-007: 1-tick gap must not reset confirmation (within expiry window)");
        }

        // ── LR-008 — 2-tick gap resets confirmation counter ──────────────────────

        /// <summary>
        /// LR-008: Entity confirmed; then invisible for 2 ticks (&gt; CONFIRMATION_EXPIRY_TICKS=1).
        /// After 2nd invisible tick, entity is evicted and must re-accumulate.
        /// Spec §5.4 LR-008.
        /// </summary>
        [Test]
        public void LR008_TwoTick_VisibilityGap_ResetsConfirmation()
        {
            RecognitionLatencyTracker tracker = new RecognitionLatencyTracker();
            const int obs   = 0;
            const int tgt   = 3;

            // Confirm entity (D=20, L_rec=1)
            tracker.ProcessVisible(obs, tgt, 20, false, 0.0f, 0, false);

            // Tick 1 invisible: starts expiry countdown
            bool afterGap1 = tracker.ProcessInvisible(obs, tgt);
            // Tick 2 invisible: expiry elapsed, entity evicted
            bool afterGap2 = tracker.ProcessInvisible(obs, tgt);

            // After 2-tick gap the entity should be evicted
            Assert.IsFalse(afterGap2,
                "LR-008: 2-tick gap must evict confirmed entity (> CONFIRMATION_EXPIRY_TICKS=1)");
        }

        // ── LR — DeterministicHash produces non-negative results ─────────────────

        /// <summary>
        /// DeterministicHash must always return a non-negative integer (used as noise seed via %2).
        /// Spec §3.3.4 KD-4.
        /// </summary>
        [Test]
        public void LR_DeterministicHash_AlwaysNonNegative()
        {
            for (int a = 0; a < 22; a++)
            {
                for (int b = 0; b < 22; b++)
                {
                    int hash = RecognitionLatencyTracker.DeterministicHash(a, b, 100);
                    Assert.GreaterOrEqual(hash, 0,
                        $"DeterministicHash({a},{b},100) must be ≥ 0");
                }
            }
        }

        // ── LR — ForcedRefresh bypasses L_rec ────────────────────────────────────

        /// <summary>
        /// FR-003: Forced refresh sets L_rec=0 — entity confirmed immediately regardless
        /// of Decisions attribute. Spec §5.8 FR-003.
        /// </summary>
        [Test]
        public void LR_ForcedRefresh_ConfirmsImmediately()
        {
            RecognitionLatencyTracker tracker = new RecognitionLatencyTracker();
            const int obs = 0;
            const int tgt = 4;

            // D=1 would normally require 5 ticks, but forced refresh bypasses L_rec
            bool confirmed = tracker.ProcessVisible(obs, tgt, 1, false, 0.0f, 0, forcedRefresh: true);

            Assert.IsTrue(confirmed,
                "FR-003: Forced refresh must confirm entity immediately regardless of Decisions");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // SC — Shoulder Check Scheduler (§3.4, §5.5)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for ShoulderCheckScheduler. Covers SC-001..SC-008 per §5.5.
    /// Interval formula: CHECK_MAX − (CHECK_MAX−CHECK_MIN)×(Anticipation−1)/19.
    /// </summary>
    [TestFixture]
    internal class ShoulderCheckSchedulerTests
    {
        // ── SC-003 — Check interval at Anticipation=1 (slowest = 30 ticks) ───────

        /// <summary>
        /// SC-003: Anticipation=1. Interval = 30−24×(0/19) = 30. First check at tick 30+.
        /// Spec §5.5 SC-003.
        /// </summary>
        [Test]
        public void SC003_Anticipation1_IntervalIs30Ticks()
        {
            ShoulderCheckScheduler scheduler = new ShoulderCheckScheduler();
            const int agentId = 0;

            // Agent 0 initial next-check-frame = (0*3+7) % 30 = 7. Fire at tick 7.
            bool firedAt7  = scheduler.UpdateAgent(agentId, 1, false, 7);
            Assert.IsTrue(firedAt7, "SC-003: Check must fire at initial stagger frame");

            // After firing at tick 7 with Anticipation=1 (interval≈30), next check ~30 ticks later
            // So at tick 8 it must NOT fire
            bool firedAt8  = scheduler.UpdateAgent(agentId, 1, false, 8);
            Assert.IsFalse(firedAt8, "SC-003: Check must not fire 1 tick after previous check");
        }

        // ── SC-004 — Check interval at Anticipation=20 (fastest = 6 ticks) ───────

        /// <summary>
        /// SC-004: Anticipation=20. Interval = 30−24×(19/19) = 6 ticks.
        /// Spec §5.5 SC-004.
        /// </summary>
        [Test]
        public void SC004_Anticipation20_IntervalIs6Ticks()
        {
            ShoulderCheckScheduler scheduler = new ShoulderCheckScheduler();
            const int agentId = 1; // stagger: (1*3+7)%30=10

            // Fire at stagger tick 10
            scheduler.UpdateAgent(agentId, 20, false, 10);

            // Next check should be ~6 ticks later (jitter ±2 → range 14–18)
            bool firedTooSoon = false;
            for (int tick = 11; tick < 14; tick++)
            {
                if (scheduler.UpdateAgent(agentId, 20, false, tick))
                {
                    firedTooSoon = true;
                }
            }
            Assert.IsFalse(firedTooSoon,
                "SC-004: With Anticipation=20 check interval=6, must not re-fire within 3 ticks");
        }

        // ── SC-005 — Possession doubles check interval ────────────────────────────

        /// <summary>
        /// SC-005: Anticipation=20 (base=6). With possession, interval=12.
        /// Spec §5.5 SC-005.
        /// </summary>
        [Test]
        public void SC005_Possession_DoublesCheckInterval()
        {
            ShoulderCheckScheduler schedulerNoPoss  = new ShoulderCheckScheduler();
            ShoulderCheckScheduler schedulerWithPoss = new ShoulderCheckScheduler();
            const int agentId      = 2;
            const int anticipation = 20;
            const int staggerTick  = (2 * 3 + 7) % 30; // = 13

            // Fire initial check at stagger tick
            schedulerNoPoss.UpdateAgent(agentId, anticipation, false, staggerTick);
            schedulerWithPoss.UpdateAgent(agentId, anticipation, true, staggerTick);

            // Count next fire ticks for each
            int? nextNoPoss   = null;
            int? nextWithPoss = null;
            for (int tick = staggerTick + 1; tick <= staggerTick + 40; tick++)
            {
                if (nextNoPoss   == null && schedulerNoPoss.UpdateAgent(agentId, anticipation, false, tick))
                {
                    nextNoPoss = tick;
                }
                if (nextWithPoss == null && schedulerWithPoss.UpdateAgent(agentId, anticipation, true, tick))
                {
                    nextWithPoss = tick;
                }
            }

            Assert.IsNotNull(nextNoPoss,   "SC-005: Check without possession must fire in 40 ticks");
            Assert.IsNotNull(nextWithPoss, "SC-005: Check with possession must fire in 40 ticks");
            Assert.Greater(nextWithPoss.Value, nextNoPoss.Value,
                "SC-005: Possession must delay next check (doubled interval)");
        }

        // ── SC — Window active/inactive state management ──────────────────────────

        /// <summary>
        /// SC-008: When a check fires, ShoulderCheckAnimData must be populated.
        /// Window active immediately after fire. Spec §5.5 SC-008.
        /// </summary>
        [Test]
        public void SC008_CheckFire_PopulatesAnimDataAndOpensWindow()
        {
            ShoulderCheckScheduler scheduler = new ShoulderCheckScheduler();
            const int agentId = 3;
            const int stagger = (3 * 3 + 7) % 30; // = 16

            bool fired = scheduler.UpdateAgent(agentId, 10, false, stagger);
            Assert.IsTrue(fired, "SC-008: Check must fire at stagger frame");
            Assert.IsTrue(scheduler.IsWindowActive(agentId),
                "SC-008: Window must be active immediately after check fires");

            ShoulderCheckAnimData anim = scheduler.GetAnimData(agentId);
            Assert.AreEqual(agentId, anim.AgentId, "SC-008: AnimData AgentId must match agent");
            Assert.AreEqual(stagger, anim.FireFrame, "SC-008: AnimData FireFrame must equal fire tick");
        }

        /// <summary>
        /// SC-002: Window closes after SHOULDER_CHECK_DURATION ticks. Spec §5.5 SC-002.
        /// </summary>
        [Test]
        public void SC002_Window_ClosesAfterDurationTicks()
        {
            ShoulderCheckScheduler scheduler = new ShoulderCheckScheduler();
            const int agentId = 4;
            const int stagger = (4 * 3 + 7) % 30; // = 19

            scheduler.UpdateAgent(agentId, 10, false, stagger);
            int expiryFrame = scheduler.GetWindowExpiryFrame(agentId);

            // Window must be active for ticks <= expiryFrame
            for (int tick = stagger + 1; tick <= expiryFrame; tick++)
            {
                scheduler.UpdateAgent(agentId, 10, false, tick);
                Assert.IsTrue(scheduler.IsWindowActive(agentId),
                    $"SC-002: Window must still be active at tick {tick} (expiry={expiryFrame})");
            }

            // One tick past expiry, window should close
            scheduler.UpdateAgent(agentId, 10, false, expiryFrame + 1);
            Assert.IsFalse(scheduler.IsWindowActive(agentId),
                "SC-002: Window must be closed after expiry tick passes");
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // BP — Ball Perception (§3.5, §5.6)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests for BallPerceptionEvaluator. Covers BP-001 through BP-006 per §5.6.
    /// Ball is exempt from L_rec (OQ-2). FoV and occlusion tests still apply.
    /// </summary>
    [TestFixture]
    internal class BallPerceptionEvaluatorTests
    {
        private static AgentState[] BuildEmptyAgentStates()
        {
            return new AgentState[22];
        }

        private static PerceptionAgentAttributes[] BuildAgentAttrs(int observerTeam = 0)
        {
            PerceptionAgentAttributes[] attrs = new PerceptionAgentAttributes[22];
            for (int i = 0; i < 22; i++)
            {
                attrs[i] = PerceptionAgentAttributes.CreateDefault();
                attrs[i].TeamId = observerTeam;
            }
            return attrs;
        }

        // ── BP-001 — Ball within FoV receives immediate inclusion (no L_rec) ──────

        /// <summary>
        /// BP-001: Ball at 30° offset, not occluded, EffectiveFoV=160°.
        /// BallVisible=true. BallPerceivedPosition=ground truth. BallStalenessFrames=0.
        /// Spec §5.6 BP-001.
        /// </summary>
        [Test]
        public void BP001_BallWithinFov_IsImmediatelyVisible()
        {
            Vector2 observerPos = Vector2.zero;
            Vector2 facingDir   = new Vector2(1.0f, 0.0f); // East
            float halfAngle     = FovCalculator.ComputeEffectiveFoVHalfAngle(
                FovCalculator.ComputeEffectiveFoV(10, 0.0f)); // 82.5°

            // Ball at 30° offset from East
            float angleRad = 30.0f * Mathf.Deg2Rad;
            Vector3 ballPos3D = new Vector3(Mathf.Cos(angleRad) * 10.0f, Mathf.Sin(angleRad) * 10.0f, 0.1f);
            BallState ballState = BallState.CreateAtPosition(ballPos3D);

            AgentState[]              agentStates = BuildEmptyAgentStates();
            PerceptionAgentAttributes[] agentAttrs  = BuildAgentAttrs();

            BallPerceptionEvaluator.Evaluate(
                observerPos, facingDir, 0, halfAngle,
                ballState, agentStates, agentAttrs,
                new int[0], 0,
                Vector2.zero, 5,
                out bool ballVisible,
                out Vector2 perceivedPos,
                out int staleness);

            Assert.IsTrue(ballVisible, "BP-001: Ball within FoV must be immediately visible");
            Assert.AreEqual(0, staleness, "BP-001: BallStalenessFrames must be 0 when visible");
            Assert.AreEqual(ballPos3D.x, perceivedPos.x, 0.001f,
                "BP-001: PerceivedPosition X must match ground truth");
            Assert.AreEqual(ballPos3D.y, perceivedPos.y, 0.001f,
                "BP-001: PerceivedPosition Y must match ground truth");
        }

        // ── BP-002 — Ball outside FoV is not visible ─────────────────────────────

        /// <summary>
        /// BP-002: Ball at 110° offset. EffectiveFoV=160° (half=80°). 110° &gt; 80°.
        /// BallVisible=false. PerceivedPosition retains last known. Staleness increments.
        /// Spec §5.6 BP-002.
        /// </summary>
        [Test]
        public void BP002_BallOutsideFov_NotVisible_RetainsLastKnown()
        {
            Vector2 observerPos = Vector2.zero;
            Vector2 facingDir   = new Vector2(1.0f, 0.0f);
            float halfAngle     = 80.0f; // 160° FoV

            float angleRad = 110.0f * Mathf.Deg2Rad;
            Vector3 ballPos3D = new Vector3(Mathf.Cos(angleRad) * 10.0f, Mathf.Sin(angleRad) * 10.0f, 0.1f);
            BallState ballState = BallState.CreateAtPosition(ballPos3D);

            Vector2 lastKnownPos = new Vector2(30.0f, 20.0f);

            AgentState[]              agentStates = BuildEmptyAgentStates();
            PerceptionAgentAttributes[] agentAttrs  = BuildAgentAttrs();

            BallPerceptionEvaluator.Evaluate(
                observerPos, facingDir, 0, halfAngle,
                ballState, agentStates, agentAttrs,
                new int[0], 0,
                lastKnownPos, 2,
                out bool ballVisible,
                out Vector2 perceivedPos,
                out int staleness);

            Assert.IsFalse(ballVisible, "BP-002: Ball at 110° must not be visible (half-angle=80°)");
            Assert.AreEqual(3, staleness, "BP-002: BallStalenessFrames must increment from 2 to 3");
            Assert.AreEqual(lastKnownPos.x, perceivedPos.x, 0.001f,
                "BP-002: PerceivedPosition must retain last known X when invisible");
            Assert.AreEqual(lastKnownPos.y, perceivedPos.y, 0.001f,
                "BP-002: PerceivedPosition must retain last known Y when invisible");
        }

        // ── BP-004 — BallStalenessFrames increments each invisible tick ───────────

        /// <summary>
        /// BP-004: Ball invisible for 4 consecutive ticks. Staleness = 1,2,3,4 progressively.
        /// Spec §5.6 BP-004.
        /// </summary>
        [Test]
        public void BP004_BallStaleness_IncrementsEachInvisibleTick()
        {
            Vector2 observerPos = Vector2.zero;
            Vector2 facingDir   = new Vector2(1.0f, 0.0f);
            float halfAngle     = 80.0f;

            // Ball behind observer — always invisible
            Vector3 ballPos3D = new Vector3(-20.0f, 0.0f, 0.1f);
            BallState ballState = BallState.CreateAtPosition(ballPos3D);

            AgentState[]              agentStates = BuildEmptyAgentStates();
            PerceptionAgentAttributes[] agentAttrs  = BuildAgentAttrs();

            int prevStaleness = 0;
            Vector2 prevPos   = new Vector2(5.0f, 5.0f);

            for (int tick = 1; tick <= 4; tick++)
            {
                BallPerceptionEvaluator.Evaluate(
                    observerPos, facingDir, 0, halfAngle,
                    ballState, agentStates, agentAttrs,
                    new int[0], 0,
                    prevPos, prevStaleness,
                    out bool ballVisible,
                    out Vector2 perceivedPos,
                    out int staleness);

                Assert.IsFalse(ballVisible, $"BP-004: Ball must be invisible at tick {tick}");
                Assert.AreEqual(tick, staleness,
                    $"BP-004: BallStalenessFrames must be {tick} at tick {tick}");

                prevStaleness = staleness;
                prevPos       = perceivedPos;
            }
        }

        // ── BP-005 — BallStalenessFrames resets on re-visibility ─────────────────

        /// <summary>
        /// BP-005: Ball invisible 3 ticks (Staleness=3). Ball re-enters visibility at tick 4.
        /// Expected: BallStalenessFrames=0. Spec §5.6 BP-005.
        /// </summary>
        [Test]
        public void BP005_BallStaleness_ResetsOnRevisibility()
        {
            Vector2 observerPos = Vector2.zero;
            Vector2 facingDir   = new Vector2(1.0f, 0.0f);
            float halfAngle     = 80.0f;

            // Ball now in front — visible
            Vector3 ballPos3D = new Vector3(10.0f, 0.0f, 0.1f);
            BallState ballState = BallState.CreateAtPosition(ballPos3D);

            AgentState[]              agentStates = BuildEmptyAgentStates();
            PerceptionAgentAttributes[] agentAttrs  = BuildAgentAttrs();

            BallPerceptionEvaluator.Evaluate(
                observerPos, facingDir, 0, halfAngle,
                ballState, agentStates, agentAttrs,
                new int[0], 0,
                new Vector2(30.0f, 20.0f), 3, // prevStaleness=3 from before
                out bool ballVisible,
                out _,
                out int staleness);

            Assert.IsTrue(ballVisible, "BP-005: Ball in FoV must be visible");
            Assert.AreEqual(0, staleness,
                "BP-005: BallStalenessFrames must reset to 0 on re-visibility");
        }

        // ── BP-006 — Last known ball position retained during occlusion ───────────

        /// <summary>
        /// BP-006: Ball at (30,20) at tick N. Then invisible for multiple ticks.
        /// PerceivedPosition must remain (30,20). Spec §5.6 BP-006.
        /// </summary>
        [Test]
        public void BP006_LastKnownPosition_RetainedDuringInvisibility()
        {
            Vector2 observerPos   = Vector2.zero;
            Vector2 facingDir     = new Vector2(1.0f, 0.0f);
            float halfAngle       = 80.0f;
            Vector2 lastKnownPos  = new Vector2(30.0f, 20.0f);

            // Ball invisible (behind observer)
            BallState ballState = BallState.CreateAtPosition(new Vector3(-15.0f, 0.0f, 0.1f));
            AgentState[]              agentStates = BuildEmptyAgentStates();
            PerceptionAgentAttributes[] agentAttrs  = BuildAgentAttrs();

            for (int tick = 1; tick <= 4; tick++)
            {
                BallPerceptionEvaluator.Evaluate(
                    observerPos, facingDir, 0, halfAngle,
                    ballState, agentStates, agentAttrs,
                    new int[0], 0,
                    lastKnownPos, tick - 1,
                    out bool _,
                    out Vector2 perceivedPos,
                    out int _);

                Assert.AreEqual(lastKnownPos.x, perceivedPos.x, 0.001f,
                    $"BP-006: PerceivedPosition X must remain (30) at invisible tick {tick}");
                Assert.AreEqual(lastKnownPos.y, perceivedPos.y, 0.001f,
                    $"BP-006: PerceivedPosition Y must remain (20) at invisible tick {tick}");
            }
        }
    }

    // ════════════════════════════════════════════════════════════════════════════
    // SNAP — Snapshot / PerceptionDiagnostics field correctness (§3.7, §5.9)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Tests verifying PerceptionConstants, FovCalculator diagnostics fields, and
    /// derived values used in snapshot assembly. Covers SNAP-010 and related §5.9 cases
    /// that can be tested without a full PerceptionSystem orchestrator.
    /// </summary>
    [TestFixture]
    internal class SnapshotDiagnosticsTests
    {
        // ── SNAP-010 — EffectiveFoVAngle reflects pressure-narrowed value ─────────

        /// <summary>
        /// SNAP-010: Decisions=15, PressureScalar=0.4.
        /// Derivation: bonus=(15/20)×10=7.5°. Reduction=0.4×30=12°. 160+7.5−12=155.5°.
        /// Spec §5.9 SNAP-010.
        /// </summary>
        [Test]
        public void SNAP010_EffectiveFovAngle_ReflectsPostPressureValue()
        {
            float effectiveFoV = FovCalculator.ComputeEffectiveFoV(15, 0.4f);
            Assert.AreEqual(155.5f, effectiveFoV, 0.01f,
                "SNAP-010: EffectiveFoVAngle must be 155.5° (post-pressure, Decisions=15, PS=0.4)");
        }

        // ── SNAP — PerceivedAgent.ConfidenceScore = 1.0 at Stage 0 ───────────────

        /// <summary>
        /// SNAP-006: Stage 0 binary confidence — ConfidenceScore must be 1.0 for confirmed entities.
        /// Spec §5.9 SNAP-006.
        /// </summary>
        [Test]
        public void SNAP006_PerceivedAgent_ConfidenceScore_Is1AtStage0()
        {
            // Verify the constant used for Stage 0 confidence
            PerceivedAgent agent = new PerceivedAgent
            {
                AgentId           = 1,
                PerceivedPosition = new Vector2(10.0f, 5.0f),
                PerceivedVelocity = Vector2.zero,
                ConfidenceScore   = 1.0f // Stage 0 binary confidence
            };

            Assert.AreEqual(1.0f, agent.ConfidenceScore, float.Epsilon,
                "SNAP-006: Stage 0 confirmed entity must have ConfidenceScore=1.0");
        }

        // ── SNAP — PerceptionConstants sanity checks ──────────────────────────────

        /// <summary>
        /// Constants must satisfy the spec-defined invariants for FoV range [120°,170°].
        /// Spec §3.10.
        /// </summary>
        [Test]
        public void Constants_FovRange_SatisfiesSpecInvariants()
        {
            float minPossibleFoV = PerceptionConstants.MIN_FOV_ANGLE;
            float maxPossibleFoV = PerceptionConstants.BASE_FOV_ANGLE
                                 + PerceptionConstants.MAX_FOV_BONUS_ANGLE;

            Assert.AreEqual(120.0f, minPossibleFoV, 0.001f,
                "MIN_FOV_ANGLE must be 120°");
            Assert.AreEqual(170.0f, maxPossibleFoV, 0.001f,
                "BASE_FOV_ANGLE + MAX_FOV_BONUS_ANGLE must equal 170°");
        }

        /// <summary>
        /// AGENT_BODY_RADIUS must be positive. MIN_SHADOW_HALF_ANGLE must be positive.
        /// Spec §3.10.
        /// </summary>
        [Test]
        public void Constants_OcclusionGeometry_ArePositive()
        {
            Assert.Greater(PerceptionConstants.AGENT_BODY_RADIUS, 0.0f,
                "AGENT_BODY_RADIUS must be positive");
            Assert.Greater(PerceptionConstants.MIN_SHADOW_HALF_ANGLE, 0.0f,
                "MIN_SHADOW_HALF_ANGLE must be positive");
        }

        /// <summary>
        /// L_MIN &lt; L_MAX and both within sensible tick ranges. Spec §3.10.
        /// </summary>
        [Test]
        public void Constants_LrecBounds_LMinLessThanLMax()
        {
            Assert.Less(PerceptionConstants.L_MIN, PerceptionConstants.L_MAX,
                "L_MIN must be strictly less than L_MAX");
            Assert.GreaterOrEqual(PerceptionConstants.L_MIN, 1,
                "L_MIN must be at least 1 tick");
        }

        /// <summary>
        /// BASE_FOV_HALF_ANGLE is derived correctly from BASE_FOV_ANGLE.
        /// PERIPHERAL_ARC_INNER_BOUND = BASE_FOV_HALF_ANGLE / 2. Spec §3.10.
        /// </summary>
        [Test]
        public void Constants_DerivedFovValues_AreCorrect()
        {
            Assert.AreEqual(PerceptionConstants.BASE_FOV_ANGLE / 2.0f,
                PerceptionConstants.BASE_FOV_HALF_ANGLE, 0.001f,
                "BASE_FOV_HALF_ANGLE must equal BASE_FOV_ANGLE / 2");
            Assert.AreEqual(PerceptionConstants.BASE_FOV_HALF_ANGLE / 2.0f,
                PerceptionConstants.PERIPHERAL_ARC_INNER_BOUND, 0.001f,
                "PERIPHERAL_ARC_INNER_BOUND must equal BASE_FOV_HALF_ANGLE / 2");
            // With defaults: 160/2=80°; 80/2=40°
            Assert.AreEqual(80.0f, PerceptionConstants.BASE_FOV_HALF_ANGLE, 0.001f,
                "BASE_FOV_HALF_ANGLE must be 80° with default BASE_FOV_ANGLE=160°");
            Assert.AreEqual(40.0f, PerceptionConstants.PERIPHERAL_ARC_INNER_BOUND, 0.001f,
                "PERIPHERAL_ARC_INNER_BOUND must be 40° with default BASE_FOV_HALF_ANGLE=80°");
        }
    }

    // ────────────────────────────────────────────────────────────────────────────
    // §5.11 Integration Tests (Play Mode; require full 22-agent match simulation)
    // ────────────────────────────────────────────────────────────────────────────

    [TestFixture]
    internal sealed class PerceptionSystemIntegrationTests
    {
        // ── §5.11.1 AgentState → Perception (AM #2 boundary) ─────────────────────

        /// <summary>IT-AM-001: FoV orientation follows AgentState.FacingDirection. §5.11.1.</summary>
        [Test]
        public void FovOrientation_FollowsAgentStateFacingDirection()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode 22-agent simulation with AgentMovement #2 wired — IT-AM-001");
        }

        /// <summary>IT-AM-002: FoV recomputed fresh each heartbeat from current FacingDirection. §5.11.1.</summary>
        [Test]
        public void Fov_RecomputedFreshEachHeartbeat()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode 22-agent simulation — IT-AM-002");
        }

        /// <summary>IT-AM-003: Decisions attribute read correctly from PlayerAttributes. §5.11.1.</summary>
        [Test]
        public void DecisionsAttribute_ReadCorrectlyFromPlayerAttributes()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode with PlayerAttributes injection — IT-AM-003");
        }

        /// <summary>IT-AM-004: Anticipation attribute read correctly for shoulder check interval. §5.11.1.</summary>
        [Test]
        public void AnticipationAttribute_ReadCorrectlyForShoulderCheckInterval()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode with PlayerAttributes injection — IT-AM-004");
        }

        // ── §5.11.2 BallState → Perception (Ball Physics #1 boundary) ────────────

        /// <summary>IT-BP-001: BallState.Position 3D → 2D projection. §5.11.2.</summary>
        [Test]
        public void BallPosition_3DTo2DProjection_Correct()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode with BallPhysics #1 wired — IT-BP-001");
        }

        /// <summary>IT-BP-002: Ball contact forced refresh reaches correct agents only. §5.11.2.</summary>
        [Test]
        public void BallContactForcedRefresh_ReachesCorrectAgentsOnly()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode with EventBus BallContactEvent — IT-BP-002");
        }

        // ── §5.11.3 Spatial Hash → Perception (Collision System #3 boundary) ──────

        /// <summary>IT-CS-001: QueryRadius returns correct candidate count (self excluded). §5.11.3.</summary>
        [Test]
        public void QueryRadius_ReturnsCorrectCandidateCount_SelfExcluded()
        {
            Assert.Ignore("Stage 0+1: requires SpatialHashGrid wired into PerceptionSystem.Tick — IT-CS-001");
        }

        /// <summary>IT-CS-002: Pressure query uses distinct 3.0m radius call. §5.11.3.</summary>
        [Test]
        public void PressureQuery_UsesDistinct3mRadiusCall()
        {
            Assert.Ignore("Stage 0+1: requires SpatialHashGrid wired — IT-CS-002");
        }

        /// <summary>IT-CS-003: Perception never writes to spatial hash (read-only contract). §5.11.3.</summary>
        [Test]
        public void Perception_NeverWritesToSpatialHash()
        {
            Assert.Ignore("Stage 0+1: requires SpatialHashGrid wired; verifies no writes during Perception.Tick — IT-CS-003");
        }

        // ── §5.11.4 Multi-Agent and Long-Duration Scenarios ───────────────────────

        /// <summary>IT-FULL-001: Elite midfielder vs novice defender — exact measurable differences. §5.11.4.</summary>
        [Test]
        public void EliteMidfielder_VsNoviceDefender_MeasurableFovDifference()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode 22-agent scenario with scripted agent attributes — IT-FULL-001");
        }

        /// <summary>IT-FULL-002: Awareness builds over first several ticks (match cold-start). §5.11.4.</summary>
        [Test]
        public void Awareness_BuildsOverFirstTicks_ColdStart()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode multi-tick simulation — IT-FULL-002");
        }

        /// <summary>IT-FULL-003: Agent behind wall of opponents — correctly perception-isolated. §5.11.4.</summary>
        [Test]
        public void AgentBehindOpponentWall_IsPerceptionIsolated()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode with OcclusionFilter wired into full pipeline — IT-FULL-003");
        }

        /// <summary>IT-FULL-004: 22-agent full heartbeat — all snapshots produced, zero nulls. §5.11.4.</summary>
        [Test]
        public void FullHeartbeat_22Agents_AllSnapshotsProducedNoNulls()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode PerceptionSystem.Tick with all 22 agents — IT-FULL-004");
        }

        /// <summary>IT-FULL-005: L_rec tracking does not leak memory over extended simulation. §5.11.4.</summary>
        [Test]
        public void LrecTracking_NoMemoryLeakOverExtendedSimulation()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode memory profiler over ≥ 100 ticks — IT-FULL-005");
        }

        /// <summary>IT-FULL-006: Determinism — 100 heartbeats, identical inputs → byte-identical snapshots. §5.11.4.</summary>
        [Test]
        public void Determinism_100Heartbeats_IdenticalInputsProduceBytIdenticalSnapshots()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode + Deterministic Simulation #16 digest comparison — IT-FULL-006");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-31 | —      | Initial implementation. FOV-001..008, OCC-001..007+009, LR-001..008, SC-002..005+008, BP-001..006, SNAP-006+010 + constants. 31 tests total. |
// | 1.1     | 2026-06-01 | —      | Add §5.11 integration test stubs IT-AM-001..004, IT-BP-001..002, IT-CS-001..003, IT-FULL-001..006 (15 stubs; Stage 0+1 Assert.Ignore). |
// | 1.2     | 2026-06-12 | —      | Dotnet-CI quarantine adjudication (both TEST-DEFECT): OCC-005 expectation 11.31° was arctan(0.2) — spec §A.4/App-B mandate arcsin(0.4/2) = 11.537° (production correct); LR-001 variant asserted first-call confirmation unconditionally, ignoring the §3.3.4 additive noise term (0/+1) — rewritten to derive L_rec_final via ComputeLRec and assert confirmation exactly at the L_rec-th tick. |
#endregion
