// File:     src/living-world/Tests/SeasonWorldLoopTests.cs
// Created:  2026-07-02
// Modified: 2026-07-02
// Author:   —
// Spec:     Living World System #22 §5 (T-LW-U-011..018, T-LW-I-011..014, T-LW-DET-002/006/007,
//           T-LW-FAIL-005), Testing Strategy #19 §3.1.4, Code Standards #20
// Purpose:  Season/world-loop slice-1 tests: WorldClock calendar semantics, MemoryStore episode
//           append/evict/decay/pins, ColdStore compress/rehydrate round-trip, WorldLoop phase order,
//           canonical iteration, and two-run determinism.

using System;

using NUnit.Framework;

using TacticalDirector.LivingWorld;

namespace TacticalDirector.LivingWorld.Tests
{
    [TestFixture]
    public class SeasonWorldLoopTests
    {
        private const int Manager = 0;
        private const int ContactA = 7;
        private const int ContactB = 3;

        private static byte Layers(params RelationshipLayer[] layers)
        {
            byte mask = 0;
            foreach (RelationshipLayer l in layers)
            {
                mask |= (byte)(1 << (int)l);
            }
            return mask;
        }

        private static MemoryStore StoreWithEdge(int from, int to)
        {
            MemoryStore store = new MemoryStore();
            store.GetOrCreateEdge(from, to, Layers(RelationshipLayer.Affinity, RelationshipLayer.Trust));
            return store;
        }

        // ── WorldClock (KD-4 / FR-LW-019 / T-LW-DET-006) ────────────────────────────────────

        [Test]
        public void WorldClock_AdvancesOneDayPerTick_AndRestores()
        {
            WorldClock clock = new WorldClock();
            Assert.AreEqual(0u, clock.CurrentWorldTick, "day 0 at world start");
            clock.Advance();
            clock.Advance();
            Assert.AreEqual(2u, clock.CurrentWorldTick, "one worldTick = one calendar day (FR-LW-019)");
            clock.RestoreFromSnapshot(100u);
            Assert.AreEqual(100u, clock.CurrentWorldTick, "restore from snapshot (FR-LW-022)");
        }

        [Test]
        public void TLWDET006_WorldTickAdvancesOnlyViaTheWorldLoop()
        {
            // The clock has no coupling to MatchClock / the 10 Hz / 60 Hz loops; only RunWorldTick
            // advances it — store activity (episode writes) must not move the calendar.
            WorldClock clock = new WorldClock();
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            WorldLoop loop = new WorldLoop(clock, store);

            store.RecordEpisode(Manager, ContactA, EventKind.ManagerDefence, 0u, 1);
            Assert.AreEqual(0u, loop.CurrentWorldTick, "T-LW-DET-006: store writes never advance the calendar");
            loop.RunWorldTick();
            Assert.AreEqual(1u, loop.CurrentWorldTick, "T-LW-DET-006: only the world loop advances the calendar");
        }

        // ── MemoryStore: episodes (§3.2; T-LW-U-011..018) ───────────────────────────────────

        [Test]
        public void TLWU011_EpisodeIdIsMonotonicPerEdge()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            store.GetOrCreateEdge(Manager, ContactB, Layers(RelationshipLayer.Affinity));

            Assert.AreEqual(0u, store.RecordEpisode(Manager, ContactA, EventKind.Benching, 1u, 0));
            Assert.AreEqual(1u, store.RecordEpisode(Manager, ContactA, EventKind.Benching, 2u, 0));
            Assert.AreEqual(0u, store.RecordEpisode(Manager, ContactB, EventKind.Benching, 2u, 0),
                "T-LW-U-011: episodeId is per-edge, monotonic (FR-LW-009)");
        }

