// File:     src/living-world/WorldStore.cs
// Created:  2026-07-03
// Modified: 2026-07-03
// Author:   —
// Spec:     Living World System #22 §4.2 (KD-4), §4.5, §4.6, §7.1 (KD-10), FR-LW-019/022/023/027/032,
//           Deterministic Simulation #16 §3.2.4.1, Code Standards #20
// Purpose:  The KD-10 season composition root — the persistent world store + season-calendar loop.
//           Owns and wires the living-world services (clock, memory, cold store, arcs, membership,
//           loop) into one durable object, drives the per-day season loop, and persists the whole
//           store (the §4.6 four-store block + the manager id + the FR-LW-022 membership roster).

using System;

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// The persistent world store and season-calendar loop for one manager (#22 §7.1 KD-10 — the one
    /// prerequisite owned by this spec: "persistent world store + season-calendar loop"). It is the
    /// composition root every prior slice deferred to: it constructs the six living-world services in
    /// dependency order, exposes them, drives one calendar day per <see cref="AdvanceDay"/>, and
    /// round-trips the entire store through <see cref="Snapshot"/>/<see cref="Restore"/>.
    ///
    /// SEASON LOOP (§4.2, KD-4): <see cref="AdvanceDay"/> runs the injected <see cref="WorldLoop"/>
    /// one <c>worldTick</c> = one calendar day (never inside the 10 Hz / 60 Hz match loops). The
    /// §4.2 phases that have producers run (3 salience decay, 4 arc expiry, 6 external-cap LRU); the
    /// three whose upstreams do not exist stay the WorldLoop's documented null seams — this root does
    /// NOT invent them (FR-LW-031): phase 1 match-outcome ingest (match engine), phase 2 committed
    /// human-systems read (vol-2/vol-3), phase 5 background-tier update (BackgroundTierSim). The vol-2
    /// daily human-systems update is a prior season-loop phase owned outside this layer (§4.2/KD-9) —
    /// a caller runs it before <see cref="AdvanceDay"/>; this root only reads committed output once
    /// those producers land.
    ///
    /// PERSISTENCE (§4.6 / FR-LW-022): <see cref="Snapshot"/> composes the §4.6 four-store block
    /// (<see cref="WorldStateSerializer"/> — clock + edges/episodes + arcs + cold summaries, bitwise
    /// via the #16 <see cref="CanonicalSerializer"/>) with the two pieces that block does not carry —
    /// the manager id and the active-set membership roster — behind a fail-loud composite header. The
    /// composite IS the "living-world state enters the canonical snapshot field set" step §4.6 assigns
    /// to this root; the <c>SNAPSHOT_SCHEMA_VERSION</c> fold into the match/season save stays deferred
    /// (FR-LW-003 bars the match engine from referencing this assembly).
    ///
    /// SCOPE: single-manager, matching the <see cref="ColdStore"/>/<see cref="ActiveSetMembership"/>
    /// shape (§3.5 "per-manager"). The aperiodic <c>world.text</c> RNG stream belongs to a separately-
    /// attached <see cref="InteractionTextGenerator"/> (player-triggered, not part of the tick loop),
    /// so no RNG service is owned here; its cursor is #16-serialized wherever that generator is wired.
    /// Day-cadence — not subject to the 60 Hz zero-alloc / ProfilerMarker hot-path rules.
    /// </summary>
    public sealed class WorldStore
    {
        private readonly int _managerId;
        private WorldClock _clock;
        private MemoryStore _memory;
        private ColdStore _cold;
        private ArcEngine _arcs;
        private ActiveSetMembership _membership;
        private WorldLoop _loop;

        /// <summary>The manager this world store is scoped to.</summary>
        public int ManagerId => _managerId;

        /// <summary>Current calendar day (0 at world start; advanced only by <see cref="AdvanceDay"/>).</summary>
        public uint CurrentWorldTick => _clock.CurrentWorldTick;

        /// <summary>The deep-tier episodic-memory / relationship-edge store.</summary>
        public MemoryStore Memory => _memory;

        /// <summary>The cold (background) tier of compressed summaries.</summary>
        public ColdStore Cold => _cold;

        /// <summary>The emergent-arc lifecycle engine.</summary>
        public ArcEngine Arcs => _arcs;

        /// <summary>The §3.5 active-set membership service.</summary>
        public ActiveSetMembership Membership => _membership;

        /// <summary>
        /// Constructs an empty world store for the given manager at calendar day 0. All stores are
        /// freshly allocated and wired: the loop drives arc expiry (phase 4) and the external-cap LRU
        /// (phase 6) over the same instances.
        /// </summary>
        public WorldStore(int managerId)
        {
            if (managerId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(managerId), managerId,
                    "WorldStore: manager id must be non-negative.");
            }
            _managerId = managerId;
            _clock = new WorldClock();
            _memory = new MemoryStore();
            _cold = new ColdStore();
            _arcs = new ArcEngine(_memory);
            _membership = new ActiveSetMembership(managerId, _memory, _cold);
            _loop = new WorldLoop(_clock, _memory, _arcs, _membership);
        }

        // Private ctor used by Restore — takes already-rebuilt, already-wired stores.
        private WorldStore(int managerId, WorldClock clock, MemoryStore memory, ColdStore cold,
            ArcEngine arcs, ActiveSetMembership membership, WorldLoop loop)
        {
            _managerId = managerId;
            _clock = clock;
            _memory = memory;
            _cold = cold;
            _arcs = arcs;
            _membership = membership;
            _loop = loop;
        }

        /// <summary>
        /// Advances the season by one calendar day, running the §4.2 world-loop phases that have
        /// producers (see the class doc). The prior vol-2/vol-3 daily update, if it exists, must run
        /// before this call (§4.2/KD-9).
        /// </summary>
        public void AdvanceDay()
        {
            _loop.RunWorldTick();
        }

        /// <summary>
        /// Records one interaction with a contact on the current calendar day (§3.2/§3.5): routes to
        /// <see cref="ActiveSetMembership.RecordInteraction"/> stamping the episode with
        /// <see cref="CurrentWorldTick"/> so the caller never supplies the day itself.
        /// </summary>
        /// <returns>The allocated episodeId — the durable handle an arc pins (FR-LW-009).</returns>
        public uint RecordInteraction(int entityId, bool isOwnClub, byte activeLayers, EventKind kind, ushort managerChoiceId)
        {
            return _membership.RecordInteraction(entityId, isOwnClub, activeLayers, kind, _clock.CurrentWorldTick, managerChoiceId);
        }

        // ── persistence (§4.6 / FR-LW-022) ──────────────────────────────────────────────────

        /// <summary>
        /// Serializes the whole store to one canonical payload: a fail-loud composite header
        /// (version + domain tag + manager id), the §4.6 four-store block, then the membership roster.
        /// A round-trip through <see cref="Restore"/> reproduces field-identical state.
        /// </summary>
        public byte[] Snapshot()
        {
            byte[] storeBlock = WorldStateSerializer.Serialize(_clock, _memory, _arcs, _cold);

            int size = 2 + 1 + 4;                 // version, domain tag, manager id
            size += 4 + storeBlock.Length;        // store-block length prefix + block
            size += 4;                            // member count
            size += _membership.MemberCount * (4 + 1); // entityId + isOwnClub flag per member

            byte[] payload = new byte[size];
            int offset = 0;

            CanonicalSerializer.WriteU16(payload, ref offset, LivingWorldConstants.WORLD_STORE_FORMAT_VERSION);
            CanonicalSerializer.WriteU8(payload, ref offset, LivingWorldConstants.DomainTagLivingWorld);
            CanonicalSerializer.WriteI32(payload, ref offset, _managerId);

            CanonicalSerializer.WriteI32(payload, ref offset, storeBlock.Length);
            Array.Copy(storeBlock, 0, payload, offset, storeBlock.Length);
            offset += storeBlock.Length;

            CanonicalSerializer.WriteI32(payload, ref offset, _membership.MemberCount);
            for (int i = 0; i < _membership.MemberCount; i++)
            {
                _membership.GetMemberAt(i, out int entityId, out bool isOwnClub);
                CanonicalSerializer.WriteI32(payload, ref offset, entityId);
                CanonicalSerializer.WriteU8(payload, ref offset, isOwnClub ? (byte)1 : (byte)0);
            }

            if (offset != payload.Length)
            {
                throw new InvalidOperationException("WorldStore.Snapshot: size computation drifted from the writer (internal error).");
            }
            return payload;
        }

        /// <summary>
        /// Rebuilds a world store from a <see cref="Snapshot"/> payload, re-wiring every service.
        /// Fails loud on an unknown composite version, a wrong domain tag, a corrupt length/count
        /// prefix, an out-of-range membership flag, trailing bytes, or any store-seam validation the
        /// payload violates (edges/arcs/summaries via <see cref="WorldStateSerializer"/>, roster via
        /// <see cref="ActiveSetMembership.RestoreMember"/>).
        /// </summary>
        public static WorldStore Restore(byte[] payload)
        {
            if (payload == null)
            {
                throw new ArgumentNullException(nameof(payload));
            }
            int offset = 0;

            ushort version = CanonicalSerializer.ReadU16(payload, ref offset);
            if (version != LivingWorldConstants.WORLD_STORE_FORMAT_VERSION)
            {
                throw new ArgumentException("WorldStore.Restore: unknown WORLD_STORE_FORMAT_VERSION.", nameof(payload));
            }
            byte tag = CanonicalSerializer.ReadU8(payload, ref offset);
            if (tag != LivingWorldConstants.DomainTagLivingWorld)
            {
                throw new ArgumentException("WorldStore.Restore: payload does not carry DOMAIN_TAG_LIVING_WORLD.", nameof(payload));
            }
            int managerId = CanonicalSerializer.ReadI32(payload, ref offset);
            if (managerId < 0)
            {
                throw new ArgumentException("WorldStore.Restore: manager id must be non-negative.", nameof(payload));
            }

            int storeLen = ReadCount(payload, ref offset);
            byte[] storeBlock = new byte[storeLen];
            Array.Copy(payload, offset, storeBlock, 0, storeLen);
            offset += storeLen;
            WorldStateSerializer.Deserialize(storeBlock, out WorldClock clock, out MemoryStore memory,
                out ArcEngine arcs, out ColdStore cold);

            ActiveSetMembership membership = new ActiveSetMembership(managerId, memory, cold);
            int memberCount = ReadCount(payload, ref offset);
            for (int i = 0; i < memberCount; i++)
            {
                int entityId = CanonicalSerializer.ReadI32(payload, ref offset);
                byte flag = CanonicalSerializer.ReadU8(payload, ref offset);
                if (flag > 1)
                {
                    throw new ArgumentException("WorldStore.Restore: membership isOwnClub flag must be 0 or 1.", nameof(payload));
                }
                membership.RestoreMember(entityId, flag == 1);
            }

            if (offset != payload.Length)
            {
                throw new ArgumentException("WorldStore.Restore: trailing bytes after the membership roster.", nameof(payload));
            }

            WorldLoop loop = new WorldLoop(clock, memory, arcs, membership);
            return new WorldStore(managerId, clock, memory, cold, arcs, membership, loop);
        }

        /// <summary>
        /// Reads a length prefix and fails loud on a corrupt count (the WorldStateSerializer.ReadCount
        /// parallel): a negative count desyncs the read offset; an absurd positive count throws on the
        /// element read/allocation. Every subsequent record is ≥ 1 byte, so a valid count can never
        /// exceed the bytes remaining after the prefix.
        /// </summary>
        private static int ReadCount(byte[] payload, ref int offset)
        {
            int count = CanonicalSerializer.ReadI32(payload, ref offset);
            if (count < 0 || count > payload.Length - offset)
            {
                throw new ArgumentException(
                    "WorldStore.Restore: corrupt length prefix (negative, or exceeds the remaining payload).", nameof(payload));
            }
            return count;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-03 | —      | KD-10 season composition root: persistent world store +       |
// |         |            |        | season-calendar loop. Owns/wires clock + memory + cold +      |
// |         |            |        | arcs + membership + loop; AdvanceDay drives the §4.2 phases    |
// |         |            |        | that have producers (phases 1/2/5 stay WorldLoop null seams,  |
// |         |            |        | FR-LW-031); Snapshot/Restore round-trip the §4.6 four-store    |
// |         |            |        | block + manager id + FR-LW-022 membership roster.             |
#endregion
