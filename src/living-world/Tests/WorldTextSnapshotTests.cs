// File:     src/living-world/Tests/WorldTextSnapshotTests.cs
// Created:  2026-07-02
// Modified: 2026-07-03 (slice-3 AR-1: +L-1 count-prefix + L-2 non-finite-input locks)
// Author:   —
// Spec:     Living World System #22 §5 (T-LW-U-019..026 subset, T-LW-DET-003/004, F5 parallels),
//           Testing Strategy #19 §3.1.4, Code Standards #20
// Purpose:  Slice-3 tests: InteractionTextGenerator (§3.3 world.text determinism, slot expansion,
//           citation gate) and WorldStateSerializer (§4.6 Appendix B round-trip + fail-loud gates).

using System;

using NUnit.Framework;

using TacticalDirector.DeterministicSim;
using TacticalDirector.LivingWorld;

namespace TacticalDirector.LivingWorld.Tests
{
    [TestFixture]
    public class InteractionTextGeneratorTests
    {
        private const ulong Seed = 0xC0FFEE01UL;

        private static InteractionSlots Slots()
            => new InteractionSlots("Kade Moreno", "Halden Rovers", 2, 1);

        private static MemoryEpisode Episode(float salience, EventKind kind = EventKind.ManagerCriticism)
            => new MemoryEpisode(0u, kind, salience, 10u, 1);

        // ── T-LW-DET-003: same (intent, cursor, slots) ⇒ identical string ────────────────────

        [Test]
        public void Generate_SameSeedSameCursor_ProducesIdenticalStrings()
        {
            InteractionTextGenerator genA = new InteractionTextGenerator(new DeterministicRngService(Seed));
            InteractionTextGenerator genB = new InteractionTextGenerator(new DeterministicRngService(Seed));
            InteractionSlots slots = Slots();
            for (int i = 0; i < 3; i++)
            {
                string a = genA.Generate(InteractionIntent.MediaProvokeTitlePressure, in slots);
                string b = genB.Generate(InteractionIntent.MediaProvokeTitlePressure, in slots);
                Assert.AreEqual(a, b, "T-LW-DET-003: identical cursor + inputs must reproduce the string (call " + i + ").");
            }
        }

        [Test]
        public void Generate_ExpandsEverySlotToken()
        {
            InteractionTextGenerator gen = new InteractionTextGenerator(new DeterministicRngService(Seed));
            InteractionSlots slots = Slots();
            string text = gen.Generate(InteractionIntent.MediaProvokeTitlePressure, in slots);
            StringAssert.Contains("Kade Moreno", text);
            StringAssert.Contains("Halden Rovers", text);
            StringAssert.Contains("2-1", text);
            Assert.IsFalse(text.Contains("{"), "no unexpanded slot token may survive expansion");
        }

        [Test]
        public void Generate_EligibleCitation_AppendsEpisodeClause()
        {
            InteractionTextGenerator gen = new InteractionTextGenerator(new DeterministicRngService(Seed));
            InteractionSlots slots = new InteractionSlots("Kade Moreno", "Halden Rovers", 0, 3, Episode(0.55f));
            string text = gen.Generate(InteractionIntent.PlayerQuestionsMinutes, in slots);
            StringAssert.Contains("The public criticism still hangs over the exchange.", text);
        }

        [Test]
        public void Generate_BelowThresholdCitation_RefusedWithoutConsumingADraw()
        {
            DeterministicRngService svc = new DeterministicRngService(Seed);
            InteractionTextGenerator gen = new InteractionTextGenerator(svc);
            // 0.1 < SALIENCE_REF_THRESHOLD (0.30) — §3.2 referencing gate.
            InteractionSlots bad = new InteractionSlots("Kade Moreno", "Halden Rovers", 2, 1, Episode(0.1f));
            Assert.Throws<InvalidOperationException>(() => gen.Generate(InteractionIntent.PlayerQuestionsMinutes, in bad));

            RngStreamState state = svc.GetStreamState(gen.StreamIndex);
            Assert.AreEqual(0UL, state.RngCursor, "a refused Generate must not consume cursor");
            Assert.AreEqual(0UL, state.ActionOrdinal, "a refused Generate must not consume an action");

            // Replay parity: the next successful call equals a fresh same-seed first call.
            InteractionTextGenerator fresh = new InteractionTextGenerator(new DeterministicRngService(Seed));
            InteractionSlots slots = Slots();
            Assert.AreEqual(
                fresh.Generate(InteractionIntent.BoardSignalsConfidence, in slots),
                gen.Generate(InteractionIntent.BoardSignalsConfidence, in slots));
        }

