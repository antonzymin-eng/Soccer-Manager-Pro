// File:     src/match-engine/tests/MatchEngineFoulCardTests.cs
// Created:  2026-07-14
// Modified: 2026-07-26 (foul/discipline balance pass: referee-call probability + capture-rule locks)
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

            // §5.Z Phase H (KD-H1): the free kick is awarded to the VICTIM's team, and the taker is that
            // team's nearest eligible agent to the foul spot — which here is the victim itself, standing
            // on it. (Pre-Phase-H this asserted NO_POSSESSION.)
            Assert.AreEqual(victim, engine.TestOnly_PossessingAgentId,
                "The victim is standing on the foul spot, so it takes its own free kick.");
            Assert.AreEqual(MatchEngineConstants.FoulCooldownTicks, engine.TestOnly_FoulCooldownRemaining);
        }

        [Test]
        public void InjectedFoul_SentOffVictim_DiscardedWithoutRestartOrCooldown()
        {
            // AR-9 M-1 regression lock: a sent-off agent cannot WIN a foul. Identical setup to
            // InjectedFoul_AwardsFreeKickAtVictimPosition_AndArmsCooldown (which proves this exact
            // candidate IS applied when the victim is active) except the victim is sent off — the
            // candidate must be discarded: no free kick at the victim's feet, no cooldown armed.
            var engine = new MatchEngine(MatchSeed);
            int offender = 0;
            int victim   = 12;
            Vector2 victimPos = new Vector2(40f, 20f);
            engine.TestOnly_SetAgent(victim, AgentState.CreateAtPosition(victimPos, new Vector2(1f, 0f)));
            engine.TestOnly_SetCommand(victim, MovementCommand.Stop(victimPos));
            engine.TestOnly_SetIsSentOff(victim, true);

            engine.TestOnly_InjectFoulCandidate(offender, victim);
            engine.RunTick();

            BallState ball = engine.TestOnly_BallSnapshot;
            Assert.IsFalse(Mathf.Abs(ball.Position.x - 40f) < 1e-4f && Mathf.Abs(ball.Position.y - 20f) < 1e-4f,
                "A discarded candidate must not award a free kick at the sent-off victim's position.");
            Assert.AreEqual(0, engine.TestOnly_FoulCooldownRemaining,
                "A discarded candidate must not arm the foul cooldown.");
        }

        [Test]
        public void InjectedFoul_SentOffOffender_DiscardedWithoutCardOrCooldown()
        {
            // AR-9 M-1 regression lock, offender direction: a sent-off agent cannot COMMIT a foul
            // (reachable while the forced-stop is still decelerating a just-sent-off agent). The
            // candidate is discarded before the card draw, so the offender's discipline state is
            // untouched and no cooldown is armed.
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetIsSentOff(0, true);

            engine.TestOnly_InjectFoulCandidate(offender: 0, victim: 12);
            engine.RunTick();

            Assert.AreEqual(0, engine.TestOnly_FoulCooldownRemaining,
                "A discarded candidate must not arm the foul cooldown.");
            Assert.AreEqual((byte)0, engine.TestOnly_YellowCards(0),
                "A discarded candidate must not reach the card draw.");
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

        // ── Referee-call probability (foul-discipline-balance-design.md KD-F1..KD-F4) ───────

        [Test]
        public void FoulCallProbability_AtThreshold_EqualsTheConfiguredProbability()
        {
            Assert.AreEqual(
                MatchEngineConstants.FoulCallProbability,
                MatchEngine.TestOnly_FoulCallProbability(MatchEngineConstants.FoulImpactForceThresholdN),
                1e-6f,
                "At the qualifying threshold the call probability is exactly the [GT] rate knob.");
        }

        [Test]
        public void FoulCallProbability_ScalesLinearlyWithForce()
        {
            float atThreshold = MatchEngine.TestOnly_FoulCallProbability(
                MatchEngineConstants.FoulImpactForceThresholdN);
            float atDouble = MatchEngine.TestOnly_FoulCallProbability(
                MatchEngineConstants.FoulImpactForceThresholdN * 2f);

            Assert.AreEqual(atThreshold * 2f, atDouble, 1e-6f,
                "A twice-as-hard challenge is twice as likely to be given (KD-F1) — this is the whole "
                + "reason the capture rule keeps the strongest contact in a tick rather than the first.");
        }

        [Test]
        public void FoulCallProbability_SaturatesAtOne()
        {
            Assert.AreEqual(1f, MatchEngine.TestOnly_FoulCallProbability(MatchEngine.CertainFoulForceN), 1e-6f);
        }

        [Test]
        public void FoulCallProbability_NonPositiveOrNonFinite_ReturnsZero()
        {
            // Fail-closed: a corrupt force yields a MISSED foul, never a phantom one.
            Assert.AreEqual(0f, MatchEngine.TestOnly_FoulCallProbability(0f));
            Assert.AreEqual(0f, MatchEngine.TestOnly_FoulCallProbability(-1f));
            Assert.AreEqual(0f, MatchEngine.TestOnly_FoulCallProbability(float.NaN));
            Assert.AreEqual(0f, MatchEngine.TestOnly_FoulCallProbability(float.PositiveInfinity));
        }

        [Test]
        public void WavedOnCandidate_LeavesNoTrace_NoCardNoCooldownNoRestart()
        {
            // KD-F3 lock. A zero-force candidate has call probability 0, so it is waved on with
            // certainty whatever the RNG produces — the deterministic end of the probability range.
            // Critically the cooldown must stay at 0: arming it on a no-call would silently suppress
            // detection of the genuine foul that follows.
            var engine = new MatchEngine(MatchSeed);
            Vector2 victimPos = new Vector2(40f, 20f);
            engine.TestOnly_SetAgent(12, AgentState.CreateAtPosition(victimPos, new Vector2(1f, 0f)));
            engine.TestOnly_SetCommand(12, MovementCommand.Stop(victimPos));

            engine.TestOnly_InjectFoulCandidate(offender: 0, victim: 12, forceN: 0f);
            engine.RunTick();

            Assert.AreEqual(0, engine.TestOnly_FoulCooldownRemaining,
                "A waved-on candidate must not arm the cooldown (KD-F3).");
            Assert.AreEqual((byte)0, engine.TestOnly_YellowCards(0),
                "A waved-on candidate must not reach the card issue.");

            BallState ball = engine.TestOnly_BallSnapshot;
            Assert.IsFalse(Mathf.Abs(ball.Position.x - victimPos.x) < 1e-4f
                           && Mathf.Abs(ball.Position.y - victimPos.y) < 1e-4f,
                "A waved-on candidate must not award a free kick.");
        }

        [Test]
        public void WavedOnCandidate_IsClearedAndDoesNotLeakIntoTheNextTick()
        {
            // The candidate slot must be consumed by the wave-on, not left armed — otherwise a single
            // no-call would be re-adjudicated every tick until it happened to be given.
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_InjectFoulCandidate(offender: 0, victim: 12, forceN: 0f);
            engine.RunTick();

            Assert.AreEqual(0f, engine.TestOnly_FoulCandidateForceN,
                "The waved-on candidate must be cleared, not carried forward.");
        }

        [Test]
        public void CandidateCapture_KeepsTheStrongestContactInATick()
        {
            // KD-F4 lock, driven through the real consumer with synthetic events: under a force-scaled
            // call probability, first-wins would let a marginal contact shadow a hard challenge in the
            // same tick and systematically under-call the fouls that most deserve giving.
            var engine = new MatchEngine(MatchSeed);
            float threshold = MatchEngineConstants.FoulImpactForceThresholdN;

            engine.TestOnly_FoulCandidateConsumer.OnCollisionEvent(
                CrossTeamFromBehind(engine, forceN: threshold + 1f));
            engine.TestOnly_FoulCandidateConsumer.OnCollisionEvent(
                CrossTeamFromBehind(engine, forceN: threshold + 900f));
            engine.TestOnly_FoulCandidateConsumer.OnCollisionEvent(
                CrossTeamFromBehind(engine, forceN: threshold + 400f));

            Assert.AreEqual(threshold + 900f, engine.TestOnly_FoulCandidateForceN, 1e-3f,
                "The strongest qualifying contact in the tick wins, regardless of arrival order.");
        }

        [Test]
        public void CandidateCapture_IgnoresContactsBelowTheThreshold()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_FoulCandidateConsumer.OnCollisionEvent(
                CrossTeamFromBehind(engine, forceN: MatchEngineConstants.FoulImpactForceThresholdN - 1f));

            Assert.AreEqual(0f, engine.TestOnly_FoulCandidateForceN,
                "Contact below the qualifying threshold is not a candidate at all.");
        }

        /// <summary>Builds a synthetic cross-team FROM_BEHIND agent-agent collision at the given force.
        /// Agents 0 and 12 are on opposite teams in the boot lineup.</summary>
        private static TacticalDirector.CollisionSystem.CollisionEvent CrossTeamFromBehind(
            MatchEngine engine, float forceN)
        {
            Assert.AreNotEqual(engine.AgentTeamId(0), engine.AgentTeamId(12),
                "Fixture assumption: agents 0 and 12 are on opposite teams.");

            return new TacticalDirector.CollisionSystem.CollisionEvent
            {
                Type = TacticalDirector.CollisionSystem.CollisionType.AGENT_AGENT,
                Entity1ID = 0,
                Entity2ID = 12,
                FoulData = new TacticalDirector.CollisionSystem.ContactForceData
                {
                    Type = TacticalDirector.CollisionSystem.ContactType.FROM_BEHIND,
                    ForceMagnitude = forceN,
                    InstigatorAgentID = 0,
                    VictimAgentID = 12,
                },
            };
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
// | 1.1     | 2026-07-17 | —      | AR-9 M-1 locks: a foul candidate with a sent-off VICTIM (the   |
// |         |            |        |   exact geometry of the positive free-kick test) or a sent-off |
// |         |            |        |   OFFENDER is discarded — no restart at the victim's feet, no  |
// |         |            |        |   cooldown armed, no card draw reached.                        |
// | 1.2     | 2026-07-26 | —      | Foul/discipline balance pass locks (KD-F1..KD-F4): the pure    |
// |         |            |        |   referee-call probability shape (threshold identity, linear   |
// |         |            |        |   force scaling, saturation, fail-closed on corrupt force);    |
// |         |            |        |   the wave-on path leaving no trace and not leaking forward;   |
// |         |            |        |   and strongest-wins candidate capture driven through the real |
// |         |            |        |   consumer with synthetic collision events.                    |
#endregion
