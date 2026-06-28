// File:     src/decision-tree/Tests/TacticTranslationTests.cs
// Created:  2026-06-28
// Modified: 2026-06-28
// Author:   —
// Spec:     Tactical Instructions #21 §3.1, §3.2, §4.7; Code Standards #20
// Purpose:  Locks the #21 → #8 T2 consumer seam: enum-translation validity + F5 clamp,
//           Mentality identity rows (FR-TI-031), and the §3.2 risk/line gradation shape.

using NUnit.Framework;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.DecisionTree.Tests
{
    [TestFixture]
    internal class TacticTranslationTests
    {
        // ── §3.1 / §4.7 check 1: maps onto a valid subsystem enum (by rank, not ordinal) ──

        [Test]
        public void ToPressingMode_MapsByName_NotRawOrdinal()
        {
            Assert.AreEqual(PressingMode.LOW,    TacticTranslation.ToPressingMode(TacticPressing.Low));
            Assert.AreEqual(PressingMode.MEDIUM, TacticTranslation.ToPressingMode(TacticPressing.Medium));
            Assert.AreEqual(PressingMode.HIGH,   TacticTranslation.ToPressingMode(TacticPressing.High));
        }

        [Test]
        public void ToPassingStyle_MapsByName_NotRawOrdinal()
        {
            Assert.AreEqual(PassingStyle.SHORT,  TacticTranslation.ToPassingStyle(TacticPassing.Short));
            Assert.AreEqual(PassingStyle.MIXED,  TacticTranslation.ToPassingStyle(TacticPassing.Mixed));
            Assert.AreEqual(PassingStyle.DIRECT, TacticTranslation.ToPassingStyle(TacticPassing.Direct));
        }

        // The #21 and #8 enums order oppositely (Low=0 vs HIGH=0). A naive (cast) would invert;
        // these assert the rank mapping did not collapse to a raw cast.
        [Test]
        public void Translation_DoesNotInvert()
        {
            Assert.AreNotEqual((int)TacticPressing.Low,  (int)TacticTranslation.ToPressingMode(TacticPressing.Low));
            Assert.AreNotEqual((int)TacticPassing.Short, (int)TacticTranslation.ToPassingStyle(TacticPassing.Short));
        }

        // ── §3.1 F5: a widened (out-of-range) value clamps to the nearest peer ──

        [Test]
        public void ToPressingMode_WidenedValue_ClampsToHigh()
        {
            Assert.AreEqual(PressingMode.HIGH, TacticTranslation.ToPressingMode((TacticPressing)99));
        }

        [Test]
        public void ToPassingStyle_WidenedValue_ClampsToDirect()
        {
            Assert.AreEqual(PassingStyle.DIRECT, TacticTranslation.ToPassingStyle((TacticPassing)99));
        }

        // ── §3.2 / §4.7 check 3: Balanced is the exact identity (FR-TI-031) ──

        [Test]
        public void MentalityRiskMultiplier_Balanced_IsExactlyOne()
        {
            Assert.AreEqual(1.0f, TacticTranslation.MentalityRiskMultiplier(Mentality.Balanced));
        }

        [Test]
        public void MentalityLineBias_Balanced_IsExactlyZero()
        {
            Assert.AreEqual(0.0f, TacticTranslation.MentalityLineBias(Mentality.Balanced));
        }

        // The Stage 0 default context must resolve to the identity multiplier so the seam is a no-op.
        [Test]
        public void Stage0Default_ResolvesToIdentityRisk()
        {
            TacticalContext ctx = TacticalContext.Stage0Default(UnityEngine.Vector2.zero);
            Assert.AreEqual(1.0f, TacticTranslation.MentalityRiskMultiplier(ctx.Mentality));
        }

        // ── §3.2 shape: risk/line are monotone in mentality (the reviewable contract) ──

        [Test]
        public void MentalityRiskMultiplier_IsMonotoneIncreasing()
        {
            Assert.Less(TacticTranslation.MentalityRiskMultiplier(Mentality.VeryDefensive),
                        TacticTranslation.MentalityRiskMultiplier(Mentality.Balanced));
            Assert.Less(TacticTranslation.MentalityRiskMultiplier(Mentality.Balanced),
                        TacticTranslation.MentalityRiskMultiplier(Mentality.VeryAttacking));
        }

        [Test]
        public void MentalityLineBias_IsMonotoneIncreasing()
        {
            Assert.Less(TacticTranslation.MentalityLineBias(Mentality.VeryDefensive),
                        TacticTranslation.MentalityLineBias(Mentality.Balanced));
            Assert.Less(TacticTranslation.MentalityLineBias(Mentality.Balanced),
                        TacticTranslation.MentalityLineBias(Mentality.VeryAttacking));
        }

        [Test]
        public void MentalityRiskMultiplier_WidenedValue_ClampsToTableBounds()
        {
            Assert.AreEqual(TacticTranslation.MentalityRiskMultiplier(Mentality.VeryAttacking),
                            TacticTranslation.MentalityRiskMultiplier((Mentality)99));
        }
    }
}