        [Test]
        public void Generate_NaNSalienceCitation_FailsClosed()
        {
            InteractionTextGenerator gen = new InteractionTextGenerator(new DeterministicRngService(Seed));
            InteractionSlots bad = new InteractionSlots("Kade Moreno", "Halden Rovers", 2, 1, Episode(float.NaN));
            Assert.Throws<InvalidOperationException>(() => gen.Generate(InteractionIntent.PlayerQuestionsMinutes, in bad));
        }

        [Test]
        public void Generate_RefusesNoneAndOutOfRosterIntents()
        {
            InteractionTextGenerator gen = new InteractionTextGenerator(new DeterministicRngService(Seed));
            InteractionSlots slots = Slots();
            Assert.Throws<ArgumentOutOfRangeException>(() => gen.Generate(InteractionIntent.None, in slots));
            Assert.Throws<ArgumentOutOfRangeException>(() => gen.Generate((InteractionIntent)200, in slots));
        }

        [Test]
        public void Generate_RefusesDefaultValuedSlots()
        {
            InteractionTextGenerator gen = new InteractionTextGenerator(new DeterministicRngService(Seed));
            InteractionSlots malformed = default;
            Assert.Throws<ArgumentException>(() => gen.Generate(InteractionIntent.PlayerQuestionsMinutes, in malformed));
        }

        [Test]
        public void Generate_KindlessCitation_Refused()
        {
            InteractionTextGenerator gen = new InteractionTextGenerator(new DeterministicRngService(Seed));
            InteractionSlots bad = new InteractionSlots("Kade Moreno", "Halden Rovers", 2, 1, Episode(0.9f, EventKind.None));
            Assert.Throws<ArgumentOutOfRangeException>(() => gen.Generate(InteractionIntent.PlayerQuestionsMinutes, in bad));
        }

