// File:     src/living-world/WorldStateSerializer.cs
// Created:  2026-07-02
// Modified: 2026-07-02
// Author:   —
// Spec:     Living World System #22 §4.6, Appendix B, FR-LW-009/022/025, Deterministic Simulation #16
//           §3.2.4.1, Code Standards #20
// Purpose:  Canonical serialization of the living-world store (clock + edges/episodes + arcs + cold
//           summaries) in the Appendix B pinned field order, via the #16 CanonicalSerializer.

using System;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// Serializes/deserializes the whole living-world value state (#22 §4.6 / FR-LW-022) as one
    /// canonical byte payload: format version, domain tag, worldTick, then edges (with their episode
    /// buffers), live arcs (with provenance + pins), and cold summaries — each block in the
    /// <b>Appendix B pinned field order</b> (order is load-bearing for the digest, ERR-022-002).
    /// All primitives go through the #16 <see cref="CanonicalSerializer"/> (§3.2.4.1 encodings;
    /// floats are raw IEEE-754 bits with −0.0 normalization, so the round-trip is bitwise).
    ///
    /// SCOPE: this is the living-world block itself. The composite-save integration — entering the
    /// canonical snapshot field set with a #16 <c>SNAPSHOT_SCHEMA_VERSION</c> bump — happens at the
    /// season-level composition root, which does not exist yet (KD-10; and FR-LW-003 forbids the
    /// match engine from referencing this assembly, so the match snapshot cannot host the block).
    /// The <c>world.text</c> RNG stream cursor is NOT part of this block (Appendix B lists none):
    /// RNG stream state is #16-owned and serialized via <c>GetStreamState</c>/<c>RestoreStream</c>
    /// at that same composition root.
    ///
    /// Deserialization rebuilds the stores through their validating seams — edges via
    /// <c>MemoryStore.InsertEdge</c>, arcs via <c>ArcEngine.SpawnArc</c> +
    /// <c>AdvanceState</c> (which re-takes every episode pin, reconstructing the FR-LW-018
    /// reference counts exactly since arcs are the only pin holders), summaries via
    /// <c>ColdStore.Add</c> — so a corrupt payload fails loud at the same gates live mutation does.
    /// NOTE: <c>SpawnArc</c> re-gates arc lifetimes against the CURRENT
    /// <c>ARC_MAX_LIFETIME_DAYS</c>; a save authored under a larger [GT] value refuses to load
    /// (fail-loud beats silent truncation — config-migration policy is owned by the Stage-1 loader).
    /// </summary>
    public static class WorldStateSerializer
    {
        /// <summary>Serializes the four stores into a new exact-size payload.</summary>
        public static byte[] Serialize(WorldClock clock, MemoryStore memory, ArcEngine arcs, ColdStore cold)
        {
            if (clock == null)
            {
                throw new ArgumentNullException(nameof(clock));
            }
            if (memory == null)
            {
                throw new ArgumentNullException(nameof(memory));
            }
            if (arcs == null)
            {
                throw new ArgumentNullException(nameof(arcs));
            }
            if (cold == null)
            {
                throw new ArgumentNullException(nameof(cold));
            }

            byte[] payload = new byte[ComputeSize(memory, arcs, cold)];
            int offset = 0;

            CanonicalSerializer.WriteU16(payload, ref offset, LivingWorldConstants.WORLD_SNAPSHOT_FORMAT_VERSION);
            CanonicalSerializer.WriteU8(payload, ref offset, LivingWorldConstants.DomainTagLivingWorld);
            CanonicalSerializer.WriteU32(payload, ref offset, clock.CurrentWorldTick);

            // Edge block — canonical (FromId, ToId) order is the store's own iteration order
            // (FR-LW-021), so the payload is a pure function of state.
            CanonicalSerializer.WriteI32(payload, ref offset, memory.EdgeCount);
            for (int i = 0; i < memory.EdgeCount; i++)
            {
                RelationshipEdge edge = memory.GetEdgeAt(i);
                CanonicalSerializer.WriteI32(payload, ref offset, edge.FromId);
                CanonicalSerializer.WriteI32(payload, ref offset, edge.ToId);
                CanonicalSerializer.WriteU8(payload, ref offset, edge.ActiveLayers);
                CanonicalSerializer.WriteF32(payload, ref offset, edge.PlayerEdge);
                CanonicalSerializer.WriteF32(payload, ref offset, edge.Affinity);
                CanonicalSerializer.WriteF32(payload, ref offset, edge.Trust);
                // NextEpisodeId sits between the layer values and Memory[] per the Appendix B v0.7
                // order patch — without it a rehydrated live edge would re-derive the high-water
                // mark from retained ids and break FR-LW-009 (the ColdSummary parallel).
                CanonicalSerializer.WriteU32(payload, ref offset, edge.NextEpisodeId);
                WriteEpisodes(payload, ref offset, edge.Memory);
            }

            // Arc block — spawn order (the ArcEngine's own deterministic order, FR-LW-017/021).
            CanonicalSerializer.WriteI32(payload, ref offset, arcs.ArcCount);
            for (int i = 0; i < arcs.ArcCount; i++)
            {
                Arc arc = arcs.GetArcAt(i);
                CanonicalSerializer.WriteU8(payload, ref offset, (byte)arc.Kind);
                CanonicalSerializer.WriteU8(payload, ref offset, arc.State);
                CanonicalSerializer.WriteU16(payload, ref offset, arc.Cause.TriggerId);
                SpawnCause.Input[] inputs = arc.Cause.Inputs ?? Array.Empty<SpawnCause.Input>();
                CanonicalSerializer.WriteI32(payload, ref offset, inputs.Length);
                for (int k = 0; k < inputs.Length; k++)
                {
                    CanonicalSerializer.WriteI16(payload, ref offset, inputs[k].Key);
                    CanonicalSerializer.WriteF32(payload, ref offset, inputs[k].Value);
                }
                CanonicalSerializer.WriteU64(payload, ref offset, arc.Cause.SnapshotRef);
                CanonicalSerializer.WriteU32(payload, ref offset, arc.Cause.WorldTick);
                CanonicalSerializer.WriteI32(payload, ref offset, arc.PinnedEpisodes.Length);
                for (int k = 0; k < arc.PinnedEpisodes.Length; k++)
                {
                    CanonicalSerializer.WriteI32(payload, ref offset, arc.PinnedEpisodes[k].EdgeFromId);
                    CanonicalSerializer.WriteI32(payload, ref offset, arc.PinnedEpisodes[k].EdgeToId);
                    CanonicalSerializer.WriteU32(payload, ref offset, arc.PinnedEpisodes[k].EpisodeId);
                }
                CanonicalSerializer.WriteU32(payload, ref offset, arc.SpawnTick);
                CanonicalSerializer.WriteU32(payload, ref offset, arc.MaxLifetimeTick);
            }

            // Cold-summary block — canonical EntityId order (the store's own order).
            CanonicalSerializer.WriteI32(payload, ref offset, cold.Count);
            for (int i = 0; i < cold.Count; i++)
            {
                ColdSummary summary = cold.GetAt(i);
                CanonicalSerializer.WriteI32(payload, ref offset, summary.EntityId);
                CanonicalSerializer.WriteU8(payload, ref offset, summary.ActiveLayers);
                CanonicalSerializer.WriteF32(payload, ref offset, summary.NetRelationship);
                CanonicalSerializer.WriteU32(payload, ref offset, summary.NextEpisodeId);
                WriteEpisodes(payload, ref offset, summary.RetainedEpisodes);
            }

            if (offset != payload.Length)
            {
                throw new InvalidOperationException("WorldStateSerializer.Serialize: size computation drifted from the writer (internal error).");
            }
            return payload;
        }

        /// <summary>
        /// Rebuilds the four stores from a payload. Fails loud on an unknown format version, a wrong
        /// domain tag, trailing bytes, and on any store-seam validation the payload violates.
        /// </summary>
        public static void Deserialize(byte[] payload, out WorldClock clock, out MemoryStore memory, out ArcEngine arcs, out ColdStore cold)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            int offset = 0;

            // Format version gates all field interpretation (the #19 FR-TS-070 ordering parallel).
            ushort version = CanonicalSerializer.ReadU16(payload, ref offset);
            if (version != LivingWorldConstants.WORLD_SNAPSHOT_FORMAT_VERSION)
            {
                throw new ArgumentException("WorldStateSerializer.Deserialize: unknown WORLD_SNAPSHOT_FORMAT_VERSION.", nameof(payload));
            }
            byte tag = CanonicalSerializer.ReadU8(payload, ref offset);
            if (tag != LivingWorldConstants.DomainTagLivingWorld)
            {
                throw new ArgumentException("WorldStateSerializer.Deserialize: payload does not carry DOMAIN_TAG_LIVING_WORLD.", nameof(payload));
            }

            clock = new WorldClock(CanonicalSerializer.ReadU32(payload, ref offset));

            memory = new MemoryStore();
            int edgeCount = CanonicalSerializer.ReadI32(payload, ref offset);
            for (int i = 0; i < edgeCount; i++)
            {
                RelationshipEdge edge = default;
                edge.FromId = CanonicalSerializer.ReadI32(payload, ref offset);
                edge.ToId = CanonicalSerializer.ReadI32(payload, ref offset);
                edge.ActiveLayers = CanonicalSerializer.ReadU8(payload, ref offset);
                edge.PlayerEdge = CanonicalSerializer.ReadF32(payload, ref offset);
                edge.Affinity = CanonicalSerializer.ReadF32(payload, ref offset);
                edge.Trust = CanonicalSerializer.ReadF32(payload, ref offset);
                edge.NextEpisodeId = CanonicalSerializer.ReadU32(payload, ref offset);
                edge.Memory = ReadEpisodes(payload, ref offset);
                memory.InsertEdge(in edge);
            }

            arcs = new ArcEngine(memory);
            int arcCount = CanonicalSerializer.ReadI32(payload, ref offset);
            for (int i = 0; i < arcCount; i++)
            {
                ArcKind kind = (ArcKind)CanonicalSerializer.ReadU8(payload, ref offset);
                byte state = CanonicalSerializer.ReadU8(payload, ref offset);
                ushort triggerId = CanonicalSerializer.ReadU16(payload, ref offset);
                int inputCount = CanonicalSerializer.ReadI32(payload, ref offset);
                SpawnCause.Input[] inputs = inputCount == 0 ? Array.Empty<SpawnCause.Input>() : new SpawnCause.Input[inputCount];
                for (int k = 0; k < inputCount; k++)
                {
                    // CanonicalSerializer has no ReadI16 (WriteI16 encodes as u16 bits, §3.2.4.1) —
                    // the cast round-trips the two's-complement bits exactly.
                    short key = (short)CanonicalSerializer.ReadU16(payload, ref offset);
                    float value = CanonicalSerializer.ReadF32(payload, ref offset);
                    inputs[k] = new SpawnCause.Input(key, value);
                }
                ulong snapshotRef = CanonicalSerializer.ReadU64(payload, ref offset);
                uint causeTick = CanonicalSerializer.ReadU32(payload, ref offset);
                int pinCount = CanonicalSerializer.ReadI32(payload, ref offset);
                Arc.PinnedEpisode[] pins = pinCount == 0 ? Array.Empty<Arc.PinnedEpisode>() : new Arc.PinnedEpisode[pinCount];
                for (int k = 0; k < pinCount; k++)
                {
                    int fromId = CanonicalSerializer.ReadI32(payload, ref offset);
                    int toId = CanonicalSerializer.ReadI32(payload, ref offset);
                    uint episodeId = CanonicalSerializer.ReadU32(payload, ref offset);
                    pins[k] = new Arc.PinnedEpisode(fromId, toId, episodeId);
                }
                uint spawnTick = CanonicalSerializer.ReadU32(payload, ref offset);
                uint maxLifetimeTick = CanonicalSerializer.ReadU32(payload, ref offset);
                if (maxLifetimeTick <= spawnTick)
                {
                    throw new ArgumentException("WorldStateSerializer.Deserialize: arc MaxLifetimeTick must exceed SpawnTick.", nameof(payload));
                }
                SpawnCause cause = new SpawnCause(triggerId, inputs, snapshotRef, causeTick);
                // SpawnArc re-takes every pin (reconstructing FR-LW-018 refcounts) and re-gates the
                // lifetime; AdvanceState restores the kind-specific lifecycle byte.
                int idx = arcs.SpawnArc(kind, in cause, pins, spawnTick, maxLifetimeTick - spawnTick);
                arcs.AdvanceState(idx, state);
            }

            cold = new ColdStore();
            int coldCount = CanonicalSerializer.ReadI32(payload, ref offset);
            for (int i = 0; i < coldCount; i++)
            {
                ColdSummary summary = default;
                summary.EntityId = CanonicalSerializer.ReadI32(payload, ref offset);
                summary.ActiveLayers = CanonicalSerializer.ReadU8(payload, ref offset);
                summary.NetRelationship = CanonicalSerializer.ReadF32(payload, ref offset);
                summary.NextEpisodeId = CanonicalSerializer.ReadU32(payload, ref offset);
                summary.RetainedEpisodes = ReadEpisodes(payload, ref offset);
                cold.Add(in summary);
            }

            if (offset != payload.Length)
            {
                throw new ArgumentException("WorldStateSerializer.Deserialize: trailing bytes after the cold-summary block.", nameof(payload));
            }
        }

        // ── internals ──────────────────────────────────────────────────────────────────────

        private const int EpisodeBytes = 4 + 1 + 4 + 4 + 2; // EpisodeId, Kind, Salience, WorldTick, ManagerChoiceId

        /// <summary>
        /// Highest currently-defined <see cref="EventKind"/> ordinal — the deserialization junk-cast
        /// gate (the ArcEngine MaxDefinedArcKind parallel). Extend alongside any roster APPEND
        /// (FR-LW-028).
        /// </summary>
        private const EventKind MaxDefinedEventKind = EventKind.PressConferenceTrap;

        private static void WriteEpisodes(byte[] payload, ref int offset, MemoryEpisode[] episodes)
        {
            CanonicalSerializer.WriteI32(payload, ref offset, episodes.Length);
            for (int i = 0; i < episodes.Length; i++)
            {
                CanonicalSerializer.WriteU32(payload, ref offset, episodes[i].EpisodeId);
                CanonicalSerializer.WriteU8(payload, ref offset, (byte)episodes[i].Kind);
                CanonicalSerializer.WriteF32(payload, ref offset, episodes[i].Salience);
                CanonicalSerializer.WriteU32(payload, ref offset, episodes[i].WorldTick);
                CanonicalSerializer.WriteU16(payload, ref offset, episodes[i].ManagerChoiceId);
            }
        }

        private static MemoryEpisode[] ReadEpisodes(byte[] payload, ref int offset)
        {
            int count = CanonicalSerializer.ReadI32(payload, ref offset);
            if (count == 0)
            {
                return Array.Empty<MemoryEpisode>();
            }
            MemoryEpisode[] episodes = new MemoryEpisode[count];
            for (int i = 0; i < count; i++)
            {
                uint episodeId = CanonicalSerializer.ReadU32(payload, ref offset);
                EventKind kind = (EventKind)CanonicalSerializer.ReadU8(payload, ref offset);
                if (kind > MaxDefinedEventKind)
                {
                    throw new ArgumentException("WorldStateSerializer: episode EventKind is out of roster (FR-LW-028).", nameof(payload));
                }
                float salience = CanonicalSerializer.ReadF32(payload, ref offset);
                uint worldTick = CanonicalSerializer.ReadU32(payload, ref offset);
                ushort managerChoiceId = CanonicalSerializer.ReadU16(payload, ref offset);
                episodes[i] = new MemoryEpisode(episodeId, kind, salience, worldTick, managerChoiceId);
            }
            return episodes;
        }

        private static int ComputeSize(MemoryStore memory, ArcEngine arcs, ColdStore cold)
        {
            int size = 2 + 1 + 4; // version, domain tag, worldTick

            size += 4; // edge count
            for (int i = 0; i < memory.EdgeCount; i++)
            {
                RelationshipEdge edge = memory.GetEdgeAt(i);
                size += 4 + 4 + 1 + 4 + 4 + 4 + 4;              // FromId..Trust, NextEpisodeId
                size += 4 + edge.Memory.Length * EpisodeBytes;   // episode block
            }

            size += 4; // arc count
            for (int i = 0; i < arcs.ArcCount; i++)
            {
                Arc arc = arcs.GetArcAt(i);
                int inputCount = arc.Cause.Inputs == null ? 0 : arc.Cause.Inputs.Length;
                size += 1 + 1;                                   // Kind, State
                size += 2 + 4 + inputCount * (2 + 4) + 8 + 4;    // Cause
                size += 4 + arc.PinnedEpisodes.Length * (4 + 4 + 4);
                size += 4 + 4;                                   // SpawnTick, MaxLifetimeTick
            }

            size += 4; // cold count
            for (int i = 0; i < cold.Count; i++)
            {
                ColdSummary summary = cold.GetAt(i);
                size += 4 + 1 + 4 + 4;
                size += 4 + summary.RetainedEpisodes.Length * EpisodeBytes;
            }
            return size;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial implementation (slice 3): §4.6 canonical living-world |
// |         |            |        | block in the Appendix B pinned order (v0.7 — live edges gain  |
// |         |            |        | NextEpisodeId per FR-LW-009); deserialization rebuilds        |
// |         |            |        | through the validating store seams (SpawnArc re-pins ⇒        |
// |         |            |        | FR-LW-018 refcounts reconstructed); fail-loud version/tag/    |
// |         |            |        | trailing-byte/roster gates. Composite SNAPSHOT_SCHEMA_VERSION |
// |         |            |        | bump stays at the KD-10 season composition root.              |
#endregion
