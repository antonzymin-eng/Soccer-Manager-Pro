// File:     src/living-world/Arc.cs
// Created:  2026-06-21
// Modified: 2026-08-08
// Author:   —
// Spec:     Living World System #22 §2.2.4, §3.4, Code Standards #20
// Purpose:  Arc value type: a serialised emergent-narrative state machine.

namespace TacticalDirector.LivingWorld
{
    /// <summary>
    /// A live narrative arc. #22 §2.2.4 / §3.4. Serialisable value type (FR-LW-022).
    /// `State` byte values are APPEND-only per kind (FR-LW-028). An arc pins its source episodes
    /// (by (edge, episodeId)) non-evictable until it resolves (FR-LW-018), and is protected from
    /// active-set demotion of the contacts it references (§3.5).
    /// </summary>
    public struct Arc
    {
        /// <summary>A pinned episode reference: the edge entity and the episode's stable id.</summary>
        public readonly struct PinnedEpisode
        {
            public readonly int EdgeFromId;
            public readonly int EdgeToId;
            public readonly uint EpisodeId;

            public PinnedEpisode(int edgeFromId, int edgeToId, uint episodeId)
            {
                EdgeFromId = edgeFromId;
                EdgeToId = edgeToId;
                EpisodeId = episodeId;
            }
        }

        /// <summary>The arc kind (ordinal-stable; also the non-entity evaluation order, FR-LW-017).</summary>
        public ArcKind Kind;

        /// <summary>Arc-specific lifecycle state (APPEND-only per kind, FR-LW-028).</summary>
        public byte State;

        /// <summary>Provenance captured at spawn (FR-LW-016 / KD-8).</summary>
        public SpawnCause Cause;

        /// <summary>Episodes pinned non-evictable until this arc resolves (FR-LW-018).</summary>
        public PinnedEpisode[] PinnedEpisodes;

        /// <summary>Calendar day the arc spawned.</summary>
        public uint SpawnTick;

        /// <summary>Per-instance liveness bound: must resolve by this tick (FR-LW-014 / §6.2).</summary>
        public uint MaxLifetimeTick;

        /// <summary>True if this arc has exceeded its liveness bound at the given tick.</summary>
        public bool IsExpired(uint worldTick) => worldTick > MaxLifetimeTick;
    }
}

#region VersionHistory
// | Version | Date       | Author       | Notes                                                     |
// | 1.0     | 2026-06-21 | —            | Initial file. |
// | 1.1     | 2026-08-08 | Claude Code  | Added the required #region VersionHistory block (FR-CS-058; tools/recurring-defect-lint.py hygiene pass). |
#endregion