        [Test]
        public void SlotConstructor_FailsLoudOnMalformedFacts()
        {
            Assert.Throws<ArgumentException>(() => new InteractionSlots(null, "Rovers", 1, 0));
            Assert.Throws<ArgumentException>(() => new InteractionSlots("Kade", "", 1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new InteractionSlots("Kade", "Rovers", -1, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => new InteractionSlots("Kade", "Rovers", 1, -2));
        }

        [Test]
        public void Generate_AdvancesTheWorldTextCursorByExactlyOne()
        {
            DeterministicRngService svc = new DeterministicRngService(Seed);
            InteractionTextGenerator gen = new InteractionTextGenerator(svc);
            InteractionSlots slots = Slots();
            gen.Generate(InteractionIntent.MediaProvokeTitlePressure, in slots);
            RngStreamState state = svc.GetStreamState(gen.StreamIndex);
            Assert.AreEqual(1UL, state.RngCursor);
            Assert.AreEqual(1UL, state.ActionOrdinal);
        }

        // ── T-LW-DET-004: world.text draws never perturb a sibling stream ───────────────────

        [Test]
        public void TextDraws_DoNotPerturbASiblingStreamsNextValue()
        {
            // Run A: three text generations happen before the sibling stream draws.
            DeterministicRngService svcA = new DeterministicRngService(Seed);
            InteractionTextGenerator genA = new InteractionTextGenerator(svcA);
            int siblingA = svcA.RegisterStream("world.test-sibling", SubsystemOrdinals.LivingWorld, 5, 1);
            InteractionSlots slots = Slots();
            for (int i = 0; i < 3; i++)
            {
                genA.Generate(InteractionIntent.BoardSignalsConfidence, in slots);
            }
            Assert.AreEqual(0, svcA.Reserve(siblingA, 1));
            Assert.AreEqual(0, svcA.DrawReserved(siblingA, 0, out ulong drawA));
            svcA.CloseReservation(siblingA);

            // Run B: identical registration order, zero text generations.
            DeterministicRngService svcB = new DeterministicRngService(Seed);
            InteractionTextGenerator genB = new InteractionTextGenerator(svcB);
            Assert.IsNotNull(genB); // registered for stream-order parity with run A
            int siblingB = svcB.RegisterStream("world.test-sibling", SubsystemOrdinals.LivingWorld, 5, 1);
            Assert.AreEqual(0, svcB.Reserve(siblingB, 1));
            Assert.AreEqual(0, svcB.DrawReserved(siblingB, 0, out ulong drawB));
            svcB.CloseReservation(siblingB);

            Assert.AreEqual(drawB, drawA, "T-LW-DET-004: aperiodic world.text draws must not perturb sibling streams");
        }
    }

    [TestFixture]
    public class WorldStateSerializerTests
    {
        private const int Manager = 0;
        private const int ContactA = 7;
        private const int ContactB = 9;

        private static byte OwnedLayers()
            => (byte)((1 << (int)RelationshipLayer.Affinity) | (1 << (int)RelationshipLayer.Trust));

        private static void BuildWorld(out WorldClock clock, out MemoryStore memory, out ArcEngine arcs, out ColdStore cold)
        {
            clock = new WorldClock(42u);
            memory = new MemoryStore();
            memory.GetOrCreateEdge(Manager, ContactA, OwnedLayers());
            memory.GetOrCreateEdge(Manager, ContactB, OwnedLayers());
            uint ep0 = memory.RecordEpisode(Manager, ContactA, EventKind.PressConferenceTrap, 10u, 3);
            uint ep1 = memory.RecordEpisode(Manager, ContactA, EventKind.Benching, 11u, 4);
            memory.RecordEpisode(Manager, ContactB, EventKind.ManagerDefence, 12u, 5);
            memory.ApplyEvent(Manager, ContactA, RelationshipLayer.Affinity, 0.4f, 0.3f);
            memory.ApplyEvent(Manager, ContactA, RelationshipLayer.Trust, -0.2f, 0.25f);

            arcs = new ArcEngine(memory);
            SpawnCause causeA = new SpawnCause(11, new[] { new SpawnCause.Input(1, 0.5f), new SpawnCause.Input(2, -0.25f) }, 0xDEADBEEFUL, 40u);
            arcs.SpawnArc(ArcKind.MediaVendetta, in causeA,
                new[] { new Arc.PinnedEpisode(Manager, ContactA, ep0) }, 40u, 30u);
            SpawnCause causeB = new SpawnCause(12, Array.Empty<SpawnCause.Input>(), 0UL, 41u);
            int idxB = arcs.SpawnArc(ArcKind.BoardPatienceCollapse, in causeB,
                new[] { new Arc.PinnedEpisode(Manager, ContactA, ep0), new Arc.PinnedEpisode(Manager, ContactA, ep1) }, 41u, 60u);
            arcs.AdvanceState(idxB, 2);

            cold = new ColdStore();
            ColdSummary summary = default;
            summary.EntityId = 33;
            summary.ActiveLayers = OwnedLayers();
            summary.NetRelationship = 0.75f;
            summary.NextEpisodeId = 5u;
            summary.RetainedEpisodes = new[]
            {
                new MemoryEpisode(2u, EventKind.TransferRumour, 0.6f, 8u, 2),
                new MemoryEpisode(4u, EventKind.ContractSnub, 0.9f, 9u, 6),
            };
            cold.Add(in summary);
        }

        [Test]
        public void RoundTrip_RestoresEveryRetainedFieldAndReconstructsPins()
        {
            BuildWorld(out WorldClock clock, out MemoryStore memory, out ArcEngine arcs, out ColdStore cold);
            byte[] payload = WorldStateSerializer.Serialize(clock, memory, arcs, cold);
            WorldStateSerializer.Deserialize(payload, out WorldClock clock2, out MemoryStore memory2, out ArcEngine arcs2, out ColdStore cold2);

            Assert.AreEqual(42u, clock2.CurrentWorldTick);

            Assert.AreEqual(memory.EdgeCount, memory2.EdgeCount);
            for (int i = 0; i < memory.EdgeCount; i++)
            {
                RelationshipEdge a = memory.GetEdgeAt(i);
                RelationshipEdge b = memory2.GetEdgeAt(i);
                Assert.AreEqual(a.FromId, b.FromId);
                Assert.AreEqual(a.ToId, b.ToId);
                Assert.AreEqual(a.ActiveLayers, b.ActiveLayers);
                Assert.AreEqual(a.PlayerEdge, b.PlayerEdge);
                Assert.AreEqual(a.Affinity, b.Affinity);
                Assert.AreEqual(a.Trust, b.Trust);
                Assert.AreEqual(a.NextEpisodeId, b.NextEpisodeId, "FR-LW-009: the live-edge high-water mark must survive the round-trip");
                Assert.AreEqual(a.Memory.Length, b.Memory.Length);
                for (int k = 0; k < a.Memory.Length; k++)
                {
                    Assert.AreEqual(a.Memory[k].EpisodeId, b.Memory[k].EpisodeId);
                    Assert.AreEqual(a.Memory[k].Kind, b.Memory[k].Kind);
                    Assert.AreEqual(a.Memory[k].Salience, b.Memory[k].Salience);
                    Assert.AreEqual(a.Memory[k].WorldTick, b.Memory[k].WorldTick);
                    Assert.AreEqual(a.Memory[k].ManagerChoiceId, b.Memory[k].ManagerChoiceId);
                }
            }

            Assert.AreEqual(2, arcs2.ArcCount);
            for (int i = 0; i < 2; i++)
            {
                Arc a = arcs.GetArcAt(i);
                Arc b = arcs2.GetArcAt(i);
                Assert.AreEqual(a.Kind, b.Kind);
                Assert.AreEqual(a.State, b.State);
                Assert.AreEqual(a.Cause.TriggerId, b.Cause.TriggerId);
                Assert.AreEqual(a.Cause.SnapshotRef, b.Cause.SnapshotRef);
                Assert.AreEqual(a.Cause.WorldTick, b.Cause.WorldTick);
                Assert.AreEqual(a.Cause.Inputs.Length, b.Cause.Inputs.Length);
                for (int k = 0; k < a.Cause.Inputs.Length; k++)
                {
                    Assert.AreEqual(a.Cause.Inputs[k].Key, b.Cause.Inputs[k].Key);
                    Assert.AreEqual(a.Cause.Inputs[k].Value, b.Cause.Inputs[k].Value);
                }
                Assert.AreEqual(a.PinnedEpisodes.Length, b.PinnedEpisodes.Length);
                Assert.AreEqual(a.SpawnTick, b.SpawnTick);
                Assert.AreEqual(a.MaxLifetimeTick, b.MaxLifetimeTick);
            }

            // FR-LW-018 refcount reconstruction: ep0 is pinned by BOTH arcs. Resolving the first
            // must leave it pinned; resolving the second releases it.
            Assert.IsTrue(memory2.IsEpisodePinned(Manager, ContactA, 0u));
            arcs2.ResolveArc(0);
            Assert.IsTrue(memory2.IsEpisodePinned(Manager, ContactA, 0u), "shared pin must survive the first arc's resolve (refcount)");
            arcs2.ResolveArc(0);
            Assert.IsFalse(memory2.IsEpisodePinned(Manager, ContactA, 0u));

            Assert.AreEqual(1, cold2.Count);
            ColdSummary s2 = cold2.GetAt(0);
            Assert.AreEqual(33, s2.EntityId);
            Assert.AreEqual(OwnedLayers(), s2.ActiveLayers);
            Assert.AreEqual(0.75f, s2.NetRelationship);
            Assert.AreEqual(5u, s2.NextEpisodeId);
            Assert.AreEqual(2, s2.RetainedEpisodes.Length);
            Assert.AreEqual(4u, s2.RetainedEpisodes[1].EpisodeId);
        }

        [Test]
        public void Serialize_IsBitwiseDeterministic_AndStableAcrossARoundTrip()
        {
            BuildWorld(out WorldClock clock, out MemoryStore memory, out ArcEngine arcs, out ColdStore cold);
            byte[] first = WorldStateSerializer.Serialize(clock, memory, arcs, cold);
            byte[] second = WorldStateSerializer.Serialize(clock, memory, arcs, cold);
            CollectionAssert.AreEqual(first, second, "same state must serialize byte-identically");

            WorldStateSerializer.Deserialize(first, out WorldClock clock2, out MemoryStore memory2, out ArcEngine arcs2, out ColdStore cold2);
            byte[] third = WorldStateSerializer.Serialize(clock2, memory2, arcs2, cold2);
            CollectionAssert.AreEqual(first, third, "serialize → deserialize → serialize must be byte-identical");
        }

        [Test]
        public void Deserialize_RefusesUnknownFormatVersion()
        {
            BuildWorld(out WorldClock clock, out MemoryStore memory, out ArcEngine arcs, out ColdStore cold);
            byte[] payload = WorldStateSerializer.Serialize(clock, memory, arcs, cold);
            payload[0] = 0xFF;
            Assert.Throws<ArgumentException>(() => WorldStateSerializer.Deserialize(payload, out _, out _, out _, out _));
        }

        [Test]
        public void Deserialize_RefusesWrongDomainTag()
        {
            BuildWorld(out WorldClock clock, out MemoryStore memory, out ArcEngine arcs, out ColdStore cold);
            byte[] payload = WorldStateSerializer.Serialize(clock, memory, arcs, cold);
            payload[2] = 0x01; // the domain-tag byte follows the u16 version
            Assert.Throws<ArgumentException>(() => WorldStateSerializer.Deserialize(payload, out _, out _, out _, out _));
        }

        [Test]
        public void Deserialize_RefusesTrailingBytes()
        {
            BuildWorld(out WorldClock clock, out MemoryStore memory, out ArcEngine arcs, out ColdStore cold);
            byte[] payload = WorldStateSerializer.Serialize(clock, memory, arcs, cold);
            byte[] padded = new byte[payload.Length + 1];
            Array.Copy(payload, padded, payload.Length);
            Assert.Throws<ArgumentException>(() => WorldStateSerializer.Deserialize(padded, out _, out _, out _, out _));
        }

        [Test]
        public void Deserialize_RefusesCorruptCountPrefix()
        {
            // L-1: the edge-count prefix sits at offset 7 (u16 version + u8 tag + u32 worldTick).
            // A negative count would desync the offset; ReadCount must fail loud instead.
            BuildWorld(out WorldClock clock, out MemoryStore memory, out ArcEngine arcs, out ColdStore cold);
            byte[] payload = WorldStateSerializer.Serialize(clock, memory, arcs, cold);
            payload[7] = 0xFF;
            payload[8] = 0xFF;
            payload[9] = 0xFF;
            payload[10] = 0xFF; // edgeCount = -1
            Assert.Throws<ArgumentException>(() => WorldStateSerializer.Deserialize(payload, out _, out _, out _, out _));

            byte[] huge = WorldStateSerializer.Serialize(clock, memory, arcs, cold);
            huge[7] = 0xFF;
            huge[8] = 0xFF;
            huge[9] = 0xFF;
            huge[10] = 0x7F; // edgeCount = 0x7FFFFFFF ≫ remaining bytes
            Assert.Throws<ArgumentException>(() => WorldStateSerializer.Deserialize(huge, out _, out _, out _, out _));
        }

        [Test]
        public void Serialize_RefusesNonFiniteInputValue()
        {
            // L-2: a NaN SpawnCause.Input.Value must not reach the digest preimage.
            WorldClock clock = new WorldClock(1u);
            MemoryStore memory = new MemoryStore();
            memory.GetOrCreateEdge(Manager, ContactA, OwnedLayers());
            uint ep = memory.RecordEpisode(Manager, ContactA, EventKind.Benching, 1u, 1);
            ArcEngine arcs = new ArcEngine(memory);
            SpawnCause bad = new SpawnCause(7, new[] { new SpawnCause.Input(1, float.NaN) }, 0UL, 1u);
            arcs.SpawnArc(ArcKind.MediaVendetta, in bad, new[] { new Arc.PinnedEpisode(Manager, ContactA, ep) }, 1u, 10u);
            ColdStore cold = new ColdStore();
            Assert.Throws<ArgumentException>(() => WorldStateSerializer.Serialize(clock, memory, arcs, cold));
        }

        [Test]
        public void Serialize_NullStores_FailLoud()
        {
            BuildWorld(out WorldClock clock, out MemoryStore memory, out ArcEngine arcs, out ColdStore cold);
            Assert.Throws<ArgumentNullException>(() => WorldStateSerializer.Serialize(null, memory, arcs, cold));
            Assert.Throws<ArgumentNullException>(() => WorldStateSerializer.Serialize(clock, null, arcs, cold));
            Assert.Throws<ArgumentNullException>(() => WorldStateSerializer.Serialize(clock, memory, null, cold));
            Assert.Throws<ArgumentNullException>(() => WorldStateSerializer.Serialize(clock, memory, arcs, null));
            Assert.Throws<ArgumentNullException>(() => WorldStateSerializer.Deserialize(null, out _, out _, out _, out _));
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial suite (slice 3): text-generator determinism (T-LW-    |
// |         |            |        | DET-003/004), slot expansion, §3.2 citation gate (incl. the   |
// |         |            |        | no-draw-on-refusal replay-parity lock); §4.6 serializer       |
// |         |            |        | round-trip field identity + shared-pin refcount               |
// |         |            |        | reconstruction + bitwise determinism + fail-loud gates.       |
// | 1.1     | 2026-07-03 | —      | Slice-3 AR-1 locks: corrupt count-prefix refusal (L-1) +      |
// |         |            |        | non-finite SpawnCause.Input.Value refusal (L-2).              |
#endregion
