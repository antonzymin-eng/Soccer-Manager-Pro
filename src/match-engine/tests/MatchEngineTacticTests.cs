// File:     src/match-engine/tests/MatchEngineTacticTests.cs
// Created:  2026-06-28
// Modified: 2026-06-29
// Author:   —
// Spec:     Tactical Instructions #21 §3.1/§3.2/§3.4/§4.6 (FR-TI-017/027/031); Match Engine design note §5; Code Standards #20
// Purpose:  #21 T2 runtime-activation tests — SetTeamTactic routes a live TeamTactic into each
//           agent's DecisionTree input (Mentality/Pressing/Passing) and into the Pressing AI (#13)
//           snapshot (LineOfEngagement) at the AI-stride boundary, the Balanced default is
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

        [Test]
        public void SetTeamTactic_InvalidTeam_Throws()
        {
            var engine = new MatchEngine(MatchSeed);
            Assert.Throws<System.ArgumentOutOfRangeException>(() => engine.SetTeamTactic(2, Attacking()));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => engine.SetTeamTactic(-1, Attacking()));
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
#endregion
