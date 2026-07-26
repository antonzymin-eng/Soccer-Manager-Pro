// File:     src/match-engine/tests/MatchEnginePossessionBootstrapTests.cs
// Created:  2026-07-26
// Modified: 2026-07-26
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5.Z Phase H,
//           ERR-030-014 / ERR-008-014 / ERR-008-015, Code Standards #20
// Purpose:  Unit locks for the §5.Z Phase H possession bootstrap — the three production seams that
//           together break the "no motion ⇒ no reception ⇒ no possession ⇒ no kick ⇒ no motion" loop:
//           the kickoff/restart taker award (KD-H1), the loose-ball pickup (KD-H3), the loose-ball
//           collector designation (KD-H5), and the DecisionTree PASS/SHOOT completion sweep (KD-H4).
//           The composed proof that play actually develops is the Simulation-layer acceptance scenario
//           (MatchEnginePlayDevelopmentTests); these are the per-seam contracts underneath it.

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.BallPhysics;
using TacticalDirector.DecisionTree;

namespace TacticalDirector.MatchEngine
{
    [TestFixture]
    public sealed class MatchEnginePossessionBootstrapTests
    {
        private const ulong MatchSeed = 0x0123456789ABCDEFUL;

        // ── KD-H1: the kickoff / restart taker award ──────────────────────────────────

        [Test]
        public void Kickoff_AwardsPossessionToAHomeAgent()
        {
            // The single most important assertion in this fixture: a booted engine has a possessor at
            // all. Pre-Phase-H it did not, and because production had no other way to grant possession,
            // every match was a 90-minute 0-0 deadlock (ERR-030-014).
            var engine = new MatchEngine(MatchSeed);

            int taker = engine.TestOnly_PossessingAgentId;
            Assert.AreNotEqual(MatchEngineConstants.NO_POSSESSION, taker,
                "A booted engine must award the kickoff — with nobody in possession no agent can ever " +
                "kick, so the ball never moves and no first touch can ever occur.");
            Assert.AreEqual(MatchEngineConstants.FIRST_HALF_KICKOFF_TEAM, engine.AgentTeamId(taker),
                "The home team kicks off the first half.");
        }

        [Test]
        public void Kickoff_TakerIsTheNearestEligibleAgentToTheCentreSpot()
        {
            var engine = new MatchEngine(MatchSeed);
            int taker = engine.TestOnly_PossessingAgentId;

            var centre = new Vector2(MatchEngineConstants.KickoffBallXM, MatchEngineConstants.KickoffBallYM);
            float takerDist = Vector2.Distance(engine.AgentView(taker).Position, centre);
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                if (engine.AgentTeamId(i) != MatchEngineConstants.FIRST_HALF_KICKOFF_TEAM)
                {
                    continue;
                }
                Assert.LessOrEqual(takerDist, Vector2.Distance(engine.AgentView(i).Position, centre) + 1e-4f,
                    $"Agent {i} is closer to the centre spot than the selected taker {taker}.");
            }
        }

        [Test]
        public void Restart_SkipsSentOffAgents_WhenSelectingTheTaker()
        {
            // A sent-off agent is not a participant on ANY surface — the exclusion this project has had
            // to add to the first-touch receiver scan (AR-8 M-1), the foul interpretation (AR-9 M-1), AI
            // dispatch and every Mechanics-AI snapshot fill. Awarding a restart to one would be strictly
            // worse than the others: a frozen agent is never dispatched an action, so it can never
            // release the ball, and play would deadlock outright.
            var engine = new MatchEngine(MatchSeed);
            int naturalTaker = engine.TestOnly_PossessingAgentId;

            var withSentOff = new MatchEngine(MatchSeed);
            withSentOff.TestOnly_SetIsSentOff(naturalTaker, true);
            // Re-award by driving a fresh restart at the same spot.
            withSentOff.TestOnly_CheckMatchFlowTransitions(MatchEngineConstants.HALF_TIME_BOUNDARY_TICK);

            int taker = withSentOff.TestOnly_PossessingAgentId;
            Assert.AreNotEqual(MatchEngineConstants.NO_POSSESSION, taker, "A restart must still find a taker.");
            Assert.IsFalse(withSentOff.TestOnly_IsSentOff(taker), "A sent-off agent must never take a restart.");
        }

        // ── KD-H3: loose-ball pickup ──────────────────────────────────────────────────

