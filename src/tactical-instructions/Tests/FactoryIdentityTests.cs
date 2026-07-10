// File:     src/tactical-instructions/Tests/FactoryIdentityTests.cs
// Created:  2026-06-21
// Modified: 2026-07-07
// Author:   —
// Spec:     Tactical Instructions #21 §2.2, §3.2–§3.4, Appendix A, FR-TI-031, Code Standards #20
// Purpose:  Locks the identity factories (TeamTactic.Balanced / PlayerTactic.Default /
//           PlayerInstructions.Default) and the catalogue identity rows so a default
//           tactic is behaviour-neutral (FR-TI-031 / KD-10). Catalogue shape checks:
//           T-TI-U-029 ([0.5,2.0] RoleWeightModifiers), identity-row exactness.

using NUnit.Framework;

using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.TacticalInstructions.Tests
{
    /// <summary>Identity-factory and catalogue-shape locks for FR-TI-031.</summary>
    public class FactoryIdentityTests
    {
        // ── TeamTactic.Balanced reproduces Stage0Default (§2.2.1) ─────────────

        [Test]
        public void TeamTactic_Balanced_IsTheIdentityTactic()
        {
            TeamTactic t = TeamTactic.Balanced;

            Assert.AreEqual(Mentality.Balanced, t.Mentality);
            Assert.AreEqual(TacticFormation.F442, t.Formation);
            Assert.AreEqual(Tempo.Standard, t.Tempo);
            Assert.AreEqual(TacticWidth.Standard, t.Width);
            Assert.AreEqual(TacticPassing.Mixed, t.Passing);
            Assert.AreEqual(TacticPressing.Medium, t.Pressing);
            Assert.AreEqual(LineOfEngagement.Standard, t.LineOfEngagement);
            Assert.AreEqual(0.5f, t.DefensiveLine);
            Assert.AreEqual(TacticDefWidth.Standard, t.DefensiveWidth);
            Assert.AreEqual(TransitionPlan.HoldShape, t.TransitionWon);
            Assert.AreEqual(TransitionPlan.Regroup, t.TransitionLost);
            Assert.IsFalse(t.OffsideTrap);
            Assert.AreEqual(TacticTriggerMask.None, t.TriggerPressMask);
            Assert.AreEqual(FocusPlay.Mixed, t.FocusPlay);
            Assert.AreEqual(GkDistributionPolicy.SlowDown, t.GkDistribution);
            Assert.AreEqual(0, t.TimeWasting);
            Assert.AreEqual(MarkingOrientation.Balanced, t.MarkingOrientation);
            Assert.AreEqual(DismarkIntensity.Off, t.DismarkIntensity);
            Assert.AreEqual(BuildUpStructure.None, t.BuildUpStructure);
            Assert.AreEqual(RotationFreedom.Off, t.RotationFreedom);
        }

        // ── PlayerInstructions.Default (§2.2.2) ───────────────────────────────

        [Test]
        public void PlayerInstructions_Default_IsAllIdentity()
        {
            PlayerInstructions p = PlayerInstructions.Default;

            Assert.AreEqual(InstrBias.Default, p.RiskyPasses);
            Assert.AreEqual(InstrBias.Default, p.ShootTendency);
            Assert.AreEqual(InstrBias.Default, p.DribbleTendency);
            Assert.AreEqual(InstrBias.Default, p.CrossTendency);
            Assert.AreEqual(InstrBias.Default, p.PositioningFreedom);
            Assert.AreEqual(InstrBias.Default, p.CloseDown);
            Assert.IsFalse(p.TightMarking);
            Assert.AreEqual(TacticalInstructionsConstants.MARK_TARGET_NONE, p.MarkTargetEntityId);
            Assert.AreEqual(SetPieceDutyFlags.None, p.SetPieceRoles);
        }

        // ── PlayerTactic.Default(role) (§2.2.3) ───────────────────────────────

        [Test]
        public void PlayerTactic_Default_KeepsRoleAndUsesSupportPlusDefaultInstructions()
        {
            PlayerTactic d = PlayerTactic.Default(PlayerRole.Poacher);

            Assert.AreEqual(PlayerRole.Poacher, d.Role);
            Assert.AreEqual(Duty.Support, d.Duty);
            Assert.AreEqual(InstrBias.Default, d.Instructions.RiskyPasses);
            Assert.AreEqual(TacticalInstructionsConstants.MARK_TARGET_NONE, d.Instructions.MarkTargetEntityId);
        }

        [Test]
        public void PlayerTactic_Default_WithDefaultRole_IsTheFullBaseline()
        {
            PlayerTactic d = PlayerTactic.Default(PlayerRole.Default);

            Assert.AreEqual(PlayerRole.Default, d.Role);
            Assert.AreEqual(Duty.Support, d.Duty);
        }

        // ── Default-struct values are NOT the identity (AR-1 L-1 hazard lock) ─

        [Test]
        public void DefaultPlayerInstructions_IsNotTheIdentity_EncodesManMarkOnAgentZero()
        {
            // default() skips the factory: MarkTargetEntityId is 0 (a valid entity id ⇒ a man-mark
            // request on agent 0), not the −1 "none" sentinel. Consumers must use Default.
            PlayerInstructions d = default;

            Assert.AreEqual(0, d.MarkTargetEntityId);
            Assert.AreNotEqual(PlayerInstructions.Default.MarkTargetEntityId, d.MarkTargetEntityId);
        }

        [Test]
        public void DefaultTeamTactic_IsNotTheBalancedIdentity()
        {
            TeamTactic d = default;

            // default ordinals land on VeryDefensive / Short, not Balanced / Mixed.
            Assert.AreEqual(Mentality.VeryDefensive, d.Mentality);
            Assert.AreEqual(TacticPassing.Short, d.Passing);
            Assert.AreNotEqual(TeamTactic.Balanced.Mentality, d.Mentality);
        }

        // ── Catalogue identity rows are exact (FR-TI-031) ─────────────────────

        [Test]
        public void MentalityBalanced_RiskAndLineRowsAreIdentity()
        {
            Assert.AreEqual(1.0f, TacticalInstructionsConstants.MentalityRiskMult[(int)Mentality.Balanced]);
            Assert.AreEqual(0.0f, TacticalInstructionsConstants.MentalityLineBias[(int)Mentality.Balanced]);
            Assert.AreEqual(1.0f, TacticalInstructionsConstants.RiskMultBalanced);
            Assert.AreEqual(0.0f, TacticalInstructionsConstants.LineBiasBalanced);
        }

        [Test]
        public void InstrBiasDefault_IsMultiplicativeIdentity()
        {
            Assert.AreEqual(1.0f, TacticalInstructionsConstants.InstrBiasMult[(int)InstrBias.Default]);
        }

        [Test]
        public void DutySupport_IsOffsetAndAggressionIdentity()
        {
            Assert.AreEqual(0.0f, TacticalInstructionsConstants.DutyForeOffsetM[(int)Duty.Support]);
            Assert.AreEqual(0.0f, TacticalInstructionsConstants.DutyAggressionBias[(int)Duty.Support]);
        }

        [Test]
        public void ScalarIdentityRows_AreExactlyOne()
        {
            Assert.AreEqual(1.0f, TacticalInstructionsConstants.WidthScalar[(int)TacticWidth.Standard]);
            Assert.AreEqual(1.0f, TacticalInstructionsConstants.DefWidthScalar[(int)TacticDefWidth.Standard]);
            Assert.AreEqual(1.0f, TacticalInstructionsConstants.LineOfEngagementScalar[(int)LineOfEngagement.Standard]);
            Assert.AreEqual(1.0f, TacticalInstructionsConstants.TempoBreadthScalar[(int)Tempo.Standard]);
        }

        [Test]
        public void TempoStandardRow_IsExactIdentity()
        {
            float[] row = TacticalInstructionsConstants.TempoActionBias[(int)Tempo.Standard];
            foreach (float cell in row)
            {
                Assert.AreEqual(1.0f, cell);
            }
        }

        [Test]
        public void RoleWeightModifiers_DefaultRow_IsExactIdentity()
        {
            float[] row = TacticalInstructionsConstants.RoleWeightModifiers[(int)PlayerRole.Default];
            foreach (float cell in row)
            {
                Assert.AreEqual(1.0f, cell);
            }
        }

        // ── Catalogue shape / range invariants ────────────────────────────────

        [Test]
        public void RoleWeightModifiers_AllCellsWithinHalfToTwo()
        {
            // T-TI-U-029: every cell ∈ [0.5, 2.0].
            foreach (float[] row in TacticalInstructionsConstants.RoleWeightModifiers)
            {
                Assert.AreEqual(7, row.Length); // one column per #8 ActionType
                foreach (float cell in row)
                {
                    Assert.GreaterOrEqual(cell, 0.5f);
                    Assert.LessOrEqual(cell, 2.0f);
                }
            }
        }

        [Test]
        public void PerActionTables_HaveExpectedDimensions()
        {
            Assert.AreEqual(TacticalInstructionsConstants.MENTALITY_LEVELS,
                            TacticalInstructionsConstants.MentalityRiskMult.Length);
            Assert.AreEqual(TacticalInstructionsConstants.MENTALITY_LEVELS,
                            TacticalInstructionsConstants.MentalityLineBias.Length);
            Assert.AreEqual(TacticalInstructionsConstants.INSTR_BIAS_LEVELS,
                            TacticalInstructionsConstants.InstrBiasMult.Length);

            Assert.AreEqual(5, TacticalInstructionsConstants.TempoActionBias.Length);
            foreach (float[] row in TacticalInstructionsConstants.TempoActionBias)
            {
                Assert.AreEqual(7, row.Length);
            }

            Assert.AreEqual(6, TacticalInstructionsConstants.RoleWeightModifiers.Length);
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-06-21 | —      | Initial implementation (T0 #21).                                   |
// | 1.1     | 2026-06-21 | —      | AR-1 L-1: default-struct-is-not-identity locks (PlayerInstructions |
// |         |            |        | man-mark-on-0 hazard; default TeamTactic ≠ Balanced).             |
// | 1.2     | 2026-07-07 | —      | Cheap-item addition: + MarkingOrientation.Balanced identity assert.|
// | 1.3     | 2026-07-10 | —      | #23/#24/#25 T0: + DismarkIntensity.Off / BuildUpStructure.None /   |
// |         |            |        |   RotationFreedom.Off identity asserts (ERR-021-005/006/007).      |
#endregion
