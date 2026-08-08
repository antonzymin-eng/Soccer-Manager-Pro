// File:     src/living-world/RelationshipEdge.cs
// Created:  2026-06-21
// Modified: 2026-08-08
// Author:   —
// Spec:     Living World System #22 §2.2.1, §3.1, Appendix C, Code Standards #20
// Purpose:  RelationshipEdge value type: a directed per-ordered-pair relationship edge.

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// A directed relationship edge between an ordered node pair. #22 §2.2.1 / §3.1.
    /// The graph is directed: symmetric relationships are two equal directed edges. Not every layer is
    /// active on every edge — the ActiveLayers mask records which are meaningful (bit positions =
    /// RelationshipLayer ordinals); inactive layers hold 0.0 and are excluded from updates and the
    /// F6 [0,1] invariant (no NaN sentinel — it would break the bitwise round-trip / digest).
    /// `PlayerEdge` is a READ-ONLY mirror of vol-2 §2.1's authoritative edge — never mutated here
    /// (FR-LW-004 / KD-9); only Affinity / Trust are owned and written by this layer.
    /// </summary>
    public struct RelationshipEdge
    {
        /// <summary>Directed-pair source entity id (int per project convention; no shared EntityId type).</summary>
        public int FromId;

        /// <summary>Directed-pair target entity id.</summary>
        public int ToId;

        /// <summary>Bitmask of meaningful layers (bit i = RelationshipLayer ordinal i). FR-LW-005.</summary>
        public byte ActiveLayers;

        /// <summary>vol-2 §2.1 relationship strength [0,1] — read-only mirror, never mutated here.</summary>
        public float PlayerEdge;

        /// <summary>Manager↔non-player personal relationship [0,1] (owned).</summary>
        public float Affinity;

        /// <summary>Directional trust [0,1] (owned).</summary>
        public float Trust;

        /// <summary>Bounded ring buffer of episodes (FR-LW-008).</summary>
        public MemoryEpisode[] Memory;

        /// <summary>Per-edge monotonic episodeId allocator high-water mark (survives cold-store via
        /// ColdSummary.NextEpisodeId; never re-derived from retained ids). FR-LW-009 / §3.5.</summary>
        public uint NextEpisodeId;

        /// <summary>True if the given layer's bit is set in <see cref="ActiveLayers"/>.</summary>
        public bool IsLayerActive(RelationshipLayer layer)
            => (ActiveLayers & (1 << (int)layer)) != 0;
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-06-21 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
