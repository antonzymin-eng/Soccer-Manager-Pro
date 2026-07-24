// File:     src/living-world/Tests/ArcTriggerTests.cs
// Created:  2026-07-24
// Modified: 2026-07-24 (arc-triggers Low-2: non-zero-cursor post-restore resume test)
// Author:   —
// Spec:     Living World System #22 §3.4, §6.2, FR-LW-016/017/018/020/021, arc-triggers-design §9 (tests
//           1-4, 6-11), Testing Strategy #19 §3.1.4, Code Standards #20
// Purpose:  Arc-triggers Slice 1/E1 + Slice 2/E2 tests: distinct world.arcs stream key, flag-off
//           round-trip, stub-canon deterministic spawn (incl. KD-7 single-fire + re-arm), FR-LW-017/021
//           trigger-order determinism, the E2 save@N→restore→advance acceptance predicate (both a latched
//           post-restore run and a non-zero-cursor post-restore DRAW resume), v2-payload rejection, and
//           the KD-7 re-fire-after-restore completeness lock.

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

        // ── Test 2: null-canon round-trip (E2, WORLD_STORE_FORMAT_VERSION 3) ─────────────────────

        [Test]
        public void NullCanon_Snapshot_RoundTrips_AtFormatVersion3()
        {
            WorldStore store = new WorldStore(Manager, Seed);   // no canon => flag-off
            store.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.ManagerCriticism, 1);
            store.AdvanceDay();
            store.AdvanceDay();

            byte[] snap = store.Snapshot();

            int offset = 0;
            ushort version = CanonicalSerializer.ReadU16(snap, ref offset);
            Assert.AreEqual(LivingWorldConstants.WORLD_STORE_FORMAT_VERSION, version);
            Assert.AreEqual((ushort)3, version, "E2 bumps WORLD_STORE_FORMAT_VERSION to 3 (world.arcs cursor + latch)");

            // A flag-off run round-trips field-identically; the arc stream never advanced and the latch is empty.
            WorldStore restored = WorldStore.Restore(snap);
            Assert.AreEqual(snap, restored.Snapshot(), "flag-off Snapshot/Restore is byte-identical");
            Assert.AreEqual(0UL, store.ArcTriggers.RngCursor, "flag-off never draws the world.arcs stream");
            Assert.AreEqual(0, restored.ArcTriggers.LatchedCount, "flag-off restores an empty latch");
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

        // ── Test 6: E2 acceptance — flag-on save@N → restore(canon) → advance == uninterrupted ───

        [Test]
        public void FlagOn_SaveRestoreAdvance_MatchesUninterruptedRun()
        {
            const int N = 3, K = 2;
            ArcCanonSource above = EgoClashCanon(ContactA, 0.9f);

            WorldStore run = new WorldStore(Manager, Seed);
            run.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.ManagerCriticism, 1);
            run.SetArcCanon(above);
            for (int i = 0; i < N; i++)
            {
                run.AdvanceDay();   // fires on day 1, latched thereafter
            }
            Assert.AreEqual(1, run.Arcs.ArcCount);
            Assert.Greater(run.ArcTriggers.RngCursor, 0UL, "the save carries a non-zero world.arcs cursor");
            Assert.AreEqual(1, run.ArcTriggers.LatchedCount, "the save carries a non-empty latch");

            byte[] save = run.Snapshot();   // a flag-on save now succeeds (E2)

            for (int i = 0; i < K; i++)
            {
                run.AdvanceDay();
            }
            byte[] uninterrupted = run.Snapshot();

            WorldStore restored = WorldStore.Restore(save, above);
            for (int i = 0; i < K; i++)
            {
                restored.AdvanceDay();
            }
            Assert.AreEqual(uninterrupted, restored.Snapshot(),
                "§2.7: save@N → Restore(canon) → advance to N+K is byte-identical to the uninterrupted run");
        }

        // ── Test 6b: E2 resume — a post-restore rising edge DRAWS from a resumed NON-ZERO cursor ──
        //
        // Test 6 saves from a non-zero cursor but the post-restore days are all latched, so no draw
        // occurs after restore — it exercises cursor PRESERVATION, not cursor RESUMPTION. This test
        // saves after firing once (cursor 1) and then dropping below to re-arm the latch, so the FIRST
        // post-restore rising edge actually draws the world.arcs stream (cursor 1 → 2). It proves the
        // resumed stream keys on the restored cursor + ActionOrdinal, not on a fresh registration at 0.

        [Test]
        public void FlagOn_RestoreFromNonZeroCursor_ResumesArcStreamDeterministically()
        {
            ArcCanonSource above = EgoClashCanon(ContactA, 0.90f);
            ArcCanonSource below = EgoClashCanon(ContactA, 0.50f);

            WorldStore run = new WorldStore(Manager, Seed);
            run.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.ManagerCriticism, 1);

            // Fire once (cursor 0 → 1), then drop below to re-arm the latch.
            run.SetArcCanon(above);
            run.AdvanceDay();
            run.SetArcCanon(below);
            run.AdvanceDay();
            Assert.AreEqual(1, run.Arcs.ArcCount);
            Assert.AreEqual(1UL, run.ArcTriggers.RngCursor, "the save is taken from a NON-ZERO world.arcs cursor");
            Assert.AreEqual(0, run.ArcTriggers.LatchedCount, "and a re-armed (empty) latch, so the next rising edge draws");

            byte[] save = run.Snapshot();

            // Uninterrupted continuation: a fresh rising edge fires arc #2, drawing the cursor 1 → 2.
            run.SetArcCanon(above);
            run.AdvanceDay();
            Assert.AreEqual(2, run.Arcs.ArcCount);
            Assert.AreEqual(2UL, run.ArcTriggers.RngCursor, "the post-save rising edge advanced the cursor from 1 to 2");
            Arc uninterruptedArc2 = run.Arcs.GetArcAt(1);
            byte[] uninterrupted = run.Snapshot();

            // Restore from the non-zero cursor, then drive the same rising edge.
            WorldStore restored = WorldStore.Restore(save, above);
            Assert.AreEqual(1UL, restored.ArcTriggers.RngCursor, "the non-zero cursor is restored verbatim");
            restored.AdvanceDay();
            Assert.AreEqual(2, restored.Arcs.ArcCount, "the post-restore rising edge spawns arc #2");
            Arc restoredArc2 = restored.Arcs.GetArcAt(1);

            Assert.AreEqual(uninterruptedArc2.Cause.SnapshotRef, restoredArc2.Cause.SnapshotRef,
                "the post-restore draw resumes from cursor 1 (+ ActionOrdinal), so arc #2's stochastic component matches");
            Assert.AreEqual(uninterrupted, restored.Snapshot(),
                "save-from-non-zero-cursor → Restore → rising edge is byte-identical to the uninterrupted run");
        }

        // ── Test 8: E2 restore rejects a v2-format payload (no in-place migration) ───────────────

        [Test]
        public void Restore_RejectsPreE2FormatVersion()
        {
            WorldStore store = new WorldStore(Manager, Seed);
            byte[] snap = store.Snapshot();
            int o = 0;
            CanonicalSerializer.WriteU16(snap, ref o, 2);   // stamp the pre-E2 (world.text-only) version
            Assert.Throws<ArgumentException>(() => WorldStore.Restore(snap),
                "E2: a v2 payload is rejected fail-loud at the WORLD_STORE_FORMAT_VERSION gate (no migration)");
        }

        // ── Test 8b: KD-7 latch serialization seam — canonical round-trip + fail-loud on bad order ─

        [Test]
        public void Latch_EnumerateRestore_RoundTripsCanonically_AndRejectsNonCanonicalOrder()
        {
            DeterministicRngService rng = new DeterministicRngService(Seed);
            ArcTriggerEvaluator ev = new ArcTriggerEvaluator(rng, Manager);

            // Canonical order: ScopeKey ascending (board sentinel int.MinValue sorts first), then TriggerId.
            ArcTriggerEvaluator.LatchEntry[] canonical =
            {
                new ArcTriggerEvaluator.LatchEntry(LivingWorldConstants.ARC_BOARD_SCOPE_KEY, 4),
                new ArcTriggerEvaluator.LatchEntry(5, 1),
                new ArcTriggerEvaluator.LatchEntry(5, 2),
                new ArcTriggerEvaluator.LatchEntry(7, 1),
            };
            ev.RestoreLatched(canonical);
            Assert.AreEqual(4, ev.LatchedCount);
            CollectionAssert.AreEqual(canonical, ev.EnumerateLatchedCanonical(),
                "EnumerateLatchedCanonical returns the (scopeKey, triggerId) canonical order");

            // A non-canonical block (descending scope key) is rejected fail-loud.
            ArcTriggerEvaluator.LatchEntry[] descending =
            {
                new ArcTriggerEvaluator.LatchEntry(7, 1),
                new ArcTriggerEvaluator.LatchEntry(5, 1),
            };
            Assert.Throws<InvalidOperationException>(() => ev.RestoreLatched(descending));

            // A duplicate (scopeKey, triggerId) pair is rejected fail-loud.
            ArcTriggerEvaluator.LatchEntry[] duplicate =
            {
                new ArcTriggerEvaluator.LatchEntry(5, 1),
                new ArcTriggerEvaluator.LatchEntry(5, 1),
            };
            Assert.Throws<InvalidOperationException>(() => ev.RestoreLatched(duplicate));
        }

        // ── Test 11: KD-7 re-fire-after-restore lock (the serialized latch earns its keep) ──────

        [Test]
        public void FlagOn_StillLatchedTrigger_DoesNotRefireOnRestore()
        {
            ArcCanonSource above = EgoClashCanon(ContactA, 0.9f);
            WorldStore run = new WorldStore(Manager, Seed);
            run.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.ManagerCriticism, 1);
            run.SetArcCanon(above);
            run.AdvanceDay();   // fires; signal stays above ⇒ latched
            Assert.AreEqual(1, run.Arcs.ArcCount);
            byte[] save = run.Snapshot();

            WorldStore restored = WorldStore.Restore(save, above);
            Assert.AreEqual(1, restored.ArcTriggers.LatchedCount, "the armed-off latch is restored");
            restored.AdvanceDay();
            Assert.AreEqual(1, restored.Arcs.ArcCount,
                "KD-7: a still-latched trigger does NOT re-fire on the first post-restore tick");

            // Negative control: drop the restored latch and the SAME trigger re-fires immediately —
            // proving the serialized latch (not some other restored state) is what suppresses the re-fire.
            WorldStore control = WorldStore.Restore(save, above);
            control.ArcTriggers.RestoreLatched(Array.Empty<ArcTriggerEvaluator.LatchEntry>());
            control.AdvanceDay();
            Assert.AreEqual(2, control.Arcs.ArcCount, "without the restored latch the trigger re-fires");
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
