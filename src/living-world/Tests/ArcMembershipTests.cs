// File:     src/living-world/Tests/ArcMembershipTests.cs
// Created:  2026-07-02
// Modified: 2026-07-02 (slice-2 AR-2 regression locks)
// Author:   —
// Spec:     Living World System #22 §5 (T-LW-U-005/006, T-LW-I-002/003, T-LW-FAIL-001,
//           T-LW-DET-002/007 subsets), Testing Strategy #19 §3.1.4, Code Standards #20
// Purpose:  Season/world-loop slice-2 tests: ArcEngine spawn/pin/resolve/expiry (§3.4, FR-LW-014/018)
//           and ActiveSetMembership entry/LRU-demotion/promotion (§3.5, FR-LW-023/025), including the
//           WorldLoop phase-4/phase-6 wiring and two-run determinism.

using System;

using NUnit.Framework;

using TacticalDirector.LivingWorld;

namespace TacticalDirector.LivingWorld.Tests
{
    [TestFixture]
    public class ArcMembershipTests
    {
        private const int Manager = 0;
        private const int ContactA = 7;

        private static byte OwnedLayers()
            => (byte)((1 << (int)RelationshipLayer.Affinity) | (1 << (int)RelationshipLayer.Trust));

        private static SpawnCause Cause(ushort triggerId, uint worldTick)
            => new SpawnCause(triggerId, Array.Empty<SpawnCause.Input>(), 0UL, worldTick);

        private static MemoryStore StoreWithEdge(int from, int to)
        {
            MemoryStore store = new MemoryStore();
            store.GetOrCreateEdge(from, to, OwnedLayers());
            return store;
        }

        // ── ArcEngine (§3.4 / FR-LW-014/016/018) ────────────────────────────────────────────

        [Test]
        public void SpawnArc_PinsSourceEpisodes_AndCapturesProvenance()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            uint e0 = store.RecordEpisode(Manager, ContactA, EventKind.PressConferenceTrap, 10u, 1);
            uint e1 = store.RecordEpisode(Manager, ContactA, EventKind.ManagerCriticism, 11u, 2);
            ArcEngine engine = new ArcEngine(store);

            int idx = engine.SpawnArc(
                ArcKind.MediaVendetta,
                Cause(42, 11u),
                new[] { new Arc.PinnedEpisode(Manager, ContactA, e0), new Arc.PinnedEpisode(Manager, ContactA, e1) },
                spawnTick: 11u,
                maxLifetimeDays: 30u);

            Assert.AreEqual(1, engine.ArcCount);
            Assert.IsTrue(store.IsEpisodePinned(Manager, ContactA, e0), "FR-LW-018: source episode pinned at spawn");
            Assert.IsTrue(store.IsEpisodePinned(Manager, ContactA, e1), "FR-LW-018: source episode pinned at spawn");
            Arc arc = engine.GetArcAt(idx);
            Assert.AreEqual(ArcKind.MediaVendetta, arc.Kind);
            Assert.AreEqual((byte)0, arc.State, "§3.4 step 3: state0");
            Assert.AreEqual((ushort)42, arc.Cause.TriggerId, "FR-LW-016 / KD-8: provenance captured inline");
            Assert.AreEqual(11u, arc.SpawnTick);
            Assert.AreEqual(41u, arc.MaxLifetimeTick, "MaxLifetimeTick = spawnTick + maxLifetime");
        }

        [Test]
        public void SpawnArc_DanglingEpisode_FailsLoud_AndRollsBackPriorPins()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            uint e0 = store.RecordEpisode(Manager, ContactA, EventKind.Benching, 5u, 1);
            ArcEngine engine = new ArcEngine(store);

            // T-LW-FAIL-001 (F1): the second pin dangles; the first must be rolled back.
            Assert.Throws<InvalidOperationException>(() => engine.SpawnArc(
                ArcKind.DressingRoomSplit,
                Cause(1, 5u),
                new[] { new Arc.PinnedEpisode(Manager, ContactA, e0), new Arc.PinnedEpisode(Manager, ContactA, 999u) },
                5u, 10u));

