// File:     src/match-engine/tests/PlayerTacticConfigTests.cs
// Created:  2026-06-30
// Author:   —
// Spec:     Tactical Instructions #21 §3.3 (per-agent tactic config surface), FR-TI-027/031; Match Engine design note §5; Code Standards #20
// Purpose:  Tests the in-code PlayerTactic config source + boot applier: ForAgent mapping, the Identity
//           config, length / null guards, the applier routing each agent's tactic through SetPlayerTactic
//           at the stride boundary, and behaviour-neutrality of the Identity config (digest chain unchanged).

using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Tests for <see cref="PlayerTacticConfig"/> and <see cref="PlayerTacticConfigApplier"/> — the in-code
    /// per-agent config source + boot applier feeding <see cref="MatchEngine.SetPlayerTactic"/> (#21 §3.3).
    /// </summary>
    [TestFixture]
    public sealed class PlayerTacticConfigTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

        private static int Stride => DeterministicSimConstants.AI_PHASE_STRIDE;
        private const int HomeAgent = 0;

        private static PlayerTactic Bold() =>
            new PlayerTactic(PlayerRole.Poacher, Duty.Attack, PlayerInstructions.Default);

        private static PlayerTacticConfig OneBold(int agentId)
        {
            PlayerTactic[] all = new PlayerTactic[MatchEngineConstants.SQUAD_SIZE];
            for (int i = 0; i < all.Length; i++)
            {
                all[i] = PlayerTactic.Default(PlayerRole.Default);
            }
            all[agentId] = Bold();
            return new PlayerTacticConfig(all);
        }

        private static void TickToFirstStride(MatchEngine engine)
        {
            for (int i = 0; i < Stride; i++)
            {
                engine.RunTick();
            }
            Assert.IsTrue(engine.DidAiPhaseRunLastTick, "Test must tick to an AI-stride boundary.");
        }

        // ── PlayerTacticConfig model ──

        [Test]
        public void Identity_IsIdentityForEveryAgent()
        {
            PlayerTacticConfig config = PlayerTacticConfig.Identity;
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                Assert.AreEqual(PlayerRole.Default, config.ForAgent(i).Role);
                Assert.AreEqual(Duty.Support, config.ForAgent(i).Duty);
            }
        }

        [Test]
        public void ForAgent_ReturnsPerAgentTactic()
        {
            PlayerTacticConfig config = OneBold(HomeAgent);
            Assert.AreEqual(PlayerRole.Poacher, config.ForAgent(HomeAgent).Role);
            Assert.AreEqual(PlayerRole.Default, config.ForAgent(HomeAgent + 1).Role);
        }

        [Test]
        public void ForAgent_InvalidAgent_Throws()
        {
            PlayerTacticConfig config = PlayerTacticConfig.Identity;
            Assert.Throws<System.ArgumentOutOfRangeException>(() => config.ForAgent(MatchEngineConstants.SQUAD_SIZE));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => config.ForAgent(-1));
        }

        [Test]
        public void Ctor_WrongLength_Throws()
        {
            Assert.Throws<System.ArgumentNullException>(() => new PlayerTacticConfig(null));
            Assert.Throws<System.ArgumentException>(
                () => new PlayerTacticConfig(new PlayerTactic[MatchEngineConstants.SQUAD_SIZE - 1]));
        }

        // ── Applier null-guards ──

        [Test]
        public void Apply_NullArguments_Throw()
        {
            var engine = new MatchEngine(MatchSeed);
            Assert.Throws<System.ArgumentNullException>(
                () => PlayerTacticConfigApplier.Apply(null, PlayerTacticConfig.Identity));
            Assert.Throws<System.ArgumentNullException>(
                () => PlayerTacticConfigApplier.Apply(engine, null));
        }

        // ── Applier routes each agent's tactic into the DecisionTree input at the stride boundary ──

        [Test]
        public void Apply_RoutesEachAgentTactic()
        {
            var engine = new MatchEngine(MatchSeed);
            PlayerTacticConfigApplier.Apply(engine, OneBold(HomeAgent));
            TickToFirstStride(engine);

            Assert.AreEqual(PlayerRole.Poacher, engine.TestOnly_PlayerTactic(HomeAgent).Role);
            Assert.AreEqual(Duty.Attack,        engine.TestOnly_PlayerTactic(HomeAgent).Duty);
            Assert.AreEqual(PlayerRole.Default, engine.TestOnly_PlayerTactic(HomeAgent + 1).Role);
        }

        // ── Applying the Identity config is behaviour-neutral (digest chain unchanged) ──

        [Test]
        public void ApplyIdentity_IsBehaviourNeutral_DigestUnchanged()
        {
            const int ticks = 2 * 6 * 2;

            List<byte[]> unconfigured = RunChain(ticks, configure: null);
            List<byte[]> defaulted = RunChain(ticks, configure: e =>
                PlayerTacticConfigApplier.Apply(e, PlayerTacticConfig.Identity));

            for (int i = 0; i < ticks; i++)
            {
                CollectionAssert.AreEqual(unconfigured[i], defaulted[i],
                    $"Applying the Identity config perturbed the digest at tick {i + 1} — not behaviour-neutral.");
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
// | 1.0     | 2026-06-30 | —      | Initial implementation — PlayerTacticConfig + applier tests (#21 §3.3). |
#endregion
