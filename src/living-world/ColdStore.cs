// File:     src/living-world/ColdStore.cs
// Created:  2026-07-02
// Modified: 2026-07-02 (slice-2 AR-1 M-2: TryPeek verify-before-take companion)
// Author:   —
// Spec:     Living World System #22 §3.5, §4.3, §4.5, FR-LW-009/021/023/025, Code Standards #20
// Purpose:  Cold-store container plus the §3.5 demotion/rehydration transforms: compress a live edge
//           to a ColdSummary (top-N salient episodes + NetRelationship + NextEpisodeId) and expand it
//           back on re-entry, deterministic and lossless within the retained-fields contract (F5).

using System;
using System.Collections.Generic;

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// The cold tier of the world store (#22 §3.5 / §4.3): compressed summaries of contacts that left
    /// the active set, kept sorted by EntityId so iteration and §4.5 eviction are canonical
    /// (FR-LW-021). Demotion is lossy by design (only COLD_SUMMARY_RETAINED_EPISODES survive);
    /// rehydration resumes the edge's episodeId counter from <see cref="ColdSummary.NextEpisodeId"/>
    /// so monotonicity holds across the cycle (FR-LW-009).
    ///
    /// RESIDUE-A DECISION (v1, recorded per §7.3): the compression schema is NetRelationship = mean of
    /// the ACTIVE owned layers (Affinity / Trust) of the demoted manager→contact edge; PlayerEdge is
    /// canon-owned (vol-2 §2.1) and re-read at rehydration, never summarised here (KD-9).
    /// </summary>
    public sealed class ColdStore
    {
        private readonly List<ColdSummary> _summaries = new List<ColdSummary>();

        /// <summary>Number of cold-stored contacts.</summary>
        public int Count => _summaries.Count;

        /// <summary>Returns the summary at the given canonical-order index (ascending EntityId).</summary>
        public ColdSummary GetAt(int index) => _summaries[index];

        /// <summary>
        /// Adds a summary (the §3.5 demotion path). Throws if the contact is already cold-stored —
        /// a double demotion without an intervening rehydration is a membership-bookkeeping error.
        /// </summary>
        public void Add(in ColdSummary summary)
        {
            if (summary.EntityId < 0)
            {
                throw new ArgumentException("ColdStore.Add: EntityId must be non-negative.");
            }
            if (summary.RetainedEpisodes == null)
            {
                throw new ArgumentException("ColdStore.Add: RetainedEpisodes must be non-null (use Array.Empty).");
            }
            // AR-1 L-3 coherence gates: NetRelationship must be finite in [0,1] (negated compare fails
            // closed on NaN), and every retained id must sit BELOW the high-water mark — otherwise
            // rehydration would re-allocate a retained id and break FR-LW-009 monotonicity.
            if (!(summary.NetRelationship >= 0f && summary.NetRelationship <= 1f))
            {
                throw new ArgumentException("ColdStore.Add: NetRelationship must be finite in [0, 1].");
            }
            for (int i = 0; i < summary.RetainedEpisodes.Length; i++)
            {
                if (summary.RetainedEpisodes[i].EpisodeId >= summary.NextEpisodeId)
                {
                    throw new ArgumentException(
                        "ColdStore.Add: retained episodeId >= NextEpisodeId — rehydration would reuse the id and break FR-LW-009 monotonicity.");
                }
                // AR-2 L-1/L-2: ascending-strict ids (duplicates would rehydrate into ambiguous pin
                // identity); salience finite in [0,1] (NaN fails closed — it feeds the eviction
                // comparator and §3.3 refThreshold on rehydration).
                if (i > 0 && summary.RetainedEpisodes[i].EpisodeId <= summary.RetainedEpisodes[i - 1].EpisodeId)
                {
                    throw new ArgumentException("ColdStore.Add: retained episodeIds must be strictly ascending.");
                }
                if (!(summary.RetainedEpisodes[i].Salience >= 0f && summary.RetainedEpisodes[i].Salience <= 1f))
                {
                    throw new ArgumentException("ColdStore.Add: retained episode salience must be finite in [0, 1].");
                }
            }
            int idx = FindIndex(summary.EntityId, out bool found);
            if (found)
            {
                throw new InvalidOperationException(
                    "ColdStore.Add: contact already cold-stored — demotion must not duplicate state (FR-LW-025).");
            }
            _summaries.Insert(idx, summary);
        }

        /// <summary>
        /// Non-destructive read of the contact's summary, if cold-stored. The verify-before-take
        /// companion to <see cref="TryTake"/> (slice-2 AR-1 M-2): pre-take validation (e.g. an
        /// ActiveLayers mask check) must run against a peek — validating after the destructive take
        /// and throwing would strand the summary outside both tiers (FR-LW-025).
        /// </summary>
        public bool TryPeek(int entityId, out ColdSummary summary)
        {
            int idx = FindIndex(entityId, out bool found);
            summary = found ? _summaries[idx] : default;
            return found;
        }

        /// <summary>
        /// Removes and returns the summary for the contact (the §3.5 promotion path). Returns false
        /// if the contact has no cold history (a genuinely new contact).
        /// ORDERING (AR-3 L-2): removal is destructive — the promotion flow must verify no live edge
        /// exists for the pair BEFORE taking (TryTake → Rehydrate → InsertEdge), because a duplicate-
        /// edge throw from InsertEdge after a take would strand the summary outside both tiers,
        /// violating the FR-LW-025 neither-loses-nor-duplicates contract. The §3.5 membership service
        /// (follow-up slice) owns that sequencing.
        /// </summary>
        public bool TryTake(int entityId, out ColdSummary summary)
        {
            int idx = FindIndex(entityId, out bool found);
            if (!found)
            {
                summary = default;
                return false;
            }
            summary = _summaries[idx];
            _summaries.RemoveAt(idx);
            return true;
        }

        /// <summary>
        /// §3.5 demotion transform: compresses a live edge to a ColdSummary. Retains the top
        /// <paramref name="retainedMax"/> episodes by keep-worthiness (the exact inverse of the
        /// FR-LW-021 eviction order — highest salience, ties → newest worldTick → highest episodeId),
        /// stored in ascending episodeId order for canonical serialisation. Pure transform: does not
        /// touch any store.
        /// </summary>
        public static ColdSummary Compress(in RelationshipEdge edge, int retainedMax)
        {
            if (retainedMax < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(retainedMax), retainedMax, "ColdStore.Compress: retainedMax must be ≥ 0.");
            }
            if (edge.Memory == null)
            {
                throw new ArgumentException("ColdStore.Compress: edge Memory buffer must be non-null (a live edge always carries one).");
            }

            // Residue-A v1: mean of the active owned layers (see class doc).
            float sum = 0f;
            int layers = 0;
            if (edge.IsLayerActive(RelationshipLayer.Affinity))
            {
                sum += edge.Affinity;
                layers++;
            }
            if (edge.IsLayerActive(RelationshipLayer.Trust))
            {
                sum += edge.Trust;
                layers++;
            }
            float net = layers > 0 ? sum / layers : 0f;

            MemoryEpisode[] ranked = new MemoryEpisode[edge.Memory.Length];
            Array.Copy(edge.Memory, ranked, edge.Memory.Length);
            // Descending keep-worthiness == descending CompareEvictability (a total order, so the
            // sort result is container-order independent; FR-LW-021).
            Array.Sort(ranked, (a, b) => LivingWorldMath.CompareEvictability(b, a));

            int keep = ranked.Length < retainedMax ? ranked.Length : retainedMax;
            MemoryEpisode[] retained = new MemoryEpisode[keep];
            Array.Copy(ranked, retained, keep);
            Array.Sort(retained, (a, b) => a.EpisodeId.CompareTo(b.EpisodeId));

            ColdSummary summary = default;
            summary.EntityId = edge.ToId;
            summary.ActiveLayers = edge.ActiveLayers;
            summary.NetRelationship = LivingWorldMath.Clamp01(net);
            summary.NextEpisodeId = edge.NextEpisodeId;
            summary.RetainedEpisodes = retained;
            return summary;
        }

        /// <summary>
        /// §3.5 promotion transform: expands a ColdSummary back into a live edge for the manager pair.
        /// Layers are seeded from NetRelationship (active owned layers only; inactive hold 0.0), memory
        /// from the retained episodes, and the episodeId counter resumes from NextEpisodeId — never
        /// max(retained)+1, which would reuse compressed-away ids (FR-LW-009). PlayerEdge is left 0.0
        /// for the canon read at wiring (KD-9). Pure transform: does not touch any store.
        /// </summary>
        public static RelationshipEdge Rehydrate(in ColdSummary summary, int managerFromId)
        {
            if (managerFromId < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(managerFromId), managerFromId, "ColdStore.Rehydrate: manager id must be non-negative.");
            }

            RelationshipEdge edge = default;
            edge.FromId = managerFromId;
            edge.ToId = summary.EntityId;
            edge.ActiveLayers = summary.ActiveLayers;
            edge.Affinity = edge.IsLayerActive(RelationshipLayer.Affinity) ? summary.NetRelationship : 0f;
            edge.Trust = edge.IsLayerActive(RelationshipLayer.Trust) ? summary.NetRelationship : 0f;
            edge.NextEpisodeId = summary.NextEpisodeId;

            MemoryEpisode[] retained = summary.RetainedEpisodes ?? Array.Empty<MemoryEpisode>();
            MemoryEpisode[] memory = new MemoryEpisode[retained.Length];
            Array.Copy(retained, memory, retained.Length);
            edge.Memory = memory;
            return edge;
        }

        /// <summary>Binary search on the canonical EntityId order. Returns the match or insertion index.</summary>
        private int FindIndex(int entityId, out bool found)
        {
            int lo = 0;
            int hi = _summaries.Count - 1;
            while (lo <= hi)
            {
                int mid = (lo + hi) >> 1;
                int c = _summaries[mid].EntityId.CompareTo(entityId);
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
// | 1.0     | 2026-07-02 | —      | Initial implementation (season/world loop slice 1): sorted    |
// |         |            |        | summary container + §3.5 Compress/Rehydrate transforms;       |
// |         |            |        | Residue-A v1 compression schema recorded.                     |
// | 1.1     | 2026-07-02 | —      | AR-1 L-3: Add coherence gates — NetRelationship finite in     |
// |         |            |        | [0,1] (NaN fails closed); every retained episodeId must sit   |
// |         |            |        | below NextEpisodeId or rehydration would reuse an id and      |
// |         |            |        | break FR-LW-009 monotonicity.                                 |
// | 1.2     | 2026-07-02 | —      | AR-2 L-1/L-2: Add also requires strictly-ascending retained   |
// |         |            |        | episodeIds (duplicates rehydrate into ambiguous pin identity) |
// |         |            |        | and salience finite in [0,1] (NaN fails closed).              |
// | 1.3     | 2026-07-02 | —      | AR-3 L-2 (doc-only): TryTake ordering contract — verify no    |
// |         |            |        | live edge exists BEFORE taking (destructive removal; a post-  |
// |         |            |        | take InsertEdge throw would strand the summary, FR-LW-025).   |
// | 1.4     | 2026-07-02 | —      | Slice-2 AR-1 M-2: TryPeek added — the non-destructive         |
// |         |            |        | verify-before-take companion (pre-take validation must not    |
// |         |            |        | run after the destructive removal, FR-LW-025).                |
#endregion
