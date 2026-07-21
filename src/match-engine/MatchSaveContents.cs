// File:     src/match-engine/MatchSaveContents.cs
// Created:  2026-07-21
// Modified: 2026-07-21
// Author:   —
// Spec:     On-disk match save file (docs/tracking/match-save-file-design.md) §4 / KD-2..KD-5;
//           Match Engine design note §5 Phase G-Phase 3; Code Standards #20
// Purpose:  The decoded contents of a match save blob: the boot match seed plus the snapshot
//           (header, payload) pair. MatchSaveCodec.Decode returns this; MatchSaveManager.Load
//           threads it into MatchEngine.RestoreFromSnapshot.

using TacticalDirector.DeterministicSim;

namespace TacticalDirector.MatchEngine
{
    /// <summary>
    /// The three things a match save persists: the boot <see cref="MatchSeed"/> (which the world-state
    /// payload does not carry — a boot constant, not cross-tick state, per snapshot-deserialize KD-7),
    /// and the snapshot <see cref="Header"/> + <see cref="Payload"/> pair that
    /// <see cref="MatchEngine.RestoreFromSnapshot"/> consumes. Produced by
    /// <see cref="MatchSaveCodec.Decode"/>; a distinct-squad restore additionally needs an
    /// <see cref="ISquadProvider"/>, which is a Load-time parameter and is NOT part of the save
    /// (match-save-file-design.md KD-4).
    /// </summary>
    public readonly struct MatchSaveContents
    {
        /// <summary>The boot match seed the save was captured from.</summary>
        public readonly ulong MatchSeed;

        /// <summary>The reconstructed snapshot header (fingerprint + digest chain + tick + versions + cursor).</summary>
        public readonly SnapshotHeader Header;

        /// <summary>The reconstructed snapshot payload (the cross-tick world-state bytes).</summary>
        public readonly SnapshotPayload Payload;

        /// <summary>Constructs the decoded save contents.</summary>
        public MatchSaveContents(ulong matchSeed, SnapshotHeader header, SnapshotPayload payload)
        {
            MatchSeed = matchSeed;
            Header    = header;
            Payload   = payload;
        }
    }
}

#region VersionHistory
// | Version | Date       | Author | Notes                   |
// | 1.0     | 2026-07-21 | —      | Initial implementation. |
#endregion
