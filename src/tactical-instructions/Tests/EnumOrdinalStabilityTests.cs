// File:     src/tactical-instructions/Tests/EnumOrdinalStabilityTests.cs
// Created:  2026-06-21
// Modified: 2026-07-07
// Author:   —
// Spec:     Tactical Instructions #21 FR-TI-007, §2.2.4, Code Standards #20
// Purpose:  Mechanically locks the APPEND-only contract on all 17 instruction enums.
//           The 15 sequential enums assert (int)Member == N; the 2 [Flags] enums
//           assert bit-position stability + the 8-flag byte ceiling (FR-TI-007).
//           Any mid-insertion fails the suite immediately (#1 ball-physics precedent).

using System;

using NUnit.Framework;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.TacticalInstructions.Tests
{
    /// <summary>Ordinal- / bit-position-stability locks for the #21 enum vocabulary (FR-TI-007).</summary>
    public class EnumOrdinalStabilityTests
    {
        // ── Sequential enums (assert (int)Member == N) ────────────────────────

        [Test]
        public void Mentality_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)Mentality.VeryDefensive);
            Assert.AreEqual(1, (int)Mentality.Defensive);
            Assert.AreEqual(2, (int)Mentality.Cautious);
            Assert.AreEqual(3, (int)Mentality.Balanced);
            Assert.AreEqual(4, (int)Mentality.Positive);
            Assert.AreEqual(5, (int)Mentality.Attacking);
            Assert.AreEqual(6, (int)Mentality.VeryAttacking);
        }

        [Test]
        public void Tempo_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)Tempo.VerySlow);
            Assert.AreEqual(1, (int)Tempo.Slow);
            Assert.AreEqual(2, (int)Tempo.Standard);
            Assert.AreEqual(3, (int)Tempo.Fast);
            Assert.AreEqual(4, (int)Tempo.VeryFast);
        }

        [Test]
        public void TacticWidth_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)TacticWidth.VeryNarrow);
            Assert.AreEqual(1, (int)TacticWidth.Narrow);
            Assert.AreEqual(2, (int)TacticWidth.Standard);
            Assert.AreEqual(3, (int)TacticWidth.Wide);
            Assert.AreEqual(4, (int)TacticWidth.VeryWide);
        }

        [Test]
        public void TacticDefWidth_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)TacticDefWidth.Narrow);
            Assert.AreEqual(1, (int)TacticDefWidth.Standard);
            Assert.AreEqual(2, (int)TacticDefWidth.Wide);
        }

        [Test]
        public void LineOfEngagement_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)LineOfEngagement.VeryLow);
            Assert.AreEqual(1, (int)LineOfEngagement.Low);
            Assert.AreEqual(2, (int)LineOfEngagement.Standard);
            Assert.AreEqual(3, (int)LineOfEngagement.High);
            Assert.AreEqual(4, (int)LineOfEngagement.VeryHigh);
        }

        [Test]
        public void TransitionPlan_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)TransitionPlan.CounterAttack);
            Assert.AreEqual(1, (int)TransitionPlan.HoldShape);
            Assert.AreEqual(2, (int)TransitionPlan.CounterPress);
            Assert.AreEqual(3, (int)TransitionPlan.Regroup);
        }

        [Test]
        public void GkDistributionPolicy_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)GkDistributionPolicy.SlowDown);
            Assert.AreEqual(1, (int)GkDistributionPolicy.Quick);
            Assert.AreEqual(2, (int)GkDistributionPolicy.ShortKick);
            Assert.AreEqual(3, (int)GkDistributionPolicy.LongKick);
            Assert.AreEqual(4, (int)GkDistributionPolicy.RollOut);
            Assert.AreEqual(5, (int)GkDistributionPolicy.ThrowOut);
        }

        [Test]
        public void FocusPlay_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)FocusPlay.Mixed);
            Assert.AreEqual(1, (int)FocusPlay.LeftFlank);
            Assert.AreEqual(2, (int)FocusPlay.RightFlank);
            Assert.AreEqual(3, (int)FocusPlay.ThroughMiddle);
        }

        [Test]
        public void TacticPassing_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)TacticPassing.Short);
            Assert.AreEqual(1, (int)TacticPassing.Mixed);
            Assert.AreEqual(2, (int)TacticPassing.Direct);
        }

        [Test]
        public void TacticPressing_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)TacticPressing.Low);
            Assert.AreEqual(1, (int)TacticPressing.Medium);
            Assert.AreEqual(2, (int)TacticPressing.High);
        }

        [Test]
        public void TacticFormation_OrdinalsAreStable()
        {
            // Ordinals match #12 FormationFamily; keep them in lock-step.
            Assert.AreEqual(0, (int)TacticFormation.F442);
            Assert.AreEqual(1, (int)TacticFormation.F433);
            Assert.AreEqual(2, (int)TacticFormation.F4231);
        }

        [Test]
        public void Duty_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)Duty.Defend);
            Assert.AreEqual(1, (int)Duty.Support);
            Assert.AreEqual(2, (int)Duty.Attack);
        }

        [Test]
        public void PlayerRole_OrdinalsAreStable()
        {
            // Row index into RoleWeightModifiers; index 0 is the all-1.0 identity row.
            Assert.AreEqual(0, (int)PlayerRole.Default);
            Assert.AreEqual(1, (int)PlayerRole.Poacher);
            Assert.AreEqual(2, (int)PlayerRole.DeepLyingPlaymaker);
            Assert.AreEqual(3, (int)PlayerRole.BallWinningMid);
            Assert.AreEqual(4, (int)PlayerRole.InsideForward);
            Assert.AreEqual(5, (int)PlayerRole.TargetMan);
        }

        [Test]
        public void InstrBias_OrdinalsAreStable()
        {
            Assert.AreEqual(0, (int)InstrBias.Less);
            Assert.AreEqual(1, (int)InstrBias.Default);
            Assert.AreEqual(2, (int)InstrBias.More);
        }

        [Test]
        public void MarkingOrientation_OrdinalsAreStable()
        {
            // Row index into MarkingOrientationScalar; index 1 is the identity row.
            Assert.AreEqual(0, (int)MarkingOrientation.BallOriented);
            Assert.AreEqual(1, (int)MarkingOrientation.Balanced);
            Assert.AreEqual(2, (int)MarkingOrientation.ManOriented);
        }

        // ── [Flags] enums (assert bit positions + the 8-flag byte ceiling) ────

        [Test]
        public void TacticTriggerMask_BitPositionsAreStable()
        {
            Assert.AreEqual(0, (int)TacticTriggerMask.None);
            Assert.AreEqual(1, (int)TacticTriggerMask.BadTouch);
            Assert.AreEqual(2, (int)TacticTriggerMask.BackwardPass);
            Assert.AreEqual(4, (int)TacticTriggerMask.SidelineTrap);
            Assert.AreEqual(8, (int)TacticTriggerMask.WeakReceiver);
        }

        [Test]
        public void SetPieceDutyFlags_BitPositionsAreStable()
        {
            Assert.AreEqual(0, (int)SetPieceDutyFlags.None);
            Assert.AreEqual(1, (int)SetPieceDutyFlags.FreeKickTaker);
            Assert.AreEqual(2, (int)SetPieceDutyFlags.CornerTaker);
            Assert.AreEqual(4, (int)SetPieceDutyFlags.PenaltyTaker);
        }

        // ── byte backing + the 8-flag ceiling (FR-TI-007) ─────────────────────

        [Test]
        public void AllInstructionEnums_AreByteBacked()
        {
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(Mentality)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(Tempo)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(TacticWidth)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(TacticDefWidth)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(LineOfEngagement)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(TransitionPlan)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(GkDistributionPolicy)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(FocusPlay)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(TacticPassing)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(TacticPressing)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(TacticTriggerMask)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(TacticFormation)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(Duty)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(PlayerRole)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(InstrBias)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(SetPieceDutyFlags)));
            Assert.AreEqual(typeof(byte), Enum.GetUnderlyingType(typeof(MarkingOrientation)));
        }

        [Test]
        public void FlagEnums_EachFlagIsASingleBitWithinTheByteCeiling()
        {
            // FR-TI-007 "8-flag byte ceiling": each flag must occupy a single bit no higher than
            // bit 7 (value ≤ 128), so at most 8 distinct flags ever fit the byte backing. A bare
            // `<= 255` check is vacuous (any byte-backed value satisfies it); the single-bit +
            // ≤ 128 check actually guards the ceiling — a 9th flag would need bit value 256.
            foreach (TacticTriggerMask flag in Enum.GetValues(typeof(TacticTriggerMask)))
            {
                AssertSingleBitWithinByte((int)flag);
            }

            foreach (SetPieceDutyFlags flag in Enum.GetValues(typeof(SetPieceDutyFlags)))
            {
                AssertSingleBitWithinByte((int)flag);
            }
        }

        private static void AssertSingleBitWithinByte(int value)
        {
            if (value == 0)
            {
                return; // None sentinel occupies no bit.
            }

            Assert.LessOrEqual(value, 128, "flag bit must not exceed bit 7 (the byte [Flags] ceiling)");
            Assert.AreEqual(0, value & (value - 1), "each flag must be a single power-of-two bit");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                                 |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).                                      |
// | 1.1     | 2026-06-21 | —      | AR-1 L-2: flag-ceiling test was vacuous (`<= 255` holds for any byte  |
// |         |            |        | enum); now asserts each flag is a single power-of-two bit ≤ 128.      |
// | 1.2     | 2026-07-07 | —      | Cheap-item addition: + MarkingOrientation ordinal + byte-backing lock.|
#endregion
