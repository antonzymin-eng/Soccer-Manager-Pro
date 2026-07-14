// File:     src/match-engine/tests/MatchEngineFoulCardTests.cs
// Created:  2026-07-14
// Modified: 2026-07-14
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-flow-completion-design.md) §3, Code Standards #20
// Purpose:  Locks the pure card-severity band lookup + promotion logic (deterministic, independent of
//           the RNG draw), the Resolve-phase foul application (free kick + cooldown), and the sent-off
//           movement freeze. FoulCommittedEvent/CardIssuedEvent/SubstitutionEvent were registered in
//           EventRegistry with zero producers before this note; this is their first wiring.

using NUnit.Framework;

using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEngineFoulCardTests
    {
        private const ulong MatchSeed = 0x00C0FFEE5EEDBA11UL;

        // ── Pure card-severity logic ──────────────────────────────────────────────────

        [Test]
        public void DetermineCardKind_RedBand_LowerBoundInclusive_UpperBoundExclusive()
        {
            Assert.AreEqual((byte)1, MatchEngine.TestOnly_DetermineCardKind(0f));
            Assert.AreEqual((byte)1, MatchEngine.TestOnly_DetermineCardKind(MatchEngineConstants.RedCardProbability - 0.0001f));
            Assert.AreEqual((byte?)0, MatchEngine.TestOnly_DetermineCardKind(MatchEngineConstants.RedCardProbability),
                "At the exact red/yellow boundary the band is [RedCardProbability, ...) for yellow — no longer red.");
        }

        [Test]
        public void DetermineCardKind_YellowBand()
        {
            float mid = MatchEngineConstants.RedCardProbability + MatchEngineConstants.YellowCardProbability * 0.5f;
            Assert.AreEqual((byte)0, MatchEngine.TestOnly_DetermineCardKind(mid));
        }

        [Test]
        public void DetermineCardKind_NoCardBand_ReturnsNull()
        {
            float justBeyond = MatchEngineConstants.RedCardProbability + MatchEngineConstants.YellowCardProbability;
            Assert.IsNull(MatchEngine.TestOnly_DetermineCardKind(justBeyond));
            Assert.IsNull(MatchEngine.TestOnly_DetermineCardKind(0.999f));
        }

        [Test]
        public void ApplyCardAndCheckSentOff_FirstYellow_NotSentOff()
        {
            var engine = new MatchEngine(MatchSeed);
            byte issued = engine.TestOnly_ApplyCardAndCheckSentOff(agentId: 2, drawnKind: 0);

            Assert.AreEqual((byte)0, issued, "First yellow issues as plain Yellow.");
            Assert.AreEqual((byte)1, engine.TestOnly_YellowCards(2));
            Assert.IsFalse(engine.TestOnly_IsSentOff(2));
        }

        [Test]
        public void ApplyCardAndCheckSentOff_SecondYellow_PromotesToRed_AndSendsOff()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_ApplyCardAndCheckSentOff(agentId: 2, drawnKind: 0); // first yellow
            byte issued = engine.TestOnly_ApplyCardAndCheckSentOff(agentId: 2, drawnKind: 0); // second

            Assert.AreEqual((byte)2, issued, "Second yellow promotes to SecondYellow (2).");
            Assert.IsTrue(engine.TestOnly_IsSentOff(2));
        }

        [Test]
        public void ApplyCardAndCheckSentOff_StraightRed_SendsOffImmediately()
        {
            var engine = new MatchEngine(MatchSeed);
            byte issued = engine.TestOnly_ApplyCardAndCheckSentOff(agentId: 5, drawnKind: 1);

            Assert.AreEqual((byte)1, issued);
            Assert.IsTrue(engine.TestOnly_IsSentOff(5));
            Assert.AreEqual((byte)0, engine.TestOnly_YellowCards(5), "A straight red does not touch the yellow count.");
        }

        // ── MatchEngine integration ───────────────────────────────────────────────────

        [Test]
        public void InjectedFoul_AwardsFreeKickAtVictimPosition_AndArmsCooldown()
        {
            var engine = new MatchEngine(MatchSeed);
            int offender = 0;
            int victim   = 12;
            Vector2 victimPos = new Vector2(40f, 20f);
            engine.TestOnly_SetAgent(victim, AgentState.CreateAtPosition(victimPos, new Vector2(1f, 0f)));
            // Keep the held command's embedded target consistent with the new position — Stop(pos)
            // bakes pos in as both current-position and target, so leaving the stale boot-time
            // command would otherwise steer the agent back toward its original kickoff slot during
            // this tick's Physics phase (which runs before Resolve applies the foul).
            engine.TestOnly_SetCommand(victim, MovementCommand.Stop(victimPos));

            Assert.AreEqual(0, engine.TestOnly_FoulCooldownRemaining);
            engine.TestOnly_InjectFoulCandidate(offender, victim);
            engine.RunTick();

            BallState ball = engine.TestOnly_BallSnapshot;
            Assert.AreEqual(40f, ball.Position.x, 1e-4f);
            Assert.AreEqual(20f, ball.Position.y, 1e-4f);
            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId);
            Assert.AreEqual(MatchEngineConstants.FoulCooldownTicks, engine.TestOnly_FoulCooldownRemaining);
        }

        [Test]
        public void FoulCooldown_DecrementsEachTick()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_InjectFoulCandidate(0, 1);
            engine.RunTick();
            int cooldown = engine.TestOnly_FoulCooldownRemaining;
            Assert.Greater(cooldown, 0);

            engine.RunTick();
            Assert.AreEqual(cooldown - 1, engine.TestOnly_FoulCooldownRemaining);
        }

        [Test]
        public void SentOffAgent_DeceleratesToRest_UnderTheForcedStopCommand()
        {
            var engine = new MatchEngine(MatchSeed);
            int agentId = 3;
            AgentState a = engine.TestOnly_AgentSnapshot(agentId);
            a.Velocity = new Vector2(5f, 0f);
            a.Speed    = 5f;
            engine.TestOnly_SetAgent(agentId, a);
            engine.TestOnly_SetIsSentOff(agentId, true);

            for (int i = 0; i < 300; i++) // 5 s @ 60 Hz — ample time to decelerate from 5 m/s at any documented rate
            {
                engine.RunTick();
            }

            Assert.Less(engine.TestOnly_AgentSnapshot(agentId).Speed, 1.0f,
                "A sent-off agent is forced to Stop every physics tick and should be at rest well within 5 s.");
        }

        [Test]
        public void TwoRunsWithAFoul_BitwiseIdenticalDigests()
        {
            byte[] a = RunWithFoul();
            byte[] b = RunWithFoul();
            CollectionAssert.AreEqual(a, b);
        }

        private static byte[] RunWithFoul()
        {
            var engine = new MatchEngine(MatchSeed);
            for (int i = 0; i < 5; i++) engine.RunTick();
            engine.TestOnly_InjectFoulCandidate(0, 12);
            for (int i = 0; i < 10; i++) engine.RunTick();
            return engine.CurrentSnapshotDigest;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-14 | —      | Initial foul/card suite: pure DetermineCardKind band locks +   |
// |         |            |        |   ApplyCardAndCheckSentOff promotion logic (first/second       |
// |         |            |        |   yellow, straight red) + MatchEngine integration (free-kick   |
// |         |            |        |   placement, cooldown arm/decrement, sent-off deceleration     |
// |         |            |        |   freeze) + two-run determinism.                               |
#endregion
