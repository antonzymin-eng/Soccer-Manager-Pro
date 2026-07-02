// File:     src/living-world/MemoryStore.cs
// Created:  2026-07-02
// Modified: 2026-07-02
// Author:   —
// Spec:     Living World System #22 §3.1, §3.2, §4.3, §4.4, FR-LW-004/005/008/009/010/018/021, Code Standards #20
// Purpose:  The live (deep-tier) world store: relationship edges with their bounded episode buffers.
//           Owns episode append/eviction (§3.2), per-tick salience decay, arc pinning (FR-LW-018),
//           and the §3.1 owned-layer event update. Canonical (FromId, ToId) iteration order (FR-LW-021).

using System;
using System.Collections.Generic;

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// Live edge + episodic-memory store for the deep tier (#22 §3.2 / §4.3). Edges are kept sorted by
    /// the stable (FromId, ToId) key so every pass iterates in canonical order — never container
    /// enumeration order (FR-LW-021 / T-LW-DET-002). All state is plain serialisable value state
    /// (FR-LW-022). Runs at calendar-day cadence (KD-4), NOT on the 60 Hz hot path, so the
    /// append/evict array reallocation here is not subject to the zero-alloc game-loop budget.
    /// </summary>
    public sealed class MemoryStore
    {
        /// <summary>
        /// A reference-counted pin: (edge FromId, edge ToId, episodeId) + the number of live arcs
        /// holding it. FR-LW-018 — two arcs may pin the same episode; the episode stays exempt until
        /// the LAST arc unpins (AR-1 M-1: a single-entry pin let the first arc's resolve drop the
        /// second arc's protection — the F1 dangling-episode class).
        /// </summary>
        private readonly struct PinEntry
        {
            public readonly int FromId;
            public readonly int ToId;
            public readonly uint EpisodeId;
            public readonly int Count;

            public PinEntry(int fromId, int toId, uint episodeId, int count)
            {
                FromId = fromId;
                ToId = toId;
                EpisodeId = episodeId;
                Count = count;
            }
        }

        /// <summary>
        /// Bits of <see cref="RelationshipEdge.ActiveLayers"/> that correspond to a defined
        /// <see cref="RelationshipLayer"/> member. Built from the enum so a roster APPEND extends it
        /// automatically; a mask carrying any other bit is refused (AR-2 M-1 — an undefined bit made
        /// IsLayerActive pass for an out-of-roster layer value).
        /// </summary>
        private const byte DefinedLayersMask =
            (1 << (int)RelationshipLayer.PlayerEdge)
            | (1 << (int)RelationshipLayer.Affinity)
            | (1 << (int)RelationshipLayer.Trust);

        private readonly List<RelationshipEdge> _edges = new List<RelationshipEdge>();
        private readonly List<PinEntry> _pins = new List<PinEntry>();

        /// <summary>Number of live edges.</summary>
        public int EdgeCount => _edges.Count;

        /// <summary>
        /// Returns the edge at the given canonical-order index (ascending (FromId, ToId)).
        /// NOTE: the returned struct's <c>Memory</c> array is the LIVE buffer, not a copy — callers
        /// must treat it as read-only (exposed-array hand-over convention, #18 AR-1 L-3 parallel).
        /// </summary>
        public RelationshipEdge GetEdgeAt(int index) => _edges[index];

        /// <summary>Copies out the edge for the given directed pair. Same live-array caveat as <see cref="GetEdgeAt"/>.</summary>
        public bool TryGetEdge(int fromId, int toId, out RelationshipEdge edge)
        {
            int idx = FindEdgeIndex(fromId, toId, out bool found);
            edge = found ? _edges[idx] : default;
            return found;
        }

        /// <summary>
        /// Creates the directed edge if absent. A fresh edge initialises every layer to the strangers
        /// baseline 0.0 with an empty memory buffer (§2.3 identity; the vol-2 PlayerEdge mirror is
        /// populated by the canon read at wiring, never written here — FR-LW-004).
        /// </summary>
        public void GetOrCreateEdge(int fromId, int toId, byte activeLayers)
        {
            ValidatePair(fromId, toId);
            ValidateMask(activeLayers);
            int idx = FindEdgeIndex(fromId, toId, out bool found);
            if (found)
            {
                if (_edges[idx].ActiveLayers != activeLayers)
                {
                    throw new InvalidOperationException(
                        "MemoryStore.GetOrCreateEdge: edge exists with a different ActiveLayers mask — silently keeping the old mask would misroute updates (AR-1 L-1).");
                }
                return;
            }

            RelationshipEdge edge = default;
            edge.FromId = fromId;
            edge.ToId = toId;
            edge.ActiveLayers = activeLayers;
            edge.Memory = Array.Empty<MemoryEpisode>();
            _edges.Insert(idx, edge);
        }

        /// <summary>
        /// Inserts a fully-formed edge (the §3.5 rehydration path). Throws if the pair already has a
        /// live edge — promotion must not duplicate state (FR-LW-025).
        /// </summary>
        public void InsertEdge(in RelationshipEdge edge)
        {
            ValidatePair(edge.FromId, edge.ToId);
            ValidateMask(edge.ActiveLayers);
            if (edge.Memory == null)
            {
                throw new ArgumentException("MemoryStore.InsertEdge: Memory buffer must be non-null (use Array.Empty).");
            }
            // F6 gate (AR-1 L-2): every ACTIVE layer value must be finite in [0,1]; the negated
            // comparison fails closed on NaN. Inactive layers hold 0.0 and are not checked.
            if ((edge.IsLayerActive(RelationshipLayer.PlayerEdge) && !(edge.PlayerEdge >= 0f && edge.PlayerEdge <= 1f))
                || (edge.IsLayerActive(RelationshipLayer.Affinity) && !(edge.Affinity >= 0f && edge.Affinity <= 1f))
                || (edge.IsLayerActive(RelationshipLayer.Trust) && !(edge.Trust >= 0f && edge.Trust <= 1f)))
            {
                throw new ArgumentException("MemoryStore.InsertEdge: an active layer value escapes [0, 1] (F6 invariant).");
            }
            // Memory coherence gates (AR-2 L-1/L-2): the store maintains episodeIds strictly ascending
            // in the buffer (append order = allocation order; eviction preserves order) and all below
            // the NextEpisodeId high-water mark — accepting anything else lets the next RecordEpisode
            // re-allocate a live id and break the FR-LW-009 durable-handle contract pins depend on.
            // Salience must be finite in [0,1] (NaN-gated) — it feeds the eviction comparator and the
            // §3.3 refThreshold checks.
            for (int i = 0; i < edge.Memory.Length; i++)
            {
                if (edge.Memory[i].EpisodeId >= edge.NextEpisodeId)
                {
                    throw new ArgumentException("MemoryStore.InsertEdge: episodeId >= NextEpisodeId (FR-LW-009 monotonicity).");
                }
                if (i > 0 && edge.Memory[i].EpisodeId <= edge.Memory[i - 1].EpisodeId)
                {
                    throw new ArgumentException("MemoryStore.InsertEdge: episodeIds must be strictly ascending (duplicate ids break pin identity).");
                }
                if (!(edge.Memory[i].Salience >= 0f && edge.Memory[i].Salience <= 1f))
                {
                    throw new ArgumentException("MemoryStore.InsertEdge: episode salience must be finite in [0, 1].");
                }
            }
            int idx = FindEdgeIndex(edge.FromId, edge.ToId, out bool found);
            if (found)
            {
                throw new InvalidOperationException(
                    "MemoryStore.InsertEdge: edge already live for this pair — rehydration must not duplicate state (FR-LW-025).");
            }
            _edges.Insert(idx, edge);
        }

        /// <summary>
        /// Removes and returns the edge for the given pair (the §3.5 demotion path). Throws if absent —
        /// demotion of a non-live contact is a logic error (fail loud). Throws if the edge holds any
        /// arc-pinned episode: §3.5 demotion must SKIP such contacts (demoting one would orphan the
        /// arc's pinned episode — F1 — and strand stale entries in the pin table; AR-1 M-2).
        /// </summary>
        public RelationshipEdge RemoveEdge(int fromId, int toId)
        {
            int idx = FindEdgeIndex(fromId, toId, out bool found);
            if (!found)
            {
                throw new InvalidOperationException("MemoryStore.RemoveEdge: no live edge for this pair.");
            }
            if (EdgeHasPinnedEpisode(fromId, toId))
            {
                throw new InvalidOperationException(
                    "MemoryStore.RemoveEdge: edge holds an arc-pinned episode — §3.5 demotion must skip it until the arc resolves (FR-LW-018 / F1).");
            }
            RelationshipEdge edge = _edges[idx];
            _edges.RemoveAt(idx);
            return edge;
        }

        /// <summary>
        /// §3.1 event update of an OWNED layer (Affinity / Trust) toward its event target.
        /// <c>PlayerEdge</c> is a read-only mirror of vol-2 §2.1 and is refused here (FR-LW-004 / KD-9);
        /// an update on an inactive layer is refused (F6 — inactive layers are excluded from updates).
        /// </summary>
        /// <param name="delta">Signed event impact in [−1, 1].</param>
        /// <param name="volatility">Layer responsiveness v in (0, 1].</param>
        public void ApplyEvent(int fromId, int toId, RelationshipLayer layer, float delta, float volatility)
        {
            if (layer == RelationshipLayer.PlayerEdge)
            {
                throw new ArgumentException(
                    "MemoryStore.ApplyEvent: PlayerEdge is a read-only mirror of the vol-2 §2.1 authoritative edge (FR-LW-004 / KD-9).");
            }
            if (layer != RelationshipLayer.Affinity && layer != RelationshipLayer.Trust)
            {
                // AR-2 M-1: without this allowlist the else-branch below routed ANY out-of-roster
                // enum value (e.g. a junk cast) to the Trust write.
                throw new ArgumentOutOfRangeException(nameof(layer), layer,
                    "MemoryStore.ApplyEvent: not a defined owned layer.");
            }
            // NaN-gate pattern: negated comparisons fail closed on NaN.
            if (!(delta >= -1f && delta <= 1f))
            {
                throw new ArgumentOutOfRangeException(nameof(delta), delta, "MemoryStore.ApplyEvent: delta must be finite in [-1, 1].");
            }
            if (!(volatility > 0f && volatility <= 1f))
            {
                throw new ArgumentOutOfRangeException(nameof(volatility), volatility, "MemoryStore.ApplyEvent: volatility must be finite in (0, 1].");
            }

            int idx = FindEdgeIndex(fromId, toId, out bool found);
            if (!found)
            {
                throw new InvalidOperationException("MemoryStore.ApplyEvent: no live edge for this pair.");
            }
            RelationshipEdge edge = _edges[idx];
            if (!edge.IsLayerActive(layer))
            {
                throw new InvalidOperationException(
                    "MemoryStore.ApplyEvent: layer is not active on this edge (F6 — inactive layers hold 0.0 and are excluded from updates).");
            }
            if (layer == RelationshipLayer.Affinity)
            {
                edge.Affinity = LivingWorldMath.ApplyEvent(edge.Affinity, delta, volatility);
            }
            else
            {
                edge.Trust = LivingWorldMath.ApplyEvent(edge.Trust, delta, volatility);
            }
            _edges[idx] = edge;
        }

        /// <summary>
        /// §3.2 episode append: allocates the next monotonic per-edge episodeId (FR-LW-009). On a full
        /// buffer the lowest-salience UNPINNED PRE-EXISTING episode is evicted first (ties → oldest
        /// worldTick → lowest episodeId; FR-LW-010/018/021 — the §3.2 worked example's candidates are
        /// the episodes already in the buffer, so a fresh event is always remembered); if every
        /// pre-existing episode is arc-pinned the buffer grows transiently and shrinks back as arcs
        /// unpin (§3.2 step 2).
        /// </summary>
        /// <returns>The allocated episodeId — the durable handle an arc pins.</returns>
        public uint RecordEpisode(int fromId, int toId, EventKind kind, uint worldTick, ushort managerChoiceId)
        {
            int idx = FindEdgeIndex(fromId, toId, out bool found);
            if (!found)
            {
                throw new InvalidOperationException("MemoryStore.RecordEpisode: no live edge for this pair — create it first.");
            }
            RelationshipEdge edge = _edges[idx];
            uint episodeId = edge.NextEpisodeId;
            edge.NextEpisodeId = episodeId + 1u;

            while (edge.Memory.Length >= LivingWorldConstants.MEMORY_BUFFER_DEPTH && TryEvictOne(ref edge))
            {
            }

            MemoryEpisode episode = new MemoryEpisode(
                episodeId, kind, LivingWorldConstants.SALIENCE_INITIAL, worldTick, managerChoiceId);

            MemoryEpisode[] old = edge.Memory;
            MemoryEpisode[] grown = new MemoryEpisode[old.Length + 1];
            Array.Copy(old, grown, old.Length);
            grown[old.Length] = episode;
            edge.Memory = grown;
            _edges[idx] = edge;
            return episodeId;
        }

        /// <summary>
        /// Phase-3 per-tick salience decay: s' = s · (1 − decayRate) for every episode on every edge,
        /// iterated in canonical edge order (§3.2 step 3 / §4.2). A no-op on an empty store (FR-LW-034).
        /// </summary>
        public void DecayEpisodeSalience(float decayRate)
        {
            if (!(decayRate >= 0f && decayRate < 1f))
            {
                throw new ArgumentOutOfRangeException(nameof(decayRate), decayRate, "MemoryStore.DecayEpisodeSalience: rate must be finite in [0, 1).");
            }
            for (int i = 0; i < _edges.Count; i++)
            {
                MemoryEpisode[] memory = _edges[i].Memory;
                for (int j = 0; j < memory.Length; j++)
                {
                    memory[j] = memory[j].WithDecayedSalience(decayRate);
                }
            }
        }

        /// <summary>
        /// Pins an episode non-evictable until its arc resolves (FR-LW-018). Reference-counted: each
        /// call adds one hold; the episode stays exempt until every hold is released (AR-1 M-1). The
        /// episode must exist — pinning a dangling id is the F1 failure mode and fails loud here.
        /// </summary>
        public void PinEpisode(int fromId, int toId, uint episodeId)
        {
            if (!EpisodeExists(fromId, toId, episodeId))
            {
                throw new InvalidOperationException("MemoryStore.PinEpisode: episode not found on this edge (F1 dangling-id guard).");
            }
            int idx = FindPinIndex(fromId, toId, episodeId, out bool found);
            if (found)
            {
                PinEntry entry = _pins[idx];
                _pins[idx] = new PinEntry(entry.FromId, entry.ToId, entry.EpisodeId, entry.Count + 1);
                return;
            }
            _pins.Insert(idx, new PinEntry(fromId, toId, episodeId, 1));
        }

        /// <summary>
        /// Releases one pin hold (an arc resolved). Only when the LAST hold is released does the
        /// episode become evictable and the edge's buffer shrink back toward depth (§3.2 step 2 /
        /// AR-1 M-1). Unpinning an unknown key throws — the arc/pin bookkeeping must never drift.
        /// </summary>
        public void UnpinEpisode(int fromId, int toId, uint episodeId)
        {
            int idx = FindPinIndex(fromId, toId, episodeId, out bool found);
            if (!found)
            {
                throw new InvalidOperationException("MemoryStore.UnpinEpisode: pin not found.");
            }
            PinEntry entry = _pins[idx];
            if (entry.Count > 1)
            {
                _pins[idx] = new PinEntry(entry.FromId, entry.ToId, entry.EpisodeId, entry.Count - 1);
                return; // Another arc still holds the episode — nothing becomes evictable.
            }
            _pins.RemoveAt(idx);

            int edgeIdx = FindEdgeIndex(fromId, toId, out bool edgeFound);
            if (edgeFound)
            {
                EnforceDepth(edgeIdx);
            }
        }

        /// <summary>True if the episode is currently arc-pinned (FR-LW-018).</summary>
        public bool IsEpisodePinned(int fromId, int toId, uint episodeId)
        {
            FindPinIndex(fromId, toId, episodeId, out bool found);
            return found;
        }

        /// <summary>True if the edge holds any arc-pinned episode (the §3.5 LRU-demotion skip predicate).</summary>
        public bool EdgeHasPinnedEpisode(int fromId, int toId)
        {
            // _pins is sorted by (FromId, ToId, EpisodeId): the first pin for the pair, if any, sits
            // at the insertion point of (fromId, toId, episodeId=0).
            int idx = FindPinIndex(fromId, toId, 0u, out bool found);
            if (found)
            {
                return true;
            }
            return idx < _pins.Count && _pins[idx].FromId == fromId && _pins[idx].ToId == toId;
        }

        // ── internals ──────────────────────────────────────────────────────────────────────

        private static void ValidateMask(byte activeLayers)
        {
            if ((activeLayers & ~DefinedLayersMask) != 0)
            {
                throw new ArgumentException(
                    "MemoryStore: ActiveLayers carries a bit with no defined RelationshipLayer member (AR-2 M-1).");
            }
        }

        private static void ValidatePair(int fromId, int toId)
        {
            if (fromId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(fromId), fromId, "MemoryStore: entity ids must be non-negative (-1 is the loose/none sentinel).");
            }
            if (toId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(toId), toId, "MemoryStore: entity ids must be non-negative (-1 is the loose/none sentinel).");
            }
            if (fromId == toId)
            {
                throw new ArgumentException("MemoryStore: a relationship edge to self is invalid.");
            }
        }

        private bool EpisodeExists(int fromId, int toId, uint episodeId)
        {
            int idx = FindEdgeIndex(fromId, toId, out bool found);
            if (!found)
            {
                return false;
            }
            MemoryEpisode[] memory = _edges[idx].Memory;
            for (int i = 0; i < memory.Length; i++)
            {
                if (memory[i].EpisodeId == episodeId)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Post-unpin shrink: while over MEMORY_BUFFER_DEPTH (transient growth from an all-pinned
        /// append), evict lowest-salience unpinned episodes so the buffer returns to depth as arcs
        /// resolve (§3.2 step 2).
        /// </summary>
        private void EnforceDepth(int edgeIdx)
        {
            RelationshipEdge edge = _edges[edgeIdx];
            while (edge.Memory.Length > LivingWorldConstants.MEMORY_BUFFER_DEPTH && TryEvictOne(ref edge))
            {
            }
            _edges[edgeIdx] = edge;
        }

        /// <summary>
        /// Evicts the single lowest-salience unpinned episode per the FR-LW-021 total order
        /// (salience → oldest worldTick → lowest episodeId). Returns false — transient growth —
        /// when every episode is arc-pinned (FR-LW-018).
        /// </summary>
        private bool TryEvictOne(ref RelationshipEdge edge)
        {
            int evict = -1;
            for (int i = 0; i < edge.Memory.Length; i++)
            {
                if (IsEpisodePinned(edge.FromId, edge.ToId, edge.Memory[i].EpisodeId))
                {
                    continue;
                }
                if (evict < 0 || LivingWorldMath.CompareEvictability(edge.Memory[i], edge.Memory[evict]) < 0)
                {
                    evict = i;
                }
            }
            if (evict < 0)
            {
                return false; // All pinned — bounded by simultaneous arc pins + the §4.5 budget.
            }
            MemoryEpisode[] shrunk = new MemoryEpisode[edge.Memory.Length - 1];
            for (int i = 0, k = 0; i < edge.Memory.Length; i++)
            {
                if (i != evict)
                {
                    shrunk[k++] = edge.Memory[i];
                }
            }
            edge.Memory = shrunk;
            return true;
        }

        /// <summary>Binary search on the canonical (FromId, ToId) order. Returns the match or insertion index.</summary>
        private int FindEdgeIndex(int fromId, int toId, out bool found)
        {
            int lo = 0;
            int hi = _edges.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                RelationshipEdge e = _edges[mid];
                int c = e.FromId != fromId ? e.FromId.CompareTo(fromId) : e.ToId.CompareTo(toId);
                if (c == 0)
                {
                    found = true;
                    return mid;
                }
                if (c < 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            found = false;
            return lo;
        }

        /// <summary>Binary search on the canonical (FromId, ToId, EpisodeId) pin order.</summary>
        private int FindPinIndex(int fromId, int toId, uint episodeId, out bool found)
        {
            int lo = 0;
            int hi = _pins.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                PinEntry p = _pins[mid];
                int c = p.FromId != fromId ? p.FromId.CompareTo(fromId)
                      : p.ToId != toId ? p.ToId.CompareTo(toId)
                      : p.EpisodeId.CompareTo(episodeId);
                if (c == 0)
                {
                    found = true;
                    return mid;
                }
                if (c < 0)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid - 1;
                }
            }
            found = false;
            return lo;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                                                          |
// | 1.0     | 2026-07-02 | —      | Initial implementation (season/world loop slice 1): edges,    |
// |         |            |        | §3.2 episode append/evict/decay, FR-LW-018 pins, §3.1 owned-   |
// |         |            |        | layer ApplyEvent, canonical iteration (FR-LW-021).             |
// | 1.1     | 2026-07-02 | —      | AR-1 fix pass. M-1: pins reference-counted — two arcs pinning  |
// |         |            |        | one episode no longer lose protection when the first resolves |
// |         |            |        | (F1 class). M-2: RemoveEdge refuses an edge holding a pinned   |
// |         |            |        | episode (§3.5 demotion skip; orphan-pin hazard). L-1: GetOr-   |
// |         |            |        | CreateEdge throws on a conflicting ActiveLayers mask. L-2:     |
// |         |            |        | InsertEdge F6 gate (active layers finite in [0,1], NaN-gated). |
// |         |            |        | L-4: ValidatePair names the offending parameter.               |
// | 1.2     | 2026-07-02 | —      | AR-2 fix pass. M-1: ApplyEvent explicit owned-layer allowlist  |
// |         |            |        | (the else-branch routed any out-of-roster enum value to the    |
// |         |            |        | Trust write) + DefinedLayersMask validation on GetOrCreateEdge |
// |         |            |        | and InsertEdge (an undefined mask bit made IsLayerActive pass  |
// |         |            |        | for a junk layer). L-1/L-2: InsertEdge memory coherence gates  |
// |         |            |        | — episodeIds strictly ascending and < NextEpisodeId            |
// |         |            |        | (FR-LW-009); salience finite in [0,1], NaN-gated.              |
#endregion