        [Test]
        public void LooseBallAtRest_IsPickedUpByTheNearestAgentInReach()
        {
            // The second half of the deadlock: RunFirstTouch correctly refuses a ball that is not moving
            // (a still ball is not an incoming receive), so without a separate pickup path a pass that
            // simply ran out of momentum could never be collected and play died.
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetPossession(MatchEngineConstants.NO_POSSESSION);

            var spot = new Vector2(52.5f, 20.0f);
            const int Agent = 1;
            engine.TestOnly_SetAgent(Agent, AgentState.CreateAtPosition(spot, new Vector2(1f, 0f)));
            engine.TestOnly_SetCommand(Agent, MovementCommand.Stop(spot));
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                spot.x + 0.4f, spot.y, MatchEngineConstants.BALL_REST_HEIGHT_M)));

            engine.RunTick();

            Assert.AreEqual(Agent, engine.TestOnly_PossessingAgentId,
                "An agent standing over a resting loose ball must simply have it.");
        }

        [Test]
        public void LooseBallAtRest_OutOfReach_StaysLoose()
        {
            // The pickup is a reach gate, not teleportation — a ball nobody is standing over stays loose
            // (the designated collector then walks to it; see the collector tests below).
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetPossession(MatchEngineConstants.NO_POSSESSION);
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                MatchEngineConstants.KickoffBallXM, MatchEngineConstants.KickoffBallYM,
                MatchEngineConstants.BALL_REST_HEIGHT_M)));

            engine.RunTick();

            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId,
                "No agent is within pickup reach of the centre spot at kickoff, so the ball stays loose.");
        }

        [Test]
        public void LooseBallAtRest_SentOffAgentDoesNotPickItUp()
        {
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetPossession(MatchEngineConstants.NO_POSSESSION);

            var spot = new Vector2(52.5f, 20.0f);
            const int Agent = 1;
            engine.TestOnly_SetAgent(Agent, AgentState.CreateAtPosition(spot, new Vector2(1f, 0f)));
            engine.TestOnly_SetCommand(Agent, MovementCommand.Stop(spot));
            engine.TestOnly_SetIsSentOff(Agent, true);
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                spot.x + 0.4f, spot.y, MatchEngineConstants.BALL_REST_HEIGHT_M)));

            engine.RunTick();

            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId,
                "A sent-off agent must not collect the ball — it could never release it again.");
        }

        [Test]
        public void MovingLooseBall_IsNotPickedUp_FirstTouchOwnsIt()
        {
            // Pickup and first touch are disjoint by construction: pickup requires a ball BELOW the
            // first-touch minimum speed, first touch requires it at or above. This locks that a ball
            // moving fast enough for a genuine receive is never short-circuited by the pickup path.
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetPossession(MatchEngineConstants.NO_POSSESSION);

            var spot = new Vector2(52.5f, 20.0f);
            const int Agent = 1;
            engine.TestOnly_SetAgent(Agent, AgentState.CreateAtPosition(spot, new Vector2(1f, 0f)));
            engine.TestOnly_SetCommand(Agent, MovementCommand.Stop(spot));

            // Receding at 2 m/s and 0.6 m PAST the agent: too fast for pickup, and the first-touch
            // closing-direction gate rejects it — so no path may grant possession.
            BallState ball = BallState.CreateAtPosition(new Vector3(
                spot.x + 0.6f, spot.y, MatchEngineConstants.BALL_REST_HEIGHT_M));
            ball.Velocity = new Vector3(2f, 0f, 0f);
            ball.State    = BallStateType.Rolling;
            engine.TestOnly_SetBall(ball);

            engine.RunTick();

            Assert.AreEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId,
                "A ball moving above the first-touch speed gate belongs to first touch, not pickup — " +
                "and first touch refuses a receding ball.");
        }

        // ── KD-H5: loose-ball collector designation ───────────────────────────────────

        [Test]
        public void LooseBallAtRest_DesignatesOneCollectorPerTeam_ExcludingSentOff()
        {
            // The collector is designated by the HOST because only the host knows who is sent off. A
            // perception-derived "nearest teammate" rule defers to red-carded players, who never move —
            // measured, that deadlocked a match with the ball 4 m from a frozen sent-off agent.
            var engine = new MatchEngine(MatchSeed);
            engine.TestOnly_SetPossession(MatchEngineConstants.NO_POSSESSION);

            var ballSpot = new Vector2(52.5f, 34.0f);
            engine.TestOnly_SetBall(BallState.CreateAtPosition(new Vector3(
                ballSpot.x, ballSpot.y, MatchEngineConstants.BALL_REST_HEIGHT_M)));

            // Park a sent-off home agent right next to the ball (but outside pickup reach) and an
            // eligible home agent further away. The eligible one must be designated.
            const int SentOffAgent = 1;
            const int EligibleAgent = 2;
            var nearSpot = new Vector2(ballSpot.x + 3f, ballSpot.y);
            var farSpot  = new Vector2(ballSpot.x + 8f, ballSpot.y);
            engine.TestOnly_SetAgent(SentOffAgent, AgentState.CreateAtPosition(nearSpot, new Vector2(1f, 0f)));
            engine.TestOnly_SetIsSentOff(SentOffAgent, true);
            engine.TestOnly_SetAgent(EligibleAgent, AgentState.CreateAtPosition(farSpot, new Vector2(1f, 0f)));
            // Every other home outfielder is parked far away so the two above are the candidates.
            for (int i = 0; i < MatchEngineConstants.PLAYERS_PER_TEAM; i++)
            {
                if (i == SentOffAgent || i == EligibleAgent)
                {
                    continue;
                }
                engine.TestOnly_SetAgent(i, AgentState.CreateAtPosition(new Vector2(2f, 2f + i), new Vector2(1f, 0f)));
            }

            RunToFirstAiStride(engine);

            Assert.IsFalse(engine.TestOnly_LooseBallCollector(SentOffAgent),
                "A sent-off agent must never be designated to collect the ball.");
            Assert.IsTrue(engine.TestOnly_LooseBallCollector(EligibleAgent),
                "The nearest ELIGIBLE agent must be designated, even with a sent-off agent closer.");

            int homeCollectors = 0;
            for (int i = 0; i < MatchEngineConstants.PLAYERS_PER_TEAM; i++)
            {
                if (engine.TestOnly_LooseBallCollector(i))
                {
                    homeCollectors++;
                }
            }
            Assert.AreEqual(1, homeCollectors, "Exactly one collector is designated per team.");
        }

        [Test]
        public void PossessedBall_DesignatesNoCollector()
        {
            // The identity case: with the ball possessed, no context carries the flag, so the off-ball
            // branch is byte-identical to its pre-Phase-H shape.
            var engine = new MatchEngine(MatchSeed);
            RunToFirstAiStride(engine);

            Assert.AreNotEqual(MatchEngineConstants.NO_POSSESSION, engine.TestOnly_PossessingAgentId,
                "Precondition: the kickoff award means the ball is possessed.");
            for (int i = 0; i < MatchEngineConstants.SQUAD_SIZE; i++)
            {
                Assert.IsFalse(engine.TestOnly_LooseBallCollector(i),
                    $"Agent {i} must not be designated a collector while the ball is possessed.");
            }
        }

        // ── KD-H4: the DecisionTree PASS/SHOOT completion sweep ───────────────────────

        [Test]
        public void DecisionTreeParkedOnAPassWithAnIdleExecutor_IsReleasedToIdle()
        {
            // §3.7.2 holds a tree in EXECUTING after a PASS/SHOOT dispatch and re-evaluates only on
            // NotifyActionComplete / NotifyInterrupt / a forced refresh — but NOTHING in production ever
            // called NotifyActionComplete, so an agent that passed (or whose Execute was rejected, which
            // the dispatcher deliberately does not inspect) was frozen for the rest of the match: no
            // decisions, no movement commands, and no way to release the ball if it still held it.
            var engine = new MatchEngine(MatchSeed);
            const int Agent = 3;

            engine.TestOnly_SetDecisionTreeState(Agent, new DecisionTreeState(
                state: (int)DtState.EXECUTING,
                lastAction: new AgentAction(
                    agentId: Agent, type: ActionType.PASS, targetAgentId: -1,
                    targetPosition: Vector2.zero, passParams: default, shotParams: default,
                    utilityScore: 0f, heartbeatTick: 0),
                hasDispatchedAction: true));
            Assert.IsTrue(engine.TestOnly_PassExecutorIdle(Agent), "Precondition: no pass is in flight.");

            engine.RunTick();

            Assert.AreEqual(DtState.IDLE, engine.TestOnly_DtState(Agent),
                "A tree waiting on an executor that is not running has nothing left to wait for.");
        }

        [Test]
        public void DecisionTreeParkedOnAMovementAction_IsNotTouchedByTheSweep()
        {
            // The sweep must respect §3.7.2's continuous-action rule: movement-routed actions are
            // re-evaluated every heartbeat by the state machine itself and are not the sweep's business.
            var engine = new MatchEngine(MatchSeed);
            const int Agent = 3;

            engine.TestOnly_SetDecisionTreeState(Agent, new DecisionTreeState(
                state: (int)DtState.EXECUTING,
                lastAction: new AgentAction(
                    agentId: Agent, type: ActionType.MOVE_TO_POSITION, targetAgentId: -1,
                    targetPosition: Vector2.zero, passParams: default, shotParams: default,
                    utilityScore: 0f, heartbeatTick: 0),
                hasDispatchedAction: true));

            engine.RunTick();

            Assert.AreEqual(DtState.EXECUTING, engine.TestOnly_DtState(Agent),
                "A continuous action must not be completed by the PASS/SHOOT sweep.");
        }

        /// <summary>Ticks to (and through) the first AI stride so per-agent TacticalContexts are written.</summary>
        private static void RunToFirstAiStride(MatchEngine engine)
        {
            for (int i = 0; i < DeterministicSim.DeterministicSimConstants.AI_PHASE_STRIDE; i++)
            {
                engine.RunTick();
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                              |
// | 1.0     | 2026-07-26 | —      | Initial implementation — §5.Z Phase H per-seam locks: kickoff/restart|
// |         |            |        | taker award (incl. sent-off exclusion), loose-ball pickup (reach,   |
// |         |            |        | sent-off, disjointness from first touch), collector designation      |
// |         |            |        | (sent-off exclusion, one per team, identity when possessed), and the |
// |         |            |        | DecisionTree PASS/SHOOT completion sweep (releases blocking actions, |
// |         |            |        | leaves continuous ones alone).                                       |
#endregion
