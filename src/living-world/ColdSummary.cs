// File:     src/living-world/ColdSummary.cs
// Created:  2026-06-21
// Modified: 2026-06-21
// Author:   —
// Spec:     Living World System #22 §2.2.5, §3.5, Code Standards #20
// Purpose:  ColdSummary value type: compressed standing of a departed contact.

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// The compressed cold-stored standing of a contact that left the active set. #22 §2.2.5 / §3.5.
    /// Demotion is lossy by design: only the top-N salient episodes survive (COLD_SUMMARY_RETAINED_
    /// EPISODES). Rehydration resumes the edge's episodeId counter from <see cref="NextEpisodeId"/>
    /// (never max(retained)+1, which would reuse compressed-away ids) so monotonicity holds (FR-LW-009).
    /// </summary>
    public struct ColdSummary
    {
        /// <summary>The cold-stored contact's entity id.</summary>
        public int EntityId;

        /// <summary>Retained so rehydration can reconstruct which layers to populate (FR-LW-005).</summary>
        public byte ActiveLayers;

        /// <summary>Compressed net relationship standing in [0,1].</summary>
        public float NetRelationship;

        /// <summary>Per-edge episodeId high-water mark — preserves monotonicity across rehydration.</summary>
        public uint NextEpisodeId;

        /// <summary>Top-N episodes by salience that survived demotion.</summary>
        public MemoryEpisode[] RetainedEpisodes;
    }
}
