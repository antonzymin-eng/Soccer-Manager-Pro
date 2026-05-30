// File:     src/deterministic-sim/DespawnEntry.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §3.2.3, §3.4, Code Standards #20
// Purpose:  Tombstone entry for the DespawnLog. Records the final authoritative state of a despawned
//           entity's RNG cursor so replay can verify EntityId no-reuse across match lifetime.

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Tombstone record for a despawned entity.
    /// Written to DespawnLog by the Resolve phase; read-only by Snapshot and Replay.
    /// EntityId no-reuse is a Tier A invariant spanning the entire match lifetime.
    /// Deterministic Simulation #16 §3.2.3.
    /// </summary>
    public readonly struct DespawnEntry
    {
        /// <summary>Entity that was despawned. Must be unique for the match lifetime (Tier A). §3.2.3.</summary>
        public readonly int EntityId;

        /// <summary>The actionOrdinal of the stream owning this entity at the moment of despawn. §3.2.5.</summary>
        public readonly ulong FinalActionOrdinal;

        /// <summary>The RNG cursor value for this entity's stream at the moment of despawn. §3.2.5.</summary>
        public readonly ulong FinalRngCursor;

        /// <summary>Physics tick at which the entity was despawned.</summary>
        public readonly ulong DespawnTick;

        /// <summary>Constructs a DespawnEntry tombstone.</summary>
        public DespawnEntry(int entityId, ulong finalActionOrdinal, ulong finalRngCursor, ulong despawnTick)
        {
            EntityId            = entityId;
            FinalActionOrdinal  = finalActionOrdinal;
            FinalRngCursor      = finalRngCursor;
            DespawnTick         = despawnTick;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
