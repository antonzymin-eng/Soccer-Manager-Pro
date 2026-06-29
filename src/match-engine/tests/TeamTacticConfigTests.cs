// File:     src/match-engine/tests/TeamTacticConfigTests.cs
// Created:  2026-06-29
// Modified: 2026-06-29
// Author:   —
// Spec:     Tactical Instructions #21 §3.1/§3.2 (T2 runtime activation), FR-TI-027/031; Match Engine design note §5; Code Standards #20
// Purpose:  Tests the in-code TeamTactic config source + boot applier: ForTeam mapping, the Default
//           Balanced identity, the applier routing each team's tactic through SetTeamTactic at the
//           stride boundary, and behaviour-neutrality of the Default config (digest chain unchanged).

using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.DecisionTree;
using TacticalDirector.DeterministicSim;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Tests for <see cref="TeamTacticConfig"/> and <see cref="TeamTacticConfigApplier"/> — the in-code
    /// config source + boot applier feeding <see cref="MatchEngine.SetTeamTactic"/> (#21 T2).
    /// </summary>
    [TestFixture]
    public sealed class TeamTacticConfigTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

        private static int Stride => DeterministicSimConstants.AI_PHASE_STRIDE;
        private static int HomeAgent => 0;
        private static int AwayAgent => MatchEngineConstants.PLAYERS_PER_TEAM;

        // A bold, fully non-Balanced attacking tactic (every translated dimension differs from Balanced).
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

        private static void TickToFirstStride(MatchEngine engine)
        {
            for (int i = 0; i < Stride; i++)
            {
                engine.RunTick();
            }
            Assert.IsTrue(engine.DidAiPhaseRunLastTick, "Test must tick to an AI-stride boundary.");
        }

        // ── TeamTacticConfig model ──

        [Test]
        public void Default_IsBalancedForEveryTeam()
        {
            TeamTacticConfig config = TeamTacticConfig.Default;
            for (int teamId = 0; teamId < MatchEngineConstants.TEAM_COUNT; teamId++)
            {
                Assert.AreEqual(Mentality.Balanced, config.ForTeam(teamId).Mentality);
            }
        }

        [Test]
        public void ForTeam_ReturnsPerTeamTactic()
        {
            var config = new TeamTacticConfig(Attacking(), Defending());
            Assert.AreEqual(Mentality.VeryAttacking, config.ForTeam(0).Mentality);
            Assert.AreEqual(Mentality.VeryDefensive, config.ForTeam(1).Mentality);
        }

        [Test]
        public void ForTeam_InvalidTeam_Throws()
        {
            TeamTacticConfig config = TeamTacticConfig.Default;
            Assert.Throws<System.ArgumentOutOfRangeException>(() => config.ForTeam(MatchEngineConstants.TEAM_COUNT));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => config.ForTeam(-1));
        }

        // ── Applier null-guards ──

        [Test]
        public void Apply_NullArguments_Throw()
        {
            var engine = new MatchEngine(MatchSeed);
            Assert.Throws<System.ArgumentNullException>(
                () => TeamTacticConfigApplier.Apply(null, TeamTacticConfig.Default));
            Assert.Throws<System.ArgumentNullException>(
                () => TeamTacticConfigApplier.Apply(engine, null));
        }

        // ── Applier routes each team's tactic into the DecisionTree input at the stride boundary ──

        [Test]
        public void Apply_RoutesEachTeamTactic_Translated()
        {
            var engine = new MatchEngine(MatchSeed);
            TeamTacticConfigApplier.Apply(engine, new TeamTacticConfig(Attacking(), Defending()));
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

        // ── Applying the Default config is behaviour-neutral (digest chain unchanged) ──

        [Test]
        public void ApplyDefault_IsBehaviourNeutral_DigestUnchanged()
        {
            const int ticks = 2 * 6 * 2; // a couple of strides past the first AI tick

            List<byte[]> unconfigured = RunChain(ticks, configure: null);
            List<byte[]> defaulted = RunChain(ticks, configure: e =>
                TeamTacticConfigApplier.Apply(e, TeamTacticConfig.Default));

            for (int i = 0; i < ticks; i++)
            {
                CollectionAssert.AreEqual(unconfigured[i], defaulted[i],
                    $"Applying the Default config perturbed the digest at tick {i + 1} — not behaviour-neutral.");
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
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-06-29 | —      | Initial implementation — TeamTacticConfig + applier tests (#21 T2). |
#endregion
