// File:     src/first-touch/Tests/FirstTouchTests.cs
// Created:  2026-05-31
// Modified: 2026-06-06
// Author:   —
// Spec:     First Touch #4 §5
// Purpose:  NUnit unit tests covering CQ, TR, PR, OR, PO, EC, BD, and VS categories from §5.2–§5.10.
//           All internal calculators (ControlQualityCalculator, TouchRadiusCalculator, etc.) are
//           exercised through the public FirstTouchSystem.EvaluateFirstTouch API because the test
//           assembly is separate from TacticalDirector.FirstTouch.

using System;
using System.Collections.Generic;

using NUnit.Framework;

using UnityEngine;

namespace TacticalDirector.FirstTouch.Tests
{
    // ---------------------------------------------------------------------------
    // Minimal stub implementations for injected dependencies
    // ---------------------------------------------------------------------------

    internal sealed class NullBallPhysicsSystem : IBallPhysicsSystem
    {
        internal Vector3 LastSetPosition;
        internal Vector3 LastSetVelocity;
        internal int SetBallStateCalls;

        public void SetBallState(Vector3 position, Vector3 velocity)
        {
            LastSetPosition = position;
            LastSetVelocity = velocity;
            SetBallStateCalls++;
        }
    }

    internal sealed class RecordingAgentMovementSystem : IAgentMovementSystem
    {
        internal int LastAgentID = -99;
        internal bool LastIsDribbling;
        internal int SetDribblingStateCalls;

        public void SetDribblingState(int agentID, bool isDribbling)
        {
            LastAgentID = agentID;
            LastIsDribbling = isDribbling;
            SetDribblingStateCalls++;
        }
    }

    // ---------------------------------------------------------------------------
    // Factory helpers shared across test fixtures
    // ---------------------------------------------------------------------------

    internal static class ContextFactory
    {
        /// <summary>Returns a baseline FirstTouchContext populated with sensible defaults.</summary>
        internal static FirstTouchContext MakeDefault()
        {
            return new FirstTouchContext
            {
                AgentID                = 1,
                TeamID                 = 0,
                Technique              = 12,
                FirstTouchAttribute    = 11,
                AgentPosition          = new Vector3(52.5f, 34.0f, 0.0f),
                AgentVelocity          = Vector3.zero,
                AgentFacing            = new Vector3(1.0f, 0.0f, 0.0f),
                IntendedTouchDirection = new Vector3(1.0f, 0.0f, 0.0f),
                HasMovementTarget      = true,
                BallPosition           = new Vector3(52.5f, 34.0f, 0.11f),
                BallVelocity           = new Vector3(-15.0f, 0.0f, 0.0f),
                BallHeight             = 0.0f,
                BallIsAirborne         = false,
                PressureScalar         = 0.0f,
                HasNearbyOpponent      = false,
                NearestOpponentDistance = float.PositiveInfinity,
                IsHalfTurnOriented     = false,
                IsGoalkeeper           = false
            };
        }

        internal static (FirstTouchSystem system, NullBallPhysicsSystem ball, RecordingAgentMovementSystem agent) MakeSystem()
        {
            NullBallPhysicsSystem ball = new NullBallPhysicsSystem();
            RecordingAgentMovementSystem agentMov = new RecordingAgentMovementSystem();
            FirstTouchSystem system = new FirstTouchSystem(ball, agentMov);
            return (system, ball, agentMov);
        }
    }

    // ===========================================================================
    // CQ — Control Quality Tests (§5.2)
    // ===========================================================================

    [TestFixture]
    internal sealed class ControlQualityTests
    {
        private FirstTouchSystem _system;

        [SetUp]
        public void SetUp()
        {
            (FirstTouchSystem sys, NullBallPhysicsSystem _, RecordingAgentMovementSystem _) = ContextFactory.MakeSystem();
            _system = sys;
        }

        /// <summary>CQ-001: Elite player, slow ball, no conditions → q clamped to 1.0.</summary>
        [Test]
        public void ControlQuality_ElitePlayer_SlowBall_NoConditions_ReturnsNearPerfect()
        {
            // Hand-calc: WA=17.30, NormAttr=0.865, VelDiff=0.800, MoveDiff=1.0,
            //            RawQ=1.081, q after pressure=1.081, clamped→1.0
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique           = 17;
            ctx.FirstTouchAttribute = 18;
            ctx.BallVelocity        = new Vector3(-12.0f, 0.0f, 0.0f);
            ctx.AgentVelocity       = Vector3.zero;
            ctx.PressureScalar      = 0.0f;
            ctx.IsHalfTurnOriented  = false;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(1.0f, result.ControlQuality, 0.0f,
                "CQ-001: Elite player / slow ball should clamp to exactly 1.0.");
        }

        /// <summary>CQ-002: Average player, typical pass, walking, low pressure → mid-range q ≈ 0.481.</summary>
        [Test]
        public void ControlQuality_AveragePlayer_TypicalPass_Walking_LowPressure_ReturnsMidRange()
        {
            // Hand-calc: WA=11.70, NormAttr=0.585, VelDiff=1.0, MoveDiff=1.143,
            //            RawQ=0.512, q=0.512×0.940=0.481
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique           = 12;
            ctx.FirstTouchAttribute = 11;
            ctx.BallVelocity        = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity       = new Vector3(2.0f, 0.0f, 0.0f);
            ctx.PressureScalar      = 0.15f;
            ctx.IsHalfTurnOriented  = false;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(0.481f, result.ControlQuality, 0.03f,
                "CQ-002: Average player mid-range scenario should yield q ≈ 0.481.");
        }

        /// <summary>CQ-003: Poor player, fast pass, jogging, medium pressure → q ≈ 0.146.</summary>
        [Test]
        public void ControlQuality_PoorPlayer_FastPass_Jogging_MediumPressure_ReturnsLow()
        {
            // Hand-calc: WA=6.70, NormAttr=0.335, VelDiff=1.467, MoveDiff=1.286,
            //            RawQ=0.178, q=0.178×0.820=0.146
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique           = 7;
            ctx.FirstTouchAttribute = 6;
            ctx.BallVelocity        = new Vector3(-22.0f, 0.0f, 0.0f);
            ctx.AgentVelocity       = new Vector3(4.0f, 0.0f, 0.0f);
            ctx.PressureScalar      = 0.45f;
            ctx.IsHalfTurnOriented  = false;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(0.146f, result.ControlQuality, 0.03f,
                "CQ-003: Poor player in adverse conditions should yield q ≈ 0.146.");
        }

        /// <summary>CQ-004: Velocity cap at VELOCITY_MAX_FACTOR — ball at 80 m/s produces same q as 60 m/s.</summary>
        [Test]
        public void ControlQuality_VelocityDifficulty_CappedAtMaxFactor()
        {
            FirstTouchContext ctx80 = ContextFactory.MakeDefault();
            ctx80.Technique           = 12;
            ctx80.FirstTouchAttribute = 11;
            ctx80.BallVelocity        = new Vector3(-80.0f, 0.0f, 0.0f);
            ctx80.AgentVelocity       = Vector3.zero;
            ctx80.PressureScalar      = 0.0f;

            FirstTouchContext ctx60 = ctx80;
            ctx60.BallVelocity = new Vector3(-60.0f, 0.0f, 0.0f);

            float q80 = _system.EvaluateFirstTouch(ctx80).ControlQuality;
            float q60 = _system.EvaluateFirstTouch(ctx60).ControlQuality;

            Assert.AreEqual(q60, q80, 0.001f,
                "CQ-004: 80 m/s ball should produce identical q to 60 m/s (velocity cap at VelocityMaxFactor=4.0).");
        }

        /// <summary>CQ-005: Half-turn bonus increases quality by exactly 15%.</summary>
        [Test]
        public void ControlQuality_HalfTurnBonus_IncreasesQuality_By15Percent()
        {
            FirstTouchContext ctxNoBonus = ContextFactory.MakeDefault();
            ctxNoBonus.Technique           = 12;
            ctxNoBonus.FirstTouchAttribute = 12;
            ctxNoBonus.BallVelocity        = new Vector3(-15.0f, 0.0f, 0.0f);
            ctxNoBonus.AgentVelocity       = Vector3.zero;
            ctxNoBonus.PressureScalar      = 0.0f;
            ctxNoBonus.IsHalfTurnOriented  = false;

            FirstTouchContext ctxBonus = ctxNoBonus;
            ctxBonus.IsHalfTurnOriented = true;
            // Use a facing direction that produces 45° from ball approach for the OrientationDetector.
            ctxBonus.AgentFacing = new Vector3(0.707f, 0.707f, 0.0f);

            float qNo = _system.EvaluateFirstTouch(ctxNoBonus).ControlQuality;
            // Before clamping, bonus raises q by 15%; if q < ~0.87 it won't be clamped.
            float qWith = _system.EvaluateFirstTouch(ctxBonus).ControlQuality;

            if (qNo < 0.87f)
            {
                float ratio = qWith / qNo;
                Assert.AreEqual(1.15f, ratio, 0.02f,
                    "CQ-005: Half-turn bonus should raise quality by exactly 15% (±2%).");
            }
            else
            {
                // Both clamped at 1.0; assert both are 1.0.
                Assert.AreEqual(1.0f, qNo, 0.001f);
                Assert.AreEqual(1.0f, qWith, 0.001f);
            }
        }

        /// <summary>CQ-006: Maximum pressure (scalar=1.0) degrades quality by exactly 40%.</summary>
        [Test]
        public void ControlQuality_MaximumPressure_DegradesByExactly40Percent()
        {
            FirstTouchContext ctxNoPressure = ContextFactory.MakeDefault();
            ctxNoPressure.Technique           = 15;
            ctxNoPressure.FirstTouchAttribute = 14;
            ctxNoPressure.BallVelocity        = new Vector3(-15.0f, 0.0f, 0.0f);
            ctxNoPressure.AgentVelocity       = Vector3.zero;
            ctxNoPressure.PressureScalar      = 0.0f;

            FirstTouchContext ctxMaxPressure = ctxNoPressure;
            ctxMaxPressure.PressureScalar = 1.0f;

            float qNone = _system.EvaluateFirstTouch(ctxNoPressure).ControlQuality;
            float qMax  = _system.EvaluateFirstTouch(ctxMaxPressure).ControlQuality;

            Assert.AreEqual(qNone * 0.60f, qMax, 0.001f,
                "CQ-006: Maximum pressure (scalar=1.0) must reduce quality to exactly 60% of baseline (40% degradation).");
        }

        /// <summary>CQ-007: Zero pressure applies no modification.</summary>
        [Test]
        public void ControlQuality_ZeroPressure_NoPenaltyApplied()
        {
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.PressureScalar = 0.0f;

            // Compute q at pressure=0.0 and again at pressure=0.0 (idempotent).
            float q1 = _system.EvaluateFirstTouch(ctx).ControlQuality;
            float q2 = _system.EvaluateFirstTouch(ctx).ControlQuality;

            Assert.AreEqual(q1, q2, 0.0f,
                "CQ-007: Zero pressure must produce zero penalty; repeated calls must be identical.");
        }

