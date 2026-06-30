// File:     src/match-engine/tests/MatchEngineTacticTests.cs
// Created:  2026-06-28
// Modified: 2026-06-29
// Author:   —
// Spec:     Tactical Instructions #21 §3.1/§3.2/§3.4/§4.6 (FR-TI-017/027/031); Match Engine design note §5; Code Standards #20
// Purpose:  #21 T2 runtime-activation tests — SetTeamTactic routes a live TeamTactic into each
//           agent's DecisionTree input (Mentality/Pressing/Passing) and into the Pressing AI (#13)
//           snapshot (LineOfEngagement), the Defensive AI (#14) snapshot (OffsideTrap), the
//           Attacking AI (#15) snapshot (FocusPlay), and the Positioning AI (#12) modifiers
//           (Width / DefensiveWidth) at the AI-stride boundary, the Balanced default is
//           behaviour-neutral (digest unchanged), and activation stays deterministic.

using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.DecisionTree;
using TacticalDirector.DeterministicSim;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Phase D #21 T2 runtime-activation tests for <see cref="MatchEngine.SetTeamTactic"/>.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineTacticTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

        private static int Stride => DeterministicSimConstants.AI_PHASE_STRIDE;
        private static int HomeAgent => 0;
        private static int AwayAgent => MatchEngineConstants.PLAYERS_PER_TEAM;

        // A bold, fully non-Balanced tactic (every translated dimension differs from Balanced).
        private static TeamTactic Attacking() => new TeamTactic(
            Mentality.VeryAttacking, TacticFormation.F442, Tempo.Standard, TacticWidth.Standard,
            TacticPassing.Direct, TacticPressing.High, LineOfEngagement.Standard, 0.5f,
            TacticDefWidth.Standard, TransitionPlan.HoldShape, TransitionPlan.Regroup, false,
            TacticTriggerMask.None, FocusPlay.Mixed, GkDistributionPolicy.SlowDown, 0);

        private static TeamTactic Defending() => new TeamTactic(
            Mentality.VeryDefensive, TacticFormation.F442, Tempo.Standard, TacticWidth.Standard,
            TacticPassing.Short, TacticPressing.Low, LineOfEngagement.Standard, 0.5f,
            TacticDefWidth.Standard, TransitionPlan.HoldShape, TransitionPlan.Regroup, false,
            TacticTriggerMask.None, FocusPlay.Mixed, GkDistributionPolicy.SlowDown, 0);

        // Balanced in every dimension except the line of engagement (the #13 routing axis under test).
        private static TeamTactic WithLine(LineOfEngagement line) => new TeamTactic(
            Mentality.Balanced, TacticFormation.F442, Tempo.Standard, TacticWidth.Standard,
            TacticPassing.Mixed, TacticPressing.Medium, line, 0.5f,
            TacticDefWidth.Standard, TransitionPlan.HoldShape, TransitionPlan.Regroup, false,
            TacticTriggerMask.None, FocusPlay.Mixed, GkDistributionPolicy.SlowDown, 0);

        // Balanced in every dimension except OffsideTrap (the #14 routing axis under test).
        private static TeamTactic WithOffsideTrap(bool trap) => new TeamTactic(
            Mentality.Balanced, TacticFormation.F442, Tempo.Standard, TacticWidth.Standard,
            TacticPassing.Mixed, TacticPressing.Medium, LineOfEngagement.Standard, 0.5f,
            TacticDefWidth.Standard, TransitionPlan.HoldShape, TransitionPlan.Regroup, trap,
            TacticTriggerMask.None, FocusPlay.Mixed, GkDistributionPolicy.SlowDown, 0);

        // Balanced in every dimension except FocusPlay (the #15 routing axis under test).
        private static TeamTactic WithFocus(FocusPlay focus) => new TeamTactic(
            Mentality.Balanced, TacticFormation.F442, Tempo.Standard, TacticWidth.Standard,
            TacticPassing.Mixed, TacticPressing.Medium, LineOfEngagement.Standard, 0.5f,
            TacticDefWidth.Standard, TransitionPlan.HoldShape, TransitionPlan.Regroup, false,
            TacticTriggerMask.None, focus, GkDistributionPolicy.SlowDown, 0);

        // Balanced in every dimension except Width / DefensiveWidth (the #12 routing axes under test).
        private static TeamTactic WithWidth(TacticWidth width, TacticDefWidth defWidth) => new TeamTactic(
            Mentality.Balanced, TacticFormation.F442, Tempo.Standard, width,
            TacticPassing.Mixed, TacticPressing.Medium, LineOfEngagement.Standard, 0.5f,
            defWidth, TransitionPlan.HoldShape, TransitionPlan.Regroup, false,
            TacticTriggerMask.None, FocusPlay.Mixed, GkDistributionPolicy.SlowDown, 0);

        private static void TickToFirstStride(MatchEngine engine)
        {
            for (int i = 0; i < Stride; i++)
            {
                engine.RunTick();
            }
            Assert.IsTrue(engine.DidAiPhaseRunLastTick, "Test must tick to an AI-stride boundary.");
        }

        // ── Default tactic is the Balanced identity, routed to the DecisionTree input ──

        [Test]
        public void DefaultTactic_RoutesBalancedIdentity()
        {
            var engine = new MatchEngine(MatchSeed);
            TickToFirstStride(engine);

            Assert.AreEqual(Mentality.Balanced, engine.TestOnly_Mentality(HomeAgent));
            Assert.AreEqual(PressingMode.MEDIUM, engine.TestOnly_Pressing(HomeAgent));
            Assert.AreEqual(PassingStyle.MIXED,  engine.TestOnly_Passing(HomeAgent));
        }

        // ── SetTeamTactic reaches the DecisionTree input, per team, translated correctly ──

        [Test]
        public void SetTeamTactic_RoutesPerTeam_Translated()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.SetTeamTactic(0, Attacking());
            engine.SetTeamTactic(1, Defending());
            TickToFirstStride(engine);

            // Home → attacking; Pressing.High → HIGH, Passing.Direct → DIRECT (rank map, not raw cast).
            Assert.AreEqual(Mentality.VeryAttacking, engine.TestOnly_Mentality(HomeAgent));
            Assert.AreEqual(PressingMode.HIGH,   engine.TestOnly_Pressing(HomeAgent));
            Assert.AreEqual(PassingStyle.DIRECT, engine.TestOnly_Passing(HomeAgent));

            // Away → defending; Pressing.Low → LOW, Passing.Short → SHORT.
            Assert.AreEqual(Mentality.VeryDefensive, engine.TestOnly_Mentality(AwayAgent));
            Assert.AreEqual(PressingMode.LOW,   engine.TestOnly_Pressing(AwayAgent));
            Assert.AreEqual(PassingStyle.SHORT, engine.TestOnly_Passing(AwayAgent));
        }

        // ── #13 Phase-D writer: LineOfEngagement routes per team into the Pressing AI snapshot ──

        [Test]
        public void SetTeamTactic_RoutesLineOfEngagement_PerTeam()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.SetTeamTactic(0, WithLine(LineOfEngagement.VeryHigh));
            engine.SetTeamTactic(1, WithLine(LineOfEngagement.VeryLow));
            TickToFirstStride(engine);

            Assert.AreEqual(LineOfEngagement.VeryHigh, engine.TestOnly_PressLineOfEngagement(0));
            Assert.AreEqual(LineOfEngagement.VeryLow,  engine.TestOnly_PressLineOfEngagement(1));
        }

        [Test]
        public void DefaultTactic_RoutesStandardLineOfEngagement()
        {
            var engine = new MatchEngine(MatchSeed);
            TickToFirstStride(engine);

            // Balanced ⇒ Standard ⇒ the #13 trigger-radius scalar is ×1.0 (behaviour-neutral).
            Assert.AreEqual(LineOfEngagement.Standard, engine.TestOnly_PressLineOfEngagement(0));
            Assert.AreEqual(LineOfEngagement.Standard, engine.TestOnly_PressLineOfEngagement(1));
        }

        // ── #14 Phase-D writer: OffsideTrap routes per team into the Defensive AI snapshot ──

        [Test]
        public void SetTeamTactic_RoutesOffsideTrap_PerTeam()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.SetTeamTactic(0, WithOffsideTrap(true));
            engine.SetTeamTactic(1, WithOffsideTrap(false));
            TickToFirstStride(engine);

            Assert.IsTrue(engine.TestOnly_OffsideTrapRequested(0));
            Assert.IsFalse(engine.TestOnly_OffsideTrapRequested(1));
        }

        [Test]
        public void DefaultTactic_RoutesFalseOffsideTrap()
        {
            var engine = new MatchEngine(MatchSeed);
            TickToFirstStride(engine);

            // Balanced ⇒ OffsideTrap false ⇒ the #14 routing identity (KD-9 request-not-guarantee).
            Assert.IsFalse(engine.TestOnly_OffsideTrapRequested(0));
            Assert.IsFalse(engine.TestOnly_OffsideTrapRequested(1));
        }

        // ── #15 Phase-D writer: FocusPlay routes per team into the Attacking AI snapshot ──

        [Test]
        public void SetTeamTactic_RoutesFocusPlay_PerTeam()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.SetTeamTactic(0, WithFocus(FocusPlay.LeftFlank));
            engine.SetTeamTactic(1, WithFocus(FocusPlay.RightFlank));
            TickToFirstStride(engine);

            Assert.AreEqual(FocusPlay.LeftFlank,  engine.TestOnly_FocusPlay(0));
            Assert.AreEqual(FocusPlay.RightFlank, engine.TestOnly_FocusPlay(1));
        }

        [Test]
        public void DefaultTactic_RoutesMixedFocusPlay()
        {
            var engine = new MatchEngine(MatchSeed);
            TickToFirstStride(engine);

            // Balanced ⇒ FocusPlay.Mixed ⇒ no lateral preference (the #15 routing identity).
            Assert.AreEqual(FocusPlay.Mixed, engine.TestOnly_FocusPlay(0));
            Assert.AreEqual(FocusPlay.Mixed, engine.TestOnly_FocusPlay(1));
        }

        // ── #12 Phase-D writer: Width / DefensiveWidth route per team into the Positioning modifiers ──

        [Test]
        public void SetTeamTactic_RoutesWidth_PerTeam()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.SetTeamTactic(0, WithWidth(TacticWidth.VeryWide,   TacticDefWidth.Wide));
            engine.SetTeamTactic(1, WithWidth(TacticWidth.VeryNarrow, TacticDefWidth.Narrow));
            TickToFirstStride(engine);

            Assert.AreEqual(TacticWidth.VeryWide,      engine.TestOnly_PositioningWidth(0));
            Assert.AreEqual(TacticDefWidth.Wide,       engine.TestOnly_PositioningDefWidth(0));
            Assert.AreEqual(TacticWidth.VeryNarrow,    engine.TestOnly_PositioningWidth(1));
            Assert.AreEqual(TacticDefWidth.Narrow,     engine.TestOnly_PositioningDefWidth(1));
        }

        [Test]
        public void DefaultTactic_RoutesStandardWidth()
        {
            var engine = new MatchEngine(MatchSeed);
            TickToFirstStride(engine);

            // Balanced ⇒ Standard / Standard ⇒ the #12 lateral-compactness scalar is 1.00 (behaviour-neutral).
            Assert.AreEqual(TacticWidth.Standard,    engine.TestOnly_PositioningWidth(0));
            Assert.AreEqual(TacticDefWidth.Standard, engine.TestOnly_PositioningDefWidth(0));
            Assert.AreEqual(TacticWidth.Standard,    engine.TestOnly_PositioningWidth(1));
            Assert.AreEqual(TacticDefWidth.Standard, engine.TestOnly_PositioningDefWidth(1));
        }

        [Test]
        public void SetTeamTactic_InvalidTeam_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => engine.SetTeamTactic(2, Attacking()));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => engine.SetTeamTactic(-1, Attacking()));
        }

        // ── #21 §3.3 per-agent PlayerTactic config surface (SetPlayerTactic) ───

        // Balanced in every dimension except Mentality + DefensiveLine (the §3.4 depth recompute axes).
        private static TeamTactic WithMentalityAndLine(Mentality mentality, float defensiveLine) => new TeamTactic(
            mentality, TacticFormation.F442, Tempo.Standard, TacticWidth.Standard,
            TacticPassing.Mixed, TacticPressing.Medium, LineOfEngagement.Standard, defensiveLine,
            TacticDefWidth.Standard, TransitionPlan.HoldShape, TransitionPlan.Regroup, false,
            TacticTriggerMask.None, FocusPlay.Mixed, GkDistributionPolicy.SlowDown, 0);

        private static PlayerTactic BoldPlayer() =>
            new PlayerTactic(PlayerRole.Poacher, Duty.Attack, PlayerInstructions.Default);

        [Test]
        public void DefaultPlayerTactic_RoutesIdentity()
        {
            var engine = new MatchEngine(MatchSeed);
            TickToFirstStride(engine);

            PlayerTactic t = engine.TestOnly_PlayerTactic(HomeAgent);
            Assert.AreEqual(PlayerRole.Default, t.Role);
            Assert.AreEqual(Duty.Support, t.Duty);
        }

        [Test]
        public void SetPlayerTactic_RoutesPerAgent()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.SetPlayerTactic(HomeAgent, BoldPlayer());
            TickToFirstStride(engine);

            PlayerTactic home = engine.TestOnly_PlayerTactic(HomeAgent);
            Assert.AreEqual(PlayerRole.Poacher, home.Role);
            Assert.AreEqual(Duty.Attack, home.Duty);

            // A different agent is untouched (still the identity).
            Assert.AreEqual(PlayerRole.Default, engine.TestOnly_PlayerTactic(HomeAgent + 1).Role);
        }

        [Test]
        public void SetPlayerTactic_TakesEffectOnlyAtStride()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.SetPlayerTactic(HomeAgent, BoldPlayer());

            engine.RunTick(); // tick 1 — not a stride tick
            Assert.IsFalse(engine.DidAiPhaseRunLastTick);
            Assert.AreEqual(PlayerRole.Default, engine.TestOnly_PlayerTactic(HomeAgent).Role,
                "A pending per-agent tactic must not apply before the stride boundary (FR-TI-027).");

            for (ulong t = engine.CurrentTick + 1; t <= (ulong)Stride; t++)
            {
                engine.RunTick();
            }
            Assert.AreEqual(PlayerRole.Poacher, engine.TestOnly_PlayerTactic(HomeAgent).Role);
        }

        [Test]
        public void SetPlayerTactic_InvalidAgent_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => engine.SetPlayerTactic(MatchEngineConstants.SQUAD_SIZE, BoldPlayer()));
            Assert.Throws<System.ArgumentOutOfRangeException>(
                () => engine.SetPlayerTactic(-1, BoldPlayer()));
        }

        [Test]
        public void ExplicitIdentityPlayerTactic_IsBehaviourNeutral_DigestUnchanged()
        {
            const int ticks = 2 * 6 * 2;
            PlayerTactic identity = PlayerTactic.Default(PlayerRole.Default);

            List<byte[]> defaultChain = RunChain(ticks, configure: null);
            List<byte[]> identityChain = RunChain(ticks, configure: e =>
            {
                for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
                {
                    e.SetPlayerTactic(i, identity);
                }
            });

            for (int i = 0; i < ticks; i++)
            {
                CollectionAssert.AreEqual(defaultChain[i], identityChain[i],
                    $"Explicit identity PlayerTactic perturbed the digest at tick {i + 1} — not behaviour-neutral.");
            }
        }

        // ── #21 §3.4 DefensiveLine depth recompute: Clamp01(DefensiveLine + MentalityLineBias) ──

        [Test]
        public void DefaultTactic_DefensiveLineDepthIsHalf()
        {
            var engine = new MatchEngine(MatchSeed);
            TickToFirstStride(engine);

            // Balanced ⇒ Clamp01(0.5 + 0.0) = 0.5 = the prior STAGE0 default (behaviour-neutral).
            Assert.AreEqual(0.5f, engine.TestOnly_DefensiveLineDepth(HomeAgent));
        }

        [Test]
        public void DefensiveLineDepth_RecomputedFromDialPlusMentalityBias()
        {
            // Cautious (line bias −0.05) + dial 0.50 ⇒ Clamp01(0.45) = 0.45.
            var deeper = new MatchEngine(MatchSeed);
            deeper.SetTeamTactic(0, WithMentalityAndLine(Mentality.Cautious, 0.50f));
            TickToFirstStride(deeper);
            Assert.AreEqual(0.45f, deeper.TestOnly_DefensiveLineDepth(HomeAgent), 1e-5f);

            // VeryAttacking (line bias +0.20) + dial 0.90 ⇒ Clamp01(1.10) = 1.0 (the Clamp01 ceiling).
            var higher = new MatchEngine(MatchSeed);
            higher.SetTeamTactic(0, WithMentalityAndLine(Mentality.VeryAttacking, 0.90f));
            TickToFirstStride(higher);
            Assert.AreEqual(1.0f, higher.TestOnly_DefensiveLineDepth(HomeAgent), 1e-5f);
        }

        // ── FR-TI-027: a pending change does not take effect before the stride boundary ──

        [Test]
        public void SetTeamTactic_TakesEffectOnlyAtStride()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.SetTeamTactic(0, Attacking());

            // Tick 1 is not a stride tick (first stride is at tick == Stride), so the AI phase has not
            // run and the routed Mentality is still the boot Balanced seed.
            engine.RunTick();
            Assert.IsFalse(engine.DidAiPhaseRunLastTick);
            Assert.AreEqual(Mentality.Balanced, engine.TestOnly_Mentality(HomeAgent),
                "A pending tactic must not be applied before the first AI-stride boundary (FR-TI-027).");

            for (ulong t = engine.CurrentTick + 1; t <= (ulong)Stride; t++)
            {
                engine.RunTick();
            }
            Assert.IsTrue(engine.DidAiPhaseRunLastTick);
            Assert.AreEqual(Mentality.VeryAttacking, engine.TestOnly_Mentality(HomeAgent),
                "The pending tactic must be committed at the stride boundary.");
        }

        // ── FR-TI-031: explicitly setting Balanced is byte-identical to the untouched default ──

        [Test]
        public void ExplicitBalanced_IsBehaviourNeutral_DigestUnchanged()
        {
            const int ticks = 2 * 6 * 2; // a couple of strides past the first AI tick

            List<byte[]> defaultChain = RunChain(ticks, configure: null);
            List<byte[]> balancedChain = RunChain(ticks, configure: e =>
            {
                e.SetTeamTactic(0, TeamTactic.Balanced);
                e.SetTeamTactic(1, TeamTactic.Balanced);
            });

            for (int i = 0; i < ticks; i++)
            {
                CollectionAssert.AreEqual(defaultChain[i], balancedChain[i],
                    $"Explicit Balanced tactic perturbed the digest at tick {i + 1} — not behaviour-neutral.");
            }
        }

        // ── Activation stays deterministic: same non-Balanced tactics ⇒ identical digest chains ──

        [Test]
        public void NonBalancedTactic_IsDeterministic()
        {
            const int ticks = 2 * 6 * 2;

            List<byte[]> a = RunChain(ticks, configure: e =>
            {
                e.SetTeamTactic(0, Attacking());
                e.SetTeamTactic(1, Defending());
            });
            List<byte[]> b = RunChain(ticks, configure: e =>
            {
                e.SetTeamTactic(0, Attacking());
                e.SetTeamTactic(1, Defending());
            });

            for (int i = 0; i < ticks; i++)
            {
                CollectionAssert.AreEqual(a[i], b[i],
                    $"Two same-seed, same-tactic runs diverged at tick {i + 1} — activation is non-deterministic.");
            }
        }

        private static List<byte[]> RunChain(int ticks, System.Action<MatchEngine> configure)
        {
            var engine = new MatchEngine(MatchSeed);
            configure?.Invoke(engine);
            var chain = new List<byte[]>(ticks);
            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
                chain.Add(engine.CurrentSnapshotDigest);
            }
            return chain;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                   |
// | 1.0     | 2026-06-28 | —      | #21 T2 runtime-activation tests (SetTeamTactic routing). |
// | 1.1     | 2026-06-29 | —      | #13 Phase-D writer: LineOfEngagement per-team routing + Standard-default cases. |
// | 1.2     | 2026-06-29 | —      | #14/#15 Phase-D writers: OffsideTrap + FocusPlay per-team routing + identity defaults. |
// | 1.3     | 2026-06-29 | —      | #12 Phase-D writer: Width / DefensiveWidth per-team routing + Standard-default cases. |
// | 1.4     | 2026-06-30 | —      | #21 §3.3 per-agent PlayerTactic config (SetPlayerTactic routing / stride-gating / |
// |         |            |        | invalid-agent / identity behaviour-neutrality) + §3.4 DefensiveLine depth recompute. |
#endregion
