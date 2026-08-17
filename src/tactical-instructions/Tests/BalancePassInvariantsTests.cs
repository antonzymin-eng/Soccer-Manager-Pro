// File:     src/tactical-instructions/Tests/BalancePassInvariantsTests.cs
// Created:  2026-06-30
// Modified: 2026-08-08
// Author:   —
// Spec:     Tactical Instructions #21 §3.2, §3.3, §3.4, §5.6, Appendix A, FR-TI-031, Code Standards #20
// Purpose:  Locks the §5.6 / G2 balance-pass-PINNED [GT] magnitudes and the numerical-mirror invariants
//           the review checked: exact identity rows (behaviour-neutral default), strict monotonicity of
//           the Mentality/width/line tables (gradation preserved), and [0.5,2.0] RoleWeightModifiers with
//           the §3.3 directional shapes. A future edit that breaks an invariant fails the gate.

using NUnit.Framework;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.TacticalInstructions.Tests
{
    /// <summary>§5.6 / G2 balance-pass invariant locks for the pinned [GT] magnitudes.</summary>
    public class BalancePassInvariantsTests
    {
        // ── Pinned exact values (§3.2 table) ──────────────────────────────────

        [Test]
        public void MentalityRiskMult_PinnedExactValues()
        {
            float[] expected = { 0.80f, 0.88f, 0.94f, 1.00f, 1.06f, 1.14f, 1.20f };
            CollectionAssert.AreEqual(expected, TacticalInstructionsConstants.MentalityRiskMult);
        }

        [Test]
        public void MentalityLineBias_PinnedExactValues()
        {
            float[] expected = { -0.20f, -0.12f, -0.05f, 0.00f, 0.05f, 0.12f, 0.20f };
            CollectionAssert.AreEqual(expected, TacticalInstructionsConstants.MentalityLineBias);
        }

        [Test]
        public void InstrBiasMult_PinnedExactValues()
        {
            float[] expected = { 0.85f, 1.00f, 1.15f };
            CollectionAssert.AreEqual(expected, TacticalInstructionsConstants.InstrBiasMult);
        }

        // ── Identity rows are EXACT (FR-TI-031 — default tactic byte-identical) ─

        [Test]
        public void IdentityRows_AreExactlyNeutral()
        {
            Assert.AreEqual(1.00f, TacticalInstructionsConstants.MentalityRiskMult[(int)Mentality.Balanced]);
            Assert.AreEqual(0.00f, TacticalInstructionsConstants.MentalityLineBias[(int)Mentality.Balanced]);
            Assert.AreEqual(1.00f, TacticalInstructionsConstants.InstrBiasMult[(int)InstrBias.Default]);
            Assert.AreEqual(1.00f, TacticalInstructionsConstants.TempoBreadthScalar[(int)Tempo.Standard]);
            Assert.AreEqual(1.00f, TacticalInstructionsConstants.WidthScalar[(int)TacticWidth.Standard]);
            Assert.AreEqual(1.00f, TacticalInstructionsConstants.DefWidthScalar[(int)TacticDefWidth.Standard]);
            Assert.AreEqual(1.00f, TacticalInstructionsConstants.LineOfEngagementScalar[(int)LineOfEngagement.Standard]);
            Assert.AreEqual(0.00f, TacticalInstructionsConstants.DutyAggressionBias[(int)Duty.Support]);

            // Tempo identity row (Standard) and Default role row are all 1.0.
            foreach (float v in TacticalInstructionsConstants.TempoActionBias[(int)Tempo.Standard])
            {
                Assert.AreEqual(1.0f, v);
            }
            foreach (float v in TacticalInstructionsConstants.RoleWeightModifiers[(int)PlayerRole.Default])
            {
                Assert.AreEqual(1.0f, v);
            }
        }

        // ── Strict monotonicity (gradation not collapsed — §3.2 "gradation carrier") ─

        [Test]
        public void MentalityTables_AreStrictlyMonotonic()
        {
            float[] risk = TacticalInstructionsConstants.MentalityRiskMult;
            float[] line = TacticalInstructionsConstants.MentalityLineBias;
            for (int i = 1; i < TacticalInstructionsConstants.MENTALITY_LEVELS; i++)
            {
                Assert.Greater(risk[i], risk[i - 1], "riskMultiplier must rise with mentality.");
                Assert.Greater(line[i], line[i - 1], "defensiveLineBias must rise with mentality.");
            }
        }

        [Test]
        public void WidthAndEngagementScalars_AreStrictlyMonotonic()
        {
            AssertStrictlyIncreasing(TacticalInstructionsConstants.WidthScalar);
            AssertStrictlyIncreasing(TacticalInstructionsConstants.DefWidthScalar);
            AssertStrictlyIncreasing(TacticalInstructionsConstants.LineOfEngagementScalar);
            AssertStrictlyIncreasing(TacticalInstructionsConstants.TempoBreadthScalar);
            AssertStrictlyIncreasing(TacticalInstructionsConstants.InstrBiasMult);
        }

        // ── RoleWeightModifiers ∈ [0.5, 2.0] with the §3.3 directional shapes ──

        [Test]
        public void RoleWeightModifiers_WithinBoundsAndShaped()
        {
            float[][] rows = TacticalInstructionsConstants.RoleWeightModifiers;
            foreach (float[] row in rows)
            {
                Assert.AreEqual(7, row.Length, "each role row has one cell per ActionType ordinal.");
                foreach (float v in row)
                {
                    Assert.GreaterOrEqual(v, 0.5f);
                    Assert.LessOrEqual(v, 2.0f);
                }
            }

            // Column ordinals: 0 PASS, 1 SHOOT, 2 DRIBBLE, 3 HOLD, 5 PRESS, 6 INTERCEPT.
            float[] poacher = rows[(int)PlayerRole.Poacher];
            Assert.Greater(poacher[1], 1.0f, "Poacher raises SHOOT.");
            Assert.Less(poacher[3], 1.0f, "Poacher lowers HOLD.");

            float[] dlp = rows[(int)PlayerRole.DeepLyingPlaymaker];
            Assert.Greater(dlp[0], 1.0f, "Deep-Lying Playmaker raises PASS.");
            Assert.Less(dlp[2], 1.0f, "Deep-Lying Playmaker lowers DRIBBLE.");

            float[] bwm = rows[(int)PlayerRole.BallWinningMid];
            Assert.Greater(bwm[5], 1.0f, "Ball-Winning Mid raises PRESS.");
            Assert.Greater(bwm[6], 1.0f, "Ball-Winning Mid raises INTERCEPT.");
        }

        private static void AssertStrictlyIncreasing(float[] table)
        {
            for (int i = 1; i < table.Length; i++)
            {
                Assert.Greater(table[i], table[i - 1]);
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-06-30 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