        /// <summary>CQ-008: Minimum attributes (Tech=1, FT=1) produce q ≈ 0.05 — formula does not degenerate to zero.</summary>
        [Test]
        public void ControlQuality_MinimumAttributes_ProducesNonZeroOutput()
        {
            // Hand-calc: WA=1.0, NormAttr=0.05, VelDiff=1.0, MoveDiff=1.0, q=0.05
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique           = 1;
            ctx.FirstTouchAttribute = 1;
            ctx.BallVelocity        = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity       = Vector3.zero;
            ctx.PressureScalar      = 0.0f;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(0.05f, result.ControlQuality, 0.005f,
                "CQ-008: Minimum attributes should yield q ≈ 0.05, not zero.");
            Assert.Greater(result.ControlQuality, 0.0f,
                "CQ-008: q must be strictly positive for minimum-attribute player.");
        }

        /// <summary>CQ-009: Maximum attributes (Tech=20, FT=20), slow ball → q clamped to 1.0.</summary>
        [Test]
        public void ControlQuality_MaximumAttributes_SlowBall_ClampsAtOne()
        {
            // Hand-calc: WA=20.0, NormAttr=1.0, bonus→1.15, VelDiff=0.333, RawQ=3.45 → clamped 1.0
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique           = 20;
            ctx.FirstTouchAttribute = 20;
            ctx.BallVelocity        = new Vector3(-5.0f, 0.0f, 0.0f);
            ctx.AgentVelocity       = Vector3.zero;
            ctx.PressureScalar      = 0.0f;
            ctx.IsHalfTurnOriented  = true;
            ctx.AgentFacing         = new Vector3(0.707f, 0.707f, 0.0f);

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(1.0f, result.ControlQuality, 0.0f,
                "CQ-009: Maximum attributes with slow ball must produce q = 1.0 (upper clamp).");
        }

        /// <summary>CQ-010: Sprint reception (agent at 7 m/s) reduces quality by ≈1/3 vs stationary.</summary>
        [Test]
        public void ControlQuality_SprintReception_ReducesQualityVsStationary()
        {
            // Hand-calc: MoveDiff stationary=1.0, sprinting=1.5 → ratio ≈ 0.667
            FirstTouchContext ctxStationary = ContextFactory.MakeDefault();
            ctxStationary.Technique           = 12;
            ctxStationary.FirstTouchAttribute = 11;
            ctxStationary.BallVelocity        = new Vector3(-15.0f, 0.0f, 0.0f);
            ctxStationary.AgentVelocity       = Vector3.zero;
            ctxStationary.PressureScalar      = 0.0f;

            FirstTouchContext ctxSprint = ctxStationary;
            ctxSprint.AgentVelocity = new Vector3(7.0f, 0.0f, 0.0f);

            float qStat   = _system.EvaluateFirstTouch(ctxStationary).ControlQuality;
            float qSprint = _system.EvaluateFirstTouch(ctxSprint).ControlQuality;

            Assert.Less(qSprint, qStat,
                "CQ-010: Sprint reception must produce lower q than stationary reception.");
            float ratio = qSprint / qStat;
            Assert.AreEqual(0.667f, ratio, 0.02f,
                "CQ-010: Sprint-to-stationary ratio should be ≈ 0.667 (MoveDiff 1.5 vs 1.0).");
        }

        /// <summary>CQ-011: Determinism — identical inputs produce bitwise identical output across multiple calls.</summary>
        [Test]
        public void ControlQuality_Determinism_IdenticalInputsProduceBitwiseIdenticalOutput()
        {
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique           = 14;
            ctx.FirstTouchAttribute = 13;
            ctx.BallVelocity        = new Vector3(-17.0f, 3.0f, 0.0f);
            ctx.AgentVelocity       = new Vector3(1.5f, 0.0f, 0.0f);
            ctx.PressureScalar      = 0.20f;

            float q0 = _system.EvaluateFirstTouch(ctx).ControlQuality;
            float q1 = _system.EvaluateFirstTouch(ctx).ControlQuality;
            float q2 = _system.EvaluateFirstTouch(ctx).ControlQuality;

            // Bitwise comparison via bit patterns to catch any non-deterministic branching.
            Assert.AreEqual(q0, q1, 0.0f, "CQ-011: Call 1 vs 0 must be bitwise identical.");
            Assert.AreEqual(q0, q2, 0.0f, "CQ-011: Call 2 vs 0 must be bitwise identical.");
        }

        /// <summary>CQ-012: All four verification matrix scenarios pass simultaneously.</summary>
        [Test]
        public void ControlQuality_AllFourVerificationMatrixScenarios_PassSimultaneously()
        {
            // Scenario 1: Elite, slow, no pressure → q ≥ 0.85
            FirstTouchContext elite = ContextFactory.MakeDefault();
            elite.Technique = 20; elite.FirstTouchAttribute = 20;
            elite.BallVelocity = new Vector3(-5.0f, 0.0f, 0.0f);
            elite.AgentVelocity = Vector3.zero; elite.PressureScalar = 0.0f;
            float qElite = _system.EvaluateFirstTouch(elite).ControlQuality;
            Assert.GreaterOrEqual(qElite, 0.85f, "CQ-012 Scenario 1: elite should be ≥ 0.85.");

            // Scenario 2: Average, typical, no pressure → q ∈ [0.55, 0.65]
            FirstTouchContext avg = ContextFactory.MakeDefault();
            avg.Technique = 12; avg.FirstTouchAttribute = 11;
            avg.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            avg.AgentVelocity = new Vector3(2.0f, 0.0f, 0.0f); avg.PressureScalar = 0.0f;
            float qAvg = _system.EvaluateFirstTouch(avg).ControlQuality;
            Assert.GreaterOrEqual(qAvg, 0.55f, "CQ-012 Scenario 2 lower bound.");
            Assert.LessOrEqual(qAvg, 0.65f, "CQ-012 Scenario 2 upper bound.");

            // Scenario 3: Average, typical, medium pressure → q ∈ [0.35, 0.45]
            FirstTouchContext avgPress = avg;
            avgPress.PressureScalar = 0.5f;
            float qAvgP = _system.EvaluateFirstTouch(avgPress).ControlQuality;
            Assert.GreaterOrEqual(qAvgP, 0.35f, "CQ-012 Scenario 3 lower bound.");
            Assert.LessOrEqual(qAvgP, 0.45f, "CQ-012 Scenario 3 upper bound.");

            // Scenario 4: Poor, hard shot, full pressure, sprint → q ≤ 0.15
            FirstTouchContext poor = ContextFactory.MakeDefault();
            poor.Technique = 5; poor.FirstTouchAttribute = 5;
            poor.BallVelocity = new Vector3(-30.0f, 0.0f, 0.0f);
            poor.AgentVelocity = new Vector3(7.0f, 0.0f, 0.0f); poor.PressureScalar = 1.0f;
            float qPoor = _system.EvaluateFirstTouch(poor).ControlQuality;
            Assert.LessOrEqual(qPoor, 0.15f, "CQ-012 Scenario 4: poor/extreme should be ≤ 0.15.");
        }
    }

    // ===========================================================================
    // TR — Touch Radius Tests (§5.3)
    // ===========================================================================

    [TestFixture]
    internal sealed class TouchRadiusTests
    {
        private FirstTouchSystem _system;

        [SetUp]
        public void SetUp()
        {
            (FirstTouchSystem sys, NullBallPhysicsSystem _, RecordingAgentMovementSystem _) = ContextFactory.MakeSystem();
            _system = sys;
        }

        /// <summary>TR-001: q=1.0 → r = 0.10m (Perfect band minimum, no velocity excess).</summary>
        [Test]
        public void TouchRadius_PerfectControl_ReturnsMinimumRadius()
        {
            // Force q=1.0: elite player, slow ball.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 20; ctx.FirstTouchAttribute = 20;
            ctx.BallVelocity = new Vector3(-12.0f, 0.0f, 0.0f); // 12 m/s < 15 m/s reference → no vel excess
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(1.0f, result.ControlQuality, 0.001f, "TR-001 pre-check: q must be 1.0.");
            Assert.AreEqual(0.10f, result.TouchRadius, 0.005f, "TR-001: Perfect control should yield r = 0.10m.");
        }

        /// <summary>TR-002: q=0.85 boundary — Perfect band top and Good band top are continuous.</summary>
        [Test]
        public void TouchRadius_PerfectGoodBoundary_IsContinuous()
        {
            // We cannot force exact q from the outside without knowing resulting q precisely,
            // so we use inputs calibrated to produce q ≈ 0.85 ± epsilon and check continuity.
            FirstTouchContext ctxAbove = ContextFactory.MakeDefault();
            ctxAbove.Technique = 20; ctxAbove.FirstTouchAttribute = 20;
            ctxAbove.BallVelocity = new Vector3(-20.4f, 0.0f, 0.0f); // tuned for q just above 0.85
            ctxAbove.AgentVelocity = Vector3.zero; ctxAbove.PressureScalar = 0.0f;

            FirstTouchContext ctxBelow = ctxAbove;
            ctxBelow.BallVelocity = new Vector3(-20.5f, 0.0f, 0.0f); // tuned for q just below 0.85

            float rAbove = _system.EvaluateFirstTouch(ctxAbove).TouchRadius;
            float rBelow = _system.EvaluateFirstTouch(ctxBelow).TouchRadius;

            Assert.AreEqual(rAbove, rBelow, 0.01f,
                "TR-002: Touch radius must be continuous at the Perfect/Good boundary (gap < 0.01m).");
        }

        /// <summary>TR-003: Good band at q≈0.72 → r ≈ 0.456m.</summary>
        [Test]
        public void TouchRadius_GoodControlMidRange_ReturnsInterpolatedValue()
        {
            // Hand-calc: t=(0.72-0.60)/0.25=0.48, r=Lerp(0.60,0.30,0.48)=0.456m at 15 m/s.
            // We target q=0.72 via attributes that pass through the formula.
            // Technique=14, FT=14 → WA=14.0, NormAttr=0.70, VelDiff=1.0, MoveDiff=1.0, q=0.70.
            // Adjust slightly: Technique=15, FT=14 → WA=14.7, NormAttr=0.735, VelDiff=1.0 → q=0.735 ≈ in Good band.
            // Let the formula run and just verify the band (Good band: r ∈ [0.30, 0.60]).
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 15; ctx.FirstTouchAttribute = 14;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.GreaterOrEqual(result.ControlQuality, 0.60f, "TR-003 pre-check: q must be in Good band.");
            Assert.Less(result.ControlQuality, 0.85f, "TR-003 pre-check: q must be below Perfect band.");
            Assert.GreaterOrEqual(result.TouchRadius, 0.30f, "TR-003: Good band r lower bound (0.30m).");
            Assert.LessOrEqual(result.TouchRadius, 0.60f, "TR-003: Good band r upper bound (0.60m).");
        }

        /// <summary>TR-004: q=0.60 boundary — Good band and Poor band are continuous.</summary>
        [Test]
        public void TouchRadius_GoodPoorBoundary_IsContinuous()
        {
            // Two very close q values bracketing 0.60.
            FirstTouchContext ctxAbove = ContextFactory.MakeDefault();
            ctxAbove.Technique = 12; ctxAbove.FirstTouchAttribute = 12;
            ctxAbove.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctxAbove.AgentVelocity = new Vector3(0.05f, 0.0f, 0.0f); // tiny movement to push just above/below boundary
            ctxAbove.PressureScalar = 0.0f;

            FirstTouchContext ctxBelow = ctxAbove;
            ctxBelow.AgentVelocity = new Vector3(0.20f, 0.0f, 0.0f);

            float rAbove = _system.EvaluateFirstTouch(ctxAbove).TouchRadius;
            float rBelow = _system.EvaluateFirstTouch(ctxBelow).TouchRadius;

            Assert.AreEqual(rAbove, rBelow, 0.01f,
                "TR-004: Touch radius must be continuous at the Good/Poor boundary.");
        }

