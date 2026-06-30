// File:     src/match-engine/tests/PlayerTacticFileLoaderTests.cs
// Created:  2026-06-30
// Author:   —
// Spec:     Tactical Instructions #21 §3.3, FR-TI-031, FR-CS-019; Match Engine design note §5; Code Standards #20
// Purpose:  Locks the on-disk → PlayerTacticConfig parser swap: round-trips the text grammar onto
//           PlayerTactic / PlayerInstructions fields, proves an empty/comment-only file is the identity
//           (behaviour-neutral), that malformed input fails loudly, and that a parsed config feeds
//           PlayerTacticConfigApplier and routes per agent through SetPlayerTactic.

using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.TacticalInstructions;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class PlayerTacticFileLoaderTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

        private static int Stride => DeterministicSimConstants.AI_PHASE_STRIDE;

        private static void TickToFirstStride(MatchEngine engine)
        {
            for (int i = 0; i < Stride; i++)
            {
                engine.RunTick();
            }
            Assert.IsTrue(engine.DidAiPhaseRunLastTick, "Test must tick to an AI-stride boundary.");
        }

        // ── Grammar round-trips onto fields ──

        [Test]
        public void Parse_AgentSections_RoundTripOntoFields()
        {
            const string text =
                "# sample per-agent tactic file\n" +
                "[agent 1]\n" +
                "role = Poacher          # the striker\n" +
                "duty = Attack\n" +
                "shootTendency = More\n" +
                "tightMarking = true\n" +
                "markTarget = 14\n" +
                "\n" +
                "[agent 2]\n" +
                "role = DeepLyingPlaymaker\n" +
                "riskyPasses = Less\n";

            PlayerTacticConfig config = PlayerTacticFileLoader.Parse(text);

            PlayerTactic a1 = config.ForAgent(1);
            Assert.AreEqual(PlayerRole.Poacher, a1.Role);
            Assert.AreEqual(Duty.Attack, a1.Duty);
            Assert.AreEqual(InstrBias.More, a1.Instructions.ShootTendency);
            Assert.IsTrue(a1.Instructions.TightMarking);
            Assert.AreEqual(14, a1.Instructions.MarkTargetEntityId);
            // Untouched instruction inherits the identity default.
            Assert.AreEqual(InstrBias.Default, a1.Instructions.RiskyPasses);

            PlayerTactic a2 = config.ForAgent(2);
            Assert.AreEqual(PlayerRole.DeepLyingPlaymaker, a2.Role);
            Assert.AreEqual(InstrBias.Less, a2.Instructions.RiskyPasses);
            Assert.AreEqual(Duty.Support, a2.Duty); // omitted → identity

            // An agent with no section is the full identity tactic.
            PlayerTactic a0 = config.ForAgent(0);
            Assert.AreEqual(PlayerRole.Default, a0.Role);
            Assert.AreEqual(Duty.Support, a0.Duty);
            Assert.AreEqual(TacticalInstructionsConstants.MARK_TARGET_NONE, a0.Instructions.MarkTargetEntityId);
        }

        // ── Empty / null / comment-only input is the identity (behaviour-neutral) ──

        [Test]
        public void Parse_EmptyOrCommentOnly_IsIdentityForEveryAgent()
        {
            foreach (string text in new[] { null, "", "   \n# only a comment\n\n" })
            {
                PlayerTacticConfig config = PlayerTacticFileLoader.Parse(text);
                for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
                {
                    PlayerTactic t = config.ForAgent(i);
                    Assert.AreEqual(PlayerRole.Default, t.Role, $"agent {i}");
                    Assert.AreEqual(Duty.Support, t.Duty, $"agent {i}");
                    Assert.AreEqual(TacticalInstructionsConstants.MARK_TARGET_NONE,
                        t.Instructions.MarkTargetEntityId, $"agent {i}");
                }
            }
        }

        [Test]
        public void ParseEmpty_AppliedConfig_IsBehaviourNeutral_DigestUnchanged()
        {
            const int ticks = 2 * 6 * 2;

            List<byte[]> unconfigured = RunChain(ticks, configure: null);
            List<byte[]> defaulted = RunChain(ticks, configure: e =>
                PlayerTacticConfigApplier.Apply(e, PlayerTacticFileLoader.Parse("")));

            for (int i = 0; i < ticks; i++)
            {
                CollectionAssert.AreEqual(unconfigured[i], defaulted[i],
                    $"Applying the parsed empty config perturbed the digest at tick {i + 1} — not behaviour-neutral.");
            }
        }

        // ── Parsed config routes per agent through SetPlayerTactic at the stride boundary ──

        [Test]
        public void Parse_ThenApply_RoutesPerAgent()
        {
            const string text =
                "[agent 0]\n" +
                "role = Poacher\n" +
                "duty = Attack\n";

            var engine = new MatchEngine(MatchSeed);
            PlayerTacticConfigApplier.Apply(engine, PlayerTacticFileLoader.Parse(text));
            TickToFirstStride(engine);

            Assert.AreEqual(PlayerRole.Poacher, engine.TestOnly_PlayerTactic(0).Role);
            Assert.AreEqual(Duty.Attack, engine.TestOnly_PlayerTactic(0).Duty);
            Assert.AreEqual(PlayerRole.Default, engine.TestOnly_PlayerTactic(1).Role);
        }

        // ── Fail-loud cases ──

        [Test]
        public void Parse_Malformed_ThrowsFormatException()
        {
            // key before any section
            Assert.Throws<System.FormatException>(() => PlayerTacticFileLoader.Parse("role = Poacher\n"));
            // unknown section
            Assert.Throws<System.FormatException>(() => PlayerTacticFileLoader.Parse("[team 0]\nrole = Poacher\n"));
            // agent index out of range
            Assert.Throws<System.FormatException>(
                () => PlayerTacticFileLoader.Parse($"[agent {MatchEngineConstants.SQUAD_SIZE}]\nrole = Poacher\n"));
            // non-numeric agent index
            Assert.Throws<System.FormatException>(() => PlayerTacticFileLoader.Parse("[agent x]\nrole = Poacher\n"));
            // unknown key
            Assert.Throws<System.FormatException>(() => PlayerTacticFileLoader.Parse("[agent 0]\nbogus = 1\n"));
            // unparsable enum value
            Assert.Throws<System.FormatException>(() => PlayerTacticFileLoader.Parse("[agent 0]\nrole = Nope\n"));
            // unparsable markTarget
            Assert.Throws<System.FormatException>(() => PlayerTacticFileLoader.Parse("[agent 0]\nmarkTarget = -2\n"));
            // duplicate key in a section
            Assert.Throws<System.FormatException>(
                () => PlayerTacticFileLoader.Parse("[agent 0]\nrole = Poacher\nrole = TargetMan\n"));
            // duplicate section
            Assert.Throws<System.FormatException>(
                () => PlayerTacticFileLoader.Parse("[agent 0]\nrole = Poacher\n[agent 0]\nduty = Attack\n"));
            // malformed line (no '=')
            Assert.Throws<System.FormatException>(() => PlayerTacticFileLoader.Parse("[agent 0]\nrole Poacher\n"));
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
// | 1.0     | 2026-06-30 | —      | Initial implementation — PlayerTacticFileLoader parser tests (#21 §3.3). |
#endregion
