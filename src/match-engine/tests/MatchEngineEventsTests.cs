// File:     src/match-engine/tests/MatchEngineEventsTests.cs
// Created:  2026-06-27
// Modified: 2026-06-27
// Author:   —
// Spec:     Match Engine design note (docs/tracking/match-engine-design.md) §5 Phase E, Event System #17 §4.4, Code Standards #20
// Purpose:  Phase E tests — events-phase consumers. Proves the host publishes a Tier A
//           PossessionChangedEvent on a possession transition (not on a no-change tick), that the
//           subscribed consumer interrupts ONLY the new holder's DecisionTree, that two same-seed
//           runs with a transition produce byte-identical (ledger-backed) digest chains, and that the
//           Tier A boot-phase Subscribe contract holds.

using System.Collections.Generic;

using NUnit.Framework;

using TacticalDirector.DecisionTree;
using TacticalDirector.DeterministicSim;
using TacticalDirector.EventSystem;

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
        private const int   NewHolder   = 5;   // an outfielder that gains possession
        private const int   OtherAgent  = 16;  // an unrelated agent (different team) — a control

        /// <summary>An EXECUTING DecisionTree state with a dispatched action — the only state from which
        /// NotifyInterrupt is observable (OnInterrupt transitions only from EXECUTING; §8 §3.7.2 row 6).</summary>
        private static DecisionTreeState ExecutingState() =>
            new DecisionTreeState(state: (int)DtState.EXECUTING, lastAction: default, hasDispatchedAction: true);

        /// <summary>
        /// Builds an engine, optionally scripts a one-shot possession gain by <paramref name="newHolder"/>
        /// before the FIRST processed tick (tick 1 is not an AI stride, so the AI phase does not overwrite
        /// the scripted possession that tick), runs <paramref name="ticks"/> ticks, and captures the
        /// per-tick snapshot digest chain.
        /// </summary>
        private static List<byte[]> RunWithTransition(ulong seed, int ticks, int newHolder)
        {
            var engine = new MatchEngine(seed);
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
            engine.TestOnly_SetDecisionTreeState(NewHolder, ExecutingState());

            // No TestOnly_SetPossession: possession stays NO_POSSESSION == the boot-seeded previous holder,
            // so the Resolve-phase diff finds no change and publishes no event. With no AI on tick 1, an
            // untouched EXECUTING DecisionTree is the proof that no possession-changed event was delivered.
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
#endregion
