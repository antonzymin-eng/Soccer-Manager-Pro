// File:     src/positioning-ai/Tests/MarkingPressureEvaluatorTests.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Dismarking AI #23 §3.1–§3.3 (FM-DM-01/02), §5 test plan, Code Standards #20
// Purpose:  Locks the #23 T0 pure math: §3.1/§3.3 worked examples, the §3.2 dwell state machine
//           (decay ramp, phase freeze-with-decay, marker-handoff no-reset), the Off identity,
//           the pressure floor, and the F1/F3 NaN/degenerate gates.

using System;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.PositioningAI.Tests
{
    [TestFixture]
    internal class MarkingPressureEvaluatorTests
    {
        private const float Tolerance = 1e-5f;

        // ---------- §3.1 FM-DM-01 ----------

        [Test]
        public void Pressure_WorkedExample_MarkerAt1p2m_Dwell7_Is0p42()
        {
            // §3.1 worked example: proximity01 = 1 − 1.2/3.0 = 0.6; dwell01 = 0.7 → 0.42.
            float p = MarkingPressureEvaluator.ComputePressure(
                Phase.InPoss, markerExists: true, markerDistance: 1.2f, dwellTicks: 7);
            Assert.AreEqual(0.42f, p, Tolerance);
        }

        [Test]
        public void Pressure_OutOfPhase_IsZero_Identity()
        {
            // FR-DM-006: any phase other than InPoss returns the identity.
            Assert.AreEqual(0f, MarkingPressureEvaluator.ComputePressure(Phase.OutOfPoss, true, 0.5f, 10));
            Assert.AreEqual(0f, MarkingPressureEvaluator.ComputePressure(Phase.TransToAtk, true, 0.5f, 10));
            Assert.AreEqual(0f, MarkingPressureEvaluator.ComputePressure(Phase.TransToDef, true, 0.5f, 10));
        }

        [Test]
        public void Pressure_NoMarker_OrNaNDistance_IsZero()
        {
            Assert.AreEqual(0f, MarkingPressureEvaluator.ComputePressure(Phase.InPoss, false, 1.0f, 10));
            Assert.AreEqual(0f, MarkingPressureEvaluator.ComputePressure(Phase.InPoss, true, float.NaN, 10));
        }

        [Test]
        public void Pressure_DwellSaturates_AtFullTicks()
        {
            // dwell01 caps at 1 — dwell 10 and 10+ give the same pressure at fixed distance.
            float atCap = MarkingPressureEvaluator.ComputePressure(
                Phase.InPoss, true, 0f, PositioningAIConstants.MARKING_DWELL_FULL_TICKS);
            Assert.AreEqual(1f, atCap, Tolerance);
        }

        // ---------- §3.1 marker search ----------

        [Test]
        public void FindMarker_PicksNearestInsideRadius_SkipsNonFinite()
        {
            Vector2 agent = new Vector2(50f, 30f);
            Span<Vector2> pos = stackalloc Vector2[3]
            {
                new Vector2(52.5f, 30f),               // 2.5 m — qualifies
                new Vector2(float.NaN, 30f),           // F1 — skipped
                new Vector2(51.2f, 30f),               // 1.2 m — nearest
            };
            Span<int> ids = stackalloc int[3] { 7, 8, 9 };

            bool found = MarkingPressureEvaluator.TryFindNearestMarker(
                agent, pos, ids, out int markerId, out Vector2 markerPos, out float d);

            Assert.IsTrue(found);
            Assert.AreEqual(9, markerId);
            Assert.AreEqual(1.2f, d, Tolerance);
            Assert.AreEqual(51.2f, markerPos.x, Tolerance);
        }

        [Test]
        public void FindMarker_OutsideRadius_ReturnsFalse()
        {
            Vector2 agent = new Vector2(50f, 30f);
            Span<Vector2> pos = stackalloc Vector2[1] { new Vector2(54f, 30f) }; // 4.0 m > 3.0 m
            Span<int> ids = stackalloc int[1] { 7 };
            bool found = MarkingPressureEvaluator.TryFindNearestMarker(
                agent, pos, ids, out int markerId, out _, out _);
            Assert.IsFalse(found);
            Assert.AreEqual(MarkingDwellState.NoMarker, markerId);
        }

        [Test]
        public void FindMarker_MismatchedSpans_Throws()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                Span<Vector2> pos = stackalloc Vector2[2];
                Span<int> ids = stackalloc int[1];
                MarkingPressureEvaluator.TryFindNearestMarker(
                    Vector2.zero, pos, ids, out _, out _, out _);
            });
        }

        // ---------- §3.2 dwell state machine ----------

        [Test]
        public void Dwell_Accumulates_CapsAtFullTicks_AndTracksMarkerId()
        {
            MarkingDwellState s = MarkingDwellState.Unmarked;
            for (int i = 0; i < PositioningAIConstants.MARKING_DWELL_FULL_TICKS + 3; i++)
            {
                s = MarkingPressureEvaluator.UpdateDwell(in s, Phase.InPoss, markerExists: true, markerId: 4);
            }
            Assert.AreEqual(PositioningAIConstants.MARKING_DWELL_FULL_TICKS, s.DwellTicks);
            Assert.AreEqual(4, s.LastMarkerId);
        }

        [Test]
        public void Dwell_DecayWorkedExample_ReachesZeroAfterFiveUnmarkedHeartbeats()
        {
            // §3.2 worked example: 10 → 8 → 6 → 4 → 2 → 0 (decay 2/tick).
            MarkingDwellState s = new MarkingDwellState
            {
                DwellTicks = PositioningAIConstants.MARKING_DWELL_FULL_TICKS,
                LastMarkerId = 4,
            };
            for (int i = 0; i < 4; i++)
            {
                s = MarkingPressureEvaluator.UpdateDwell(in s, Phase.InPoss, false, MarkingDwellState.NoMarker);
                Assert.Greater(s.DwellTicks, 0);
                Assert.AreEqual(4, s.LastMarkerId, "id clears only at zero dwell (§3.2)");
            }
            s = MarkingPressureEvaluator.UpdateDwell(in s, Phase.InPoss, false, MarkingDwellState.NoMarker);
            Assert.AreEqual(0, s.DwellTicks);
            Assert.AreEqual(MarkingDwellState.NoMarker, s.LastMarkerId);
        }

        [Test]
        public void Dwell_OutOfPhase_DecaysButKeepsMarkerId()
        {
            // FR-DM-006: accumulation stops, decay continues, LastMarkerId stays (cheap resume).
            MarkingDwellState s = new MarkingDwellState { DwellTicks = 6, LastMarkerId = 4 };
            s = MarkingPressureEvaluator.UpdateDwell(in s, Phase.OutOfPoss, markerExists: true, markerId: 9);
            Assert.AreEqual(4, s.DwellTicks);
            Assert.AreEqual(4, s.LastMarkerId, "out-of-phase never accumulates or retargets");
        }

        [Test]
        public void Dwell_MarkerHandoff_DoesNotResetDwell()
        {
            // §3.2: being handed marker-to-marker is still being marked.
            MarkingDwellState s = new MarkingDwellState { DwellTicks = 6, LastMarkerId = 4 };
            s = MarkingPressureEvaluator.UpdateDwell(in s, Phase.InPoss, markerExists: true, markerId: 9);
            Assert.AreEqual(7, s.DwellTicks);
            Assert.AreEqual(9, s.LastMarkerId);
        }

        // ---------- §3.3 FM-DM-02 offset ----------

        [Test]
        public void Offset_WorkedExample_Pressure0p42_Aggressive_Is1p05m_AwayFromMarker()
        {
            // §3.3 worked example: mag = 2.5 × 0.42 × 1.0 = 1.05 m, directly away from the marker.
            Vector2 agent = new Vector2(50f, 30f);
            Vector2 marker = new Vector2(49f, 30f); // marker directly to the −x side
            Vector2 off = MarkingPressureEvaluator.ComputeOffset(agent, marker, 0.42f, DismarkIntensity.Aggressive);
            Assert.AreEqual(1.05f, off.magnitude, Tolerance);
            Assert.Greater(off.x, 0f, "offset points away from the marker (+x here)");
            Assert.AreEqual(0f, off.y, Tolerance);
        }

        [Test]
        public void Offset_OffDial_IsExactZero_Identity()
        {
            // FR-DM-012: Off is the exact identity, checked before any distance math (§6.3).
            Vector2 off = MarkingPressureEvaluator.ComputeOffset(
                new Vector2(50f, 30f), new Vector2(49f, 30f), 1.0f, DismarkIntensity.Off);
            Assert.AreEqual(Vector2.zero, off);
        }

        [Test]
        public void Offset_ConservativeScalar_Applies()
        {
            Vector2 off = MarkingPressureEvaluator.ComputeOffset(
                new Vector2(50f, 30f), new Vector2(49f, 30f), 1.0f, DismarkIntensity.Conservative);
            Assert.AreEqual(
                PositioningAIConstants.DISMARK_OFFSET_MAX_M * 0.6f, off.magnitude, Tolerance);
        }

        [Test]
        public void Offset_AtOrBelowPressureFloor_IsZero()
        {
            Vector2 agent = new Vector2(50f, 30f);
            Vector2 marker = new Vector2(49f, 30f);
            Assert.AreEqual(Vector2.zero, MarkingPressureEvaluator.ComputeOffset(
                agent, marker, PositioningAIConstants.DISMARK_PRESSURE_FLOOR, DismarkIntensity.Aggressive));
            Assert.AreEqual(Vector2.zero, MarkingPressureEvaluator.ComputeOffset(
                agent, marker, float.NaN, DismarkIntensity.Aggressive));
        }

        [Test]
        public void Offset_CoincidentMarker_SkipsPerF3()
        {
            Vector2 agent = new Vector2(50f, 30f);
            Vector2 marker = new Vector2(50f, 30f + PositioningAIConstants.DISMARK_MIN_MARKER_DIST_EPS * 0.5f);
            Assert.AreEqual(Vector2.zero,
                MarkingPressureEvaluator.ComputeOffset(agent, marker, 0.9f, DismarkIntensity.Aggressive));
        }

        [Test]
        public void Offset_NonFinitePositions_AreGatedPerF1()
        {
            Assert.AreEqual(Vector2.zero, MarkingPressureEvaluator.ComputeOffset(
                new Vector2(float.NaN, 30f), new Vector2(49f, 30f), 0.9f, DismarkIntensity.Aggressive));
            Assert.AreEqual(Vector2.zero, MarkingPressureEvaluator.ComputeOffset(
                new Vector2(50f, 30f), new Vector2(float.PositiveInfinity, 30f), 0.9f, DismarkIntensity.Aggressive));
        }

        [Test]
        public void Offset_UndefinedDialByte_RefusesPerF4()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                MarkingPressureEvaluator.ComputeOffset(
                    new Vector2(50f, 30f), new Vector2(49f, 30f), 0.9f, (DismarkIntensity)7));
        }

        [Test]
        public void ScalarTable_OffRowIsExactlyZero()
        {
            // FR-DM-012: the Off row is the identity and must stay exactly 0.0.
            Assert.AreEqual(0f, PositioningAIConstants.DISMARK_INTENSITY_SCALAR[(int)DismarkIntensity.Off]);
            Assert.AreEqual(3, PositioningAIConstants.DISMARK_INTENSITY_SCALAR.Length);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial T0 suite (#23): FM-DM-01/02 worked examples, dwell machine, gates. |
#endregion
