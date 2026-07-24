// File:     src/living-world/Tests/ArcTriggerTests.cs
// Created:  2026-07-24
// Modified: 2026-07-24
// Author:   —
// Spec:     Living World System #22 §3.4, §6.2, FR-LW-016/017/018/020/021, arc-triggers-design §9 (tests
//           1-5, 7, 10), Testing Strategy #19 §3.1.4, Code Standards #20
// Purpose:  Arc-triggers Slice 1/E1 tests: distinct world.arcs stream key, flag-off byte-neutrality,
//           stub-canon deterministic spawn (incl. KD-7 single-fire + re-arm), E1 fail-loud, and
//           FR-LW-017/021 trigger-order determinism.

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.LivingWorld;

namespace TacticalDirector.LivingWorld.Tests
{
    [TestFixture]
    public class ArcTriggerTests
    {
        private const int Manager = 0;
        private const int ContactA = 7;
        private const int ContactB = 5;
        private const ulong Seed = 0xA11CE5EEDUL;

        private static byte OwnedLayers()
            => (byte)((1 << (int)RelationshipLayer.Affinity) | (1 << (int)RelationshipLayer.Trust));

        // Canon that crosses the WonderkidVsVeteran (ego-clash) entity trigger for one contact.
        private static ArcCanonSource EgoClashCanon(int entityId, float signal)
            => new ArcCanonSource.Builder()
                .SetEntitySignal(entityId, LivingWorldConstants.ARC_SIGNAL_KEY_EGO_CLASH, signal)
                .Build();

        // ── Test 1: distinct world.arcs / world.text stream key (KD-4) ──────────────────────────

        [Test]
        public void WorldArcsStreamKey_DiffersFromWorldTextKey()
        {
            DeterministicRngService rng = new DeterministicRngService(Seed);
            InteractionTextGenerator text = new InteractionTextGenerator(rng);
            ArcTriggerEvaluator arcs = new ArcTriggerEvaluator(rng, Manager);

            ulong textKey = rng.GetStreamState(text.StreamIndex).StreamKey;
            ulong arcsKey = rng.GetStreamState(arcs.StreamIndex).StreamKey;

            Assert.AreNotEqual(textKey, arcsKey,
                "KD-4: world.arcs must use a distinct entity sentinel (−2) so its ComputeStreamKey differs from world.text (−1).");
            Assert.AreNotEqual(text.StreamIndex, arcs.StreamIndex, "the two streams occupy distinct registry slots");
        }

        // ── Test 2: null-canon byte-neutrality (E1) ─────────────────────────────────────────────

        [Test]
        public void NullCanon_Snapshot_IsByteNeutral_AtFormatVersion2()
        {
            WorldStore store = new WorldStore(Manager, Seed);   // no canon => flag-off
            store.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.ManagerCriticism, 1);
            store.AdvanceDay();
            store.AdvanceDay();

            byte[] snap = store.Snapshot();   // a flag-off (null-canon) run is snapshot-safe at E1

            int offset = 0;
            ushort version = CanonicalSerializer.ReadU16(snap, ref offset);
            Assert.AreEqual(LivingWorldConstants.WORLD_STORE_FORMAT_VERSION, version);
            Assert.AreEqual((ushort)2, version, "E1 does NOT bump WORLD_STORE_FORMAT_VERSION (still 2)");

            // Round-trips field-identically (flag-off), and the arc stream never advanced.
            WorldStore restored = WorldStore.Restore(snap);
            Assert.AreEqual(snap, restored.Snapshot(), "flag-off Snapshot/Restore is byte-identical");
            Assert.AreEqual(0UL, store.ArcTriggers.RngCursor, "flag-off never draws the world.arcs stream");
        }

        // ── Test 3: flag-off no-op through the loop ─────────────────────────────────────────────

        [Test]
        public void NullCanon_AdvanceDay_SpawnsNothing_LeavesCursorZero()
        {
            WorldStore store = new WorldStore(Manager, Seed);
            store.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.ManagerCriticism, 1);
            for (int i = 0; i < 5; i++)
            {
                store.AdvanceDay();
            }