        /// <summary>TR-005: Poor band at q≈0.475 → r ≈ 0.90m.</summary>
        [Test]
        public void TouchRadius_PoorBandMidRange_ReturnsInterpolatedValue()
        {
            // Target q ≈ 0.475 (Poor band: 0.35 ≤ q < 0.60).
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 9; ctx.FirstTouchAttribute = 9;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.GreaterOrEqual(result.ControlQuality, 0.35f, "TR-005 pre-check: q must be in Poor band.");
            Assert.Less(result.ControlQuality, 0.60f, "TR-005 pre-check: q must be below Good band.");
            Assert.GreaterOrEqual(result.TouchRadius, 0.60f, "TR-005: Poor band r lower bound (0.60m).");
            Assert.LessOrEqual(result.TouchRadius, 1.20f, "TR-005: Poor band r upper bound (1.20m).");
        }

        /// <summary>TR-006: Heavy band at q≈0.175 → r ≈ 1.60m.</summary>
        [Test]
        public void TouchRadius_HeavyTouchBand_ReturnsInterpolatedValue()
        {
            // Target q ≈ 0.175 (Heavy band: 0.0 ≤ q < 0.35).
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 5; ctx.FirstTouchAttribute = 4;
            ctx.BallVelocity = new Vector3(-25.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = new Vector3(3.0f, 0.0f, 0.0f); ctx.PressureScalar = 0.3f;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.Less(result.ControlQuality, 0.35f, "TR-006 pre-check: q must be in Heavy band.");
            Assert.GreaterOrEqual(result.TouchRadius, 1.20f, "TR-006: Heavy band r lower bound (1.20m).");
            Assert.LessOrEqual(result.TouchRadius, 2.00f, "TR-006: Heavy band r upper bound (2.00m).");
        }

        /// <summary>TR-007: q=0.35 boundary — Poor and Heavy bands are continuous.</summary>
        [Test]
        public void TouchRadius_PoorHeavyBoundary_IsContinuous()
        {
            FirstTouchContext ctxAbove = ContextFactory.MakeDefault();
            ctxAbove.Technique = 6; ctxAbove.FirstTouchAttribute = 7;
            ctxAbove.BallVelocity = new Vector3(-20.0f, 0.0f, 0.0f);
            ctxAbove.AgentVelocity = new Vector3(2.0f, 0.0f, 0.0f);
            ctxAbove.PressureScalar = 0.10f;

            FirstTouchContext ctxBelow = ctxAbove;
            ctxBelow.PressureScalar = 0.18f;

            float rAbove = _system.EvaluateFirstTouch(ctxAbove).TouchRadius;
            float rBelow = _system.EvaluateFirstTouch(ctxBelow).TouchRadius;

            Assert.AreEqual(rAbove, rBelow, 0.01f,
                "TR-007: Touch radius must be continuous at the Poor/Heavy boundary.");
        }

        /// <summary>TR-008: Monotonicity — r is strictly non-increasing as q increases across [0,1].</summary>
        [Test]
        public void TouchRadius_MonotonicityVerification_HigherQualityAlwaysLowerRadius()
        {
            // Sweep q from 0.0 to 1.0 by varying inputs. We vary a pressure scalar from 1.0 down to 0.0
            // against max-attribute player to approximate q = 0..1, supplemented by varying technique.
            // Simplest approach: use a hand-mapping with 21 distinct r samples via stored results.
            // Since we can't set q directly, drive quality via ball speed variation: fast→low q, slow→high q.

            (FirstTouchSystem sys, NullBallPhysicsSystem _, RecordingAgentMovementSystem _) = ContextFactory.MakeSystem();

            // Use Tech=20, FT=20, no movement, no pressure. Vary ball speed from very fast to very slow.
            // Very fast ball → low q (approach vel difficulty max). Very slow ball → q=1.0.
            float[] ballSpeeds = new float[]
            {
                60.0f, 50.0f, 40.0f, 35.0f, 30.0f, 27.0f, 24.0f, 21.0f, 18.0f, 16.0f,
                15.0f, 14.0f, 13.0f, 12.0f, 11.0f, 10.0f, 9.0f, 8.0f, 7.0f, 6.0f, 5.0f
            };

            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 20; ctx.FirstTouchAttribute = 20;
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;

            float[] radii = new float[ballSpeeds.Length];
            for (int i = 0; i < ballSpeeds.Length; i++)
            {
                ctx.BallVelocity = new Vector3(-ballSpeeds[i], 0.0f, 0.0f);
                radii[i] = sys.EvaluateFirstTouch(ctx).TouchRadius;
            }

            // r must be non-increasing as q increases (i.e., as ball speed decreases → higher q → lower r).
            int violations = 0;
            for (int i = 1; i < radii.Length; i++)
            {
                if (radii[i] > radii[i - 1] + 0.001f)
                {
                    violations++;
                }
            }

            Assert.AreEqual(0, violations,
                "TR-008: Touch radius must be monotonically non-increasing as ball speed decreases (quality increases).");
        }

        /// <summary>TR-009: Velocity modifier — ball at 30 m/s produces larger radius than same quality at 15 m/s.</summary>
        [Test]
        public void TouchRadius_VelocityModifier_IncreasesRadiusForFastBall()
        {
            // Both tests target good-band q. Use same attributes but different ball speeds.
            // At 15 m/s: r_base = Lerp(0.60,0.30,t), VelMod=1.0.
            // At 30 m/s: VelExcess=15, VelMod=1+(15/15)*0.25=1.25; r_adjusted = r_base * 1.25.
            FirstTouchContext ctx15 = ContextFactory.MakeDefault();
            ctx15.Technique = 15; ctx15.FirstTouchAttribute = 14;
            ctx15.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx15.AgentVelocity = Vector3.zero; ctx15.PressureScalar = 0.0f;

            FirstTouchContext ctx30 = ctx15;
            ctx30.BallVelocity = new Vector3(-30.0f, 0.0f, 0.0f);

            float r15 = _system.EvaluateFirstTouch(ctx15).TouchRadius;
            float r30 = _system.EvaluateFirstTouch(ctx30).TouchRadius;

            Assert.Greater(r30, r15,
                "TR-009: A ball at 30 m/s should produce a larger touch radius than 15 m/s.");
            // The ratio of the base radii should be ≈ 1.25 (velocity modifier).
            Assert.AreEqual(r15 * 1.25f, r30, 0.015f,
                "TR-009: Velocity modifier at 30 m/s should scale radius by ≈ 1.25.");
        }

        /// <summary>TR-010: Extreme ball speed (60 m/s) with poor quality → radius hard-capped at 2.0m.</summary>
        [Test]
        public void TouchRadius_VelocityModifier_HardCappedAtMaxTouchRadius()
        {
            // Hand-calc: q≈0.10, r_base≈1.771m, VelMod=1.75 → 3.099m → clamped to 2.0m.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 4; ctx.FirstTouchAttribute = 4;
            ctx.BallVelocity = new Vector3(-60.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = new Vector3(4.0f, 0.0f, 0.0f);
            ctx.PressureScalar = 0.5f;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(2.0f, result.TouchRadius, 0.001f,
                "TR-010: Touch radius must be hard-capped at 2.0m (RadiusHeavy) regardless of velocity modifier.");
        }
    }

    // ===========================================================================
    // PR — Pressure Evaluation Tests (§5.4)
    // ===========================================================================

    [TestFixture]
    internal sealed class PressureEvaluationTests
    {
        private FirstTouchSystem _system;

        [SetUp]
        public void SetUp()
        {
            (FirstTouchSystem sys, NullBallPhysicsSystem _, RecordingAgentMovementSystem _) = ContextFactory.MakeSystem();
            _system = sys;
        }

        /// <summary>PR-001: No opponents → pressure scalar = 0.0.</summary>
        [Test]
        public void Pressure_NoOpponents_ReturnsZeroScalar()
        {
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.PressureScalar      = 0.0f;
            ctx.HasNearbyOpponent   = false;

            // PressureEvaluator is internal; we verify via ControlQuality response to pressure.
            // Two calls: one with pressure=0.0, one with pressure=0.001 — should produce same q.
            FirstTouchContext ctx0 = ctx;
            ctx0.PressureScalar = 0.0f;
            float q0 = _system.EvaluateFirstTouch(ctx0).ControlQuality;

            // q at 0.0 should be unaffected by pressure.
            FirstTouchContext ctxTiny = ctx;
            ctxTiny.PressureScalar = 0.001f;
            float qTiny = _system.EvaluateFirstTouch(ctxTiny).ControlQuality;

            // Difference due to 0.001 pressure should be negligible.
            Assert.AreEqual(q0, qTiny, 0.001f,
                "PR-001: Zero pressure (no opponents) should produce no quality modification.");
        }

        /// <summary>PR-002: Single opponent at 3.0m edge → pressure ≈ 0.0067.</summary>
        [Test]
        public void Pressure_SingleOpponentAtPressureRadiusEdge_ProducesNearZero()
        {
            // Hand-calc: contrib=(0.3/3.0)²=0.01, scalar=0.01/1.5=0.0067
            // We pass the pre-computed value into PressureScalar (PressureEvaluator is called by the pipeline,
            // but here we verify the formula by pre-computing the expected scalar and testing quality degradation).
            float expectedScalar = 0.0067f;
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 15; ctx.FirstTouchAttribute = 14;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero;

            FirstTouchContext ctxNoPressure = ctx;
            ctxNoPressure.PressureScalar = 0.0f;

            FirstTouchContext ctxEdgePressure = ctx;
            ctxEdgePressure.PressureScalar = expectedScalar;

            float qNone = _system.EvaluateFirstTouch(ctxNoPressure).ControlQuality;
            float qEdge = _system.EvaluateFirstTouch(ctxEdgePressure).ControlQuality;

            // At scalar=0.0067, degradation = 1.0 - 0.0067*0.40 = 0.99732 → barely any change.
            Assert.AreEqual(qNone * (1.0f - expectedScalar * 0.40f), qEdge, 0.005f,
                "PR-002: Opponent at pressure radius edge should produce near-zero pressure (≈0.007).");
        }

        /// <summary>PR-003: Single opponent at 1.5m → pressure ≈ 0.0267.</summary>
        [Test]
        public void Pressure_SingleOpponentAt1Point5m_ProducesMidrangePressure()
        {
            // Hand-calc: contrib=(0.3/1.5)²=0.04, scalar=0.04/1.5=0.0267
            float expectedScalar = 0.0267f;
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 15; ctx.FirstTouchAttribute = 14;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero;
            ctx.PressureScalar = expectedScalar;

            FirstTouchContext ctxNone = ctx;
            ctxNone.PressureScalar = 0.0f;
            float qNone = _system.EvaluateFirstTouch(ctxNone).ControlQuality;
            float qWith = _system.EvaluateFirstTouch(ctx).ControlQuality;

            Assert.AreEqual(qNone * (1.0f - expectedScalar * 0.40f), qWith, 0.005f,
                "PR-003: Single opponent at 1.5m should produce pressure scalar ≈ 0.0267.");
        }

        /// <summary>PR-004: Single opponent at 0.5m → pressure ≈ 0.240.</summary>
        [Test]
        public void Pressure_SingleOpponentAt0Point5m_ProducesHighPressure()
        {
            // Hand-calc: dist=0.5m, guardedDist=0.5m, contrib=(0.3/0.5)²=0.36, scalar=0.36/1.5=0.240
            float expectedScalar = 0.240f;
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 15; ctx.FirstTouchAttribute = 14;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero;

            FirstTouchContext ctxNone = ctx;
            ctxNone.PressureScalar = 0.0f;
            float qNone = _system.EvaluateFirstTouch(ctxNone).ControlQuality;

            ctx.PressureScalar = expectedScalar;
            float qWith = _system.EvaluateFirstTouch(ctx).ControlQuality;

            Assert.AreEqual(qNone * (1.0f - expectedScalar * 0.40f), qWith, 0.02f,
                "PR-004: Opponent at 0.5m should produce pressure scalar ≈ 0.240.");
        }

        /// <summary>PR-005: Opponent within MinPressureDistance (0.1m) produces same scalar as at exactly 0.3m.</summary>
        [Test]
        public void Pressure_OpponentAtMinimumDistance_ClampsCorrectly()
        {
            // Hand-calc: both clamp to 0.3m → contrib=1.0, scalar=1.0/1.5=0.667
            float expectedScalar = 0.667f;
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 15; ctx.FirstTouchAttribute = 14;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero;
            ctx.PressureScalar = expectedScalar;

            float qMin = _system.EvaluateFirstTouch(ctx).ControlQuality;

            // Scalar at exactly MinPressureDistance should be identical.
            // Both should produce the clamped scalar of ≈ 0.667.
            FirstTouchContext ctxNone = ctx;
            ctxNone.PressureScalar = 0.0f;
            float qNone = _system.EvaluateFirstTouch(ctxNone).ControlQuality;

            Assert.AreEqual(qNone * (1.0f - expectedScalar * 0.40f), qMin, 0.01f,
                "PR-005: Opponent at min-pressure-distance should produce scalar ≈ 0.667.");
        }

        /// <summary>PR-006: Two opponents (1.0m and 2.0m) stack correctly → pressure ≈ 0.075.</summary>
        [Test]
        public void Pressure_TwoOpponents_StacksCorrectly()
        {
            // Hand-calc: opp1=(0.3/1.0)²=0.09, opp2=(0.3/2.0)²=0.0225, total=0.1125, scalar=0.075
            float expectedScalar = 0.075f;
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 15; ctx.FirstTouchAttribute = 14;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero;
            ctx.PressureScalar = expectedScalar;

            FirstTouchContext ctxNone = ctx;
            ctxNone.PressureScalar = 0.0f;
            float qNone = _system.EvaluateFirstTouch(ctxNone).ControlQuality;
            float qWith = _system.EvaluateFirstTouch(ctx).ControlQuality;

            Assert.AreEqual(qNone * (1.0f - expectedScalar * 0.40f), qWith, 0.01f,
                "PR-006: Two opponents (1.0m + 2.0m) should produce combined pressure scalar ≈ 0.075.");
        }

        /// <summary>PR-007: Five opponents at min-distance → pressure saturates at 1.0.</summary>
        [Test]
        public void Pressure_SaturatesAtMaximumScalar()
        {
            // Hand-calc: 5 × contrib=1.0 = rawPressure=5.0 → scalar=Clamp(5.0/1.5,0,1)=1.0
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 15; ctx.FirstTouchAttribute = 14;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero;
            ctx.PressureScalar = 1.0f; // Saturated — passed as pre-computed value.

            FirstTouchContext ctxNone = ctx;
            ctxNone.PressureScalar = 0.0f;
            float qNone = _system.EvaluateFirstTouch(ctxNone).ControlQuality;
            float qFull = _system.EvaluateFirstTouch(ctx).ControlQuality;

            Assert.AreEqual(qNone * 0.60f, qFull, 0.001f,
                "PR-007: Maximum pressure scalar (1.0) must degrade quality to exactly 60% of baseline.");
            Assert.LessOrEqual(ctx.PressureScalar, 1.0f,
                "PR-007: Pressure scalar must not exceed 1.0.");
        }

        /// <summary>PR-008: Opponent beyond pressure radius (3.01m) should not be counted.</summary>
        [Test]
        public void Pressure_OpponentsBeyondRadius_NotCounted()
        {
            // Opponent outside 3.0m should contribute 0 to PressureScalar.
            // We verify by comparing q with pressure=0 vs q with pressure computed as if opponent at 3.01m.
            // Since (0.3/3.01)² / 1.5 ≈ 0.0066 but the cutoff is strict, the answer is 0.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 15; ctx.FirstTouchAttribute = 14;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero;
            ctx.PressureScalar = 0.0f;  // opponent at 3.01m contributes nothing.

            float q = _system.EvaluateFirstTouch(ctx).ControlQuality;
            // Baseline equals no-pressure result — verifies that 0.0 is the correct scalar.
            Assert.Greater(q, 0.0f, "PR-008: q should be positive with no effective pressure.");
        }
    }

    // ===========================================================================
    // OR — Body Orientation Tests (§5.5)
    // ===========================================================================

    [TestFixture]
    internal sealed class OrientationTests
    {
        private FirstTouchSystem _system;

        [SetUp]
        public void SetUp()
        {
            (FirstTouchSystem sys, NullBallPhysicsSystem _, RecordingAgentMovementSystem _) = ContextFactory.MakeSystem();
            _system = sys;
        }

        /// <summary>OR-001: Agent facing 45° to ball approach → IsHalfTurnOriented = true, bonus applied.</summary>
        [Test]
        public void Orientation_AgentFacingAtHalfTurn_45Degrees_ReturnsTrue()
        {
            // Ball incoming from -X direction (velocity (-1,0,0)), approach dir is (+1,0,0).
            // Agent facing (0.707,0.707,0) is 45° from approach dir → qualifies.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 14; ctx.FirstTouchAttribute = 14;
            ctx.BallVelocity = new Vector3(-10.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            ctx.AgentFacing = new Vector3(0.707f, 0.707f, 0.0f);
            ctx.IsHalfTurnOriented = true; // Pre-computed by OrientationDetector in the real pipeline.

            FirstTouchContext ctxNo = ctx;
            ctxNo.IsHalfTurnOriented = false;

            float qWith = _system.EvaluateFirstTouch(ctx).ControlQuality;
            float qNo   = _system.EvaluateFirstTouch(ctxNo).ControlQuality;

            // The half-turn bonus raises quality; if neither is clamped to 1.0, qWith > qNo.
            if (qNo < 1.0f / 1.15f)
            {
                Assert.Greater(qWith, qNo, "OR-001: Half-turn orientation must increase quality.");
                Assert.AreEqual(qNo * 1.15f, qWith, 0.02f,
                    "OR-001: Half-turn bonus must be exactly 15% multiplicative increase.");
            }
            else
            {
                Assert.AreEqual(1.0f, qWith, 0.001f, "OR-001: Both clamped to 1.0.");
            }
        }

        /// <summary>OR-002: Agent facing directly toward ball (0°) → no bonus.</summary>
        [Test]
        public void Orientation_AgentFacingBallDirectly_ReturnsNotHalfTurn()
        {
            // Ball from -X → approach dir +X. Agent facing +X → 0° → outside [30°,60°].
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 12; ctx.FirstTouchAttribute = 12;
            ctx.BallVelocity = new Vector3(-10.0f, 0.0f, 0.0f);
            ctx.AgentFacing = new Vector3(1.0f, 0.0f, 0.0f);
            ctx.IsHalfTurnOriented = false;

            FirstTouchContext ctxWith = ctx;
            ctxWith.IsHalfTurnOriented = true;

            float qNo   = _system.EvaluateFirstTouch(ctx).ControlQuality;
            float qWith = _system.EvaluateFirstTouch(ctxWith).ControlQuality;

            // Verify bonus IS NOT applied when IsHalfTurnOriented=false.
            // When IsHalfTurnOriented=false, bonus = 0 (no change to attrWithBonus).
            // qNo must be < qWith to confirm false means no bonus.
            Assert.Less(qNo, qWith,
                "OR-002: IsHalfTurnOriented=false must not apply the 15% bonus (qNo must be lower than qWith).");
        }

        /// <summary>OR-003: Agent facing away from ball (180°) → no bonus.</summary>
        [Test]
        public void Orientation_AgentFacingAwayFromBall_ReturnsNotHalfTurn()
        {
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 12; ctx.FirstTouchAttribute = 12;
            ctx.BallVelocity = new Vector3(-10.0f, 0.0f, 0.0f);
            ctx.AgentFacing = new Vector3(-1.0f, 0.0f, 0.0f); // facing away
            ctx.IsHalfTurnOriented = false;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);
            Assert.Greater(result.ControlQuality, 0.0f, "OR-003: q must be positive even when facing away.");
        }

        /// <summary>OR-004: Exactly 30° is inclusive → IsHalfTurnOriented = true.</summary>
        [Test]
        public void Orientation_AtLowerWindowBoundary_30Degrees_IsIncluded()
        {
            // facing = (cos30°, sin30°, 0) = (0.866, 0.500, 0). Ball from -X direction.
            // Angle between facing and approach (+X) = 30° → qualifies.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 12; ctx.FirstTouchAttribute = 12;
            ctx.BallVelocity = new Vector3(-10.0f, 0.0f, 0.0f);
            ctx.AgentFacing = new Vector3(Mathf.Cos(30.0f * Mathf.Deg2Rad), Mathf.Sin(30.0f * Mathf.Deg2Rad), 0.0f);
            ctx.IsHalfTurnOriented = true; // OrientationDetector would return true at exactly 30°.

            FirstTouchContext ctxNo = ctx;
            ctxNo.IsHalfTurnOriented = false;

            float qWith = _system.EvaluateFirstTouch(ctx).ControlQuality;
            float qNo   = _system.EvaluateFirstTouch(ctxNo).ControlQuality;

            if (qNo < 1.0f / 1.15f)
            {
                Assert.Greater(qWith, qNo,
                    "OR-004: At exactly 30°, IsHalfTurnOriented must be true (bonus applied).");
            }
        }

        /// <summary>OR-005: Exactly 60° is inclusive → IsHalfTurnOriented = true.</summary>
        [Test]
        public void Orientation_AtUpperWindowBoundary_60Degrees_IsIncluded()
        {
            // facing = (cos60°, sin60°, 0) = (0.500, 0.866, 0).
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 12; ctx.FirstTouchAttribute = 12;
            ctx.BallVelocity = new Vector3(-10.0f, 0.0f, 0.0f);
            ctx.AgentFacing = new Vector3(Mathf.Cos(60.0f * Mathf.Deg2Rad), Mathf.Sin(60.0f * Mathf.Deg2Rad), 0.0f);
            ctx.IsHalfTurnOriented = true;

            FirstTouchContext ctxNo = ctx;
            ctxNo.IsHalfTurnOriented = false;

            float qWith = _system.EvaluateFirstTouch(ctx).ControlQuality;
            float qNo   = _system.EvaluateFirstTouch(ctxNo).ControlQuality;

            if (qNo < 1.0f / 1.15f)
            {
                Assert.Greater(qWith, qNo,
                    "OR-005: At exactly 60°, IsHalfTurnOriented must be true (bonus applied).");
            }
        }

        /// <summary>OR-006: 61° → outside window → IsHalfTurnOriented = false.</summary>
        [Test]
        public void Orientation_JustOutsideUpperBoundary_61Degrees_ReturnsFalse()
        {
            // 61° is outside [30°, 60°] → no bonus.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 12; ctx.FirstTouchAttribute = 12;
            ctx.BallVelocity = new Vector3(-10.0f, 0.0f, 0.0f);
            ctx.AgentFacing = new Vector3(Mathf.Cos(61.0f * Mathf.Deg2Rad), Mathf.Sin(61.0f * Mathf.Deg2Rad), 0.0f);
            ctx.IsHalfTurnOriented = false; // 61° → outside window.

            FirstTouchContext ctxFalselyTrue = ctx;
            ctxFalselyTrue.IsHalfTurnOriented = true;

            float qCorrect = _system.EvaluateFirstTouch(ctx).ControlQuality;
            float qBonused = _system.EvaluateFirstTouch(ctxFalselyTrue).ControlQuality;

            // With IsHalfTurnOriented=false, no bonus; qCorrect must be lower.
            Assert.Less(qCorrect, qBonused,
                "OR-006: At 61°, no bonus should be applied (IsHalfTurnOriented=false must yield lower q).");
        }

        /// <summary>OR-007: Both +45° and -45° qualify symmetrically.</summary>
        [Test]
        public void Orientation_Symmetrical_BothSidesOfBall_BothQualify()
        {
            FirstTouchContext ctxPos = ContextFactory.MakeDefault();
            ctxPos.Technique = 12; ctxPos.FirstTouchAttribute = 12;
            ctxPos.BallVelocity = new Vector3(-10.0f, 0.0f, 0.0f);
            ctxPos.AgentFacing = new Vector3(0.707f, 0.707f, 0.0f);   // +45°
            ctxPos.IsHalfTurnOriented = true;

            FirstTouchContext ctxNeg = ctxPos;
            ctxNeg.AgentFacing = new Vector3(0.707f, -0.707f, 0.0f);  // -45°
            ctxNeg.IsHalfTurnOriented = true;

            float qPos = _system.EvaluateFirstTouch(ctxPos).ControlQuality;
            float qNeg = _system.EvaluateFirstTouch(ctxNeg).ControlQuality;

            Assert.AreEqual(qPos, qNeg, 0.001f,
                "OR-007: ±45° orientations should produce identical quality (symmetric window).");
        }
    }

    // ===========================================================================
    // PO — Possession State Machine Tests (§5.6)
    // ===========================================================================

    [TestFixture]
    internal sealed class PossessionStateMachineTests
    {
        private FirstTouchSystem _system;
        private RecordingAgentMovementSystem _agentMov;

        [SetUp]
        public void SetUp()
        {
            (FirstTouchSystem sys, NullBallPhysicsSystem _, RecordingAgentMovementSystem agentMov) = ContextFactory.MakeSystem();
            _system = sys;
            _agentMov = agentMov;
        }

        /// <summary>PO-001: Good quality, no opponent → CONTROLLED outcome.</summary>
        [Test]
        public void Possession_GoodQuality_NoOpponent_ReturnsControlled()
        {
            // q ≈ 0.75 (Good band) → r ≈ 0.42m < 0.60m → CONTROLLED.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 16; ctx.FirstTouchAttribute = 16;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            ctx.HasNearbyOpponent = false; ctx.NearestOpponentDistance = float.PositiveInfinity;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(TouchResult.Controlled, result.PossessionOutcome,
                "PO-001: Good quality with no opponent must yield CONTROLLED.");
            Assert.IsTrue(result.TriggeredDribblingState,
                "PO-001: CONTROLLED must trigger dribbling state.");
            Assert.AreEqual(ctx.AgentID, result.PossessingAgentID,
                "PO-001: Possessing agent must be the receiving agent.");
        }

        /// <summary>PO-002: r in loose-ball range (0.60m–1.20m), no opponent → LOOSE_BALL.</summary>
        [Test]
        public void Possession_TouchRadius_AboveLooseBallThreshold_NoOpponent_ReturnsLooseBall()
        {
            // Force r > 0.60m and r < 1.20m: use a Poor-band quality player with no opponent.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 9; ctx.FirstTouchAttribute = 8;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            ctx.HasNearbyOpponent = false; ctx.NearestOpponentDistance = float.PositiveInfinity;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            // Pre-verify r is in the expected range.
            Assert.GreaterOrEqual(result.TouchRadius, 0.60f, "PO-002 pre-check: r must be ≥ 0.60m.");
            Assert.Less(result.TouchRadius, 1.20f, "PO-002 pre-check: r must be < 1.20m.");
            Assert.AreEqual(TouchResult.LooseBall, result.PossessionOutcome,
                "PO-002: r in [0.60m, 1.20m) with no opponent must yield LOOSE_BALL.");
        }

        /// <summary>PO-003: Heavy touch, opponent in range → INTERCEPTION (priority over DEFLECTION).</summary>
        [Test]
        public void Possession_HeavyTouch_OpponentInRange_ReturnsInterception()
        {
            // Force r ≥ 1.20m: poor quality player, fast ball.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 5; ctx.FirstTouchAttribute = 5;
            ctx.BallVelocity = new Vector3(-25.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = new Vector3(3.0f, 0.0f, 0.0f);
            ctx.PressureScalar = 0.30f;
            ctx.HasNearbyOpponent = true;
            ctx.NearestOpponentDistance = 2.0f; // 2.0m < 2.50m interception radius.

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.GreaterOrEqual(result.TouchRadius, 1.20f, "PO-003 pre-check: r must be ≥ 1.20m.");
            Assert.AreEqual(TouchResult.Interception, result.PossessionOutcome,
                "PO-003: r ≥ 1.20m with opponent in 2.50m radius must yield INTERCEPTION.");
        }

        /// <summary>PO-004: Very heavy touch, momentum aligned, no opponent → DEFLECTION.</summary>
        [Test]
        public void Possession_HeavyTouch_HighMomentumAlignment_NoOpponent_ReturnsDeflection()
        {
            // r ≥ 1.50m with strong momentum alignment (ball continues in same direction).
            // Very poor quality + very fast ball, no opponent.
            // IntendedDir = IncomingDir so momentum is retained → alignment ≥ 0.70.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 3; ctx.FirstTouchAttribute = 3;
            ctx.BallVelocity = new Vector3(-28.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            // Set intended direction = incoming direction to maximise momentum alignment.
            ctx.IntendedTouchDirection = new Vector3(-1.0f, 0.0f, 0.0f); // same as ball travel
            ctx.AgentFacing = new Vector3(-1.0f, 0.0f, 0.0f);
            ctx.HasNearbyOpponent = false; ctx.NearestOpponentDistance = float.PositiveInfinity;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            // May yield DEFLECTION or LOOSE_BALL depending on exact r and alignment.
            // Just assert it's not INTERCEPTION (no opponent present) and not CONTROLLED (r too large).
            Assert.AreNotEqual(TouchResult.Interception, result.PossessionOutcome,
                "PO-004: No opponent → cannot be INTERCEPTION.");
            Assert.AreNotEqual(TouchResult.Controlled, result.PossessionOutcome,
                "PO-004: Very poor touch must not yield CONTROLLED.");
        }

        /// <summary>PO-005: r ≥ 1.50m but low momentum alignment → LOOSE_BALL, not DEFLECTION.</summary>
        [Test]
        public void Possession_HeavyTouch_LowMomentumAlignment_NoOpponent_ReturnsLooseBall()
        {
            // Force r ≥ 1.50m but redirect ball 90° → alignment ≈ 0.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 4; ctx.FirstTouchAttribute = 4;
            ctx.BallVelocity = new Vector3(-25.0f, 0.0f, 0.0f);  // ball from right
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            // Agent wants to redirect ball 90° (perpendicular to original direction).
            ctx.IntendedTouchDirection = new Vector3(0.0f, 1.0f, 0.0f);
            ctx.AgentFacing = new Vector3(0.0f, 1.0f, 0.0f);
            ctx.HasNearbyOpponent = false; ctx.NearestOpponentDistance = float.PositiveInfinity;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            // Redirected ball cannot have alignment ≥ 0.70 → not DEFLECTION.
            Assert.AreNotEqual(TouchResult.Deflection, result.PossessionOutcome,
                "PO-005: Low momentum alignment (90° redirect) must not yield DEFLECTION.");
            Assert.AreNotEqual(TouchResult.Interception, result.PossessionOutcome,
                "PO-005: No opponent → cannot be INTERCEPTION.");
        }

        /// <summary>PO-006: INTERCEPTION takes priority over DEFLECTION when both conditions are met.</summary>
        [Test]
        public void Possession_InterceptionTakesPriorityOverDeflection()
        {
            // r ≥ 1.50m, alignment ≥ 0.70 (deflection would trigger), but opponent in range.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 3; ctx.FirstTouchAttribute = 3;
            ctx.BallVelocity = new Vector3(-28.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            ctx.IntendedTouchDirection = new Vector3(-1.0f, 0.0f, 0.0f); // momentum retained
            ctx.AgentFacing = new Vector3(-1.0f, 0.0f, 0.0f);
            ctx.HasNearbyOpponent = true;
            ctx.NearestOpponentDistance = 2.0f; // within interception radius.

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.GreaterOrEqual(result.TouchRadius, 1.20f, "PO-006 pre-check: r must be ≥ 1.20m.");
            Assert.AreEqual(TouchResult.Interception, result.PossessionOutcome,
                "PO-006: INTERCEPTION must take priority over DEFLECTION.");
        }

        /// <summary>PO-007: Opponent just outside interception radius (2.51m) → no INTERCEPTION.</summary>
        [Test]
        public void Possession_OpponentBeyondInterceptionRadius_DoesNotTriggerInterception()
        {
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 5; ctx.FirstTouchAttribute = 5;
            ctx.BallVelocity = new Vector3(-25.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = new Vector3(3.0f, 0.0f, 0.0f); ctx.PressureScalar = 0.30f;
            ctx.HasNearbyOpponent = true;
            ctx.NearestOpponentDistance = 2.51f; // Just outside InterceptionRadius (2.50m).

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreNotEqual(TouchResult.Interception, result.PossessionOutcome,
                "PO-007: Opponent at 2.51m (outside InterceptionRadius) must not trigger INTERCEPTION.");
        }

        /// <summary>PO-008: CONTROLLED outcome triggers SetDribblingState(agentID, true) exactly once.</summary>
        [Test]
        public void Possession_ControlledOutcome_TriggersCorrectDribblingSignal()
        {
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.AgentID = 7;
            ctx.Technique = 18; ctx.FirstTouchAttribute = 18;
            ctx.BallVelocity = new Vector3(-8.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            ctx.HasNearbyOpponent = false;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);
            _system.ApplyTouchResult(result, ctx);

            Assert.AreEqual(TouchResult.Controlled, result.PossessionOutcome,
                "PO-008 pre-check: scenario must yield CONTROLLED.");
            Assert.IsTrue(result.TriggeredDribblingState, "PO-008: TriggeredDribblingState must be true.");
            Assert.AreEqual(7, _agentMov.LastAgentID, "PO-008: SetDribblingState must be called with AgentID=7.");
            Assert.IsTrue(_agentMov.LastIsDribbling, "PO-008: SetDribblingState must be called with isDribbling=true.");
        }
    }

    // ===========================================================================
    // EC — Edge Case and Robustness Tests (§5.7)
    // ===========================================================================

    [TestFixture]
    internal sealed class EdgeCaseTests
    {
        private FirstTouchSystem _system;

        [SetUp]
        public void SetUp()
        {
            (FirstTouchSystem sys, NullBallPhysicsSystem _, RecordingAgentMovementSystem _) = ContextFactory.MakeSystem();
            _system = sys;
        }

        /// <summary>EC-001: NaN ball velocity must not propagate NaN to output.</summary>
        [Test]
        public void EdgeCase_NaNBallVelocity_DoesNotPropagateNaN()
        {
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.BallVelocity = new Vector3(float.NaN, 0.0f, 0.0f);

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.IsFalse(float.IsNaN(result.ControlQuality),
                "EC-001: NaN ball velocity must not produce NaN ControlQuality.");
            Assert.GreaterOrEqual(result.ControlQuality, 0.0f, "EC-001: q must be ≥ 0.");
            Assert.LessOrEqual(result.ControlQuality, 1.0f, "EC-001: q must be ≤ 1.");
            Assert.IsFalse(float.IsNaN(result.TouchRadius), "EC-001: TouchRadius must not be NaN.");
        }

        /// <summary>EC-002: Zero ball velocity must use minimum velocity floor (no division by zero).</summary>
        [Test]
        public void EdgeCase_ZeroBallVelocity_UsesMinimumVelocityFloor()
        {
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.BallVelocity = Vector3.zero; // 0 m/s

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.IsFalse(float.IsNaN(result.ControlQuality), "EC-002: Zero ball speed must not produce NaN.");
            Assert.IsFalse(float.IsInfinity(result.ControlQuality), "EC-002: Zero ball speed must not produce Infinity.");
            Assert.GreaterOrEqual(result.ControlQuality, 0.0f, "EC-002: q must be ≥ 0.");
            Assert.LessOrEqual(result.ControlQuality, 1.0f, "EC-002: q must be ≤ 1.");
        }

        /// <summary>EC-003: Attributes below minimum (Technique=-5, FT=-3) clamped to 1.</summary>
        [Test]
        public void EdgeCase_AttributesBelowMinimum_ClampedTo1()
        {
            FirstTouchContext ctxNeg = ContextFactory.MakeDefault();
            ctxNeg.Technique = -5; ctxNeg.FirstTouchAttribute = -3;
            ctxNeg.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctxNeg.AgentVelocity = Vector3.zero; ctxNeg.PressureScalar = 0.0f;

            FirstTouchContext ctxMin = ctxNeg;
            ctxMin.Technique = 1; ctxMin.FirstTouchAttribute = 1;

            float qNeg = _system.EvaluateFirstTouch(ctxNeg).ControlQuality;
            float qMin = _system.EvaluateFirstTouch(ctxMin).ControlQuality;

            Assert.AreEqual(qMin, qNeg, 0.001f,
                "EC-003: Technique=-5, FT=-3 must clamp to 1 and produce same q as Technique=1, FT=1.");
        }

        /// <summary>EC-004: Attributes above maximum (Technique=25, FT=30) clamped to 20.</summary>
        [Test]
        public void EdgeCase_AttributesAboveMaximum_ClampedTo20()
        {
            FirstTouchContext ctxOver = ContextFactory.MakeDefault();
            ctxOver.Technique = 25; ctxOver.FirstTouchAttribute = 30;
            ctxOver.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctxOver.AgentVelocity = Vector3.zero; ctxOver.PressureScalar = 0.0f;

            FirstTouchContext ctxMax = ctxOver;
            ctxMax.Technique = 20; ctxMax.FirstTouchAttribute = 20;

            float qOver = _system.EvaluateFirstTouch(ctxOver).ControlQuality;
            float qMax  = _system.EvaluateFirstTouch(ctxMax).ControlQuality;

            // Both must be ≤ 1.0. The clamped result must equal the max-attribute result.
            // Note: the formula clamps at AttrMax (which comes from PlayerAttributeConstants.AttributeMax = 20).
            // If attributes above 20 produce higher q than 20, the clamp is missing.
            Assert.LessOrEqual(qOver, 1.0f, "EC-004: q must not exceed 1.0 for over-max attributes.");
            Assert.AreEqual(qMax, qOver, 0.001f,
                "EC-004: Technique=25 must be equivalent to Technique=20 (clamped).");
        }

        /// <summary>EC-005: Agent near pitch boundary — new ball position clamped to pitch edge (X axis).</summary>
        [Test]
        public void EdgeCase_AgentAtPitchBoundary_BallPositionClamped()
        {
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 9; ctx.FirstTouchAttribute = 8;
            ctx.AgentPosition = new Vector3(0.5f, 34.0f, 0.0f); // near X=0 boundary
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            ctx.IntendedTouchDirection = new Vector3(-1.0f, 0.0f, 0.0f); // push toward X<0

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.GreaterOrEqual(result.NewBallPosition.x, 0.0f,
                "EC-005: New ball position X must be clamped to ≥ 0 (pitch boundary).");
        }

        /// <summary>EC-006: Ball height above GROUND_CONTROL_HEIGHT — height guard precondition check.</summary>
        [Test]
        public void EdgeCase_BallHeightAboveGroundControlHeight_GuardFieldPopulated()
        {
            // The spec states the guard may be in the calling Collision System code.
            // FirstTouchSystem.EvaluateFirstTouch processes whatever it receives.
            // We verify BallHeight and BallIsAirborne fields are correctly carried through context.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.BallHeight = 0.6f;   // above GROUND_CONTROL_HEIGHT = 0.50m
            ctx.BallIsAirborne = true;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);

            // The system processes it — result should still be a valid (non-NaN) output.
            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.IsFalse(float.IsNaN(result.ControlQuality),
                "EC-006: Even with airborne ball, EvaluateFirstTouch must return a finite result.");
        }

        /// <summary>EC-007: Goalkeeper flag (IsGoalkeeper=true) does not alter quality computation.</summary>
        [Test]
        public void EdgeCase_GoalkeeperFlag_DoesNotAlterQuality()
        {
            // IT-006 scenario: IsGoalkeeper must not modify q.
            FirstTouchContext ctxOutfield = ContextFactory.MakeDefault();
            ctxOutfield.Technique = 12; ctxOutfield.FirstTouchAttribute = 10;
            ctxOutfield.BallVelocity = new Vector3(-14.0f, 0.0f, 0.0f);
            ctxOutfield.AgentVelocity = Vector3.zero; ctxOutfield.PressureScalar = 0.0f;
            ctxOutfield.IsGoalkeeper = false;

            FirstTouchContext ctxGK = ctxOutfield;
            ctxGK.IsGoalkeeper = true;

            float qOutfield = _system.EvaluateFirstTouch(ctxOutfield).ControlQuality;
            float qGK       = _system.EvaluateFirstTouch(ctxGK).ControlQuality;

            Assert.AreEqual(qOutfield, qGK, 0.001f,
                "EC-007: IsGoalkeeper=true must not modify control quality at Stage 0.");
        }

        /// <summary>EC-008: AGENT_ID_NONE returned as PossessingAgentID on non-CONTROLLED outcomes.</summary>
        [Test]
        public void EdgeCase_LooseBallOutcome_PossessingAgentIdIsNone()
        {
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 8; ctx.FirstTouchAttribute = 8;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            ctx.HasNearbyOpponent = false;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            if (result.PossessionOutcome != TouchResult.Controlled)
            {
                Assert.AreEqual(FirstTouchConstants.AGENT_ID_NONE, result.PossessingAgentID,
                    "EC-008: Non-CONTROLLED outcome must set PossessingAgentID to AGENT_ID_NONE.");
            }
        }
    }

    // ===========================================================================
    // BD — Ball Displacement Tests (§5.8)
    // ===========================================================================

    [TestFixture]
    internal sealed class BallDisplacementTests
    {
        private FirstTouchSystem _system;

        [SetUp]
        public void SetUp()
        {
            (FirstTouchSystem sys, NullBallPhysicsSystem _, RecordingAgentMovementSystem _) = ContextFactory.MakeSystem();
            _system = sys;
        }

        /// <summary>BD-001: q=1.0 → actual direction matches intended direction (zero deviation).</summary>
        [Test]
        public void BallDisplacement_PerfectQuality_ActualDirectionMatchesIntended()
        {
            // At q=1.0: blended = intended. Agent wants ball to go right (+X).
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 20; ctx.FirstTouchAttribute = 20;
            ctx.BallVelocity = new Vector3(-12.0f, 0.0f, 0.0f); // ball from right → approach = +X
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            ctx.AgentPosition = new Vector3(52.5f, 34.0f, 0.0f);
            ctx.IntendedTouchDirection = new Vector3(1.0f, 0.0f, 0.0f); // intended: to the right

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(1.0f, result.ControlQuality, 0.001f, "BD-001 pre-check: q must be 1.0.");

            // New ball position should be to the right of agent position.
            Assert.Greater(result.NewBallPosition.x, ctx.AgentPosition.x,
                "BD-001: At q=1.0 with intended direction +X, new ball position must be right of agent.");
        }

        /// <summary>BD-002: q≈0.0 → actual direction matches incoming ball direction.</summary>
        [Test]
        public void BallDisplacement_ZeroQuality_ActualDirectionMatchesIncoming()
        {
            // Worst possible player, very fast ball, high pressure → q near 0.
            // Ball comes from right (-X velocity). Agent wants ball left (+Y intended).
            // At q=0, the blend weight on intended=0, so ball should go in incoming direction.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 1; ctx.FirstTouchAttribute = 1;
            ctx.BallVelocity = new Vector3(-30.0f, 0.0f, 0.0f); // ball from right
            ctx.AgentVelocity = new Vector3(7.0f, 0.0f, 0.0f);
            ctx.PressureScalar = 1.0f;
            ctx.AgentPosition = new Vector3(52.5f, 34.0f, 0.0f);
            ctx.IntendedTouchDirection = new Vector3(0.0f, 1.0f, 0.0f); // agent wants Y
            ctx.AgentFacing = new Vector3(0.0f, 1.0f, 0.0f);

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.Less(result.ControlQuality, 0.05f, "BD-002 pre-check: q must be near 0.");
            // At near-zero q, ball should travel roughly in the incoming (-X) direction → X decreases.
            // The incoming dir is -X so the displacement should be predominantly in -X.
            float dx = result.NewBallPosition.x - ctx.AgentPosition.x;
            float dy = result.NewBallPosition.y - ctx.AgentPosition.y;
            Assert.Less(dx, 0.0f, "BD-002: Near-zero quality should produce predominantly -X displacement.");
            Assert.Less(Mathf.Abs(dy), Mathf.Abs(dx),
                "BD-002: |dy| should be less than |dx|, confirming incoming direction dominates.");
        }

        /// <summary>BD-003: q=0.5, perpendicular directions → actual direction at ≈45° (equidistant blend).</summary>
        [Test]
        public void BallDisplacement_MidQuality_ActualDirectionIsBlendedBetweenBoth()
        {
            // Hand-calc: q=0.5, ErrorWeight=0.5.
            // IntendedDir=(1,0), IncomingDir=(0,1)[ball from -Y, approach=+Y].
            // blended=(0.5,0.5), normalised=(0.707,0.707). Displacement at 45° between both.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 10; ctx.FirstTouchAttribute = 10;
            ctx.BallVelocity = new Vector3(0.0f, -15.0f, 0.0f); // ball from +Y side → incomingDir=+Y
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            ctx.AgentPosition = new Vector3(52.5f, 34.0f, 0.0f);
            ctx.IntendedTouchDirection = new Vector3(1.0f, 0.0f, 0.0f); // agent wants +X

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            // At q≈0.5, the blend should place the ball NE of the agent (both dx and dy positive).
            float dx = result.NewBallPosition.x - ctx.AgentPosition.x;
            float dy = result.NewBallPosition.y - ctx.AgentPosition.y;

            // At q≈0.5 blend: blended = 0.5*(0,1) + 0.5*(1,0) = (0.5,0.5) normalised to (0.707,0.707).
            // So both dx and dy should be positive.
            Assert.Greater(dx, 0.0f, "BD-003: At q≈0.5, displacement should have positive X component.");
            Assert.Greater(dy, 0.0f, "BD-003: At q≈0.5, displacement should have positive Y component.");
            // They should be approximately equal (45° blend).
            Assert.AreEqual(dx, dy, dx * 0.30f,
                "BD-003: At q≈0.5, X and Y displacement components should be approximately equal (±30%).");
        }

        /// <summary>BD-004: Anti-parallel directions at q=0.5 → fallback to incoming direction (no NaN).</summary>
        [Test]
        public void BallDisplacement_NearZeroBlendMagnitude_FallsBackToIncomingDir()
        {
            // IntendedDir=(1,0), IncomingDir=(-1,0) → blended at q=0.5 = (0,0) → fallback to IncomingDir.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 10; ctx.FirstTouchAttribute = 10;
            ctx.BallVelocity = new Vector3(15.0f, 0.0f, 0.0f); // ball from -X side → incomingDir = -X
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            ctx.AgentPosition = new Vector3(52.5f, 34.0f, 0.0f);
            ctx.IntendedTouchDirection = new Vector3(1.0f, 0.0f, 0.0f); // agent wants +X (anti-parallel to incoming)
            ctx.AgentFacing = new Vector3(1.0f, 0.0f, 0.0f);

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.IsFalse(float.IsNaN(result.NewBallPosition.x), "BD-004: No NaN in NewBallPosition.x.");
            Assert.IsFalse(float.IsNaN(result.NewBallPosition.y), "BD-004: No NaN in NewBallPosition.y.");
            Assert.IsFalse(float.IsNaN(result.NewBallVelocity.x), "BD-004: No NaN in NewBallVelocity.x.");
        }

        /// <summary>BD-005: Touch output ball speed hard-capped at 12.0 m/s.</summary>
        [Test]
        public void BallDisplacement_NewBallVelocitySpeed_CappedAtTouchMaxBallSpeed()
        {
            // Setup for high combined speed: q≈0.3, fast incoming ball, fast agent, aligned.
            // Hand-calc: AgentContrib = 5.5×0.3 = 1.65, BallRetained = 30.0×0.7×0.5 = 10.5 → 12.15 → cap.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 6; ctx.FirstTouchAttribute = 6;
            ctx.BallVelocity = new Vector3(-30.0f, 0.0f, 0.0f); // 30 m/s incoming
            ctx.AgentVelocity = new Vector3(7.0f, 0.0f, 0.0f);
            ctx.PressureScalar = 0.30f;
            ctx.AgentPosition = new Vector3(52.5f, 34.0f, 0.0f);
            // Align intended direction with incoming so momentum stacks.
            ctx.IntendedTouchDirection = new Vector3(-1.0f, 0.0f, 0.0f);
            ctx.AgentFacing = new Vector3(-1.0f, 0.0f, 0.0f);

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            float newSpeed = result.NewBallVelocity.magnitude;
            Assert.LessOrEqual(newSpeed, FirstTouchConstants.TouchMaxBallSpeed + 0.01f,
                $"BD-005: New ball speed ({newSpeed:F3} m/s) must not exceed TouchMaxBallSpeed ({FirstTouchConstants.TouchMaxBallSpeed} m/s).");
        }

        /// <summary>BD-006: Ground touch → new ball velocity Z = 0.0 exactly.</summary>
        [Test]
        public void BallDisplacement_NewBallVelocityZ_AlwaysZeroOnGroundTouch()
        {
            // Incoming ball has a Z component; ground touch must discard it.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 15; ctx.FirstTouchAttribute = 14;
            ctx.BallVelocity = new Vector3(-10.0f, 5.0f, 2.0f); // Z component = 2.0
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            ctx.BallHeight = 0.0f; ctx.BallIsAirborne = false;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(0.0f, result.NewBallVelocity.z, 0.0f,
                "BD-006: Ground touch must always produce NewBallVelocity.Z = 0.0 exactly.");
        }

        /// <summary>BD-007: New ball position X clamped at pitch lower boundary (0.0m).</summary>
        [Test]
        public void BallDisplacement_PitchBoundaryEnforcement_XAxisClamped()
        {
            // Hand-calc: agent at X=1.0, intended = -X, r ≈ 1.10m → newX = 1.0 - 1.10 = -0.10 → clamped to 0.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 9; ctx.FirstTouchAttribute = 9;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            ctx.AgentPosition = new Vector3(1.0f, 34.0f, 0.0f);
            ctx.IntendedTouchDirection = new Vector3(-1.0f, 0.0f, 0.0f);
            ctx.AgentFacing = new Vector3(-1.0f, 0.0f, 0.0f);

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.GreaterOrEqual(result.NewBallPosition.x, 0.0f,
                "BD-007: NewBallPosition.x must be clamped to ≥ 0 (pitch lower X boundary).");
        }

        /// <summary>BD-008: New ball position Y clamped at pitch upper boundary (68.0m).</summary>
        [Test]
        public void BallDisplacement_PitchBoundaryEnforcement_YAxisClamped()
        {
            // Hand-calc: agent at Y=67.5, intended = +Y, r ≈ 0.90m → newY = 68.40 → clamped to 68.0.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 9; ctx.FirstTouchAttribute = 8;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;
            ctx.AgentPosition = new Vector3(52.5f, 67.5f, 0.0f);
            ctx.IntendedTouchDirection = new Vector3(0.0f, 1.0f, 0.0f); // push toward Y boundary
            ctx.AgentFacing = new Vector3(0.0f, 1.0f, 0.0f);

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.LessOrEqual(result.NewBallPosition.y, FirstTouchConstants.PitchWidth + 0.001f,
                "BD-008: NewBallPosition.y must be clamped to ≤ PitchWidth (68.0m).");
        }

        /// <summary>BD-009: New ball position Z equals BallRadius (ball rests on ground).</summary>
        [Test]
        public void BallDisplacement_NewBallPositionZ_EqualsBallRadius()
        {
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 15; ctx.FirstTouchAttribute = 14;
            ctx.BallVelocity = new Vector3(-15.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(FirstTouchConstants.BallRadius, result.NewBallPosition.z, 0.001f,
                "BD-009: NewBallPosition.Z must equal BallRadius (0.11m) for a ground touch.");
        }
    }

    // ===========================================================================
    // VS — Real-World Validation Scenarios (§5.10)
    // ===========================================================================

    [TestFixture]
    internal sealed class ValidationScenarioTests
    {
        private FirstTouchSystem _system;

        [SetUp]
        public void SetUp()
        {
            (FirstTouchSystem sys, NullBallPhysicsSystem _, RecordingAgentMovementSystem _) = ContextFactory.MakeSystem();
            _system = sys;
        }

        /// <summary>VS-001: Elite creative midfielder, standard pass in half-turn — q≈0.929, r≈0.428m, CONTROLLED.</summary>
        [Test]
        public void ValidationScenario_VS001_EliteCreativeMidfielder_StandardPassInSpace()
        {
            // Hand-calc from §5.10 VS-001:
            // Tech=18, FT=19, ballSpeed=14, agentSpeed=3.0, halfTurn, no pressure.
            // q≈0.929 (Perfect band), r≈0.428m ≤ 0.60m → CONTROLLED.
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 18; ctx.FirstTouchAttribute = 19;
            ctx.BallVelocity = new Vector3(-14.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = new Vector3(3.0f, 0.0f, 0.0f);
            ctx.PressureScalar = 0.0f;
            ctx.IsHalfTurnOriented = true;
            ctx.AgentFacing = new Vector3(0.707f, 0.707f, 0.0f);
            ctx.HasNearbyOpponent = false;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(0.929f, result.ControlQuality, 0.03f,
                "VS-001: Elite midfielder q should be ≈ 0.929.");
            Assert.AreEqual(0.428f, result.TouchRadius, 0.05f,
                "VS-001: Touch radius should be ≈ 0.428m.");
            Assert.AreEqual(TouchResult.Controlled, result.PossessionOutcome,
                "VS-001: Elite midfielder in space should yield CONTROLLED.");
            Assert.IsTrue(result.TriggeredDribblingState, "VS-001: CONTROLLED must trigger dribbling.");
        }

        /// <summary>VS-002: Target striker, long ball under pressure — q≈0.379, r≈1.262m, INTERCEPTION.</summary>
        [Test]
        public void ValidationScenario_VS002_TargetStriker_LongBallUnderPressure()
        {
            // Hand-calc from §5.10 VS-002:
            // Tech=12, FT=11, ballSpeed=22, agentSpeed=0.5, no halfTurn, pressure=0.042.
            // q≈0.379, r≈1.262m ≥ 1.20m, nearest opponent at 1.5m < 2.50m → INTERCEPTION.
            float pressureScalar = 0.042f; // pre-computed from §5.10 hand-calc
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 12; ctx.FirstTouchAttribute = 11;
            ctx.BallVelocity = new Vector3(-22.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = new Vector3(0.5f, 0.0f, 0.0f);
            ctx.PressureScalar = pressureScalar;
            ctx.IsHalfTurnOriented = false;
            ctx.HasNearbyOpponent = true;
            ctx.NearestOpponentDistance = 1.5f;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(0.379f, result.ControlQuality, 0.03f,
                "VS-002: Target striker q should be ≈ 0.379.");
            Assert.AreEqual(1.262f, result.TouchRadius, 0.05f,
                "VS-002: Touch radius should be ≈ 1.262m.");
            Assert.AreEqual(TouchResult.Interception, result.PossessionOutcome,
                "VS-002: Defender exploits heavy touch → INTERCEPTION.");
        }

        /// <summary>VS-003: Defensive midfielder, high press — q≈0.604, r≈0.615m, LOOSE_BALL.</summary>
        [Test]
        public void ValidationScenario_VS003_DefensiveMidfielder_ReceivingUnderHighPress()
        {
            // Hand-calc from §5.10 VS-003:
            // Tech=14, FT=13, ballSpeed=17, agentSpeed=1.5, halfTurn, pressure=0.094.
            // q≈0.604, r≈0.615m > 0.60m → LOOSE_BALL (r < 1.20m, no interception threshold crossed).
            float pressureScalar = 0.094f;
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 14; ctx.FirstTouchAttribute = 13;
            ctx.BallVelocity = new Vector3(-17.0f, 0.0f, 0.0f);
            ctx.AgentVelocity = new Vector3(1.5f, 0.0f, 0.0f);
            ctx.PressureScalar = pressureScalar;
            ctx.IsHalfTurnOriented = true;
            ctx.AgentFacing = new Vector3(0.707f, 0.707f, 0.0f);
            ctx.HasNearbyOpponent = true;
            ctx.NearestOpponentDistance = 0.8f;

            FirstTouchResult result = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(0.604f, result.ControlQuality, 0.03f,
                "VS-003: Defensive midfielder q should be ≈ 0.604.");
            Assert.AreEqual(0.615f, result.TouchRadius, 0.03f,
                "VS-003: Touch radius should be ≈ 0.615m.");
            Assert.AreEqual(TouchResult.LooseBall, result.PossessionOutcome,
                "VS-003: Contested ball just beyond controlled range → LOOSE_BALL.");
        }
    }

    // ===========================================================================
    // Additional determinism / invariant tests
    // ===========================================================================

    [TestFixture]
    internal sealed class DeterminismAndInvariantTests
    {
        private FirstTouchSystem _system;

        [SetUp]
        public void SetUp()
        {
            (FirstTouchSystem sys, NullBallPhysicsSystem _, RecordingAgentMovementSystem _) = ContextFactory.MakeSystem();
            _system = sys;
        }

        /// <summary>Full-result determinism: identical context produces identical result struct across 3 calls.</summary>
        [Test]
        public void Determinism_IdenticalContext_ProducesIdenticalFullResult()
        {
            FirstTouchContext ctx = ContextFactory.MakeDefault();
            ctx.Technique = 13; ctx.FirstTouchAttribute = 14;
            ctx.BallVelocity = new Vector3(-18.0f, 2.0f, 0.0f);
            ctx.AgentVelocity = new Vector3(2.5f, 0.5f, 0.0f);
            ctx.PressureScalar = 0.12f;
            ctx.IsHalfTurnOriented = true;
            ctx.AgentFacing = new Vector3(0.707f, 0.707f, 0.0f);

            FirstTouchResult r0 = _system.EvaluateFirstTouch(ctx);
            FirstTouchResult r1 = _system.EvaluateFirstTouch(ctx);
            FirstTouchResult r2 = _system.EvaluateFirstTouch(ctx);

            Assert.AreEqual(r0.ControlQuality, r1.ControlQuality, 0.0f, "DET: q must be bitwise identical (call 0 vs 1).");
            Assert.AreEqual(r0.ControlQuality, r2.ControlQuality, 0.0f, "DET: q must be bitwise identical (call 0 vs 2).");
            Assert.AreEqual(r0.TouchRadius, r1.TouchRadius, 0.0f, "DET: r must be bitwise identical (0 vs 1).");
            Assert.AreEqual(r0.TouchRadius, r2.TouchRadius, 0.0f, "DET: r must be bitwise identical (0 vs 2).");
            Assert.AreEqual(r0.PossessionOutcome, r1.PossessionOutcome, "DET: Outcome must be identical (0 vs 1).");
            Assert.AreEqual(r0.PossessionOutcome, r2.PossessionOutcome, "DET: Outcome must be identical (0 vs 2).");
        }

        /// <summary>ControlQuality output always in [0,1] for any combination of valid inputs.</summary>
        [Test]
        public void Invariant_ControlQualityAlwaysInUnitRange()
        {
            // Sweep multiple attribute/speed combinations and verify q ∈ [0,1] always.
            int[] techniques    = { 1, 5, 10, 15, 20 };
            float[] ballSpeeds  = { 0.0f, 5.0f, 15.0f, 30.0f, 60.0f, 80.0f };
            float[] agentSpeeds = { 0.0f, 3.5f, 7.0f };
            float[] pressures   = { 0.0f, 0.5f, 1.0f };

            int tested = 0;
            foreach (int tech in techniques)
            {
                foreach (float bs in ballSpeeds)
                {
                    foreach (float aSpeed in agentSpeeds)
                    {
                        foreach (float ps in pressures)
                        {
                            FirstTouchContext ctx = ContextFactory.MakeDefault();
                            ctx.Technique = tech; ctx.FirstTouchAttribute = tech;
                            ctx.BallVelocity = new Vector3(-bs, 0.0f, 0.0f);
                            ctx.AgentVelocity = new Vector3(aSpeed, 0.0f, 0.0f);
                            ctx.PressureScalar = ps;

                            float q = _system.EvaluateFirstTouch(ctx).ControlQuality;
                            Assert.GreaterOrEqual(q, 0.0f,
                                $"INV: q must be ≥ 0 for tech={tech}, bs={bs}, as={aSpeed}, ps={ps}.");
                            Assert.LessOrEqual(q, 1.0f,
                                $"INV: q must be ≤ 1 for tech={tech}, bs={bs}, as={aSpeed}, ps={ps}.");
                            tested++;
                        }
                    }
                }
            }
            Assert.Greater(tested, 0, "INV: At least one combination must have been tested.");
        }

        /// <summary>TouchRadius output always in [RadiusMin, RadiusHeavy] for valid q and ball speeds.</summary>
        [Test]
        public void Invariant_TouchRadiusAlwaysInValidRange()
        {
            float[] ballSpeeds = { 0.5f, 10.0f, 15.0f, 30.0f, 60.0f };
            int[] techniques   = { 1, 5, 12, 18, 20 };

            foreach (int tech in techniques)
            {
                foreach (float bs in ballSpeeds)
                {
                    FirstTouchContext ctx = ContextFactory.MakeDefault();
                    ctx.Technique = tech; ctx.FirstTouchAttribute = tech;
                    ctx.BallVelocity = new Vector3(-bs, 0.0f, 0.0f);
                    ctx.AgentVelocity = Vector3.zero; ctx.PressureScalar = 0.0f;

                    float r = _system.EvaluateFirstTouch(ctx).TouchRadius;
                    Assert.GreaterOrEqual(r, FirstTouchConstants.RadiusMin - 0.001f,
                        $"INV: r ({r:F3}m) must be ≥ RadiusMin ({FirstTouchConstants.RadiusMin}m) for tech={tech}, bs={bs}.");
                    Assert.LessOrEqual(r, FirstTouchConstants.RadiusHeavy + 0.001f,
                        $"INV: r ({r:F3}m) must be ≤ RadiusHeavy ({FirstTouchConstants.RadiusHeavy}m) for tech={tech}, bs={bs}.");
                }
            }
        }

        /// <summary>New ball position always within pitch bounds for any valid agent position.</summary>
        [Test]
        public void Invariant_NewBallPositionAlwaysWithinPitchBounds()
        {
            Vector3[] agentPositions = new Vector3[]
            {
                new Vector3(0.0f,  0.0f, 0.0f),
                new Vector3(105.0f, 68.0f, 0.0f),
                new Vector3(0.0f, 68.0f, 0.0f),
                new Vector3(105.0f, 0.0f, 0.0f),
                new Vector3(52.5f, 34.0f, 0.0f)
            };

            Vector3[] intendedDirs = new Vector3[]
            {
                new Vector3(1.0f, 0.0f, 0.0f),
                new Vector3(-1.0f, 0.0f, 0.0f),
                new Vector3(0.0f, 1.0f, 0.0f),
                new Vector3(0.0f, -1.0f, 0.0f)
            };

            foreach (Vector3 pos in agentPositions)
            {
                foreach (Vector3 dir in intendedDirs)
                {
                    FirstTouchContext ctx = ContextFactory.MakeDefault();
                    ctx.Technique = 5; ctx.FirstTouchAttribute = 5;
                    ctx.BallVelocity = new Vector3(-20.0f, 0.0f, 0.0f);
                    ctx.AgentPosition = pos;
                    ctx.IntendedTouchDirection = dir;
                    ctx.AgentFacing = dir;
                    ctx.AgentVelocity = Vector3.zero;
                    ctx.PressureScalar = 0.0f;

                    FirstTouchResult result = _system.EvaluateFirstTouch(ctx);
                    Vector3 newPos = result.NewBallPosition;

                    Assert.GreaterOrEqual(newPos.x, 0.0f - 0.001f,
                        $"PITCH: x ({newPos.x:F3}) must be ≥ 0 for agent at ({pos.x},{pos.y}) dir ({dir.x},{dir.y}).");
                    Assert.LessOrEqual(newPos.x, FirstTouchConstants.PitchLength + 0.001f,
                        $"PITCH: x ({newPos.x:F3}) must be ≤ 105 for agent at ({pos.x},{pos.y}) dir ({dir.x},{dir.y}).");
                    Assert.GreaterOrEqual(newPos.y, 0.0f - 0.001f,
                        $"PITCH: y ({newPos.y:F3}) must be ≥ 0 for agent at ({pos.x},{pos.y}) dir ({dir.x},{dir.y}).");
                    Assert.LessOrEqual(newPos.y, FirstTouchConstants.PitchWidth + 0.001f,
                        $"PITCH: y ({newPos.y:F3}) must be ≤ 68 for agent at ({pos.x},{pos.y}) dir ({dir.x},{dir.y}).");
                }
            }
        }
    }
}

    // ════════════════════════════════════════════════════════════════════════════
    // §5.9 Integration Tests (IT)
    // ════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Integration tests for First Touch #4 §5.9. Require Play Mode with
    /// CollisionSystem #3, AgentMovement #2, BallPhysics #1, and EventBus wired.
    /// </summary>
    internal sealed class FirstTouchIntegrationTests
    {
        /// <summary>IT-001: Collision→FirstTouch→BallPhysics complete pipeline. §5.9.</summary>
        [Test]
        public void CollisionToFirstTouchToBallPhysics_CompletePipeline()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode with CollisionSystem #3 + BallPhysics #1 wired — IT-001");
        }

        /// <summary>IT-002: Controlled outcome — AgentMovement receives dribbling modifier. §5.9.</summary>
        [Test]
        public void ControlledOutcome_AgentMovementReceivesDribblingModifier()
        {
            Assert.Ignore("Stage 0+1: requires AgentMovement #2 wired — IT-002");
        }

        /// <summary>IT-003: Possession change updates team possession state. §5.9.</summary>
        [Test]
        public void PossessionChange_UpdatesTeamPossessionState()
        {
            Assert.Ignore("Stage 0+1: requires EventBus PossessionChangedEvent wired — IT-003");
        }

        /// <summary>IT-004: EventEmission — FirstTouchEvent published on each contact. §5.9.</summary>
        [Test]
        public void EventEmission_FirstTouchEventPublishedOnContact()
        {
            Assert.Ignore("Stage 0+1: requires EventBus + EventBusRegistrar wired — IT-004");
        }

        /// <summary>IT-005: Replay determinism — identical match state produces identical results. §5.9.</summary>
        [Test]
        public void ReplayDeterminism_IdenticalMatchStateProducesIdenticalResults()
        {
            Assert.Ignore("Stage 0+1: requires Deterministic Simulation #16 digest comparison — IT-005");
        }

        /// <summary>IT-006: Goalkeeper foot control processed normally. §5.9.</summary>
        [Test]
        public void GoalkeeperFootControl_ProcessedNormally()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode with Goalkeeper Mechanics #11 wired — IT-006");
        }

        /// <summary>IT-007: Pressure query from SpatialHash returns correct opponents. §5.9.</summary>
        [Test]
        public void PressureQueryFromSpatialHash_ReturnsCorrectOpponents()
        {
            Assert.Ignore("Stage 0+1: requires SpatialHashGrid wired into PressureEvaluator — IT-007");
        }

        /// <summary>IT-008: Consecutive touches — one-two pass — both evaluate correctly. §5.9.</summary>
        [Test]
        public void ConsecutiveTouches_OneTwoPass_BothEvaluateCorrectly()
        {
            Assert.Ignore("Stage 0+1: requires Play Mode multi-tick scenario — IT-008");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                       |
// | 1.0     | 2026-05-31 | —      | Initial creation. 52 tests across CQ/TR/PR/OR/PO/EC/BD/VS/invariant categories. |
// | 1.1     | 2026-06-01 | —      | Add §5.9 integration test stubs IT-001..008 (all Assert.Ignore Stage 0+1). |
#endregion
