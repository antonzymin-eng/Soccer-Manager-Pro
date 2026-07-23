// File:     src/decision-tree/Tests/TacticTranslationTests.cs
// Created:  2026-06-28
// Modified: 2026-06-29 (#21 §3.3 PlayerTacticActionMultiplier coverage)
// Author:   —
// Spec:     Tactical Instructions #21 §3.1, §3.2, §3.3, §4.7; Code Standards #20
// Purpose:  Locks the #21 → #8 T2 consumer seam: enum-translation validity + F5 clamp,
//           Mentality identity rows (FR-TI-031), the §3.2 risk/line gradation shape, and the
//           §3.3 per-agent role/duty/instruction × tempo utility product (identity ⇒ ×1.0).

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

        // ── §3.3 per-agent utility product: identity is exact ×1.0, shapes are directional ──

        private static PlayerTactic Identity => PlayerTactic.Default(PlayerRole.Default);

        [Test]
        public void PlayerTacticActionMultiplier_Identity_IsExactlyOne()
        {
            // PlayerRole.Default / Duty.Support / every InstrBias.Default at Tempo.Standard ⇒ ×1.0
            // for every action (FR-TI-031) — the seam is a no-op for a default-tactic match.
            foreach (ActionType action in System.Enum.GetValues(typeof(ActionType)))
            {
                // ERR-008-013: SAVE is not a tactic-modulated action — UtilityScorer.ComputeUtility
                // exempts it from this multiplier, whose #21 RoleWeightModifiers / TempoActionBias tables
                // are 7-wide (ordinals 0–6). It is never called with SAVE in production.
                if (action == ActionType.SAVE) continue;
                Assert.AreEqual(1.0f,
                    TacticTranslation.PlayerTacticActionMultiplier(Identity, Tempo.Standard, action),
                    $"Identity PlayerTactic must resolve to ×1.0 for {action}.");
            }
        }

        // The Stage 0 default context must resolve to the identity product so the whole seam is a no-op.
        [Test]
        public void Stage0Default_ResolvesToIdentityProduct()
        {
            TacticalContext ctx = TacticalContext.Stage0Default(UnityEngine.Vector2.zero);
            Assert.AreEqual(1.0f,
                TacticTranslation.PlayerTacticActionMultiplier(ctx.PlayerTactic, ctx.Tempo, ActionType.SHOOT));
        }

        [Test]
        public void PlayerTacticActionMultiplier_RoleRaisesNamedAction()
        {
            // Poacher's A.4 row raises SHOOT above the Default identity row (the reviewable shape).
            Assert.Greater(
                TacticTranslation.PlayerTacticActionMultiplier(
                    PlayerTactic.Default(PlayerRole.Poacher), Tempo.Standard, ActionType.SHOOT),
                1.0f);
        }

        [Test]
        public void PlayerTacticActionMultiplier_TempoShiftsPassVsHold()
        {
            // Faster tempo raises PASS and lowers HOLD relative to the Standard identity row.
            Assert.Greater(
                TacticTranslation.PlayerTacticActionMultiplier(Identity, Tempo.VeryFast, ActionType.PASS), 1.0f);
            Assert.Less(
                TacticTranslation.PlayerTacticActionMultiplier(Identity, Tempo.VeryFast, ActionType.HOLD), 1.0f);
        }

        [Test]
        public void PlayerTacticActionMultiplier_InstructionBiasModulatesNamedTerm()
        {
            // A "shoot more" individual instruction lifts SHOOT; everything else stays identity.
            var instr = new PlayerInstructions(
                InstrBias.Default, InstrBias.More, InstrBias.Default, InstrBias.Default,
                InstrBias.Default, InstrBias.Default, false,
                TacticalInstructionsConstants.MARK_TARGET_NONE, SetPieceDutyFlags.None);
            var tactic = new PlayerTactic(PlayerRole.Default, Duty.Support, instr);

            Assert.Greater(
                TacticTranslation.PlayerTacticActionMultiplier(tactic, Tempo.Standard, ActionType.SHOOT), 1.0f);
            Assert.AreEqual(1.0f,
                TacticTranslation.PlayerTacticActionMultiplier(tactic, Tempo.Standard, ActionType.PASS));
        }

        [Test]
        public void PlayerTacticActionMultiplier_AttackDutyRaisesOnBallActions()
        {
            // Attack duty's positive aggression bias lifts the on-ball attacking actions; Defend lowers them.
            var attack  = new PlayerTactic(PlayerRole.Default, Duty.Attack,  PlayerInstructions.Default);
            var defend  = new PlayerTactic(PlayerRole.Default, Duty.Defend,  PlayerInstructions.Default);
            Assert.Greater(
                TacticTranslation.PlayerTacticActionMultiplier(attack, Tempo.Standard, ActionType.DRIBBLE), 1.0f);
            Assert.Less(
                TacticTranslation.PlayerTacticActionMultiplier(defend, Tempo.Standard, ActionType.DRIBBLE), 1.0f);
        }

        [Test]
        public void PlayerTacticActionMultiplier_WidenedRoleAndTempo_ClampToTableBounds()
        {
            Assert.AreEqual(
                TacticTranslation.PlayerTacticActionMultiplier(
                    PlayerTactic.Default((PlayerRole)99), (Tempo)99, ActionType.PASS),
                TacticTranslation.PlayerTacticActionMultiplier(
                    PlayerTactic.Default(PlayerRole.TargetMan), Tempo.VeryFast, ActionType.PASS));
        }
    }
}
