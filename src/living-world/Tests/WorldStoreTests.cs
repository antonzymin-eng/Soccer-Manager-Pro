// File:     src/living-world/Tests/WorldStoreTests.cs
// Created:  2026-07-03
// Modified: 2026-08-08
// Author:   —
// Spec:     Living World System #22 §4.2/§4.6/§7.1 (KD-10), FR-LW-019/022/023, Testing Strategy #19
//           §3.1.4, Code Standards #20
// Purpose:  KD-10 season composition root tests: construction/wiring, the season loop driving the
//           §4.2 phases that have producers (decay, arc expiry), current-tick interaction stamping,
//           the composite Snapshot/Restore round-trip (four-store block + manager id + membership
//           roster), two-run determinism, and the fail-loud restore gates.

using System;

using NUnit.Framework;

using TacticalDirector.LivingWorld;

namespace TacticalDirector.LivingWorld.Tests
{
    [TestFixture]
    public class WorldStoreTests
    {
        private const int Manager = 0;
        private const byte AffinityTrust = 0b110; // Affinity (bit 1) | Trust (bit 2)

        private static SpawnCause Cause(uint worldTick)
        {
            return new SpawnCause(42, new[] { new SpawnCause.Input(1, 0.5f) }, 999UL, worldTick);
        }

        /// <summary>
        /// Builds a store exercising every serialized block: 3 live edges (2 external + 1 own-club),
        /// a live arc pinning an episode, and a departed contact compressed into cold-store. Two days
        /// advanced so the calendar and decayed salience are non-trivial.
        /// </summary>
        private static WorldStore PopulatedStore()
        {
            WorldStore s = new WorldStore(Manager);

            uint ep5 = s.RecordInteraction(5, isOwnClub: false, AffinityTrust, EventKind.ManagerCriticism, 11);
            s.Arcs.SpawnArc(ArcKind.MediaVendetta, Cause(s.CurrentWorldTick),
                new[] { new Arc.PinnedEpisode(Manager, 5, ep5) }, s.CurrentWorldTick, 30u);

            s.RecordInteraction(8, isOwnClub: true, AffinityTrust, EventKind.ManagerDefence, 0);
            s.RecordInteraction(3, isOwnClub: false, AffinityTrust, EventKind.Benching, 0);

            s.RecordInteraction(12, isOwnClub: true, AffinityTrust, EventKind.ContractSnub, 0);
            s.Membership.Depart(12); // no pinned episode ⇒ demotes to cold-store

            // Two player-triggered generations advance the owned world.text cursor so the round-trip
            // and determinism tests exercise a non-zero stream position.
            InteractionSlots slots = new InteractionSlots("Boss", "Rivals FC", 1, 2);
            s.GenerateInteractionText(InteractionIntent.MediaProvokeTitlePressure, in slots);
            s.GenerateInteractionText(InteractionIntent.BoardSignalsConfidence, in slots);

            s.AdvanceDay();
            s.AdvanceDay();
            return s;
        }

        // ── construction + wiring ───────────────────────────────────────────────────────────

        [Test]
        public void NewStore_StartsEmptyAtDayZero()
        {
            WorldStore s = new WorldStore(7);
            Assert.AreEqual(7, s.ManagerId);
            Assert.AreEqual(0u, s.CurrentWorldTick, "day 0 at world start");
            Assert.AreEqual(0, s.Memory.EdgeCount);
            Assert.AreEqual(0, s.Arcs.ArcCount);
            Assert.AreEqual(0, s.Cold.Count);
            Assert.AreEqual(0, s.Membership.MemberCount);
        }

