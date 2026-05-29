// File:     src/deterministic-sim/DespawnLog.cs
// Created:  2026-05-29
// Modified: 2026-05-29
// Author:   —
// Spec:     Deterministic Simulation #16 §3.2.3, §3.4, §3.6.1, Code Standards #20
// Purpose:  Pre-allocated tombstone list for despawned entities. Tier A authoritative state.
//           Written by Resolve phase (append-only); read by Snapshot and Replay phases.
//           Included in SnapshotPayload digest scope. Zero allocation on hot path after construction.

namespace TacticalDirector.DeterministicSim
{
    /// <summary>
    /// Pre-allocated tombstone log for despawned entities.
    /// Tier A authoritative state; written append-only by the Resolve phase.
    /// Snapshot phase includes all entries in SnapshotPayload digest. §3.2.3 / §3.6.1.
    /// </summary>
    public sealed class DespawnLog
    {
        private readonly DespawnEntry[] _entries;
        private int _count;

        /// <summary>Number of tombstone entries currently in the log.</summary>
        public int Count => _count;

        /// <summary>Pre-allocated capacity (MaxDespawnEntries).</summary>
        public int Capacity => _entries.Length;

        /// <summary>
        /// Allocates the tombstone buffer at match start.
        /// No allocation occurs on the hot path after this call.
        /// </summary>
        public DespawnLog()
        {
            _entries = new DespawnEntry[DeterministicSimConstants.MaxDespawnEntries];
            _count   = 0;
        }

        /// <summary>
        /// Appends a tombstone for a despawned entity.
        /// Called only from the Resolve phase (§3.6.1).
        /// Returns false and discards the entry if the log is full (capacity overflow: should never occur in a valid match).
        /// </summary>
        public bool Append(DespawnEntry entry)
        {
            if (_count >= _entries.Length)
            {
                return false;
            }

            _entries[_count++] = entry;
            return true;
        }

        /// <summary>Returns the tombstone entry at the given index. No bounds check on hot path — caller must guard.</summary>
        public ref readonly DespawnEntry GetEntry(int index) => ref _entries[index];

        /// <summary>Returns true if the given EntityId appears as a tombstone in this log.
        /// Used by replay to enforce the no-reuse invariant. Linear scan; not hot-path.</summary>
        public bool ContainsEntity(int entityId)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_entries[i].EntityId == entityId)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Resets the log to empty (match restart or replay rehydration).
        /// Does not reallocate the buffer.
        /// </summary>
        public void Clear()
        {
            _count = 0;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-05-29 | —      | Initial implementation. |
#endregion