        [Test]
        public void TLWU012_FullBufferEvictsLowestSalienceUnpinned()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            // Fill to depth; decay between writes so earlier episodes have lower salience.
            for (int i = 0; i < LivingWorldConstants.MEMORY_BUFFER_DEPTH; i++)
            {
                store.RecordEpisode(Manager, ContactA, EventKind.Benching, (uint)i, 0);
                store.DecayEpisodeSalience(LivingWorldConstants.SALIENCE_DECAY_RATE);
            }
            // Episode 0 has decayed most → lowest salience → evicted on overflow.
            store.RecordEpisode(Manager, ContactA, EventKind.ManagerCriticism, 100u, 0);

            store.TryGetEdge(Manager, ContactA, out RelationshipEdge edge);
            Assert.AreEqual(LivingWorldConstants.MEMORY_BUFFER_DEPTH, edge.Memory.Length, "buffer back at depth");
            for (int i = 0; i < edge.Memory.Length; i++)
            {
                Assert.AreNotEqual(0u, edge.Memory[i].EpisodeId, "T-LW-U-012: lowest-salience episode 0 evicted (FR-LW-010)");
            }
        }

        [Test]
        public void TLWU013_EvictionTieBreaksOldestTickThenLowestId()
        {
            // All episodes written same-tick with no decay between ⇒ equal salience; the FR-LW-021
            // tiebreak must evict the oldest worldTick, then the lowest episodeId.
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            for (int i = 0; i < LivingWorldConstants.MEMORY_BUFFER_DEPTH + 1; i++)
            {
                store.RecordEpisode(Manager, ContactA, EventKind.Benching, 5u, 0);
            }
            store.TryGetEdge(Manager, ContactA, out RelationshipEdge edge);
            Assert.AreEqual(LivingWorldConstants.MEMORY_BUFFER_DEPTH, edge.Memory.Length);
            for (int i = 0; i < edge.Memory.Length; i++)
            {
                Assert.AreNotEqual(0u, edge.Memory[i].EpisodeId,
                    "T-LW-U-013: salience tie evicts lowest episodeId (FR-LW-021)");
            }
        }

        [Test]
        public void TLWU014_PinnedEpisodeIsSkippedByEviction()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            uint pinnedId = store.RecordEpisode(Manager, ContactA, EventKind.PressConferenceTrap, 0u, 0);
            store.PinEpisode(Manager, ContactA, pinnedId);
            store.DecayEpisodeSalience(0.5f); // Make the pinned episode the salience floor.

            for (int i = 0; i < LivingWorldConstants.MEMORY_BUFFER_DEPTH; i++)
            {
                store.RecordEpisode(Manager, ContactA, EventKind.Benching, (uint)(i + 1), 0);
            }

            store.TryGetEdge(Manager, ContactA, out RelationshipEdge edge);
            Assert.AreEqual(LivingWorldConstants.MEMORY_BUFFER_DEPTH, edge.Memory.Length, "next-lowest unpinned evicted instead");
            bool pinnedSurvives = false;
            for (int i = 0; i < edge.Memory.Length; i++)
            {
                pinnedSurvives |= edge.Memory[i].EpisodeId == pinnedId;
            }
            Assert.IsTrue(pinnedSurvives, "T-LW-U-014: arc-pinned episode exempt from eviction (FR-LW-018)");
        }

        [Test]
        public void TLWU015_AllPinnedGrowsTransiently_ShrinksOnUnpin()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            for (int i = 0; i < LivingWorldConstants.MEMORY_BUFFER_DEPTH; i++)
            {
                uint id = store.RecordEpisode(Manager, ContactA, EventKind.Benching, (uint)i, 0);
                store.PinEpisode(Manager, ContactA, id);
            }
            uint overflowId = store.RecordEpisode(Manager, ContactA, EventKind.ManagerCriticism, 99u, 0);
            store.PinEpisode(Manager, ContactA, overflowId);

            store.TryGetEdge(Manager, ContactA, out RelationshipEdge edge);
            Assert.AreEqual(LivingWorldConstants.MEMORY_BUFFER_DEPTH + 1, edge.Memory.Length,
                "T-LW-U-015: all-pinned buffer grows transiently (§3.2 step 2)");

            store.UnpinEpisode(Manager, ContactA, 0u);
            store.TryGetEdge(Manager, ContactA, out edge);
            Assert.AreEqual(LivingWorldConstants.MEMORY_BUFFER_DEPTH, edge.Memory.Length,
                "T-LW-U-015: buffer shrinks back to depth as arcs unpin");
        }

        [Test]
        public void TLWU016_SalienceDecayMatchesGeometricRule()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            store.RecordEpisode(Manager, ContactA, EventKind.Benching, 0u, 0);
            const int days = 10;
            for (int i = 0; i < days; i++)
            {
                store.DecayEpisodeSalience(LivingWorldConstants.SALIENCE_DECAY_RATE);
            }
            store.TryGetEdge(Manager, ContactA, out RelationshipEdge edge);
            float expected = LivingWorldConstants.SALIENCE_INITIAL;
            for (int i = 0; i < days; i++)
            {
                expected *= 1f - LivingWorldConstants.SALIENCE_DECAY_RATE;
            }
            Assert.That(edge.Memory[0].Salience, Is.EqualTo(expected).Within(1e-6f),
                "T-LW-U-016: s' = s · (1 − rate) per day (§3.2 step 3)");
        }

        [Test]
        public void TLWU017_PinningDanglingEpisodeFailsLoud()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            Assert.Throws<InvalidOperationException>(
                () => store.PinEpisode(Manager, ContactA, 42u),
                "T-LW-U-017: F1 dangling-id guard");
        }

        [Test]
        public void TLWU018_ApplyEventRefusesPlayerEdgeAndInactiveLayer()
        {
            MemoryStore store = new MemoryStore();
            store.GetOrCreateEdge(Manager, ContactA, Layers(RelationshipLayer.Affinity)); // Trust inactive.

            Assert.Throws<ArgumentException>(
                () => store.ApplyEvent(Manager, ContactA, RelationshipLayer.PlayerEdge, 0.4f, 0.3f),
                "T-LW-U-018: PlayerEdge is a read-only vol-2 mirror (FR-LW-004)");
            Assert.Throws<InvalidOperationException>(
                () => store.ApplyEvent(Manager, ContactA, RelationshipLayer.Trust, 0.4f, 0.3f),
                "T-LW-U-018: inactive layers are excluded from updates (F6)");

            // The §3.1 worked example through the store on the active layer.
            store.ApplyEvent(Manager, ContactA, RelationshipLayer.Affinity, 0.4f, 0.3f); // 0 → 0.12
            store.TryGetEdge(Manager, ContactA, out RelationshipEdge edge);
            Assert.That(edge.Affinity, Is.EqualTo(0.12f).Within(1e-5f), "x' = x + v·δ·(1−x)");
        }

        [Test]
        public void ApplyEvent_NaNGate_FailsClosed()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            Assert.Throws<ArgumentOutOfRangeException>(
                () => store.ApplyEvent(Manager, ContactA, RelationshipLayer.Trust, float.NaN, 0.3f));
            Assert.Throws<ArgumentOutOfRangeException>(
                () => store.ApplyEvent(Manager, ContactA, RelationshipLayer.Trust, 0.4f, float.NaN));
        }

        // ── Canonical iteration order (FR-LW-021; T-LW-DET-002) ─────────────────────────────

        [Test]
        public void TLWDET002_EdgeIterationIsCanonicalKeyOrder_NotInsertionOrder()
        {
            MemoryStore store = new MemoryStore();
            store.GetOrCreateEdge(5, 9, 0);
            store.GetOrCreateEdge(0, 3, 0);
            store.GetOrCreateEdge(5, 1, 0);
            store.GetOrCreateEdge(0, 7, 0);

            (int, int)[] expected = { (0, 3), (0, 7), (5, 1), (5, 9) };
            Assert.AreEqual(expected.Length, store.EdgeCount);
            for (int i = 0; i < expected.Length; i++)
            {
                RelationshipEdge e = store.GetEdgeAt(i);
                Assert.AreEqual(expected[i], (e.FromId, e.ToId),
                    "T-LW-DET-002: canonical (FromId, ToId) order, not insertion order (FR-LW-021)");
            }
        }

        // ── ColdStore: demotion / rehydration (§3.5; T-LW-I-011..014, T-LW-FAIL-005) ────────

        private static MemoryStore StoreWithHistory(out uint lastEpisodeId)
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            store.ApplyEvent(Manager, ContactA, RelationshipLayer.Affinity, 0.8f, 0.5f); // 0.40
            store.ApplyEvent(Manager, ContactA, RelationshipLayer.Trust, 0.4f, 0.5f);    // 0.20
            lastEpisodeId = 0u;
            for (int i = 0; i < 8; i++)
            {
                lastEpisodeId = store.RecordEpisode(Manager, ContactA, EventKind.TransferRumour, (uint)i, 0);
                store.DecayEpisodeSalience(LivingWorldConstants.SALIENCE_DECAY_RATE);
            }
            return store;
        }

        [Test]
        public void TLWI011_DemotionRetainsTopNBySalience()
        {
            MemoryStore store = StoreWithHistory(out uint lastId);
            RelationshipEdge edge = store.RemoveEdge(Manager, ContactA);
            ColdSummary summary = ColdStore.Compress(edge, LivingWorldConstants.COLD_SUMMARY_RETAINED_EPISODES);

            Assert.AreEqual(ContactA, summary.EntityId);
            Assert.AreEqual(LivingWorldConstants.COLD_SUMMARY_RETAINED_EPISODES, summary.RetainedEpisodes.Length);
            // Later episodes decayed fewer times ⇒ highest salience ⇒ the newest N survive.
            for (int i = 0; i < summary.RetainedEpisodes.Length; i++)
            {
                Assert.Greater(summary.RetainedEpisodes[i].EpisodeId,
                    lastId - (uint)LivingWorldConstants.COLD_SUMMARY_RETAINED_EPISODES,
                    "T-LW-I-011: top-N salient episodes retained (§3.5)");
            }
        }

        [Test]
        public void TLWI012_RehydrationRoundTrip_RetainedFieldsEqual()
        {
            // T-LW-FAIL-005 / F5: demote → rehydrate yields an edge equal on all retained fields.
            MemoryStore store = StoreWithHistory(out _);
            RelationshipEdge live = store.RemoveEdge(Manager, ContactA);
            ColdSummary summary = ColdStore.Compress(live, LivingWorldConstants.COLD_SUMMARY_RETAINED_EPISODES);

            ColdStore cold = new ColdStore();
            cold.Add(summary);
            Assert.IsTrue(cold.TryTake(ContactA, out ColdSummary taken));
            RelationshipEdge back = ColdStore.Rehydrate(taken, Manager);

            Assert.AreEqual(live.FromId, back.FromId);
            Assert.AreEqual(live.ToId, back.ToId);
            Assert.AreEqual(live.ActiveLayers, back.ActiveLayers);
            Assert.AreEqual(live.NextEpisodeId, back.NextEpisodeId, "episodeId counter survives the cycle (FR-LW-009)");
            Assert.AreEqual(summary.RetainedEpisodes.Length, back.Memory.Length);
            for (int i = 0; i < back.Memory.Length; i++)
            {
                MemoryEpisode expected = summary.RetainedEpisodes[i];
                MemoryEpisode actual = back.Memory[i];
                Assert.AreEqual(expected.EpisodeId, actual.EpisodeId, "T-LW-I-012 / T-LW-FAIL-005: retained-fields equality");
                Assert.AreEqual(expected.Kind, actual.Kind);
                Assert.AreEqual(expected.Salience, actual.Salience);
                Assert.AreEqual(expected.WorldTick, actual.WorldTick);
                Assert.AreEqual(expected.ManagerChoiceId, actual.ManagerChoiceId);
            }
        }

        [Test]
        public void TLWI013_RehydratedEdgeResumesEpisodeIdFromHighWaterMark()
        {
            MemoryStore store = StoreWithHistory(out uint lastId);
            RelationshipEdge live = store.RemoveEdge(Manager, ContactA);
            ColdSummary summary = ColdStore.Compress(live, LivingWorldConstants.COLD_SUMMARY_RETAINED_EPISODES);
            RelationshipEdge back = ColdStore.Rehydrate(summary, Manager);
            store.InsertEdge(back);

            uint nextId = store.RecordEpisode(Manager, ContactA, EventKind.ManagerDefence, 50u, 0);
            Assert.AreEqual(lastId + 1u, nextId,
                "T-LW-I-013: counter resumes from NextEpisodeId, never max(retained)+1 (FR-LW-009 / §3.5)");
        }

        [Test]
        public void TLWI014_DoubleDemotionAndDuplicatePromotionFailLoud()
        {
            ColdStore cold = new ColdStore();
            ColdSummary summary = default;
            summary.EntityId = ContactA;
            summary.RetainedEpisodes = Array.Empty<MemoryEpisode>();
            cold.Add(summary);
            Assert.Throws<InvalidOperationException>(() => cold.Add(summary),
                "T-LW-I-014: double demotion must not duplicate state (FR-LW-025)");

            MemoryStore store = StoreWithEdge(Manager, ContactA);
            RelationshipEdge dup = ColdStore.Rehydrate(summary, Manager);
            Assert.Throws<InvalidOperationException>(() => store.InsertEdge(dup),
                "T-LW-I-014: promotion onto a live edge must not duplicate state (FR-LW-025)");
        }

        // ── AR-1 regression locks ───────────────────────────────────────────────────────────

        private static bool EpisodePresent(MemoryStore store, int from, int to, uint episodeId)
        {
            store.TryGetEdge(from, to, out RelationshipEdge edge);
            for (int i = 0; i < edge.Memory.Length; i++)
            {
                if (edge.Memory[i].EpisodeId == episodeId)
                {
                    return true;
                }
            }
            return false;
        }

        [Test]
        public void AR1M1_RefCountedPin_StaysExemptUntilLastArcUnpins()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            uint pinned = store.RecordEpisode(Manager, ContactA, EventKind.PressConferenceTrap, 0u, 0);
            store.PinEpisode(Manager, ContactA, pinned); // Arc A
            store.PinEpisode(Manager, ContactA, pinned); // Arc B — same episode
            store.DecayEpisodeSalience(0.5f);            // Make it the salience floor.
            for (int i = 0; i < LivingWorldConstants.MEMORY_BUFFER_DEPTH; i++)
            {
                store.RecordEpisode(Manager, ContactA, EventKind.Benching, (uint)(i + 1), 0);
            }

            store.UnpinEpisode(Manager, ContactA, pinned); // Arc A resolves.
            Assert.IsTrue(store.IsEpisodePinned(Manager, ContactA, pinned),
                "AR-1 M-1: Arc B's hold must survive Arc A's unpin (FR-LW-018)");
            store.RecordEpisode(Manager, ContactA, EventKind.Benching, 20u, 0);
            Assert.IsTrue(EpisodePresent(store, Manager, ContactA, pinned),
                "AR-1 M-1: still-held episode exempt from eviction");

            store.UnpinEpisode(Manager, ContactA, pinned); // Arc B resolves — last hold.
            Assert.IsFalse(store.IsEpisodePinned(Manager, ContactA, pinned));
            store.RecordEpisode(Manager, ContactA, EventKind.Benching, 21u, 0);
            Assert.IsFalse(EpisodePresent(store, Manager, ContactA, pinned),
                "AR-1 M-1: fully-released episode evicts as the salience floor");
        }

        [Test]
        public void AR1M2_RemoveEdgeWithArcPinnedEpisode_FailsLoud()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            uint pinned = store.RecordEpisode(Manager, ContactA, EventKind.TransferRumour, 0u, 0);
            store.PinEpisode(Manager, ContactA, pinned);

            Assert.Throws<InvalidOperationException>(() => store.RemoveEdge(Manager, ContactA),
                "AR-1 M-2: §3.5 demotion must skip a contact holding an arc-pinned episode (F1)");

            store.UnpinEpisode(Manager, ContactA, pinned);
            RelationshipEdge edge = store.RemoveEdge(Manager, ContactA);
            Assert.AreEqual(ContactA, edge.ToId, "AR-1 M-2: removal legal once the arc resolves");
        }

        [Test]
        public void AR1L1_GetOrCreateEdgeWithConflictingMask_FailsLoud()
        {
            MemoryStore store = new MemoryStore();
            store.GetOrCreateEdge(Manager, ContactA, Layers(RelationshipLayer.Affinity));
            Assert.DoesNotThrow(() => store.GetOrCreateEdge(Manager, ContactA, Layers(RelationshipLayer.Affinity)),
                "same mask is idempotent");
            Assert.Throws<InvalidOperationException>(
                () => store.GetOrCreateEdge(Manager, ContactA, Layers(RelationshipLayer.Affinity, RelationshipLayer.Trust)),
                "AR-1 L-1: a conflicting mask must not be silently ignored");
        }

        [Test]
        public void AR1L2_InsertEdgeWithActiveLayerEscaping01_FailsLoud()
        {
            MemoryStore store = new MemoryStore();
            RelationshipEdge bad = default;
            bad.FromId = Manager;
            bad.ToId = ContactA;
            bad.ActiveLayers = Layers(RelationshipLayer.Affinity);
            bad.Memory = Array.Empty<MemoryEpisode>();

            bad.Affinity = float.NaN;
            RelationshipEdge nanEdge = bad;
            Assert.Throws<ArgumentException>(() => store.InsertEdge(nanEdge), "AR-1 L-2: NaN active layer (F6)");

            bad.Affinity = 1.5f;
            RelationshipEdge rangeEdge = bad;
            Assert.Throws<ArgumentException>(() => store.InsertEdge(rangeEdge), "AR-1 L-2: active layer > 1 (F6)");
        }

        [Test]
        public void AR1L3_ColdStoreAddIncoherentSummary_FailsLoud()
        {
            ColdStore cold = new ColdStore();
            ColdSummary summary = default;
            summary.EntityId = ContactA;
            summary.NetRelationship = 0.5f;
            summary.NextEpisodeId = 2u;
            summary.RetainedEpisodes = new[] { new MemoryEpisode(5u, EventKind.None, 0.5f, 0u, 0) };
            Assert.Throws<ArgumentException>(() => cold.Add(summary),
                "AR-1 L-3: retained id >= NextEpisodeId would break FR-LW-009 on rehydrate");

            summary.NextEpisodeId = 6u;
            summary.NetRelationship = float.NaN;
            ColdSummary nanSummary = summary;
            Assert.Throws<ArgumentException>(() => cold.Add(nanSummary),
                "AR-1 L-3: NaN NetRelationship fails closed");
        }

        // ── WorldLoop (§4.2; FR-LW-034 / T-LW-DET-007 subset) ──────────────────────────────

        [Test]
        public void WorldLoop_RunsDecayPhaseEachTick()
        {
            WorldClock clock = new WorldClock();
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            store.RecordEpisode(Manager, ContactA, EventKind.Benching, 0u, 0);
            WorldLoop loop = new WorldLoop(clock, store);

            loop.RunWorldTick();
            store.TryGetEdge(Manager, ContactA, out RelationshipEdge edge);
            float expected = LivingWorldConstants.SALIENCE_INITIAL * (1f - LivingWorldConstants.SALIENCE_DECAY_RATE);
            Assert.That(edge.Memory[0].Salience, Is.EqualTo(expected).Within(1e-6f),
                "§4.2 phase 3 runs once per world tick");
            Assert.AreEqual(1u, loop.CurrentWorldTick);
        }

        [Test]
        public void TLWDET007_EmptyWorldTickIsAdditiveIdentity()
        {
            // FR-LW-034 subset assertable now: a world with no edges/episodes/arcs ticks without
            // touching anything — only the calendar advances.
            WorldClock clock = new WorldClock();
            MemoryStore store = new MemoryStore();
            WorldLoop loop = new WorldLoop(clock, store);
            for (int i = 0; i < 30; i++)
            {
                loop.RunWorldTick();
            }
            Assert.AreEqual(0, store.EdgeCount, "T-LW-DET-007: additive-only identity — nothing is created");
            Assert.AreEqual(30u, loop.CurrentWorldTick);
        }

        [Test]
        public void WorldLoop_NullDependenciesFailLoud()
        {
            Assert.Throws<ArgumentNullException>(() => new WorldLoop(null, new MemoryStore()));
            Assert.Throws<ArgumentNullException>(() => new WorldLoop(new WorldClock(), null));
        }

        // ── Two-run determinism (FR-LW-021/022 subset; T-LW-DET-001 precursor) ──────────────

        [Test]
        public void TwoIdenticallyDrivenWorlds_AreFieldIdentical()
        {
            MemoryStore[] stores = { new MemoryStore(), new MemoryStore() };
            WorldLoop[] loops = new WorldLoop[2];
            for (int r = 0; r < 2; r++)
            {
                loops[r] = new WorldLoop(new WorldClock(), stores[r]);
                stores[r].GetOrCreateEdge(Manager, ContactA, Layers(RelationshipLayer.Affinity, RelationshipLayer.Trust));
                stores[r].GetOrCreateEdge(Manager, ContactB, Layers(RelationshipLayer.Affinity));
                for (int day = 0; day < 40; day++)
                {
                    if (day % 3 == 0)
                    {
                        stores[r].RecordEpisode(Manager, ContactA, EventKind.Benching, (uint)day, (ushort)day);
                    }
                    if (day % 7 == 0)
                    {
                        stores[r].ApplyEvent(Manager, ContactB, RelationshipLayer.Affinity, 0.2f, 0.3f);
                    }
                    loops[r].RunWorldTick();
                }
            }

            Assert.AreEqual(loops[0].CurrentWorldTick, loops[1].CurrentWorldTick);
            Assert.AreEqual(stores[0].EdgeCount, stores[1].EdgeCount);
            for (int i = 0; i < stores[0].EdgeCount; i++)
            {
                RelationshipEdge a = stores[0].GetEdgeAt(i);
                RelationshipEdge b = stores[1].GetEdgeAt(i);
                Assert.AreEqual(a.FromId, b.FromId);
                Assert.AreEqual(a.ToId, b.ToId);
                Assert.AreEqual(a.ActiveLayers, b.ActiveLayers);
                Assert.AreEqual(a.Affinity, b.Affinity, "bitwise layer equality across runs");
                Assert.AreEqual(a.Trust, b.Trust);
                Assert.AreEqual(a.NextEpisodeId, b.NextEpisodeId);
                Assert.AreEqual(a.Memory.Length, b.Memory.Length);
                for (int j = 0; j < a.Memory.Length; j++)
                {
                    Assert.AreEqual(a.Memory[j].EpisodeId, b.Memory[j].EpisodeId);
                    Assert.AreEqual(a.Memory[j].Salience, b.Memory[j].Salience, "bitwise salience equality across runs");
                    Assert.AreEqual(a.Memory[j].WorldTick, b.Memory[j].WorldTick);
                }
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial season/world-loop slice-1 suite (20 tests).           |
// | 1.1     | 2026-07-02 | —      | AR-1 regression locks (+5 tests, 25 total): M-1 ref-counted   |
// |         |            |        | pins, M-2 RemoveEdge pinned-edge refusal, L-1 mask conflict,  |
// |         |            |        | L-2 InsertEdge F6 gate, L-3 ColdStore.Add coherence gates.    |
#endregion