        [Test]
        public void Ctor_NegativeManager_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WorldStore(-1));
        }

        // ── season loop drives the §4.2 phases that have producers ──────────────────────────

        [Test]
        public void AdvanceDay_AdvancesCalendarAndDecaysSalience()
        {
            WorldStore s = new WorldStore(Manager);
            s.RecordInteraction(5, isOwnClub: false, AffinityTrust, EventKind.ManagerCriticism, 0);

            float before = s.Memory.GetEdgeAt(0).Memory[0].Salience;
            s.AdvanceDay();
            Assert.AreEqual(1u, s.CurrentWorldTick, "AdvanceDay advances the calendar one day");
            float after = s.Memory.GetEdgeAt(0).Memory[0].Salience;
            Assert.Less(after, before, "phase 3: AdvanceDay decays episode salience");
        }

        [Test]
        public void AdvanceDay_ExpiresArcsPastTheirLifetime()
        {
            WorldStore s = new WorldStore(Manager);
            uint ep = s.RecordInteraction(5, isOwnClub: false, AffinityTrust, EventKind.ManagerCriticism, 0);
            s.Arcs.SpawnArc(ArcKind.MediaVendetta, Cause(0u),
                new[] { new Arc.PinnedEpisode(Manager, 5, ep) }, 0u, 2u); // MaxLifetimeTick = 2

            Assert.AreEqual(1, s.Arcs.ArcCount);
            Assert.IsTrue(s.Memory.EdgeHasPinnedEpisode(Manager, 5), "episode pinned while the arc is live");

            s.AdvanceDay(); // day 1
            s.AdvanceDay(); // day 2 — not expired (2 > 2 is false)
            Assert.AreEqual(1, s.Arcs.ArcCount, "phase 4: arc still live at its lifetime boundary");

            s.AdvanceDay(); // day 3 — expired
            Assert.AreEqual(0, s.Arcs.ArcCount, "phase 4: AdvanceDay force-resolves the expired arc");
            Assert.IsFalse(s.Memory.EdgeHasPinnedEpisode(Manager, 5), "expiry unpins the episode");
        }

        [Test]
        public void RecordInteraction_StampsCurrentWorldTick()
        {
            WorldStore s = new WorldStore(Manager);
            s.AdvanceDay();
            s.AdvanceDay(); // day 2
            s.RecordInteraction(5, isOwnClub: false, AffinityTrust, EventKind.Benching, 0);
            Assert.AreEqual(2u, s.Memory.GetEdgeAt(0).Memory[0].WorldTick,
                "the episode is stamped with the store's current calendar day, not a caller value");
        }

        // ── persistence (§4.6 / FR-LW-022) ──────────────────────────────────────────────────

        [Test]
        public void SnapshotRestore_IsFieldIdentical()
        {
            WorldStore original = PopulatedStore();
            byte[] payload = original.Snapshot();

            WorldStore restored = WorldStore.Restore(payload);

            Assert.AreEqual(original.ManagerId, restored.ManagerId);
            Assert.AreEqual(original.CurrentWorldTick, restored.CurrentWorldTick);
            Assert.AreEqual(original.Memory.EdgeCount, restored.Memory.EdgeCount);
            Assert.AreEqual(original.Arcs.ArcCount, restored.Arcs.ArcCount);
            Assert.AreEqual(original.Cold.Count, restored.Cold.Count);
            Assert.AreEqual(original.Membership.MemberCount, restored.Membership.MemberCount);
            Assert.IsTrue(restored.Membership.IsOwnClubMember(8), "own-club class survives the round-trip");
            Assert.IsTrue(restored.Membership.IsActive(3) && !restored.Membership.IsOwnClubMember(3),
                "external class survives the round-trip");
            Assert.IsTrue(restored.Memory.EdgeHasPinnedEpisode(Manager, 5),
                "SpawnArc re-takes the pin on restore (FR-LW-018 refcount reconstructed)");

            // Idempotent re-serialization is the strongest field-identity check.
            CollectionAssert.AreEqual(payload, restored.Snapshot(),
                "Restore(Snapshot()).Snapshot() reproduces the original bytes");
        }

        [Test]
        public void SnapshotRestore_EmptyStore_RoundTrips()
        {
            WorldStore empty = new WorldStore(4);
            empty.AdvanceDay(); // day 1, still no edges/arcs/cold/members
            byte[] payload = empty.Snapshot();

            WorldStore restored = WorldStore.Restore(payload);
            Assert.AreEqual(4, restored.ManagerId);
            Assert.AreEqual(1u, restored.CurrentWorldTick);
            Assert.AreEqual(0, restored.Memory.EdgeCount);
            Assert.AreEqual(0, restored.Membership.MemberCount);
            CollectionAssert.AreEqual(payload, restored.Snapshot());
        }

        [Test]
        public void RestoredStore_KeepsAdvancing()
        {
            WorldStore restored = WorldStore.Restore(PopulatedStore().Snapshot());
            uint before = restored.CurrentWorldTick;
            restored.AdvanceDay();
            Assert.AreEqual(before + 1u, restored.CurrentWorldTick, "the restored loop is fully wired");
        }

        [Test]
        public void Snapshot_IsDeterministicAcrossTwoIdenticalStores()
        {
            byte[] a = PopulatedStore().Snapshot();
            byte[] b = PopulatedStore().Snapshot();
            CollectionAssert.AreEqual(a, b, "two identically-built stores serialize byte-identically");
        }

        // ── world.text generation (§3.3) wired into the store ───────────────────────────────

        [Test]
        public void GenerateInteractionText_ExpandsSlots()
        {
            WorldStore s = new WorldStore(Manager);
            InteractionSlots slots = new InteractionSlots("Boss", "Rivals FC", 1, 2);
            string text = s.GenerateInteractionText(InteractionIntent.MediaProvokeTitlePressure, in slots);
            Assert.IsFalse(string.IsNullOrEmpty(text));
            StringAssert.Contains("Boss", text);
            StringAssert.Contains("Rivals FC", text);
            StringAssert.Contains("1-2", text);
        }

        [Test]
        public void GenerateInteractionText_ResumesDeterministicallyAfterRestore()
        {
            // PopulatedStore has already advanced the world.text cursor (2 generations). Snapshot at
            // that position, restore, and confirm the restored stream produces the SAME continuation
            // as the original — the draw keys on the saved action ordinal (§3.2.5), so a store that
            // failed to restore the cursor would diverge on the very next generation.
            WorldStore original = PopulatedStore();
            WorldStore restored = WorldStore.Restore(original.Snapshot());

            InteractionSlots slots = new InteractionSlots("Boss", "Rivals FC", 0, 3);
            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(
                    original.GenerateInteractionText(InteractionIntent.MediaProvokeTitlePressure, in slots),
                    restored.GenerateInteractionText(InteractionIntent.MediaProvokeTitlePressure, in slots),
                    "restored world.text stream resumes at the saved cursor");
            }
        }

        [Test]
        public void GenerateInteractionText_DivergesWithWorldSeed()
        {
            // The explicit-seed overload plumbs a distinct world.text stream: the same intent/slots at
            // the same (zero) cursor draw different action-ordinal-keyed values across enough seeds
            // that at least one selects a different template.
            InteractionSlots slots = new InteractionSlots("Boss", "Rivals FC", 0, 0);
            string baseline = new WorldStore(Manager, 1UL)
                .GenerateInteractionText(InteractionIntent.MediaProvokeTitlePressure, in slots);
            bool anyDifferent = false;
            for (ulong seed = 2UL; seed <= 40UL && !anyDifferent; seed++)
            {
                string other = new WorldStore(Manager, seed)
                    .GenerateInteractionText(InteractionIntent.MediaProvokeTitlePressure, in slots);
                anyDifferent = other != baseline;
            }
            Assert.IsTrue(anyDifferent, "world seed selects the world.text draw sequence");
        }

        // ── auto-cited world.text generation (§3.3 referencing) ─────────────────────────────

        // The §3.3 episode clauses (InteractionTextCorpus.EpisodeClause) the auto-cite path appends.
        private const string CriticismClause = "The public criticism still hangs over the exchange.";
        private const string BenchingClause = "The benching has not been forgotten.";

        [Test]
        public void GenerateInteractionText_AutoCite_CitesTheContactsEpisode()
        {
            WorldStore s = new WorldStore(Manager);
            s.RecordInteraction(5, isOwnClub: false, AffinityTrust, EventKind.ManagerCriticism, 11);

            string text = s.GenerateInteractionText(
                InteractionIntent.MediaProvokeTitlePressure, entityId: 5, "Boss", "Rivals FC", 1, 2);

            StringAssert.Contains("Boss", text);
            StringAssert.Contains(CriticismClause, text, "the fresh episode clears the salience gate and is cited");
        }

        [Test]
        public void GenerateInteractionText_AutoCite_BreaksTiesDeterministically()
        {
            WorldStore s = new WorldStore(Manager);
            // Both recorded on day 0 ⇒ salience 1.0 and worldTick 0 tie; the tiebreak falls through to
            // the higher episodeId (the Benching episode, allocated second).
            s.RecordInteraction(5, isOwnClub: false, AffinityTrust, EventKind.ManagerCriticism, 11);
            s.RecordInteraction(5, isOwnClub: false, AffinityTrust, EventKind.Benching, 12);

            string text = s.GenerateInteractionText(
                InteractionIntent.MediaProvokeTitlePressure, entityId: 5, "Boss", "Rivals FC", 1, 2);

            StringAssert.Contains(BenchingClause, text, "on a full tie the higher episodeId wins");
            StringAssert.DoesNotContain(CriticismClause, text);
        }

        [Test]
        public void GenerateInteractionText_AutoCite_NoEdge_OmitsCitation()
        {
            WorldStore s = new WorldStore(Manager);
            // Entity 99 has no recorded interaction ⇒ no edge ⇒ nothing citable.
            string text = s.GenerateInteractionText(
                InteractionIntent.MediaProvokeTitlePressure, entityId: 99, "Boss", "Rivals FC", 1, 2);

            StringAssert.Contains("Boss", text);
            StringAssert.DoesNotContain(CriticismClause, text);
            StringAssert.DoesNotContain(BenchingClause, text);
        }

        [Test]
        public void GenerateInteractionText_AutoCite_ResumesDeterministicallyAfterRestore()
        {
            // The cited episode is a pure function of the serialized MemoryStore and the draw resumes
            // at the saved cursor, so an auto-cite call reproduces byte-for-byte across the save boundary.
            WorldStore original = PopulatedStore();
            WorldStore restored = WorldStore.Restore(original.Snapshot());

            for (int i = 0; i < 4; i++)
            {
                Assert.AreEqual(
                    original.GenerateInteractionText(InteractionIntent.MediaProvokeTitlePressure, 5, "Boss", "Rivals FC", 0, 3),
                    restored.GenerateInteractionText(InteractionIntent.MediaProvokeTitlePressure, 5, "Boss", "Rivals FC", 0, 3),
                    "auto-cite selection + world.text stream both resume from the restored state");
            }
        }

        // ── fail-loud restore gates ─────────────────────────────────────────────────────────

        [Test]
        public void Restore_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => WorldStore.Restore(null));
        }

        [Test]
        public void Restore_WrongVersion_Throws()
        {
            byte[] p = PopulatedStore().Snapshot();
            p[0] = 0xEE;
            p[1] = 0xEE; // both bytes of the u16 version ⇒ guaranteed != WORLD_STORE_FORMAT_VERSION
            Assert.Throws<ArgumentException>(() => WorldStore.Restore(p));
        }

        [Test]
        public void Restore_WrongDomainTag_Throws()
        {
            byte[] p = PopulatedStore().Snapshot();
            p[2] = 0x00; // domain tag byte (after the u16 version); 0x00 != DOMAIN_TAG_LIVING_WORLD (0x1E)
            Assert.Throws<ArgumentException>(() => WorldStore.Restore(p));
        }

        [Test]
        public void Restore_CorruptStoreLength_Throws()
        {
            byte[] p = PopulatedStore().Snapshot();
            // Store-block length prefix (i32) sits after version(2) + tag(1) + managerId(4) = offset 7.
            p[7] = 0x7F;
            p[8] = 0x7F;
            p[9] = 0x7F;
            p[10] = 0x7F; // a large positive count exceeding the remaining payload
            Assert.Throws<ArgumentException>(() => WorldStore.Restore(p));
        }

        [Test]
        public void Restore_TrailingBytes_Throws()
        {
            byte[] p = PopulatedStore().Snapshot();
            byte[] longer = new byte[p.Length + 1];
            Array.Copy(p, longer, p.Length);
            Assert.Throws<ArgumentException>(() => WorldStore.Restore(longer));
        }

        [Test]
        public void Restore_BadMembershipFlag_Throws()
        {
            byte[] p = PopulatedStore().Snapshot();
            // The last byte is the final member's isOwnClub flag (roster is written last).
            p[p.Length - 1] = 0x02; // neither 0 nor 1
            Assert.Throws<ArgumentException>(() => WorldStore.Restore(p));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-07-03 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
