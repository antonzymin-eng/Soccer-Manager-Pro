// File:     src/match-engine/tests/MatchEngineEventsTests.cs
// Created:  2026-06-27
// Modified: 2026-09-11 (W5 / ERR-013-011 second review: positive EventBus -> next tactical stride -> two-heartbeat BACKWARD_PASS debounce lock)
// Modified: 2026-06-27 (AR F1 — [TearDown] resets the static bus for test isolation)
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5 Phase E, Event System #17 §4.4, Code Standards #20
// Purpose:  Phase E tests — events-phase consumers. Proves the host publishes a Tier A
//           PossessionChangedEvent on a possession transition (not on a no-change tick), that the
//           subscribed consumer interrupts ONLY the new holder's DecisionTree, that two same-seed
//           runs with a transition produce byte-identical (ledger-backed) digest chains, and that the
//           Tier A boot-phase Subscribe contract holds.

using System.Collections.Generic;

using NUnit.Framework;
using UnityEngine;

using TacticalDirector.AgentMovement;
using TacticalDirector.DecisionTree;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;
using TacticalDirector.PassMechanics;
using TacticalDirector.PressingAI;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// Phase E events-phase consumer tests for <see cref="MatchEngine"/>.
    /// </summary>
    [TestFixture]
    public sealed class MatchEngineEventsTests
    {
        private const ulong MatchSeed   = 0x0123456789ABCDEFUL;
        private const int   TickCount   = 30;
        // §5.Z Phase H: a booted engine awards the kickoff to the home agent nearest the centre spot,
        // which is roster 5. NewHolder must therefore NOT be 5, or "script a possession gain" would be a
        // no-op against the boot award and the transition-vs-baseline control would compare two identical
        // runs. Roster 4 is a different home outfielder.
        private const int   NewHolder   = 4;   // an outfielder that gains possession (never the boot taker)
        private const int   OtherAgent  = 16;  // an unrelated agent (different team) — a control

        /// <summary>
        /// Resets the process-static EventBus after every test. Each test's first <c>new MatchEngine(...)</c>
        /// already resets the bus on the way in (via <c>Boot</c>'s <see cref="EventBus.ResetForNewMatch"/>),
        /// but <see cref="TierA_Subscribe_AfterBootPhase_Throws"/> deliberately ends with
        /// <c>BootPhaseComplete</c> set and a live <c>OnPossessionChanged</c> handler subscribed. Clearing
        /// the bus here guarantees this fixture leaves no Tier A subscriber or boot-phase state behind for a
        /// later fixture that might inspect the bus without first constructing an engine (the fixture-order
        /// hazard class the project has hit before).
        /// </summary>
        [TearDown]
        public void ResetEventBus()
        {
            EventBus.ResetForNewMatch();
        }

        /// <summary>An EXECUTING DecisionTree state with a dispatched action — the only state from which
        /// NotifyInterrupt is observable (OnInterrupt transitions only from EXECUTING; §8 §3.7.2 row 6).
        ///
        /// The dispatched action is a CONTINUOUS one (MOVE_TO_POSITION). §5.Z Phase H added the
        /// orchestrator's PASS/SHOOT completion sweep (ERR-008-015: a tree parked in EXECUTING on a
        /// pass/shot whose executor is idle is released to IDLE), so parking on the default action —
        /// whose Type is PASS, ordinal 0 — would now be undone by that sweep in the same tick, before the
        /// interrupt could be observed. A movement action is exempt from the sweep by §3.7.2's
        /// continuous-action rule, so it isolates the interrupt exactly as before.</summary>
        private static DecisionTreeState ExecutingState() =>
            new DecisionTreeState(
                state: (int)DtState.EXECUTING,
                lastAction: new AgentAction(
                    agentId: 0, type: ActionType.MOVE_TO_POSITION, targetAgentId: -1,
                    targetPosition: Vector2.zero, passParams: default, shotParams: default,
                    utilityScore: 0f, heartbeatTick: 0),
                hasDispatchedAction: true);

        /// <summary>
        /// Builds an engine, optionally scripts a one-shot possession gain by <paramref name="newHolder"/>
        /// before the FIRST processed tick (tick 1 is not an AI stride, so the AI phase does not overwrite
        /// the scripted possession that tick), runs <paramref name="ticks"/> ticks, and captures the
        /// per-tick snapshot digest chain.
        /// </summary>
        private static List<byte[]> RunWithTransition(ulong seed, int ticks, int newHolder)
        {
            var engine = new MatchEngine(seed);
            // Overrides the §5.Z Phase H boot kickoff award, so tick 1's Resolve settles on THIS holder.
            engine.TestOnly_SetPossession(newHolder); // settles in tick 1's Resolve → one PossessionChangedEvent
            var chain = new List<byte[]>(ticks);
            for (int i = 0; i < ticks; i++)
            {
                engine.RunTick();
                chain.Add(engine.CurrentSnapshotDigest);
            }
            return chain;
        }

        [Test]
        public void PossessionChange_InterruptsOnlyNewHolderDecisionTree()
        {
            var engine = new MatchEngine(MatchSeed);

            // Park two DecisionTrees in EXECUTING so an interrupt is observable as INTERRUPTED.
            engine.TestOnly_SetDecisionTreeState(NewHolder,  ExecutingState());
            engine.TestOnly_SetDecisionTreeState(OtherAgent, ExecutingState());

            // Force a possession gain by NewHolder. Tick 1 is not an AI stride, so the only thing that can
            // touch a DecisionTree this tick is the Events-phase possession-changed consumer.
            engine.TestOnly_SetPossession(NewHolder);
            engine.RunTick();

            Assert.AreEqual(DtState.INTERRUPTED, engine.TestOnly_DtState(NewHolder),
                "The possession-changed consumer must interrupt the NEW holder's DecisionTree " +
                "(EXECUTING → INTERRUPTED) so it re-plans next AI stride.");
            Assert.AreEqual(DtState.EXECUTING, engine.TestOnly_DtState(OtherAgent),
                "Only the new holder is interrupted — an unrelated agent's DecisionTree is untouched.");
        }

        [Test]
        public void NoPossessionChange_PublishesNothing_AndInterruptsNoDecisionTree()
        {
            var engine = new MatchEngine(MatchSeed);

            // §5.Z Phase H: tick 1 is no longer a no-change tick — Boot awards the kickoff, so the first
            // Resolve settles that award and publishes loose → taker. Run it first, THEN park the tree and
            // run a second tick in which possession genuinely does not move (tick 2 is not an AI stride
            // either, so nothing else can touch a DecisionTree).
            engine.RunTick();
            engine.TestOnly_SetDecisionTreeState(NewHolder, ExecutingState());
            engine.RunTick();

            Assert.AreEqual(DtState.EXECUTING, engine.TestOnly_DtState(NewHolder),
                "With no possession change, no PossessionChangedEvent is published, so no DecisionTree " +
                "is interrupted.");
        }

        [Test]
        public void TwoSameSeedRunsWithTransition_ProduceIdenticalDigestChains()
        {
            // The Phase E acceptance criterion: Tier A ledger digest STABILITY. Both runs publish the same
            // PossessionChangedEvent into the (process-static) ledger and the consumer applies the same
            // interrupt; the per-tick digest chain (world state + serialized ledger) must be byte-identical.
            // This also locks EventBus.ResetForNewMatch: run B's engine is constructed AFTER run A's first
            // DrainTick set BootPhaseComplete — without the per-match reset, run B's boot-time Subscribe
            // would throw (and the leaked run-A subscriber would accumulate toward MaxHandlersPerEventType).
            List<byte[]> chainA = RunWithTransition(MatchSeed, TickCount, NewHolder);
            List<byte[]> chainB = RunWithTransition(MatchSeed, TickCount, NewHolder);

            Assert.AreEqual(TickCount, chainA.Count);
            Assert.AreEqual(TickCount, chainB.Count);
            for (int i = 0; i < TickCount; i++)
            {
                CollectionAssert.AreEqual(chainA[i], chainB[i],
                    $"Ledger-backed snapshot digest diverged at tick {i + 1} — Phase E is non-deterministic.");
            }
        }

        [Test]
        public void TransitionRun_DiffersFromNoTransitionRun()
        {
            // Sanity that the possession transition actually changes the observable simulation (the event
            // is delivered and its consumer mutates DecisionTree state, which the D4 snapshot serializes).
            // Not an isolation of the ledger byte contribution — both the consumer effect and MatchContext
            // possession are in the digest preimage — but it guards against a silently inert Phase E.
            List<byte[]> withTransition = RunWithTransition(MatchSeed, TickCount, NewHolder);

            var baselineEngine = new MatchEngine(MatchSeed);
            var baseline = new List<byte[]>(TickCount);
            for (int i = 0; i < TickCount; i++)
            {
                baselineEngine.RunTick();
                baseline.Add(baselineEngine.CurrentSnapshotDigest);
            }

            // The transition is scripted before tick 1, so the very first digest already differs.
            CollectionAssert.AreNotEqual(baseline[0], withTransition[0],
                "A possession transition left tick 1's digest identical to the no-transition run — " +
                "Phase E produced no observable effect.");
        }

        [Test]
        public void PassAttemptEvent_RoutesOnlyToOpposingPressRing()
        {
            var engine = new MatchEngine(MatchSeed);

            EventBus.BeginTick(0);
            EventBus.BeginPhase(PhaseId.Resolve);
            EventBus.Publish(new PassAttemptEvent
            {
                AgentId = 16,
                TeamId = 1,
                TargetPosition = new Vector3(90f, 34f, 0f),
                TargetAgentId = -1,
            });
            EventBus.BeginPhase(PhaseId.Events);
            EventBus.DrainTick();
            EventBus.OnTickBoundary();

            Assert.IsTrue(engine.TestOnly_TryGetPressPassEvent(0, out PassAttemptEvent routed),
                "An away-team CONTACT pass must feed the home pressing ring.");
            Assert.AreEqual(16, routed.AgentId);
            Assert.AreEqual(1, routed.TeamId);
            Assert.AreEqual(90f, routed.TargetPosition.x);
            Assert.IsFalse(engine.TestOnly_TryGetPressPassEvent(1, out _),
                "A team's own pass must not overwrite its pressing trigger ring.");
        }

        [Test]
        public void HomePass_RingPreservesAuthoritativeWorldFrame()
        {
            var engine = new MatchEngine(MatchSeed);
            var worldTarget = new Vector3(20f, 10f, 1.5f);
            var worldVelocity = new Vector3(12f, -3f, 2f);
            var worldSpin = new Vector3(0.5f, 1.25f, -0.75f);

            EventBus.BeginTick(7);
            EventBus.BeginPhase(PhaseId.Resolve);
            EventBus.Publish(new PassAttemptEvent
            {
                AgentId = 4,
                TeamId = 0,
                TargetPosition = worldTarget,
                FinalVelocity = worldVelocity,
                FinalSpin = worldSpin,
                TargetAgentId = -1,
            });
            EventBus.BeginPhase(PhaseId.Events);
            EventBus.DrainTick();
            EventBus.OnTickBoundary();

            Assert.IsTrue(engine.TestOnly_TryGetPressPassEvent(1, out PassAttemptEvent routed),
                "A home-team CONTACT pass must feed the away pressing ring.");
            Assert.AreEqual(worldTarget, routed.TargetPosition,
                "The ring must retain the authoritative world-frame target; #13 normalizes only at read time.");
            Assert.AreEqual(worldVelocity, routed.FinalVelocity,
                "Velocity must remain in the same authoritative world frame as the retained event.");
            Assert.AreEqual(worldSpin, routed.FinalSpin,
                "Spin must remain unchanged; the retained PassAttemptEvent may not become a hybrid frame.");
            Assert.AreEqual(7u, routed.Tick,
                "The authoritative EventBus header tick is the recency key consumed by ERR-013-011.");
            Assert.IsFalse(engine.TestOnly_TryGetPressPassEvent(0, out _),
                "The passing team's own ring must remain untouched.");
        }

        [Test]
        public void PassAttemptEvent_FromCompletedPhysicsWindow_ReachesPressingDebounce()
        {
            var engine = new MatchEngine(MatchSeed);
            const int awayPasser = 16;
            var passerPos = new Vector2(50f, 34f);
            var passerState = AgentState.CreateAtPosition(passerPos, new Vector2(-1f, 0f));

            // Positioning AI seeds InPoss and requires three 10 Hz observations before committing
            // OutOfPoss. Warm that real production phase gate first (AI strides 6, 12, 18); otherwise
            // PressingAITick correctly returns before trigger evaluation on the first away-possession stride.
            for (int tick = 1; tick <= 18; tick++)
            {
                engine.TestOnly_SetAgent(awayPasser, passerState);
                engine.TestOnly_SetPossession(awayPasser);
                engine.RunTick();
            }
            Assert.AreEqual(18UL, engine.CurrentTick);

            // Publish after tick 18's AI read. At the next tactical evaluation (physics tick 24),
            // ERR-013-011 makes the completed visible interval [18,24), so the lower-bound event
            // must be accepted. Keeping the passer at a fixed point isolates the #13 geometry.
            EventBus.BeginTick(18);
            EventBus.BeginPhase(PhaseId.Resolve);
            EventBus.Publish(new PassAttemptEvent
            {
                AgentId = awayPasser,
                TeamId = 1,
                TargetPosition = new Vector3(56f, 34f, 0f),
                TargetAgentId = -1,
            });
            EventBus.BeginPhase(PhaseId.Events);
            EventBus.DrainTick();
            EventBus.OnTickBoundary();

            for (int tick = 19; tick <= 24; tick++)
            {
                engine.TestOnly_SetAgent(awayPasser, passerState);
                engine.TestOnly_SetPossession(awayPasser);
                engine.RunTick();
            }

            Assert.AreEqual(24UL, engine.CurrentTick);
            Assert.AreEqual(1, engine.TestOnly_PressingState(0).Trigger.BackwardPassDwell,
                "A lower-bound EventBus pass delivered after the prior AI read must start dwell on the next stride.");

            for (int tick = 25; tick <= 30; tick++)
            {
                engine.TestOnly_SetAgent(awayPasser, passerState);
                engine.TestOnly_SetPossession(awayPasser);
                engine.RunTick();
            }

            Assert.AreEqual(30UL, engine.CurrentTick);
            Assert.AreEqual(PressingAIConstants.TriggerDwellTicks,
                engine.TestOnly_PressingState(0).Trigger.BackwardPassDwell,
                "The EventBus-fed discrete pass must complete #13's two-heartbeat debounce after the event leaves the fresh window.");
        }

        [Test]
        public void TierA_Subscribe_AfterBootPhase_Throws()
        {
            // Locks the E2 ordering contract: the host MUST subscribe its Tier A consumer during the boot
            // phase (FR-EVT-020/021). Once a tick has drained, BootPhaseComplete is set and any further
            // Tier A Subscribe throws — which is exactly why MatchEngine subscribes in Boot, not lazily.
            var engine = new MatchEngine(MatchSeed);
            engine.RunTick(); // first DrainTick marks the boot phase complete

            Assert.Throws<System.InvalidOperationException>(
                () => EventBus.Subscribe<PossessionChangedEvent>(static (in PossessionChangedEvent _) => { }),
                "Tier A Subscribe after the first DrainTick must throw ERR_EVT_REGISTRATION_PHASE.");
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                        |
// | 1.0     | 2026-06-27 | —      | Initial Phase E events-phase consumer tests: publish-on-change |
// |         |            |        | (consumer interrupts only the new holder), no-event-on-no-     |
// |         |            |        | change, two-same-seed ledger-backed digest stability (+ reset   |
// |         |            |        | seam lock), transition-vs-baseline effect, Tier A boot-phase    |
// |         |            |        | Subscribe guard.                                                |
// | 1.1     | 2026-06-27 | —      | AR F1: added [TearDown] EventBus.ResetForNewMatch() so the      |
// |         |            |        | boot-phase-guard test cannot leave a live Tier A subscriber +   |
// |         |            |        | BootPhaseComplete set for a later (cross-fixture) test —        |
// |         |            |        | closes the static-bus order-dependence hazard.                 |
// | 1.2     | 2026-09-11 | —      | W5: added production EventBus routing lock for PassAttemptEvent -> opposing pressing ring. |
// | 1.3     | 2026-09-11 | —      | W5 review correction: ring retains the full authoritative world-frame event (including header Tick); #13 normalizes TargetPosition only at evaluation. |
// | 1.4     | 2026-09-11 | —      | ERR-013-011: positive EventBus tick-5 pass starts dwell at AI tick 6 and completes bounded event dwell at tick 12. |
#endregion