            Assert.AreEqual(0, engine.ArcCount, "failed spawn leaves no arc");
            Assert.IsFalse(store.IsEpisodePinned(Manager, ContactA, e0), "atomic spawn: prior pins rolled back — no leaked hold");
        }

        [Test]
        public void SpawnArc_Validations_FailLoud()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            ArcEngine engine = new ArcEngine(store);
            Arc.PinnedEpisode[] none = Array.Empty<Arc.PinnedEpisode>();

            Assert.Throws<ArgumentOutOfRangeException>(
                () => engine.SpawnArc((ArcKind)9, Cause(1, 0u), none, 0u, 10u), "undefined ArcKind ordinal refused (AR-2 M-1 class)");
            Assert.Throws<ArgumentException>(
                () => engine.SpawnArc(ArcKind.MediaVendetta, Cause(1, 0u), null, 0u, 10u), "null pin array refused");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => engine.SpawnArc(ArcKind.MediaVendetta, Cause(1, 0u), none, 0u, 0u), "zero lifetime refused");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => engine.SpawnArc(ArcKind.MediaVendetta, Cause(1, 0u), none, 0u,
                    LivingWorldConstants.ARC_MAX_LIFETIME_DAYS + 1u), "lifetime above the §6.2 cap refused");
        }

        [Test]
        public void ResolveArc_ReleasesPins_AndRemoves()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            uint e0 = store.RecordEpisode(Manager, ContactA, EventKind.ContractSnub, 3u, 1);
            ArcEngine engine = new ArcEngine(store);
            int idx = engine.SpawnArc(ArcKind.BoardPatienceCollapse, Cause(2, 3u),
                new[] { new Arc.PinnedEpisode(Manager, ContactA, e0) }, 3u, 20u);

            engine.ResolveArc(idx);

            Assert.AreEqual(0, engine.ArcCount);
            Assert.IsFalse(store.IsEpisodePinned(Manager, ContactA, e0), "resolve releases the FR-LW-018 hold");
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.ResolveArc(0), "stale index fails loud");
        }

        [Test]
        public void SharedPin_SurvivesFirstArcResolve_ReleasesOnLast()
        {
            // Integration with the reference-counted pins (AR-1 M-1): two arcs share one episode.
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            uint e0 = store.RecordEpisode(Manager, ContactA, EventKind.PressConferenceTrap, 8u, 1);
            ArcEngine engine = new ArcEngine(store);
            Arc.PinnedEpisode[] pins = { new Arc.PinnedEpisode(Manager, ContactA, e0) };
            engine.SpawnArc(ArcKind.MediaVendetta, Cause(3, 8u), pins, 8u, 10u);
            engine.SpawnArc(ArcKind.BoardPatienceCollapse, Cause(4, 8u), pins, 8u, 10u);

            engine.ResolveArc(0);
            Assert.IsTrue(store.IsEpisodePinned(Manager, ContactA, e0), "second arc still holds the episode");
            engine.ResolveArc(0);
            Assert.IsFalse(store.IsEpisodePinned(Manager, ContactA, e0), "last release makes it evictable");
        }

        [Test]
        public void Update_ExpiresArcsPastMaxLifetime_Only()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            uint e0 = store.RecordEpisode(Manager, ContactA, EventKind.Benching, 10u, 1);
            ArcEngine engine = new ArcEngine(store);
            engine.SpawnArc(ArcKind.WonderkidVsVeteran, Cause(5, 10u),
                new[] { new Arc.PinnedEpisode(Manager, ContactA, e0) }, 10u, 5u); // MaxLifetimeTick = 15

            Assert.AreEqual(0, engine.Update(15u), "worldTick == MaxLifetimeTick is still live (IsExpired is strict >)");
            Assert.AreEqual(1, engine.ArcCount);
            Assert.AreEqual(1, engine.Update(16u), "§6.2: no arc stays live past its bound");
            Assert.AreEqual(0, engine.ArcCount);
            Assert.IsFalse(store.IsEpisodePinned(Manager, ContactA, e0), "expiry releases the pins (FR-LW-014)");
        }

        [Test]
        public void AdvanceState_SetsState_AndBoundsCheck()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            ArcEngine engine = new ArcEngine(store);
            int idx = engine.SpawnArc(ArcKind.DressingRoomSplit, Cause(6, 0u), Array.Empty<Arc.PinnedEpisode>(), 0u, 10u);

            engine.AdvanceState(idx, 2);
            Assert.AreEqual((byte)2, engine.GetArcAt(idx).State, "§3.4 lifecycle state advanced");
            Assert.Throws<ArgumentOutOfRangeException>(() => engine.AdvanceState(5, 1));
        }

        // ── ActiveSetMembership (§3.5 / FR-LW-023/025) ──────────────────────────────────────

        private static ActiveSetMembership Membership(out MemoryStore memory, out ColdStore cold)
        {
            memory = new MemoryStore();
            cold = new ColdStore();
            return new ActiveSetMembership(Manager, memory, cold);
        }

        /// <summary>Fills the set with <c>count</c> external contacts ids base..base+count−1 at ascending ticks.</summary>
        private static void FillExternals(ActiveSetMembership set, int baseId, int count, uint baseTick)
        {
            for (int i = 0; i < count; i++)
            {
                set.RecordInteraction(baseId + i, false, OwnedLayers(), EventKind.TransferRumour, baseTick + (uint)i, 0);
            }
        }

        [Test]
        public void RecordInteraction_FirstContact_EntersActiveSet()
        {
            ActiveSetMembership set = Membership(out MemoryStore memory, out _);

            uint episodeId = set.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.PressConferenceTrap, 4u, 3);

            Assert.AreEqual(0u, episodeId, "first episode on a fresh edge");
            Assert.IsTrue(set.IsActive(ContactA), "§3.5: entry on first interaction");
            Assert.AreEqual(1, set.ExternalCount);
            Assert.IsTrue(memory.TryGetEdge(Manager, ContactA, out RelationshipEdge edge));
            Assert.AreEqual(1, edge.Memory.Length, "the interaction's episode is recorded");
        }

        [Test]
        public void RecordInteraction_Repeat_AppendsEpisode_NoDuplicateMember()
        {
            ActiveSetMembership set = Membership(out _, out _);
            set.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.ManagerDefence, 4u, 0);

            uint second = set.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.ManagerCriticism, 9u, 0);

            Assert.AreEqual(1u, second, "FR-LW-009: monotonic per-edge episodeId");
            Assert.AreEqual(1, set.ActiveCount, "no duplicate membership");
        }

        [Test]
        public void RecordInteraction_ClassFlipOrMaskConflict_FailsLoud()
        {
            ActiveSetMembership set = Membership(out _, out _);
            set.RecordInteraction(ContactA, true, OwnedLayers(), EventKind.Benching, 1u, 0);

            Assert.Throws<InvalidOperationException>(
                () => set.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.Benching, 2u, 0),
                "own-club exit must go through Depart");
            Assert.Throws<InvalidOperationException>(
                () => set.RecordInteraction(ContactA, true, (byte)(1 << (int)RelationshipLayer.Affinity), EventKind.Benching, 2u, 0),
                "ActiveLayers conflict with the live edge fails loud");
        }

        [Test]
        public void ExternalCap_DemotesLeastRecentlyInteracted_ToColdStore()
        {
            ActiveSetMembership set = Membership(out MemoryStore memory, out ColdStore cold);
            int max = LivingWorldConstants.ACTIVE_SET_EXTERNAL_CONTACTS_MAX;
            FillExternals(set, 1000, max, 10u); // 1000 is the oldest (tick 10)

            set.RecordInteraction(2000, false, OwnedLayers(), EventKind.TransferRumour, 500u, 0);

            Assert.AreEqual(max, set.ExternalCount, "FR-LW-023: cap holds after demotion");
            Assert.IsFalse(set.IsActive(1000), "LRU (oldest max-episode worldTick) demoted");
            Assert.IsFalse(memory.TryGetEdge(Manager, 1000, out _), "demoted edge left the deep tier");
            Assert.AreEqual(1, cold.Count, "…and entered the cold store (FR-LW-025)");
            Assert.AreEqual(1000, cold.GetAt(0).EntityId);
            Assert.IsTrue(set.IsActive(2000), "the entering contact is never the demotion victim here (tick 500 is newest)");
        }

        [Test]
        public void ExternalCap_WorldTickTie_DemotesLowestEntityId()
        {
            ActiveSetMembership set = Membership(out _, out ColdStore cold);
            int max = LivingWorldConstants.ACTIVE_SET_EXTERNAL_CONTACTS_MAX;
            FillExternals(set, 3000, max, 5u);

            // Overwrite every contact's recency to the SAME tick so the tiebreak decides.
            for (int i = 0; i < max; i++)
            {
                set.RecordInteraction(3000 + i, false, OwnedLayers(), EventKind.TransferRumour, 90u, 0);
            }
            set.RecordInteraction(4000, false, OwnedLayers(), EventKind.TransferRumour, 90u, 0);

            Assert.IsFalse(set.IsActive(3000), "FR-LW-021: worldTick tie broken by lowest EntityId");
            Assert.AreEqual(3000, cold.GetAt(0).EntityId);
        }

        [Test]
        public void ExternalCap_SkipsArcPinnedContact_DemotesNextEligible()
        {
            ActiveSetMembership set = Membership(out MemoryStore memory, out ColdStore cold);
            int max = LivingWorldConstants.ACTIVE_SET_EXTERNAL_CONTACTS_MAX;
            FillExternals(set, 1000, max, 10u);
            memory.PinEpisode(Manager, 1000, 0u); // the LRU contact holds an arc-pinned episode

            set.RecordInteraction(2000, false, OwnedLayers(), EventKind.TransferRumour, 500u, 0);

            Assert.IsTrue(set.IsActive(1000), "§3.5: demotion skips a live-arc contact (would orphan the arc, F1)");
            Assert.IsFalse(set.IsActive(1001), "the next-least-recent eligible contact demotes instead");
            Assert.AreEqual(1001, cold.GetAt(0).EntityId);
        }

        [Test]
        public void ExternalCap_OwnClubNeverLruDemoted()
        {
            ActiveSetMembership set = Membership(out _, out ColdStore cold);
            int max = LivingWorldConstants.ACTIVE_SET_EXTERNAL_CONTACTS_MAX;
            set.RecordInteraction(5, true, OwnedLayers(), EventKind.Benching, 1u, 0); // own-club, oldest of all
            FillExternals(set, 1000, max, 10u);

            set.RecordInteraction(2000, false, OwnedLayers(), EventKind.TransferRumour, 500u, 0);

            Assert.IsTrue(set.IsActive(5), "§3.5: own-club members never demote while at the club");
            Assert.IsTrue(set.IsOwnClubMember(5));
            Assert.AreEqual(1000, cold.GetAt(0).EntityId, "the oldest EXTERNAL contact demoted instead");
        }

        [Test]
        public void Depart_DemotesToCold_AndReentryResumesEpisodeIds()
        {
            ActiveSetMembership set = Membership(out MemoryStore memory, out ColdStore cold);
            set.RecordInteraction(ContactA, true, OwnedLayers(), EventKind.Benching, 1u, 0);
            set.RecordInteraction(ContactA, true, OwnedLayers(), EventKind.ManagerCriticism, 2u, 0);
            set.RecordInteraction(ContactA, true, OwnedLayers(), EventKind.ContractSnub, 3u, 0);

            set.Depart(ContactA); // FR-LW-025: own-club exit preserves history in the cold tier

            Assert.IsFalse(set.IsActive(ContactA));
            Assert.AreEqual(1, cold.Count);
            Assert.AreEqual(3u, cold.GetAt(0).NextEpisodeId, "high-water mark travels with the summary");

            // Re-sign: promotion rehydrates and the episodeId counter resumes (FR-LW-009).
            uint episodeId = set.RecordInteraction(ContactA, true, OwnedLayers(), EventKind.ManagerDefence, 40u, 0);

            Assert.AreEqual(3u, episodeId, "FR-LW-009: resumes from NextEpisodeId, never max(retained)+1");
            Assert.AreEqual(0, cold.Count, "summary consumed by the promotion (neither lost nor duplicated)");
            Assert.IsTrue(set.IsOwnClubMember(ContactA), "re-entry class is the caller's declaration (re-signed)");
            Assert.IsTrue(memory.TryGetEdge(Manager, ContactA, out RelationshipEdge edge));
            Assert.AreEqual(4, edge.Memory.Length, "3 retained episodes + the re-entry interaction");
        }

        [Test]
        public void Depart_PinnedEdge_DefersDemotion_AsExternal_UntilArcResolves()
        {
            ActiveSetMembership set = Membership(out MemoryStore memory, out ColdStore cold);
            uint e0 = set.RecordInteraction(5, true, OwnedLayers(), EventKind.PressConferenceTrap, 1u, 0);
            memory.PinEpisode(Manager, 5, e0); // a live arc references the departing member's episode

            set.Depart(5);

            Assert.IsTrue(set.IsActive(5), "pinned departure defers: demoting now would orphan the arc (F1)");
            Assert.IsFalse(set.IsOwnClubMember(5), "…but the at-club exemption is gone");
            Assert.AreEqual(1, set.ExternalCount);

            int max = LivingWorldConstants.ACTIVE_SET_EXTERNAL_CONTACTS_MAX;
            FillExternals(set, 1000, max, 10u); // pushes the population over the cap; entity 5 (tick 1) is LRU but pinned
            Assert.IsTrue(set.IsActive(5), "still skipped while pinned");
            Assert.AreEqual(1000, cold.GetAt(0).EntityId, "next-eligible demoted in its place");

            memory.UnpinEpisode(Manager, 5, e0); // the arc resolves
            set.RecordInteraction(2000, false, OwnedLayers(), EventKind.TransferRumour, 500u, 0);
            Assert.IsFalse(set.IsActive(5), "after the arc resolves, the ex-member demotes via the normal LRU path");
        }

        [Test]
        public void Depart_NonOwnClub_FailsLoud()
        {
            ActiveSetMembership set = Membership(out _, out _);
            set.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.TransferRumour, 1u, 0);

            Assert.Throws<InvalidOperationException>(() => set.Depart(ContactA), "Depart is the own-club exit path");
            Assert.Throws<InvalidOperationException>(() => set.Depart(999), "unknown contact fails loud");
        }

        // ── WorldLoop phase wiring (§4.2 phases 4 + 6) ──────────────────────────────────────

        [Test]
        public void WorldLoop_Phase4_ExpiresArcs_Phase6_EnforcesCap()
        {
            MemoryStore memory = new MemoryStore();
            ColdStore cold = new ColdStore();
            ActiveSetMembership set = new ActiveSetMembership(Manager, memory, cold);
            ArcEngine arcs = new ArcEngine(memory);
            WorldClock clock = new WorldClock();
            WorldLoop loop = new WorldLoop(clock, memory, arcs, set);

            uint e0 = set.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.Benching, 0u, 0);
            arcs.SpawnArc(ArcKind.DressingRoomSplit, Cause(1, 0u),
                new[] { new Arc.PinnedEpisode(Manager, ContactA, e0) }, 0u, 1u); // MaxLifetimeTick = 1

            loop.RunWorldTick(); // day 1 — arc still live
            Assert.AreEqual(1, arcs.ArcCount);
            loop.RunWorldTick(); // day 2 — past the bound
            Assert.AreEqual(0, arcs.ArcCount, "phase 4 runs the §6.2 expiry sweep");
            Assert.IsFalse(memory.IsEpisodePinned(Manager, ContactA, e0));
            Assert.IsTrue(set.IsActive(ContactA), "under-cap membership untouched by phase 6");
        }

        // ── AR-1 regression locks ───────────────────────────────────────────────────────────

        [Test]
        public void AR1M1_MutatingCallerPinArrayAfterSpawn_DoesNotDesyncResolve()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            uint e0 = store.RecordEpisode(Manager, ContactA, EventKind.Benching, 1u, 0);
            uint e1 = store.RecordEpisode(Manager, ContactA, EventKind.ContractSnub, 2u, 0);
            ArcEngine engine = new ArcEngine(store);
            Arc.PinnedEpisode[] callerArray = { new Arc.PinnedEpisode(Manager, ContactA, e0) };
            int idx = engine.SpawnArc(ArcKind.MediaVendetta, Cause(1, 2u), callerArray, 2u, 10u);

            callerArray[0] = new Arc.PinnedEpisode(Manager, ContactA, e1); // hostile post-spawn mutation

            engine.ResolveArc(idx); // must release the hold it TOOK (e0), not the mutated key (e1)
            Assert.IsFalse(store.IsEpisodePinned(Manager, ContactA, e0), "AR-1 M-1: resolve unpins the spawn-time snapshot");
            Assert.IsFalse(store.IsEpisodePinned(Manager, ContactA, e1), "e1 was never pinned — and no throw on a phantom key");
        }

        [Test]
        public void AR1M2_PromotionMaskConflict_FailsLoud_WithoutStrandingTheSummary()
        {
            ActiveSetMembership set = Membership(out _, out ColdStore cold);
            set.RecordInteraction(ContactA, true, OwnedLayers(), EventKind.Benching, 1u, 0);
            set.Depart(ContactA);
            Assert.AreEqual(1, cold.Count);

            byte conflicting = (byte)(1 << (int)RelationshipLayer.Affinity);
            Assert.Throws<InvalidOperationException>(
                () => set.RecordInteraction(ContactA, false, conflicting, EventKind.TransferRumour, 5u, 0),
                "AR-1 M-2: mask conflicting with the cold summary fails loud");
            Assert.AreEqual(1, cold.Count, "checked against the PEEK — the summary is not stranded (FR-LW-025)");
            Assert.IsFalse(set.IsActive(ContactA), "the failed entry left no half-promoted member");

            uint episodeId = set.RecordInteraction(ContactA, false, OwnedLayers(), EventKind.TransferRumour, 5u, 0);
            Assert.AreEqual(1u, episodeId, "the correct mask still promotes, resuming episodeIds (FR-LW-009)");
            Assert.AreEqual(0, cold.Count);
        }

        [Test]
        public void AR1L1_SpawnTickLifetimeOverflow_FailsLoud()
        {
            MemoryStore store = StoreWithEdge(Manager, ContactA);
            ArcEngine engine = new ArcEngine(store);

            Assert.Throws<ArgumentOutOfRangeException>(
                () => engine.SpawnArc(ArcKind.MediaVendetta, Cause(1, 0u), Array.Empty<Arc.PinnedEpisode>(),
                    uint.MaxValue - 1u, 10u),
                "AR-1 L-1: calendar overflow refused instead of wrapping into an instantly expired arc");
        }

        // ── AR-2 regression locks ───────────────────────────────────────────────────────────

        [Test]
        public void AR2M1_ColdStoreAdd_RefusesUndefinedActiveLayersMaskBit()
        {
            ColdStore cold = new ColdStore();
            ColdSummary summary = default;
            summary.EntityId = ContactA;
            summary.ActiveLayers = 0b0010_0000; // no defined RelationshipLayer at bit 5
            summary.RetainedEpisodes = Array.Empty<MemoryEpisode>();

            Assert.Throws<ArgumentException>(() => cold.Add(summary),
                "AR-2 M-1: an undefined mask bit must be refused at Add — accepted, it strands the summary at the post-take InsertEdge mask gate (FR-LW-025)");
        }

        [Test]
        public void AR2M1_RecordInteraction_SelfContact_FailsLoud_BeforeAnyTake()
        {
            ActiveSetMembership set = Membership(out _, out ColdStore cold);
            // Craft the hazard: a cold summary claiming the manager's own id (only reachable via a
            // direct Add — the managed demotion path can never produce a self-edge).
            ColdSummary summary = default;
            summary.EntityId = Manager;
            summary.ActiveLayers = OwnedLayers();
            summary.RetainedEpisodes = Array.Empty<MemoryEpisode>();
            cold.Add(summary);

            Assert.Throws<ArgumentException>(
                () => set.RecordInteraction(Manager, false, OwnedLayers(), EventKind.None, 1u, 0),
                "AR-2 M-1: self-contact refused up front");
            Assert.Throws<ArgumentOutOfRangeException>(
                () => set.RecordInteraction(-1, false, OwnedLayers(), EventKind.None, 1u, 0),
                "negative id refused up front");
            Assert.AreEqual(1, cold.Count, "refused BEFORE the destructive take — the summary is not stranded (FR-LW-025)");
        }

        // ── Determinism (T-LW-DET-002/007 subset) ───────────────────────────────────────────

        [Test]
        public void TwoRuns_SameOperations_FieldIdenticalWorldState()
        {
            static (MemoryStore memory, ColdStore cold, ActiveSetMembership set, ArcEngine arcs) Run()
            {
                MemoryStore memory = new MemoryStore();
                ColdStore cold = new ColdStore();
                ActiveSetMembership set = new ActiveSetMembership(Manager, memory, cold);
                ArcEngine arcs = new ArcEngine(memory);
                WorldClock clock = new WorldClock();
                WorldLoop loop = new WorldLoop(clock, memory, arcs, set);

                int max = LivingWorldConstants.ACTIVE_SET_EXTERNAL_CONTACTS_MAX;
                FillExternals(set, 100, max + 3, 10u); // three LRU demotions along the way
                uint pin = set.RecordInteraction(50, true, OwnedLayers(), EventKind.PressConferenceTrap, 200u, 7);
                arcs.SpawnArc(ArcKind.MediaVendetta, Cause(9, 1u),
                    new[] { new Arc.PinnedEpisode(Manager, 50, pin) }, 1u, 2u); // MaxLifetimeTick = 3
                for (int day = 0; day < 5; day++)
                {
                    loop.RunWorldTick(); // decays salience; the arc expires on day 4
                }
                return (memory, cold, set, arcs);
            }

            (MemoryStore memory, ColdStore cold, ActiveSetMembership set, ArcEngine arcs) a = Run();
            (MemoryStore memory, ColdStore cold, ActiveSetMembership set, ArcEngine arcs) b = Run();

            Assert.AreEqual(a.set.ActiveCount, b.set.ActiveCount);
            Assert.AreEqual(a.arcs.ArcCount, b.arcs.ArcCount);
            Assert.AreEqual(a.cold.Count, b.cold.Count);
            for (int i = 0; i < a.cold.Count; i++)
            {
                Assert.AreEqual(a.cold.GetAt(i).EntityId, b.cold.GetAt(i).EntityId);
                Assert.AreEqual(a.cold.GetAt(i).NextEpisodeId, b.cold.GetAt(i).NextEpisodeId);
                Assert.AreEqual(a.cold.GetAt(i).NetRelationship, b.cold.GetAt(i).NetRelationship);
            }
            Assert.AreEqual(a.memory.EdgeCount, b.memory.EdgeCount);
            for (int i = 0; i < a.memory.EdgeCount; i++)
            {
                RelationshipEdge ea = a.memory.GetEdgeAt(i);
                RelationshipEdge eb = b.memory.GetEdgeAt(i);
                Assert.AreEqual(ea.ToId, eb.ToId, "canonical edge order is run-independent (FR-LW-021)");
                Assert.AreEqual(ea.NextEpisodeId, eb.NextEpisodeId);
                Assert.AreEqual(ea.Memory.Length, eb.Memory.Length);
                for (int j = 0; j < ea.Memory.Length; j++)
                {
                    Assert.AreEqual(ea.Memory[j].EpisodeId, eb.Memory[j].EpisodeId);
                    Assert.AreEqual(ea.Memory[j].Salience, eb.Memory[j].Salience, "bitwise-equal decay across runs");
                }
            }
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial slice-2 suite: ArcEngine spawn/pin-rollback/resolve/  |
// |         |            |        | shared-pin/expiry/state (7), ActiveSetMembership entry/LRU/   |
// |         |            |        | tiebreak/pin-skip/own-club/depart/reentry (10), WorldLoop     |
// |         |            |        | phase-4/6 wiring (1), two-run determinism (1) — 19 tests.     |
// | 1.1     | 2026-07-02 | —      | AR-1 regression locks (+3, 22 total): M-1 post-spawn caller-  |
// |         |            |        | array mutation cannot desync resolve; M-2 promotion mask      |
// |         |            |        | conflict fails loud without stranding the summary; L-1        |
// |         |            |        | spawnTick+lifetime overflow refused.                          |
// | 1.2     | 2026-07-02 | —      | AR-2 regression locks (+2, 24 total): M-1 ColdStore.Add       |
// |         |            |        | refuses an undefined ActiveLayers mask bit; self-contact and  |
// |         |            |        | negative-id RecordInteraction refused up front, BEFORE any    |
// |         |            |        | destructive take (FR-LW-025).                                 |
#endregion
