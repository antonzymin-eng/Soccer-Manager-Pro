// File:     src/positioning-ai/Tests/BuildUpStructureTests.cs
// Created:  2026-07-10
// Modified: 2026-07-10
// Author:   —
// Spec:     Build-Up Structures #24 §3.1–§3.3, §5 test plan (T-BU-U-001..012 subset), Code Standards #20
// Purpose:  Locks the #24 T0 pure math: FM-BU-01 hysteresis worked examples (incl. the long-ball
//           jump), FM-BU-03 suppression arming/decrement, the None/FinalThird identities, the
//           FR-BU-008 catalogue bound, the centreline sign rule, and the ERR-024-001 regression
//           that every family actually receives a non-zero BackThree own-third overlay.

using System;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.PositioningAI.Tests
{
    [TestFixture]
    internal class BuildUpStructureTests
    {
        private const float Tolerance = 1e-5f;

        // ---------- §3.1 FM-BU-01 classifier ----------

        [Test]
        public void Classifier_WorkedExample_HysteresisHoldsAndCommits()
        {
            // §3.1 worked example: committed OwnThird claims [0, 37.0).
            Assert.AreEqual(BuildUpZone.OwnThird, BuildUpZoneClassifier.Classify(BuildUpZone.OwnThird, 35.5f));
            Assert.AreEqual(BuildUpZone.MiddleThird, BuildUpZoneClassifier.Classify(BuildUpZone.OwnThird, 37.2f));
            // MiddleThird claims [33.0, 72.0).
            Assert.AreEqual(BuildUpZone.MiddleThird, BuildUpZoneClassifier.Classify(BuildUpZone.MiddleThird, 34.1f));
            Assert.AreEqual(BuildUpZone.OwnThird, BuildUpZoneClassifier.Classify(BuildUpZone.MiddleThird, 32.9f));
        }

        [Test]
        public void Classifier_LongBall_JumpsDirectlyToFinalThird()
        {
            // §3.1: committed OwnThird, ball cleared to x = 80 → rawZone(80) = FinalThird directly.
            Assert.AreEqual(BuildUpZone.FinalThird, BuildUpZoneClassifier.Classify(BuildUpZone.OwnThird, 80f));
        }

        [Test]
        public void Classifier_NonFiniteX_HoldsCommittedZonePerF1()
        {
            Assert.AreEqual(BuildUpZone.MiddleThird,
                BuildUpZoneClassifier.Classify(BuildUpZone.MiddleThird, float.NaN));
            Assert.AreEqual(BuildUpZone.OwnThird,
                BuildUpZoneClassifier.Classify(BuildUpZone.OwnThird, float.PositiveInfinity));
        }

        [Test]
        public void Classifier_RawZone_ThresholdsAreThirds()
        {
            Assert.AreEqual(BuildUpZone.OwnThird, BuildUpZoneClassifier.RawZone(34.9f));
            Assert.AreEqual(BuildUpZone.MiddleThird, BuildUpZoneClassifier.RawZone(35.0f));
            Assert.AreEqual(BuildUpZone.MiddleThird, BuildUpZoneClassifier.RawZone(69.9f));
            Assert.AreEqual(BuildUpZone.FinalThird, BuildUpZoneClassifier.RawZone(70.0f));
        }

        [Test]
        public void Classifier_FinalThirdHysteresis_HoldsAboveT2MinusH()
        {
            // FinalThird claims [68.0, 105]: 68.5 holds, 67.9 re-commits MiddleThird.
            Assert.AreEqual(BuildUpZone.FinalThird, BuildUpZoneClassifier.Classify(BuildUpZone.FinalThird, 68.5f));
            Assert.AreEqual(BuildUpZone.MiddleThird, BuildUpZoneClassifier.Classify(BuildUpZone.FinalThird, 67.9f));
        }

        // ---------- §3.3 FM-BU-03 suppression window ----------

        [Test]
        public void Suppression_ArmsOnlyForCounterTransitionPlans()
        {
            BuildUpZoneState s = default;
            Assert.AreEqual(PositioningAIConstants.REGAIN_SUPPRESS_TICKS,
                BuildUpZoneClassifier.ArmOnTeamRegain(in s, TransitionPlan.CounterAttack).SuppressTicksRemaining);
            Assert.AreEqual(PositioningAIConstants.REGAIN_SUPPRESS_TICKS,
                BuildUpZoneClassifier.ArmOnTeamRegain(in s, TransitionPlan.CounterPress).SuppressTicksRemaining);
            Assert.AreEqual(0,
                BuildUpZoneClassifier.ArmOnTeamRegain(in s, TransitionPlan.HoldShape).SuppressTicksRemaining);
            Assert.AreEqual(0,
                BuildUpZoneClassifier.ArmOnTeamRegain(in s, TransitionPlan.Regroup).SuppressTicksRemaining);
        }

        [Test]
        public void Suppression_DecrementsToZero_AndStays()
        {
            // §3.3 worked example: armed 30, regain at heartbeat 100 → closed from 130.
            BuildUpZoneState s = BuildUpZoneClassifier.ArmOnTeamRegain(default, TransitionPlan.CounterAttack);
            for (int i = 0; i < PositioningAIConstants.REGAIN_SUPPRESS_TICKS; i++)
            {
                Assert.Greater(s.SuppressTicksRemaining, 0);
                s = BuildUpZoneClassifier.TickSuppression(in s);
            }
            Assert.AreEqual(0, s.SuppressTicksRemaining);
            s = BuildUpZoneClassifier.TickSuppression(in s);
            Assert.AreEqual(0, s.SuppressTicksRemaining);
        }

        // ---------- §3.2 FM-BU-02 catalogue ----------

        [Test]
        public void Catalogue_None_IsExactZero_ForEveryCombination()
        {
            // FR-BU-005: the zero-value dial is the exact identity.
            foreach (BuildUpZone zone in (BuildUpZone[])Enum.GetValues(typeof(BuildUpZone)))
            {
                foreach (LineId line in (LineId[])Enum.GetValues(typeof(LineId)))
                {
                    foreach (LaneId lane in (LaneId[])Enum.GetValues(typeof(LaneId)))
                    {
                        Assert.AreEqual(Vector2.zero, BuildUpOverlayCatalogue.GetOffset(
                            BuildUpStructure.None, zone, line, lane, 12f));
                    }
                }
            }
        }

        [Test]
        public void Catalogue_FinalThird_IsAllZero_ByRule()
        {
            // FR-BU-004: build-up structure is an own/middle-third concept.
            foreach (BuildUpStructure st in (BuildUpStructure[])Enum.GetValues(typeof(BuildUpStructure)))
            {
                foreach (LineId line in (LineId[])Enum.GetValues(typeof(LineId)))
                {
                    foreach (LaneId lane in (LaneId[])Enum.GetValues(typeof(LaneId)))
                    {
                        Assert.AreEqual(Vector2.zero, BuildUpOverlayCatalogue.GetOffset(
                            st, BuildUpZone.FinalThird, line, lane, 12f));
                    }
                }
            }
        }

        [Test]
        public void Catalogue_EveryRow_SatisfiesOffsetBound()
        {
            // FR-BU-008 (T-BU-U-004): |Δx|,|Δy| ≤ BUILDUP_OFFSET_MAX_M over the full key space.
            foreach (BuildUpStructure st in (BuildUpStructure[])Enum.GetValues(typeof(BuildUpStructure)))
            {
                foreach (BuildUpZone zone in (BuildUpZone[])Enum.GetValues(typeof(BuildUpZone)))
                {
                    foreach (LineId line in (LineId[])Enum.GetValues(typeof(LineId)))
                    {
                        foreach (LaneId lane in (LaneId[])Enum.GetValues(typeof(LaneId)))
                        {
                            Vector2 d = BuildUpOverlayCatalogue.GetOffset(st, zone, line, lane, 12f);
                            Assert.LessOrEqual(Math.Abs(d.x), PositioningAIConstants.BUILDUP_OFFSET_MAX_M);
                            Assert.LessOrEqual(Math.Abs(d.y), PositioningAIConstants.BUILDUP_OFFSET_MAX_M);
                        }
                    }
                }
            }
        }

        [Test]
        public void Catalogue_WorkedExample_BackThreeOwnThird_LeftBackTucks()
        {
            // §3.2 worked example (re-keyed per ERR-024-001): a left back at (18, 12) — DEF line,
            // half-space lane per the recorded family data — moves to (14, 18): Δ = (−4, +6).
            Vector2 d = BuildUpOverlayCatalogue.GetOffset(
                BuildUpStructure.BackThree, BuildUpZone.OwnThird, LineId.Defense, LaneId.LH, 12f);
            Assert.AreEqual(-4.0f, d.x, Tolerance);
            Assert.AreEqual(6.0f, d.y, Tolerance);
        }

        [Test]
        public void Catalogue_LateralSign_ResolvesTowardCentre()
        {
            // §3.2 / PASS-1 L-1: y < 34 ⇒ +, y > 34 ⇒ −, exactly 34 ⇒ 0.
            Vector2 below = BuildUpOverlayCatalogue.GetOffset(
                BuildUpStructure.BackThree, BuildUpZone.OwnThird, LineId.Defense, LaneId.LH, 12f);
            Vector2 above = BuildUpOverlayCatalogue.GetOffset(
                BuildUpStructure.BackThree, BuildUpZone.OwnThird, LineId.Defense, LaneId.RH, 56f);
            Vector2 exact = BuildUpOverlayCatalogue.GetOffset(
                BuildUpStructure.BackThree, BuildUpZone.OwnThird, LineId.Defense, LaneId.RH,
                PositioningAIConstants.PITCH_HALF_WIDTH_M);
            Assert.Greater(below.y, 0f);
            Assert.Less(above.y, 0f);
            Assert.AreEqual(0f, exact.y);
            Assert.AreEqual(below.y, -above.y, Tolerance, "lane-symmetric magnitude");
        }

        [Test]
        public void Catalogue_RowKeys_HitEveryFamily_Err024001Regression()
        {
            // ERR-024-001: the spec's v0.2 lane keys matched NO slot (fullbacks are recorded LH/RH,
            // not wide L/R) — the whole catalogue was a structural no-op. Lock that at least one
            // outfield slot in EVERY family receives a non-zero offset for each structure's
            // own-third table.
            foreach (FormationFamily family in (FormationFamily[])Enum.GetValues(typeof(FormationFamily)))
            {
                FormationSlotRecord[] table = family switch
                {
                    FormationFamily.F433 => PositioningAIConstants.Family433,
                    FormationFamily.F4231 => PositioningAIConstants.Family4231,
                    _ => PositioningAIConstants.Family442,
                };
                foreach (BuildUpStructure st in new[]
                {
                    BuildUpStructure.BackThree, BuildUpStructure.DoublePivot, BuildUpStructure.InvertedFullBacks,
                })
                {
                    bool anyHit = false;
                    for (int i = 0; i < table.Length; i++)
                    {
                        if (table[i].IsGoalkeeper)
                        {
                            continue; // FR-BU-009.
                        }
                        Vector2 d = BuildUpOverlayCatalogue.GetOffset(
                            st, BuildUpZone.OwnThird, table[i].DefaultLine, table[i].DefaultLane,
                            table[i].LateralPct * PositioningAIConstants.PITCH_WIDTH_M);
                        if (d != Vector2.zero)
                        {
                            anyHit = true;
                            break;
                        }
                    }
                    Assert.IsTrue(anyHit, $"{st} own-third overlay hits no slot in {family}");
                }
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                   |
// | 1.0     | 2026-07-10 | —      | Initial T0 suite (#24): FM-BU-01/03 worked examples, catalogue bound + identities + ERR-024-001 regression. |
#endregion