            Assert.AreEqual(0, store.Arcs.ArcCount, "no canon ⇒ no arcs");
            Assert.AreEqual(0UL, store.ArcTriggers.RngCursor, "no rising edge ⇒ no world.arcs draw");
            Assert.AreEqual(0, store.ArcTriggers.LatchedCount);
        }

        // ── Test 4: stub-canon deterministic spawn + KD-7 single-fire + pin-less spawn ──────────

        [Test]
        public void StubCanon_RisingEdge_SpawnsArc_WithProvenanceAndPin_AndDoesNotRefire()
        {
            WorldStore store = new WorldStore(Manager, Seed);   // constructed WITHOUT canon...
            uint episodeId = store.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.ManagerCriticism, 1);
            store.SetArcCanon(EgoClashCanon(ContactA, 0.9f));   // ...canon set AFTER construction (dead-setter guard)

            store.AdvanceDay();

            Assert.AreEqual(1, store.Arcs.ArcCount, "the ego-clash rising edge spawns one arc");
            Arc arc = store.Arcs.GetArcAt(0);
            Assert.AreEqual(ArcKind.WonderkidVsVeteran, arc.Kind);
            Assert.AreEqual((ushort)1, arc.Cause.TriggerId, "FR-LW-016: TriggerId recorded inline");
            Assert.AreEqual(1, arc.Cause.Inputs.Length);
            Assert.AreEqual(LivingWorldConstants.ARC_SIGNAL_KEY_EGO_CLASH, arc.Cause.Inputs[0].Key);
            Assert.AreEqual(0.9f, arc.Cause.Inputs[0].Value, 0f, "the crossing signal is captured verbatim");
            Assert.IsTrue(store.Memory.IsEpisodePinned(Manager, ContactA, episodeId),
                "FR-LW-018: the source episode is pinned at spawn");
            Assert.AreEqual(1, store.ArcTriggers.LatchedCount, "KD-7: the (entity, trigger) pair is now armed-off");
            Assert.AreEqual(1UL, store.ArcTriggers.RngCursor, "exactly one world.arcs draw on the rising edge");

            // A second day, signal still above threshold ⇒ NO re-fire (KD-7 single-fire).
            store.AdvanceDay();
            Assert.AreEqual(1, store.Arcs.ArcCount, "KD-7: a sustained-high signal spawns exactly one arc, not one per day");
            Assert.AreEqual(1UL, store.ArcTriggers.RngCursor, "a latched (non-edge) tick consumes no cursor");
        }

        [Test]
        public void StubCanon_TwoRuns_ProduceIdenticalArcProvenance()
        {
            Arc RunOnce()
            {
                WorldStore store = new WorldStore(Manager, Seed);
                store.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.ManagerCriticism, 1);
                store.SetArcCanon(EgoClashCanon(ContactA, 0.9f));
                store.AdvanceDay();
                return store.Arcs.GetArcAt(0);
            }

            Arc a = RunOnce();
            Arc b = RunOnce();
            Assert.AreEqual(a.Cause.TriggerId, b.Cause.TriggerId);
            Assert.AreEqual(a.Cause.SnapshotRef, b.Cause.SnapshotRef,
                "same seed ⇒ same world.arcs draw ⇒ same recorded stochastic component");
            Assert.AreEqual(a.MaxLifetimeTick, b.MaxLifetimeTick);
        }

        [Test]
        public void BoardTrigger_NoEdge_SpawnsPinlessArc()
        {
            // A board/squad-level crossing with no citable edge is a valid PIN-LESS spawn, not a skip.
            WorldStore store = new WorldStore(Manager, Seed);
            ArcCanonSource canon = new ArcCanonSource.Builder()
                .SetBoardSignal(ArcKind.DressingRoomSplit, LivingWorldConstants.ARC_SIGNAL_KEY_PULSE_DIVERGENCE, 0.9f)
                .Build();
            store.SetArcCanon(canon);

            store.AdvanceDay();

            Assert.AreEqual(1, store.Arcs.ArcCount, "board rising edge spawns even with no episodes to pin");
            Arc arc = store.Arcs.GetArcAt(0);
            Assert.AreEqual(ArcKind.DressingRoomSplit, arc.Kind);
            Assert.AreEqual(0, arc.PinnedEpisodes.Length, "a board arc is pin-less at Stage 0");
        }

        // ── Test 5: E1 fail-loud on a flag-on Snapshot ──────────────────────────────────────────

        [Test]
        public void FlagOn_Snapshot_FailsLoud_AtE1()
        {
            WorldStore store = new WorldStore(Manager, Seed, EgoClashCanon(ContactA, 0.9f));
            store.AdvanceDay();   // fires ⇒ world.arcs cursor advances ⇒ not yet snapshot-safe
            Assert.Greater(store.ArcTriggers.RngCursor, 0UL);

            Assert.Throws<NotSupportedException>(() => store.Snapshot(),
                "E1: a flag-on run is not snapshot-safe until E2 serializes the cursor + latch");
        }

        // ── Test 7: FR-LW-017/021 trigger-order determinism ─────────────────────────────────────

        [Test]
        public void TriggerOrder_IsEntityIdAscending_ThenBoardByArcKindOrdinal()
        {
            WorldStore store = new WorldStore(Manager, Seed);
            ArcCanonSource canon = new ArcCanonSource.Builder()
                // two entities cross the ego-clash trigger on the same tick (distinct signal values so
                // the recorded provenance identifies which entity's arc spawned first)
                .SetEntitySignal(ContactB, LivingWorldConstants.ARC_SIGNAL_KEY_EGO_CLASH, 0.80f) // id 5 (lower)
                .SetEntitySignal(ContactA, LivingWorldConstants.ARC_SIGNAL_KEY_EGO_CLASH, 0.90f) // id 7 (higher)
                // two board triggers cross on the same tick (ArcKind ordinal 0 then 2)
                .SetBoardSignal(ArcKind.DressingRoomSplit, LivingWorldConstants.ARC_SIGNAL_KEY_PULSE_DIVERGENCE, 0.70f)
                .SetBoardSignal(ArcKind.BoardPatienceCollapse, LivingWorldConstants.ARC_SIGNAL_KEY_BOARD_IMPATIENCE, 0.85f)
                .Build();
            store.SetArcCanon(canon);

            store.AdvanceDay();

            Assert.AreEqual(4, store.Arcs.ArcCount);
            // Entity-scoped first (ascending id 5 then 7), then board by ArcKind ordinal (0 then 2).
            Assert.AreEqual((ushort)1, store.Arcs.GetArcAt(0).Cause.TriggerId, "entity 5 (Wonderkid) first");
            Assert.AreEqual(0.80f, store.Arcs.GetArcAt(0).Cause.Inputs[0].Value, 0f, "entity 5's signal");
            Assert.AreEqual((ushort)1, store.Arcs.GetArcAt(1).Cause.TriggerId, "entity 7 (Wonderkid) second");
            Assert.AreEqual(0.90f, store.Arcs.GetArcAt(1).Cause.Inputs[0].Value, 0f, "entity 7's signal");
            Assert.AreEqual(ArcKind.DressingRoomSplit, store.Arcs.GetArcAt(2).Kind, "board ordinal 0 before ordinal 2");
            Assert.AreEqual(ArcKind.BoardPatienceCollapse, store.Arcs.GetArcAt(3).Kind);
        }

        // ── Test 10: KD-7 edge-trigger re-arm cycle (fire once, hold, drop = re-arm, fire again) ─

        [Test]
        public void EdgeTrigger_ReArmCycle_FiresOncePerRisingEdge()
        {
            WorldStore store = new WorldStore(Manager, Seed);
            ArcCanonSource above = EgoClashCanon(ContactA, 0.90f);
            ArcCanonSource below = EgoClashCanon(ContactA, 0.50f);

            store.SetArcCanon(above);
            store.AdvanceDay();
            Assert.AreEqual(1, store.Arcs.ArcCount, "rising edge #1 fires");
            Assert.AreEqual(1, store.ArcTriggers.LatchedCount);

            // hold above across several days ⇒ no further spawn (latched)
            store.AdvanceDay();
            store.AdvanceDay();
            Assert.AreEqual(1, store.Arcs.ArcCount, "sustained-high: no re-fire while latched");
            Assert.AreEqual(1, store.ArcTriggers.LatchedCount);

            // drop below ⇒ no spawn, re-arms (latch drops)
            store.SetArcCanon(below);
            store.AdvanceDay();
            Assert.AreEqual(1, store.Arcs.ArcCount, "falling edge does not spawn");
            Assert.AreEqual(0, store.ArcTriggers.LatchedCount, "KD-7: dropping below re-arms the latch");

            // rise above again ⇒ a NEW rising edge fires
            store.SetArcCanon(above);
            store.AdvanceDay();
            Assert.AreEqual(2, store.Arcs.ArcCount, "rising edge #2 fires after re-arm");
            Assert.AreEqual(1, store.ArcTriggers.LatchedCount);
        }
    }
}
